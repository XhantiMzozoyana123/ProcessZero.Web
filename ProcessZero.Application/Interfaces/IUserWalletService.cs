using ProcessZero.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProcessZero.Application.Interfaces
{
    /// <summary>
    /// Service interface for managing user wallets and credit transactions in Process Zero 2.0.
    /// </summary>
    public interface IUserWalletService
    {
        /// <summary>
        /// Gets the wallet for a specific user, creating one if it doesn't exist.
        /// </summary>
        Task<UserWalletDto> GetUserWalletAsync(string userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all available credit packages (active ones).
        /// </summary>
        Task<List<CreditPackageDto>> GetAvailablePackagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all credit packages (including inactive) - admin only.
        /// </summary>
        Task<List<CreditPackageDto>> GetAllPackagesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new credit package - admin only.
        /// </summary>
        Task<CreditPackageDto> CreatePackageAsync(CreateCreditPackageDto packageDto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Purchases a credit package for a user.
        /// </summary>
        Task<CreditPurchaseResponseDto> PurchaseCreditsAsync(string userId, PurchaseCreditsRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Consumes credits from a user's wallet.
        /// </summary>
        Task<CreditConsumeResponseDto> ConsumeCreditsAsync(string userId, ConsumeCreditsRequestDto request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks if a user has sufficient credits for an operation.
        /// </summary>
        Task<CreditBalanceResponseDto> CheckCreditBalanceAsync(string userId, decimal requiredCredits, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets transaction history for a user.
        /// </summary>
        Task<List<CreditTransactionDto>> GetTransactionHistoryAsync(string userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a specific transaction by ID.
        /// </summary>
        Task<CreditTransactionDto?> GetTransactionByIdAsync(int transactionId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adjusts credits manually - admin only.
        /// </summary>
        Task<CreditConsumeResponseDto> AdjustCreditsAsync(string userId, decimal creditAmount, string reason, CancellationToken cancellationToken = default);

        /// <summary>
        /// Initializes wallet for a user if it doesn't exist.
        /// </summary>
        Task InitializeUserWalletAsync(string userId, CancellationToken cancellationToken = default);
    }
}