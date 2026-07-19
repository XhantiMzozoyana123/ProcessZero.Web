using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Dtos;
using ProcessZero.Application.Interfaces;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(IInvoiceService invoiceService, ILogger<InvoiceController> logger)
        {
            _invoiceService = invoiceService;
            _logger = logger;
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var invoices = await _invoiceService.GetAllInvoicesByUserIdAsync(userId);
            return Ok(invoices);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();
            return Ok(invoice);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateInvoiceRequest request, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var invoice = new Domain.Entities.Invoice
            {
                ClientId = request.ClientId,
                ProductId = request.ProductId,
                Amount = request.Amount,
                IsPaid = request.IsPaid,
                UserId = userId,
                IssuedAt = DateTime.UtcNow
            };

            await _invoiceService.CreateInvoiceAsync(invoice);
            return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceRequest request, CancellationToken cancellationToken)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null) return NotFound();

            invoice.Amount = request.Amount;
            invoice.ProductId = request.ProductId;
            invoice.ClientId = request.ClientId;
            invoice.IsPaid = request.IsPaid;

            await _invoiceService.EditInvoiceAsync(invoice);
            return Ok(invoice);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            await _invoiceService.DeleteInvoiceAsync(id);
            return NoContent();
        }
    }

    public class CreateInvoiceRequest
    {
        public int ClientId { get; set; }
        public int ProductId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }

    public class UpdateInvoiceRequest
    {
        public int ClientId { get; set; }
        public int ProductId { get; set; }
        public decimal Amount { get; set; }
        public bool IsPaid { get; set; }
    }
}