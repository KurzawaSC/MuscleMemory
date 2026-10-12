using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using MuscleMemory.Tests.Testing;

namespace MuscleMemory.Tests.Architecture;

public sealed partial class MauiProgramTests
{
    private const string ServiceGroup = "service";

    private static readonly string[] AndroidMauiProgramPath = ["MuscleMemory", "Platforms", "Android", "MauiProgram.cs"];

    [Fact]
    public void CreateMauiApp_EveryRegisteredAppType_HasEveryConstructorParameterRegistered()
    {
        using var app = CreateMauiApp();
        var container = app.Services.GetRequiredService<IServiceProviderIsService>();
        var androidOnly = AndroidRegistrations().ToHashSet();
        bool IsRegistered(Type type) => container.IsService(type) || androidOnly.Contains(type);

        var registered = TypeDependencies.DeclaredAppTypes.Where(container.IsService).ToList();
        var violations = registered.SelectMany(service => UnregisteredParameters(service, IsRegistered));

        Assert.NotEmpty(registered);
        Violations.AssertNone(violations);
    }

    private static MauiApp CreateMauiApp() =>
        (MauiApp)typeof(MauiProgram).GetMethod(nameof(MauiProgram.CreateMauiApp))!.Invoke(null, null)!;

    private static IEnumerable<string> UnregisteredParameters(Type service, Func<Type, bool> isRegistered)
    {
        var implementations = Implementations(service).ToList();
        if (implementations is not [var implementation])
        {
            return [$"{service.FullName} has {implementations.Count} implementations"];
        }

        var constructor = implementation.GetConstructors().MaxBy(candidate => candidate.GetParameters().Length);
        if (constructor is null)
        {
            return [$"{implementation.FullName} has no public constructor"];
        }

        return constructor.GetParameters()
            .Where(parameter => !isRegistered(parameter.ParameterType))
            .Select(parameter => $"{implementation.FullName} constructor parameter {parameter.Name}: {parameter.ParameterType.FullName} is not registered");
    }

    private static IEnumerable<Type> Implementations(Type service) => service.IsInterface
        ? TypeDependencies.DeclaredAppTypes.Where(type => type is { IsClass: true, IsAbstract: false } && service.IsAssignableFrom(type))
        : [service];

    private static IEnumerable<Type> AndroidRegistrations() =>
        Registration().Matches(File.ReadAllText(RepositoryFiles.PathOf(AndroidMauiProgramPath)))
            .Select(match => match.Groups[ServiceGroup].Value)
            .Select(name => Assert.Single(TypeDependencies.AppTypes, type => type.Name == name));

    [GeneratedRegex(@"Services\.Add(?:Singleton|Transient|Scoped)<(?<service>\w+)")]
    private static partial Regex Registration();
}
