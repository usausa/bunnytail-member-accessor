namespace BunnyTail.MemberAccessor.Generator;

using System.Collections.Immutable;
using System.Globalization;
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
    private const string ConstructorAccessorSuffix = "_ConstructorAccessor";

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
        syntax is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;

    private static Result<TypeModel> GetTypeModel(GeneratorAttributeSyntaxContext context)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;

        var ns = String.IsNullOrEmpty(symbol.ContainingNamespace.Name)
            ? string.Empty
            : symbol.ContainingNamespace.ToDisplayString();

        // Collect public instance properties including inherited ones
        var allProperties = new List<IPropertySymbol>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = symbol;
        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var member in current.GetMembers().OfType<IPropertySymbol>())
            {
                // Public instance properties only (exclude static members, indexers, and properties with no public accessor)
                if (member.IsStatic || member.IsIndexer)
                {
                    continue;
                }

                if (!CanAccess(member.GetMethod) && !CanAccess(member.SetMethod))
                {
                    continue;
                }

                if (seen.Add(member.Name))
                {
                    allProperties.Add(member);
                }
            }
            current = current.BaseType;
        }

        var properties = allProperties
            .Select(static x => new PropertyModel(
                x.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                x.Name,
                CanAccess(x.GetMethod),
                CanWrite(x.SetMethod)))
            .ToArray();

        // Collect constructors (public, arity 0-4)
        var constructors = symbol.InstanceConstructors
            .Where(static c => c.DeclaredAccessibility == Accessibility.Public && c.Parameters.Length <= 4)
            .OrderBy(static c => c.Parameters.Length)
            .Select(static c => new ConstructorModel(new EquatableArray<ConstructorParameterModel>(
                c.Parameters.Select(static p => new ConstructorParameterModel(
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.Name)).ToArray())))
            .ToArray();

        return Results.Success(new TypeModel(
            ns,
            symbol.GetClassName(),
            symbol.IsValueType,
            symbol.TypeArguments.Length,
            new EquatableArray<PropertyModel>(properties),
            new EquatableArray<ConstructorModel>(constructors)));
    }

    private static bool CanAccess(IMethodSymbol? method)
    {
        return (method is not null) && (method.DeclaredAccessibility == Accessibility.Public);
    }

    // init-only setters cannot be assigned outside of object initialization, so they are treated as read-only.
    private static bool CanWrite(IMethodSymbol? method)
    {
        return (method is not null) && (method.DeclaredAccessibility == Accessibility.Public) && !method.IsInitOnly;
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
            return Results.Errors<ClosedGenericModel>();
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
        foreach (var info in closedGenerics.SelectMany(static x => x.SelectError()))
        {
            context.ReportDiagnostic(info);
        }

        var targetTypes = types.SelectValue().ToList();
        var closedTypes = closedGenerics.SelectMany(static x => x.SelectValue()).ToList();

        // BTMA0003: no readable or writable properties
        foreach (var type in targetTypes)
        {
            if (!type.Properties.Any(static x => x.CanRead || x.CanWrite))
            {
                // We can't recover a location here easily; emit at null location
                context.ReportDiagnostic(Diagnostic.Create(
                    Diagnostics.NoAccessibleMembers,
                    Location.None,
                    type.ClassName));
            }
        }

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
        var properties = type.Properties;
        var readableProperties = properties.Where(static x => x.CanRead).ToArray();
        var writableProperties = properties.Where(static x => x.CanWrite).ToArray();
        var constructors = type.Constructors;

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
        BuildFactorySource(builder, type, className, properties, readableProperties, writableProperties);

        if (constructors.Count > 0)
        {
            builder.NewLine();
            BuildConstructorAccessorSource(builder, type, className, constructors);
        }
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
        if (type.IsValueType)
        {
            builder.Indent()
                .Append("var target = (")
                .Append(className)
                .Append(")obj;")
                .NewLine();
        }
        else
        {
            builder.Indent()
                .Append("var target = global::System.Runtime.CompilerServices.Unsafe.As<")
                .Append(className)
                .Append(">(obj);")
                .NewLine();
        }
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var property in readableProperties)
        {
            builder.Indent()
                .Append("\"").Append(property.Name).Append("\" => target.").Append(property.Name).Append(",")
                .NewLine();
        }
        builder.Indent().Append("_ => throw new global::System.ArgumentException(\"Readable property not found.\", nameof(name))").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();

        builder.NewLine();

        // set
        builder.Indent()
            .Append("public void SetValue(object obj, string name, object? value)")
            .NewLine();
        builder.BeginScope();
        if (writableProperties.Length == 0)
        {
            builder.Indent()
                .Append("throw new global::System.ArgumentException(\"Writable property not found.\", nameof(name));")
                .NewLine();
        }
        else
        {
            if (type.IsValueType)
            {
                // Value types: unbox, modify, box back not possible in place. We use ref via Unsafe for structs stored as object.
                // For simplicity, we cast - note this only works if callers hold a boxed struct reference.
                builder.Indent()
                    .Append("// Note: for value types the object must be a boxed instance; modifications affect the boxed copy.")
                    .NewLine();
            }
            builder.Indent().Append("switch (name)").NewLine();
            builder.BeginScope();
            foreach (var property in writableProperties)
            {
                builder.Indent().Append("case \"").Append(property.Name).Append("\":").NewLine();
                builder.IndentLevel++;
                if (type.IsValueType)
                {
                    builder.Indent()
                        .Append("global::System.Runtime.CompilerServices.Unsafe.Unbox<")
                        .Append(className)
                        .Append(">(obj).")
                        .Append(property.Name)
                        .Append(" = (")
                        .Append(property.Type)
                        .Append(")value!;")
                        .NewLine();
                }
                else
                {
                    builder.Indent()
                        .Append("global::System.Runtime.CompilerServices.Unsafe.As<")
                        .Append(className)
                        .Append(">(obj).")
                        .Append(property.Name)
                        .Append(" = (")
                        .Append(property.Type)
                        .Append(")value!;")
                        .NewLine();
                }
                builder.Indent().Append("return;").NewLine();
                builder.IndentLevel--;
            }
            builder.Indent().Append("default:").NewLine();
            builder.IndentLevel++;
            builder.Indent()
                .Append("throw new global::System.ArgumentException(\"Writable property not found.\", nameof(name));")
                .NewLine();
            builder.IndentLevel--;
            builder.EndScope();
        }
        builder.EndScope();

        builder.EndScope();
    }

    private static void BuildFactorySource(SourceBuilder builder, TypeModel type, string className, PropertyModel[] allProperties, PropertyModel[] readableProperties, PropertyModel[] writableProperties)
    {
        // MemberDescriptor static field
        var memberDescriptors = allProperties;

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

        // Members property
        builder.Indent()
            .Append("private static readonly global::System.Collections.Generic.IReadOnlyList<global::BunnyTail.MemberAccessor.MemberDescriptor> MembersField =")
            .NewLine();
        builder.Indent()
            .Append("    [")
            .NewLine();
        builder.IndentLevel++;
        foreach (var property in memberDescriptors)
        {
            builder.Indent()
                .Append("new global::BunnyTail.MemberAccessor.MemberDescriptor(\"")
                .Append(property.Name)
                .Append("\", typeof(")
                .Append(property.Type)
                .Append("), ")
                .Append(property.CanRead ? "true" : "false")
                .Append(", ")
                .Append(property.CanWrite ? "true" : "false")
                .Append("),")
                .NewLine();
        }
        builder.IndentLevel--;
        builder.Indent().Append("    ];").NewLine();
        builder.NewLine();

        builder.Indent()
            .Append("public global::System.Collections.Generic.IReadOnlyList<global::BunnyTail.MemberAccessor.MemberDescriptor> Members => MembersField;")
            .NewLine();
        builder.NewLine();

        // CreateGetter(string name) -> object
        builder.Indent()
            .Append("public global::System.Func<object, object?>? CreateGetter(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var property in readableProperties)
        {
            builder.Indent()
                .Append("\"").Append(property.Name).Append("\" => static x => ((")
                .Append(className).Append(")x).").Append(property.Name).Append("!,")
                .NewLine();
        }
        builder.Indent().Append("_ => null").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();
        builder.NewLine();

        // CreateSetter(string name) -> object
        builder.Indent()
            .Append("public global::System.Action<object, object?>? CreateSetter(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var property in writableProperties)
        {
            if (type.IsValueType)
            {
                builder.Indent()
                    .Append("\"").Append(property.Name).Append("\" => static (x, v) => global::System.Runtime.CompilerServices.Unsafe.Unbox<")
                    .Append(className).Append(">(x).").Append(property.Name)
                    .Append(" = (").Append(property.Type).Append(")v!,")
                    .NewLine();
            }
            else
            {
                builder.Indent()
                    .Append("\"").Append(property.Name).Append("\" => static (x, v) => ((")
                    .Append(className).Append(")x).").Append(property.Name)
                    .Append(" = (").Append(property.Type).Append(")v!,")
                    .NewLine();
            }
        }
        builder.Indent().Append("_ => null").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();
        builder.NewLine();

        // CreateGetter<TProperty>(string name)
        builder.Indent()
            .Append("public global::System.Func<")
            .Append(className)
            .Append(", TProperty>? CreateGetter<TProperty>(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var property in readableProperties)
        {
            builder.Indent()
                .Append("\"").Append(property.Name).Append("\" => (global::System.Func<")
                .Append(className).Append(", TProperty>?)(object?)(global::System.Func<")
                .Append(className).Append(", ").Append(property.Type)
                .Append(">)(static (").Append(className).Append(" x) => x.").Append(property.Name).Append("!),")
                .NewLine();
        }
        builder.Indent().Append("_ => null").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();
        builder.NewLine();

        // CreateSetter<TProperty>(string name)
        builder.Indent()
            .Append("public global::System.Action<")
            .Append(className)
            .Append(", TProperty>? CreateSetter<TProperty>(string name)")
            .NewLine();
        builder.BeginScope();
        if (type.IsValueType)
        {
            // Typed setters cannot mutate value types (the delegate would receive a copy);
            // use IAccessor.SetValue with a boxed instance instead.
            builder.Indent().Append("return null;").NewLine();
        }
        else
        {
            builder.Indent().Append("return name switch").NewLine();
            builder.BeginScope();
            foreach (var property in writableProperties)
            {
                builder.Indent()
                    .Append("\"").Append(property.Name).Append("\" => (global::System.Action<")
                    .Append(className).Append(", TProperty>?)(object?)(global::System.Action<")
                    .Append(className).Append(", ").Append(property.Type)
                    .Append(">)(static (").Append(className).Append(" x, ").Append(property.Type)
                    .Append(" v) => x.").Append(property.Name).Append(" = v!),")
                    .NewLine();
            }
            builder.Indent().Append("_ => null").NewLine();
            builder.IndentLevel--;
            builder.Indent().Append("};").NewLine();
        }
        builder.EndScope();

        builder.EndScope();
    }

    private static void BuildConstructorAccessorSource(SourceBuilder builder, TypeModel type, string className, ConstructorModel[] constructors)
    {
        builder.Indent()
            .Append("internal sealed class ")
            .AppendBy(type, BuildConstructorAccessorName)
            .Append(" : global::BunnyTail.MemberAccessor.IConstructorAccessor<")
            .Append(className)
            .Append('>')
            .NewLine();
        builder.BeginScope();

        // Group by arity; multiple constructors may share an arity and are disambiguated by argument type.
        var byArity = constructors.GroupBy(static c => c.Parameters.Count)
            .ToDictionary(static g => g.Key, static g => g.ToArray());

        // Create() - 0 args
        builder.Indent().Append("public ").Append(className).Append(" Create()").NewLine();
        builder.BeginScope();
        if (byArity.ContainsKey(0))
        {
            builder.Indent().Append("return new ").Append(className).Append("();").NewLine();
        }
        else
        {
            builder.Indent().Append("throw new global::System.NotSupportedException(\"No parameterless constructor.\");").NewLine();
        }
        builder.EndScope();
        builder.NewLine();

        // Create<TArg>(TArg arg) - 1 arg
        builder.Indent().Append("public ").Append(className).Append(" Create<TArg>(TArg arg)").NewLine();
        builder.BeginScope();
        BuildCreateBody(builder, className, byArity, 1);
        builder.EndScope();
        builder.NewLine();

        // Create<TArg1, TArg2>(TArg1 arg1, TArg2 arg2) - 2 args
        builder.Indent().Append("public ").Append(className).Append(" Create<TArg1, TArg2>(TArg1 arg1, TArg2 arg2)").NewLine();
        builder.BeginScope();
        BuildCreateBody(builder, className, byArity, 2);
        builder.EndScope();
        builder.NewLine();

        // Create<TArg1, TArg2, TArg3>
        builder.Indent().Append("public ").Append(className).Append(" Create<TArg1, TArg2, TArg3>(TArg1 arg1, TArg2 arg2, TArg3 arg3)").NewLine();
        builder.BeginScope();
        BuildCreateBody(builder, className, byArity, 3);
        builder.EndScope();
        builder.NewLine();

        // Create<TArg1, TArg2, TArg3, TArg4>
        builder.Indent().Append("public ").Append(className).Append(" Create<TArg1, TArg2, TArg3, TArg4>(TArg1 arg1, TArg2 arg2, TArg3 arg3, TArg4 arg4)").NewLine();
        builder.BeginScope();
        BuildCreateBody(builder, className, byArity, 4);
        builder.EndScope();

        builder.EndScope();
    }

    private static void BuildCreateBody(SourceBuilder builder, string className, Dictionary<int, ConstructorModel[]> byArity, int arity)
    {
        if (!byArity.TryGetValue(arity, out var constructors))
        {
            builder.Indent()
                .Append("throw new global::System.NotSupportedException(\"No ")
                .Append(arity.ToString(CultureInfo.InvariantCulture))
                .Append("-parameter constructor.\");")
                .NewLine();
            return;
        }

        // Single constructor for the arity: bind directly (preserves implicit-conversion behavior).
        if (constructors.Length == 1)
        {
            BuildNewExpression(builder, className, constructors[0], arity);
            return;
        }

        // Multiple constructors share the arity: select by exact argument type at runtime.
        foreach (var ctor in constructors)
        {
            builder.Indent().Append("if (");
            for (var i = 0; i < arity; i++)
            {
                if (i > 0)
                {
                    builder.Append(" && ");
                }
                builder.Append("typeof(").Append(ArgTypeParameterName(arity, i)).Append(") == typeof(").Append(ctor.Parameters[i].Type).Append(')');
            }
            builder.Append(')').NewLine();
            builder.BeginScope();
            BuildNewExpression(builder, className, ctor, arity);
            builder.EndScope();
        }
        builder.Indent()
            .Append("throw new global::System.NotSupportedException(\"No matching ")
            .Append(arity.ToString(CultureInfo.InvariantCulture))
            .Append("-parameter constructor.\");")
            .NewLine();
    }

    private static void BuildNewExpression(SourceBuilder builder, string className, ConstructorModel ctor, int arity)
    {
        builder.Indent().Append("return new ").Append(className).Append('(');
        for (var i = 0; i < arity; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            builder.Append('(').Append(ctor.Parameters[i].Type).Append(")(object)").Append(ArgName(arity, i)).Append('!');
        }
        builder.Append(");").NewLine();
    }

    private static string ArgTypeParameterName(int arity, int index) =>
        arity == 1 ? "TArg" : $"TArg{index + 1}";

    private static string ArgName(int arity, int index) =>
        arity == 1 ? "arg" : $"arg{index + 1}";

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
            .Append("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"AOT\", \"IL3050\", Justification = \"Open-generic registrations require dynamic code; AOT users should use [TypedAccessor] to pre-register closed types.\")]")
            .NewLine();
        builder
            .Indent()
            .Append("[global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage(\"Trimming\", \"IL2026\", Justification = \"Open-generic registrations require unreferenced code; AOT users should use [TypedAccessor] to pre-register closed types.\")]")
            .NewLine();
        builder
            .Indent()
            .Append("public static void Initialize()")
            .NewLine();
        builder.BeginScope();

        foreach (var type in types)
        {
            if (type.TypeArgumentCount == 0)
            {
                // Non-generic: register instances directly (AOT-safe, no Activator)
                builder
                    .Indent()
                    .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterFactory(typeof(")
                    .AppendBy(type, BuildRegistryTargetName)
                    .Append("), new ")
                    .AppendBy(type, BuildRegistryAccessorName)
                    .Append("(), new ")
                    .AppendBy(type, BuildRegistryFactoryName)
                    .Append("());")
                    .NewLine();

                // Register constructor accessor if constructors exist
                if (type.Constructors.Count > 0)
                {
                    builder
                        .Indent()
                        .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterConstructor<")
                        .AppendBy(type, BuildRegistryTargetName)
                        .Append(">(typeof(")
                        .AppendBy(type, BuildRegistryTargetName)
                        .Append("), new ")
                        .AppendBy(type, BuildRegistryConstructorAccessorName)
                        .Append("());")
                        .NewLine();
                }
            }
            else
            {
                // Open generic: register open-generic factory delegate + pre-registered closed types
                // Pre-registered closed types from [TypedAccessor]
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
                            .Append("), new ")
                            .AppendBy(closedType, BuildRegistryAccessorName)
                            .Append("(), new ")
                            .AppendBy(closedType, BuildRegistryFactoryName)
                            .Append("());")
                            .NewLine();

                        if (type.Constructors.Count > 0)
                        {
                            builder
                                .Indent()
                                .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterConstructor<")
                                .AppendBy(closedType, BuildRegistryTargetName)
                                .Append(">(typeof(")
                                .AppendBy(closedType, BuildRegistryTargetName)
                                .Append("), new ")
                                .AppendBy(closedType, BuildRegistryConstructorAccessorName)
                                .Append("());")
                                .NewLine();
                        }
                    }
                }

                // Open-generic delegate registration for on-demand instantiation
                builder
                    .Indent()
                    .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterOpenGenericFactory(typeof(")
                    .AppendBy(type, BuildRegistryOpenTargetName)
                    .Append("),")
                    .NewLine();
                builder.IndentLevel++;
                builder
                    .Indent()
                    .Append("static typeArgs => (global::BunnyTail.MemberAccessor.IAccessor)global::System.Activator.CreateInstance(")
                    .Append("typeof(")
                    .AppendBy(type, BuildRegistryAccessorOpenName)
                    .Append(").MakeGenericType(typeArgs))!,")
                    .NewLine();
                builder
                    .Indent()
                    .Append("static typeArgs => (global::BunnyTail.MemberAccessor.IAccessorFactory)global::System.Activator.CreateInstance(")
                    .Append("typeof(")
                    .AppendBy(type, BuildRegistryFactoryOpenName)
                    .Append(").MakeGenericType(typeArgs))!);")
                    .NewLine();
                builder.IndentLevel--;

                // Open-generic constructor accessor delegate for on-demand instantiation
                if (type.Constructors.Count > 0)
                {
                    builder
                        .Indent()
                        .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterOpenGenericConstructorFactory(typeof(")
                        .AppendBy(type, BuildRegistryOpenTargetName)
                        .Append("),")
                        .NewLine();
                    builder.IndentLevel++;
                    builder
                        .Indent()
                        .Append("static typeArgs => global::System.Activator.CreateInstance(")
                        .Append("typeof(")
                        .AppendBy(type, BuildRegistryConstructorAccessorOpenName)
                        .Append(").MakeGenericType(typeArgs))!);")
                        .NewLine();
                    builder.IndentLevel--;
                }
            }
        }

        builder.EndScope();

        builder.EndScope();
    }

    // ------------------------------------------------------------
    // Helper: name builders
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

    private static void BuildConstructorAccessorName(SourceBuilder builder, TypeModel model)
    {
        var index = model.ClassName.IndexOf('<');
        if (index < 0)
        {
            builder.Append(model.ClassName).Append(ConstructorAccessorSuffix);
        }
        else
        {
            builder.Append(model.ClassName.Substring(0, index)).Append(ConstructorAccessorSuffix).Append(model.ClassName.Substring(index));
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

    private static void BuildRegistryOpenTargetName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);
        var index = model.ClassName.IndexOf('<');
        builder.Append(index < 0 ? model.ClassName : model.ClassName.Substring(0, index))
               .AppendBy(model.TypeArgumentCount, BuildGenericParameter);
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

    private static void BuildRegistryAccessorOpenName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);
        var index = model.ClassName.IndexOf('<');
        builder.Append(index < 0 ? model.ClassName : model.ClassName.Substring(0, index)).Append(AccessorSuffix)
               .AppendBy(model.TypeArgumentCount, BuildGenericParameter);
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

    private static void BuildRegistryFactoryOpenName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);
        var index = model.ClassName.IndexOf('<');
        builder.Append(index < 0 ? model.ClassName : model.ClassName.Substring(0, index)).Append(AccessorFactorySuffix)
               .AppendBy(model.TypeArgumentCount, BuildGenericParameter);
    }

    private static void BuildRegistryConstructorAccessorName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        var index = model.ClassName.IndexOf('<');
        if (index < 0)
        {
            builder.Append(model.ClassName).Append(ConstructorAccessorSuffix);
        }
        else
        {
            builder.Append(model.ClassName.Substring(0, index)).Append(ConstructorAccessorSuffix).AppendBy(model.TypeArgumentCount, BuildGenericParameter);
        }
    }

    private static void BuildRegistryConstructorAccessorOpenName(SourceBuilder builder, TypeModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);
        var index = model.ClassName.IndexOf('<');
        builder.Append(index < 0 ? model.ClassName : model.ClassName.Substring(0, index)).Append(ConstructorAccessorSuffix)
               .AppendBy(model.TypeArgumentCount, BuildGenericParameter);
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

    private static void BuildRegistryConstructorAccessorName(SourceBuilder builder, ClosedGenericModel model)
    {
        builder.AppendBy(model.Namespace, BuildNamespace);

        var index = model.ClassName.IndexOf('<');
        builder.Append(model.ClassName.Substring(0, index)).Append(ConstructorAccessorSuffix).Append(model.ClassName.Substring(index));
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
