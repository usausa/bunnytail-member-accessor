namespace BunnyTail.MemberAccessor;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
public sealed class GenerateAccessorForAttribute : Attribute
{
    public Type TargetType { get; }

    public GenerateAccessorForAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
