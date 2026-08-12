namespace BunnyTail.MemberAccessor;

public class RegistryTest
{
    private static Type GetRuntimeType<T>() => typeof(T);

    [Fact]
    public void TestUnregisteredTypeReturnsNull()
    {
        // Lookup misses force the module initializer of the target assembly and retry.
        // Types without accessors must still resolve to null without throwing.
        var type = GetRuntimeType<Uri>();
        Assert.Null(AccessorRegistry.FindAccessor(type));
        Assert.Null(AccessorRegistry.FindFactory(type));
        Assert.Null(AccessorRegistry.FindAccessor<Uri>());
        Assert.Null(AccessorRegistry.FindFactory<Uri>());
        Assert.Null(AccessorRegistry.FindConstructor<Uri>());
    }

    [Fact]
    public void TestUnregisteredGenericTypeReturnsNull()
    {
        // Generic types without open-generic registrations resolve to null without throwing
        Assert.Null(AccessorRegistry.FindAccessor(GetRuntimeType<List<int>>()));
        Assert.Null(AccessorRegistry.FindFactory<List<int>>());
        Assert.Null(AccessorRegistry.FindConstructor<List<int>>());
    }

    [Fact]
    public void TestNonGenericLookupByType()
    {
        // The Type-based overloads resolve the same singletons as the generic overloads
        var type = GetRuntimeType<Data>();
        Assert.Same(AccessorRegistry.FindAccessor<Data>(), AccessorRegistry.FindAccessor(type));
        Assert.Same(AccessorRegistry.FindFactory<Data>(), AccessorRegistry.FindFactory(type));
    }
}
