namespace BunnyTail.MemberAccessor;

public class GenerateAccessorForTests
{
    [Fact]
    public void TestExternalPlainType()
    {
        var accessor = AccessorProvider.FindAccessor<PlainData>();
        var factory = AccessorProvider.FindFactory<PlainData>();
        var ctor = AccessorProvider.FindConstructor<PlainData>();
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
        var factory = AccessorProvider.FindFactory<PlainGenericData<int>>();
        Assert.NotNull(factory);

        var data = new PlainGenericData<int>(123);
        var getValue = factory.CreateGetter<int>(nameof(PlainGenericData<>.Value));
        Assert.NotNull(getValue);
        Assert.Equal(123, getValue(ref data));
    }

    [Fact]
    public void TestExternalGenericOnDemand()
    {
        var factory = AccessorProvider.FindFactory<PlainGenericData<string>>();
        Assert.NotNull(factory);

        var data = new PlainGenericData<string>("abc");
        var getValue = factory.CreateGetter<string>(nameof(PlainGenericData<>.Value));
        Assert.NotNull(getValue);
        Assert.Equal("abc", getValue(ref data));
    }

    [Fact]
    public void TestExternalBclType()
    {
        var factory = AccessorProvider.FindFactory<Version>();
        var ctor = AccessorProvider.FindConstructor<Version>();
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
    public void TestProviderForPlainType()
    {
        var factory = AccessorProvider.GetFactory<PlainData, AccessorProviders>();
        var ctor = AccessorProvider.GetConstructor<PlainData, AccessorProviders>();

        var data = ctor.Create();
        var setId = factory.CreateSetter<int>(nameof(PlainData.Id));
        Assert.NotNull(setId);
        setId(ref data, 42);
        Assert.Equal(42, data.Id);

        // Same singleton as the registry path
        Assert.Same(AccessorProvider.FindFactory<PlainData>(), factory);
    }

    [Fact]
    public void TestProviderForClosedGeneric()
    {
        // Arrange
        var factory = AccessorProvider.GetFactory<PlainGenericData<int>, AccessorProviders>();

        var data = new PlainGenericData<int>(7);

        // Act & Assert
        var getValue = factory.CreateGetter<int>(nameof(PlainGenericData<>.Value));
        Assert.NotNull(getValue);
        Assert.Equal(7, getValue(ref data));
    }

    [Fact]
    public void TestProviderForGenerateAccessorType()
    {
        var factory = AccessorProvider.GetFactory<Data, AccessorProviders>();

        Assert.Same(AccessorProvider.FindFactory<Data>(), factory);
    }
}
