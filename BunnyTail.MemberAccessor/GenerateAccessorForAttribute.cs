namespace BunnyTail.MemberAccessor;

// Generates accessors for a type that cannot be annotated with [GenerateAccessor]
// (external assembly types, closed generic instantiations, etc.).
// When applied to a partial class, the class also becomes a witness implementing
// IAccessorProvider<TargetType> (and IConstructorProvider<TargetType> when constructors exist).
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = true)]
public sealed class GenerateAccessorForAttribute : Attribute
{
    public Type TargetType { get; }

    public GenerateAccessorForAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
