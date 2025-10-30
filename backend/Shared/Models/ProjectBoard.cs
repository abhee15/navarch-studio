namespace Shared.Models;

/// <summary>
/// Represents a project board for organizing vessels and tasks
/// </summary>
public class ProjectBoard
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /// <summary>
    /// Board name (e.g., "Container Ship Design", "Benchmark Cases")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Board description
    /// </summary>
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public ICollection<BoardCard> Cards { get; set; } = new List<BoardCard>();
}

/// <summary>
/// Represents a card on a project board (typically corresponds to a vessel or task)
/// </summary>
public class BoardCard
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BoardId { get; set; }
    public ProjectBoard Board { get; set; } = null!;

    /// <summary>
    /// Optional reference to a vessel (if this card represents a vessel)
    /// </summary>
    public Guid? VesselId { get; set; }
    public Vessel? Vessel { get; set; }

    /// <summary>
    /// Card title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Card description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Card category taxonomy
    /// Values: "calc", "ux", "data", "docs", "benchmark", etc.
    /// </summary>
    public string Category { get; set; } = "data";

    /// <summary>
    /// Card tags for filtering
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Display order on the board
    /// </summary>
    public int DisplayOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Category taxonomy for organizing board cards
/// </summary>
public static class CardCategory
{
    public const string Calc = "calc"; // Calculations & analysis
    public const string Ux = "ux"; // User experience & UI
    public const string Data = "data"; // Data & datasets
    public const string Docs = "docs"; // Documentation
    public const string Benchmark = "benchmark"; // Benchmark cases
    public const string Template = "template"; // Templates

    /// <summary>
    /// Get all available categories
    /// </summary>
    public static string[] GetAll() => new[] { Calc, Ux, Data, Docs, Benchmark, Template };
}
