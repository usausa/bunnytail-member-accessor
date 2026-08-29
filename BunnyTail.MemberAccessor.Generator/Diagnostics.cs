namespace BunnyTail.MemberAccessor.Generator;

using Microsoft.CodeAnalysis;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidTypeArgument { get; } = new(
        id: "BTMA0001",
        title: "Invalid type argument",
        messageFormat: "Type must be a generic type. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidAttributeLocation { get; } = new(
        id: "BTMA0002",
        title: "Invalid attribute location",
        messageFormat: "Attribute is in a different location. type=[{0}]",
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
        messageFormat: "Target type has no [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedConstructorArity { get; } = new(
        id: "BTMA0005",
        title: "Unsupported constructor arity",
        messageFormat: "Constructor has more than {1} parameters. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor TypeNotPartial { get; } = new(
        id: "BTMA0006",
        title: "Type is not partial",
        messageFormat: "IAccessorProvider is not generated. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor InvalidExternalTarget { get; } = new(
        id: "BTMA0007",
        title: "Invalid target type",
        messageFormat: "[GenerateAccessorFor] target type is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AccessorAlreadyGenerated { get; } = new(
        id: "BTMA0008",
        title: "Accessor already generated",
        messageFormat: "Target type already has [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor GenericUnsafeAccessorNotSupported { get; } = new(
        id: "BTMA0009",
        title: "Generic UnsafeAccessor not supported",
        messageFormat: "Non-public member access needs .NET 9. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
