using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    public interface ILLMService
    {
        Task<string> GenerateTextAsync(string prompt);
    }
}
