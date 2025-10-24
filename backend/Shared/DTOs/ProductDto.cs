namespace Shared.DTOs;

public record ProductDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required string Description { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateProductDto
{
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required string Description { get; init; }
}





