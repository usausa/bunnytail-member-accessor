namespace BunnyTail.MemberAccessor.Generator.Models;

internal sealed record ProviderModel(
    string Namespace,
    string ClassName,
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
