namespace BunnyTail.MemberAccessor.Generator;

using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;

using BunnyTail.MemberAccessor.Generator.Models;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using SourceGenerateHelper;

[Generator]
public sealed class AccessorGenerator : IIncrementalGenerator
{
    private const string GenerateAccessorAttributeName = "BunnyTail.MemberAccessor.GenerateAccessorAttribute";
    private const string TypedAccessorAttributeName = "BunnyTail.MemberAccessor.TypedAccessorAttribute";
    private const string GenerateAccessorForAttributeName = "BunnyTail.MemberAccessor.GenerateAccessorForAttribute";
    private const string AccessorMemberAttributeName = "BunnyTail.MemberAccessor.AccessorMemberAttribute";
    private const string SetsRequiredMembersAttributeName = "System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute";

    private const string AccessorSuffix = "_Accessor";
    private const string AccessorFactorySuffix = "_AccessorFactory";
    private const string ConstructorAccessorSuffix = "_ConstructorAccessor";
    private const string UnsafeAccessSuffix = "_UnsafeAccess";

    private const string BridgeGetPrefix = "Get_";
    private const string BridgeSetPrefix = "Set_";
    private const string BridgeFieldPrefix = "Field_";

    // Maximum constructor arity supported by IConstructor<T>.Create overloads
    private const int MaxConstructorArity = 16;

    private static readonly SymbolDisplayFormat ExpandedTupleFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

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

        var treeProvider = context.ForAttributeWithMetadataNameSyntaxTrees(
                GenerateAccessorAttributeName,
                static (syntax, _) => IsTypeSyntax(syntax))
            .Combine(context.ForAttributeWithMetadataNameSyntaxTrees(TypedAccessorAttributeName, static (_, _) => true))
            .Combine(context.ForAttributeWithMetadataNameSyntaxTrees(GenerateAccessorForAttributeName, static (_, _) => true))
            .Select(static (trees, _) => trees.Left.Left.AddRange(trees.Left.Right).AddRange(trees.Right));

        context.RegisterSourceOutput(
            typeProvider.Combine(closedGenericProvider).Combine(externalProvider).Combine(treeProvider),
            static (context, provider) => ReportDiagnostics(context, provider.Left.Left.Left, provider.Left.Left.Right, provider.Left.Right, provider.Right));

        var collisionProvider = typeProvider
            .Combine(externalProvider)
            .Select(static (provider, _) => new EquatableArray<string>(FindHintNameCollisions(provider.Left, provider.Right).Select(static x => x.HintName)))
            .WithTrackingName("Collisions");

        var models = typeProvider.SelectMany(static (types, _) => types.SelectValue().ToImmutableArray());
        context.RegisterImplementationSourceOutput(
            models.Combine(collisionProvider),
            static (context, provider) => ExecuteClass(context, provider.Left, provider.Right));

        var typeKeyProvider = typeProvider
            .Select(static (types, _) => new EquatableArray<string>(types.SelectValue().Select(MakeTypeKey)))
            .WithTrackingName("TypeKeys");
        context.RegisterImplementationSourceOutput(
            externalProvider.Combine(typeKeyProvider).Combine(collisionProvider),
            static (context, provider) => ExecuteExternal(context, provider.Left.Left, provider.Left.Right, provider.Right));

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
        var location = syntax.Identifier.GetLocation();

        if (symbol.IsStatic || symbol.IsRefLikeType)
        {
            return Results.Error<TypeModel>(new DiagnosticInfo(Diagnostics.UnsupportedType, location, symbol.Name));
        }

        var isPartial = syntax.Modifiers.Any(SyntaxKind.PartialKeyword);
        var supportsGenericUnsafe = SupportsGenericUnsafeAccessor(context.TargetNode.SyntaxTree);

        return GetTypeModel(symbol, location, isPartial, sameAssembly: true, supportsGenericUnsafe, context.SemanticModel.Compilation);
    }

    private static Result<TypeModel> GetTypeModel(INamedTypeSymbol symbol, Location? location, bool isPartial, bool sameAssembly, bool supportsGenericUnsafe, Compilation compilation)
    {
        var ns = GetNamespace(symbol);

        // Collect containing types
        var containingSymbols = symbol.GetContainingTypes();
        if (containingSymbols.Count > 0)
        {
            if ((symbol.Arity > 0) || containingSymbols.Any(static x => x.Arity > 0))
            {
                return Results.Error<TypeModel>(new DiagnosticInfo(Diagnostics.NestedGenericTypeNotSupported, location, symbol.Name));
            }

            if (!IsAccessibleFromNamespace(symbol) || containingSymbols.Any(static x => !IsAccessibleFromNamespace(x) || x.IsFileLocal))
            {
                return Results.Error<TypeModel>(new DiagnosticInfo(Diagnostics.NestedTypeNotAccessible, location, symbol.Name));
            }
        }

        if (symbol.IsFileLocal)
        {
            return Results.Error<TypeModel>(new DiagnosticInfo(Diagnostics.NestedTypeNotAccessible, location, symbol.Name));
        }

        var containingTypes = containingSymbols
            .Select(static x => new ContainingTypeModel(x.GetClassName(), x.GetDeclarationKeyword(), IsPartialType(x)))
            .ToArray();

        // Collect instance members
        var members = CollectMembers(symbol, sameAssembly, out var hasRequiredMembers);

        // Collect constructors
        var publicConstructors = new List<IMethodSymbol>();
        if (!symbol.IsAbstract)
        {
            foreach (var constructor in symbol.InstanceConstructors)
            {
                if (IsSupportedConstructor(constructor, hasRequiredMembers))
                {
                    if (constructor.Parameters.Length > MaxConstructorArity)
                    {
                        return Results.Error<TypeModel>(new DiagnosticInfo(
                            Diagnostics.UnsupportedConstructorArity,
                            location,
                            symbol.Name,
                            MaxConstructorArity.ToString(CultureInfo.InvariantCulture)));
                    }

                    publicConstructors.Add(constructor);
                }
            }
        }

        var orderedConstructors = OrderConstructors(publicConstructors, compilation);
        var constructors = new ConstructorModel[orderedConstructors.Count];
        var hasDeclaredConstructor = false;
        for (var i = 0; i < orderedConstructors.Count; i++)
        {
            var constructor = orderedConstructors[i];
            var parameters = new ConstructorParameterModel[constructor.Parameters.Length];
            for (var j = 0; j < parameters.Length; j++)
            {
                parameters[j] = CreateParameterModel(constructor.Parameters[j]);
            }

            constructors[i] = new ConstructorModel(new EquatableArray<ConstructorParameterModel>(parameters));
            hasDeclaredConstructor |= !constructor.IsImplicitlyDeclared;
        }

        var className = symbol.GetClassName();
        var diagnostics = new List<DiagnosticInfo>();

        if ((members.Count == 0) && !hasDeclaredConstructor)
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.NoAccessibleMembers, location, className));
        }

        // UnsafeAccessor with generic parameters requires .NET 9 or later
        if ((symbol.Arity > 0) && !supportsGenericUnsafe && members.Any(static x => x.RequiresUnsafe))
        {
            diagnostics.Add(new DiagnosticInfo(Diagnostics.GenericUnsafeAccessorNotSupported, location, className));
        }

        var model = new TypeModel(
            ns,
            className,
            new EquatableArray<ContainingTypeModel>(containingTypes),
            symbol.IsValueType,
            symbol.GetDeclarationKeyword(),
            new EquatableArray<string>(symbol.TypeParameters.Select(static x => x.Name).ToArray()),
            MakeConstraintClauses(symbol),
            isPartial,
            new EquatableArray<ConstructorModel>(constructors),
            new EquatableArray<MemberModel>(members));
        return new Result<TypeModel>(model, new EquatableArray<DiagnosticInfo>(diagnostics));
    }

    private static List<MemberModel> CollectMembers(INamedTypeSymbol symbol, bool sameAssembly, out bool hasRequiredMembers)
    {
        var members = new List<MemberModel>();
        HashSet<string>? hidden = null;
        hasRequiredMembers = false;
        for (var current = symbol; (current is not null) && (current.SpecialType != SpecialType.System_Object); current = current.BaseType)
        {
            var internalVisible = sameAssembly && SymbolEqualityComparer.Default.Equals(current.ContainingAssembly, symbol.ContainingAssembly);

            var hasBase = current.BaseType is { SpecialType: not (SpecialType.System_Object or SpecialType.System_ValueType) };
            var declared = hasBase ? new List<string>() : null;
            foreach (var member in current.GetMembers())
            {
                hasRequiredMembers |= member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true };

                if ((hidden is not null) && hidden.Contains(member.Name))
                {
                    continue;
                }

                if (IsVisible(member.DeclaredAccessibility, internalVisible))
                {
                    declared?.Add(member.Name);
                }

                if (member is IPropertySymbol property)
                {
                    if (property.IsStatic || property.IsIndexer || !IsSupportedType(property.Type) || IsObsoleteError(property))
                    {
                        continue;
                    }

                    var (ignore, optIn) = GetMemberAttributeInfo(property);
                    if (ignore)
                    {
                        declared?.Add(property.Name);
                        continue;
                    }

                    var getterAccess = ClassifyAccessor(property.GetMethod, optIn, internalVisible);
                    var setterAccess = ((property.SetMethod is not null) && property.SetMethod.IsInitOnly)
                        ? MemberAccess.None
                        : ClassifyAccessor(property.SetMethod, optIn, internalVisible);
                    if ((getterAccess == MemberAccess.None) && (setterAccess == MemberAccess.None))
                    {
                        continue;
                    }

                    declared?.Add(property.Name);
                    members.Add(CreateMemberModel(property, property.Type, false, getterAccess, setterAccess));
                }
                else if (member is IFieldSymbol field)
                {
                    if (field.IsStatic || field.IsConst || field.IsImplicitlyDeclared || (field.AssociatedSymbol is not null) ||
                        !IsSupportedType(field.Type) || IsObsoleteError(field))
                    {
                        continue;
                    }

                    var (ignore, optIn) = GetMemberAttributeInfo(field);
                    if (ignore)
                    {
                        declared?.Add(field.Name);
                        continue;
                    }

                    var access = ClassifyAccessibility(field.DeclaredAccessibility, optIn, internalVisible);
                    if (access == MemberAccess.None)
                    {
                        continue;
                    }

                    declared?.Add(field.Name);
                    members.Add(CreateMemberModel(field, field.Type, true, access, field.IsReadOnly ? MemberAccess.None : access));
                }
            }

            if (hidden is null)
            {
                hidden = declared is not null ? new HashSet<string>(declared, StringComparer.Ordinal) : null;
            }
            else if (declared is not null)
            {
                hidden.UnionWith(declared);
            }
        }

        return members;
    }

    private static MemberModel CreateMemberModel(ISymbol member, ITypeSymbol type, bool isField, MemberAccess getterAccess, MemberAccess setterAccess)
    {
        var unsafeTargetType = ((getterAccess == MemberAccess.Unsafe) || (setterAccess == MemberAccess.Unsafe))
            ? member.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            : string.Empty;
        return new MemberModel(
            type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable),
            type.ToTypeOfName(),
            member.Name,
            isField,
            getterAccess,
            setterAccess,
            unsafeTargetType);
    }

    private static bool IsSupportedType(ITypeSymbol type) =>
        !type.IsRefLikeType && (type.TypeKind is not (TypeKind.Pointer or TypeKind.FunctionPointer));

    private static bool IsObsoleteError(ISymbol symbol) =>
        symbol.IsObsolete(out var isError) && isError;

    private static bool IsSupportedConstructor(IMethodSymbol constructor, bool hasRequiredMembers)
    {
        if ((constructor.DeclaredAccessibility != Accessibility.Public) ||
            IsObsoleteError(constructor) ||
            (hasRequiredMembers && !constructor.HasAttribute(SetsRequiredMembersAttributeName)))
        {
            return false;
        }

        foreach (var parameter in constructor.Parameters)
        {
            if ((parameter.RefKind is not (RefKind.None or RefKind.In)) || !IsSupportedType(parameter.Type))
            {
                return false;
            }
        }

        return true;
    }

    private static List<IMethodSymbol> OrderConstructors(List<IMethodSymbol> constructors, Compilation compilation)
    {
        var ordered = new List<IMethodSymbol>(constructors.Count);
        foreach (var constructor in constructors.OrderBy(static x => x.Parameters.Length))
        {
            var index = ordered.Count;
            for (var i = 0; i < ordered.Count; i++)
            {
                if ((ordered[i].Parameters.Length == constructor.Parameters.Length) && IsMoreSpecific(constructor, ordered[i], compilation))
                {
                    index = i;
                    break;
                }
            }

            ordered.Insert(index, constructor);
        }

        return ordered;
    }

    private static bool IsMoreSpecific(IMethodSymbol constructor, IMethodSymbol other, Compilation compilation)
    {
        if (compilation is not CSharpCompilation csharp)
        {
            return false;
        }

        var more = false;
        for (var i = 0; i < constructor.Parameters.Length; i++)
        {
            var type = constructor.Parameters[i].Type;
            var otherType = other.Parameters[i].Type;
            if (SymbolEqualityComparer.Default.Equals(type, otherType))
            {
                continue;
            }

            var conversion = csharp.ClassifyConversion(type, otherType);
            if (!conversion.IsImplicit || !(conversion.IsIdentity || conversion.IsReference || conversion.IsBoxing))
            {
                return false;
            }

            more |= !conversion.IsIdentity;
        }

        return more;
    }

    private static ConstructorParameterModel CreateParameterModel(IParameterSymbol parameter)
    {
        var type = parameter.Type;

        // Nullable<T> is matched by its underlying type at runtime
        var isNullable = (type is INamedTypeSymbol named) && (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T);
        var checkType = isNullable ? ((INamedTypeSymbol)type).TypeArguments[0] : type;

        return new ConstructorParameterModel(
            type.ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable),
            type.ToTypeOfName(),
            parameter.Name,
            checkType.TypeKind == TypeKind.Dynamic ? "object" : checkType.ToDisplayString(ExpandedTupleFormat),
            isNullable || !type.IsValueType);
    }

    private static (bool Ignore, bool OptIn) GetMemberAttributeInfo(ISymbol member)
    {
        var attribute = member.FindAttribute(AccessorMemberAttributeName);
        if (attribute is null)
        {
            return (false, false);
        }

        var ignore = attribute.TryGetNamedArgument<bool>("Ignore", out var value) && value;
        return (ignore, !ignore);
    }

    private static bool IsVisible(Accessibility accessibility, bool internalVisible) =>
        (accessibility == Accessibility.Public) ||
        (internalVisible && (accessibility is Accessibility.Internal or Accessibility.ProtectedOrInternal));

    private static MemberAccess ClassifyAccessor(IMethodSymbol? method, bool optIn, bool internalVisible) =>
        method is null ? MemberAccess.None : ClassifyAccessibility(method.DeclaredAccessibility, optIn, internalVisible);

    private static MemberAccess ClassifyAccessibility(Accessibility accessibility, bool optIn, bool internalVisible)
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
            Accessibility.Internal or Accessibility.ProtectedOrInternal => internalVisible ? MemberAccess.Direct : MemberAccess.Unsafe,
            _ => MemberAccess.Unsafe
        };
    }

    private static string MakeConstraintClauses(INamedTypeSymbol symbol)
    {
        var builder = new StringBuilder();
        foreach (var typeParameter in symbol.TypeParameters)
        {
            var constraints = new List<string>();
            if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints.Add(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
            }
            else if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (typeParameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            for (var i = 0; i < typeParameter.ConstraintTypes.Length; i++)
            {
                constraints.Add(typeParameter.ConstraintTypes[i]
                    .WithNullableAnnotation(typeParameter.ConstraintNullableAnnotations[i])
                    .ToDisplayString(SymbolDisplayFormats.FullyQualifiedNullable));
            }

            if (typeParameter.HasConstructorConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                builder.Append(" where ").Append(CSharpIdentifier.EscapeTypeName(typeParameter.Name)).Append(" : ").Append(String.Join(", ", constraints));
            }
        }

        return builder.ToString();
    }

    // ------------------------------------------------------------
    // Parser : ClosedGenericModel
    // ------------------------------------------------------------

    private static EquatableArray<Result<ClosedGenericModel>> GetClosedGenericModel(GeneratorAttributeSyntaxContext context)
    {
        var list = new List<Result<ClosedGenericModel>>();
        var openGenericSymbol = (context.TargetSymbol as INamedTypeSymbol)?.OriginalDefinition;

        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var data in context.Attributes)
        {
            list.Add(GetClosedGenericModel(openGenericSymbol, data));
        }

        return new(list);
    }

    private static Result<ClosedGenericModel> GetClosedGenericModel(INamedTypeSymbol? openGenericSymbol, AttributeData attributeData)
    {
        if (!attributeData.TryGetConstructorArgument<INamedTypeSymbol>(0, out var symbol) || (symbol.TypeKind == TypeKind.Error))
        {
            return Results.Errors<ClosedGenericModel>();
        }

        var location = attributeData.ApplicationSyntaxReference?.GetSyntax().GetLocation();
        if (!symbol.IsGenericType)
        {
            return Results.Error<ClosedGenericModel>(new DiagnosticInfo(Diagnostics.InvalidTypeArgument, location, symbol.Name));
        }

        if ((openGenericSymbol is not null) && !SymbolEqualityComparer.Default.Equals(openGenericSymbol, symbol.OriginalDefinition))
        {
            return Results.Error<ClosedGenericModel>(new DiagnosticInfo(Diagnostics.InvalidAttributeLocation, location, symbol.Name));
        }

        // The target type must be decorated with GenerateAccessorAttribute
        if (!symbol.OriginalDefinition.HasAttribute(GenerateAccessorAttributeName))
        {
            return Results.Error<ClosedGenericModel>(new DiagnosticInfo(Diagnostics.TypedAccessorTargetNotDecorated, location, symbol.Name));
        }

        return Results.Success(CreateClosedGenericModel(symbol));
    }

    private static ClosedGenericModel CreateClosedGenericModel(INamedTypeSymbol symbol) =>
        new(
            GetNamespace(symbol),
            symbol.GetClassName(),
            new EquatableArray<string>(symbol.TypeArguments.Select(static x => x.ToDisplayString(ExpandedTupleFormat)).ToArray()));

    // ------------------------------------------------------------
    // Parser : ExternalModel
    // ------------------------------------------------------------

    private static EquatableArray<Result<ExternalModel>> GetExternalModels(GeneratorAttributeSyntaxContext context)
    {
        var list = new List<Result<ExternalModel>>();
        var compilation = context.SemanticModel.Compilation;
        var supportsGenericUnsafe = SupportsGenericUnsafeAccessor(context.TargetNode.SyntaxTree);

        // Provider type information
        var providerSymbol = context.TargetSymbol as INamedTypeSymbol;
        var providerSyntax = context.TargetNode as TypeDeclarationSyntax;
        var canImplementProvider = providerSymbol is { IsStatic: false, IsFileLocal: false } &&
                                   (providerSyntax is not null) && providerSyntax.Modifiers.Any(SyntaxKind.PartialKeyword) &&
                                   providerSymbol.GetContainingTypes().All(IsPartialType);
        var providerNotPartialReported = false;

        foreach (var data in context.Attributes)
        {
            if (!data.TryGetConstructorArgument(0, out var argument) || (argument.Value is ITypeSymbol { TypeKind: TypeKind.Error }))
            {
                continue;
            }

            var location = data.ApplicationSyntaxReference?.GetSyntax().GetLocation();
            if (argument.Value is not INamedTypeSymbol target)
            {
                list.Add(Results.Error<ExternalModel>(new DiagnosticInfo(Diagnostics.InvalidExternalTarget, location, argument.Value?.ToString() ?? "unknown")));
                continue;
            }

            if ((target.TypeKind is not (TypeKind.Class or TypeKind.Struct)) ||
                target.IsStatic ||
                target.IsRefLikeType ||
                target.IsFileLocal ||
                (target.ContainingType is not null) ||
                !compilation.IsSymbolAccessibleWithin(target.OriginalDefinition, compilation.Assembly))
            {
                list.Add(Results.Error<ExternalModel>(new DiagnosticInfo(Diagnostics.InvalidExternalTarget, location, target.Name)));
                continue;
            }

            var sameAssembly = SymbolEqualityComparer.Default.Equals(target.ContainingAssembly, compilation.Assembly);
            var hasGenerateAccessor = sameAssembly && target.OriginalDefinition.HasAttribute(GenerateAccessorAttributeName);

            var typeResult = GetTypeModel(target.OriginalDefinition, location, isPartial: false, sameAssembly, supportsGenericUnsafe, compilation);
            var diagnostics = hasGenerateAccessor ? [] : typeResult.Diagnostics.ToList();
            if (!typeResult.HasValue)
            {
                list.Add(Results.Errors<ExternalModel>(diagnostics));
                continue;
            }

            var typeModel = typeResult.Value;

            ClosedGenericModel? closedGeneric = null;
            if (target.IsGenericType && !target.IsUnboundGenericType)
            {
                closedGeneric = CreateClosedGenericModel(target);
            }

            // Provider implementation is only possible for closed types
            ProviderModel? provider = null;
            var closedTarget = !target.IsGenericType || (closedGeneric is not null);
            if ((providerSymbol is not null) && closedTarget)
            {
                if (canImplementProvider)
                {
                    provider = CreateProviderModel(providerSymbol, typeModel, closedGeneric);
                }
                else if (!providerNotPartialReported)
                {
                    providerNotPartialReported = true;
                    diagnostics.Add(new DiagnosticInfo(Diagnostics.TypeNotPartial, providerSyntax?.Identifier.GetLocation() ?? location, providerSymbol.Name));
                }
            }

            if (hasGenerateAccessor && (provider is null) && (closedGeneric is null))
            {
                diagnostics.Add(new DiagnosticInfo(Diagnostics.AccessorAlreadyGenerated, location, target.Name));
            }

            list.Add(new Result<ExternalModel>(
                new ExternalModel(typeModel, closedGeneric, provider, hasGenerateAccessor),
                new EquatableArray<DiagnosticInfo>(diagnostics)));
        }

        return new(list);
    }

    private static ProviderModel CreateProviderModel(INamedTypeSymbol providerSymbol, TypeModel typeModel, ClosedGenericModel? closedGeneric)
    {
        var providerNs = GetNamespace(providerSymbol);
        var providerKeyword = providerSymbol.GetDeclarationKeyword();

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

        var containingTypes = providerSymbol.GetContainingTypes()
            .Select(static x => new ContainingTypeModel(x.GetClassName(), x.GetDeclarationKeyword(), true))
            .ToArray();

        return new ProviderModel(
            providerNs,
            providerSymbol.GetClassName(),
            new EquatableArray<ContainingTypeModel>(containingTypes),
            providerKeyword,
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
        var collisions = new HashSet<string>(FindHintNameCollisions(types, externals).Select(static x => x.HintName), StringComparer.Ordinal);
        var dropped = new HashSet<string>(
            types.SelectValue().Where(x => collisions.Contains(MakeClassHintName(x))).Select(static x => MakeOpenKey(x.Namespace, x.ClassName, x.TypeParameters.Count)),
            StringComparer.Ordinal);
        var targetTypes = types.SelectValue().Where(x => !collisions.Contains(MakeClassHintName(x))).Select(MakeRegistryType).ToList();
        var closedTypes = closedGenerics.SelectMany(static x => x.SelectValue()).ToList();

        // Merge external targets
        var typeKeys = new HashSet<string>(types.SelectValue().Select(MakeTypeKey), StringComparer.Ordinal);
        foreach (var external in externals.SelectMany(static x => x.SelectValue()))
        {
            if (!external.TargetHasGenerateAccessor && typeKeys.Add(MakeTypeKey(external.Type)) && !collisions.Contains(MakeClassHintName(external.Type)))
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
            new EquatableArray<RegistryTypeModel>(targetTypes),
            new EquatableArray<ClosedGenericModel>(closedTypes.Where(x => !dropped.Contains(MakeOpenKey(x.Namespace, x.ClassName, x.TypeArguments.Count)) && closedKeys.Add(MakeClosedTypeKey(x)))));

        static RegistryTypeModel MakeRegistryType(TypeModel type) =>
            new(type.Namespace, MakeTypePath(type), MakeFlatName(type), type.TypeParameters.Count, type.Constructors.Count > 0);
    }

    // ------------------------------------------------------------
    // Parser : Shared
    // ------------------------------------------------------------

    private static bool SupportsGenericUnsafeAccessor(SyntaxTree tree) =>
        (tree.Options is CSharpParseOptions options) && options.PreprocessorSymbolNames.Contains("NET9_0_OR_GREATER");

    private static string GetNamespace(INamedTypeSymbol symbol) =>
        String.IsNullOrEmpty(symbol.ContainingNamespace.Name) ? string.Empty : symbol.ContainingNamespace.ToDisplayString();

    private static bool IsPartialType(INamedTypeSymbol symbol) =>
        symbol.DeclaringSyntaxReferences.Any(static x => (x.GetSyntax() is TypeDeclarationSyntax syntax) && syntax.Modifiers.Any(SyntaxKind.PartialKeyword));

    private static bool IsAccessibleFromNamespace(INamedTypeSymbol symbol) =>
        symbol.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal;

    // ------------------------------------------------------------
    // Diagnostics
    // ------------------------------------------------------------

    private static void ReportDiagnostics(
        SourceProductionContext context,
        ImmutableArray<Result<TypeModel>> types,
        ImmutableArray<EquatableArray<Result<ClosedGenericModel>>> closedGenerics,
        ImmutableArray<EquatableArray<Result<ExternalModel>>> externals,
        ImmutableArray<SyntaxTree> trees)
    {
        var diagnostics = types.SelectError()
            .Concat(closedGenerics.SelectMany(static x => x.SelectError()))
            .Concat(externals.SelectMany(static x => x.SelectError()))
            .Concat(FindHintNameCollisions(types, externals).Select(static x => new DiagnosticInfo(Diagnostics.HintNameCollision, (Location?)null, x.Name, x.Other)))
            .Distinct();
        context.ReportDiagnostics(diagnostics, trees);
    }

    private static List<(string HintName, string Name, string Other)> FindHintNameCollisions(
        ImmutableArray<Result<TypeModel>> types,
        ImmutableArray<EquatableArray<Result<ExternalModel>>> externals)
    {
        var files = new List<(string HintName, string Name)>();
        foreach (var type in types.SelectValue())
        {
            files.Add((MakeClassHintName(type), MakeDisplayName(type.Namespace, MakeTypePath(type))));
        }

        foreach (var external in externals.SelectMany(static x => x.SelectValue()))
        {
            if (!external.TargetHasGenerateAccessor)
            {
                files.Add((MakeClassHintName(external.Type), MakeDisplayName(external.Type.Namespace, MakeTypePath(external.Type))));
            }

            if (external.Provider is { } provider)
            {
                files.Add((MakeProviderFilename(provider), MakeDisplayName(provider.Namespace, MakeProviderPath(provider))));
            }
        }

        var collisions = new List<(string HintName, string Name, string Other)>();
        var firsts = new Dictionary<string, (string HintName, string Name)>(StringComparer.OrdinalIgnoreCase);
        var reported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files.OrderBy(static x => x.HintName, StringComparer.Ordinal))
        {
            if (!firsts.TryGetValue(file.HintName, out var first))
            {
                firsts.Add(file.HintName, file);
            }
            else if ((first.HintName != file.HintName) && reported.Add(file.HintName))
            {
                collisions.Add((file.HintName, file.Name, first.Name));
            }
        }

        return collisions;
    }

    // ------------------------------------------------------------
    // Generator
    // ------------------------------------------------------------

    private static void ExecuteClass(SourceProductionContext context, TypeModel type, EquatableArray<string> collisions)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var hintName = MakeClassHintName(type);
        if (collisions.Contains(hintName))
        {
            return;
        }

        var builder = new SourceBuilder();
        BuildClassSource(builder, type);

        context.AddSource(hintName, builder);
    }

    private static void ExecuteExternal(
        SourceProductionContext context,
        ImmutableArray<EquatableArray<Result<ExternalModel>>> externals,
        EquatableArray<string> typeKeys,
        EquatableArray<string> collisions)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var emitted = new HashSet<string>(typeKeys, StringComparer.Ordinal);
        var providerEmitted = new HashSet<string>(StringComparer.Ordinal);
        foreach (var external in externals.SelectMany(static x => x.SelectValue()))
        {
            if (!external.TargetHasGenerateAccessor && emitted.Add(MakeTypeKey(external.Type)))
            {
                var hintName = MakeClassHintName(external.Type);
                if (!collisions.Contains(hintName))
                {
                    var builder = new SourceBuilder();
                    BuildClassSource(builder, external.Type);

                    context.AddSource(hintName, builder);
                }
            }

            if ((external.Provider is { } provider) && !collisions.Contains(MakeClassHintName(external.Type)))
            {
                var providerKey = $"{provider.Namespace}/{MakeProviderPath(provider)}::{provider.TargetTypeName}";
                var hintName = MakeProviderFilename(provider);
                if (providerEmitted.Add(providerKey) && !collisions.Contains(hintName))
                {
                    var builder = new SourceBuilder();
                    BuildProviderSource(builder, provider);

                    context.AddSource(hintName, builder);
                }
            }
        }
    }

    private static void ExecuteRegistry(SourceProductionContext context, RegistryModel model)
    {
        context.CancellationToken.ThrowIfCancellationRequested();

        var builder = new SourceBuilder();
        BuildRegistrySource(builder, model.Types, model.ClosedTypes);
        context.AddSource("AccessorInitializer.g.cs", builder);
    }

    // ------------------------------------------------------------
    // Build
    // ------------------------------------------------------------

    private static void BuildClassSource(SourceBuilder builder, TypeModel type)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Disable("CS0612, CS0618");
        builder.NewLine();

        var className = MakeQualifiedName(type.Namespace, MakeTypePath(type));
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
        if (type.IsPartial && type.ContainingTypes.All(static x => x.IsPartial))
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
            .Append(MakeSuffixedName(MakeFlatName(type), AccessorSuffix))
            .Append(" : global::BunnyTail.MemberAccessor.IAccessor")
            .Append(type.Constraints)
            .NewLine();
        builder.BeginScope();

        // Singleton
        builder.Indent()
            .Append("internal static readonly ")
            .Append(MakeSuffixedName(MakeFlatName(type), AccessorSuffix))
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
            .Append(MakeSuffixedName(MakeFlatName(type), AccessorFactorySuffix))
            .Append(" : global::BunnyTail.MemberAccessor.IAccessorFactory<")
            .Append(className)
            .Append('>')
            .Append(type.Constraints)
            .NewLine();
        builder.BeginScope();

        // Singleton
        builder.Indent()
            .Append("internal static readonly ")
            .Append(MakeSuffixedName(MakeFlatName(type), AccessorFactorySuffix))
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
                .Append(member.TypeOfName)
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

        var propertyType = MakeTypeParameterName(type, "TProperty");
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
            .Append(", ").Append(propertyType).Append(">? CreateGetter<").Append(propertyType).Append(">(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var member in readableMembers)
        {
            builder.Indent()
                .Append("\"").Append(member.Name).Append("\" => (global::BunnyTail.MemberAccessor.Getter<")
                .Append(className).Append(", ").Append(propertyType).Append(">?)(object?)(global::BunnyTail.MemberAccessor.Getter<")
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
            .Append(", ").Append(propertyType).Append(">? CreateSetter<").Append(propertyType).Append(">(string name)")
            .NewLine();
        builder.BeginScope();
        builder.Indent().Append("return name switch").NewLine();
        builder.BeginScope();
        foreach (var member in writableMembers)
        {
            builder.Indent()
                .Append("\"").Append(member.Name).Append("\" => (global::BunnyTail.MemberAccessor.Setter<")
                .Append(className).Append(", ").Append(propertyType).Append(">?)(object?)(global::BunnyTail.MemberAccessor.Setter<")
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
            return $"{targetExpr}.{CSharpIdentifier.Escape(member.Name)}";
        }

        var bridge = MakeSuffixedName(MakeFlatName(type), UnsafeAccessSuffix);
        var argExpr = type.IsValueType ? $"ref {targetExpr}" : targetExpr;
        return member.IsField
            ? $"{bridge}.{BridgeFieldPrefix}{member.Name}({argExpr})"
            : $"{bridge}.{BridgeGetPrefix}{member.Name}({argExpr})";
    }

    private static string BuildWriteStatement(TypeModel type, MemberModel member, string targetExpr, string valueExpr)
    {
        if (member.SetterAccess != MemberAccess.Unsafe)
        {
            return $"{targetExpr}.{CSharpIdentifier.Escape(member.Name)} = {valueExpr}";
        }

        var bridge = MakeSuffixedName(MakeFlatName(type), UnsafeAccessSuffix);
        var argExpr = type.IsValueType ? $"ref {targetExpr}" : targetExpr;
        return member.IsField
            ? $"{bridge}.{BridgeFieldPrefix}{member.Name}({argExpr}) = {valueExpr}"
            : $"{bridge}.{BridgeSetPrefix}{member.Name}({argExpr}, {valueExpr})";
    }

    private static void BuildConstructorAccessorSource(SourceBuilder builder, TypeModel type, string className)
    {
        builder.Indent()
            .Append("internal sealed class ")
            .Append(MakeSuffixedName(MakeFlatName(type), ConstructorAccessorSuffix))
            .Append(" : global::BunnyTail.MemberAccessor.IConstructor<")
            .Append(className)
            .Append('>')
            .Append(type.Constraints)
            .NewLine();
        builder.BeginScope();

        // Singleton
        builder.Indent()
            .Append("internal static readonly ")
            .Append(MakeSuffixedName(MakeFlatName(type), ConstructorAccessorSuffix))
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
            var argTypes = MakeArgTypeParameterNames(type, arity);
            builder.Indent().Append("public ").Append(className).Append(" Create<");
            for (var i = 0; i < arity; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(argTypes[i]);
            }
            builder.Append(">(");
            for (var i = 0; i < arity; i++)
            {
                if (i > 0)
                {
                    builder.Append(", ");
                }
                builder.Append(argTypes[i]).Append(' ').Append(ArgName(arity, i));
            }
            builder.Append(')').NewLine();
            builder.BeginScope();
            BuildCreateBody(builder, className, byArity, argTypes);
            builder.EndScope();
            builder.NewLine();
        }

        // CreateInstance(params object?[]) - reflection-style creation
        BuildCreateInstanceSource(builder, className, byArity);

        builder.EndScope();
    }

    private static void BuildCreateInstanceSource(SourceBuilder builder, string className, Dictionary<int, ConstructorModel[]> byArity)
    {
        builder.Indent().Append("public object CreateInstance(params object?[] args)").NewLine();
        builder.BeginScope();
        builder.Indent().Append("switch (args?.Length ?? 0)").NewLine();
        builder.BeginScope();
        foreach (var pair in byArity.OrderBy(static x => x.Key))
        {
            var arity = pair.Key;
            var constructors = pair.Value;
            builder.Indent().Append("case ").Append(arity.ToString(CultureInfo.InvariantCulture)).Append(':').NewLine();
            builder.IndentLevel++;

            // Single constructor bind directly
            if (constructors.Length == 1)
            {
                BuildNewInstanceExpression(builder, className, constructors[0]);
            }
            else
            {
                // Multiple constructors matched by runtime argument type
                foreach (var ctor in constructors)
                {
                    builder.Indent().Append("if (");
                    for (var i = 0; i < arity; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(" && ");
                        }
                        var parameter = ctor.Parameters[i];
                        if (parameter.AllowsNull)
                        {
                            builder.Append("((args![").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] is null) || (args[")
                                .Append(i.ToString(CultureInfo.InvariantCulture)).Append("] is ").Append(parameter.CheckType).Append("))");
                        }
                        else
                        {
                            builder.Append("args![").Append(i.ToString(CultureInfo.InvariantCulture)).Append("] is ").Append(parameter.CheckType);
                        }
                    }
                    builder.Append(')').NewLine();
                    builder.BeginScope();
                    BuildNewInstanceExpression(builder, className, ctor);
                    builder.EndScope();
                }
                builder.Indent().Append("break;").NewLine();
            }
            builder.IndentLevel--;
        }
        builder.EndScope();
        builder.Indent().Append("throw new global::System.NotSupportedException(\"No matching constructor.\");").NewLine();
        builder.EndScope();
    }

    private static void BuildNewInstanceExpression(SourceBuilder builder, string className, ConstructorModel ctor)
    {
        builder.Indent().Append("return new ").Append(className).Append('(');
        for (var i = 0; i < ctor.Parameters.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }
            builder.Append('(').Append(ctor.Parameters[i].Type).Append(")args![").Append(i.ToString(CultureInfo.InvariantCulture)).Append("]!");
        }
        builder.Append(");").NewLine();
    }

    private static void BuildCreateBody(SourceBuilder builder, string className, Dictionary<int, ConstructorModel[]> byArity, string[] argTypes)
    {
        var arity = argTypes.Length;
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
                builder.Append("typeof(").Append(argTypes[i]).Append(") == typeof(").Append(ctor.Parameters[i].TypeOfName).Append(')');
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

    private static string[] MakeArgTypeParameterNames(TypeModel type, int arity)
    {
        var names = new string[arity];
        for (var i = 0; i < arity; i++)
        {
            names[i] = MakeTypeParameterName(type, ArgTypeParameterName(arity, i));
        }

        return names;
    }

    private static string ArgName(int arity, int index) => arity == 1 ? "arg" : $"arg{index + 1}";

    private static void BuildUnsafeAccessSource(SourceBuilder builder, TypeModel type)
    {
        builder.Indent()
            .Append("internal static class ")
            .Append(MakeSuffixedName(MakeFlatName(type), UnsafeAccessSuffix))
            .Append(type.Constraints)
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

        foreach (var containingType in type.ContainingTypes)
        {
            builder.Indent()
                .Append("partial ")
                .Append(containingType.TypeKeyword)
                .Append(' ')
                .Append(containingType.ClassName)
                .NewLine();
            builder.BeginScope();
        }

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
            .Append(MakeQualifiedName(type.Namespace, MakeSuffixedName(MakeFlatName(type), AccessorSuffix)))
            .Append(".Instance;")
            .NewLine();
        builder.NewLine();

        builder.Indent()
            .Append("static global::BunnyTail.MemberAccessor.IAccessorFactory<")
            .Append(className)
            .Append("> global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(className)
            .Append(">.AccessorFactory => ")
            .Append(MakeQualifiedName(type.Namespace, MakeSuffixedName(MakeFlatName(type), AccessorFactorySuffix)))
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
                .Append(MakeQualifiedName(type.Namespace, MakeSuffixedName(MakeFlatName(type), ConstructorAccessorSuffix)))
                .Append(".Instance;")
                .NewLine();
        }

        builder.EndScope();

        for (var i = 0; i < type.ContainingTypes.Count; i++)
        {
            builder.EndScope();
        }
    }

    private static void BuildProviderSource(SourceBuilder builder, ProviderModel provider)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Disable("CS0612, CS0618");
        builder.NewLine();

        if (!String.IsNullOrEmpty(provider.Namespace))
        {
            builder.Namespace(provider.Namespace);
            builder.NewLine();
        }

        var hasConstructor = !String.IsNullOrEmpty(provider.ConstructorName);

        foreach (var containingType in provider.ContainingTypes)
        {
            builder.Indent()
                .Append("partial ")
                .Append(containingType.TypeKeyword)
                .Append(' ')
                .Append(containingType.ClassName)
                .NewLine();
            builder.BeginScope();
        }

        builder.Indent()
            .Append("partial ")
            .Append(provider.TypeKeyword)
            .Append(' ')
            .Append(provider.ClassName)
            .Append(" : global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(provider.TargetTypeName)
            .Append('>');
        if (hasConstructor)
        {
            builder
                .Append(", global::BunnyTail.MemberAccessor.IConstructorProvider<")
                .Append(provider.TargetTypeName)
                .Append('>');
        }
        builder.NewLine();
        builder.BeginScope();

        builder.Indent()
            .Append("static global::BunnyTail.MemberAccessor.IAccessor global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(provider.TargetTypeName)
            .Append(">.Accessor => ")
            .Append(provider.AccessorName)
            .Append(".Instance;")
            .NewLine();
        builder.NewLine();

        builder.Indent()
            .Append("static global::BunnyTail.MemberAccessor.IAccessorFactory<")
            .Append(provider.TargetTypeName)
            .Append("> global::BunnyTail.MemberAccessor.IAccessorProvider<")
            .Append(provider.TargetTypeName)
            .Append(">.AccessorFactory => ")
            .Append(provider.FactoryName)
            .Append(".Instance;")
            .NewLine();

        if (hasConstructor)
        {
            builder.NewLine();
            builder.Indent()
                .Append("static global::BunnyTail.MemberAccessor.IConstructor<")
                .Append(provider.TargetTypeName)
                .Append("> global::BunnyTail.MemberAccessor.IConstructorProvider<")
                .Append(provider.TargetTypeName)
                .Append(">.Constructor => ")
                .Append(provider.ConstructorName)
                .Append(".Instance;")
                .NewLine();
        }

        builder.EndScope();

        for (var i = 0; i < provider.ContainingTypes.Count; i++)
        {
            builder.EndScope();
        }
    }

    private static void BuildRegistrySource(SourceBuilder builder, EquatableArray<RegistryTypeModel> types, EquatableArray<ClosedGenericModel> closedTypes)
    {
        builder.AutoGenerated();
        builder.EnableNullable();
        builder.Disable("CS0612, CS0618");
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
                var targetName = MakeQualifiedName(type.Namespace, type.TypePath);
                builder
                    .Indent()
                    .Append("global::BunnyTail.MemberAccessor.Internal.AccessorRegistry.RegisterFactory(typeof(")
                    .Append(targetName)
                    .Append("), ")
                    .Append(MakeQualifiedName(type.Namespace, $"{type.FlatName}{AccessorSuffix}"))
                    .Append(".Instance, ")
                    .Append(MakeQualifiedName(type.Namespace, $"{type.FlatName}{AccessorFactorySuffix}"))
                    .Append(".Instance);")
                    .NewLine();

                // Register constructor accessor
                if (type.HasConstructors)
                {
                    builder
                        .Indent()
                        .Append("global::BunnyTail.MemberAccessor.Internal.AccessorRegistry.RegisterConstructor<")
                        .Append(targetName)
                        .Append(">(typeof(")
                        .Append(targetName)
                        .Append("), ")
                        .Append(MakeQualifiedName(type.Namespace, $"{type.FlatName}{ConstructorAccessorSuffix}"))
                        .Append(".Instance);")
                        .NewLine();
                }
            }
            else
            {
                // Open generic type
                var prefix = OpenNamePart(type.TypePath);
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
                            .Append("global::BunnyTail.MemberAccessor.Internal.AccessorRegistry.RegisterFactory(typeof(")
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
                                .Append("global::BunnyTail.MemberAccessor.Internal.AccessorRegistry.RegisterConstructor<")
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
                    .Append("global::BunnyTail.MemberAccessor.Internal.AccessorRegistry.RegisterOpenGenericFactory(typeof(")
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
                        .Append("global::BunnyTail.MemberAccessor.Internal.AccessorRegistry.RegisterOpenGenericConstructorFactory(typeof(")
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
        String.IsNullOrEmpty(ns) ? $"global::{name}" : $"global::{CSharpIdentifier.EscapeQualifiedName(ns)}.{name}";

    private static string MakeTypePath(TypeModel type) =>
        String.Join(".", [.. type.ContainingTypes.Select(static x => x.ClassName), type.ClassName]);

    private static string MakeFlatName(TypeModel type) =>
        String.Join("__", [.. type.ContainingTypes.Select(static x => x.ClassName), type.ClassName]);

    private static string MakeDisplayName(string ns, string path) =>
        String.IsNullOrEmpty(ns) ? path : $"{ns}.{path}";

    private static string MakeProviderPath(ProviderModel provider) =>
        String.Join(".", [.. provider.ContainingTypes.Select(static x => x.ClassName), provider.ClassName]);

    private static string MakeClassHintName(TypeModel type) =>
        HintNameBuilder.Build(type.Namespace, [.. type.ContainingTypes.Select(static x => x.ClassName), type.ClassName, "Accessor"]);

    private static string MakeOpenKey(string ns, string className, int arity) =>
        $"{ns}/{OpenNamePart(className)}`{arity}";

    private static string OpenNamePart(string className)
    {
        var index = className.IndexOf('<');
        return index < 0 ? className : className.Substring(0, index);
    }

    private static string MakeTypeParameterName(TypeModel type, string name)
    {
        while (type.TypeParameters.Contains(name))
        {
            name = "_" + name;
        }

        return name;
    }

    private static string MakeTypeArgumentsPart(EquatableArray<string> typeArguments) =>
        $"<{String.Join(", ", typeArguments)}>";

    private static string MakeOpenAnglePart(int count) =>
        $"<{new string(',', count - 1)}>";

    private static string MakeTypeKey(TypeModel type) =>
        $"{type.Namespace}/{MakeTypePath(type)}";

    private static string MakeClosedTypeKey(ClosedGenericModel closedType) =>
        $"{closedType.Namespace}/{OpenNamePart(closedType.ClassName)}{MakeTypeArgumentsPart(closedType.TypeArguments)}";

    // ------------------------------------------------------------
    // Helper
    // ------------------------------------------------------------

    // The target type name comes from a typeof() argument, so it can carry characters that are
    // illegal in a file name beyond the ones HintNameBuilder handles. Fold them first.
    private static string MakeProviderFilename(ProviderModel provider)
    {
        var target = new StringBuilder(provider.TargetTypeName.Length);
        foreach (var c in provider.TargetTypeName)
        {
            target.Append(Char.IsLetterOrDigit(c) ? c : '_');
        }

        return HintNameBuilder.Build(provider.Namespace, [.. provider.ContainingTypes.Select(static x => OpenNamePart(x.ClassName)), OpenNamePart(provider.ClassName), "Provider", target.ToString()]);
    }
}
