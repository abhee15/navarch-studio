using Microsoft.AspNetCore.Mvc;
using Shared.DTOs;
using DataService.Services;
using Shared.Services;

namespace DataService.Controllers;

[ApiController]
[Asp.Versioning.ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductsController> _logger;
    private readonly IJwtService _jwtService;

    public ProductsController(
        IProductService productService,
        ILogger<ProductsController> logger,
        IJwtService jwtService)
    {
        _productService = productService;
        _logger = logger;
        _jwtService = jwtService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(CancellationToken cancellationToken)
    {
        // Optional: Get authenticated user info from JWT claims
        var userId = _jwtService.GetUserId(User);
        if (!string.IsNullOrEmpty(userId))
        {
            _logger.LogInformation("User {UserId} is fetching products", userId);
        }

        var products = await _productService.GetAllProductsAsync(cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(string id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);

        if (product == null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(
        [FromBody] CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        // Require authentication for creating products
        var userId = _jwtService.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { message = "Authentication required to create products" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation("User {UserId} is creating a product", userId);
        var product = await _productService.CreateProductAsync(dto, cancellationToken);

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            product);
    }
}





