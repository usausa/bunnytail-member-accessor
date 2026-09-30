namespace BunnyTail.MemberAccessor.Generator;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal static class Diagnostics
{
    public static DiagnosticDescriptor InvalidTypeArgument { get; } = new(
        id: "BTMA0001",
        title: "Invalid type argument",
        messageFormat: "Type must be a generic type. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidAttributeLocation { get; } = new(
        id: "BTMA0002",
        title: "Invalid attribute location",
        messageFormat: "Attribute is in a different location. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NoAccessibleMembers { get; } = new(
        id: "BTMA0003",
        title: "No accessible members",
        messageFormat: "Type has no accessible members or constructors. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor TypedAccessorTargetNotDecorated { get; } = new(
        id: "BTMA0004",
        title: "TypedAccessor target not decorated",
        messageFormat: "Target type has no [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor UnsupportedConstructorArity { get; } = new(
        id: "BTMA0005",
        title: "Unsupported constructor arity",
        messageFormat: "Constructor has more than {1} parameters. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

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
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

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
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NestedGenericTypeNotSupported { get; } = new(
        id: "BTMA0010",
        title: "Nested generic type not supported",
        messageFormat: "Nested type or its containing type is generic. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NestedTypeNotAccessible { get; } = new(
        id: "BTMA0011",
        title: "Nested type not accessible",
        messageFormat: "Type is not accessible from the generated code. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor UnsupportedType { get; } = new(
        id: "BTMA0012",
        title: "Unsupported type",
        messageFormat: "Static class or ref struct cannot have an accessor. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor HintNameCollision { get; } = new(
        id: "BTMA0013",
        title: "Type name differs only in case",
        messageFormat: "Type name differs only in case from another type, and its accessor is not generated. type=[{0}], other=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);
}
