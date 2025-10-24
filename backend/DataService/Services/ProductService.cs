using Microsoft.EntityFrameworkCore;
using Shared.DTOs;
using Shared.Models;
using DataService.Data;

namespace DataService.Services;

public class ProductService : IProductService
{
    private readonly DataDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(DataDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken)
    {
        var products = await _context.Products
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return product != null ? MapToDto(product) : null;
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            Price = dto.Price,
            Description = dto.Description,
            CreatedAt = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product created: {ProductId}", product.Id);
        return MapToDto(product);
    }

    private static ProductDto MapToDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Price = product.Price,
        Description = product.Description,
        CreatedAt = product.CreatedAt
    };
}





