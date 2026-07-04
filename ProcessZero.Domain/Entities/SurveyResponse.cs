using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores a submitted market research survey response.
    /// Responses from all respondents feed into AI for product creation.
    /// </summary>
    public class SurveyResponse : BaseEntity
    {
        public int SurveyRespondentId { get; set; }

        public SurveyRespondent? Respondent { get; set; }

        public string? AnswersJson { get; set; }

        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    }
}
