namespace BunnyTail.MemberAccessor;

public class AccessorTest
{
    [Fact]
    public void TestBasic()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<Data>();
        Assert.NotNull(accessor);
        var data = new Data { Id = 123, Name = "abc" };

        // Act & Assert
        Assert.Equal(123, accessor.GetValue(data, nameof(data.Id)));
        Assert.Equal("abc", accessor.GetValue(data, nameof(data.Name)));

        // Act
        accessor.SetValue(data, nameof(data.Id), 234);
        accessor.SetValue(data, nameof(data.Name), "xyz");

        // Assert
        Assert.Equal(234, data.Id);
        Assert.Equal("xyz", data.Name);
    }

    [Fact]
    public void TestNullable()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<NullableData>();
        Assert.NotNull(accessor);
        var data = new NullableData { Id = 123, Name = "abc" };

        // Act & Assert
        Assert.Equal(123, accessor.GetValue(data, nameof(data.Id)));
        Assert.Equal("abc", accessor.GetValue(data, nameof(data.Name)));

        // Act
        accessor.SetValue(data, nameof(data.Id), 234);
        accessor.SetValue(data, nameof(data.Name), "xyz");

        // Assert
        Assert.Equal(234, data.Id);
        Assert.Equal("xyz", data.Name);

        // Act
        accessor.SetValue(data, nameof(data.Id), null);
        accessor.SetValue(data, nameof(data.Name), null);

        // Assert
        Assert.Null(data.Id);
        Assert.Null(data.Name);
    }

    [Fact]
    public void TestGenerics()
    {
        // Arrange
        var accessor1 = AccessorRegistry.FindAccessor<GenericData<int>>();
        var accessor2 = AccessorRegistry.FindAccessor<GenericData<string>>();
        Assert.NotNull(accessor1);
        Assert.NotNull(accessor2);
        var data1 = new GenericData<int> { Value = 123 };

        // Act & Assert
        Assert.Equal(123, accessor1.GetValue(data1, nameof(data1.Value)));

        // Act
        accessor1.SetValue(data1, nameof(data1.Value), 234);

        // Assert
        Assert.Equal(234, data1.Value);

        // Arrange
        var data2 = new GenericData<string> { Value = "abc" };

        // Act & Assert
        Assert.Equal("abc", accessor2.GetValue(data2, nameof(data2.Value)));

        // Act
        accessor2.SetValue(data2, nameof(data2.Value), "xyz");

        // Assert
        Assert.Equal("xyz", data2.Value);
    }

    [Fact]
    public void TestMultiGenerics()
    {
        // Arrange
        var accessor1 = AccessorRegistry.FindAccessor<MultiGenericData<int, int>>();
        var accessor2 = AccessorRegistry.FindAccessor<MultiGenericData<string, string>>();
        Assert.NotNull(accessor1);
        Assert.NotNull(accessor2);
        var data1 = new MultiGenericData<int, int> { Value1 = 123 };

        // Act & Assert
        Assert.Equal(123, accessor1.GetValue(data1, nameof(data1.Value1)));

        // Act
        accessor1.SetValue(data1, nameof(data1.Value1), 234);

        // Assert
        Assert.Equal(234, data1.Value1);

        // Arrange
        var data2 = new MultiGenericData<string, string> { Value2 = "abc" };

        // Act & Assert
        Assert.Equal("abc", accessor2.GetValue(data2, nameof(data2.Value2)));

        // Act
        accessor2.SetValue(data2, nameof(data2.Value2), "xyz");

        // Assert
        Assert.Equal("xyz", data2.Value2);
    }
}
