namespace BunnyTail.MemberAccessor;

public class AccessorProviderTest
{
    private static IAccessor GetAccessor<T>()
        where T : IAccessorProvider<T>
        => T.Accessor;

    private static IAccessorFactory<T> GetFactory<T>()
        where T : IAccessorProvider<T>
        => T.AccessorFactory;

    private static IConstructor<T> GetConstructor<T>()
        where T : IConstructorProvider<T>
        => T.Constructor;

    [Fact]
    public void TestProviderClass()
    {
        // Arrange
        var accessor = GetAccessor<Data>();
        var factory = GetFactory<Data>();

        var data = new Data { Id = 1, Name = "abc" };

        // Act & Assert
        Assert.Equal(1, accessor.GetValue(data, nameof(Data.Id)));

        var getName = factory.CreateGetter<string>(nameof(Data.Name));
        var setName = factory.CreateSetter<string>(nameof(Data.Name));
        Assert.NotNull(getName);
        Assert.NotNull(setName);
        Assert.Equal("abc", getName(ref data));

        setName(ref data, "xyz");
        Assert.Equal("xyz", data.Name);
    }

    [Fact]
    public void TestProviderSharesRegistryInstance()
    {
        // Registry and IAccessorProvider expose the same singleton instances
        Assert.Same(AccessorRegistry.FindAccessor<Data>(), GetAccessor<Data>());
        Assert.Same(AccessorRegistry.FindFactory<Data>(), GetFactory<Data>());
        Assert.Same(AccessorRegistry.FindConstructor<CtorData2>(), GetConstructor<CtorData2>());
    }

    [Fact]
    public void TestProviderStruct()
    {
        // Arrange
        var factory = GetFactory<StructData>();

        var data = new StructData { Id = 1, Name = "abc" };

        // Act
        var setId = factory.CreateSetter<int>(nameof(StructData.Id));
        Assert.NotNull(setId);
        setId(ref data, 99);

        // Assert
        Assert.Equal(99, data.Id);
    }

    [Fact]
    public void TestProviderRecord()
    {
        // Arrange
        var factory = GetFactory<RecordData>();

        var data = new RecordData { Id = 1, Name = "abc" };

        // Act & Assert
        var getId = factory.CreateGetter<int>(nameof(RecordData.Id));
        Assert.NotNull(getId);
        Assert.Equal(1, getId(ref data));
    }

    [Fact]
    public void TestProviderClosedGeneric()
    {
        // Closed generic instantiations flow through the type system:
        // no registry, no MakeGenericType, works even for type arguments never pre-registered.
        var factory = GetFactory<GenericData<Guid>>();

        var value = Guid.NewGuid();
        var data = new GenericData<Guid> { Value = value };

        var getValue = factory.CreateGetter<Guid>(nameof(GenericData<>.Value));
        Assert.NotNull(getValue);
        Assert.Equal(value, getValue(ref data));
    }

    [Fact]
    public void TestConstructorProvider()
    {
        // Arrange
        var ctor = GetConstructor<CtorData2>();

        // Act
        var instance = ctor.Create(1, "abc");

        // Assert
        Assert.Equal(1, instance.Id);
        Assert.Equal("abc", instance.Name);
    }

    [Fact]
    public void TestConstructorProviderGeneric()
    {
        // Arrange
        var ctor = GetConstructor<GenericHolder<Guid>>();

        var value = Guid.NewGuid();

        // Act
        var instance = ctor.Create(value);

        // Assert
        Assert.Equal(value, instance.Value);
    }
}
