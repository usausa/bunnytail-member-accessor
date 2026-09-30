namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record ProviderModel(
    string Namespace,
    string ClassName,
    EquatableArray<ContainingTypeModel> ContainingTypes,
    string TypeKeyword,
    string TargetTypeName,
    string AccessorName,
    string FactoryName,
    string ConstructorName);

internal sealed record ExternalModel(
    TypeModel Type,
    ClosedGenericModel? ClosedGeneric,
    ProviderModel? Provider,
    bool TargetHasGenerateAccessor);
