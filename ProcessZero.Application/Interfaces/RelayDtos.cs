using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;

namespace ProcessZero.Application.Interfaces
{
    // ─────────────────────────────────────────────
    // CAMPAIGN REQUEST / RESPONSE DTOs
    // ─────────────────────────────────────────────

    public record CreateCampaignRequest
    (
        string Name,
        string? Description = null,
        int DailySendLimit = 50,
        DateTime? StartDate = null,
        DateTime? EndDate = null,
        List<CreateSequenceRequest>? Sequences = null,
        List<int>? InboxAccountIds = null,
        List<int>? LeadIds = null
    );

    // ─────────────────────────────────────────────
    // SEQUENCE / STEP / VARIANT (A/B) BUILD DTOs
    // Used to create a full SmartLead-style campaign in one request:
    // campaign -> sequences -> steps -> variants (A/B messages)
    // ─────────────────────────────────────────────

    public record CreateSequenceRequest
    (
        string Name,
        bool MessageRotationEnabled = false,
        bool InboxRotationEnabled = false,
        List<CreateSequenceStepRequest>? Steps = null
    );

    public record CreateSequenceStepRequest
    (
        string Name,
        int StepOrder,
        int DelayDays = 0,
        bool IsActive = true,
        List<CreateEmailVariantRequest>? Variants = null
    );

    public record CreateEmailVariantRequest
    (
        string Subject,
        string HtmlBody,
        string VariantName = "A",
        int Weight = 50
    );


    public record UpdateCampaignRequest
    (
        string? Name = null,
        string? Description = null
    );

    public record CampaignSummaryDto
    (
        int Id,
        string Name,
        string? Description,
        bool IsActive,
        int ActiveLeadCount,
        int TotalLeadCount,
        DateTime CreatedAt
    );

    public record CampaignDetailDto
    (
        int Id,
        string Name,
        string? Description,
        bool IsActive,
        int TotalLeadCount,
        int ActiveLeadCount,
        int CompletedLeadCount,
        int RepliedLeadCount,
        DateTime CreatedAt,
        DateTime? UpdatedAt
    );

    // ─────────────────────────────────────────────
    // LEAD BATCH OPERATION DTOs
    // ─────────────────────────────────────────────

    public record UpdateLeadRequest
    (
        int LeadId,
        int? CurrentSequenceStepId = null,
        CampaignLeadStatus? Status = null,
        bool? Replied = null,
        bool? Unsubscribed = null,
        bool? Completed = null
    );

    public record RelayLeadUpdateDto
    (
        int LeadId,
        int? CurrentSequenceStepId = null,
        CampaignLeadStatus? Status = null,
        bool? Replied = null,
        bool? Unsubscribed = null,
        bool? Completed = null
    );

    public record BatchLeadModificationRequest
    (
        List<int>? Add,
        List<int>? Remove,
        List<UpdateLeadRequest>? Edit
    );

    public record BatchLeadModificationDto
    (
        List<int>? Add,
        List<int>? Remove,
        List<RelayLeadUpdateDto>? Edit
    );

    public record BatchResultDto
    (
        int Added,
        int Removed,
        int Updated,
        List<string>? Errors = null
    );

    public record LeadStateDto
    (
        int Id,
        int RelayCampaignId,
        int RelayLeadId,
        int? CurrentSequenceStepId,
        CampaignLeadStatus Status,
        bool Replied,
        bool Unsubscribed,
        bool Completed,
        DateTime CreatedAt,
        DateTime UpdatedAt
    );

    // ─────────────────────────────────────────────
    // IMPORT DTOs
    // ─────────────────────────────────────────────

    public record CsvLeadRow
    (
        string? Email = null,
        string? FirstName = null,
        string? LastName = null,
        string? Company = null
    );

    public record ImportStatusDto
    (
        string JobId,
        int CampaignId,
        int TotalRows,
        int ProcessedRows,
        bool Completed,
        string? ErrorMessage = null
    );

    public record ImportResultDto
    (
        string JobId,
        int LeadsAdded,
        int LeadsFailed,
        List<string>? ErrorMessages = null
    );

    // ─────────────────────────────────────────────
    // CAMPAIGN START REQUEST
    // ─────────────────────────────────────────────

    public record StartCampaignRequest
    (
        string? HangfireQueueName = "default"
    );

    public record StartCampaignResultDto
    (
        string HangfireJobId,
        string Message
    );

    // ─────────────────────────────────────────────
    // IMPORT STATUS SERVICE
    // ─────────────────────────────────────────────

    public interface IImportStatusService
    {
        void Create(string jobId, int campaignId);

        void UpdateProgress(string jobId, int processedRows, int totalRows);

        void Complete(string jobId);

        void Fail(string jobId, string message);

        ImportStatusDto? Get(string jobId);
    }
}
