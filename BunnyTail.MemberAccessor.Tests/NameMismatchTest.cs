namespace BunnyTail.MemberAccessor;

public class NameMismatchTest
{
    private const string UnknownName = "NoSuchProperty";

    [Fact]
    public void TestAccessorGetValueThrows()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<Data>();
        Assert.NotNull(accessor);

        var data = new Data { Id = 1, Name = "abc" };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => accessor.GetValue(data, UnknownName));

        // Assert
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void TestAccessorSetValueThrows()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<Data>();
        Assert.NotNull(accessor);

        var data = new Data { Id = 1, Name = "abc" };

        // Act
        var ex = Assert.Throws<ArgumentException>(() => accessor.SetValue(data, UnknownName, 1));

        // Assert
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void TestFactoryTypedCreateReturnsNull()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<Data>();
        Assert.NotNull(factory);

        // Act & Assert
        Assert.Null(factory.CreateGetter<int>(UnknownName));
        Assert.Null(factory.CreateSetter<int>(UnknownName));
    }

    [Fact]
    public void TestFactoryObjectCreateReturnsNull()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<Data>();
        Assert.NotNull(factory);

        // Act & Assert
        Assert.Null(factory.CreateGetter(UnknownName));
        Assert.Null(factory.CreateSetter(UnknownName));
    }
}
