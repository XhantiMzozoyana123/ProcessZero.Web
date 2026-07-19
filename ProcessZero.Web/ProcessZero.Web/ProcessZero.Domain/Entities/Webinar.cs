using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Domain.Entities
{
    public class Webinar : BaseEntity
    {

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string YoutubeUrl { get; set; } = string.Empty;

        public string ThumbnailBase64 { get; set; } = string.Empty;
    }
}
