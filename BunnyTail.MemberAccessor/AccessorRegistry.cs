namespace BunnyTail.MemberAccessor;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

public static class AccessorRegistry
{
    private static readonly ConcurrentDictionary<Type, IAccessor> AccessorInstances = new();
    private static readonly ConcurrentDictionary<Type, IAccessorFactory> FactoryInstances = new();
    private static readonly ConcurrentDictionary<Type, object> ConstructorInstances = new();

    private static readonly ConcurrentDictionary<Type, Func<Type[], IAccessor>> OpenAccessorFactories = new();
    private static readonly ConcurrentDictionary<Type, Func<Type[], IAccessorFactory>> OpenFactoryFactories = new();
    private static readonly ConcurrentDictionary<Type, Func<Type[], object>> OpenConstructorFactories = new();

    // ------------------------------------------------------------
    // Registration
    // ------------------------------------------------------------

    public static void RegisterFactory(Type type, IAccessor accessor, IAccessorFactory factory)
    {
        AccessorInstances[type] = accessor;
        FactoryInstances[type] = factory;
    }

    public static void RegisterConstructor<T>(Type type, IConstructor<T> constructor)
    {
        ConstructorInstances[type] = constructor;
    }

    [RequiresDynamicCode("Open generic type registration requires dynamic code (MakeGenericType).")]
    [RequiresUnreferencedCode("Open generic type registration may not be compatible with trimming.")]
    public static void RegisterOpenGenericFactory(
        Type openType,
        Func<Type[], IAccessor> accessorFactory,
        Func<Type[], IAccessorFactory> factoryFactory)
    {
        OpenAccessorFactories[openType] = accessorFactory;
        OpenFactoryFactories[openType] = factoryFactory;
    }

    [RequiresDynamicCode("Open generic type registration requires dynamic code (MakeGenericType).")]
    [RequiresUnreferencedCode("Open generic type registration may not be compatible with trimming.")]
    public static void RegisterOpenGenericConstructorFactory(
        Type openType,
        Func<Type[], object> constructorFactory)
    {
        OpenConstructorFactories[openType] = constructorFactory;
    }

    // ------------------------------------------------------------
    // Static generic cache
    // ------------------------------------------------------------

    // ReSharper disable once UnusedTypeParameter
    private static class AccessorCache<T>
    {
#pragma warning disable SA1401
        // ReSharper disable once StaticMemberInGenericType
        internal static IAccessor? Instance;
#pragma warning restore SA1401
    }

    private static class FactoryCache<T>
    {
#pragma warning disable SA1401
        // ReSharper disable once StaticMemberInGenericType
        internal static IAccessorFactory<T>? Instance;
#pragma warning restore SA1401
    }

    private static class ConstructorCache<T>
    {
#pragma warning disable SA1401
        // ReSharper disable once StaticMemberInGenericType
        internal static IConstructor<T>? Instance;
#pragma warning restore SA1401
    }

    // ------------------------------------------------------------
    // Lookup
    // ------------------------------------------------------------

    private static void EnsureModuleInitialized(Type type)
    {
        try
        {
            RuntimeHelpers.RunModuleConstructor(type.Module.ModuleHandle);
        }
        catch (Exception e) when (e is PlatformNotSupportedException or NotSupportedException)
        {
            // Native AOT executes module initializers eagerly at startup
        }
    }

    public static IAccessor? FindAccessor<T>()
    {
        if (AccessorCache<T>.Instance is { } cached)
        {
            return cached;
        }

        var result = FindAccessorCore(typeof(T));
        AccessorCache<T>.Instance = result;
        return result;
    }

    public static IAccessor? FindAccessor(Type type) => FindAccessorCore(type);

    private static IAccessor? FindAccessorCore(Type type)
    {
        if (AccessorInstances.TryGetValue(type, out var accessor))
        {
            return accessor;
        }

        EnsureModuleInitialized(type);

        if (AccessorInstances.TryGetValue(type, out accessor))
        {
            return accessor;
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var openType = type.GetGenericTypeDefinition();
        if (!OpenAccessorFactories.TryGetValue(openType, out var factory))
        {
            return null;
        }

        return AccessorInstances.GetOrAdd(type, static (t, f) => f(t.GenericTypeArguments), factory);
    }

    public static IAccessorFactory<T>? FindFactory<T>()
    {
        if (FactoryCache<T>.Instance is { } cached)
        {
            return cached;
        }

        var result = (IAccessorFactory<T>?)FindFactoryCore(typeof(T));
        FactoryCache<T>.Instance = result;
        return result;
    }

    public static IAccessorFactory? FindFactory(Type type) => FindFactoryCore(type);

    private static IAccessorFactory? FindFactoryCore(Type type)
    {
        if (FactoryInstances.TryGetValue(type, out var factory))
        {
            return factory;
        }

        EnsureModuleInitialized(type);

        if (FactoryInstances.TryGetValue(type, out factory))
        {
            return factory;
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var openType = type.GetGenericTypeDefinition();
        if (!OpenFactoryFactories.TryGetValue(openType, out var openFactory))
        {
            return null;
        }

        return FactoryInstances.GetOrAdd(type, static (t, f) => f(t.GenericTypeArguments), openFactory);
    }

    public static IConstructor<T>? FindConstructor<T>()
    {
        if (ConstructorCache<T>.Instance is { } cached)
        {
            return cached;
        }

        var result = (IConstructor<T>?)FindConstructorCore(typeof(T));
        ConstructorCache<T>.Instance = result;
        return result;
    }

    public static IConstructor? FindConstructor(Type type) => (IConstructor?)FindConstructorCore(type);

    private static object? FindConstructorCore(Type type)
    {
        if (ConstructorInstances.TryGetValue(type, out var ctor))
        {
            return ctor;
        }

        EnsureModuleInitialized(type);

        if (ConstructorInstances.TryGetValue(type, out ctor))
        {
            return ctor;
        }

        if (!type.IsGenericType)
        {
            return null;
        }

        var openType = type.GetGenericTypeDefinition();
        if (!OpenConstructorFactories.TryGetValue(openType, out var factory))
        {
            return null;
        }

        return ConstructorInstances.GetOrAdd(type, static (t, f) => f(t.GenericTypeArguments), factory);
    }
}
