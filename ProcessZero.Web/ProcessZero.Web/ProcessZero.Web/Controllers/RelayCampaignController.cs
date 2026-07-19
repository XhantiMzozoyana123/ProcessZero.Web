using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Hangfire;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using ProcessZero.Infrastructure.Services;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// RelayCampaign Controller - SmartLead-style outreach campaign automation.
    /// 
    /// ENTITIES USED:
    /// 
    /// 1. RelayCampaign (Domain Model)
    ///    - Represents an outreach campaign container
    ///    - Key Columns:
    ///      * Id (int, Primary Key, inherited from BaseEntity)
    ///      * Name (string) - Campaign name (e.g., "Q4 2024 Outreach")
    ///      * Description (string) - Campaign details/notes
    ///      * IsActive (bool) - Whether campaign is currently running
    ///      * DailySendLimit (int) - Max emails to send per day
    ///      * StartDate (DateTime?) - Campaign launch date
    ///      * EndDate (DateTime?) - Campaign end date
    ///      * CreatedAt (DateTime, inherited from BaseEntity)
    ///      * UpdatedAt (DateTime?, inherited from BaseEntity)
    ///    - Relationships:
    ///      * Leads (ICollection<RelayCampaignLead>) - Many-to-many junction with RelayLead
    ///      * Sequences (ICollection<RelaySequence>) - Email sequences to run
    ///      * Inboxes (ICollection<RelayCampaignInbox>) - Associated reply inboxes
    /// 
    /// 2. RelayLead (Domain Model)
    ///    - Enriched lead data (synced from LeadLake, ready for campaigns)
    ///    - Key Columns:
    ///      * Id (int, Primary Key)
    ///      * FirstName (string) - Lead's first name
    ///      * LastName (string) - Lead's last name
    ///      * Email (string) - Contact email (primary outreach channel)
    ///      * Phone (string) - Phone number
    ///      * Company (string) - Company/organization name
    ///      * JobTitle (string) - Role title at company
    ///      * Location (string) - Geographic location (city/state/country)
    ///      * Industry (enum: LeadLakeIndustry) - Business sector
    ///      * Intent (enum: LeadIntent) - Engagement likelihood (High/Medium/Low)
    ///      * CreatedAt, UpdatedAt (DateTime)
    ///    - Relationships:
    ///      * Campaigns (ICollection<RelayCampaignLead>) - Campaigns this lead is in
    ///      * Activities (ICollection<RelayEmailActivity>) - Email sends, opens, clicks
    /// 
    /// 3. RelayCampaignLead (Junction Entity - Many-to-Many)
    ///    - Links RelayCampaign to RelayLead with campaign-specific state
    ///    - Key Columns:
    ///      * Id (int, Primary Key)
    ///      * RelayCampaignId (int, Foreign Key) - Which campaign
    ///      * RelayLeadId (int, Foreign Key) - Which lead (links to RelayLead.Id)
    ///      * CurrentSequenceStepId (int?) - Which email in sequence to send next
    ///      * Status (enum: CampaignLeadStatus) - Lead state in campaign:
    ///        - Pending: not yet contacted
    ///        - Active: in progress (waiting for next step or reply)
    ///        - Replied: lead responded to email
    ///        - Completed: lead completed campaign sequence
    ///        - Bounced: email bounced
    ///        - Unsubscribed: lead opted out
    ///      * Replied (bool) - Has lead replied to any email?
    ///      * Unsubscribed (bool) - Did lead unsubscribe?
    ///      * Completed (bool) - Did lead finish sequence?
    ///      * CreatedAt, UpdatedAt (DateTime)
    ///    - Relationships:
    ///      * RelayCampaign - Back-reference to campaign
    ///      * RelayLead - Back-reference to lead
    ///      * CurrentSequenceStep - Which step to send next
    /// 
    /// 4. LeadLake (Domain Model)
    ///    - Raw lead pool; source for RelayLead enrichment
    ///    - Key Columns:
    ///      * Id (int, Primary Key)
    ///      * FirstName (string)
    ///      * LastName (string)
    ///      * Email (string) - Unique identifier for lead
    ///      * Phone (string)
    ///      * Company (string)
    ///      * Job (string) - Job function
    ///      * Location (string)
    ///      * Industry (enum: LeadLakeIndustry)
    ///      * Intent (enum: LeadIntent)
    ///    - Used by: CSV import process reads from LeadLake, adds to RelayLeadService
    /// 
    /// WORKFLOW:
    /// 1. Admin creates RelayCampaign (name, description)
    /// 2. Admin uploads CSV → mapped to LeadLake entries → leads are synced/created in RelayLead
    /// 3. RelayCampaignLead junction entries created linking campaign to leads
    /// 4. Admin can batch-edit leads (change status, mark replied, etc.) in RelayCampaignLead
    /// 5. Admin starts campaign → Hangfire job picks sequence steps and sends emails
    /// 6. LeadLake provides raw lead pool; RelayLead is the enriched, reusable lead DB
    /// 
    /// High-level endpoints for admin users to:
    /// - Create and manage campaigns
    /// - Import leads via CSV (batch, background processing)
    /// - Modify leads in batches (add/remove/edit transactionally)
    /// - Monitor import progress
    /// - Start campaign processing
    /// 
    /// Design principles:
    /// - Thin controller (orchestration only, no business logic)
    /// - All requests/responses use explicit DTOs
    /// - Batch-only lead operations (never single-lead from UI)
    /// - Background jobs for long-running work (CSV import, campaign runs)
    /// - Transactional batch operations (all-or-nothing)
    /// </summary>
    [ApiController]
    [Route("api/relay/campaigns")]
    [Authorize(Roles = "Admin")]
    public class RelayCampaignController : ControllerBase
    {
        private readonly IRelayCampaignService _campaignService;
        private readonly IRelayLeadService _leadService;
        private readonly IImportStatusService _statusService;

        public RelayCampaignController(
            IRelayCampaignService campaignService,
            IRelayLeadService leadService,
            IImportStatusService statusService)
        {
            _campaignService = campaignService;
            _leadService = leadService;
            _statusService = statusService;
        }

        // L
        // 📋 CAMPAIGN CRUD OPERATIONS
        // ─────────────────────────────────────────────

        /// <summary>
        /// Create a new outreach campaign — SmartLead-style, fully configured in one request.
        /// 
        /// ENTITY IMPACT (all created atomically in a single transaction):
        /// - RelayCampaign record:
        ///   * Name, Description (metadata)
        ///   * DailySendLimit (throttle), StartDate, EndDate (schedule)
        ///   * IsActive: defaults to false (must activate via /activate endpoint)
        /// - RelaySequence records (request.Sequences): the email cadence(s)
        ///   * MessageRotationEnabled / InboxRotationEnabled flags
        /// - RelaySequenceStep records (sequence.Steps): each touch in the cadence
        ///   * StepOrder (send order), DelayDays (wait before this step)
        /// - RelayEmailVariant records (step.Variants): the actual MESSAGES + A/B test
        ///   * Subject, HtmlBody, VariantName ("A"/"B"...), Weight (A/B split)
        /// - RelayCampaignInbox records (request.InboxAccountIds): sending inboxes
        /// - RelayCampaignLead records (request.LeadIds): leads enrolled (Status = Pending)
        /// 
        /// All collections are optional — a minimal request with just Name still works,
        /// but supplying sequences/steps/variants/inboxes makes the campaign ready to
        /// activate and run. Leads can also be added later via import or batch endpoints.
        /// 
        /// RESPONSE:
        /// Returns CampaignSummaryDto with Id (newly assigned primary key) and counts.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(CampaignSummaryDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateCampaign([FromBody] CreateCampaignRequest request)
        {
            if (request == null)
                return BadRequest("Request required");

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Campaign name required");

            int id;
            try
            {
                // Builds the entire campaign graph (sequences, steps, A/B variants,
                // inboxes, leads) in one transaction.
                id = await _campaignService.CreateFullCampaignAsync(request);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ProblemDetails { Detail = ex.Message });
            }

            var created = await _campaignService.GetCampaignAsync(id);

            var dto = MapCampaignToSummary(created);
            return CreatedAtAction(nameof(GetCampaign), new { campaignId = id }, dto);
        }


        /// <summary>
        /// Get campaign details including lead counts and status.
        /// </summary>
        [HttpGet("{campaignId}")]
        [ProducesResponseType(typeof(CampaignDetailDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetCampaign(int campaignId)
        {
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound();

            var dto = MapCampaignToDetail(campaign);
            return Ok(dto);
        }

        /// <summary>
        /// List all campaigns (paginated).
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<CampaignSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListCampaigns([FromQuery] int skip = 0, [FromQuery] int take = 10)
        {
            if (take > 100) take = 100;
            var campaigns = await _campaignService.GetActiveCampaignsAsync();
            var dtos = campaigns
                .OrderByDescending(x => x.CreatedAt)
                .Skip(skip)
                .Take(take)
                .Select(MapCampaignToSummary)
                .ToList();

            return Ok(dtos);
        }

        /// <summary>
        /// Update campaign metadata (name, description).
        /// </summary>
        [HttpPut("{campaignId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UpdateCampaign(int campaignId, [FromBody] UpdateCampaignRequest request)
        {
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(request.Name))
                campaign.Name = request.Name;

            if (request.Description != null)
                campaign.Description = request.Description;

            campaign.UpdatedAt = DateTime.UtcNow;
            await _campaignService.UpdateCampaignAsync(campaign);

            return Ok(new { message = "Campaign updated" });
        }

        /// <summary>
        /// Activate a campaign (allow processing to begin).
        /// </summary>
        [HttpPost("{campaignId}/activate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ActivateCampaign(int campaignId)
        {
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound();

            await _campaignService.ActivateCampaignAsync(campaignId);
            return Ok(new { message = "Campaign activated" });
        }

        /// <summary>
        /// Pause a campaign (stop processing).
        /// </summary>
        [HttpPost("{campaignId}/pause")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PauseCampaign(int campaignId)
        {
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound();

            await _campaignService.PauseCampaignAsync(campaignId);
            return Ok(new { message = "Campaign paused" });
        }

        /// <summary>
        /// Delete campaign and optionally all associated leads.
        /// </summary>
        [HttpDelete("{campaignId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCampaign(int campaignId, [FromQuery] bool removeLeads = true)
        {
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound("Campaign not found");

            if (removeLeads && campaign.Leads != null && campaign.Leads.Any())
            {
                var leadIds = campaign.Leads.Select(x => x.RelayLeadId).ToList();
                await _leadService.RemoveLeadsFromCampaignAsync(campaignId, leadIds);
            }

            await _campaignService.DeleteCampaignAsync(campaignId);
            return Ok(new { message = "Campaign deleted" });
        }

        // ─────────────────────────────────────────────
        // 📥 LEAD IMPORT (CSV)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Upload CSV file to import leads into campaign.
        /// Enqueues background job for processing.
        /// Returns jobId for status polling.
        /// 
        /// ENTITY FLOW:
        /// 1. CSV rows are parsed and mapped to LeadLake entries (raw lead pool)
        /// 2. LeadLake entries are synced/created in RelayLead (enriched lead DB)
        /// 3. RelayCampaignLead junction records created linking campaign to leads
        /// 4. Each RelayCampaignLead starts with:
        ///    - Status: "Pending"
        ///    - CurrentSequenceStepId: null (no sequence assigned yet)
        ///    - Replied: false
        ///    - Unsubscribed: false
        ///    - Completed: false
        /// 
        /// CSV format: Email, FirstName, LastName, Company
        /// Example:
        ///   Email,FirstName,LastName,Company
        ///   john@example.com,John,Doe,Acme Inc
        ///   jane@example.com,Jane,Smith,Tech Corp
        /// </summary>
        [HttpPost("{campaignId}/import")]
        [ProducesResponseType(typeof(ImportJobDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> ImportLeads(int campaignId, IFormFile file)
        {
            // Validate campaign exists
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound("Campaign not found");

            // Validate file
            if (file == null || file.Length == 0)
                return BadRequest("CSV file required");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only .csv files accepted");

            if (file.Length > 10 * 1024 * 1024) // 10 MB limit
                return BadRequest("File size cannot exceed 10 MB");

            var jobId = Guid.NewGuid().ToString("N");

            // Save temp file
            var tempPath = Path.GetTempFileName();
            try
            {
                await using (var stream = System.IO.File.Create(tempPath))
                {
                    await file.CopyToAsync(stream);
                }

                // Track job status
                _statusService.Create(jobId, campaignId);

                // Enqueue background job
                BackgroundJob.Enqueue<ImportProcessor>(proc =>
                    proc.ProcessAsync(jobId, campaignId, tempPath));

                return Accepted(new ImportJobDto(jobId, "Import started. Check status endpoint."));
            }
            catch (Exception ex)
            {
                try { System.IO.File.Delete(tempPath); } catch { }
                return StatusCode(500, new ProblemDetails { Detail = ex.Message });
            }
        }

        /// <summary>
        /// Get import job status and progress.
        /// </summary>
        [HttpGet("{campaignId}/import/{jobId}/status")]
        [ProducesResponseType(typeof(ImportStatusDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult GetImportStatus(int campaignId, string jobId)
        {
            var status = _statusService.Get(jobId);
            if (status == null || status.CampaignId != campaignId)
                return NotFound();

            return Ok(status);
        }

        // ─────────────────────────────────────────────
        // 👥 BATCH LEAD OPERATIONS
        // ─────────────────────────────────────────────

        /// <summary>
        /// Batch modify leads: add, remove, and edit in a single transaction.
        /// 
        /// ENTITY OPERATIONS:
        /// 
        /// ADD: Creates RelayCampaignLead records linking RelayLead IDs to this campaign
        ///   - New entries: Status = "Pending", Replied = false, Unsubscribed = false, Completed = false
        ///   - RelayLeadId references the ID in RelayLead table
        ///   - RelayLead must exist (typically from prior CSV import)
        /// 
        /// REMOVE: Deletes RelayCampaignLead records from campaign
        ///   - Only removes the junction record, not the RelayLead itself
        ///   - RelayLead remains in database (can be reused in other campaigns)
        /// 
        /// EDIT: Updates RelayCampaignLead columns:
        ///   - Status: Update the current lead state (Pending, Active, Replied, Completed, Bounced, Unsubscribed)
        ///   - CurrentSequenceStepId: Which step to send next (null = no sequence assigned yet)
        ///   - Replied: Set to true if lead responded
        ///   - Unsubscribed: Set to true if lead opted out
        ///   - Completed: Set to true if lead finished sequence
        /// 
        /// TRANSACTIONAL: All operations wrapped in DB transaction
        ///   - If any operation fails, entire batch rolls back
        ///   - Ensures consistency (no partial updates)
        /// 
        /// Example request:
        /// {
        ///   "add": [10, 20, 30],
        ///   "remove": [5, 6],
        ///   "edit": [
        ///     { "leadId": 15, "status": "Active", "replied": false },
        ///     { "leadId": 16, "currentSequenceStepId": 3 }
        ///   ]
        /// }
        /// </summary>
        [HttpPost("{campaignId}/leads/batch")]
        [ProducesResponseType(typeof(BatchResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> BatchModifyLeads(int campaignId, [FromBody] BatchLeadModificationRequest request)
        {
            if (request == null)
                return BadRequest("Request required");

            // Validate campaign exists
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound("Campaign not found");

            // Convert request to DTO
            var editList = request.Edit == null ? null : request.Edit
                .Select(e => new RelayLeadUpdateDto(
                    e.LeadId,
                    e.CurrentSequenceStepId,
                    e.Status,
                    e.Replied,
                    e.Unsubscribed,
                    e.Completed))
                .ToList();

            var dto = new BatchLeadModificationDto(request.Add, request.Remove, editList);

            // Execute batch operation (transactional)
            var result = await _leadService.ProcessBatchAsync(campaignId, dto);

            return Ok(result);
        }

        // ─────────────────────────────────────────────
        // 🚀 CAMPAIGN EXECUTION
        // ─────────────────────────────────────────────

        /// <summary>
        /// Start campaign processing.
        /// Enqueues sequence engine job to Hangfire for background execution.
        /// 
        /// WORKFLOW:
        /// 1. Enqueues IRelaySequenceService.ProcessSequenceAsync(campaignId) to Hangfire
        /// 2. Sequence engine queries RelayCampaignLead records for campaign:
        ///    - Filters by Status = "Active" or "Pending"
        ///    - For each lead, checks CurrentSequenceStepId to determine next action
        ///    - Fetches RelaySequenceStep details (template, delay, etc.)
        /// 3. Based on step type and lead state, enqueues send jobs
        /// 4. Email service sends message, logs in RelayEmailActivity
        /// 5. Updates RelayCampaignLead.CurrentSequenceStepId to next step
        /// 6. Sets RelayCampaignLead.Status to track progress (Active, Completed, etc.)
        /// 
        /// Returns immediately with jobId (202 Accepted) — processing happens in background.
        /// Monitor progress via Hangfire dashboard or job status tracking.
        /// </summary>
        [HttpPost("{campaignId}/start")]
        [ProducesResponseType(typeof(CampaignJobDto), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> StartCampaign(int campaignId, [FromBody] StartCampaignRequest? request = null)
        {
            var campaign = await _campaignService.GetCampaignAsync(campaignId);
            if (campaign == null)
                return NotFound("Campaign not found");

            var queueName = request?.HangfireQueueName ?? "default";

            // Enqueue sequence processor
            var jobId = BackgroundJob.Enqueue<IRelaySequenceService>(
                svc => svc.ProcessSequenceAsync(campaignId));

            return Accepted(new CampaignJobDto(
                jobId,
                "Campaign processing started. Visit Hangfire dashboard to monitor.",
                queueName));
        }

        // ─────────────────────────────────────────────
        // 🛠️ HELPER METHODS (MAPPING)
        // ─────────────────────────────────────────────

        /// <summary>
        /// Map RelayCampaign entity to CampaignSummaryDto.
        /// 
        /// Entity Usage:
        /// - campaign.Id (PK)
        /// - campaign.Name, Description (metadata)
        /// - campaign.IsActive (campaign status flag)
        /// - campaign.Leads (ICollection<RelayCampaignLead>) — junction records
        ///   * Filtered by Status == CampaignLeadStatus.Active to get active count
        ///   * Total count = all leads in campaign (any status)
        /// - campaign.CreatedAt (timestamp)
        /// </summary>
        private CampaignSummaryDto MapCampaignToSummary(RelayCampaign campaign)
        {
            return new CampaignSummaryDto(
                campaign.Id,
                campaign.Name,
                campaign.Description,
                campaign.IsActive,
                campaign.Leads?.Count(x => x.Status == CampaignLeadStatus.Active) ?? 0,
                campaign.Leads?.Count ?? 0,
                campaign.CreatedAt);
        }

        /// <summary>
        /// Map RelayCampaign entity to CampaignDetailDto.
        /// 
        /// Entity Usage:
        /// - campaign.Leads (ICollection<RelayCampaignLead>) — filtered by status:
        ///   * TotalLeadCount: all leads (any Status)
        ///   * ActiveLeadCount: Status == Active
        ///   * CompletedLeadCount: Completed == true
        ///   * RepliedLeadCount: Replied == true
        /// - campaign.UpdatedAt (last modification timestamp)
        /// </summary>
        private CampaignDetailDto MapCampaignToDetail(RelayCampaign campaign)
        {
            return new CampaignDetailDto(
                campaign.Id,
                campaign.Name,
                campaign.Description,
                campaign.IsActive,
                campaign.Leads?.Count ?? 0,
                campaign.Leads?.Count(x => x.Status == CampaignLeadStatus.Active) ?? 0,
                campaign.Leads?.Count(x => x.Completed) ?? 0,
                campaign.Leads?.Count(x => x.Replied) ?? 0,
                campaign.CreatedAt,
                campaign.UpdatedAt);
        }
    }

    // ─────────────────────────────────────────────
    // RESPONSE DTOs (not in RelayDtos.cs)
    // ─────────────────────────────────────────────

    public record ImportJobDto(string JobId, string Message);
    public record CampaignJobDto(string HangfireJobId, string Message, string Queue);
}
