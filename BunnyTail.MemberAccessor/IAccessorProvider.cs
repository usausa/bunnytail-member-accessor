namespace BunnyTail.MemberAccessor;

// Registry-free accessor lookup. Implemented by generated code on partial target types
// (T is the implementing type itself) or on witness types (T is the external target type).
public interface IAccessorProvider<T>
{
    static abstract IAccessor Accessor { get; }

    static abstract IAccessorFactory<T> AccessorFactory { get; }
}
