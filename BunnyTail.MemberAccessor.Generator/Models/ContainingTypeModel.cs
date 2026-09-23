namespace BunnyTail.MemberAccessor.Generator.Models;

internal sealed record ContainingTypeModel(
    string ClassName,
    string TypeKeyword,
    bool IsPartial);
