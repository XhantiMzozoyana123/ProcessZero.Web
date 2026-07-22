using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;
using System.Text.Json;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CreditController : ControllerBase
    {
        private readonly IUserWalletService _walletService;
        private readonly IConfiguration _configuration;
        private readonly IPayPalService _payPalService;
        private readonly ILogger<CreditController> _logger;

        public CreditController(IUserWalletService walletService, IConfiguration configuration, IPayPalService payPalService, ILogger<CreditController> logger)
        {
            _walletService = walletService;
            _configuration = configuration;
            _payPalService = payPalService;
            _logger = logger;
        }

        private string GetUserId() =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        [HttpGet("wallet")]
        public async Task<IActionResult> GetWallet()
        {
            var userId = GetUserId();
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

        [HttpPost("check")]
        public async Task<IActionResult> CheckCreditBalance([FromBody] decimal requiredCredits)
        {
            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var result = await _walletService.CheckCreditBalanceAsync(userId, requiredCredits);
            return Ok(result);
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

            var (orderId, approvalUrl) = await _payPalService.CreateOrderAsync(package.Price, package.Currency, returnUrl, cancelUrl, cancellationToken);

            return Ok(new { orderId, approvalUrl, packageId = package.Id });
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
