namespace BunnyTail.MemberAccessor;

public class NameMismatchTest
{
    private const string UnknownName = "NoSuchProperty";

    [Fact]
    public void TestAccessorGetValueThrows()
    {
        var accessor = AccessorRegistry.FindAccessor<Data>();

        Assert.NotNull(accessor);

        var data = new Data { Id = 1, Name = "abc" };

        var ex = Assert.Throws<ArgumentException>(() => accessor.GetValue(data, UnknownName));
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void TestAccessorSetValueThrows()
    {
        var accessor = AccessorRegistry.FindAccessor<Data>();

        Assert.NotNull(accessor);

        var data = new Data { Id = 1, Name = "abc" };

        var ex = Assert.Throws<ArgumentException>(() => accessor.SetValue(data, UnknownName, 1));
        Assert.Equal("name", ex.ParamName);
    }

    [Fact]
    public void TestFactoryTypedCreateReturnsNull()
    {
        var factory = AccessorRegistry.FindFactory<Data>();

        Assert.NotNull(factory);

        Assert.Null(factory.CreateGetter<int>(UnknownName));
        Assert.Null(factory.CreateSetter<int>(UnknownName));
    }

    [Fact]
    public void TestFactoryObjectCreateReturnsNull()
    {
        var factory = AccessorRegistry.FindFactory<Data>();

        Assert.NotNull(factory);

        Assert.Null(factory.CreateGetter(UnknownName));
        Assert.Null(factory.CreateSetter(UnknownName));
    }
}
