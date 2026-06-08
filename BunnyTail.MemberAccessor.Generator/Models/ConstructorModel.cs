namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record ConstructorParameterModel(
    string Type,
    string Name);

internal sealed record ConstructorModel(
    EquatableArray<ConstructorParameterModel> Parameters);
