namespace BunnyTail.MemberAccessor;

using System.Runtime.CompilerServices;

using BunnyTail.MemberAccessor.Internal;

public static class AccessorProvider
{
    // ------------------------------------------------------------
    // Compile time resolver
    // ------------------------------------------------------------

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

    // ------------------------------------------------------------
    // Runtime resolver
    // ------------------------------------------------------------

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessor? FindAccessor<T>() => AccessorRegistry.FindAccessor<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessor? FindAccessor(Type type) => AccessorRegistry.FindAccessor(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessorFactory<T>? FindFactory<T>() => AccessorRegistry.FindFactory<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IAccessorFactory? FindFactory(Type type) => AccessorRegistry.FindFactory(type);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConstructor<T>? FindConstructor<T>() => AccessorRegistry.FindConstructor<T>();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IConstructor? FindConstructor(Type type) => AccessorRegistry.FindConstructor(type);
}
