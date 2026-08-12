namespace BunnyTail.MemberAccessor;

public interface IConstructorProvider<out T>
{
    static abstract IConstructor<T> Constructor { get; }
}
