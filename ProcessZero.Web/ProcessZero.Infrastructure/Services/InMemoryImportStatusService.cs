using ProcessZero.Application.Interfaces;
using System.Collections.Concurrent;

namespace ProcessZero.Infrastructure.Services
{
    public class InMemoryImportStatusService : IImportStatusService
    {
        private readonly ConcurrentDictionary<string, ImportStatusDto> _store = new();

        public void Create(string jobId, int campaignId)
        {
            var dto = new ImportStatusDto(jobId, campaignId, 0, 0, false, null);
            _store[jobId] = dto;
        }

        public ImportStatusDto? Get(string jobId)
        {
            _store.TryGetValue(jobId, out var dto);
            return dto;
        }

        public void UpdateProgress(string jobId, int processedRows, int totalRows)
        {
            if (_store.TryGetValue(jobId, out var dto))
            {
                _store[jobId] = dto with { TotalRows = totalRows, ProcessedRows = processedRows };
            }
        }

        public void Complete(string jobId)
        {
            if (_store.TryGetValue(jobId, out var dto))
            {
                _store[jobId] = dto with { Completed = true };
            }
        }

        public void Fail(string jobId, string message)
        {
            if (_store.TryGetValue(jobId, out var dto))
            {
                _store[jobId] = dto with { Completed = true, ErrorMessage = message };
            }
        }
    }
}
