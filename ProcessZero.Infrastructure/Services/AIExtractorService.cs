using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class AIExtractorService : IAIExtractorService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILLMService _llmService;

        public AIExtractorService(ApplicationDbContext context, ILLMService llmService)
        {
            _context = context;
            _llmService = llmService;
        }

        public async Task<string> GetCompanyAsync(string description, string subtitles)
        {
            try
            {
                var prompt = BuildNamePrompt(description + subtitles, "company");

                var result = await _llmService.GenerateTextAsync(prompt);

                return CleanResult(result);
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public async Task<string> GetFirstNameAsync(string description, string subtitles)
        {
            try
            {
                var prompt = BuildNamePrompt(description + subtitles, "first name");

                var result = await _llmService.GenerateTextAsync(prompt);

                return CleanResult(result);
            }
            catch (Exception ex) 
            {
                return string.Empty;
            }
        }

        public async Task<string> GetJobTitleAsync(string description, string subtitles)
        {
            try
            {
                var prompt = BuildNamePrompt(description + subtitles, "job title");

                var result = await _llmService.GenerateTextAsync(prompt);

                return CleanResult(result);
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public async Task<string> GetLastNameAsync(string description, string subtitles)
        {
            try
            {
                var prompt = BuildNamePrompt(description + subtitles, "last name");

                var result = await _llmService.GenerateTextAsync(prompt);

                return CleanResult(result);
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        public async Task<string> GetLocationAsync(string description, string subtitles)
        {
            try
            {
                var prompt = BuildNamePrompt(description + subtitles, "location");

                var result = await _llmService.GenerateTextAsync(prompt);

                return CleanResult(result);
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        // INDUSTRY (FIXED ENUM ISSUE)
        // -------------------------
        public async Task<LeadLakeIndustry> GetIndustryAsync(string description, string subtitles)
        {
            var prompt = $@"
                You are a classification system.

                Classify the industry into ONE of these exact values:
                Technology, Finance, Healthcare, Education, Retail, Manufacturing, Energy, Transportation, Entertainment, Hospitality, Other

                Rules:
                - Return ONLY one word
                - No explanation
                - No punctuation

                Text:
                {description} {subtitles}
                ";

            var result = await _llmService.GenerateTextAsync(prompt);

            var clean = CleanResult(result);

            return ParseIndustry(clean);
        }

        public async Task<LeadIntent> GetIntentAsync(string description, string subtitles)
        {
            var prompt = BuildIntentPrompt(description + " " + subtitles);

            var result = await _llmService.GenerateTextAsync(prompt);

            var clean = CleanResult(result);

            return ParseIntent(clean);
        }


        private LeadLakeIndustry ParseIndustry(string value)
        {
            return Enum.TryParse<LeadLakeIndustry>(value, true, out var parsed)
                ? parsed
                : LeadLakeIndustry.Other;
        }

        private string BuildIntentPrompt(string text)
        {
            return $@"
                You are a lead intent classification system.

                Classify the intent of this lead as ONE of the following:

                High
                Medium
                Low

                Rules:
                - Return ONLY one word
                - No explanation
                - No punctuation
                - No extra text

                Guidelines:
                - High = urgent buying intent, decision maker, ready to act
                - Medium = interested but not ready
                - Low = casual / no clear intent

                Text:
                {text}
                ";
        }

        private LeadIntent ParseIntent(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return LeadIntent.Low;

            var cleaned = value.Trim();

            return Enum.TryParse<LeadIntent>(cleaned, true, out var parsed)
                ? parsed
                : LeadIntent.Low;
        }

        private string BuildNamePrompt(string text, string target)
        {
            return $@"
                You are a precise information extraction system.

                Extract ONLY the person's {target} from the text below.

                Rules:
                - Return only the {target}
                - No explanations
                - No punctuation
                - If not found return: UNKNOWN

                Text:
                {text}
                ";
        }

        private string CleanResult(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "UNKNOWN";

            return input.Trim()
                        .Replace("\n", "")
                        .Replace("\r", "");
        }
    }
}