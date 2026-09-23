namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    EquatableArray<ContainingTypeModel> ContainingTypes,
    bool IsValueType,
    string TypeKeyword,
    int TypeArgumentCount,
    bool IsPartial,
    bool SupportsGenericUnsafeAccessor,
    EquatableArray<ConstructorModel> Constructors,
    EquatableArray<MemberModel> Members);
