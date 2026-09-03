namespace BunnyTail.MemberAccessor;

public class AccessorExtensionsTests
{
    private sealed class Holder
    {
        public Data Item { get; set; } = default!;
    }

    [Fact]
    public void TestReadWriteViaProperty()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<Data>();
        Assert.NotNull(factory);

        var getName = factory.CreateGetter<string>(nameof(Data.Name));
        var setName = factory.CreateSetter<string>(nameof(Data.Name));
        Assert.NotNull(getName);
        Assert.NotNull(setName);

        var holder = new Holder { Item = new Data { Id = 1, Name = "abc" } };

        // Act & Assert
        Assert.Equal("abc", getName.Read(holder.Item));

        // Act
        setName.Write(holder.Item, "xyz");

        // Assert
        Assert.Equal("xyz", holder.Item.Name);
    }

    [Fact]
    public void TestReadWriteViaListElement()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<Data>();
        Assert.NotNull(factory);

        var getId = factory.CreateGetter<int>(nameof(Data.Id));
        var setId = factory.CreateSetter<int>(nameof(Data.Id));
        Assert.NotNull(getId);
        Assert.NotNull(setId);

        var list = new List<Data> { new() { Id = 1, Name = "abc" } };

        // Act & Assert
        Assert.Equal(1, getId.Read(list[0]));

        // Act
        setId.Write(list[0], 99);

        // Assert
        Assert.Equal(99, list[0].Id);
    }

    [Fact]
    public void TestWriteAffectsOriginalInstance()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<Data>();
        Assert.NotNull(factory);

        var setId = factory.CreateSetter<int>(nameof(Data.Id));
        Assert.NotNull(setId);

        var data = new Data { Id = 1, Name = "abc" };
        var alias = data;

        // Act
        setId.Write(data, 99);

        // Assert
        Assert.Equal(99, data.Id);
        Assert.Equal(99, alias.Id);
    }

    [Fact]
    public void TestReadWriteInForEach()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<Data>();
        Assert.NotNull(factory);

        var setId = factory.CreateSetter<int>(nameof(Data.Id));
        Assert.NotNull(setId);

        var items = new[]
        {
            new Data { Id = 1, Name = "a" },
            new Data { Id = 2, Name = "b" }
        };

        // Act
        foreach (var item in items)
        {
            setId.Write(item, 0);
        }

        // Assert
        Assert.All(items, x => Assert.Equal(0, x.Id));
    }

    [Fact]
    public void TestExtensionMatchesRefInvocation()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<Data>();
        Assert.NotNull(factory);

        var getName = factory.CreateGetter<string>(nameof(Data.Name));
        Assert.NotNull(getName);

        var data = new Data { Id = 1, Name = "abc" };

        // Act & Assert
        Assert.Equal(getName(ref data), getName.Read(data));
    }

    [Fact]
    public void TestStructReadWriteViaVariable()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<StructData>();
        Assert.NotNull(factory);

        var getId = factory.CreateGetter<int>(nameof(StructData.Id));
        var setId = factory.CreateSetter<int>(nameof(StructData.Id));
        Assert.NotNull(getId);
        Assert.NotNull(setId);

        var data = new StructData { Id = 1, Name = "abc" };

        // Act & Assert
        Assert.Equal(1, getId.Read(data));

        // Act
        setId.Write(data, 99);

        // Assert
        Assert.Equal(99, data.Id);
    }

    [Fact]
    public void TestStructWriteViaArrayElement()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<StructData>();
        Assert.NotNull(factory);

        var setId = factory.CreateSetter<int>(nameof(StructData.Id));
        Assert.NotNull(setId);

        var items = new[] { new StructData { Id = 1, Name = "a" } };

        // Act
        setId.Write(items[0], 99);

        // Assert
        Assert.Equal(99, items[0].Id);
    }

    [Fact]
    public void TestStructWriteToTemporaryCopyIsLost()
    {
        // Arrange
        var factory = AccessorProvider.FindFactory<StructData>();
        Assert.NotNull(factory);

        var setId = factory.CreateSetter<int>(nameof(StructData.Id));
        Assert.NotNull(setId);

        var list = new List<StructData> { new() { Id = 1, Name = "a" } };

        // Act
        // List<T> indexer returns a copy
        setId.Write(list[0], 99);

        // Assert
        Assert.Equal(1, list[0].Id);
    }
}
