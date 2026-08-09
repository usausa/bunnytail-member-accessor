namespace BunnyTail.MemberAccessor;

public class GenerateAccessorForTest
{
    private static IAccessorFactory<T> GetFactory<T, TWitness>()
        where TWitness : IAccessorProvider<T>
        => TWitness.AccessorFactory;

    private static IConstructor<T> GetConstructor<T, TWitness>()
        where TWitness : IConstructorProvider<T>
        => TWitness.Constructor;

    [Fact]
    public void TestExternalPlainType()
    {
        // Types without [GenerateAccessor] are covered by [GenerateAccessorFor]
        var accessor = AccessorRegistry.FindAccessor<PlainData>();
        var factory = AccessorRegistry.FindFactory<PlainData>();
        var ctor = AccessorRegistry.FindConstructor<PlainData>();
        Assert.NotNull(accessor);
        Assert.NotNull(factory);
        Assert.NotNull(ctor);

        var data = ctor.Create();
        accessor.SetValue(data, nameof(PlainData.Id), 1);
        Assert.Equal(1, data.Id);

        var setName = factory.CreateSetter<string>(nameof(PlainData.Name));
        Assert.NotNull(setName);
        setName(ref data, "abc");
        Assert.Equal("abc", data.Name);
    }

    [Fact]
    public void TestExternalGenericClosed()
    {
        // Closed generic target is pre-registered (AOT-safe path)
        var factory = AccessorRegistry.FindFactory<PlainGenericData<int>>();
        Assert.NotNull(factory);

        var data = new PlainGenericData<int>(123);
        var getValue = factory.CreateGetter<int>(nameof(PlainGenericData<>.Value));
        Assert.NotNull(getValue);
        Assert.Equal(123, getValue(ref data));
    }

    [Fact]
    public void TestExternalGenericOnDemand()
    {
        // Other instantiations resolve on demand through the open-generic registration
        var factory = AccessorRegistry.FindFactory<PlainGenericData<string>>();
        Assert.NotNull(factory);

        var data = new PlainGenericData<string>("abc");
        var getValue = factory.CreateGetter<string>(nameof(PlainGenericData<>.Value));
        Assert.NotNull(getValue);
        Assert.Equal("abc", getValue(ref data));
    }

    [Fact]
    public void TestExternalBclType()
    {
        // BCL type registered via assembly-level [GenerateAccessorFor(typeof(Version))]
        var factory = AccessorRegistry.FindFactory<Version>();
        var ctor = AccessorRegistry.FindConstructor<Version>();
        Assert.NotNull(factory);
        Assert.NotNull(ctor);

        var version = ctor.Create(1, 2);
        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);

        var getMajor = factory.CreateGetter<int>(nameof(Version.Major));
        Assert.NotNull(getMajor);
        Assert.Equal(1, getMajor(ref version));

        // All Version properties are read-only
        Assert.Null(factory.CreateSetter<int>(nameof(Version.Major)));

        var major = factory.Members.First(m => m.Name == nameof(Version.Major));
        Assert.True(major.CanRead);
        Assert.False(major.CanWrite);
    }

    [Fact]
    public void TestWitnessProvider()
    {
        // The attribute-holding partial class acts as a witness implementing IAccessorProvider<T>
        var factory = GetFactory<PlainData, AccessorWitness>();
        var ctor = GetConstructor<PlainData, AccessorWitness>();

        var data = ctor.Create();
        var setId = factory.CreateSetter<int>(nameof(PlainData.Id));
        Assert.NotNull(setId);
        setId(ref data, 42);
        Assert.Equal(42, data.Id);

        // Same singleton as the registry path
        Assert.Same(AccessorRegistry.FindFactory<PlainData>(), factory);
    }

    [Fact]
    public void TestWitnessForClosedGeneric()
    {
        // Arrange
        var factory = GetFactory<PlainGenericData<int>, AccessorWitness>();

        var data = new PlainGenericData<int>(7);

        // Act & Assert
        var getValue = factory.CreateGetter<int>(nameof(PlainGenericData<>.Value));
        Assert.NotNull(getValue);
        Assert.Equal(7, getValue(ref data));
    }

    [Fact]
    public void TestWitnessForGenerateAccessorType()
    {
        // Target already has [GenerateAccessor]: no duplicate classes, witness reuses them
        var factory = GetFactory<Data, AccessorWitness>();

        Assert.Same(AccessorRegistry.FindFactory<Data>(), factory);
    }
}
