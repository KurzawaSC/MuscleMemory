using MuscleMemory.Data;
using MuscleMemory.Data.Repositories;
using MuscleMemory.Services;
using MuscleMemory.Tests.Testing;
using MuscleMemory.ViewModels;

namespace MuscleMemory.Tests.Architecture;

public sealed class LayeringTests
{
    private const string RepositorySuffix = "Repository";

    private static readonly string ViewModelsNamespace = typeof(ActiveWorkoutViewModel).Namespace!;
    private static readonly string ServicesNamespace = typeof(IErrorHandler).Namespace!;
    private static readonly string DataNamespace = typeof(DatabaseContext).Namespace!;
    private static readonly string RepositoriesNamespace = typeof(IWorkoutRepository).Namespace!;
    private static readonly Type CompositionRoot = typeof(MauiProgram);

    [Fact]
    public void ViewModels_ConstructorParametersFieldsAndProperties_UseNothingFromTheDataLayer()
    {
        var violations = TypeDependencies.DeclaredAppTypes
            .Where(type => TypeDependencies.IsInNamespace(type, ViewModelsNamespace))
            .SelectMany(TypeDependencies.OfMembers)
            .Where(dependency => TypeDependencies.IsInNamespace(dependency.Target, DataNamespace))
            .Select(dependency => dependency.ToString());

        Violations.AssertNone(violations);
    }

    [Fact]
    public void RepositoryTypes_AnyReference_ComesOnlyFromServicesDataOrTheCompositionRoot()
    {
        var violations = TypeDependencies.AppTypes
            .Where(type => !IsAllowedToUseRepositories(type))
            .SelectMany(TypeDependencies.OfEverything)
            .Where(dependency => IsRepository(dependency.Target))
            .Select(dependency => dependency.ToString());

        Violations.AssertNone(violations);
    }

    private static bool IsAllowedToUseRepositories(Type type) =>
        TypeDependencies.IsInNamespace(type, ServicesNamespace)
        || TypeDependencies.IsInNamespace(type, DataNamespace)
        || Outermost(type) == CompositionRoot;

    private static bool IsRepository(Type type) =>
        type.Namespace == RepositoriesNamespace && type.Name.EndsWith(RepositorySuffix, StringComparison.Ordinal);

    private static Type Outermost(Type type) => type.DeclaringType is { } outer ? Outermost(outer) : type;
}
