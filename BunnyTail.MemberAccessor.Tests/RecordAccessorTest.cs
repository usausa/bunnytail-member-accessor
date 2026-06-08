namespace BunnyTail.MemberAccessor;

public class RecordAccessorTest
{
    [Fact]
    public void TestRecordClassFactoryGetSet()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<RecordData>();
        Assert.NotNull(factory);
        var getId = factory.CreateGetter<int>(nameof(RecordData.Id));
        var setId = factory.CreateSetter<int>(nameof(RecordData.Id));
        var getName = factory.CreateGetter<string>(nameof(RecordData.Name));
        var setName = factory.CreateSetter<string>(nameof(RecordData.Name));
        Assert.NotNull(getId);
        Assert.NotNull(setId);
        Assert.NotNull(getName);
        Assert.NotNull(setName);
        var data = new RecordData { Id = 1, Name = "abc" };

        // Act & Assert (get)
        Assert.Equal(1, getId(data));
        Assert.Equal("abc", getName(data));

        // Act
        setId(data, 2);
        setName(data, "xyz");

        // Assert
        Assert.Equal(2, data.Id);
        Assert.Equal("xyz", data.Name);
    }

    [Fact]
    public void TestRecordClassAccessor()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<RecordData>();
        Assert.NotNull(accessor);
        var data = new RecordData { Id = 1, Name = "abc" };

        // Act & Assert (get)
        Assert.Equal(1, accessor.GetValue(data, nameof(RecordData.Id)));

        // Act
        accessor.SetValue(data, nameof(RecordData.Name), "xyz");

        // Assert
        Assert.Equal("xyz", data.Name);
    }

    [Fact]
    public void TestRecordClassMembers()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<RecordData>();
        Assert.NotNull(factory);

        // Act
        var members = factory.Members;

        // Assert
        Assert.Equal(2, members.Count);
        Assert.All(members, x =>
        {
            Assert.True(x.CanRead);
            Assert.True(x.CanWrite);
        });
    }

    [Fact]
    public void TestRecordClassConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<RecordData>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create();

        // Assert
        Assert.NotNull(instance);
        Assert.Equal(0, instance.Id);
    }

    [Fact]
    public void TestPositionalRecordPropertiesAreReadOnly()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<PositionalRecord>();
        Assert.NotNull(factory);
        var data = new PositionalRecord(1, "abc");

        // Act & Assert
        // init-only positional properties are readable...
        var getId = factory.CreateGetter<int>(nameof(PositionalRecord.Id));
        Assert.NotNull(getId);
        Assert.Equal(1, getId(data));

        // ...but not writable (init setters are treated as read-only).
        Assert.Null(factory.CreateSetter<int>(nameof(PositionalRecord.Id)));
        Assert.Null(factory.CreateSetter<string>(nameof(PositionalRecord.Name)));

        var members = factory.Members;
        Assert.Equal(2, members.Count);
        Assert.All(members, x =>
        {
            Assert.True(x.CanRead);
            Assert.False(x.CanWrite);
        });
    }

    [Fact]
    public void TestPositionalRecordConstructor()
    {
        // Arrange
        var ctor = AccessorRegistry.FindConstructor<PositionalRecord>();
        Assert.NotNull(ctor);

        // Act
        var instance = ctor.Create(7, "hello");

        // Assert
        Assert.Equal(7, instance.Id);
        Assert.Equal("hello", instance.Name);
    }
}
