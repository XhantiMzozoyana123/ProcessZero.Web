using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Application.Interfaces
{
    public interface IProductService
    {
        Task AddProductAsync(Product product);

        Task<Product> GetProductByIdAsync(int id);

        Task<List<Product>> GetAllProductAsync();

        Task UpdateProductAsync(Product product);

        Task DeleteProductAsync(int id);
    }
}
