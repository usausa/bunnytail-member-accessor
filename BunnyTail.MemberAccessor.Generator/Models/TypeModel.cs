namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record TypeModel(
    string Namespace,
    string ClassName,
    int TypeArgumentCount,
    bool IsValueType,
    EquatableArray<PropertyModel> Properties,
    EquatableArray<ConstructorModel> Constructors);
