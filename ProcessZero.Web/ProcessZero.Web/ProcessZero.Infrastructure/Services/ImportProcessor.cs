using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using System.Globalization;

namespace ProcessZero.Infrastructure.Services
{
    // Background worker that processes CSV imports and persists leads in batch
    public class ImportProcessor
    {
        private readonly ApplicationDbContext _context;
        private readonly ILeadLakeService _leadLakeService;
        private readonly IRelayLeadService _leadService;
        private readonly IImportStatusService _statusService;

        public ImportProcessor(
            ApplicationDbContext context,
            ILeadLakeService leadLakeService,
            IRelayLeadService leadService,
            IImportStatusService statusService)
        {
            _context = context;
            _leadLakeService = leadLakeService;
            _leadService = leadService;
            _statusService = statusService;
        }

        public async Task ProcessAsync(string jobId, int campaignId, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    throw new FileNotFoundException($"Upload file not found: {filePath}");

                // Parse CSV
                var rows = new List<CsvLeadRow>();
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    csv.Context.RegisterClassMap<CsvLeadRowMap>();
                    rows = csv.GetRecords<CsvLeadRow>().ToList();
                }

                var totalRows = rows.Count;
                _statusService.UpdateProgress(jobId, 0, totalRows);

                if (totalRows == 0)
                {
                    _statusService.Complete(jobId);
                    return;
                }

                // Map CSV rows to LeadLake IDs by email
                var leadIds = new List<int>();
                var errors = new List<string>();

                for (int i = 0; i < rows.Count; i++)
                {
                    try
                    {
                        var row = rows[i];
                        if (string.IsNullOrWhiteSpace(row.Email))
                        {
                            errors.Add($"Row {i + 1}: Email is required");
                            continue;
                        }

                        // Try to find LeadLake by email
                        var lead = await _context.LeadLakes
                            .FirstOrDefaultAsync(x => x.Email == row.Email.Trim());

                        if (lead == null)
                        {
                            // Create new LeadLake record
                            lead = new ProcessZero.Domain.Entities.LeadLake
                            {
                                Email = row.Email.Trim(),
                                FirstName = row.FirstName ?? "",
                                LastName = row.LastName ?? "",
                                Company = row.Company ?? "",
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };
                            _context.LeadLakes.Add(lead);
                            await _context.SaveChangesAsync();
                        }

                        if (!leadIds.Contains(lead.Id))
                            leadIds.Add(lead.Id);

                        _statusService.UpdateProgress(jobId, i + 1, totalRows);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Row {i + 1}: {ex.Message}");
                    }
                }

                // Add all leads to campaign in batch
                if (leadIds.Any())
                {
                    var batchRequest = new ProcessZero.Application.Interfaces.BatchLeadModificationRequest(
                        Add: leadIds,
                        Remove: null,
                        Edit: null
                    );

                    var dto = ConvertBatchRequest(batchRequest);
                    await _leadService.ProcessBatchAsync(campaignId, dto);
                }

                _statusService.Complete(jobId);
            }
            catch (Exception ex)
            {
                _statusService.Fail(jobId, ex.Message);
                throw;
            }
            finally
            {
                // Clean up temp file
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);
                }
                catch { }
            }
        }

        private ProcessZero.Application.Interfaces.BatchLeadModificationDto ConvertBatchRequest(BatchLeadModificationRequest req)
        {
            var editList = req.Edit == null ? null : req.Edit.Select(e =>
                new ProcessZero.Application.Interfaces.RelayLeadUpdateDto(
                    e.LeadId,
                    e.CurrentSequenceStepId,
                    e.Status,
                    e.Replied,
                    e.Unsubscribed,
                    e.Completed
                )).ToList();

            return new ProcessZero.Application.Interfaces.BatchLeadModificationDto(req.Add, req.Remove, editList);
        }
    }

    // CsvHelper class map for parsing CSV rows
    public sealed class CsvLeadRowMap : CsvHelper.Configuration.ClassMap<CsvLeadRow>
    {
        public CsvLeadRowMap()
        {
            Map(m => m.Email).Name("Email");
            Map(m => m.FirstName).Name("FirstName").Optional();
            Map(m => m.LastName).Name("LastName").Optional();
            Map(m => m.Company).Name("Company").Optional();
        }
    }
}
