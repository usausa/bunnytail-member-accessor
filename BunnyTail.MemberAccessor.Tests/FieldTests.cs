namespace BunnyTail.MemberAccessor;

public class FieldTests
{
    [Fact]
    public void TestFieldAccessor()
    {
        // Arrange
        var accessor = AccessorProvider.FindAccessor<FieldData>();
        Assert.NotNull(accessor);

        var data = new FieldData(99) { Count = 1, Tag = "abc", Value = 2 };

        // Act & Assert
        Assert.Equal(1, accessor.GetValue(data, nameof(FieldData.Count)));
        Assert.Equal("abc", accessor.GetValue(data, nameof(FieldData.Tag)));
        Assert.Equal(99, accessor.GetValue(data, nameof(FieldData.Fixed)));
        Assert.Equal(2, accessor.GetValue(data, nameof(FieldData.Value)));

        // Act
        accessor.SetValue(data, nameof(FieldData.Count), 10);
        accessor.SetValue(data, nameof(FieldData.Tag), "xyz");

        // Assert
        Assert.Equal(10, data.Count);
        Assert.Equal("xyz", data.Tag);

        // readonly field is not writable
        Assert.Throws<ArgumentException>(() => accessor.SetValue(data, nameof(FieldData.Fixed), 1));
    }

    [Fact]
    public void TestFieldTypedDelegates()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<FieldData>();
        Assert.NotNull(factory);

        var getCount = factory.CreateGetter<int>(nameof(FieldData.Count));
        var setCount = factory.CreateSetter<int>(nameof(FieldData.Count));
        var getFixed = factory.CreateGetter<int>(nameof(FieldData.Fixed));
        Assert.NotNull(getCount);
        Assert.NotNull(setCount);
        Assert.NotNull(getFixed);

        // readonly field has no setter
        Assert.Null(factory.CreateSetter<int>(nameof(FieldData.Fixed)));

        var data = new FieldData(99) { Count = 1 };

        // Act & Assert
        Assert.Equal(1, getCount(ref data));
        Assert.Equal(99, getFixed(ref data));

        // Act
        setCount(ref data, 10);

        // Assert
        Assert.Equal(10, data.Count);
    }

    [Fact]
    public void TestFieldMembers()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<FieldData>();
        Assert.NotNull(factory);

        // Act
        var members = factory.Members;

        // Assert
        Assert.Equal(4, members.Count);

        var count = members.First(m => m.Name == nameof(FieldData.Count));
        Assert.Equal(MemberKind.Field, count.Kind);
        Assert.True(count.CanRead);
        Assert.True(count.CanWrite);

        var fixedField = members.First(m => m.Name == nameof(FieldData.Fixed));
        Assert.Equal(MemberKind.Field, fixedField.Kind);
        Assert.True(fixedField.CanRead);
        Assert.False(fixedField.CanWrite);

        var value = members.First(m => m.Name == nameof(FieldData.Value));
        Assert.Equal(MemberKind.Property, value.Kind);
    }

    [Fact]
    public void TestStructFieldAccessor()
    {
        // Arrange
        var accessor = AccessorProvider.FindAccessor<StructFieldData>();
        var factory = AccessorProvider.FindFactory<StructFieldData>();
        Assert.NotNull(accessor);
        Assert.NotNull(factory);

        object boxed = new StructFieldData { X = 1, Y = "abc" };

        // Act
        accessor.SetValue(boxed, nameof(StructFieldData.X), 2);

        // Assert
        Assert.Equal(2, ((StructFieldData)boxed).X);
        Assert.Equal(2, accessor.GetValue(boxed, nameof(StructFieldData.X)));

        // Arrange
        var setX = factory.CreateSetter<int>(nameof(StructFieldData.X));
        var getY = factory.CreateGetter<string>(nameof(StructFieldData.Y));
        Assert.NotNull(setX);
        Assert.NotNull(getY);

        var data = new StructFieldData { X = 1, Y = "abc" };

        // Act
        setX(ref data, 99);

        // Assert
        Assert.Equal(99, data.X);
        Assert.Equal("abc", getY(ref data));
    }
}
