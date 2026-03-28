namespace BunnyTail.MemberAccessor.Generator;

using System.Collections.Immutable;
using System.Linq;
using System.Text;

using BunnyTail.MemberAccessor.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

[Generator]
public sealed class AccessorGenerator : IIncrementalGenerator
{
    private const string GenerateAccessorAttributeName = "BunnyTail.MemberAccessor.GenerateAccessorAttribute";
    private const string TypedAccessorAttributeName = "BunnyTail.MemberAccessor.TypedAccessorAttribute";

    private const string AccessorSuffix = "_Accessor";
    private const string AccessorFactorySuffix = "_AccessorFactory";

    // ------------------------------------------------------------
    // Initialize
    // ------------------------------------------------------------

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateAccessorAttributeName,
                static (syntax, _) => IsTypeSyntax(syntax),
                static (context, _) => GetTypeModel(context))
            .Collect();

        var closedGenericProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                TypedAccessorAttributeName,
                static (_, _) => true,
                static (context, _) => GetClosedGenericModel(context))
            .Collect();

        context.RegisterImplementationSourceOutput(
            typeProvider.Combine(closedGenericProvider),
            static (context, provider) => Execute(context, provider.Left, provider.Right));
    }

    // ------------------------------------------------------------
    // Parser
    // ------------------------------------------------------------

    private static bool IsTypeSyntax(SyntaxNode syntax) =>
        syntax is ClassDeclarationSyntax;

    private static Result<TypeModel> GetTypeModel(GeneratorAttributeSyntaxContext context)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;

        var ns = String.IsNullOrEmpty(symbol.ContainingNamespace.Name)
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        var properties = symbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Select(static x => new PropertyModel(
                x.Name,
                x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                CanAccess(x.GetMethod),
                CanAccess(x.SetMethod)))
            .ToArray();

        return Results.Success(new TypeModel(
            ns,
            symbol.GetClassName(),
            symbol.TypeArguments.Length,
            new EquatableArray<PropertyModel>(properties)));
    }

    private static bool CanAccess(IMethodSymbol? method)
    {
        return (method is not null) && (method.DeclaredAccessibility == Accessibility.Public);
    }

    private static EquatableArray<Result<ClosedGenericModel>> GetClosedGenericModel(GeneratorAttributeSyntaxContext context)
    {
        var list = new List<Result<ClosedGenericModel>>();
        if (context.TargetSymbol is ISourceAssemblySymbol assemblySymbol)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var attributeData in assemblySymbol.GetAttributes().Where(Predicate))
            {
                list.Add(GetClosedGenericModel(context.TargetNode, null, attributeData));
            }
        }
        else if (context.TargetSymbol is INamedTypeSymbol classSymbol)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var attributeData in classSymbol.GetAttributes().Where(Predicate))
            {
                list.Add(GetClosedGenericModel(context.TargetNode, classSymbol.OriginalDefinition, attributeData));
            }
        }

        return new EquatableArray<Result<ClosedGenericModel>>(list.ToArray());

        static bool Predicate(AttributeData attributeData) =>
            attributeData.AttributeClass?.ToDisplayString() == TypedAccessorAttributeName;
    }

    private static Result<ClosedGenericModel> GetClosedGenericModel(SyntaxNode syntax, INamedTypeSymbol? openGenericSymbol, AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol symbol)
        {
            return Results.Error<ClosedGenericModel>(null);
        }

        if (!symbol.IsGenericType)
        {
            return Results.Error<ClosedGenericModel>(new DiagnosticInfo(Diagnostics.InvalidTypeArgument, syntax.GetLocation(), symbol.Name));
        }

        if ((openGenericSymbol is not null) &&
            !SymbolEqualityComparer.Default.Equals(openGenericSymbol, symbol.OriginalDefinition))
        {
            return Results.Error<ClosedGenericModel>(new DiagnosticInfo(Diagnostics.InvalidAttributeLocation, syntax.GetLocation(), symbol.Name));
        }

        var ns = String.IsNullOrEmpty(symbol.ContainingNamespace.Name)
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        var typeArguments = symbol.TypeArguments.Select(static x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToArray();

        return Results.Success(new ClosedGenericModel(
            ns,
            symbol.GetClassName(),
            new EquatableArray<string>(typeArguments)));
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static void Execute(SourceProductionContext context, ImmutableArray<Result<TypeModel>> types, ImmutableArray<EquatableArray<Result<ClosedGenericModel>>> closedGenerics)
    {
        foreach (var info in types.SelectError())
        {
            context.ReportDiagnostic(info);
        }
        foreach (var info in closedGenerics.SelectMany(static x => x.ToArray().SelectError()))
        {
            context.ReportDiagnostic(info);
        }

        var targetTypes = types.SelectValue().ToList();
        var closedTypes = closedGenerics.SelectMany(static x => x.ToArray().SelectValue()).ToList();

        var builder = new SourceBuilder();
        foreach (var type in targetTypes)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            builder.Clear();
            BuildClassSource(builder, type);

            var filename = MakeFilename(type.Namespace, type.ClassName);
            var source = builder.ToString();
            context.AddSource(filename, SourceText.From(source, Encoding.UTF8));
        }

        builder.Clear();
        BuildRegistrySource(builder, targetTypes, closedTypes);
        context.AddSource(
            "AccessorInitializer.g.cs",
            SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void BuildClassSource(SourceBuilder builder, TypeModel type)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        var className = String.IsNullOrEmpty(type.Namespace) ? $"global::{type.ClassName}" : $"global::{type.Namespace}.{type.ClassName}";
        var properties = type.Properties.ToArray();
        var readableProperties = properties.Where(static x => x.CanRead).ToArray();
        var writableProperties = properties.Where(static x => x.CanWrite).ToArray();

        // namespace
        if (!String.IsNullOrEmpty(type.Namespace))
        {
            builder.Namespace(type.Namespace);
            builder.NewLine();
        }

        // accessor
        BuildAccessorSource(builder, type, className, readableProperties, writableProperties);

        builder.NewLine();

        // factory
        BuildFactorySource(builder, type, className, readableProperties, writableProperties);
    }

    private static void BuildAccessorSource(SourceBuilder builder, TypeModel type, string className, PropertyModel[] readableProperties, PropertyModel[] writableProperties)
    {
        // class
        builder.Indent()
            .Append("internal sealed class ")
            .AppendBy(type, BuildAccessorName)
            .Append(" : global::BunnyTail.MemberAccessor.IAccessor")
            .NewLine();
        builder.BeginScope();

        // get
        builder.Indent()
            .Append("public object? GetValue(object obj, string name)")
            .NewLine();
        builder.BeginScope();

        builder
            .Indent()
            .Append("switch (name)")
            .NewLine();
        builder.BeginScope();
        foreach (var property in readableProperties)
        {
            builder
                .Indent()
                .Append("case \"")
                .Append(property.Name)
                .Append("\": return ((")
                .Append(className)
                .Append(")obj).")
                .Append(property.Name)
                .Append(";")
                .NewLine();
        }
        builder.EndScope();

        builder
            .Indent()
            .Append("throw new ArgumentException(\"Readable property not found.\", nameof(name));")
            .NewLine();

        builder.EndScope();

        builder.NewLine();

        // set
        builder.Indent()
            .Append("public void SetValue(object obj, string name, object? value)")
            .NewLine();
        builder.BeginScope();

        builder
            .Indent()
            .Append("switch (name)")
            .NewLine();
        builder.BeginScope();
        foreach (var property in writableProperties)
        {
            builder
                .Indent()
                .Append("case \"")
                .Append(property.Name)
                .Append("\": ((")
                .Append(className)
                .Append(")obj).")
                .Append(property.Name)
                .Append(" = (")
                .Append(property.Type)
                .Append(")value!; return;")
                .NewLine();
        }
        builder.EndScope();

        builder
            .Indent()
            .Append("throw new ArgumentException(\"Writable property not found.\", nameof(name));")
            .NewLine();

        builder.EndScope();

        builder.EndScope();
    }

    private static void BuildFactorySource(SourceBuilder builder, TypeModel type, string className, PropertyModel[] readableProperties, PropertyModel[] writableProperties)
    {
        // class
        builder
            .Indent()
            .Append("internal sealed class ")
            .AppendBy(type, BuildFactoryName)
            .Append(" : global::BunnyTail.MemberAccessor.IAccessorFactory<")
            .Append(className)
            .Append('>')
            .NewLine();
        builder.BeginScope();

        // property

        // getter
        BeginDictionary(builder, "ObjectGetter", "global::System.Func<object, object?>");
        for (var i = 0; i < readableProperties.Length; i++)
        {
            var property = readableProperties[i];
            builder
                .Indent()
                .Append("{ \"")
                .Append(property.Name)
                .Append("\", static x => ((")
                .Append(className)
                .Append(")x).")
                .Append(property.Name)
                .Append("! }")
                .AppendIf(i < readableProperties.Length - 1, ",")
                .NewLine();
        }
        EndDictionary(builder);

        builder.NewLine();

        // setter
        BeginDictionary(builder, "ObjectSetter", "global::System.Action<object, object?>");
        for (var i = 0; i < writableProperties.Length; i++)
        {
            var property = writableProperties[i];
            builder
                .Indent()
                .Append("{ \"")
                .Append(property.Name)
                .Append("\", static (x, v) => ((")
                .Append(className)
                .Append(")x).")
                .Append(property.Name)
                .Append(" = (")
                .Append(property.Type)
                .Append(")v! }")
                .AppendIf(i < writableProperties.Length - 1, ",")
                .NewLine();
        }
        EndDictionary(builder);

        builder.NewLine();

        // getter
        BeginDictionary(builder, "TypedGetter", "object");
        for (var i = 0; i < readableProperties.Length; i++)
        {
            var property = readableProperties[i];
            builder
                .Indent()
                .Append("{ \"")
                .Append(property.Name)
                .Append("\", (global::System.Func<")
                .Append(className)
                .Append(", ")
                .Append(property.Type)
                .Append(">)(static (")
                .Append(className)
                .Append(" x) => x.")
                .Append(property.Name)
                .Append("!) }")
                .AppendIf(i < readableProperties.Length - 1, ",")
                .NewLine();
        }
        EndDictionary(builder);

        builder.NewLine();

        // setter
        BeginDictionary(builder, "TypedSetter", "object");
        for (var i = 0; i < writableProperties.Length; i++)
        {
            var property = writableProperties[i];
            builder
                .Indent()
                .Append("{ \"")
                .Append(property.Name)
                .Append("\", (global::System.Action<")
                .Append(className)
                .Append(", ")
                .Append(property.Type)
                .Append(">)(static (")
                .Append(className)
                .Append(" x, ")
                .Append(property.Type)
                .Append(" v) => x.")
                .Append(property.Name)
                .Append(" = v!) }")
                .AppendIf(i < writableProperties.Length - 1, ",")
                .NewLine();
        }
        EndDictionary(builder);

        builder.NewLine();

        // method

        // getter
        builder
            .Indent()
            .Append("public global::System.Func<object, object?>? CreateGetter(string name) => ObjectGetter.GetValueOrDefault(name);")
            .NewLine()
            .NewLine();

        // setter
        builder
            .Indent()
            .Append("public global::System.Action<object, object?>? CreateSetter(string name) => ObjectSetter.GetValueOrDefault(name);")
            .NewLine()
            .NewLine();

        // getter
        builder
            .Indent()
            .Append("public global::System.Func<")
            .Append(className)
            .Append(", TProperty>? CreateGetter<TProperty>(string name) => (global::System.Func<")
            .Append(className)
            .Append(", TProperty>?)TypedGetter.GetValueOrDefault(name);")
            .NewLine()
            .NewLine();

        // setter
        builder
            .Indent()
            .Append("public global::System.Action<")
            .Append(className)
            .Append(", TProperty>? CreateSetter<TProperty>(string name) => (global::System.Action<")
            .Append(className)
            .Append(", TProperty>?)TypedSetter.GetValueOrDefault(name);")
            .NewLine();

        builder.EndScope();
    }

    private static void BuildRegistrySource(SourceBuilder builder, List<TypeModel> types, List<ClosedGenericModel> closedTypes)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // class
        builder
            .Indent()
            .Append("internal static class AccessorFactoryInitializer")
            .NewLine();
        builder.BeginScope();

        // method
        builder
            .Indent()
            .Append("[global::System.Runtime.CompilerServices.ModuleInitializer]")
            .NewLine();
        builder
            .Indent()
            .Append("public static void Initialize()")
            .NewLine();
        builder.BeginScope();

        foreach (var type in types)
        {
            builder
                .Indent()
                .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterFactory(typeof(")
                .AppendBy(type, BuildRegistryTargetName)
                .Append("), typeof(")
                .AppendBy(type, BuildRegistryAccessorName)
                .Append("), typeof(")
                .AppendBy(type, BuildRegistryFactoryName)
                .Append("));")
                .NewLine();
            if (type.TypeArgumentCount > 0)
            {
                var namePart = type.ClassName.AsSpan(0, type.ClassName.IndexOf('<') + 1);
                foreach (var closedType in closedTypes)
                {
                    if ((type.Namespace == closedType.Namespace) &&
                        closedType.ClassName.AsSpan().StartsWith(namePart))
                    {
                        builder
                            .Indent()
                            .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterFactory(typeof(")
                            .AppendBy(closedType, BuildRegistryTargetName)
                            .Append("), typeof(")
                            .AppendBy(closedType, BuildRegistryAccessorName)
                            .Append("), typeof(")
                            .AppendBy(closedType, BuildRegistryFactoryName)
                            .Append("));")
                            .NewLine();
                    }
                }
            }
        }

        builder.EndScope();

        builder.EndScope();
    }

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    private static void BuildAccessorName(SourceBuilder builder, TypeModel model)
    {
        var index = model.ClassName.IndexOf('<');
        if (index < 0)
        {
            builder.Append(model.ClassName).Append(AccessorSuffix);
        }
        else
        {
            builder.Append(model.ClassName.Substring(0, index)).Append(AccessorSuffix).Append(model.ClassName.Substring(index));
        }
    }

    private static void BuildFactoryName(SourceBuilder builder, TypeModel model)
    {
        var index = model.ClassName.IndexOf('<');
        if (index < 0)
        {
            builder.Append(model.ClassName).Append(AccessorFactorySuffix);
        }
        else
        {
            builder.Append(model.ClassName.Substring(0, index)).Append(AccessorFactorySuffix).Append(model.ClassName.Substring(index));
        }
    }

    private static void BuildRegistryTargetName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        var index = model.ClassName.IndexOf('<');
        if (index < 0)
        {
            builder.Append(model.ClassName);
        }
        else
        {
            builder.Append(model.ClassName.Substring(0, index)).AppendBy(model.TypeArgumentCount, BuildGenericParameter);
        }
    }

    private static void BuildRegistryAccessorName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        var index = model.ClassName.IndexOf('<');
        if (index < 0)
        {
            builder.Append(model.ClassName).Append(AccessorSuffix);
        }
        else
        {
            builder.Append(model.ClassName.Substring(0, index)).Append(AccessorSuffix).AppendBy(model.TypeArgumentCount, BuildGenericParameter);
        }
    }

    private static void BuildRegistryFactoryName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        var index = model.ClassName.IndexOf('<');
        if (index < 0)
        {
            builder.Append(model.ClassName).Append(AccessorFactorySuffix);
        }
        else
        {
            builder.Append(model.ClassName.Substring(0, index)).Append(AccessorFactorySuffix).AppendBy(model.TypeArgumentCount, BuildGenericParameter);
        }
    }

    private static void BuildRegistryTargetName(SourceBuilder builder, ClosedGenericModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        builder.Append(model.ClassName);
    }

    private static void BuildRegistryAccessorName(SourceBuilder builder, ClosedGenericModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        var index = model.ClassName.IndexOf('<');
        builder.Append(model.ClassName.Substring(0, index)).Append(AccessorSuffix).Append(model.ClassName.Substring(index));
    }

    private static void BuildRegistryFactoryName(SourceBuilder builder, ClosedGenericModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        var index = model.ClassName.IndexOf('<');
        builder.Append(model.ClassName.Substring(0, index)).Append(AccessorFactorySuffix).Append(model.ClassName.Substring(index));
    }

    private static void BuildNamespace(SourceBuilder builder, string ns)
    {
        if (!String.IsNullOrEmpty(ns))
        {
            builder.Append("global::").Append(ns).Append('.');
        }
    }

    private static void BuildGenericParameter(SourceBuilder builder, int count)
    {
        builder.Append('<');
        for (var i = 0; i < count - 1; i++)
        {
            builder.Append(',');
        }
        builder.Append('>');
    }

    private static void BeginDictionary(SourceBuilder builder, string name, string valueType)
    {
        builder
            .Indent()
            .Append("private static readonly global::System.Collections.Frozen.FrozenDictionary<string, ")
            .Append(valueType)
            .Append("> ")
            .Append(name)
            .Append(" = global::System.Collections.Frozen.FrozenDictionary.ToFrozenDictionary(new global::System.Collections.Generic.Dictionary<string, ")
            .Append(valueType)
            .Append(">")
            .NewLine();
        builder
            .Indent()
            .Append("{")
            .NewLine();
        builder.IndentLevel++;
    }

    private static void EndDictionary(SourceBuilder builder)
    {
        builder.IndentLevel--;
        builder
            .Indent()
            .Append("});")
            .NewLine();
    }

    private static string MakeFilename(string ns, string className)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(ns))
        {
            buffer.Append(ns.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(className.Replace('<', '[').Replace('>', ']'));
        buffer.Append("_Accessor.g.cs");

        return buffer.ToString();
    }
}
