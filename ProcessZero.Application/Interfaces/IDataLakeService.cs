using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface IDataLakeService
    {
        Task<IDictionary<string, IEnumerable<object>>> ExtractAllTablesAsync();
    }
}
