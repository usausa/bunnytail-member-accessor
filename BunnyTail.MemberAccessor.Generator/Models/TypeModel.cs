namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    bool IsValueType,
    string TypeKeyword,
    bool IsPartial,
    bool SupportsGenericUnsafeAccessor,
    int TypeArgumentCount,
    EquatableArray<MemberModel> Members,
    EquatableArray<ConstructorModel> Constructors);
