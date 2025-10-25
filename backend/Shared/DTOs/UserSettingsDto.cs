namespace Shared.DTOs;

/// <summary>
/// DTO for user settings
/// </summary>
public record UserSettingsDto
{
    public string PreferredUnits { get; init; } = "SI";
}

/// <summary>
/// DTO for updating user settings
/// </summary>
public record UpdateUserSettingsDto
{
    public string PreferredUnits { get; init; } = "SI";
}

