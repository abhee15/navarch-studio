namespace Shared.Attributes;

/// <summary>
/// Marks a property as convertible between unit systems
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class ConvertibleAttribute : Attribute
{
    /// <summary>
    /// The type of physical quantity (Length, Mass, Area, Volume, etc.)
    /// </summary>
    public string QuantityType { get; }

    public ConvertibleAttribute(string quantityType)
    {
        QuantityType = quantityType;
    }
}

