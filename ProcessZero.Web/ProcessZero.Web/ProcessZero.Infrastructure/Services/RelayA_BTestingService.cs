using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class RelayA_BTestingService : IRelayA_BTestingService
    {
        private readonly ApplicationDbContext _context;

        public RelayA_BTestingService(ApplicationDbContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        // SELECT VARIANT (A/B DECISION ENGINE)
        // ─────────────────────────────────────────────
        public async Task<RelayEmailVariant> SelectVariantAsync(int sequenceStepId)
        {
            var variants = await _context.Set<RelayEmailVariant>()
                .Where(v => v.SequenceStepId == sequenceStepId)
                .ToListAsync();

            if (variants == null || variants.Count == 0)
                throw new Exception("No variants found for this sequence step");

            // If only one variant → return it
            if (variants.Count == 1)
                return variants.First();

            // Weighted random selection (based on Weight field)
            var totalWeight = variants.Sum(v => v.Weight);

            var random = new Random();
            var roll = random.Next(1, totalWeight + 1);

            var cumulative = 0;

            foreach (var variant in variants)
            {
                cumulative += variant.Weight;

                if (roll <= cumulative)
                    return variant;
            }

            // fallback safety
            return variants.First();
        }

        // ─────────────────────────────────────────────
        // RECORD RESULT (FEEDBACK LOOP)
        // ─────────────────────────────────────────────
        public async Task RecordVariantResultAsync(int variantId, bool wasSuccessful)
        {
            var variant = await _context.Set<RelayEmailVariant>()
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null)
                throw new Exception("Variant not found");

            // For MVP: simple learning system
            // You can later replace this with full analytics tables

            if (wasSuccessful)
            {
                variant.Weight += 1; // reward good performer
            }
            else
            {
                variant.Weight = Math.Max(1, variant.Weight - 1); // penalize weak performer
            }

            _context.Update(variant);
            await _context.SaveChangesAsync();
        }

        // ─────────────────────────────────────────────
        // GET BEST PERFORMING VARIANT
        // ─────────────────────────────────────────────
        public async Task<RelayEmailVariant> GetBestPerformingVariantAsync(int stepId)
        {
            var variants = await _context.Set<RelayEmailVariant>()
                .Where(v => v.SequenceStepId == stepId)
                .ToListAsync();

            if (variants == null || variants.Count == 0)
                throw new Exception("No variants found");

            // Highest weight = best performer (simple heuristic MVP)
            return variants
                .OrderByDescending(v => v.Weight)
                .First();
        }
    }
}
