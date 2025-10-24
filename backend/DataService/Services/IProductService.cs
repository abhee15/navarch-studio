using Shared.DTOs;

namespace DataService.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync(CancellationToken cancellationToken);
    Task<ProductDto?> GetProductByIdAsync(string id, CancellationToken cancellationToken);
    Task<ProductDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken);
}





