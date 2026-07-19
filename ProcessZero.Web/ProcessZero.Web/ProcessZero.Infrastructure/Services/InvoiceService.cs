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
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;

        public InvoiceService(
            ApplicationDbContext context,
            IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task CreateInvoiceAsync(Invoice invoice)
        {
            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null) return;

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task EditInvoiceAsync(Invoice invoice)
        {
            var existingInvoice = await _context.Invoices.FindAsync(invoice.Id);
            if (existingInvoice == null)
                throw new Exception("Invoice not found");

            if (invoice.IsPaid == true)
                await NotifyInvoiceAsPaidAsync(existingInvoice);

            existingInvoice.Amount = invoice.Amount;
            existingInvoice.ProductId = invoice.ProductId;
            existingInvoice.ClientId = invoice.ClientId;
            existingInvoice.IsPaid = invoice.IsPaid;

            _context.Invoices.Update(existingInvoice);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync(int page, int pageSize)
        {
            var skip = (Math.Max(1, page) - 1) * Math.Max(1, pageSize);
            return await _context.Invoices
                .OrderByDescending(i => i.CreatedAt)
                .Skip(skip)
                .Take(Math.Clamp(pageSize, 1, 500))
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetAllInvoicesByUserIdAsync(string userId)
        {
            return await _context.Invoices
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<Invoice> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        private async Task NotifyInvoiceAsPaidAsync(Invoice invoice)
        {
            var user = await _context.Users.FindAsync(invoice.UserId);
            if (user == null) return;
            var notice = NoticeConstant.NotifySalesRepInvoicePaid(
                user.UserName,
                user.Email,
                invoice,
                DateTime.Now,
                "Your invoice has been marked as paid."
                );

            await _emailService.SendEmailAsync(notice);
        }
    }

}
