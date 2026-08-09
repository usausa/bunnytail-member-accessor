namespace BunnyTail.MemberAccessor.Generator.Models;

internal sealed record WitnessModel(
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
    WitnessModel? Witness,
    bool TargetHasGenerateAccessor);
