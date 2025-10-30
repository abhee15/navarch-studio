namespace Shared.DTOs;

/// <summary>
/// DTO for project board
/// </summary>
public class ProjectBoardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<BoardCardDto> Cards { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for board card
/// </summary>
public class BoardCardDto
{
    public Guid Id { get; set; }
    public Guid? VesselId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "data";
    public string? Tags { get; set; }
    public int DisplayOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// DTO for creating/updating a project board
/// </summary>
public class CreateProjectBoardDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// DTO for creating/updating a board card
/// </summary>
public class CreateBoardCardDto
{
    public Guid? VesselId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "data";
    public string? Tags { get; set; }
}
