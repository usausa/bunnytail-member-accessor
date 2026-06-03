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

    [Fact]
    public void TestSameArityOverloadResolvedByArgumentType()
    {
        var ctor = AccessorRegistry.FindConstructor<OverloadCtorData>();

        Assert.NotNull(ctor);

        var fromInt = ctor.Create(42);

        Assert.Equal(42, fromInt.IntValue);
        Assert.Null(fromInt.StringValue);

        var fromString = ctor.Create("hello");

        Assert.Equal("hello", fromString.StringValue);
        Assert.Equal(0, fromString.IntValue);
    }

    [Fact]
    public void TestSameArityOverloadUnmatchedTypeThrows()
    {
        var ctor = AccessorRegistry.FindConstructor<OverloadCtorData>();

        Assert.NotNull(ctor);

        Assert.Throws<NotSupportedException>(() => ctor.Create(1.5));
    }

    [Fact]
    public void TestGenericConstructorPreRegistered()
    {
        var ctor = AccessorRegistry.FindConstructor<GenericData<DateTime>>();

        Assert.NotNull(ctor);

        var instance = ctor.Create();

        Assert.NotNull(instance);
    }

    [Fact]
    public void TestGenericConstructorOnDemand()
    {
        var ctor = AccessorRegistry.FindConstructor<GenericData<int>>();

        Assert.NotNull(ctor);

        var instance = ctor.Create();

        Assert.Equal(0, instance.Value);
    }

    [Fact]
    public void TestGenericConstructorWithArgument()
    {
        var ctor = AccessorRegistry.FindConstructor<GenericHolder<int>>();

        Assert.NotNull(ctor);

        var instance = ctor.Create(123);

        Assert.Equal(123, instance.Value);
    }

    [Fact]
    public void TestGenericConstructorWithArgumentOnDemand()
    {
        var ctor = AccessorRegistry.FindConstructor<GenericHolder<string>>();

        Assert.NotNull(ctor);

        var instance = ctor.Create("abc");

        Assert.Equal("abc", instance.Value);
    }
}
