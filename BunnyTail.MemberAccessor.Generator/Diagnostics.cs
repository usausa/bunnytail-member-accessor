namespace BunnyTail.MemberAccessor.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidTypeArgument { get; } = new(
        id: "BTMA0001",
        title: "Invalid type argument",
        messageFormat: "Type must be generic type. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidAttributeLocation { get; } = new(
        id: "BTMA0002",
        title: "Invalid attribute location",
        messageFormat: "Attribute must be in the same location as the target type. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NoAccessibleMembers { get; } = new(
        id: "BTMA0003",
        title: "No accessible members",
        messageFormat: "Type has no accessible properties. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypedAccessorTargetNotDecorated { get; } = new(
        id: "BTMA0004",
        title: "TypedAccessor target not decorated",
        messageFormat: "The target type of [TypedAccessor] does not have [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedConstructorArity { get; } = new(
        id: "BTMA0005",
        title: "Unsupported constructor arity",
        messageFormat: "Type has a constructor with more than {1} parameters, which is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
