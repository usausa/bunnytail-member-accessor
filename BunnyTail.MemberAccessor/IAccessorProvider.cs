namespace BunnyTail.MemberAccessor;

public interface IAccessorProvider<T>
{
    static abstract IAccessor Accessor { get; }

    static abstract IAccessorFactory<T> AccessorFactory { get; }
}
