namespace BunnyTail.MemberAccessor;

public sealed record MemberDescriptor(
    string Name,
    Type Type,
    MemberKind Kind,
    bool CanRead,
    bool CanWrite);
