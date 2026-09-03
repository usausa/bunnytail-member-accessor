namespace BunnyTail.MemberAccessor;

public class RegistryTests
{
    private static Type GetRuntimeType<T>() => typeof(T);

    [Fact]
    public void TestUnregisteredTypeReturnsNull()
    {
        // Lookup misses run the target module initializer and must still resolve to null without throwing
        var type = GetRuntimeType<Uri>();
        Assert.Null(AccessorProvider.FindAccessor(type));
        Assert.Null(AccessorProvider.FindFactory(type));
        Assert.Null(AccessorProvider.FindConstructor(type));
        Assert.Null(AccessorProvider.FindAccessor<Uri>());
        Assert.Null(AccessorProvider.FindFactory<Uri>());
        Assert.Null(AccessorProvider.FindConstructor<Uri>());
    }

    [Fact]
    public void TestUnregisteredGenericTypeReturnsNull()
    {
        // Generic types without open-generic registrations resolve to null without throwing
        Assert.Null(AccessorProvider.FindAccessor(GetRuntimeType<List<int>>()));
        Assert.Null(AccessorProvider.FindFactory<List<int>>());
        Assert.Null(AccessorProvider.FindConstructor<List<int>>());
    }

    [Fact]
    public void TestNonGenericLookupByType()
    {
        // The Type-based overloads resolve the same singletons as the generic overloads
        var type = GetRuntimeType<Data>();
        Assert.Same(AccessorProvider.FindAccessor<Data>(), AccessorProvider.FindAccessor(type));
        Assert.Same(AccessorProvider.FindFactory<Data>(), AccessorProvider.FindFactory(type));
    }
}
