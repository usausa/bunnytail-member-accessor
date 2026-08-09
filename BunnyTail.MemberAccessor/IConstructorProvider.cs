namespace BunnyTail.MemberAccessor;

// Registry-free constructor lookup. Implemented only when the target type has public constructors.
public interface IConstructorProvider<T>
{
    static abstract IConstructor<T> Constructor { get; }
}
