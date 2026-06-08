namespace BunnyTail.MemberAccessor;

public class ConstructorTest
{
    [Fact]
    public void TestParameterlessConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<CtorData0>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create();

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(0, instance.Id);
    }

    [Fact]
    public void TestOneParameterConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<CtorData1>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(42);

        // Assert
        Assert.Equal(42, instance.Id);
    }

    [Fact]
    public void TestTwoParameterConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<CtorData2>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(99, "hello");

        // Assert
        Assert.Equal(99, instance.Id);
        Assert.Equal("hello", instance.Name);
    }

    [Fact]
    public void TestFiveParameterConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<MultiArgCtorData>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(1, 2, 3, 4, 5);

        // Assert
        Assert.Equal(1, instance.P1);
        Assert.Equal(2, instance.P2);
        Assert.Equal(3, instance.P3);
        Assert.Equal(4, instance.P4);
        Assert.Equal(5, instance.P5);
    }

    [Fact]
    public void TestSixParameterConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<MultiArgCtorData>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(1, 2, 3, 4, 5, 6);

        // Assert
        Assert.Equal(1, instance.P1);
        Assert.Equal(2, instance.P2);
        Assert.Equal(3, instance.P3);
        Assert.Equal(4, instance.P4);
        Assert.Equal(5, instance.P5);
        Assert.Equal(6, instance.P6);
    }

    [Fact]
    public void TestSevenParameterConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<MultiArgCtorData>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(1, 2, 3, 4, 5, 6, 7);

        // Assert
        Assert.Equal(1, instance.P1);
        Assert.Equal(2, instance.P2);
        Assert.Equal(3, instance.P3);
        Assert.Equal(4, instance.P4);
        Assert.Equal(5, instance.P5);
        Assert.Equal(6, instance.P6);
        Assert.Equal(7, instance.P7);
    }

    [Fact]
    public void TestEightParameterConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<MultiArgCtorData>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(1, 2, 3, 4, 5, 6, 7, 8);

        // Assert
        Assert.Equal(1, instance.P1);
        Assert.Equal(2, instance.P2);
        Assert.Equal(3, instance.P3);
        Assert.Equal(4, instance.P4);
        Assert.Equal(5, instance.P5);
        Assert.Equal(6, instance.P6);
        Assert.Equal(7, instance.P7);
        Assert.Equal(8, instance.P8);
    }

    [Fact]
    public void TestUnsupportedArityThrows()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<CtorData1>();
        Assert.NotNull(ctor);

        // Act & Assert
        Assert.Throws<NotSupportedException>(ctor.Create);
    }

    [Fact]
    public void TestSameArityOverloadResolvedByArgumentType()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<OverloadCtorData>();
        Assert.NotNull(ctor);

        // Act
        var fromInt = ctor.Create(42);

        // Assert
        Assert.Equal(42, fromInt.IntValue);
        Assert.Null(fromInt.StringValue);

        // Act
        var fromString = ctor.Create("hello");

        // Assert
        Assert.Equal("hello", fromString.StringValue);
        Assert.Equal(0, fromString.IntValue);
    }

    [Fact]
    public void TestSameArityOverloadUnmatchedTypeThrows()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<OverloadCtorData>();
        Assert.NotNull(ctor);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => ctor.Create(1.5));
    }

    [Fact]
    public void TestGenericConstructorPreRegistered()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<GenericData<DateTime>>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create();

        // Assert
        Assert.NotNull(instance);
    }

    [Fact]
    public void TestGenericConstructorOnDemand()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<GenericData<int>>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create();

        // Assert
        Assert.Equal(0, instance.Value);
    }

    [Fact]
    public void TestGenericConstructorWithArgument()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<GenericHolder<int>>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(123);

        // Assert
        Assert.Equal(123, instance.Value);
    }

    [Fact]
    public void TestGenericConstructorWithArgumentOnDemand()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<GenericHolder<string>>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create("abc");

        // Assert
        Assert.Equal("abc", instance.Value);
    }
}
