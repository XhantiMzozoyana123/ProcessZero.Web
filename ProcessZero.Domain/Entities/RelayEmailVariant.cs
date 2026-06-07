using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelayEmailVariant : BaseEntity
    {
        public int SequenceStepId { get; set; }

        public RelaySequenceStep SequenceStep { get; set; }

        public string VariantName { get; set; } = "A";

        public string Subject { get; set; } = string.Empty;

        public string HtmlBody { get; set; } = string.Empty;

        public int Weight { get; set; } = 50;

        public ICollection<RelayEmailActivity> Activities { get; set; }
            = new List<RelayEmailActivity>();
    }
}
