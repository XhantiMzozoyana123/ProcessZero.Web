using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class RelaySequenceStep : BaseEntity
    {
        public int RelaySequenceId { get; set; }

        public RelaySequence RelaySequence { get; set; }

        public string Name { get; set; } = string.Empty;

        public int StepOrder { get; set; }

        public int DelayDays { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<RelayEmailVariant> Variants { get; set; }
            = new List<RelayEmailVariant>();
    }
}
