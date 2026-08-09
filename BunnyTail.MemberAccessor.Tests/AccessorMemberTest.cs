namespace BunnyTail.MemberAccessor;

public class AccessorMemberTest
{
    [Fact]
    public void TestIgnoredMembersExcluded()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<IgnoreData>();
        Assert.NotNull(factory);

        // Act
        var names = factory.Members.Select(m => m.Name).ToArray();

        // Assert
        Assert.Contains(nameof(IgnoreData.Value), names);
        Assert.DoesNotContain(nameof(IgnoreData.Secret), names);
        Assert.DoesNotContain(nameof(IgnoreData.IgnoredField), names);

        Assert.Null(factory.CreateGetter<string>(nameof(IgnoreData.Secret)));
        Assert.Null(factory.CreateSetter<string>(nameof(IgnoreData.Secret)));
    }

    [Fact]
    public void TestIgnoredMemberAccessorThrows()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<IgnoreData>();
        Assert.NotNull(accessor);

        var data = new IgnoreData { Value = 1, Secret = "abc" };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => accessor.GetValue(data, nameof(IgnoreData.Secret)));
    }

    [Fact]
    public void TestInternalMemberOptIn()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<HiddenData>();
        Assert.NotNull(factory);

        var get = factory.CreateGetter<string>("InternalValue");
        var set = factory.CreateSetter<string>("InternalValue");
        Assert.NotNull(get);
        Assert.NotNull(set);

        var data = new HiddenData { InternalValue = "abc" };

        // Act & Assert
        Assert.Equal("abc", get(ref data));

        // Act
        set(ref data, "xyz");

        // Assert
        Assert.Equal("xyz", data.InternalValue);
    }

    [Fact]
    public void TestPrivatePropertyOptIn()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<HiddenData>();
        Assert.NotNull(factory);

        var get = factory.CreateGetter<string>("Secret");
        var set = factory.CreateSetter<string>("Secret");
        Assert.NotNull(get);
        Assert.NotNull(set);

        var data = new HiddenData();
        data.SetValues("abc", 1, 2);

        // Act & Assert
        Assert.Equal("abc", get(ref data));

        // Act
        set(ref data, "xyz");

        // Assert
        Assert.Equal("xyz", data.ReadValues().Secret);
    }

    [Fact]
    public void TestPrivateFieldOptIn()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<HiddenData>();
        var factory = AccessorRegistry.FindFactory<HiddenData>();
        Assert.NotNull(accessor);
        Assert.NotNull(factory);

        var data = new HiddenData();
        data.SetValues("abc", 10, 2);

        // Act & Assert (object-based)
        Assert.Equal(10, accessor.GetValue(data, "counter"));
        accessor.SetValue(data, "counter", 20);
        Assert.Equal(20, data.ReadValues().Counter);

        // Act & Assert (typed)
        var get = factory.CreateGetter<int>("counter");
        var set = factory.CreateSetter<int>("counter");
        Assert.NotNull(get);
        Assert.NotNull(set);
        Assert.Equal(20, get(ref data));
        set(ref data, 30);
        Assert.Equal(30, data.ReadValues().Counter);
    }

    [Fact]
    public void TestPrivateSetterOptIn()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<HiddenData>();
        Assert.NotNull(factory);

        // public getter + private setter with [AccessorMember]: both usable
        var get = factory.CreateGetter<int>(nameof(HiddenData.Score));
        var set = factory.CreateSetter<int>(nameof(HiddenData.Score));
        Assert.NotNull(get);
        Assert.NotNull(set);

        var data = new HiddenData();
        data.SetValues("abc", 1, 5);

        // Act & Assert
        Assert.Equal(5, get(ref data));

        // Act
        set(ref data, 6);

        // Assert
        Assert.Equal(6, data.Score);
    }

    [Fact]
    public void TestNonPublicMembersIncludedInMembers()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<HiddenData>();
        Assert.NotNull(factory);

        // Act
        var members = factory.Members;

        // Assert
        var names = members.Select(m => m.Name).ToArray();
        Assert.Contains("InternalValue", names);
        Assert.Contains("Secret", names);
        Assert.Contains("counter", names);
        Assert.Contains(nameof(HiddenData.Score), names);

        var score = members.First(m => m.Name == nameof(HiddenData.Score));
        Assert.True(score.CanRead);
        Assert.True(score.CanWrite);

        var counter = members.First(m => m.Name == "counter");
        Assert.Equal(MemberKind.Field, counter.Kind);
    }

    [Fact]
    public void TestStructPrivateFieldOptIn()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<StructHiddenData>();
        Assert.NotNull(factory);

        var get = factory.CreateGetter<int>("hidden");
        var set = factory.CreateSetter<int>("hidden");
        Assert.NotNull(get);
        Assert.NotNull(set);

        var data = new StructHiddenData(1, 10);

        // Act & Assert
        Assert.Equal(10, get(ref data));

        // Act
        set(ref data, 20);

        // Assert
        Assert.Equal(20, data.ReadHidden());
    }

    [Fact]
    public void TestGenericPrivateMemberOptIn()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<GenericHiddenData<int>>();
        Assert.NotNull(factory);

        var get = factory.CreateGetter<int>("hidden");
        var set = factory.CreateSetter<int>("hidden");
        Assert.NotNull(get);
        Assert.NotNull(set);

        var data = new GenericHiddenData<int>();

        // Act
        set(ref data, 123);

        // Assert
        Assert.Equal(123, get(ref data));
        Assert.Equal(123, data.ReadHidden());
    }
}
