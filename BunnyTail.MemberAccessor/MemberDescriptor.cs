namespace BunnyTail.MemberAccessor;

public sealed record MemberDescriptor(
    string Name,
    Type Type,
    bool CanRead,
    bool CanWrite);
