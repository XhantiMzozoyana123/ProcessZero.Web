using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class MarketResearch : BaseEntity
    {
        public int SurveyId { get; set; }
        public string GeneratedReport { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
    }
}
