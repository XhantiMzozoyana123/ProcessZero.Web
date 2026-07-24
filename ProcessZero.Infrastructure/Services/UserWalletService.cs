using Microsoft.EntityFrameworkCore;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProcessZero.Infrastructure.Services
{
    public class UserWalletService : IUserWalletService
    {
        private readonly ApplicationDbContext _context;

        public UserWalletService(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<UserWalletDto> GetUserWalletAsync(string userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId));

            var wallet = await _context.UserWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            if (wallet == null)
            {
                wallet = await CreateWalletAsync(userId, cancellationToken);
            }

            return new UserWalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                CreditBalance = wallet.CreditBalance,
                TotalCreditsPurchased = wallet.TotalCreditsPurchased,
                TotalCreditsConsumed = wallet.TotalCreditsConsumed,
                LastUpdatedAt = wallet.LastUpdatedAt,
                SubscriptionId = wallet.SubscriptionId,
                SubscriptionStatus = wallet.SubscriptionStatus
            };
        }

        public async Task<List<CreditPackageDto>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default)
        {
            var packages = await _context.CreditPackages
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.SortOrder)
                .Select(p => new CreditPackageDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CreditAmount = p.CreditAmount,
                    Price = p.Price,
                    Currency = p.Currency,
                    DurationMinutes = p.DurationMinutes,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder,
                    DiscountPercentage = p.DiscountPercentage,
                    IsSubscription = p.IsSubscription,
                    DiscountedPrice = p.DiscountPercentage.HasValue 
                        ? p.Price * (1 - p.DiscountPercentage.Value / 100) 
                        : null
                })
                .ToListAsync(cancellationToken);

            return packages;
        }

        public async Task<List<CreditPackageDto>> GetAllPackagesAsync(CancellationToken cancellationToken = default)
        {
            var packages = await _context.CreditPackages
                .AsNoTracking()
                .OrderBy(p => p.SortOrder)
                .Select(p => new CreditPackageDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Description = p.Description,
                    CreditAmount = p.CreditAmount,
                    Price = p.Price,
                    Currency = p.Currency,
                    DurationMinutes = p.DurationMinutes,
                    IsActive = p.IsActive,
                    SortOrder = p.SortOrder,
                    DiscountPercentage = p.DiscountPercentage,
                    IsSubscription = p.IsSubscription,
                    DiscountedPrice = p.DiscountPercentage.HasValue 
                        ? p.Price * (1 - p.DiscountPercentage.Value / 100) 
                        : null
                })
                .ToListAsync(cancellationToken);

            return packages;
        }

        public async Task<CreditPackageDto> CreatePackageAsync(CreateCreditPackageDto packageDto, CancellationToken cancellationToken = default)
        {
            var package = new CreditPackage
            {
                Name = packageDto.Name,
                Description = packageDto.Description,
                CreditAmount = packageDto.CreditAmount,
                Price = packageDto.Price,
                Currency = packageDto.Currency,
                DurationMinutes = packageDto.DurationMinutes,
                IsActive = packageDto.IsActive,
                SortOrder = packageDto.SortOrder,
                DiscountPercentage = packageDto.DiscountPercentage,
                IsSubscription = packageDto.IsSubscription,
                CreatedAt = DateTime.UtcNow
            };

            _context.CreditPackages.Add(package);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreditPackageDto
            {
                Id = package.Id,
                Name = package.Name,
                Description = package.Description,
                CreditAmount = package.CreditAmount,
                Price = package.Price,
                Currency = package.Currency,
                DurationMinutes = package.DurationMinutes,
                IsActive = package.IsActive,
                SortOrder = package.SortOrder,
                DiscountPercentage = package.DiscountPercentage,
                IsSubscription = package.IsSubscription,
                DiscountedPrice = package.DiscountPercentage.HasValue 
                    ? package.Price * (1 - package.DiscountPercentage.Value / 100) 
                    : null
            };
        }

        public async Task<CreditPurchaseResponseDto> PurchaseCreditsAsync(string userId, PurchaseCreditsRequestDto request, CancellationToken cancellationToken = default)
        {
            // Validate and get package
            var package = await _context.CreditPackages
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == request.CreditPackageId && p.IsActive, cancellationToken);

            if (package == null)
            {
                return new CreditPurchaseResponseDto
                {
                    Success = false,
                    Message = "Credit package not found or not active"
                };
            }

            // Get or create wallet
            var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);

            // Create transaction
            var transaction = new CreditTransaction
            {
                UserWalletId = wallet.Id,
                TransactionType = CreditTransactionType.Purchase,
                CreditAmount = package.CreditAmount,
                BalanceAfterTransaction = wallet.CreditBalance + package.CreditAmount,
                Description = $"Purchase of {package.Name}",
                ReferenceId = request.PaymentReference,
                RelatedEntityType = "CreditPackage",
                RelatedEntityId = package.Id,
                TransactionDate = DateTime.UtcNow
            };

            // Update wallet
            wallet.CreditBalance += package.CreditAmount;
            wallet.TotalCreditsPurchased += package.CreditAmount;
            wallet.LastUpdatedAt = DateTime.UtcNow;

            _context.CreditTransactions.Add(transaction);
            _context.UserWallets.Update(wallet);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreditPurchaseResponseDto
            {
                Success = true,
                Message = $"Successfully purchased {package.CreditAmount} credits",
                NewBalance = wallet.CreditBalance,
                TransactionId = transaction.Id
            };
        }

        public async Task<CreditConsumeResponseDto> ConsumeCreditsAsync(string userId, ConsumeCreditsRequestDto request, CancellationToken cancellationToken = default)
        {
            var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);

            // Check if user has sufficient credits
            if (wallet.CreditBalance < request.CreditAmount)
            {
                return new CreditConsumeResponseDto
                {
                    Success = false,
                    Message = "Insufficient credits for this operation",
                    NewBalance = wallet.CreditBalance,
                    CreditsConsumed = 0
                };
            }

            // Create transaction
            var transaction = new CreditTransaction
            {
                UserWalletId = wallet.Id,
                TransactionType = CreditTransactionType.Consumption,
                CreditAmount = -request.CreditAmount, // Negative for consumption
                BalanceAfterTransaction = wallet.CreditBalance - request.CreditAmount,
                Description = request.Description,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                TransactionDate = DateTime.UtcNow
            };

            // Update wallet
            wallet.CreditBalance -= request.CreditAmount;
            wallet.TotalCreditsConsumed += request.CreditAmount;
            wallet.LastUpdatedAt = DateTime.UtcNow;

            _context.CreditTransactions.Add(transaction);
            _context.UserWallets.Update(wallet);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreditConsumeResponseDto
            {
                Success = true,
                Message = $"Successfully consumed {request.CreditAmount} credits",
                NewBalance = wallet.CreditBalance,
                CreditsConsumed = request.CreditAmount
            };
        }

        public async Task<CreditBalanceResponseDto> CheckCreditBalanceAsync(string userId, decimal requiredCredits, CancellationToken cancellationToken = default)
        {
            var wallet = await _context.UserWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            var currentBalance = wallet?.CreditBalance ?? 0;
            var hasSufficient = currentBalance >= requiredCredits;

            return new CreditBalanceResponseDto
            {
                CreditBalance = currentBalance,
                HasSufficientCredits = hasSufficient,
                Message = hasSufficient 
                    ? "Sufficient credits available" 
                    : $"Insufficient credits. Required: {requiredCredits}, Available: {currentBalance}"
            };
        }

        public async Task<List<CreditTransactionDto>> GetTransactionHistoryAsync(string userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
        {
            var wallet = await _context.UserWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            if (wallet == null)
            {
                return new List<CreditTransactionDto>();
            }

            var transactions = await _context.CreditTransactions
                .AsNoTracking()
                .Where(t => t.UserWalletId == wallet.Id)
                .OrderByDescending(t => t.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new CreditTransactionDto
                {
                    Id = t.Id,
                    UserWalletId = t.UserWalletId,
                    TransactionType = t.TransactionType.ToString(),
                    CreditAmount = t.CreditAmount,
                    BalanceAfterTransaction = t.BalanceAfterTransaction,
                    Description = t.Description,
                    ReferenceId = t.ReferenceId,
                    RelatedEntityType = t.RelatedEntityType,
                    RelatedEntityId = t.RelatedEntityId,
                    TransactionDate = t.TransactionDate
                })
                .ToListAsync(cancellationToken);

            return transactions;
        }

public async Task<CreditTransactionDto?> GetTransactionByIdAsync(string userId, int transactionId, CancellationToken cancellationToken = default)
        {
            var transaction = await _context.CreditTransactions
                .AsNoTracking()
                .Where(t => t.Id == transactionId)
                .Join(_context.UserWallets,
                    t => t.UserWalletId,
                    w => w.Id,
                    (t, w) => new { Transaction = t, Wallet = w })
                .Where(x => x.Wallet.UserId == userId)
                .Select(x => new CreditTransactionDto
                {
                    Id = x.Transaction.Id,
                    UserWalletId = x.Transaction.UserWalletId,
                    TransactionType = x.Transaction.TransactionType.ToString(),
                    CreditAmount = x.Transaction.CreditAmount,
                    BalanceAfterTransaction = x.Transaction.BalanceAfterTransaction,
                    Description = x.Transaction.Description,
                    ReferenceId = x.Transaction.ReferenceId,
                    RelatedEntityType = x.Transaction.RelatedEntityType,
                    RelatedEntityId = x.Transaction.RelatedEntityId,
                    TransactionDate = x.Transaction.TransactionDate
                })
                .FirstOrDefaultAsync(cancellationToken);

            return transaction;
        }

        public async Task<CreditConsumeResponseDto> AdjustCreditsAsync(string userId, decimal creditAmount, string reason, CancellationToken cancellationToken = default)
        {
            var wallet = await GetOrCreateWalletAsync(userId, cancellationToken);

            // Create transaction
            var transaction = new CreditTransaction
            {
                UserWalletId = wallet.Id,
                TransactionType = CreditTransactionType.Adjustment,
                CreditAmount = creditAmount,
                BalanceAfterTransaction = wallet.CreditBalance + creditAmount,
                Description = reason,
                TransactionDate = DateTime.UtcNow
            };

            // Update wallet
            wallet.CreditBalance += creditAmount;
            if (creditAmount > 0)
            {
                wallet.TotalCreditsPurchased += creditAmount;
            }
            else
            {
                wallet.TotalCreditsConsumed += Math.Abs(creditAmount);
            }
            wallet.LastUpdatedAt = DateTime.UtcNow;

            _context.CreditTransactions.Add(transaction);
            _context.UserWallets.Update(wallet);
            await _context.SaveChangesAsync(cancellationToken);

            return new CreditConsumeResponseDto
            {
                Success = true,
                Message = $"Successfully adjusted credits by {creditAmount}",
                NewBalance = wallet.CreditBalance,
                CreditsConsumed = Math.Abs(creditAmount)
            };
        }

        public async Task InitializeUserWalletAsync(string userId, CancellationToken cancellationToken = default)
        {
            await GetOrCreateWalletAsync(userId, cancellationToken);
        }

        /// <summary>
        /// Gets the remaining platform usage hours based on the user's credit balance.
        /// Consumption rate: 0.2 credits per hour (1 credit = 5 hours).
        /// </summary>
        public async Task<decimal> GetRemainingHoursAsync(string userId, CancellationToken cancellationToken = default)
        {
            var wallet = await _context.UserWallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            var creditBalance = wallet?.CreditBalance ?? 0;
            // 0.2 credits per hour = 1 credit per 5 hours
            return creditBalance / 0.2m;
        }

        /// <summary>
        /// Consumes credits for active platform usage.
        /// Consumption rate: 0.2 credits per hour.
        /// </summary>
        public async Task<CreditConsumeResponseDto> ConsumeActiveUsageAsync(string userId, int minutes = 10, CancellationToken cancellationToken = default)
        {
            // 0.2 credits per hour = 0.2/60 credits per minute
            var creditsToConsume = (decimal)minutes * 0.2m / 60m;

            return await ConsumeCreditsAsync(userId, new ConsumeCreditsRequestDto
            {
                CreditAmount = creditsToConsume,
                Description = $"Active usage for {minutes} minutes",
                RelatedEntityType = "ActiveUsage"
            }, cancellationToken);
        }

        private async Task<UserWallet> GetOrCreateWalletAsync(string userId, CancellationToken cancellationToken)
        {
            var wallet = await _context.UserWallets
                .FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

            if (wallet == null)
            {
                wallet = await CreateWalletAsync(userId, cancellationToken);
            }

            return wallet;
        }

        private async Task<UserWallet> CreateWalletAsync(string userId, CancellationToken cancellationToken)
        {
            var wallet = new UserWallet
            {
                UserId = userId,
                CreditBalance = 0,
                TotalCreditsPurchased = 0,
                TotalCreditsConsumed = 0,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            _context.UserWallets.Add(wallet);
            await _context.SaveChangesAsync(cancellationToken);

            return wallet;
        }
    }
}