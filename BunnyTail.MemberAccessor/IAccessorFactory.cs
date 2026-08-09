namespace BunnyTail.MemberAccessor;

public interface IAccessorFactory
{
    IReadOnlyList<MemberDescriptor> Members { get; }

    Func<object, object?>? CreateGetter(string name);

    Action<object, object?>? CreateSetter(string name);
}

public interface IAccessorFactory<T> : IAccessorFactory
{
    Getter<T, TProperty>? CreateGetter<TProperty>(string name);

    Setter<T, TProperty>? CreateSetter<TProperty>(string name);
}
