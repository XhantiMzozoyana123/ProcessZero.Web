using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using ProcessZero.Infrastructure.Services;
using ProcessZero.Infrastructure.Filters;
using ProcessZero.Domain;
using ProcessZero.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreditController : ControllerBase
    {
        private readonly IUserWalletService _walletService;
        private readonly IConfiguration _configuration;
        private readonly IPayPalService _payPalService;
        private readonly IPayGateService _payGateService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CreditController> _logger;
        private readonly TimerServiceClient _timerService;
        private readonly ILLMService _llmService;

        public CreditController(IUserWalletService walletService, IConfiguration configuration, IPayPalService payPalService, IPayGateService payGateService, ApplicationDbContext context, ILogger<CreditController> logger, TimerServiceClient timerService, ILLMService llmService)
        {
            _walletService = walletService;
            _configuration = configuration;
            _payPalService = payPalService;
            _payGateService = payGateService;
            _context = context;
            _logger = logger;
            _timerService = timerService;
            _llmService = llmService;
        }

        private string GetUserId() =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpGet("wallet")]
        [AllowTimerService]  // Accepts either JWT or X-Timer-Api-Key header
        public async Task<IActionResult> GetWallet()
        {
            // Try JWT claim first, then fall back to X-User-Id header (for service-to-service auth)
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = Request.Headers["X-User-Id"].FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var wallet = await _walletService.GetUserWalletAsync(userId);
            return Ok(wallet);
        }

        [HttpGet("packages")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAvailablePackages()
        {
            var packages = await _walletService.GetAvailablePackagesAsync();
            return Ok(packages);
        }

        [HttpGet("packages/all")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllPackages()
        {
            var packages = await _walletService.GetAllPackagesAsync();
            return Ok(packages);
        }

        [HttpPost("packages")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> CreatePackage([FromBody] CreateCreditPackageDto packageDto)
        {
            if (packageDto == null) return BadRequest("Package data is required.");

            var package = await _walletService.CreatePackageAsync(packageDto);
            return CreatedAtAction(nameof(GetAllPackages), new { id = package.Id }, package);
        }

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

        [HttpPost("consume")]
        [AllowTimerService]  // Accepts either JWT or X-Timer-Api-Key header
        public async Task<IActionResult> ConsumeCredits([FromBody] ConsumeCreditsRequestDto request)
        {
            // Try JWT claim first, then fall back to X-User-Id header (for service-to-service auth)
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = Request.Headers["X-User-Id"].FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request == null) return BadRequest("Consume request is required.");

            var result = await _walletService.ConsumeCreditsAsync(userId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("check")]
        [AllowTimerService]  // Accepts either JWT or X-Timer-Api-Key header
        public async Task<IActionResult> CheckCreditBalance([FromBody] decimal requiredCredits)
        {
            // Try JWT claim first, then fall back to X-User-Id header (for service-to-service auth)
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = Request.Headers["X-User-Id"].FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var result = await _walletService.CheckCreditBalanceAsync(userId, requiredCredits);
            return Ok(result);
        }

        [HttpGet("remaining-hours")]
        [AllowTimerService]  // Accepts either JWT or X-Timer-Api-Key header
        public async Task<IActionResult> GetRemainingHours(CancellationToken cancellationToken)
        {
            // Try JWT claim first, then fall back to X-User-Id header (for service-to-service auth)
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId))
            {
                userId = Request.Headers["X-User-Id"].FirstOrDefault();
            }
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            // Delegate to standalone ProcessZero.TimerService which tracks sessions independently
            var result = await _timerService.GetRemainingHoursAsync(userId, cancellationToken);
            if (result == null)
            {
                // Fallback to direct service
                var remainingHours = await _walletService.GetRemainingHoursAsync(userId, cancellationToken);
                return Ok(new { remainingHours });
            }
            return Ok(new { remainingHours = result.RemainingHours });
        }

        [HttpGet("transactions")]
        public async Task<IActionResult> GetTransactionHistory(int page = 1, int pageSize = 50)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var transactions = await _walletService.GetTransactionHistoryAsync(userId, page, pageSize);
            return Ok(transactions);
        }

        [HttpGet("transactions/{id:int}")]
        public async Task<IActionResult> GetTransactionById(int id)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var transaction = await _walletService.GetTransactionByIdAsync(userId, id);
            if (transaction == null) return NotFound();
            return Ok(transaction);
        }

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

        [HttpPost("initialize")]
        public async Task<IActionResult> InitializeWallet()
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            await _walletService.InitializeUserWalletAsync(userId);
            var wallet = await _walletService.GetUserWalletAsync(userId);
            
            return Ok(wallet);
        }

        [HttpPost("consume-active-usage")]
        public async Task<IActionResult> ConsumeActiveUsage([FromQuery] int minutes = 10, CancellationToken cancellationToken = default)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var result = await _walletService.ConsumeActiveUsageAsync(userId, minutes, cancellationToken);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("paygate/create-order")]
        public async Task<IActionResult> CreatePayGateOrder([FromBody] CreatePayGateOrderRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request == null || request.PackageId <= 0)
                return BadRequest(new { message = "Invalid package." });

            var packages = await _walletService.GetAvailablePackagesAsync();
            var package = packages.FirstOrDefault(p => p.Id == request.PackageId);
            if (package == null)
                return NotFound(new { message = "Package not found." });

            // Use the request's host for callback URL in development, or configured URL in production
            var webUrl = _configuration["PayGate:WebUrl"] ?? "https://processzero.xyz";
            var defaultProvider = _configuration["PayGate:DefaultProvider"] ?? "moonpay";
            var defaultCurrency = _configuration["PayGate:DefaultCurrency"] ?? "USD";

            // Build a unique order ID for tracking
            var orderId = Guid.NewGuid().ToString();

            // Build a unique callback URL with the user ID, package ID, and order ID
            var callbackUrl = $"{webUrl}/api/credit/paygate-callback?userId={userId}&packageId={package.Id}&orderId={orderId}";

            _logger.LogInformation(
                "Creating PayGate order for user {UserId}, package {PackageId}, orderId {OrderId}",
                userId, package.Id, orderId);

            try
            {
                // Step 1: Create wallet on PayGate
                var walletResponse = await _payGateService.CreateWalletAsync(
                    payoutWallet: null, // uses configured default
                    callbackUrl: callbackUrl,
                    cancellationToken: cancellationToken);

                // Step 2: Build the payment URL
                var paymentUrl = _payGateService.BuildPaymentUrl(
                    encryptedAddressIn: walletResponse.AddressIn,
                    amount: package.Price,
                    provider: defaultProvider,
                    email: request.Email ?? "customer@processzero.xyz",
                    currency: defaultCurrency);

                _logger.LogInformation(
                    "PayGate order created successfully for user {UserId}, package {PackageId}, orderId {OrderId}",
                    userId, package.Id, orderId);

                return Ok(new CreatePayGateOrderResponse
                {
                    PaymentUrl = paymentUrl,
                    OrderId = walletResponse.IpnToken,
                    PackageId = package.Id,
                    AddressIn = walletResponse.PolygonAddressIn
                });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PayGate order creation failed for package {PackageId}: {Message}", request.PackageId, ex.Message);
                return StatusCode(500, new { message = "Payment setup failed. Please try again later.", detail = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "PayGate API request failed for package {PackageId}: {Message}", request.PackageId, ex.Message);
                return StatusCode(500, new { message = "Payment gateway is temporarily unavailable. Please try again later.", detail = ex.Message });
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "PayGate API request timed out for package {PackageId}", request.PackageId);
                return StatusCode(500, new { message = "Payment gateway timed out. Please try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during PayGate order creation for package {PackageId}: {Message}", request.PackageId, ex.Message);
                return StatusCode(500, new { message = "An unexpected error occurred while processing your payment." });
            }
        }

        /// <summary>
        /// Handles the PayGate callback (IPN) after a customer completes payment.
        /// PayGate calls this URL with the payment status after the customer completes the checkout.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("paygate-callback")]
        public async Task<IActionResult> PayGateCallback(
            [FromQuery] string userId,
            [FromQuery] int packageId,
            [FromQuery] string orderId,
            [FromQuery] string? ipnToken,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(
                "PayGate callback received: userId={UserId}, packageId={PackageId}, orderId={OrderId}, ipnToken={IpnToken}",
                userId, packageId, orderId, ipnToken);

            if (string.IsNullOrWhiteSpace(userId) || packageId <= 0 || string.IsNullOrWhiteSpace(orderId))
            {
                _logger.LogWarning("PayGate callback received with invalid parameters");
                return BadRequest(new { message = "Invalid callback parameters." });
            }

            try
            {
                // Verify the payment status with PayGate using the IPN token
                if (!string.IsNullOrWhiteSpace(ipnToken))
                {
                    var status = await _payGateService.GetPaymentStatusAsync(ipnToken, cancellationToken);
                    if (status != null && status.Status == "completed")
                    {
                        _logger.LogInformation(
                            "PayGate callback verified: Payment completed for order {OrderId}, txid={TxidOut}",
                            orderId, status.TxidOut);

                        // Credit the user's wallet
                        var packages = await _walletService.GetAvailablePackagesAsync();
                        var package = packages.FirstOrDefault(p => p.Id == packageId);
                        if (package != null)
                        {
                            var purchaseResult = await _walletService.PurchaseCreditsAsync(userId, new PurchaseCreditsRequestDto
                            {
                                CreditPackageId = package.Id,
                                PaymentMethod = "PayGate",
                                PaymentReference = orderId
                            }, cancellationToken);

                            if (purchaseResult.Success)
                            {
                                _logger.LogInformation(
                                    "Credits purchased successfully for user {UserId}, package {PackageId}, order {OrderId}",
                                    userId, packageId, orderId);
                            }
                            else
                            {
                                _logger.LogWarning(
                                    "Credit purchase failed for user {UserId}, package {PackageId}, order {OrderId}: {Message}",
                                    userId, packageId, orderId, purchaseResult.Message);
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning(
                            "PayGate callback: Payment not completed for order {OrderId}. Status: {Status}",
                            orderId, status?.Status ?? "null");
                    }
                }
                else
                {
                    _logger.LogWarning(
                        "PayGate callback received without IPN token for order {OrderId}",
                        orderId);
                }

                // Return a success response to acknowledge receipt
                return Ok(new { message = "Callback received successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PayGate callback for order {OrderId}", orderId);
                return Ok(new { message = "Callback received but processing failed." });
            }
        }

        [HttpPost("payshap/create-order")]
        public async Task<IActionResult> CreatePayShapOrder([FromBody] CreatePayShapOrderRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request == null || request.PackageId <= 0)
                return BadRequest(new { message = "Invalid package." });

            var packages = await _walletService.GetAvailablePackagesAsync();
            var package = packages.FirstOrDefault(p => p.Id == request.PackageId);
            if (package == null)
                return NotFound(new { message = "Package not found." });

            // Get PayShap configuration from appsettings
            var payshapAccountNumber = _configuration["PayShap:AccountNumber"];
            var payshapAccountHolder = _configuration["PayShap:AccountHolder"];
            var expiresInHours = int.Parse(_configuration["PayShap:OrderExpiryHours"] ?? "24");

            if (string.IsNullOrWhiteSpace(payshapAccountNumber) || string.IsNullOrWhiteSpace(payshapAccountHolder))
            {
                _logger.LogError("PayShap configuration is missing. Check 'PayShap:AccountNumber' and 'PayShap:AccountHolder' in app settings.");
                return StatusCode(500, new { message = "Payment configuration error. Please contact support." });
            }

            // Generate unique order ID and PayShap reference
            var orderId = Guid.NewGuid().ToString();
            var payshapReference = $"PZ-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
            var expiresAt = DateTime.UtcNow.AddHours(expiresInHours);

            // Create PaymentOrder entity
            var paymentOrder = new PaymentOrder
            {
                UserId = userId,
                OrderId = orderId,
                CreditPackageId = package.Id,
                Amount = package.Price,
                Currency = package.Currency,
                PayShapAccountNumber = payshapAccountNumber,
                PayShapAccountHolder = payshapAccountHolder,
                PayShapReference = payshapReference,
                Status = "Pending",
                UserEmail = request.Email,
                UserPhone = request.Phone,
                ExpiresAt = expiresAt
            };

            _context.PaymentOrders.Add(paymentOrder);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "PayShap order created for user {UserId}, package {PackageId}, orderId {OrderId}",
                userId, package.Id, orderId);

            return Ok(new CreatePayShapOrderResponse
            {
                OrderId = orderId,
                PackageId = package.Id,
                Amount = package.Price,
                Currency = package.Currency,
                PayShapAccountNumber = payshapAccountNumber,
                PayShapAccountHolder = payshapAccountHolder,
                PayShapReference = payshapReference,
                Instructions = $"Please transfer {package.Price:N2} {package.Currency} to the account above using reference: {payshapReference}. Then upload a screenshot of your payment confirmation.",
                ExpiresAt = expiresAt
            });
        }

        [HttpPost("payshap/submit-proof")]
        public async Task<IActionResult> SubmitPaymentProof([FromBody] SubmitPaymentProofRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request == null || string.IsNullOrWhiteSpace(request.OrderId))
                return BadRequest(new { message = "Order ID is required." });

            var paymentOrder = await _context.PaymentOrders
                .FirstOrDefaultAsync(po => po.OrderId == request.OrderId && po.UserId == userId, cancellationToken);

            if (paymentOrder == null)
                return NotFound(new { message = "Payment order not found." });

            if (paymentOrder.Status != "Pending")
                return BadRequest(new { message = $"Payment order is already {paymentOrder.Status}." });

            if (DateTime.UtcNow > paymentOrder.ExpiresAt)
            {
                paymentOrder.Status = "Expired";
                await _context.SaveChangesAsync(cancellationToken);
                return BadRequest(new { message = "Payment order has expired. Please create a new order." });
            }

            paymentOrder.PaymentProofScreenshot = request.PaymentProofScreenshot;
            paymentOrder.BankTransactionReference = request.BankTransactionReference;
            paymentOrder.Status = "PaymentReceived";

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Payment proof submitted for order {OrderId} by user {UserId}",
                request.OrderId, userId);

            // Use LLM to verify the payment proof image
            // Credits are ONLY added when verification is explicitly successful.
            // If verification fails, the user can retry uploading a valid image.
            bool isVerified = false;
            string verificationMessage = "";
            
            _logger.LogInformation("About to call LLM verification for order {OrderId}", request.OrderId);
            
            try
            {
                _logger.LogInformation("Calling LLM service for order {OrderId}", request.OrderId);
                isVerified = await _llmService.VerifyPaymentProofAsync(
                    request.PaymentProofScreenshot, 
                    paymentOrder.Amount, 
                    paymentOrder.Currency);
                
                _logger.LogInformation("LLM verification result for order {OrderId}: {IsVerified}", request.OrderId, isVerified);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LLM verification encountered an error for order {OrderId} - credits will not be added", request.OrderId);
                isVerified = false;
            }

            // Build the user-friendly message about what the image should contain.
            // This is used both when AI verification fails and when the LLM service errors out.
            var imageRequirements = $"The image you uploaded doesn't appear to be a valid payment confirmation screenshot. Please ensure your screenshot includes:\n" +
                $"• The exact payment amount of {paymentOrder.Amount:N2} {paymentOrder.Currency}\n" +
                $"• Your bank's payment confirmation or success screen\n" +
                $"• The date and time of the transaction\n" +
                $"• The recipient/beneficiary name\n\n" +
                $"Please capture and upload a clearer screenshot of your completed payment, then try again.";

            if (isVerified)
            {
                // Verification passed — add credits and complete the order
                var packages = await _walletService.GetAvailablePackagesAsync();
                var package = packages.FirstOrDefault(p => p.Id == paymentOrder.CreditPackageId);
                
                if (package != null)
                {
                    var purchaseResult = await _walletService.PurchaseCreditsAsync(
                        userId, 
                        new PurchaseCreditsRequestDto
                        {
                            CreditPackageId = package.Id,
                            PaymentMethod = "PayShap",
                            PaymentReference = paymentOrder.OrderId
                        }, 
                        cancellationToken);

                    if (purchaseResult.Success)
                    {
                        paymentOrder.Status = "Completed";
                        paymentOrder.VerifiedAt = DateTime.UtcNow;
                        paymentOrder.AdminNotes = "AI verification passed - credits added successfully";
                        
                        await _context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "Credits added for order {OrderId}, user {UserId}, status: {Status}",
                            request.OrderId, userId, paymentOrder.Status);

                        verificationMessage = "Payment proof verified successfully. Credits have been added to your wallet.";
                    }
                    else
                    {
                        _logger.LogError(
                            "Failed to credit wallet for order {OrderId}: {Message}",
                            request.OrderId, purchaseResult.Message);
                        return StatusCode(500, new { message = "Payment proof submitted but failed to add credits.", detail = purchaseResult.Message });
                    }
                }

                return Ok(new SubmitPaymentProofResponse
                {
                    Success = true,
                    Message = verificationMessage,
                    OrderId = request.OrderId,
                    Status = paymentOrder.Status,
                    VerificationPassed = true
                });
            }
            else
            {
                // Verification failed or errored — do NOT add credits.
                // Reset the order back to "Pending" so the user can retry uploading a valid screenshot.
                paymentOrder.Status = "Pending";
                paymentOrder.AdminNotes = $"AI verification did not pass — image appears invalid or incomplete. User may retry uploading a valid payment screenshot.";
                
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "AI verification failed for order {OrderId}, user {UserId}. Credits not added. Order reset to Pending for retry.",
                    request.OrderId, userId);

                return Ok(new SubmitPaymentProofResponse
                {
                    Success = true,
                    Message = imageRequirements,
                    OrderId = request.OrderId,
                    Status = "Pending",
                    VerificationPassed = false
                });
            }
        }

        [HttpPost("payshap/verify")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.OrderId))
                return BadRequest(new { message = "Order ID is required." });

            var paymentOrder = await _context.PaymentOrders
                .FirstOrDefaultAsync(po => po.OrderId == request.OrderId, cancellationToken);

            if (paymentOrder == null)
                return NotFound(new { message = "Payment order not found." });

            if (request.Approved)
            {
                // Credit the user's wallet
                var packages = await _walletService.GetAvailablePackagesAsync();
                var package = packages.FirstOrDefault(p => p.Id == paymentOrder.CreditPackageId);
                
                if (package != null)
                {
                    var purchaseResult = await _walletService.PurchaseCreditsAsync(
                        paymentOrder.UserId, 
                        new PurchaseCreditsRequestDto
                        {
                            CreditPackageId = package.Id,
                            PaymentMethod = "PayShap",
                            PaymentReference = paymentOrder.OrderId
                        }, 
                        cancellationToken);

                    if (purchaseResult.Success)
                    {
                        paymentOrder.Status = "Completed";
                        paymentOrder.VerifiedAt = DateTime.UtcNow;
                        paymentOrder.CompletedAt = DateTime.UtcNow;
                        paymentOrder.AdminNotes = request.AdminNotes;

                        await _context.SaveChangesAsync(cancellationToken);

                        _logger.LogInformation(
                            "Payment verified and credits added for order {OrderId}, user {UserId}",
                            request.OrderId, paymentOrder.UserId);

                        return Ok(new VerifyPaymentResponse
                        {
                            Success = true,
                            Message = "Payment verified and credits added successfully.",
                            OrderId = request.OrderId,
                            Status = "Completed"
                        });
                    }
                    else
                    {
                        _logger.LogError(
                            "Failed to credit wallet for verified order {OrderId}: {Message}",
                            request.OrderId, purchaseResult.Message);
                        return StatusCode(500, new { message = "Payment verified but failed to add credits.", detail = purchaseResult.Message });
                    }
                }
            }
            else
            {
                paymentOrder.Status = "Failed";
                paymentOrder.VerifiedAt = DateTime.UtcNow;
                paymentOrder.AdminNotes = request.AdminNotes;

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Payment rejected for order {OrderId} by admin. Reason: {Reason}",
                    request.OrderId, request.AdminNotes);

                return Ok(new VerifyPaymentResponse
                {
                    Success = true,
                    Message = "Payment marked as failed.",
                    OrderId = request.OrderId,
                    Status = "Failed"
                });
            }

            return Ok(new VerifyPaymentResponse
            {
                Success = false,
                Message = "An error occurred during verification.",
                OrderId = request.OrderId,
                Status = paymentOrder.Status
            });
        }

        [HttpGet("payshap/orders")]
        public async Task<IActionResult> GetMyPaymentOrders(CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var orders = await _context.PaymentOrders
                .Where(po => po.UserId == userId)
                .OrderByDescending(po => po.CreatedAt)
                .Select(po => new PaymentOrderDto
                {
                    OrderId = po.OrderId,
                    UserId = po.UserId,
                    UserEmail = po.UserEmail ?? string.Empty,
                    CreditPackageId = po.CreditPackageId,
                    PackageName = po.CreditPackage.Name,
                    Amount = po.Amount,
                    Currency = po.Currency,
                    PayShapAccountNumber = po.PayShapAccountNumber,
                    PayShapAccountHolder = po.PayShapAccountHolder,
                    PayShapReference = po.PayShapReference,
                    Status = po.Status,
                    PaymentProofScreenshot = po.PaymentProofScreenshot,
                    BankTransactionReference = po.BankTransactionReference,
                    AdminNotes = po.AdminNotes,
                    CreatedAt = po.CreatedAt,
                    ExpiresAt = po.ExpiresAt,
                    VerifiedAt = po.VerifiedAt,
                    CompletedAt = po.CompletedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(orders);
        }

        [HttpGet("payshap/orders/all")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> GetAllPaymentOrders(CancellationToken cancellationToken)
        {
            var orders = await _context.PaymentOrders
                .OrderByDescending(po => po.CreatedAt)
                .Select(po => new PaymentOrderDto
                {
                    OrderId = po.OrderId,
                    UserId = po.UserId,
                    UserEmail = po.UserEmail ?? string.Empty,
                    CreditPackageId = po.CreditPackageId,
                    PackageName = po.CreditPackage.Name,
                    Amount = po.Amount,
                    Currency = po.Currency,
                    PayShapAccountNumber = po.PayShapAccountNumber,
                    PayShapAccountHolder = po.PayShapAccountHolder,
                    PayShapReference = po.PayShapReference,
                    Status = po.Status,
                    PaymentProofScreenshot = po.PaymentProofScreenshot,
                    BankTransactionReference = po.BankTransactionReference,
                    AdminNotes = po.AdminNotes,
                    CreatedAt = po.CreatedAt,
                    ExpiresAt = po.ExpiresAt,
                    VerifiedAt = po.VerifiedAt,
                    CompletedAt = po.CompletedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(orders);
        }

        [HttpPost("paypal/create")]
        public async Task<IActionResult> CreatePayPalOrder([FromBody] CreatePayPalOrderRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (request == null || request.PackageId <= 0)
                return BadRequest(new { message = "Invalid package." });

            var packages = await _walletService.GetAvailablePackagesAsync();
            var package = packages.FirstOrDefault(p => p.Id == request.PackageId);
            if (package == null)
                return NotFound(new { message = "Package not found." });

            var webUrl = _configuration["PayPal:WebUrl"] ?? "https://processzero.xyz";
            var returnUrl = request.ReturnUrl ?? $"{webUrl}/account/credits/wallet?paypal=success";
            var cancelUrl = request.CancelUrl ?? $"{webUrl}/account/credits/packages?paypal=cancelled";

            try
            {
                var (orderId, approvalUrl) = await _payPalService.CreateOrderAsync(package.Price, package.Currency, returnUrl, cancelUrl, cancellationToken);
                return Ok(new { orderId, approvalUrl, packageId = package.Id });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "PayPal order creation failed for package {PackageId}", request.PackageId);
                return StatusCode(500, new { message = "PayPal payment setup failed. Please check payment configuration or try again later." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during PayPal order creation for package {PackageId}", request.PackageId);
                return StatusCode(500, new { message = "An unexpected error occurred while setting up PayPal payment." });
            }
        }

        [HttpPost("paypal/capture")]
        public async Task<IActionResult> CapturePayPalOrder([FromBody] CapturePayPalOrderRequest request, CancellationToken cancellationToken)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            if (string.IsNullOrEmpty(request.OrderId))
                return BadRequest(new { message = "Order ID is required." });

            if (request.PackageId <= 0)
                return BadRequest(new { message = "Invalid package." });

            // Verify the package exists
            var packages = await _walletService.GetAvailablePackagesAsync();
            var package = packages.FirstOrDefault(p => p.Id == request.PackageId);
            if (package == null)
                return NotFound(new { message = "Package not found." });

            // Capture the PayPal order
            var captureJson = await _payPalService.CaptureOrderAsync(request.OrderId, cancellationToken);

            // Verify the capture was successful before crediting the user's wallet
            using var captureDoc = JsonDocument.Parse(captureJson);
            var root = captureDoc.RootElement;

            // Check the top-level order status
            var orderStatus = root.GetProperty("status").GetString();
            if (orderStatus != "COMPLETED")
            {
                _logger.LogWarning("PayPal capture for order {OrderId} was not completed. Order status: {Status}", request.OrderId, orderStatus);
                return BadRequest(new { message = $"Payment was not completed. Status: {orderStatus}" });
            }

            // Also verify the individual capture status if available
            if (root.TryGetProperty("purchase_units", out var purchaseUnits) &&
                purchaseUnits.GetArrayLength() > 0 &&
                purchaseUnits[0].TryGetProperty("payments", out var payments) &&
                payments.TryGetProperty("captures", out var captures) &&
                captures.GetArrayLength() > 0)
            {
                var captureStatus = captures[0].GetProperty("status").GetString();
                if (captureStatus != "COMPLETED")
                {
                    _logger.LogWarning("PayPal capture for order {OrderId} was not completed. Capture status: {Status}", request.OrderId, captureStatus);
                    return BadRequest(new { message = $"Payment was not completed. Status: {captureStatus}" });
                }
            }

            // Credit the user's wallet
            var purchaseResult = await _walletService.PurchaseCreditsAsync(userId, new PurchaseCreditsRequestDto
            {
                CreditPackageId = package.Id,
                PaymentMethod = "PayPal",
                PaymentReference = request.OrderId
            }, cancellationToken);

            if (!purchaseResult.Success)
                return BadRequest(purchaseResult);

            // Return both capture details and credit result
            var result = new
            {
                capture = captureJson,
                credits = purchaseResult
            };

            return Ok(result);
        }
    }
}
