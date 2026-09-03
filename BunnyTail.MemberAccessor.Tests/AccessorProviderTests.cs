namespace BunnyTail.MemberAccessor;

public class AccessorProviderTests
{
    [Fact]
    public void TestProviderClass()
    {
        // Arrange
        var accessor = AccessorProvider.GetAccessor<Data>();
        var factory = AccessorProvider.GetFactory<Data>();

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
        Assert.Same(AccessorProvider.FindAccessor<Data>(), AccessorProvider.GetAccessor<Data>());
        Assert.Same(AccessorProvider.FindFactory<Data>(), AccessorProvider.GetFactory<Data>());
        Assert.Same(AccessorProvider.FindConstructor<CtorData2>(), AccessorProvider.GetConstructor<CtorData2>());
    }

    [Fact]
    public void TestProviderStruct()
    {
        // Arrange
        var factory = AccessorProvider.GetFactory<StructData>();

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
        var factory = AccessorProvider.GetFactory<RecordData>();

        var data = new RecordData { Id = 1, Name = "abc" };

        // Act & Assert
        var getId = factory.CreateGetter<int>(nameof(RecordData.Id));
        Assert.NotNull(getId);
        Assert.Equal(1, getId(ref data));
    }

    [Fact]
    public void TestProviderClosedGeneric()
    {
        // Closed generics flow through the type system without registry lookup or pre-registration
        var factory = AccessorProvider.GetFactory<GenericData<Guid>>();

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
        var ctor = AccessorProvider.GetConstructor<CtorData2>();

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
        var ctor = AccessorProvider.GetConstructor<GenericHolder<Guid>>();

        var value = Guid.NewGuid();

        // Act
        var instance = ctor.Create(value);

        // Assert
        Assert.Equal(value, instance.Value);
    }
}
