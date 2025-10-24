using Shared.Models;
using Shared.DTOs;

namespace Shared.TestData;

public static class ProductFactory
{
    public static Product CreateProduct(string? name = null, decimal? price = null)
    {
        return new Product
        {
            Id = Guid.NewGuid().ToString(),
            Name = name ?? $"Product {Guid.NewGuid():N}",
            Price = price ?? 99.99m,
            Description = $"Description for {name ?? "Product"}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };
    }

    public static CreateProductDto CreateProductDto(string? name = null, decimal? price = null)
    {
        return new CreateProductDto
        {
            Name = name ?? $"Product {Guid.NewGuid():N}",
            Price = price ?? 99.99m,
            Description = $"Description for {name ?? "Product"}"
        };
    }

    public static ProductDto CreateProductDtoFromProduct(Product product)
    {
        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            Description = product.Description,
            CreatedAt = product.CreatedAt
        };
    }
}





