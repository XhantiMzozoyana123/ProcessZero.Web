using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Dtos
{
    public class SearchDto
    {
        public string Keywords { get; set; } = string.Empty;

        public int PageViewLimit { get; set; }

        public string ContainerUrl { get; set; } = string.Empty;
    }
}
