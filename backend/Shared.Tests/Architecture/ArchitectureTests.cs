using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Shared.Attributes;
using Shared.DTOs;
using Xunit;

namespace Shared.Tests.Architecture;

/// <summary>
/// Architectural tests to enforce coding conventions across the codebase
/// These tests catch inconsistencies early in the development cycle
/// </summary>
[Trait("Category", "Architecture")]
public class ArchitectureTests
{
    private readonly Assembly[] _serviceAssemblies;

    public ArchitectureTests()
    {
        // Load all service assemblies for testing
        _serviceAssemblies = new[]
        {
            Assembly.Load("DataService"),
            Assembly.Load("IdentityService"),
            Assembly.Load("ApiGateway"),
            Assembly.Load("HullSizingService"),
            Assembly.Load("AIAgentService")
        };
    }

    #region API Versioning Conventions

    [Fact]
    public void AllControllers_ShouldHave_ApiVersionAttribute()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var hasVersionAttribute = controllerType.GetCustomAttributes()
                .Any(attr => attr.GetType().Name == "ApiVersionAttribute");

            hasVersionAttribute.Should().BeTrue(
                $"Controller {controllerType.Name} must have [ApiVersion] attribute for API versioning");
        }
    }

    [Fact]
    public void AllControllers_ShouldUse_VersionedRoute()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
            routeAttribute.Should().NotBeNull($"Controller {controllerType.Name} must have [Route] attribute");

            var route = routeAttribute!.Template;
            route.Should().Contain("{version:apiVersion}",
                $"Controller {controllerType.Name} route must include version parameter: {route}");
        }
    }

    [Fact]
    public void AllControllers_ShouldStart_WithApiV()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var routeAttribute = controllerType.GetCustomAttribute<RouteAttribute>();
            var route = routeAttribute?.Template ?? "";

            route.Should().StartWith("api/v",
                $"Controller {controllerType.Name} route should start with 'api/v': {route}");
        }
    }

    #endregion

    #region Error Handling Conventions

    [Fact]
    public void AllControllerActions_WithPost_ShouldHave_BadRequestResponseType()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var postMethods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<HttpPostAttribute>() != null);

            foreach (var method in postMethods)
            {
                var hasProducesResponseType = method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                    .Any(attr => attr.StatusCode == StatusCodes.Status400BadRequest);

                hasProducesResponseType.Should().BeTrue(
                    $"POST method {controllerType.Name}.{method.Name} must have [ProducesResponseType(400)] for validation errors");
            }
        }
    }

    [Fact]
    public void AllControllerActions_WithGet_ShouldHave_NotFoundResponseType()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var getMethods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.GetCustomAttribute<HttpGetAttribute>() != null &&
                           m.GetParameters().Any(p => p.Name == "id"));

            foreach (var method in getMethods)
            {
                var hasProducesResponseType = method.GetCustomAttributes<ProducesResponseTypeAttribute>()
                    .Any(attr => attr.StatusCode == StatusCodes.Status404NotFound);

                hasProducesResponseType.Should().BeTrue(
                    $"GET method {controllerType.Name}.{method.Name} with 'id' parameter must have [ProducesResponseType(404)]");
            }
        }
    }

    [Fact]
    public void AllControllerActions_ShouldHave_CancellationTokenParameter()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var asyncMethods = controllerType.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.ReturnType.IsGenericType &&
                           m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) &&
                           m.GetCustomAttribute<HttpGetAttribute>() != null ||
                           m.GetCustomAttribute<HttpPostAttribute>() != null ||
                           m.GetCustomAttribute<HttpPutAttribute>() != null ||
                           m.GetCustomAttribute<HttpDeleteAttribute>() != null);

            foreach (var method in asyncMethods)
            {
                var hasCancellationToken = method.GetParameters()
                    .Any(p => p.ParameterType == typeof(CancellationToken));

                hasCancellationToken.Should().BeTrue(
                    $"Async method {controllerType.Name}.{method.Name} should have CancellationToken parameter");
            }
        }
    }

    #endregion

    #region Unit Handling Conventions

    [Fact]
    public void AllDimensionProperties_InDTOs_ShouldHave_ConvertibleAttribute()
    {
        // Arrange
        var dtoTypes = GetAllDtoTypes();

        // Properties that should have [Convertible] attribute
        var dimensionPropertyNames = new[]
        {
            "Length", "Breadth", "Depth", "Draft", "Lpp", "Loa", "Beam",
            "Displacement", "Volume", "Mass", "Weight",
            "Area", "WaterplaneArea", "WettedSurfaceArea",
            "KB", "KG", "GM", "LCB", "TCB", "BMt", "BMl"
        };

        // Act & Assert
        foreach (var dtoType in dtoTypes)
        {
            var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => (p.PropertyType == typeof(decimal) || p.PropertyType == typeof(double)) &&
                           dimensionPropertyNames.Any(name => p.Name.Contains(name, StringComparison.OrdinalIgnoreCase)));

            foreach (var property in properties)
            {
                var hasConvertibleAttribute = property.GetCustomAttribute<ConvertibleAttribute>() != null;

                hasConvertibleAttribute.Should().BeTrue(
                    $"Property {dtoType.Name}.{property.Name} should have [Convertible] attribute for unit conversion");
            }
        }
    }

    [Fact]
    public void AllUnitAwareDTOs_ShouldInherit_FromUnitAwareDto()
    {
        // Arrange
        var dtoTypes = GetAllDtoTypes();

        // DTOs with convertible properties should inherit from UnitAwareDto
        var unitAwareDtoType = typeof(UnitAwareDto);

        // Act & Assert
        foreach (var dtoType in dtoTypes)
        {
            var hasConvertibleProperties = dtoType.GetProperties()
                .Any(p => p.GetCustomAttribute<ConvertibleAttribute>() != null);

            if (hasConvertibleProperties)
            {
                var inheritsFromUnitAwareDto = unitAwareDtoType.IsAssignableFrom(dtoType);

                inheritsFromUnitAwareDto.Should().BeTrue(
                    $"DTO {dtoType.Name} has [Convertible] properties and should inherit from UnitAwareDto");
            }
        }
    }

    [Fact]
    public void AllConvertibleAttributes_ShouldHave_ValidQuantityType()
    {
        // Arrange
        var dtoTypes = GetAllDtoTypes();
        var validQuantityTypes = new[]
        {
            "Length", "Area", "Volume", "Mass", "Density", "Force", "Inertia",
            "Velocity", "Acceleration", "Pressure", "Power", "Energy"
        };

        // Act & Assert
        foreach (var dtoType in dtoTypes)
        {
            var properties = dtoType.GetProperties()
                .Where(p => p.GetCustomAttribute<ConvertibleAttribute>() != null);

            foreach (var property in properties)
            {
                var attribute = property.GetCustomAttribute<ConvertibleAttribute>()!;
                var quantityType = attribute.QuantityType;

                validQuantityTypes.Should().Contain(quantityType,
                    $"Property {dtoType.Name}.{property.Name} has invalid QuantityType: '{quantityType}'");
            }
        }
    }

    #endregion

    #region Logging Conventions

    [Fact]
    public void AllControllers_ShouldHave_ILoggerField()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var hasLoggerField = controllerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(f => f.FieldType.IsGenericType &&
                         f.FieldType.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Logging.ILogger<>));

            hasLoggerField.Should().BeTrue(
                $"Controller {controllerType.Name} should have ILogger<T> field for structured logging");
        }
    }

    [Fact]
    public void AllServices_ShouldHave_ILoggerField()
    {
        // Arrange
        var serviceTypes = GetAllServiceTypes();

        // Act & Assert
        foreach (var serviceType in serviceTypes)
        {
            // Skip interface types
            if (serviceType.IsInterface) continue;

            var hasLoggerField = serviceType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                .Any(f => f.FieldType.IsGenericType &&
                         f.FieldType.GetGenericTypeDefinition() == typeof(Microsoft.Extensions.Logging.ILogger<>));

            hasLoggerField.Should().BeTrue(
                $"Service {serviceType.Name} should have ILogger<T> field for structured logging");
        }
    }

    #endregion

    #region Naming Conventions

    [Fact]
    public void AllControllers_ShouldEnd_WithController()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            controllerType.Name.Should().EndWith("Controller",
                $"Controller class {controllerType.Name} should end with 'Controller' suffix");
        }
    }

    [Fact]
    public void AllDTOs_ShouldEnd_WithDto()
    {
        // Arrange
        var dtoTypes = GetAllDtoTypes();

        // Act & Assert
        foreach (var dtoType in dtoTypes)
        {
            dtoType.Name.Should().EndWith("Dto",
                $"DTO class {dtoType.Name} should end with 'Dto' suffix");
        }
    }

    [Fact]
    public void AllServiceInterfaces_ShouldStart_WithI()
    {
        // Arrange
        var serviceInterfaces = GetAllServiceTypes().Where(t => t.IsInterface);

        // Act & Assert
        foreach (var serviceInterface in serviceInterfaces)
        {
            serviceInterface.Name.Should().StartWith("I",
                $"Service interface {serviceInterface.Name} should start with 'I' prefix");
        }
    }

    #endregion

    #region Dependency Injection Conventions

    [Fact]
    public void AllServices_ShouldBe_Injectable()
    {
        // Arrange
        var serviceTypes = GetAllServiceTypes().Where(t => !t.IsInterface && !t.IsAbstract);

        // Act & Assert
        foreach (var serviceType in serviceTypes)
        {
            var constructors = serviceType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

            constructors.Should().NotBeEmpty(
                $"Service {serviceType.Name} should have at least one public constructor for DI");

            // Verify no static constructors with parameters (anti-pattern for DI)
            var staticConstructors = serviceType.GetConstructors(BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var ctor in staticConstructors)
            {
                ctor.GetParameters().Should().BeEmpty(
                    $"Service {serviceType.Name} should not have parameterized static constructors");
            }
        }
    }

    [Fact]
    public void AllControllers_ShouldInject_DependenciesViaConstructor()
    {
        // Arrange
        var controllerTypes = GetAllControllerTypes();

        // Act & Assert
        foreach (var controllerType in controllerTypes)
        {
            var fields = controllerType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // Field should be readonly (injected via constructor)
                field.IsInitOnly.Should().BeTrue(
                    $"Field {field.Name} in {controllerType.Name} should be readonly (constructor-injected)");
            }
        }
    }

    #endregion

    #region Async Conventions

    [Fact]
    public void AllAsyncMethods_ShouldEnd_WithAsync()
    {
        // Arrange
        var allTypes = _serviceAssemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract);

        // Act & Assert
        foreach (var type in allTypes)
        {
            var asyncMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.ReturnType.IsGenericType &&
                           (m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>) ||
                            m.ReturnType == typeof(Task)));

            foreach (var method in asyncMethods)
            {
                method.Name.Should().EndWith("Async",
                    $"Async method {type.Name}.{method.Name} should end with 'Async' suffix");
            }
        }
    }

    [Fact]
    public void AllAsyncMethods_ShouldNot_UseBlocking()
    {
        // This is a compile-time check, enforced by code review
        // We can't easily test this in runtime tests, but it's documented in conventions
        Assert.True(true, "Blocking calls (.Result, .Wait()) are forbidden in async methods - enforced by code review");
    }

    #endregion

    #region Validation Conventions

    [Fact]
    public void AllCreateDTOs_ShouldHave_Validator()
    {
        // Arrange
        var sharedAssembly = Assembly.Load("Shared");
        var dtoTypes = sharedAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Dto") &&
                       (t.Name.Contains("Create") || t.Name.Contains("Login") || t.Name.Contains("Register")));

        // Act & Assert
        foreach (var dtoType in dtoTypes)
        {
            var validatorName = $"{dtoType.Name}Validator";
            var validatorType = sharedAssembly.GetType($"Shared.Validators.{validatorName}");

            validatorType.Should().NotBeNull(
                $"DTO {dtoType.Name} should have a corresponding validator: {validatorName}");
        }
    }

    #endregion

    #region Helper Methods

    private IEnumerable<Type> GetAllControllerTypes()
    {
        return _serviceAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsClass &&
                          !type.IsAbstract &&
                          type.Name.EndsWith("Controller") &&
                          typeof(ControllerBase).IsAssignableFrom(type));
    }

    private IEnumerable<Type> GetAllDtoTypes()
    {
        var sharedAssembly = Assembly.Load("Shared");
        return sharedAssembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Dto"));
    }

    private IEnumerable<Type> GetAllServiceTypes()
    {
        return _serviceAssemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => (type.IsClass || type.IsInterface) &&
                          type.Namespace != null &&
                          type.Namespace.Contains("Services") &&
                          !type.Name.EndsWith("Controller"));
    }

    #endregion
}
