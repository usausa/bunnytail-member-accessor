namespace BunnyTail.MemberAccessor.Generator.Models;

using SourceGenerateHelper;

internal sealed record ConstructorParameterModel(string Name, string Type);

internal sealed record ConstructorModel(EquatableArray<ConstructorParameterModel> Parameters);
