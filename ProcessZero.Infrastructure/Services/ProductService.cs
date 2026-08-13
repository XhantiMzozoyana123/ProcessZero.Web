using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Constants;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class ProductService : IProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<ProductService> _logger;

        public ProductService(ApplicationDbContext context, IEmailService emailService, ILogger<ProductService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task AddProductAsync(Product product)
        {
            if (product == null) throw new ArgumentNullException(nameof(product));

            // Save the product to the database FIRST so the core operation
            // always succeeds regardless of email notification outcome.
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();

            // Notify users (sales reps) about the new product - fire-and-forget.
            // Email failures must never roll back the successful DB write.
            await NotifyProductLaunchAsync(product);
        }

        public async Task DeleteProductAsync(int id)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return;

            // Delete the product from the database FIRST so the core operation
            // always succeeds regardless of email notification outcome.
            _context.Products.Remove(existing);
            await _context.SaveChangesAsync();

            // Notify users (sales reps) about the product discontinuation - fire-and-forget.
            // Email failures must never roll back the successful DB delete.
            await NotifyProductDiscontinuedAsync(existing);
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

            // Notify users (sales reps) about the product update - fire-and-forget.
            // Email failures must never roll back the successful DB update.
            await NotifyProductUpdatedAsync(existing);
        }

        /// <summary>
        /// Sends product-launch email notifications to all users.
        /// Failures are logged and swallowed so they never break the
        /// caller's transaction (create / update / delete).
        /// </summary>
        private async Task NotifyProductLaunchAsync(Product product)
        {
            await NotifyUsersAsync(
                product,
                (name, email, p) => NoticeConstant.NotifyProductCreated(name, email, p));
        }

        private async Task NotifyProductDiscontinuedAsync(Product product)
        {
            await NotifyUsersAsync(
                product,
                (name, email, p) => NoticeConstant.NotifyProductDeleted(name, email, p));
        }

        private async Task NotifyProductUpdatedAsync(Product product)
        {
            await NotifyUsersAsync(
                product,
                (name, email, p) => NoticeConstant.NotifyProductUpdated(name, email, p));
        }

        /// <summary>
        /// Iterates over every user in the database and sends an email notification
        /// built by the supplied factory. Any exceptions - whether from
        /// missing SMTP configuration, null emails, or SMTP delivery failures -
        /// are caught and logged so that a broken email setup can never cause a
        /// 500 error on the originating CRUD operation.
        /// </summary>
        private async Task NotifyUsersAsync(
            Product product,
            Func<string, string, Product, EmailDto> buildNotice)
        {
            try
            {
                var users = await _context.Users.ToListAsync();

                foreach (var user in users)
                {
                    // Skip users that don't have an email address - sending
                    // would throw an ArgumentException inside EmailService.
                    if (string.IsNullOrWhiteSpace(user.Email))
                    {
                        _logger.LogWarning(
                            "Skipping product notification for user {UserId} ({UserName}) because their email is null or empty.",
                            user.Id, user.UserName);
                        continue;
                    }

                    try
                    {
                        var notice = buildNotice(
                            user.UserName ?? string.Empty,
                            user.Email,
                            product);

                        await _emailService.SendEmailAsync(notice);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to send product notification email to user {UserId} ({Email}). " +
                            "The product CRUD operation has already succeeded; this is non-fatal.",
                            user.Id, user.Email);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to retrieve users for product notification (product id: {ProductId}). " +
                    "The product CRUD operation has already succeeded; this is non-fatal.",
                    product.Id);
            }
        }
    }
}
