namespace BunnyTail.MemberAccessor.Generator.Models;

internal sealed record PropertyModel(
    string Type,
    string Name,
    bool CanRead,
    bool CanWrite);
