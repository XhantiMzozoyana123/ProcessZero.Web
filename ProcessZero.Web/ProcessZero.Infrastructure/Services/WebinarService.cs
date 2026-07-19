using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class WebinarService : IWebinarService
    {
        private readonly ApplicationDbContext _context;

        public WebinarService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Webinar>> GetAllAsync()
        {
            return await _context.Webinars
                                 .OrderByDescending(w => w.CreatedAt)
                                 .ToListAsync();
        }

        public async Task<Webinar?> GetByIdAsync(int id)
        {
            return await _context.Webinars.FindAsync(id);
        }

        public async Task CreateAsync(Webinar webinar)
        {
            if (webinar == null) throw new ArgumentNullException(nameof(webinar));

            webinar.CreatedAt = DateTime.UtcNow;
            webinar.UpdatedAt = DateTime.UtcNow;

            await _context.Webinars.AddAsync(webinar);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Webinar webinar)
        {
            if (webinar == null) throw new ArgumentNullException(nameof(webinar));

            var existing = await _context.Webinars.FindAsync(webinar.Id);
            if (existing == null) throw new InvalidOperationException($"Webinar with id {webinar.Id} not found");

            _context.Entry(existing).CurrentValues.SetValues(webinar);
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var webinar = await _context.Webinars.FindAsync(id);

            if (webinar != null)
            {
                _context.Webinars.Remove(webinar);
                await _context.SaveChangesAsync();
            }
        }
    }
}