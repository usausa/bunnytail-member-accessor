namespace BunnyTail.MemberAccessor;

public class StructAccessorTest
{
    [Fact]
    public void TestStructAccessorGetValue()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<StructData>();
        Assert.NotNull(accessor);

        object boxed = new StructData { Id = 10, Name = "test" };

        // Act & Assert
        Assert.Equal(10, accessor.GetValue(boxed, nameof(StructData.Id)));
        Assert.Equal("test", accessor.GetValue(boxed, nameof(StructData.Name)));
    }

    [Fact]
    public void TestStructAccessorSetValue()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<StructData>();
        Assert.NotNull(accessor);

        object boxed = new StructData { Id = 10, Name = "test" };

        // Act
        accessor.SetValue(boxed, nameof(StructData.Id), 99);
        accessor.SetValue(boxed, nameof(StructData.Name), "updated");

        // Assert
        Assert.Equal(99, ((StructData)boxed).Id);
        Assert.Equal("updated", ((StructData)boxed).Name);
    }

    [Fact]
    public void TestStructTypedGetter()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<StructData>();
        Assert.NotNull(factory);

        var getId = factory.CreateGetter<int>(nameof(StructData.Id));
        Assert.NotNull(getId);

        var data = new StructData { Id = 10, Name = "test" };

        // Act & Assert
        Assert.Equal(10, getId(ref data));
    }

    [Fact]
    public void TestStructTypedSetter()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<StructData>();
        Assert.NotNull(factory);

        // ref-based setters mutate the value type in place (no boxing required)
        var setId = factory.CreateSetter<int>(nameof(StructData.Id));
        var setName = factory.CreateSetter<string>(nameof(StructData.Name));
        Assert.NotNull(setId);
        Assert.NotNull(setName);

        var data = new StructData { Id = 10, Name = "test" };

        // Act
        setId(ref data, 99);
        setName(ref data, "updated");

        // Assert
        Assert.Equal(99, data.Id);
        Assert.Equal("updated", data.Name);
    }

    [Fact]
    public void TestStructObjectSetter()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<StructData>();
        Assert.NotNull(factory);

        // Object-based setter mutates the boxed instance
        var setId = factory.CreateSetter(nameof(StructData.Id));
        Assert.NotNull(setId);

        object boxed = new StructData { Id = 10, Name = "test" };

        // Act
        setId(boxed, 99);

        // Assert
        Assert.Equal(99, ((StructData)boxed).Id);
    }
}
