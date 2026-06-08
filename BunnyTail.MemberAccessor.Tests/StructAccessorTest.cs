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
    public void TestStructTypedSetterReturnsNull()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<StructData>();
        Assert.NotNull(factory);

        // Act & Assert
        // Typed setters cannot mutate value types (the delegate receives a copy), so null is returned
        Assert.Null(factory.CreateSetter<int>(nameof(StructData.Id)));
        Assert.Null(factory.CreateSetter<string>(nameof(StructData.Name)));

        // Typed getter and the object-based setter (via boxed instance) still work
        var getId = factory.CreateGetter<int>(nameof(StructData.Id));
        Assert.NotNull(getId);

        var setId = factory.CreateSetter(nameof(StructData.Id));
        Assert.NotNull(setId);
    }
}
