namespace Shared.Constants;

/// <summary>
/// Constants for template vessels used across the application
/// Template vessels are read-only examples available to all users
/// </summary>
public static class TemplateVessels
{
    /// <summary>
    /// System user ID used to identify template vessels
    /// Template vessels have this UserId to distinguish them from user-created vessels
    /// </summary>
    public static readonly Guid SystemUserId = Guid.Empty; // 00000000-0000-0000-0000-000000000000

    /// <summary>
    /// Fixed GUID for the Hydrostatics template vessel
    /// This vessel demonstrates complete hydrostatic analysis capabilities
    /// </summary>
    public static readonly Guid HydrostaticsVesselId = new("00000000-0000-0000-0000-000000000001");

    /// <summary>
    /// Checks if a vessel ID is a known template vessel
    /// </summary>
    public static bool IsTemplateVesselId(Guid vesselId)
    {
        return vesselId == HydrostaticsVesselId;
        // Future templates can be added here:
        // || vesselId == NextWorkflowVesselId;
    }

    /// <summary>
    /// Checks if a user ID represents the system (for template vessels)
    /// </summary>
    public static bool IsSystemUserId(Guid userId)
    {
        return userId == SystemUserId;
    }
}
