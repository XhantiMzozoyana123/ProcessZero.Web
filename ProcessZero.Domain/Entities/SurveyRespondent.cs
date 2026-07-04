using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    /// <summary>
    /// Stores respondent information for market research survey participants.
    /// </summary>
    public class SurveyRespondent : BaseEntity
    {
        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;

        public string LastName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string Company { get; set; } = string.Empty;

        public string Job { get; set; } = string.Empty;

        public string Industry { get; set; } = string.Empty;
    }
}
