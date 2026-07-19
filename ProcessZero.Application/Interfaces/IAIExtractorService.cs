using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IAIExtractorService
    {
        Task<string> GetFirstNameAsync(string description, string subtitles);

        Task<string> GetLastNameAsync(string description, string subtitles);

        Task<string> GetCompanyAsync(string description, string subtitles);

        Task<string> GetJobTitleAsync(string description, string subtitles);

        Task<string> GetLocationAsync(string description, string subtitles);

        Task<LeadLakeIndustry> GetIndustryAsync(string description, string subtitles);

        Task<LeadIntent> GetIntentAsync(string description, string subtitles);
    }
}
