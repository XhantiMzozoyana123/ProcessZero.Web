using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

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

        public CreditController(IUserWalletService walletService, IConfiguration configuration, IPayPalService payPalService)
        {
            _walletService = walletService;
            _configuration = configuration;
            _payPalService = payPalService;
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
        public async Task<IActionResult> CreatePayPalOrder([FromBody] CreatePayPalOrderRequest request)
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
            var returnUrl = $"{webUrl}/account/credits/wallet?paypal=success";
            var cancelUrl = $"{webUrl}/account/credits/packages?paypal=cancelled";

            var orderId = await _payPalService.CreateOrderAsync(package.Price, package.Currency, returnUrl, cancelUrl);

            return Ok(new { orderId, packageId = package.Id });
        }

        [HttpPost("paypal/capture")]
        public async Task<IActionResult> CapturePayPalOrder([FromBody] CapturePayPalOrderRequest request)
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
            var captureJson = await _payPalService.CaptureOrderAsync(request.OrderId);

            // Credit the user's wallet
            var purchaseResult = await _walletService.PurchaseCreditsAsync(userId, new PurchaseCreditsRequestDto
            {
                CreditPackageId = package.Id,
                PaymentMethod = "PayPal",
                PaymentReference = request.OrderId
            });

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
