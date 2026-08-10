namespace BunnyTail.MemberAccessor.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidTypeArgument { get; } = new(
        id: "BTMA0001",
        title: "Invalid type argument",
        messageFormat: "Type must be generic type. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidAttributeLocation { get; } = new(
        id: "BTMA0002",
        title: "Invalid attribute location",
        messageFormat: "Attribute must be in the same location as the target type. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NoAccessibleMembers { get; } = new(
        id: "BTMA0003",
        title: "No accessible members",
        messageFormat: "Type has no accessible members. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypedAccessorTargetNotDecorated { get; } = new(
        id: "BTMA0004",
        title: "TypedAccessor target not decorated",
        messageFormat: "The target type of [TypedAccessor] does not have [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedConstructorArity { get; } = new(
        id: "BTMA0005",
        title: "Unsupported constructor arity",
        messageFormat: "Type has a constructor with more than {1} parameters, which is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypeNotPartial { get; } = new(
        id: "BTMA0006",
        title: "Type is not partial",
        messageFormat: "Type is not partial, so the IAccessorProvider implementation is not generated. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidExternalTarget { get; } = new(
        id: "BTMA0007",
        title: "Invalid target type",
        messageFormat: "The target type of [GenerateAccessorFor] is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AccessorAlreadyGenerated { get; } = new(
        id: "BTMA0008",
        title: "Accessor already generated",
        messageFormat: "Accessor classes are already generated for the target type by [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor GenericUnsafeAccessorNotSupported { get; } = new(
        id: "BTMA0009",
        title: "Non-public member access on generic type requires .NET 9 or later",
        messageFormat: "Non-public member access on a generic type uses UnsafeAccessor with generic parameters, which requires .NET 9 or later at runtime. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
