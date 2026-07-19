using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProcessZero.Domain.Entities;

namespace ProcessZero.Application.Interfaces
{
    public interface IWebinarService
    {
        Task<IEnumerable<Webinar>> GetAllAsync();

        Task<Webinar?> GetByIdAsync(int id);

        Task CreateAsync(Webinar webinar);

        Task UpdateAsync(Webinar webinar);

        Task DeleteAsync(int id);
    }
}