namespace BunnyTail.MemberAccessor;

public class ConstructorAccessorTest
{
    [Fact]
    public void TestParameterlessConstructor()
    {
        var ctor = AccessorRegistry.FindConstructor<CtorData0>();

        Assert.NotNull(ctor);

        var instance = ctor.Create();

        Assert.NotNull(instance);
        Assert.Equal(0, instance.Id);
    }

    [Fact]
    public void TestOneParameterConstructor()
    {
        var ctor = AccessorRegistry.FindConstructor<CtorData1>();

        Assert.NotNull(ctor);

        var instance = ctor.Create(42);

        Assert.Equal(42, instance.Id);
    }

    [Fact]
    public void TestTwoParameterConstructor()
    {
        var ctor = AccessorRegistry.FindConstructor<CtorData2>();

        Assert.NotNull(ctor);

        var instance = ctor.Create(99, "hello");

        Assert.Equal(99, instance.Id);
        Assert.Equal("hello", instance.Name);
    }

    [Fact]
    public void TestUnsupportedArityThrows()
    {
        var ctor = AccessorRegistry.FindConstructor<CtorData1>();

        Assert.NotNull(ctor);

        Assert.Throws<NotSupportedException>(ctor.Create);
    }
}
