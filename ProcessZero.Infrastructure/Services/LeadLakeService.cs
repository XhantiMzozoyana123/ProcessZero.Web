using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProcessZero.Infrastructure.Services
{
    public class LeadLakeService : ILeadLakeService
    {
        private readonly ApplicationDbContext _context;

        public LeadLakeService(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all LeadLake entries from the database.
        /// </summary>
        /// <returns>List of LeadLake entities.</returns>
        public async Task<List<LeadLake>> GetLeadLakesAsync()
        {
            return await _context.LeadLakes
                .AsNoTracking()
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Retrieves a specific LeadLake by its ID.
        /// </summary>
        /// <param name="id">LeadLake ID.</param>
        /// <returns>LeadLake entity if found; otherwise null.</returns>
        public async Task<LeadLake> GetLeadLakeByIdAsync(int id)
        {
            return await _context.LeadLakes
                .AsNoTracking()
                .FirstAsync(l => l.Id == id);
        }

        /// <summary>
        /// Adds multiple LeadLake entries in a single batch.
        /// </summary>
        /// <param name="leadLakes">List of LeadLake items to add.</param>
        public async Task AddBatchLeadLakesAsync(List<LeadLake> leadLakes)
        {
            if (leadLakes == null || leadLakes.Count == 0)
                return;

            var entities = new List<LeadLake>();

            foreach (var ll in leadLakes)
            {
                if (ll == null)
                    continue;

                // Normalize/ensure required fields
                var entry = new LeadLake
                {
                    UserId = ll.UserId ?? string.Empty,
                    FirstName = ll.FirstName ?? string.Empty,
                    LastName = ll.LastName ?? string.Empty,
                    Email = ll.Email ?? string.Empty,
                    Phone = ll.Phone ?? string.Empty,
                    Company = ll.Company ?? string.Empty,
                    Job = ll.Job ?? string.Empty,
                    Location = ll.Location ?? string.Empty,
                    Industry = ll.Industry,
                    Intent = ll.Intent,
                };

                entities.Add(entry);
            }

            if (entities.Count == 0)
                return;

            await _context.LeadLakes.AddRangeAsync(entities);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Adds a single LeadLake entry. Admin operation.
        /// </summary>
        /// <param name="leadLake">The LeadLake entity to add.</param>
        public async Task AddLeadLakeAsync(LeadLake leadLake)
        {
            if (leadLake == null)
                throw new ArgumentNullException(nameof(leadLake));

            leadLake.CreatedAt = DateTime.UtcNow;
            leadLake.UpdatedAt = DateTime.UtcNow;

            await _context.LeadLakes.AddAsync(leadLake);
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Updates an existing LeadLake entry. Admin operation.
        /// </summary>
        /// <param name="leadLake">The LeadLake entity with updated values.</param>
        public async Task UpdateLeadLakeAsync(LeadLake leadLake)
        {
            if (leadLake == null)
                throw new ArgumentNullException(nameof(leadLake));

            var existing = await _context.LeadLakes.FindAsync(leadLake.Id);
            if (existing == null)
                throw new InvalidOperationException($"LeadLake with id {leadLake.Id} not found");

            // Update scalar properties while preserving tracking identity and timestamps
            existing.FirstName = leadLake.FirstName ?? string.Empty;
            existing.LastName = leadLake.LastName ?? string.Empty;
            existing.Email = leadLake.Email ?? string.Empty;
            existing.Phone = leadLake.Phone ?? string.Empty;
            existing.Company = leadLake.Company ?? string.Empty;
            existing.Job = leadLake.Job ?? string.Empty;
            existing.Location = leadLake.Location ?? string.Empty;
            existing.Industry = leadLake.Industry;
            existing.Intent = leadLake.Intent;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Deletes a LeadLake entry by its ID. Admin operation.
        /// </summary>
        /// <param name="id">LeadLake ID to delete.</param>
        public async Task DeleteLeadLakeAsync(int id)
        {
            var existing = await _context.LeadLakes.FindAsync(id);
            if (existing == null)
                return;

            _context.LeadLakes.Remove(existing);
            await _context.SaveChangesAsync();
        }
    }
}


