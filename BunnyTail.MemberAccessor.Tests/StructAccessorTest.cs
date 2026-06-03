namespace BunnyTail.MemberAccessor;

public class StructAccessorTest
{
    [Fact]
    public void TestStructAccessorGetValue()
    {
        var accessor = AccessorRegistry.FindAccessor<StructData>();

        Assert.NotNull(accessor);

        object boxed = new StructData { Id = 10, Name = "test" };

        Assert.Equal(10, accessor.GetValue(boxed, nameof(StructData.Id)));
        Assert.Equal("test", accessor.GetValue(boxed, nameof(StructData.Name)));
    }

    [Fact]
    public void TestStructAccessorSetValue()
    {
        var accessor = AccessorRegistry.FindAccessor<StructData>();

        Assert.NotNull(accessor);

        object boxed = new StructData { Id = 10, Name = "test" };

        accessor.SetValue(boxed, nameof(StructData.Id), 99);
        accessor.SetValue(boxed, nameof(StructData.Name), "updated");

        Assert.Equal(99, ((StructData)boxed).Id);
        Assert.Equal("updated", ((StructData)boxed).Name);
    }
}
