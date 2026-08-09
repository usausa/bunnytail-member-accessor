namespace BunnyTail.MemberAccessor.Generator.Models;

internal enum MemberAccess
{
    None,
    Direct,
    Unsafe
}

internal sealed record MemberModel(
    string Type,
    string Name,
    bool IsField,
    MemberAccess GetterAccess,
    MemberAccess SetterAccess,
    string UnsafeTargetType)
{
    public bool CanRead => GetterAccess != MemberAccess.None;

    public bool CanWrite => SetterAccess != MemberAccess.None;

    public bool RequiresUnsafe => (GetterAccess == MemberAccess.Unsafe) || (SetterAccess == MemberAccess.Unsafe);
}
