namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    bool IsValueType,
    int TypeArgumentCount,
    EquatableArray<PropertyModel> Properties,
    EquatableArray<ConstructorModel> Constructors);
