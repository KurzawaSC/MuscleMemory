namespace MuscleMemory.Tests.Testing;

public sealed record TypeDependency(Type Owner, string Member, Type Target)
{
    public override string ToString() => $"{Owner.FullName} {Member} -> {Target.FullName}";
}
