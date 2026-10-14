namespace BunnyTail.MemberAccessor.Generator;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper;

internal static class Diagnostics
{
    // GenerateAccessor (00xx)

    public static DiagnosticDescriptor UnsupportedType { get; } = new(
        id: "BTMA0001",
        title: "Unsupported type",
        messageFormat: "Static class or ref struct cannot have an accessor. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NestedGenericTypeNotSupported { get; } = new(
        id: "BTMA0002",
        title: "Nested generic type not supported",
        messageFormat: "Nested type or its containing type is generic. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NestedTypeNotAccessible { get; } = new(
        id: "BTMA0003",
        title: "Nested type not accessible",
        messageFormat: "Type is not accessible from the generated code. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor UnsupportedConstructorArity { get; } = new(
        id: "BTMA0004",
        title: "Unsupported constructor arity",
        messageFormat: "Constructor has more than {1} parameters. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor NoAccessibleMembers { get; } = new(
        id: "BTMA0005",
        title: "No accessible members",
        messageFormat: "Type has no accessible members or constructors. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor GenericUnsafeAccessorNotSupported { get; } = new(
        id: "BTMA0006",
        title: "Generic UnsafeAccessor not supported",
        messageFormat: "Non-public member access needs .NET 9. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // TypedAccessor (01xx)

    public static DiagnosticDescriptor InvalidTypeArgument { get; } = new(
        id: "BTMA0101",
        title: "Invalid type argument",
        messageFormat: "Type must be a generic type. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor InvalidAttributeLocation { get; } = new(
        id: "BTMA0102",
        title: "Invalid attribute location",
        messageFormat: "Attribute is in a different location. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor TypedAccessorTargetNotDecorated { get; } = new(
        id: "BTMA0103",
        title: "TypedAccessor target not decorated",
        messageFormat: "Target type has no [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    // GenerateAccessorFor (02xx)

    public static DiagnosticDescriptor InvalidExternalTarget { get; } = new(
        id: "BTMA0201",
        title: "Invalid target type",
        messageFormat: "[GenerateAccessorFor] target type is not supported. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);

    public static DiagnosticDescriptor TypeNotPartial { get; } = new(
        id: "BTMA0202",
        title: "Type is not partial",
        messageFormat: "IAccessorProvider is not generated. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor AccessorAlreadyGenerated { get; } = new(
        id: "BTMA0203",
        title: "Accessor already generated",
        messageFormat: "Target type already has [GenerateAccessor]. type=[{0}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    // Generation (03xx)

    public static DiagnosticDescriptor HintNameCollision { get; } = new(
        id: "BTMA0301",
        title: "Type name differs only in case",
        messageFormat: "Type name differs only in case from another type, and its accessor is not generated. type=[{0}], other=[{1}]",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        customTags: DiagnosticTags.NotSuppressible);
}
