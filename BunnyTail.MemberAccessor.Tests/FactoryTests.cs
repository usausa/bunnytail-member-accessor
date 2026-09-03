namespace BunnyTail.MemberAccessor;

public class FactoryTests
{
    [Fact]
    public void TestBasic()
    {
        // Arrange
        var accessorFactory = AccessorProvider.FindFactory<Data>();
        Assert.NotNull(accessorFactory);

        var getId = accessorFactory.CreateGetter<int>(nameof(Data.Id));
        var getName = accessorFactory.CreateGetter<string>(nameof(Data.Name));
        var setId = accessorFactory.CreateSetter<int>(nameof(Data.Id));
        var setName = accessorFactory.CreateSetter<string>(nameof(Data.Name));
        Assert.NotNull(getId);
        Assert.NotNull(getName);
        Assert.NotNull(setId);
        Assert.NotNull(setName);

        var data = new Data { Id = 123, Name = "abc" };

        // Act & Assert
        Assert.Equal(123, getId(ref data));
        Assert.Equal("abc", getName(ref data));

        // Act
        setId(ref data, 234);
        setName(ref data, "xyz");

        // Assert
        Assert.Equal(234, data.Id);
        Assert.Equal("xyz", data.Name);
    }

    [Fact]
    public void TestNullable()
    {
        // Arrange
        var accessorFactory = AccessorProvider.FindFactory<NullableData>();
        Assert.NotNull(accessorFactory);

        var getId = accessorFactory.CreateGetter<int?>(nameof(NullableData.Id));
        var getName = accessorFactory.CreateGetter<string?>(nameof(NullableData.Name));
        var setId = accessorFactory.CreateSetter<int?>(nameof(NullableData.Id));
        var setName = accessorFactory.CreateSetter<string?>(nameof(NullableData.Name));
        Assert.NotNull(getId);
        Assert.NotNull(getName);
        Assert.NotNull(setId);
        Assert.NotNull(setName);

        var data = new NullableData { Id = 123, Name = "abc" };

        // Act & Assert
        Assert.Equal(123, getId(ref data));
        Assert.Equal("abc", getName(ref data));

        // Act
        setId(ref data, 234);
        setName(ref data, "xyz");

        // Assert
        Assert.Equal(234, data.Id);
        Assert.Equal("xyz", data.Name);

        // Act
        setId(ref data, null);
        setName(ref data, null);

        // Assert
        Assert.Null(data.Id);
        Assert.Null(data.Name);
    }

    [Fact]
    public void TestGenerics()
    {
        // Arrange
        var accessorFactory1 = AccessorProvider.FindFactory<GenericData<int>>();
        var accessorFactory2 = AccessorProvider.FindFactory<GenericData<string>>();
        Assert.NotNull(accessorFactory1);
        Assert.NotNull(accessorFactory2);

        var get1 = accessorFactory1.CreateGetter<int>(nameof(GenericData<>.Value));
        var set1 = accessorFactory1.CreateSetter<int>(nameof(GenericData<>.Value));
        var get2 = accessorFactory2.CreateGetter<string>(nameof(GenericData<>.Value));
        var set2 = accessorFactory2.CreateSetter<string>(nameof(GenericData<>.Value));
        Assert.NotNull(get1);
        Assert.NotNull(set1);
        Assert.NotNull(get2);
        Assert.NotNull(set2);

        var data1 = new GenericData<int> { Value = 123 };

        // Act & Assert
        Assert.Equal(123, get1(ref data1));

        // Act
        set1(ref data1, 234);

        // Assert
        Assert.Equal(234, data1.Value);

        // Arrange
        var data2 = new GenericData<string> { Value = "abc" };

        // Act & Assert (get)
        Assert.Equal("abc", get2(ref data2));

        // Act
        set2(ref data2, "xyz");

        // Assert
        Assert.Equal("xyz", data2.Value);
    }

    [Fact]
    public void TestMultiGenerics()
    {
        // Arrange
        var accessorFactory1 = AccessorProvider.FindFactory<MultiGenericData<int, int>>();
        var accessorFactory2 = AccessorProvider.FindFactory<MultiGenericData<string, string>>();
        Assert.NotNull(accessorFactory1);
        Assert.NotNull(accessorFactory2);

        var get1 = accessorFactory1.CreateGetter<int>(nameof(MultiGenericData<,>.Value1));
        var set1 = accessorFactory1.CreateSetter<int>(nameof(MultiGenericData<,>.Value1));
        var get2 = accessorFactory2.CreateGetter<string>(nameof(MultiGenericData<,>.Value1));
        var set2 = accessorFactory2.CreateSetter<string>(nameof(MultiGenericData<,>.Value1));
        Assert.NotNull(get1);
        Assert.NotNull(set1);
        Assert.NotNull(get2);
        Assert.NotNull(set2);

        var data1 = new MultiGenericData<int, int> { Value1 = 123 };

        // Act & Assert
        Assert.Equal(123, get1(ref data1));

        // Act
        set1(ref data1, 234);

        // Assert
        Assert.Equal(234, data1.Value1);

        // Arrange
        var data2 = new MultiGenericData<string, string> { Value1 = "abc" };

        // Act & Assert
        Assert.Equal("abc", get2(ref data2));

        // Act
        set2(ref data2, "xyz");

        // Assert
        Assert.Equal("xyz", data2.Value1);
    }
}
