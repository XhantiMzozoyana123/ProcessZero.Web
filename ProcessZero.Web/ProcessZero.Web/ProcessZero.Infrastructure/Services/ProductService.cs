using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public ProductService(ApplicationDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task AddProductAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            await NotifyProductLaunchAsync(product);

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteProductAsync(int id)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return;

            await NotifyProductDiscontinuedAsync(existing);

            _context.Products.Remove(existing);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Product>> GetAllProductAsync()
        {
            return await _context.Products
                                 .OrderByDescending(p => p.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _context.Products.FindAsync(id);
        }

        public async Task UpdateProductAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            var existing = await _context.Products.FindAsync(product.Id);
            if (existing == null) throw new InvalidOperationException($"Product with id {product.Id} not found");

            // Update scalar properties while preserving tracking identity
            _context.Entry(existing).CurrentValues.SetValues(product);
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Notify users (sales reps) about the product update
            await NotifyProductUpdatedAsync(existing);
        }

        private async Task NotifyProductLaunchAsync(Product product)
        {
            var users = await _context.Users.ToListAsync();
            if (users == null) return;

            foreach (var user in users)
            {
                var notice = NoticeConstant.NotifyProductCreated(
                    user.UserName,
                    user.Email,
                    product
                    );

                await _emailService.SendEmailAsync(notice);
            }
        }

        private async Task NotifyProductDiscontinuedAsync(Product product)
        {
            var users = await _context.Users.ToListAsync();
            if (users == null) return;

            foreach (var user in users)
            {
                var notice = NoticeConstant.NotifyProductDeleted(
                    user.UserName,
                    user.Email,
                    product
                    );

                await _emailService.SendEmailAsync(notice);
            }
        }

        private async Task NotifyProductUpdatedAsync(Product product)
        {
            var users = await _context.Users.ToListAsync();
            if (users == null) return;

            foreach (var user in users)
            {
                var notice = NoticeConstant.NotifyProductUpdated(
                    user.UserName,
                    user.Email,
                    product
                    );

                await _emailService.SendEmailAsync(notice);
            }
        }
    }
}
