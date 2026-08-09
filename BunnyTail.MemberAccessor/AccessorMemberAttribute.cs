namespace BunnyTail.MemberAccessor;

// On a public member: Ignore = true excludes it from accessor generation.
// On a non-public member: opts the member in to accessor generation.
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AccessorMemberAttribute : Attribute
{
    public bool Ignore { get; set; }
}
