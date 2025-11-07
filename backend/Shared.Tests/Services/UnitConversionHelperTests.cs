using NavArch.UnitConversion.Services;
using Shared.Attributes;
using Shared.DTOs;
using Shared.Services;

namespace Shared.Tests.Services;

public class UnitConversionHelperTests
{
    private readonly Mock<IUnitConverter> _mockConverter;

    public UnitConversionHelperTests()
    {
        _mockConverter = new Mock<IUnitConverter>();

        // Setup default conversions
        _mockConverter
            .Setup(c => c.Convert(It.IsAny<decimal>(), "SI", "Imperial", It.IsAny<string>()))
            .Returns((decimal value, string from, string to, string qty) => value * 3.28084m);

        _mockConverter
            .Setup(c => c.Convert(It.IsAny<decimal>(), "Imperial", "SI", It.IsAny<string>()))
            .Returns((decimal value, string from, string to, string qty) => value / 3.28084m);
    }

    [Fact]
    public void ConvertToSI_WithNullUnits_ShouldDefaultToSI()
    {
        // Arrange
        var dto = new TestDto { Length = 100m, Units = null };

        // Act
        UnitConversionHelper.ConvertToSI(dto, _mockConverter.Object);

        // Assert
        dto.Units.Should().Be("SI", "null Units should default to SI");
        dto.Length.Should().Be(100m, "no conversion should occur");
        _mockConverter.Verify(
            c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never,
            "no conversion should be attempted");
    }

    [Fact]
    public void ConvertToSI_WithEmptyUnits_ShouldDefaultToSI()
    {
        // Arrange
        var dto = new TestDto { Length = 100m, Units = "" };

        // Act
        UnitConversionHelper.ConvertToSI(dto, _mockConverter.Object);

        // Assert
        dto.Units.Should().Be("SI", "empty Units should default to SI");
        dto.Length.Should().Be(100m, "no conversion should occur");
        _mockConverter.Verify(
            c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void ConvertToSI_WhenAlreadyInSI_ShouldNotConvert()
    {
        // Arrange
        var dto = new TestDto { Length = 100m, Units = "SI" };

        // Act
        UnitConversionHelper.ConvertToSI(dto, _mockConverter.Object);

        // Assert
        dto.Units.Should().Be("SI");
        dto.Length.Should().Be(100m, "no conversion should occur when already in SI");
        _mockConverter.Verify(
            c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void ConvertToSI_FromImperial_ShouldConvert()
    {
        // Arrange
        var dto = new TestDto { Length = 328.084m, Units = "Imperial" };

        // Act
        UnitConversionHelper.ConvertToSI(dto, _mockConverter.Object);

        // Assert
        dto.Units.Should().Be("SI");
        dto.Length.Should().BeApproximately(100m, 0.01m);
        _mockConverter.Verify(
            c => c.Convert(328.084m, "Imperial", "SI", "Length"),
            Times.Once);
    }

    [Fact]
    public void ConvertFromSI_ToImperial_ShouldConvert()
    {
        // Arrange
        var dto = new TestDto { Length = 100m, Units = "SI" };

        // Act
        UnitConversionHelper.ConvertFromSI(dto, "Imperial", _mockConverter.Object);

        // Assert
        dto.Units.Should().Be("Imperial");
        dto.Length.Should().BeApproximately(328.084m, 0.01m);
        _mockConverter.Verify(
            c => c.Convert(100m, "SI", "Imperial", "Length"),
            Times.Once);
    }

    [Fact]
    public void ConvertFromSI_ToSI_ShouldNotConvert()
    {
        // Arrange
        var dto = new TestDto { Length = 100m, Units = "SI" };

        // Act
        UnitConversionHelper.ConvertFromSI(dto, "SI", _mockConverter.Object);

        // Assert
        dto.Units.Should().Be("SI");
        dto.Length.Should().Be(100m);
        _mockConverter.Verify(
            c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void ConvertDto_WithNullableProperty_ShouldConvertWhenHasValue()
    {
        // Arrange
        var dto = new TestDtoWithNullable
        {
            RequiredLength = 100m,
            OptionalLength = 50m,
            Units = "SI"
        };

        // Act
        UnitConversionHelper.ConvertFromSI(dto, "Imperial", _mockConverter.Object);

        // Assert
        dto.RequiredLength.Should().BeApproximately(328.084m, 0.01m);
        dto.OptionalLength.Should().BeApproximately(164.042m, 0.01m);
        _mockConverter.Verify(
            c => c.Convert(100m, "SI", "Imperial", "Length"),
            Times.Once);
        _mockConverter.Verify(
            c => c.Convert(50m, "SI", "Imperial", "Length"),
            Times.Once);
    }

    [Fact]
    public void ConvertDto_WithNullablePropertyNull_ShouldSkipConversion()
    {
        // Arrange
        var dto = new TestDtoWithNullable
        {
            RequiredLength = 100m,
            OptionalLength = null,
            Units = "SI"
        };

        // Act
        UnitConversionHelper.ConvertFromSI(dto, "Imperial", _mockConverter.Object);

        // Assert
        dto.RequiredLength.Should().BeApproximately(328.084m, 0.01m);
        dto.OptionalLength.Should().BeNull("null values should remain null");
        _mockConverter.Verify(
            c => c.Convert(100m, "SI", "Imperial", "Length"),
            Times.Once);
        // Should not attempt to convert null value
    }

    [Fact]
    public void ConvertDto_WithMultipleProperties_ShouldConvertAll()
    {
        // Arrange
        var dto = new TestDtoMultiple
        {
            Length = 100m,
            Width = 50m,
            Height = 25m,
            Units = "SI"
        };

        // Act
        UnitConversionHelper.ConvertFromSI(dto, "Imperial", _mockConverter.Object);

        // Assert
        dto.Length.Should().BeApproximately(328.084m, 0.01m);
        dto.Width.Should().BeApproximately(164.042m, 0.01m);
        dto.Height.Should().BeApproximately(82.021m, 0.01m);
        dto.Units.Should().Be("Imperial");
    }

    [Fact]
    public void ConvertDto_WithNonConvertibleProperty_ShouldSkip()
    {
        // Arrange
        var dto = new TestDtoWithNonConvertible
        {
            ConvertibleValue = 100m,
            NonConvertibleValue = 999m,
            Units = "SI"
        };

        // Act
        UnitConversionHelper.ConvertFromSI(dto, "Imperial", _mockConverter.Object);

        // Assert
        dto.ConvertibleValue.Should().BeApproximately(328.084m, 0.01m, "convertible property should be converted");
        dto.NonConvertibleValue.Should().Be(999m, "non-convertible property should remain unchanged");
    }

    [Fact]
    public void ConvertDto_SameSourceAndTarget_ShouldNotConvert()
    {
        // Arrange
        var dto = new TestDto { Length = 100m, Units = "SI" };

        // Act
        UnitConversionHelper.ConvertDto(dto, "SI", "SI", _mockConverter.Object);

        // Assert
        dto.Length.Should().Be(100m);
        _mockConverter.Verify(
            c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public void ConvertDto_WithConverterException_ShouldThrow()
    {
        // Arrange
        var dto = new TestDto { Length = 100m, Units = "SI" };
        _mockConverter
            .Setup(c => c.Convert(It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new InvalidOperationException("Invalid unit system"));

        // Act & Assert
        var act = () => UnitConversionHelper.ConvertFromSI(dto, "Invalid", _mockConverter.Object);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Invalid unit system");
    }

    // Test DTOs
    private class TestDto : UnitAwareDto
    {
        [Convertible("Length")]
        public decimal Length { get; set; }
    }

    private class TestDtoWithNullable : UnitAwareDto
    {
        [Convertible("Length")]
        public decimal RequiredLength { get; set; }

        [Convertible("Length")]
        public decimal? OptionalLength { get; set; }
    }

    private class TestDtoMultiple : UnitAwareDto
    {
        [Convertible("Length")]
        public decimal Length { get; set; }

        [Convertible("Length")]
        public decimal Width { get; set; }

        [Convertible("Length")]
        public decimal Height { get; set; }
    }

    private class TestDtoWithNonConvertible : UnitAwareDto
    {
        [Convertible("Length")]
        public decimal ConvertibleValue { get; set; }

        // No Convertible attribute
        public decimal NonConvertibleValue { get; set; }
    }
}
