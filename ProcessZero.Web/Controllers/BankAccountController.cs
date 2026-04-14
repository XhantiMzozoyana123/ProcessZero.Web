using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    /// <summary>
    /// Controller responsible for managing bank account metadata for the authenticated user.
    /// Models and types used:
    ///
    /// BankAccount:
    /// - AccountHolderName (string) (required, max length 200)
    /// - AccountNumber (string) (required, max length 64) - sensitive, often excluded from JSON responses
    /// - BankCode (string) (optional, max length 20) - often excluded from JSON responses
    /// - BankName (string) (optional, max length 200)
    /// - MaskedAccountNumber (string) (not mapped, read-only masked view)
    /// - LastDigits(int) (helper returning last N digits)
    /// - IsAccountNumberFormatValid() (validation helper)
    ///
    /// BaseEntity (common columns):
    /// - Id (int)
    /// - UserId (string)
    /// - CreatedAt (DateTime)
    /// - UpdatedAt (DateTime)
    ///
    /// IBankAccountService (important methods):
    /// - Task<int> CreateAsync(BankAccount bankAccount, CancellationToken ct = default)
    /// - Task UpdateAsync(BankAccount bankAccount, CancellationToken ct = default)
    /// - Task<BankAccount?> GetByUserIdAsync(string userId, CancellationToken ct = default)
    /// </summary>
    public class BankAccountController : ControllerBase
    {
        private readonly IBankAccountService _bankAccountService;

        public BankAccountController(IBankAccountService bankAccountService)
        {
            _bankAccountService = bankAccountService;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BankAccount model, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();
            model.UserId = userId;

            var id = await _bankAccountService.CreateAsync(model, ct);

            return Ok(new { Id = id });
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] BankAccount model, CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            var existing = await _bankAccountService.GetByUserIdAsync(userId, ct);

            if (existing == null)
                return NotFound("Bank account not found.");
            model.Id = existing.Id;
            model.UserId = userId;

            await _bankAccountService.UpdateAsync(model, ct);

            return NoContent();
        }

        [HttpGet]
        public async Task<IActionResult> GetMyAccount(CancellationToken ct)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
                return Unauthorized();

            var account = await _bankAccountService.GetByUserIdAsync(userId, ct);

            if (account == null)
                return NotFound("No bank account found.");

            return Ok(account);
        }
    }
}
