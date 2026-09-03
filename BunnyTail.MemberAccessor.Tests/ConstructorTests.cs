namespace BunnyTail.MemberAccessor;

public class ConstructorTests
{
    private static Type GetRuntimeType<T>() => typeof(T);

    [Fact]
    public void TestParameterlessConstructor()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor<CtorData0>();
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
        var ctor = AccessorProvider.FindConstructor<CtorData1>();
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
        var ctor = AccessorProvider.FindConstructor<CtorData2>();
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
        var ctor = AccessorProvider.FindConstructor<MultiArgCtorData>();
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
        var ctor = AccessorProvider.FindConstructor<MultiArgCtorData>();
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
        var ctor = AccessorProvider.FindConstructor<MultiArgCtorData>();
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
        var ctor = AccessorProvider.FindConstructor<MultiArgCtorData>();
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
    public void TestNineParameterConstructor()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor<WideCtorData>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(1, 2, 3, 4, 5, 6, 7, 8, 9);

        // Assert
        Assert.Equal(1, instance.P1);
        Assert.Equal(5, instance.P5);
        Assert.Equal(9, instance.P9);
        Assert.Equal(0, instance.P16);
    }

    [Fact]
    public void TestSixteenParameterConstructor()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor<WideCtorData>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);

        // Assert
        Assert.Equal(1, instance.P1);
        Assert.Equal(8, instance.P8);
        Assert.Equal(9, instance.P9);
        Assert.Equal(16, instance.P16);
    }

    [Fact]
    public void TestWideUnsupportedArityThrows()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor<WideCtorData>();
        Assert.NotNull(ctor);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => ctor.Create(1, 2, 3, 4, 5, 6, 7, 8, 9, 10));
    }

    [Fact]
    public void TestUnsupportedArityThrows()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor<CtorData1>();
        Assert.NotNull(ctor);

        // Act & Assert
        Assert.Throws<NotSupportedException>(ctor.Create);
    }

    [Fact]
    public void TestSameArityOverloadResolvedByArgumentType()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor<OverloadCtorData>();
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
        var ctor = AccessorProvider.FindConstructor<OverloadCtorData>();
        Assert.NotNull(ctor);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => ctor.Create(1.5));
    }

    [Fact]
    public void TestCreateInstanceParameterless()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor(GetRuntimeType<CtorData0>());
        Assert.NotNull(ctor);

        // Act
        var instance = Assert.IsType<CtorData0>(ctor.CreateInstance());

        // Assert
        Assert.Equal(0, instance.Id);
    }

    [Fact]
    public void TestCreateInstanceWithArguments()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor(GetRuntimeType<CtorData2>());
        Assert.NotNull(ctor);

        // Act
        var instance = Assert.IsType<CtorData2>(ctor.CreateInstance(99, "hello"));

        // Assert
        Assert.Equal(99, instance.Id);
        Assert.Equal("hello", instance.Name);
    }

    [Fact]
    public void TestCreateInstanceStruct()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor(GetRuntimeType<StructData>());
        Assert.NotNull(ctor);

        // Act
        var instance = Assert.IsType<StructData>(ctor.CreateInstance());

        // Assert
        Assert.Equal(0, instance.Id);
    }

    [Fact]
    public void TestCreateInstanceSameArityOverload()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor(GetRuntimeType<OverloadCtorData>());
        Assert.NotNull(ctor);

        // Act
        var fromInt = Assert.IsType<OverloadCtorData>(ctor.CreateInstance(42));
        var fromString = Assert.IsType<OverloadCtorData>(ctor.CreateInstance("hello"));

        // Assert
        Assert.Equal(42, fromInt.IntValue);
        Assert.Equal("hello", fromString.StringValue);
    }

    [Fact]
    public void TestCreateInstanceNullableParameter()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor(GetRuntimeType<NullableCtorData>());
        Assert.NotNull(ctor);

        // Act
        var fromInt = Assert.IsType<NullableCtorData>(ctor.CreateInstance(5));
        var fromNull = Assert.IsType<NullableCtorData>(ctor.CreateInstance([null]));
        var fromString = Assert.IsType<NullableCtorData>(ctor.CreateInstance("abc"));

        // Assert
        Assert.Equal(5, fromInt.Value);
        Assert.Null(fromNull.Value);
        Assert.Null(fromNull.Text);
        Assert.Equal("abc", fromString.Text);
    }

    [Fact]
    public void TestCreateInstanceUnmatchedThrows()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor(GetRuntimeType<OverloadCtorData>());
        Assert.NotNull(ctor);

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => ctor.CreateInstance(1.5));
        Assert.Throws<NotSupportedException>(() => ctor.CreateInstance(1, 2));
    }

    [Fact]
    public void TestCreateInstanceSharesGenericInstance()
    {
        Assert.Same(AccessorProvider.FindConstructor<CtorData2>(), AccessorProvider.FindConstructor(GetRuntimeType<CtorData2>()));
    }

    [Fact]
    public void TestGenericConstructorPreRegistered()
    {
        // Arrange
        var ctor = AccessorProvider.FindConstructor<GenericData<DateTime>>();
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
        var ctor = AccessorProvider.FindConstructor<GenericData<int>>();
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
        var ctor = AccessorProvider.FindConstructor<GenericHolder<int>>();
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
        var ctor = AccessorProvider.FindConstructor<GenericHolder<string>>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create("abc");

        // Assert
        Assert.Equal("abc", instance.Value);
    }
}
