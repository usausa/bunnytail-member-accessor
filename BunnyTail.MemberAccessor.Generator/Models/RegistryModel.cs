namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record RegistryTypeModel(
    string Namespace,
    string ClassName,
    int TypeArgumentCount,
    bool HasConstructors);

internal sealed record RegistryModel(
    EquatableArray<RegistryTypeModel> Types,
    EquatableArray<ClosedGenericModel> ClosedTypes);
