namespace BunnyTail.MemberAccessor;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AccessorMemberAttribute : Attribute
{
    public bool Ignore { get; set; }
}
