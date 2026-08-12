namespace BunnyTail.MemberAccessor.Generator;

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

using BunnyTail.MemberAccessor.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

using SourceGenerateHelper;

[Generator]
public sealed class AccessorGenerator : IIncrementalGenerator
{
    private const string GenerateAccessorAttributeName = "BunnyTail.MemberAccessor.GenerateAccessorAttribute";
    private const string TypedAccessorAttributeName = "BunnyTail.MemberAccessor.TypedAccessorAttribute";
    private const string GenerateAccessorForAttributeName = "BunnyTail.MemberAccessor.GenerateAccessorForAttribute";
    private const string AccessorMemberAttributeName = "BunnyTail.MemberAccessor.AccessorMemberAttribute";

    private const string AccessorSuffix = "_Accessor";
    private const string AccessorFactorySuffix = "_AccessorFactory";
    private const string ConstructorAccessorSuffix = "_ConstructorAccessor";
    private const string UnsafeAccessSuffix = "_UnsafeAccess";

    private const string BridgeGetPrefix = "Get_";
    private const string BridgeSetPrefix = "Set_";
    private const string BridgeFieldPrefix = "Field_";

    // Maximum constructor arity supported by IConstructor<T>.Create overloads
    private const int MaxConstructorArity = 16;

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

        var externalProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                GenerateAccessorForAttributeName,
                static (_, _) => true,
                static (context, _) => GetExternalModels(context))
            .Collect();

        context.RegisterSourceOutput(
            typeProvider.Combine(closedGenericProvider).Combine(externalProvider),
            static (context, provider) => ReportDiagnostics(context, provider.Left.Left, provider.Left.Right, provider.Right));

        var models = typeProvider.SelectMany(static (types, _) => types.SelectValue().ToImmutableArray());
        context.RegisterImplementationSourceOutput(
            models,
            static (context, type) => ExecuteClass(context, type));

        var typeKeyProvider = typeProvider
            .Select(static (types, _) => new EquatableArray<string>(types.SelectValue().Select(MakeTypeKey).ToArray()))
            .WithTrackingName("TypeKeys");
        context.RegisterImplementationSourceOutput(
            externalProvider.Combine(typeKeyProvider),
            static (context, provider) => ExecuteExternal(context, provider.Left, provider.Right));

        var registryProvider = typeProvider
            .Combine(closedGenericProvider)
            .Combine(externalProvider)
            .Select(static (provider, _) => BuildRegistryModel(provider.Left.Left, provider.Left.Right, provider.Right))
            .WithTrackingName("Registry");
        context.RegisterImplementationSourceOutput(
            registryProvider,
            static (context, model) => ExecuteRegistry(context, model));
    }

    // ------------------------------------------------------------
    // Parser : TypeModel
    // ------------------------------------------------------------

    private static bool IsTypeSyntax(SyntaxNode syntax) =>
        syntax is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax;

    private static Result<TypeModel> GetTypeModel(GeneratorAttributeSyntaxContext context)
    {
        var symbol = (INamedTypeSymbol)context.TargetSymbol;
        var syntax = (TypeDeclarationSyntax)context.TargetNode;
        var isPartial = syntax.Modifiers.Any(SyntaxKind.PartialKeyword);
        var supportsGenericUnsafe = SupportsGenericUnsafeAccessor(context.TargetNode.SyntaxTree);

        return GetTypeModel(symbol, syntax.Identifier.GetLocation(), isPartial, sameAssembly: true, supportsGenericUnsafe);
    }

    private static Result<TypeModel> GetTypeModel(INamedTypeSymbol symbol, Location location, bool isPartial, bool sameAssembly, bool supportsGenericUnsafe)
    {
        var ns = String.IsNullOrEmpty(symbol.ContainingNamespace.Name) ? string.Empty : symbol.ContainingNamespace.ToDisplayString();

        // Collect instance members
        var members = new List<MemberModel>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = symbol;
        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol property)
                {
                    if (property.IsStatic || property.IsIndexer)
                    {
                        continue;
                    }

                    var (ignore, optIn) = GetMemberAttributeInfo(property);
                    if (ignore)
                    {
                        seen.Add(property.Name);
                        continue;
                    }

                    var getterAccess = ClassifyAccessor(property.GetMethod, optIn, sameAssembly);
                    var setterAccess = ((property.SetMethod is not null) && property.SetMethod.IsInitOnly)
                        ? MemberAccess.None
                        : ClassifyAccessor(property.SetMethod, optIn, sameAssembly);
                    if ((getterAccess == MemberAccess.None) && (setterAccess == MemberAccess.None))
                    {
                        continue;
                    }

                    if (seen.Add(property.Name))
                    {
                        var unsafeTargetType = ((getterAccess == MemberAccess.Unsafe) || (setterAccess == MemberAccess.Unsafe))
                            ? member.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            : string.Empty;
                        members.Add(new MemberModel(
                            property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            property.Name,
                            false,
                            getterAccess,
                            setterAccess,
                            unsafeTargetType));
                    }
                }
                else if (member is IFieldSymbol field)
                {
                    if (field.IsStatic || field.IsConst || field.IsImplicitlyDeclared || (field.AssociatedSymbol is not null))
                    {
                        continue;
                    }

                    var (ignore, optIn) = GetMemberAttributeInfo(field);
                    if (ignore)
                    {
                        seen.Add(field.Name);
                        continue;
                    }

                    var access = ClassifyAccessibility(field.DeclaredAccessibility, optIn, sameAssembly);
                    if (access == MemberAccess.None)
                    {
                        continue;
                    }

                    var setterAccess = field.IsReadOnly ? MemberAccess.None : access;
                    if (seen.Add(field.Name))
                    {
                        var unsafeTargetType = (access == MemberAccess.Unsafe)
                            ? member.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
                            : string.Empty;
                        members.Add(new MemberModel(
                            field.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                            field.Name,
                            true,
                            access,
                            setterAccess,
                            unsafeTargetType));
                    }
                }
            }
            current = current.BaseType;
        }

        // Collect constructors
        var publicConstructors = symbol.InstanceConstructors
            .Where(static x => x.DeclaredAccessibility == Accessibility.Public)
            .ToList();
        if (symbol.IsAbstract)
        {
            publicConstructors = [];
        }

        if (publicConstructors.Any(static x => x.Parameters.Length > MaxConstructorArity))
        {
            return Results.Error<TypeModel>(new DiagnosticInfo(
                Diagnostics.UnsupportedConstructorArity,
                location,
                symbol.Name,
                MaxConstructorArity.ToString(CultureInfo.InvariantCulture)));
        }

        var constructors = publicConstructors
            .OrderBy(static x => x.Parameters.Length)
            .Select(static x => new ConstructorModel(new EquatableArray<ConstructorParameterModel>(
                x.Parameters.Select(static p => new ConstructorParameterModel(
                    p.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    p.Name)).ToArray())))
            .ToArray();

        var typeKeyword = symbol.IsRecord
            ? (symbol.IsValueType ? "record struct" : "record")
            : (symbol.IsValueType ? "struct" : "class");

        return Results.Success(new TypeModel(
            ns,
            symbol.GetClassName(),
            symbol.IsValueType,
            typeKeyword,
            symbol.TypeArguments.Length,
            isPartial,
            supportsGenericUnsafe,
            new EquatableArray<ConstructorModel>(constructors),
            new EquatableArray<MemberModel>(members.ToArray())));
    }

    private static (bool Ignore, bool OptIn) GetMemberAttributeInfo(ISymbol member)
    {
        var attribute = member.GetAttributes().FirstOrDefault(static x => x.AttributeClass?.ToDisplayString() == AccessorMemberAttributeName);
        if (attribute is null)
        {
            return (false, false);
        }

        var ignore = attribute.NamedArguments.Any(static x => (x.Key == "Ignore") && (x.Value.Value is true));
        return (ignore, !ignore);
    }

    private static MemberAccess ClassifyAccessor(IMethodSymbol? method, bool optIn, bool sameAssembly) =>
        method is null ? MemberAccess.None : ClassifyAccessibility(method.DeclaredAccessibility, optIn, sameAssembly);

    private static MemberAccess ClassifyAccessibility(Accessibility accessibility, bool optIn, bool sameAssembly)
    {
        if (accessibility == Accessibility.Public)
        {
            return MemberAccess.Direct;
        }

        if (!optIn)
        {
            return MemberAccess.None;
        }

        return accessibility switch
        {
            Accessibility.Internal or Accessibility.ProtectedOrInternal => sameAssembly ? MemberAccess.Direct : MemberAccess.Unsafe,
            _ => MemberAccess.Unsafe
        };
    }

    // ------------------------------------------------------------
    // Parser : ClosedGenericModel
    // ------------------------------------------------------------

    private static EquatableArray<Result<ClosedGenericModel>> GetClosedGenericModel(GeneratorAttributeSyntaxContext context)
    {
        var list = new List<Result<ClosedGenericModel>>();
        if (context.TargetSymbol is ISourceAssemblySymbol assemblySymbol)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var data in assemblySymbol.GetAttributes().Where(Predicate))
            {
                list.Add(GetClosedGenericModel(context.TargetNode, null, data));
            }
        }
        else if (context.TargetSymbol is INamedTypeSymbol classSymbol)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var data in classSymbol.GetAttributes().Where(Predicate))
            {
                list.Add(GetClosedGenericModel(context.TargetNode, classSymbol.OriginalDefinition, data));
            }
        }

        return new EquatableArray<Result<ClosedGenericModel>>(list.ToArray());

        static bool Predicate(AttributeData data) => data.AttributeClass?.ToDisplayString() == TypedAccessorAttributeName;
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

        if ((openGenericSymbol is not null) && !SymbolEqualityComparer.Default.Equals(openGenericSymbol, symbol.OriginalDefinition))
        {
            return Results.Error<ClosedGenericModel>(new DiagnosticInfo(Diagnostics.InvalidAttributeLocation, syntax.GetLocation(), symbol.Name));
        }

        // The target type must be decorated with GenerateAccessorAttribute
        if (!symbol.OriginalDefinition.GetAttributes().Any(static x => x.AttributeClass?.ToDisplayString() == GenerateAccessorAttributeName))
        {
            return Results.Error<ClosedGenericModel>(new DiagnosticInfo(Diagnostics.TypedAccessorTargetNotDecorated, syntax.GetLocation(), symbol.Name));
        }

        var ns = String.IsNullOrEmpty(symbol.ContainingNamespace.Name) ? string.Empty : symbol.ContainingNamespace.ToDisplayString();
        var typeArguments = symbol.TypeArguments.Select(static x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToArray();

        return Results.Success(new ClosedGenericModel(
            ns,
            symbol.GetClassName(),
            new EquatableArray<string>(typeArguments)));
    }

    // ------------------------------------------------------------
    // Parser : ExternalModel
    // ------------------------------------------------------------

    private static EquatableArray<Result<ExternalModel>> GetExternalModels(GeneratorAttributeSyntaxContext context)
    {
        var list = new List<Result<ExternalModel>>();
        var compilation = context.SemanticModel.Compilation;
        var supportsGenericUnsafe = SupportsGenericUnsafeAccessor(context.TargetNode.SyntaxTree);

        // Witness type information
        var witnessSymbol = context.TargetSymbol as INamedTypeSymbol;
        var witnessIsPartial = (context.TargetNode is TypeDeclarationSyntax typeSyntax) && typeSyntax.Modifiers.Any(SyntaxKind.PartialKeyword);
        var witnessNotPartialReported = false;

        var attributes = context.TargetSymbol.GetAttributes().Where(static x => x.AttributeClass?.ToDisplayString() == GenerateAccessorForAttributeName);
        foreach (var data in attributes)
        {
            var location = context.TargetNode.GetLocation();

            if (data.ConstructorArguments[0].Value is not INamedTypeSymbol target)
            {
                list.Add(Results.Error<ExternalModel>(new DiagnosticInfo(Diagnostics.InvalidExternalTarget, location, data.ConstructorArguments[0].Value?.ToString() ?? "unknown")));
                continue;
            }

            if ((target.TypeKind is not (TypeKind.Class or TypeKind.Struct)) ||
                target.IsStatic ||
                target.IsRefLikeType ||
                (target.ContainingType is not null) ||
                !compilation.IsSymbolAccessibleWithin(target.OriginalDefinition, compilation.Assembly))
            {
                list.Add(Results.Error<ExternalModel>(new DiagnosticInfo(Diagnostics.InvalidExternalTarget, location, target.Name)));
                continue;
            }

            var sameAssembly = SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, compilation.Assembly);
            var hasGenerateAccessor = sameAssembly &&
                                      target.OriginalDefinition.GetAttributes().Any(static x => x.AttributeClass?.ToDisplayString() == GenerateAccessorAttributeName);

            var definition = target.OriginalDefinition;
            var typeResult = GetTypeModel(definition, location, isPartial: false, sameAssembly, supportsGenericUnsafe);
            if (!typeResult.HasValue)
            {
                foreach (var info in typeResult.Diagnostics)
                {
                    list.Add(Results.Error<ExternalModel>(info));
                }
                continue;
            }

            var typeModel = typeResult.Value;

            ClosedGenericModel? closedGeneric = null;
            if (target.IsGenericType && !target.IsUnboundGenericType)
            {
                var ns = String.IsNullOrEmpty(target.ContainingNamespace.Name) ? string.Empty : target.ContainingNamespace.ToDisplayString();
                var typeArguments = target.TypeArguments.Select(static x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).ToArray();
                closedGeneric = new ClosedGenericModel(ns, target.GetClassName(), new EquatableArray<string>(typeArguments));
            }

            // Witness implementation is only possible for closed types
            WitnessModel? witness = null;
            var closedTarget = !target.IsGenericType || (closedGeneric is not null);
            if ((witnessSymbol is not null) && closedTarget)
            {
                if (witnessIsPartial)
                {
                    witness = CreateWitnessModel(witnessSymbol, typeModel, closedGeneric);
                }
                else if (!witnessNotPartialReported)
                {
                    witnessNotPartialReported = true;
                    list.Add(Results.Error<ExternalModel>(new DiagnosticInfo(Diagnostics.TypeNotPartial, location, witnessSymbol.Name)));
                }
            }

            if (hasGenerateAccessor)
            {
                list.Add(Results.Error<ExternalModel>(new DiagnosticInfo(Diagnostics.AccessorAlreadyGenerated, location, target.Name)));
            }

            list.Add(Results.Success(new ExternalModel(typeModel, closedGeneric, witness, hasGenerateAccessor)));
        }

        return new EquatableArray<Result<ExternalModel>>(list.ToArray());
    }

    private static WitnessModel CreateWitnessModel(INamedTypeSymbol witnessSymbol, TypeModel typeModel, ClosedGenericModel? closedGeneric)
    {
        var witnessNs = String.IsNullOrEmpty(witnessSymbol.ContainingNamespace.Name) ? string.Empty : witnessSymbol.ContainingNamespace.ToDisplayString();
        var witnessKeyword = witnessSymbol.IsRecord
            ? (witnessSymbol.IsValueType ? "record struct" : "record")
            : (witnessSymbol.IsValueType ? "struct" : "class");

        string targetName;
        string accessorName;
        string factoryName;
        string constructorName;
        if (closedGeneric is not null)
        {
            var prefix = OpenNamePart(closedGeneric.ClassName);
            var argsPart = MakeTypeArgumentsPart(closedGeneric.TypeArguments);
            targetName = MakeQualifiedName(closedGeneric.Namespace, $"{prefix}{argsPart}");
            accessorName = MakeQualifiedName(closedGeneric.Namespace, $"{prefix}{AccessorSuffix}{argsPart}");
            factoryName = MakeQualifiedName(closedGeneric.Namespace, $"{prefix}{AccessorFactorySuffix}{argsPart}");
            constructorName = typeModel.Constructors.Count > 0
                ? MakeQualifiedName(closedGeneric.Namespace, $"{prefix}{ConstructorAccessorSuffix}{argsPart}")
                : string.Empty;
        }
        else
        {
            targetName = MakeQualifiedName(typeModel.Namespace, typeModel.ClassName);
            accessorName = MakeQualifiedName(typeModel.Namespace, $"{typeModel.ClassName}{AccessorSuffix}");
            factoryName = MakeQualifiedName(typeModel.Namespace, $"{typeModel.ClassName}{AccessorFactorySuffix}");
            constructorName = typeModel.Constructors.Count > 0
                ? MakeQualifiedName(typeModel.Namespace, $"{typeModel.ClassName}{ConstructorAccessorSuffix}")
                : string.Empty;
        }

        return new WitnessModel(
            witnessNs,
            witnessSymbol.GetClassName(),
            witnessKeyword,
            targetName,
            accessorName,
            factoryName,
            constructorName);
    }

    // ------------------------------------------------------------
    // Parser : RegistryModel
    // ------------------------------------------------------------

    private static RegistryModel BuildRegistryModel(
        ImmutableArray<Result<TypeModel>> types,
        ImmutableArray<EquatableArray<Result<ClosedGenericModel>>> closedGenerics,
        ImmutableArray<EquatableArray<Result<ExternalModel>>> externals)
    {
        var targetTypes = types.SelectValue().Select(MakeRegistryType).ToList();
        var closedTypes = closedGenerics.SelectMany(static x => x.SelectValue()).ToList();

        // Merge external targets
        var typeKeys = new HashSet<string>(types.SelectValue().Select(MakeTypeKey), StringComparer.Ordinal);
        foreach (var external in externals.SelectMany(static x => x.SelectValue()))
        {
            if (!external.TargetHasGenerateAccessor && typeKeys.Add(MakeTypeKey(external.Type)))
            {
                targetTypes.Add(MakeRegistryType(external.Type));
            }
            if (external.ClosedGeneric is not null)
            {
                closedTypes.Add(external.ClosedGeneric);
            }
        }

        // Distinct closed registrations
        var closedKeys = new HashSet<string>(StringComparer.Ordinal);

        return new RegistryModel(
            new EquatableArray<RegistryTypeModel>(targetTypes.ToArray()),
            new EquatableArray<ClosedGenericModel>(closedTypes.Where(x => closedKeys.Add(MakeClosedTypeKey(x))).ToArray()));

        static RegistryTypeModel MakeRegistryType(TypeModel type) =>
            new(type.Namespace, type.ClassName, type.TypeArgumentCount, type.Constructors.Count > 0);
    }

    // ------------------------------------------------------------
    // Parser : Shared
    // ------------------------------------------------------------

    private static bool SupportsGenericUnsafeAccessor(SyntaxTree tree) =>
        (tree.Options is CSharpParseOptions options) && options.PreprocessorSymbolNames.Contains("NET9_0_OR_GREATER");

    // ------------------------------------------------------------
    // Diagnostics
    // ------------------------------------------------------------

    private static void ReportDiagnostics(
        SourceProductionContext context,
        ImmutableArray<Result<TypeModel>> types,
        ImmutableArray<EquatableArray<Result<ClosedGenericModel>>> closedGenerics,
        ImmutableArray<EquatableArray<Result<ExternalModel>>> externals)
    {
        foreach (var info in types.SelectError())
        {
            context.ReportDiagnostic(info);
        }
        foreach (var info in closedGenerics.SelectMany(static x => x.SelectError()))
        {
            context.ReportDiagnostic(info);
        }
        foreach (var info in externals.SelectMany(static x => x.SelectError()))
        {
            context.ReportDiagnostic(info);
        }

        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in types.SelectValue())
        {
            ReportTypeDiagnostics(context, type, reported);
        }
        foreach (var external in externals.SelectMany(static x => x.SelectValue()))
        {
            ReportTypeDiagnostics(context, external.Type, reported);
        }
    }

    private static void ReportTypeDiagnostics(SourceProductionContext context, TypeModel type, HashSet<string> reported)
    {
        if (!reported.Add(MakeTypeKey(type)))
        {
            return;
        }

        // No readable/writable members
        if (!type.Members.Any(static x => x.CanRead || x.CanWrite))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.NoAccessibleMembers, Location.None, type.ClassName));
        }

        // UnsafeAccessor with generic parameters requires .NET 9 or later
        if ((type.TypeArgumentCount > 0) && !type.SupportsGenericUnsafeAccessor && type.Members.Any(static x => x.RequiresUnsafe))
        {
            context.ReportDiagnostic(Diagnostic.Create(Diagnostics.GenericUnsafeAccessorNotSupported, Location.None, type.ClassName));
        }
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static void ExecuteClass(SourceProductionContext context, TypeModel type)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var builder = new SourceBuilder();
        BuildClassSource(builder, type);

        var filename = MakeFilename(type.Namespace, type.ClassName);
        context.AddSource(filename, SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    private static void ExecuteExternal(
        SourceProductionContext context,
        ImmutableArray<EquatableArray<Result<ExternalModel>>> externals,
        EquatableArray<string> typeKeys)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var emitted = new HashSet<string>(typeKeys, StringComparer.Ordinal);
        var witnessEmitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var external in externals.SelectMany(static x => x.SelectValue()))
        {
            if (!external.TargetHasGenerateAccessor && emitted.Add(MakeTypeKey(external.Type)))
            {
                var builder = new SourceBuilder();
                BuildClassSource(builder, external.Type);

                var filename = MakeFilename(external.Type.Namespace, external.Type.ClassName);
                context.AddSource(filename, SourceText.From(builder.ToString(), Encoding.UTF8));
            }

            if (external.Witness is { } witness)
            {
                var witnessKey = $"{witness.Namespace}/{witness.ClassName}::{witness.TargetTypeName}";
                if (witnessEmitted.Add(witnessKey))
                {
                    var builder = new SourceBuilder();
                    BuildWitnessSource(builder, witness);

                    var filename = MakeWitnessFilename(witness);
                    context.AddSource(filename, SourceText.From(builder.ToString(), Encoding.UTF8));
                }
            }
        }
    }

    private static void ExecuteRegistry(SourceProductionContext context, RegistryModel model)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var builder = new SourceBuilder();
        BuildRegistrySource(builder, model.Types, model.ClosedTypes);
        context.AddSource("AccessorInitializer.g.cs", SourceText.From(builder.ToString(), Encoding.UTF8));
    }

    // ------------------------------------------------------------
    // Build
    // ------------------------------------------------------------

    private static void BuildClassSource(SourceBuilder builder, TypeModel type)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        var className = MakeQualifiedName(type.Namespace, type.ClassName);
        var members = type.Members;
        var readableMembers = members.Where(static x => x.CanRead).ToList();
        var writableMembers = members.Where(static x => x.CanWrite).ToList();

        // Namespace
        if (!String.IsNullOrEmpty(type.Namespace))
        {
            builder.Namespace(type.Namespace);
            builder.NewLine();
        }

        // Accessor
        BuildAccessorSource(builder, type, className, readableMembers, writableMembers);

        builder.NewLine();

        // Factory
        BuildFactorySource(builder, type, className, readableMembers, writableMembers);

        if (type.Constructors.Count > 0)
        {
            builder.NewLine();
            BuildConstructorAccessorSource(builder, type, className);
        }

        // UnsafeAccessor bridges for non-public members
        if (members.Any(static x => x.RequiresUnsafe))
        {
            builder.NewLine();
            BuildUnsafeAccessSource(builder, type);
        }

        // IAccessorProvider / IConstructorProvider implementation on the partial target type
        if (type.IsPartial)
        {
            builder.NewLine();
            BuildAccessorProviderPartialSource(builder, type, className);
        }
    }

    private static void BuildAccessorSource(SourceBuilder builder, TypeModel type, string className, List<MemberModel> readableMembers, List<MemberModel> writableMembers)
    {
        // Class
        builder.Indent()
            .Append("internal sealed class ")
            .Append(MakeSuffixedName(type.ClassName, AccessorSuffix))
            .Append(" : global::BunnyTail.MemberAccessor.IAccessor")
            .NewLine();
        builder.BeginScope();

        // Singleton
        builder.Indent()
            .Append("internal static readonly ")
            .Append(MakeSuffixedName(type.ClassName, AccessorSuffix))
            .Append(" Instance = new();")
            .NewLine();
        builder.NewLine();

        // Getter
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
        foreach (var member in readableMembers)
        {
            builder.Indent()
                .Append("\"").Append(member.Name).Append("\" => ")
                .Append(BuildReadExpression(type, member, "target"))
                .Append(",")
                .NewLine();
        }
        builder.Indent().Append("_ => throw new global::System.ArgumentException(\"Readable member not found.\", nameof(name))").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();

        builder.NewLine();

        // Setter
        builder.Indent()
            .Append("public void SetValue(object obj, string name, object? value)")
            .NewLine();
        builder.BeginScope();
        if (writableMembers.Count == 0)
        {
            builder.Indent()
                .Append("throw new global::System.ArgumentException(\"Writable member not found.\", nameof(name));")
                .NewLine();
        }
        else
        {
            if (type.IsValueType)
            {
                builder.Indent()
                    .Append("// For value types the object must be a boxed instance; modifications affect the boxed copy")
                    .NewLine();
            }
            var targetExpr = type.IsValueType
                ? $"global::System.Runtime.CompilerServices.Unsafe.Unbox<{className}>(obj)"
                : $"global::System.Runtime.CompilerServices.Unsafe.As<{className}>(obj)";
            builder.Indent().Append("switch (name)").NewLine();
            builder.BeginScope();
            foreach (var member in writableMembers)
            {
                builder.Indent().Append("case \"").Append(member.Name).Append("\":").NewLine();
                builder.IndentLevel++;
                builder.Indent()
                    .Append(BuildWriteStatement(type, member, targetExpr, $"({member.Type})value!"))
                    .Append(';')
                    .NewLine();
                builder.Indent().Append("return;").NewLine();
                builder.IndentLevel--;
            }
            builder.Indent().Append("default:").NewLine();
            builder.IndentLevel++;
            builder.Indent()
                .Append("throw new global::System.ArgumentException(\"Writable member not found.\", nameof(name));")
                .NewLine();
            builder.IndentLevel--;
            builder.EndScope();
        }
        builder.EndScope();

        builder.EndScope();
    }

    private static void BuildFactorySource(SourceBuilder builder, TypeModel type, string className, List<MemberModel> readableMembers, List<MemberModel> writableMembers)
    {
        // Class
        builder
            .Indent()
            .Append("internal sealed class ")
            .Append(MakeSuffixedName(type.ClassName, AccessorFactorySuffix))
            .Append(" : global::BunnyTail.MemberAccessor.IAccessorFactory<")
            .Append(className)
            .Append('>')
            .NewLine();
        builder.BeginScope();

        // Singleton
        builder.Indent()
            .Append("internal static readonly ")
            .Append(MakeSuffixedName(type.ClassName, AccessorFactorySuffix))
            .Append(" Instance = new();")
            .NewLine();
        builder.NewLine();

        // Members property
        builder.Indent()
            .Append("private static readonly global::System.Collections.Generic.IReadOnlyList<global::BunnyTail.MemberAccessor.MemberDescriptor> MembersField =")
            .NewLine();
        builder.Indent()
            .Append("    [")
            .NewLine();
        builder.IndentLevel++;
        foreach (var member in type.Members)
        {
            builder.Indent()
                .Append("new global::BunnyTail.MemberAccessor.MemberDescriptor(\"")
                .Append(member.Name)
                .Append("\", typeof(")
                .Append(member.Type)
                .Append("), global::BunnyTail.MemberAccessor.MemberKind.")
                .Append(member.IsField ? "Field" : "Property")
                .Append(", ")
                .Append(member.CanRead ? "true" : "false")
                .Append(", ")
                .Append(member.CanWrite ? "true" : "false")
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

        var objectTargetExpr = type.IsValueType
            ? $"global::System.Runtime.CompilerServices.Unsafe.Unbox<{className}>(x)"
            : $"(({className})x)";

        // CreateGetter(string name) -> object
        builder.Indent()
            .Append("public global::System.Func<object, object?>? CreateGetter(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var member in readableMembers)
        {
            builder.Indent()
                .Append("\"").Append(member.Name).Append("\" => static x => ")
                .Append(BuildReadExpression(type, member, objectTargetExpr))
                .Append("!,")
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
        foreach (var member in writableMembers)
        {
            builder.Indent()
                .Append("\"").Append(member.Name).Append("\" => static (x, v) => ")
                .Append(BuildWriteStatement(type, member, objectTargetExpr, $"({member.Type})v!"))
                .Append(",")
                .NewLine();
        }
        builder.Indent().Append("_ => null").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();
        builder.NewLine();

        // CreateGetter<TProperty>(string name)
        builder.Indent()
            .Append("public global::BunnyTail.MemberAccessor.Getter<")
            .Append(className)
            .Append(", TProperty>? CreateGetter<TProperty>(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var member in readableMembers)
        {
            builder.Indent()
                .Append("\"").Append(member.Name).Append("\" => (global::BunnyTail.MemberAccessor.Getter<")
                .Append(className).Append(", TProperty>?)(object?)(global::BunnyTail.MemberAccessor.Getter<")
                .Append(className).Append(", ").Append(member.Type)
                .Append(">)(static (ref ").Append(className).Append(" x) => ")
                .Append(BuildReadExpression(type, member, "x"))
                .Append("!),")
                .NewLine();
        }
        builder.Indent().Append("_ => null").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();
        builder.NewLine();

        // CreateSetter<TProperty>(string name)
        builder.Indent()
            .Append("public global::BunnyTail.MemberAccessor.Setter<")
            .Append(className)
            .Append(", TProperty>? CreateSetter<TProperty>(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var member in writableMembers)
        {
            builder.Indent()
                .Append("\"").Append(member.Name).Append("\" => (global::BunnyTail.MemberAccessor.Setter<")
                .Append(className).Append(", TProperty>?)(object?)(global::BunnyTail.MemberAccessor.Setter<")
                .Append(className).Append(", ").Append(member.Type)
                .Append(">)(static (ref ").Append(className).Append(" x, ").Append(member.Type)
                .Append(" v) => ")
                .Append(BuildWriteStatement(type, member, "x", "v!"))
                .Append("),")
                .NewLine();
        }
        builder.Indent().Append("_ => null").NewLine();
        builder.IndentLevel--;
        builder.Indent().Append("};").NewLine();
        builder.EndScope();

        builder.EndScope();
    }

    private static string BuildReadExpression(TypeModel type, MemberModel member, string targetExpr)
    {
        if (member.GetterAccess != MemberAccess.Unsafe)
        {
            return $"{targetExpr}.{member.Name}";
        }

        var bridge = MakeSuffixedName(type.ClassName, UnsafeAccessSuffix);
        var argExpr = type.IsValueType ? $"ref {targetExpr}" : targetExpr;
        return member.IsField
            ? $"{bridge}.{BridgeFieldPrefix}{member.Name}({argExpr})"
            : $"{bridge}.{BridgeGetPrefix}{member.Name}({argExpr})";
    }

    private static string BuildWriteStatement(TypeModel type, MemberModel member, string targetExpr, string valueExpr)
    {
        if (member.SetterAccess != MemberAccess.Unsafe)
        {
            return $"{targetExpr}.{member.Name} = {valueExpr}";
        }

        var bridge = MakeSuffixedName(type.ClassName, UnsafeAccessSuffix);
        var argExpr = type.IsValueType ? $"ref {targetExpr}" : targetExpr;
        return member.IsField
            ? $"{bridge}.{BridgeFieldPrefix}{member.Name}({argExpr}) = {valueExpr}"
            : $"{bridge}.{BridgeSetPrefix}{member.Name}({argExpr}, {valueExpr})";
    }

    private static void BuildConstructorAccessorSource(SourceBuilder builder, TypeModel type, string className)
    {
        builder.Indent()
            .Append("internal sealed class ")
            .Append(MakeSuffixedName(type.ClassName, ConstructorAccessorSuffix))
            .Append(" : global::BunnyTail.MemberAccessor.IConstructor<")
            .Append(className)
            .Append('>')
            .NewLine();
        builder.BeginScope();

        // Singleton
        builder.Indent()
            .Append("internal static readonly ")
            .Append(MakeSuffixedName(type.ClassName, ConstructorAccessorSuffix))
            .Append(" Instance = new();")
            .NewLine();
        builder.NewLine();

        // Group by arity
        var byArity = type.Constructors.GroupBy(static x => x.Parameters.Count).ToDictionary(static x => x.Key, static x => x.ToArray());

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

        // Create<TArg...>(TArg... arg...) - 1 to MaxConstructorArity args
        for (var arity = 1; arity <= MaxConstructorArity; arity++)
        {
            builder.Indent().Append("public ").Append(className).Append(" Create<");
            for (var i = 0; i < arity; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(ArgTypeParameterName(arity, i));
            }
            builder.Append(">(");
            for (var i = 0; i < arity; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(ArgTypeParameterName(arity, i)).Append(' ').Append(ArgName(arity, i));
            }
            builder.Append(')').NewLine();
            builder.BeginScope();
            BuildCreateBody(builder, className, byArity, arity);
            builder.EndScope();
            if (arity < MaxConstructorArity)
            {
                builder.NewLine();
            }
        }

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

        // Single constructor bind directly
        if (constructors.Length == 1)
        {
            BuildNewExpression(builder, className, constructors[0], arity);
            return;
        }

        // Multiple constructors
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

    private static string ArgTypeParameterName(int arity, int index) => arity == 1 ? "TArg" : $"TArg{index + 1}";

    private static string ArgName(int arity, int index) => arity == 1 ? "arg" : $"arg{index + 1}";

    private static void BuildUnsafeAccessSource(SourceBuilder builder, TypeModel type)
    {
        builder.Indent()
            .Append("internal static class ")
            .Append(MakeSuffixedName(type.ClassName, UnsafeAccessSuffix))
            .NewLine();
        builder.BeginScope();

        var first = true;
        foreach (var member in type.Members.Where(static x => x.RequiresUnsafe))
        {
            var refPrefix = type.IsValueType ? "ref " : string.Empty;
            if (member.IsField)
            {
                if (!first)
                {
                    builder.NewLine();
                }
                first = false;
                builder.Indent()
                    .Append("[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Field, Name = \"")
                    .Append(member.Name)
                    .Append("\")]")
                    .NewLine();
                builder.Indent()
                    .Append("public static extern ref ")
                    .Append(member.Type)
                    .Append(' ')
                    .Append(BridgeFieldPrefix).Append(member.Name)
                    .Append('(').Append(refPrefix).Append(member.UnsafeTargetType).Append(" target);")
                    .NewLine();
            }
            else
            {
                if (member.GetterAccess == MemberAccess.Unsafe)
                {
                    if (!first)
                    {
                        builder.NewLine();
                    }
                    first = false;
                    builder.Indent()
                        .Append("[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"get_")
                        .Append(member.Name)
                        .Append("\")]")
                        .NewLine();
                    builder.Indent()
                        .Append("public static extern ")
                        .Append(member.Type)
                        .Append(' ')
                        .Append(BridgeGetPrefix).Append(member.Name)
                        .Append('(').Append(refPrefix).Append(member.UnsafeTargetType).Append(" target);")
                        .NewLine();
                }
                if (member.SetterAccess == MemberAccess.Unsafe)
                {
                    if (!first)
                    {
                        builder.NewLine();
                    }
                    first = false;
                    builder.Indent()
                        .Append("[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_")
                        .Append(member.Name)
                        .Append("\")]")
                        .NewLine();
                    builder.Indent()
                        .Append("public static extern void ")
                        .Append(BridgeSetPrefix).Append(member.Name)
                        .Append('(').Append(refPrefix).Append(member.UnsafeTargetType).Append(" target, ")
                        .Append(member.Type).Append(" value);")
                        .NewLine();
                }
            }
        }

        builder.EndScope();
    }

    private static void BuildAccessorProviderPartialSource(SourceBuilder builder, TypeModel type, string className)
    {
        var hasConstructor = type.Constructors.Count > 0;

        builder.Indent()
            .Append("partial ")
            .Append(type.TypeKeyword)
            .Append(' ')
            .Append(type.ClassName)
            .Append(" : global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(className)
            .Append('>');
        if (hasConstructor)
        {
            builder
                .Append(", global::BunnyTail.MemberAccessor.IConstructorProvider<")
                .Append(className)
                .Append('>');
        }
        builder.NewLine();
        builder.BeginScope();

        builder.Indent()
            .Append("static global::BunnyTail.MemberAccessor.IAccessor global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(className)
            .Append(">.Accessor => ")
            .Append(MakeSuffixedName(type.ClassName, AccessorSuffix))
            .Append(".Instance;")
            .NewLine();
        builder.NewLine();

        builder.Indent()
            .Append("static global::BunnyTail.MemberAccessor.IAccessorFactory<")
            .Append(className)
            .Append("> global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(className)
            .Append(">.AccessorFactory => ")
            .Append(MakeSuffixedName(type.ClassName, AccessorFactorySuffix))
            .Append(".Instance;")
            .NewLine();

        if (hasConstructor)
        {
            builder.NewLine();
            builder.Indent()
                .Append("static global::BunnyTail.MemberAccessor.IConstructor<")
                .Append(className)
                .Append("> global::BunnyTail.MemberAccessor.IConstructorProvider<")
                .Append(className)
                .Append(">.Constructor => ")
                .Append(MakeSuffixedName(type.ClassName, ConstructorAccessorSuffix))
                .Append(".Instance;")
                .NewLine();
        }

        builder.EndScope();
    }

    private static void BuildWitnessSource(SourceBuilder builder, WitnessModel witness)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        if (!String.IsNullOrEmpty(witness.Namespace))
        {
            builder.Namespace(witness.Namespace);
            builder.NewLine();
        }

        var hasConstructor = !String.IsNullOrEmpty(witness.ConstructorName);

        builder.Indent()
            .Append("partial ")
            .Append(witness.TypeKeyword)
            .Append(' ')
            .Append(witness.ClassName)
            .Append(" : global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(witness.TargetTypeName)
            .Append('>');
        if (hasConstructor)
        {
            builder
                .Append(", global::BunnyTail.MemberAccessor.IConstructorProvider<")
                .Append(witness.TargetTypeName)
                .Append('>');
        }
        builder.NewLine();
        builder.BeginScope();

        builder.Indent()
            .Append("static global::BunnyTail.MemberAccessor.IAccessor global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(witness.TargetTypeName)
            .Append(">.Accessor => ")
            .Append(witness.AccessorName)
            .Append(".Instance;")
            .NewLine();
        builder.NewLine();

        builder.Indent()
            .Append("static global::BunnyTail.MemberAccessor.IAccessorFactory<")
            .Append(witness.TargetTypeName)
            .Append("> global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(witness.TargetTypeName)
            .Append(">.AccessorFactory => ")
            .Append(witness.FactoryName)
            .Append(".Instance;")
            .NewLine();

        if (hasConstructor)
        {
            builder.NewLine();
            builder.Indent()
                .Append("static global::BunnyTail.MemberAccessor.IConstructor<")
                .Append(witness.TargetTypeName)
                .Append("> global::BunnyTail.MemberAccessor.IConstructorProvider<")
                .Append(witness.TargetTypeName)
                .Append(">.Constructor => ")
                .Append(witness.ConstructorName)
                .Append(".Instance;")
                .NewLine();
        }

        builder.EndScope();
    }

    private static void BuildRegistrySource(SourceBuilder builder, EquatableArray<RegistryTypeModel> types, EquatableArray<ClosedGenericModel> closedTypes)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.NewLine();

        // Class
        builder
            .Indent()
            .Append("internal static class AccessorFactoryInitializer")
            .NewLine();
        builder.BeginScope();

        // Method
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
                // Register members and factory
                var targetName = MakeQualifiedName(type.Namespace, type.ClassName);
                builder
                    .Indent()
                    .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterFactory(typeof(")
                    .Append(targetName)
                    .Append("), ")
                    .Append(MakeQualifiedName(type.Namespace, $"{type.ClassName}{AccessorSuffix}"))
                    .Append(".Instance, ")
                    .Append(MakeQualifiedName(type.Namespace, $"{type.ClassName}{AccessorFactorySuffix}"))
                    .Append(".Instance);")
                    .NewLine();

                // Register constructor accessor
                if (type.HasConstructors)
                {
                    builder
                        .Indent()
                        .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterConstructor<")
                        .Append(targetName)
                        .Append(">(typeof(")
                        .Append(targetName)
                        .Append("), ")
                        .Append(MakeQualifiedName(type.Namespace, $"{type.ClassName}{ConstructorAccessorSuffix}"))
                        .Append(".Instance);")
                        .NewLine();
                }
            }
            else
            {
                // Open generic type
                var prefix = OpenNamePart(type.ClassName);
                foreach (var closedType in closedTypes)
                {
                    if ((type.Namespace == closedType.Namespace) &&
                        (OpenNamePart(closedType.ClassName) == prefix) &&
                        (closedType.TypeArguments.Count == type.TypeArgumentCount))
                    {
                        var argsPart = MakeTypeArgumentsPart(closedType.TypeArguments);
                        var targetName = MakeQualifiedName(type.Namespace, $"{prefix}{argsPart}");
                        builder
                            .Indent()
                            .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterFactory(typeof(")
                            .Append(targetName)
                            .Append("), ")
                            .Append(MakeQualifiedName(type.Namespace, $"{prefix}{AccessorSuffix}{argsPart}"))
                            .Append(".Instance, ")
                            .Append(MakeQualifiedName(type.Namespace, $"{prefix}{AccessorFactorySuffix}{argsPart}"))
                            .Append(".Instance);")
                            .NewLine();

                        if (type.HasConstructors)
                        {
                            builder
                                .Indent()
                                .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterConstructor<")
                                .Append(targetName)
                                .Append(">(typeof(")
                                .Append(targetName)
                                .Append("), ")
                                .Append(MakeQualifiedName(type.Namespace, $"{prefix}{ConstructorAccessorSuffix}{argsPart}"))
                                .Append(".Instance);")
                                .NewLine();
                        }
                    }
                }

                // Open-generic accessor and factory delegates
                var openAngle = MakeOpenAnglePart(type.TypeArgumentCount);
                builder
                    .Indent()
                    .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterOpenGenericFactory(typeof(")
                    .Append(MakeQualifiedName(type.Namespace, $"{prefix}{openAngle}"))
                    .Append("),")
                    .NewLine();
                builder.IndentLevel++;
                builder
                    .Indent()
                    .Append("static typeArgs => (global::BunnyTail.MemberAccessor.IAccessor)global::System.Activator.CreateInstance(")
                    .Append("typeof(")
                    .Append(MakeQualifiedName(type.Namespace, $"{prefix}{AccessorSuffix}{openAngle}"))
                    .Append(").MakeGenericType(typeArgs))!,")
                    .NewLine();
                builder
                    .Indent()
                    .Append("static typeArgs => (global::BunnyTail.MemberAccessor.IAccessorFactory)global::System.Activator.CreateInstance(")
                    .Append("typeof(")
                    .Append(MakeQualifiedName(type.Namespace, $"{prefix}{AccessorFactorySuffix}{openAngle}"))
                    .Append(").MakeGenericType(typeArgs))!);")
                    .NewLine();
                builder.IndentLevel--;

                // Open-generic constructor accessor delegate
                if (type.HasConstructors)
                {
                    builder
                        .Indent()
                        .Append("global::BunnyTail.MemberAccessor.AccessorRegistry.RegisterOpenGenericConstructorFactory(typeof(")
                        .Append(MakeQualifiedName(type.Namespace, $"{prefix}{openAngle}"))
                        .Append("),")
                        .NewLine();
                    builder.IndentLevel++;
                    builder
                        .Indent()
                        .Append("static typeArgs => global::System.Activator.CreateInstance(")
                        .Append("typeof(")
                        .Append(MakeQualifiedName(type.Namespace, $"{prefix}{ConstructorAccessorSuffix}{openAngle}"))
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
    // Naming
    // ------------------------------------------------------------

    // "Foo" + suffix -> "Foo_Suffix", "Foo<T1, T2>" + suffix -> "Foo_Suffix<T1, T2>"
    private static string MakeSuffixedName(string className, string suffix)
    {
        var index = className.IndexOf('<');
        return index < 0
            ? $"{className}{suffix}"
            : $"{className.Substring(0, index)}{suffix}{className.Substring(index)}";
    }

    private static string MakeQualifiedName(string ns, string name) =>
        String.IsNullOrEmpty(ns) ? $"global::{name}" : $"global::{ns}.{name}";

    private static string OpenNamePart(string className)
    {
        var index = className.IndexOf('<');
        return index < 0 ? className : className.Substring(0, index);
    }

    private static string MakeTypeArgumentsPart(EquatableArray<string> typeArguments) =>
        $"<{String.Join(", ", typeArguments)}>";

    private static string MakeOpenAnglePart(int count) =>
        $"<{new string(',', count - 1)}>";

    private static string MakeTypeKey(TypeModel type) =>
        $"{type.Namespace}/{type.ClassName}";

    private static string MakeClosedTypeKey(ClosedGenericModel closedType) =>
        $"{closedType.Namespace}/{OpenNamePart(closedType.ClassName)}{MakeTypeArgumentsPart(closedType.TypeArguments)}";

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

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

    private static string MakeWitnessFilename(WitnessModel witness)
    {
        var buffer = new StringBuilder();

        if (!String.IsNullOrEmpty(witness.Namespace))
        {
            buffer.Append(witness.Namespace.Replace('.', '_'));
            buffer.Append('_');
        }

        buffer.Append(OpenNamePart(witness.ClassName));
        buffer.Append("_Provider_");
        foreach (var c in witness.TargetTypeName)
        {
            buffer.Append(Char.IsLetterOrDigit(c) ? c : '_');
        }
        buffer.Append(".g.cs");

        return buffer.ToString();
    }
}
