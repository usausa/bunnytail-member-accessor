namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    EquatableArray<ContainingTypeModel> ContainingTypes,
    bool IsValueType,
    string TypeKeyword,
    EquatableArray<string> TypeParameters,
    string Constraints,
    bool IsPartial,
    EquatableArray<ConstructorModel> Constructors,
    EquatableArray<MemberModel> Members);
