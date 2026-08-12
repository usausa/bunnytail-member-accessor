namespace BunnyTail.MemberAccessor;

using System.Runtime.CompilerServices;

public static class AccessorProvider
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessor GetAccessor<T>()
        where T : IAccessorProvider<T>
        => T.Accessor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessorFactory<T> GetFactory<T>()
        where T : IAccessorProvider<T>
        => T.AccessorFactory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConstructor<T> GetConstructor<T>()
        where T : IConstructorProvider<T>
        => T.Constructor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessor GetAccessor<T, TProvider>()
        where TProvider : IAccessorProvider<T>
        => TProvider.Accessor;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessorFactory<T> GetFactory<T, TProvider>()
        where TProvider : IAccessorProvider<T>
        => TProvider.AccessorFactory;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConstructor<T> GetConstructor<T, TProvider>()
        where TProvider : IConstructorProvider<T>
        => TProvider.Constructor;
}
