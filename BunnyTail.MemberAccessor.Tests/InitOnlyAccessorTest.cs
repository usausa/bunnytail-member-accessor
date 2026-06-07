namespace BunnyTail.MemberAccessor;

public class InitOnlyAccessorTest
{
    [Fact]
    public void TestInitOnlyPropertyIsReadable()
    {
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();

        Assert.NotNull(factory);

        var data = new InitOnlyData { Id = 1, Name = "abc" };

        var getName = factory.CreateGetter<string>(nameof(InitOnlyData.Name));
        Assert.NotNull(getName);
        Assert.Equal("abc", getName(data));

        var accessor = AccessorRegistry.FindAccessor<InitOnlyData>();
        Assert.NotNull(accessor);
        Assert.Equal("abc", accessor.GetValue(data, nameof(InitOnlyData.Name)));
    }

    [Fact]
    public void TestInitOnlySetterReturnsNull()
    {
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();

        Assert.NotNull(factory);

        // init-only setters cannot be assigned after initialization, so they are treated as read-only.
        Assert.Null(factory.CreateSetter<string>(nameof(InitOnlyData.Name)));
        Assert.Null(factory.CreateSetter(nameof(InitOnlyData.Name)));
    }

    [Fact]
    public void TestInitOnlyAccessorSetValueThrows()
    {
        var accessor = AccessorRegistry.FindAccessor<InitOnlyData>();

        Assert.NotNull(accessor);

        var data = new InitOnlyData { Id = 1, Name = "abc" };

        Assert.Throws<ArgumentException>(() => accessor.SetValue(data, nameof(InitOnlyData.Name), "xyz"));
    }

    [Fact]
    public void TestInitOnlyMembers()
    {
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();

        Assert.NotNull(factory);

        var members = factory.Members;

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
        var factory = AccessorRegistry.FindFactory<InitOnlyData>();

        Assert.NotNull(factory);

        var setId = factory.CreateSetter<int>(nameof(InitOnlyData.Id));
        Assert.NotNull(setId);

        var data = new InitOnlyData { Id = 1 };
        setId(data, 99);

        Assert.Equal(99, data.Id);
    }
}
