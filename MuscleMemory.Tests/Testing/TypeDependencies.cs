using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace MuscleMemory.Tests.Testing;

public static class TypeDependencies
{
    private const BindingFlags DeclaredMembers =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
    private const byte TwoByteOpCodePrefix = 0xFE;
    private const int TwoByteOpCodeBase = 0xFE00;
    private const int TokenSize = sizeof(int);
    private const int BranchOffsetSize = sizeof(int);

    private static readonly Dictionary<short, OpCode> OpCodesByValue = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (OpCode)field.GetValue(null)!)
        .ToDictionary(opCode => opCode.Value);

    public static IReadOnlyList<Type> AppTypes { get; } = typeof(MauiProgram).Assembly.GetTypes();

    public static IEnumerable<Type> DeclaredAppTypes => AppTypes.Where(type => !IsCompilerGenerated(type));

    public static bool IsInNamespace(Type type, string ns) =>
        type.Namespace is { } typeNamespace && (typeNamespace == ns || typeNamespace.StartsWith(ns + Type.Delimiter, StringComparison.Ordinal));

    public static IEnumerable<TypeDependency> OfMembers(Type type) =>
        ConstructorParameters(type).Concat(Fields(type)).Concat(Properties(type));

    public static IEnumerable<TypeDependency> OfEverything(Type type) =>
        OfMembers(type).Concat(Inheritance(type)).Concat(MethodSignatures(type)).Concat(MethodBodies(type));

    private static bool IsCompilerGenerated(Type type) =>
        type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
        || (type.DeclaringType is { } outer && IsCompilerGenerated(outer));

    private static IEnumerable<TypeDependency> ConstructorParameters(Type type) =>
        from constructor in type.GetConstructors(DeclaredMembers)
        from parameter in constructor.GetParameters()
        from target in Expand(parameter.ParameterType)
        select new TypeDependency(type, $"constructor parameter {parameter.Name}", target);

    private static IEnumerable<TypeDependency> Fields(Type type) =>
        from field in type.GetFields(DeclaredMembers)
        from target in Expand(field.FieldType)
        select new TypeDependency(type, $"field {field.Name}", target);

    private static IEnumerable<TypeDependency> Properties(Type type) =>
        from property in type.GetProperties(DeclaredMembers)
        from target in Expand(property.PropertyType)
        select new TypeDependency(type, $"property {property.Name}", target);

    private static IEnumerable<TypeDependency> Inheritance(Type type) =>
        from inherited in type.GetInterfaces().Append(type.BaseType).OfType<Type>()
        from target in Expand(inherited)
        select new TypeDependency(type, "base type", target);

    private static IEnumerable<TypeDependency> MethodSignatures(Type type) =>
        from method in type.GetMethods(DeclaredMembers)
        from signatureType in method.GetParameters().Select(parameter => parameter.ParameterType).Append(method.ReturnType)
        from target in Expand(signatureType)
        select new TypeDependency(type, $"method {method.Name} signature", target);

    private static IEnumerable<TypeDependency> MethodBodies(Type type) =>
        from method in type.GetMethods(DeclaredMembers).Cast<MethodBase>().Concat(type.GetConstructors(DeclaredMembers))
        from bodyType in BodyTypes(method)
        from target in Expand(bodyType)
        select new TypeDependency(type, $"method {method.Name} body", target);

    private static IEnumerable<Type> BodyTypes(MethodBase method) =>
        method.GetMethodBody() is { } body
            ? body.LocalVariables.Select(local => local.LocalType).Concat(TokenTypes(method, body.GetILAsByteArray() ?? []))
            : [];

    private static IEnumerable<Type> TokenTypes(MethodBase method, byte[] il)
    {
        var typeArguments = method.DeclaringType is { IsGenericType: true } declaring ? declaring.GetGenericArguments() : null;
        var methodArguments = method is MethodInfo { IsGenericMethod: true } generic ? generic.GetGenericArguments() : null;
        for (var offset = 0; offset < il.Length;)
        {
            var opCode = ReadOpCode(il, ref offset);
            if (opCode.OperandType is OperandType.InlineType or OperandType.InlineTok or OperandType.InlineMethod or OperandType.InlineField)
            {
                var member = method.Module.ResolveMember(BitConverter.ToInt32(il, offset), typeArguments, methodArguments);
                foreach (var memberType in MemberTypes(member))
                {
                    yield return memberType;
                }
            }

            offset += OperandSize(opCode.OperandType, il, offset);
        }
    }

    private static OpCode ReadOpCode(byte[] il, ref int offset)
    {
        var first = il[offset++];
        var value = first == TwoByteOpCodePrefix ? unchecked((short)(TwoByteOpCodeBase | il[offset++])) : first;
        return OpCodesByValue[value];
    }

    private static int OperandSize(OperandType operandType, byte[] il, int offset) => operandType switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => sizeof(byte),
        OperandType.InlineVar => sizeof(short),
        OperandType.InlineI8 or OperandType.InlineR => sizeof(long),
        OperandType.InlineSwitch => TokenSize + (BitConverter.ToInt32(il, offset) * BranchOffsetSize),
        _ => TokenSize
    };

    private static IEnumerable<Type> MemberTypes(MemberInfo? member) => member switch
    {
        Type type => [type],
        FieldInfo field => [field.DeclaringType!, field.FieldType],
        MethodInfo method => [method.DeclaringType!, method.ReturnType, .. ParameterTypes(method)],
        ConstructorInfo constructor => [constructor.DeclaringType!, .. ParameterTypes(constructor)],
        _ => []
    };

    private static IEnumerable<Type> ParameterTypes(MethodBase method) =>
        method.GetParameters().Select(parameter => parameter.ParameterType);

    private static IEnumerable<Type> Expand(Type type)
    {
        if (type.IsGenericParameter)
        {
            return [];
        }

        if (type.HasElementType)
        {
            return Expand(type.GetElementType()!);
        }

        return type.IsGenericType
            ? type.GetGenericArguments().SelectMany(Expand).Prepend(type)
            : [type];
    }
}
