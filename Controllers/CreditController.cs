using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        public CreditController(IUserWalletService walletService)
        {
            _walletService = walletService;
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
    }
}