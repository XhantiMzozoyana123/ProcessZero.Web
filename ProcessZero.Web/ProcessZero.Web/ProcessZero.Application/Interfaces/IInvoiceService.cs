using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IInvoiceService
    {

        Task CreateInvoiceAsync(Invoice invoice);
        Task<List<Invoice>> GetAllInvoicesAsync();

        // Paginated version
        Task<List<Invoice>> GetAllInvoicesAsync(int page, int pageSize);

        Task<List<Invoice>> GetAllInvoicesByUserIdAsync(string userId);

        Task<Invoice> GetInvoiceByIdAsync(int id);

        Task EditInvoiceAsync(Invoice invoice);

        Task DeleteInvoiceAsync(int id);
    }
}
