namespace BunnyTail.MemberAccessor;

public class InitOnlyAccessorTest
{
    [Fact]
    public void TestInitOnlyPropertyIsReadable()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();
        Assert.NotNull(factory);
        var data = new InitOnlyData { Id = 1, Name = "abc" };

        // Act
        var getName = factory.CreateGetter<string>(nameof(InitOnlyData.Name));

        // Assert
        Assert.NotNull(getName);
        Assert.Equal("abc", getName(data));

        // Act
        var accessor = AccessorRegistry.FindAccessor<InitOnlyData>();

        // Assert
        Assert.NotNull(accessor);
        Assert.Equal("abc", accessor.GetValue(data, nameof(InitOnlyData.Name)));
    }

    [Fact]
    public void TestInitOnlySetterReturnsNull()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();
        Assert.NotNull(factory);

        // Act & Assert
        // init-only setters cannot be assigned after initialization, so they are treated as read-only.
        Assert.Null(factory.CreateSetter<string>(nameof(InitOnlyData.Name)));
        Assert.Null(factory.CreateSetter(nameof(InitOnlyData.Name)));
    }

    [Fact]
    public void TestInitOnlyAccessorSetValueThrows()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<InitOnlyData>();
        Assert.NotNull(accessor);
        var data = new InitOnlyData { Id = 1, Name = "abc" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => accessor.SetValue(data, nameof(InitOnlyData.Name), "xyz"));
    }

    [Fact]
    public void TestInitOnlyMembers()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();
        Assert.NotNull(factory);

        // Act
        var members = factory.Members;

        // Assert
        Assert.Equal(2, members.Count);

        var id = members.First(m => m.Name == nameof(InitOnlyData.Id));
        Assert.True(id.CanRead);
        Assert.True(id.CanWrite);

        var name = members.First(m => m.Name == nameof(InitOnlyData.Name));
        Assert.True(name.CanRead);
        Assert.False(name.CanWrite);
    }

    [Fact]
    public void TestNormalPropertyStillWritable()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();
        Assert.NotNull(factory);
        var setId = factory.CreateSetter<int>(nameof(InitOnlyData.Id));
        Assert.NotNull(setId);
        var data = new InitOnlyData { Id = 1 };

        // Act
        setId(data, 99);

        // Assert
        Assert.Equal(99, data.Id);
    }
}
