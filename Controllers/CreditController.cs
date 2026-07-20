using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    /// <summary>
    /// CreditController manages the credit-based access system for Process Zero 2.0.
    /// Users purchase credits that translate to active platform usage time.
    ///
    /// Entities referenced by this controller:
    ///
    /// UserWallet (ProcessZero.Domain.Entities.UserWallet)
    /// - Id (int) : Primary key from BaseEntity. Unique wallet identifier.
    /// - UserId (string) : Foreign key to ApplicationUser.Id. Owner of the wallet.
    /// - CreditBalance (decimal) : Current available credits for platform access.
    /// - TotalCreditsPurchased (decimal) : Lifetime total credits purchased by user.
    /// - TotalCreditsConsumed (decimal) : Lifetime total credits consumed by user.
    /// - LastUpdatedAt (DateTime?) : Last time the wallet was modified.
    /// - SubscriptionId (string?) : External subscription reference for recurring credits.
    /// - SubscriptionStatus (string?) : Status of subscription (active, cancelled, expired).
    /// - CreatedAt (DateTime) : Timestamp when wallet was created (from BaseEntity).
    /// - UpdatedAt (DateTime?) : Timestamp when wallet was last updated (from BaseEntity).
    ///
    /// CreditTransaction (ProcessZero.Domain.Entities.CreditTransaction)
    /// - Id (int) : Primary key from BaseEntity. Unique transaction identifier.
    /// - UserWalletId (int) : Foreign key to UserWallet.Id. Owner of transaction.
    /// - TransactionType (CreditTransactionType) : Type of transaction (Purchase, Consumption, Refund, Adjustment, Bonus, Subscription).
    /// - CreditAmount (decimal) : Amount of credits moved (positive for adds, negative for deductions).
    /// - BalanceAfterTransaction (decimal) : Wallet balance after this transaction completed.
    /// - Description (string) : Human-readable description of the transaction.
    /// - ReferenceId (string?) : External reference (e.g., payment provider transaction ID).
    /// - RelatedEntityType (string?) : Type of entity that triggered this transaction (e.g., "Meeting", "Product").
    /// - RelatedEntityId (int?) : ID of the related entity.
    /// - TransactionDate (DateTime) : When the transaction occurred. Defaults to UTC now.
    /// - CreatedAt (DateTime) : Timestamp when transaction was created (from BaseEntity).
    /// - UpdatedAt (DateTime?) : Timestamp when transaction was last updated (from BaseEntity).
    ///
    /// CreditPackage (ProcessZero.Domain.Entities.CreditPackage)
    /// - Id (int) : Primary key from BaseEntity. Unique package identifier.
    /// - Name (string) : Package name (e.g., "Starter Pack", "Pro Pack", "Enterprise").
    /// - Description (string) : Description of what the package provides.
    /// - CreditAmount (decimal) : Number of credits included in this package.
    /// - Price (decimal) : Cost in configured currency (default USD).
    /// - Currency (string) : Three-letter currency code (USD, EUR, etc.). Defaults to "USD".
    /// - DurationMinutes (int?) : Time duration in minutes that credits provide.
    /// - IsActive (bool) : Whether the package is available for purchase. Defaults to true.
    /// - SortOrder (int) : Display order for UI sorting. Defaults to 0.
    /// - DiscountPercentage (decimal?) : Optional discount percentage (0-100).
    /// - IsSubscription (bool) : Whether this is a recurring subscription package. Defaults to false.
    /// - CreatedAt (DateTime) : Timestamp when package was created (from BaseEntity).
    /// - UpdatedAt (DateTime?) : Timestamp when package was last updated (from BaseEntity).
    ///
    /// The credit system enables Process Zero 2.0's pay-to-use model where users purchase
    /// credits that translate to active usage time on the platform. This provides access to
    /// sales opportunities, CRM tools, AI features, and training resources.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CreditController : ControllerBase
    {
        private readonly IUserWalletService _walletService;

        public CreditController(IUserWalletService walletService)
        {
            _walletService = walletService;
        }

        /// <summary>
        /// Helper to extract the authenticated user's id from JWT claims.
        /// Returns empty string when not available.
        /// Used throughout the controller to associate operations with the current user.
        /// </summary>
        private string GetUserId() =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// GET: api/credit/wallet
        /// Returns the current user's wallet information including credit balance.
        ///
        /// If the user doesn't have a wallet yet, one will be automatically created
        /// with a zero balance. This allows for lazy initialization of user wallets.
        ///
        /// Response: UserWalletDto
        /// - Id (int) : Wallet identifier
        /// - UserId (string) : Owner's user ID
        /// - CreditBalance (decimal) : Current available credits for use
        /// - TotalCreditsPurchased (decimal) : Lifetime credits purchased (analytics)
        /// - TotalCreditsConsumed (decimal) : Lifetime credits consumed (analytics)
        /// - LastUpdatedAt (DateTime?) : Last wallet modification timestamp
        /// - SubscriptionId (string?) : External subscription reference
        /// - SubscriptionStatus (string?) : Subscription state
        /// </summary>
        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var wallet = await _walletService.GetUserWalletAsync(userId);
            return Ok(wallet);
        }

        /// <summary>
        /// GET: api/credit/packages
        /// Returns all available credit packages for purchase.
        ///
        /// This endpoint is anonymous (no auth required) so potential users can
        /// view pricing options before signing up. Only packages with IsActive=true
        /// are returned, sorted by SortOrder.
        ///
        /// Response: List<CreditPackageDto>
        /// Each CreditPackageDto contains:
        /// - Id (int) : Package identifier
        /// - Name (string) : Package name for display
        /// - Description (string) : What the package provides
        /// - CreditAmount (decimal) : Credits included in package
        /// - Price (decimal) : Cost in configured currency
        /// - Currency (string) : Three-letter currency code (USD, EUR)
        /// - DurationMinutes (int?) : Minutes of platform access per credit
        /// - IsActive (bool) : Package availability status
        /// - SortOrder (int) : UI display order
        /// - DiscountPercentage (decimal?) : Percentage discount if applicable
        /// - IsSubscription (bool) : Whether package is recurring
        /// - DiscountedPrice (decimal?) : Calculated discounted price (Price * (1 - Discount/100))
        /// </summary>
        [HttpGet("packages")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailablePackages()
        {
            var packages = await _walletService.GetAvailablePackagesAsync();
            return Ok(packages);
        }

        /// <summary>
        /// GET: api/credit/packages/all
        /// Returns all credit packages including inactive - Admin only.
        ///
        /// Response: List<CreditPackageDto> with all packages regardless of IsActive status.
        /// </summary>
        [HttpGet("packages/all")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllPackages()
        {
            var packages = await _walletService.GetAllPackagesAsync();
            return Ok(packages);
        }

        /// <summary>
        /// POST: api/credit/packages
        /// Creates a new credit package - Admin only.
        ///
        /// Request Body: CreateCreditPackageDto
        /// - Name (string) : Package name (required, e.g., "Pro Pack")
        /// - Description (string) : Package description (required, max 500 chars)
        /// - CreditAmount (decimal) : Credits in package (required)
        /// - Price (decimal) : Package cost (default USD)
        /// - Currency (string) : Optional 3-letter code (defaults to "USD")
        /// - DurationMinutes (int?) : Optional time duration per credit
        /// - IsActive (bool) : Defaults to true
        /// - SortOrder (int) : Defaults to 0 for UI ordering
        /// - DiscountPercentage (decimal?) : Optional 0-100 percentage
        /// - IsSubscription (bool) : Defaults to false, true for recurring
        ///
        /// Response: Created CreditPackageDto with calculated DiscountedPrice
        /// </summary>
        [HttpPost("packages")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> CreatePackage([FromBody] CreateCreditPackageDto packageDto)
        {
            if (packageDto == null) return BadRequest("Package data is required.");

            var package = await _walletService.CreatePackageAsync(packageDto);
            return CreatedAtAction(nameof(GetAllPackages), new { id = package.Id }, package);
        }

        /// <summary>
        /// POST: api/credit/purchase
        /// Purchases credits for the current user by selecting a credit package.
        ///
        /// Request Body: PurchaseCreditsRequestDto
        /// - CreditPackageId (int) : ID of the package to purchase (required)
        /// - PaymentMethod (string?) : Payment method identifier ("card", "paypal", etc.)
        /// - PaymentReference (string?) : External payment reference ID (Stripe, PayPal txn ID)
        ///
        /// What happens internally:
        /// 1. Validates that the CreditPackage exists and IsActive = true
        /// 2. Gets or creates the UserWallet for the current user
        /// 3. Creates CreditTransaction with TransactionType = Purchase
        ///    - CreditAmount = package.CreditAmount (positive)
        ///    - Description = "Purchase of {package.Name}"
        ///    - RelatedEntityType = "CreditPackage"
        ///    - RelatedEntityId = package.Id
        /// 4. Updates UserWallet:
        ///    - CreditBalance += CreditAmount
        ///    - TotalCreditsPurchased += CreditAmount
        ///    - LastUpdatedAt = DateTime.UtcNow
        ///
        /// Response: CreditPurchaseResponseDto
        /// - Success (bool) : Whether purchase succeeded
        /// - Message (string) : Success or error message
        /// - NewBalance (decimal) : Updated credit balance after purchase
        /// - TransactionId (int) : ID of the created transaction record
        /// </summary>
        [HttpPost("purchase")]
        public async Task<IActionResult> PurchaseCredits([FromBody] PurchaseCreditsRequestDto request)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request == null) return BadRequest("Purchase request is required.");

            var result = await _walletService.PurchaseCreditsAsync(userId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// POST: api/credit/consume
        /// Consumes credits from the current user's wallet for platform access.
        ///
        /// Request Body: ConsumeCreditsRequestDto
        /// - CreditAmount (decimal) : Number of credits to consume (required)
        /// - Description (string) : Reason for consumption (required, e.g., "Demo meeting access")
        /// - RelatedEntityType (string?) : Entity type consuming credits (e.g., "Meeting", "Product")
        /// - RelatedEntityId (int?) : ID of the related entity
        ///
        /// What happens internally:
        /// 1. Gets or creates the UserWallet for the current user
        /// 2. Validates CreditBalance >= CreditAmount (returns error if insufficient)
        /// 3. Creates CreditTransaction with TransactionType = Consumption
        ///    - CreditAmount = -requested amount (negative for consumption)
        ///    - BalanceAfterTransaction = CreditBalance - CreditAmount
        ///    - Links to related entity if provided
        /// 4. Updates UserWallet:
        ///    - CreditBalance -= CreditAmount
        ///    - TotalCreditsConsumed += CreditAmount
        ///    - LastUpdatedAt = DateTime.UtcNow
        ///
        /// Response: CreditConsumeResponseDto
        /// - Success (bool) : Whether consumption succeeded
        /// - Message (string) : Success or error message
        /// - NewBalance (decimal) : Remaining credit balance after consumption
        /// - CreditsConsumed (decimal) : Amount that was consumed
        /// </summary>
        [HttpPost("consume")]
        public async Task<IActionResult> ConsumeCredits([FromBody] ConsumeCreditsRequestDto request)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request == null) return BadRequest("Consume request is required.");

            var result = await _walletService.ConsumeCreditsAsync(userId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// POST: api/credit/check
        /// Checks if user has sufficient credits for an operation.
        ///
        /// Request Body: decimal (raw JSON value representing required credits)
        ///
        /// Response: CreditBalanceResponseDto
        /// - CreditBalance (decimal) : Current available credits in user's wallet
        /// - HasSufficientCredits (bool) : True if CreditBalance >= required amount
        /// - Message (string) : "Sufficient credits available" or "Insufficient credits. Required: X, Available: Y"
        /// </summary>
        [HttpPost("check")]
        public async Task<IActionResult> CheckCreditBalance([FromBody] decimal requiredCredits)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var result = await _walletService.CheckCreditBalanceAsync(userId, requiredCredits);
            return Ok(result);
        }

        /// <summary>
        /// GET: api/credit/transactions
        /// Returns the transaction history for the current user.
        ///
        /// Query Parameters:
        /// - page (int) : Page number for pagination (default 1)
        /// - pageSize (int) : Items per page (default 50)
        ///
        /// Response: List<CreditTransactionDto>
        /// Each CreditTransactionDto contains:
        /// - Id (int) : Transaction identifier
        /// - UserWalletId (int) : Wallet this transaction belongs to
        /// - TransactionType (string) : "Purchase", "Consumption", "Refund", "Adjustment", "Bonus", or "Subscription"
        /// - CreditAmount (decimal) : Positive for credit additions, negative for deductions
        /// - BalanceAfterTransaction (decimal) : Running balance after transaction
        /// - Description (string) : Human-readable transaction details
        /// - ReferenceId (string?) : External payment reference if applicable
        /// - RelatedEntityType (string?) : Type of entity that triggered transaction
        /// - RelatedEntityId (int?) : ID of related entity
        /// - TransactionDate (DateTime) : When transaction occurred
        ///
        /// Ordered by TransactionDate descending (most recent first).
        /// </summary>
        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactionHistory(int page = 1, int pageSize = 50)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var transactions = await _walletService.GetTransactionHistoryAsync(userId, page, pageSize);
            return Ok(transactions);
        }

        /// <summary>
        /// GET: api/credit/transactions/{id}
        /// Returns a specific transaction by ID.
        ///
        /// Response: CreditTransactionDto for the specified transaction
        /// Note: This returns any transaction by ID - consider restricting to user-owned
        /// transactions in production for privacy/security.
        /// </summary>
        [HttpGet("transactions/{id:int}")]
        public async Task<IActionResult> GetTransactionById(int id)
        {
            var transaction = await _walletService.GetTransactionByIdAsync(id);
            if (transaction == null) return NotFound();
            return Ok(transaction);
        }

        /// <summary>
        /// POST: api/credit/adjust?userId={userId}
        /// Adjusts credits for a user - Admin only.
        ///
        /// Query Parameters:
        /// - userId (string) : User ID to adjust credits for (required)
        ///
        /// Request Body: decimal (raw JSON value, positive to add, negative to remove)
        ///
        /// What happens internally:
        /// 1. Gets or creates the UserWallet for specified user
        /// 2. Creates CreditTransaction with TransactionType = Adjustment
        ///    - CreditAmount = adjustment value (signed)
        ///    - Description = "Manual adjustment by admin"
        /// 3. Updates UserWallet based on adjustment type:
        ///    - If positive: TotalCreditsPurchased += CreditAmount
        ///    - If negative: TotalCreditsConsumed += |CreditAmount|
        ///
        /// Response: CreditConsumeResponseDto (same structure as consume response)
        /// - Success (bool) : Whether adjustment succeeded
        /// - Message (string) : Result message
        /// - NewBalance (decimal) : Updated credit balance
        /// - CreditsConsumed (decimal) : Absolute value of adjustment
        /// </summary>
        [HttpPost("adjust")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> AdjustCredits([FromQuery] string userId, [FromBody] decimal creditAmount)
        {
            if (string.IsNullOrWhiteSpace(userId)) return BadRequest("User ID is required.");

            var result = await _walletService.AdjustCreditsAsync(userId, creditAmount, "Manual adjustment by admin");
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// POST: api/credit/initialize
        /// Initializes a wallet for the current user if it doesn't exist.
        ///
        /// This is useful for:
        /// - Pre-creating wallets after user registration
        /// - Admin creating wallets for existing users via service
        /// - Testing purposes
        ///
        /// Returns existing wallet if already present, creates new one if not.
        ///
        /// Response: UserWalletDto of the created or existing wallet
        /// </summary>
        [HttpPost("initialize")]
        public async Task<IActionResult> InitializeWallet()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            await _walletService.InitializeUserWalletAsync(userId);
            var wallet = await _walletService.GetUserWalletAsync(userId);
            
            return Ok(wallet);
        }
    }
}