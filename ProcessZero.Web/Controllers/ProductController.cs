using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProcessZero.Application.Interfaces;
using ProcessZero.Domain.Entities;
using System.Security.Claims;

namespace ProcessZero.Web.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    /// <summary>
    /// ProductController exposes endpoints to manage products.
    ///
    /// Models referenced below describe the columns/properties used by the controller:
    ///
    /// Product (ProcessZero.Domain.Entities.Product)
    /// - Id (int) : Primary key inherited from BaseEntity.
    /// - UserId (string) : Owner/creator user id (assigned from JWT on create).
    /// - Name (string) : Product name shown to customers.
    /// - Description (string) : Long description of the product.
    /// - ProfilePictureBase64 (string) : Optional base64-encoded image (thumbnail).
    /// - Url (string) : Optional listing or landing page URL for the product.
    /// - NegotiableAmounts (string) : Optional text describing flexible pricing tiers.
    /// - ActualAmount (decimal) : The listed price / amount for the product.
    /// - CreatedAt (DateTime) : Timestamp when the product was created (from BaseEntity).
    /// - UpdatedAt (DateTime) : Timestamp when the product was last updated (from BaseEntity).
    ///
    /// BaseEntity (ProcessZero.Domain.BaseEntity)
    /// - Id, UserId, CreatedAt, UpdatedAt
    ///
    /// The controller returns Product entities directly; consider mapping to DTOs if exposing to public clients.
    /// </summary>
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;

        /// <summary>
        /// Constructor. Requires an <see cref="IProductService"/> provided by DI.
        /// </summary>
        /// <param name="productService">Service handling product operations.</param>
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>
        /// Helper to extract the authenticated user's id from JWT claims.
        /// Returns empty string when not available.
        /// </summary>
        /// <summary>
        /// Extracts the authenticated user's id from JWT claims. Returns empty string when not available.
        /// This value is assigned to <see cref="Product.UserId"/> when creating a product.
        /// </summary>
        private string GetUserId() =>
            User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

        /// <summary>
        /// GET: api/product
        /// Returns a list of available products. This endpoint is anonymous (no auth required).
        /// </summary>
        /// <summary>
        /// GET: api/product
        /// Returns a list of available products. This endpoint is anonymous (no auth required).
        /// Returns the full Product entities; fields include Id, Name, Description, ActualAmount, Url, etc.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productService.GetAllProductAsync();
            return Ok(products);
        }

        /// <summary>
        /// GET: api/product/{id}
        /// Returns a single product by id. Anonymous access allowed.
        /// </summary>
        /// <summary>
        /// GET: api/product/{id}
        /// Returns a single product by id. Anonymous access allowed.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        /// <summary>
        /// POST: api/product
        /// Creates a new product. The authenticated admin user's id is assigned to <c>UserId</c>.
        /// Returns 201 Created with the created product when successful.
        /// </summary>
        /// <summary>
        /// POST: api/product
        /// Creates a new product. The authenticated admin user's id is assigned to Product.UserId.
        /// Required fields (on the Product entity): Name, ActualAmount. Optional: Description, Url, NegotiableAmounts.
        /// Returns 201 Created with the created product when successful.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Create([FromBody] Product product)
        {
            if (product == null) return BadRequest("Product is required.");

            var userId = GetUserId();
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            product.UserId = userId;
            await _productService.AddProductAsync(product);

            if (product.Id != 0)
                return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);

            return NoContent();
        }

        /// <summary>
        /// PUT: api/product/{id}
        /// Updates a product. Validates that the payload id matches the route id.
        /// </summary>
        /// <summary>
        /// PUT: api/product/{id}
        /// Updates a product. Validates that the payload id matches the route id.
        /// Only Admin users may call this endpoint. Modified fields on Product will be persisted and UpdatedAt will be set.
        /// </summary>
        [HttpPut("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Product product)
        {
            if (product == null) return BadRequest("Product is required.");
            if (product.Id != id) return BadRequest("Id mismatch.");

            await _productService.UpdateProductAsync(product);
            return NoContent();
        }

        /// <summary>
        /// DELETE: api/product/{id}
        /// Deletes a product. Restricted to admins by controller-level policy.
        /// </summary>
        /// <summary>
        /// DELETE: api/product/{id}
        /// Deletes a product. Restricted to admins. Deletion will also send discontinuation notices to sales reps.
        /// </summary>
        [HttpDelete("{id:int}")]
        [Authorize(Policy = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            await _productService.DeleteProductAsync(id);
            return NoContent();
        }
    }
}
