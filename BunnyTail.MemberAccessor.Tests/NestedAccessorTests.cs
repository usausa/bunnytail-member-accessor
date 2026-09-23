namespace BunnyTail.MemberAccessor;

public class NestedAccessorTests
{
    [Fact]
    public void TestNestedProviderClass()
    {
        // Arrange
        var accessor = AccessorProvider.GetAccessor<NestedOuter.NestedData>();
        var data = new NestedOuter.NestedData { Id = 1, Name = "abc" };

        // Act
        accessor.SetValue(data, nameof(NestedOuter.NestedData.Name), "xyz");

        // Assert
        Assert.Equal(1, accessor.GetValue(data, nameof(NestedOuter.NestedData.Id)));
        Assert.Equal("xyz", data.Name);
    }

    [Fact]
    public void TestNestedProviderSharesRegistryInstance()
    {
        Assert.Same(AccessorProvider.FindAccessor<NestedOuter.NestedData>(), AccessorProvider.GetAccessor<NestedOuter.NestedData>());
    }

    [Fact]
    public void TestNestedInNonPartialOuterFoundInRegistry()
    {
        // Arrange
        var accessor = AccessorProvider.FindAccessor<NestedPlainOuter.NestedData>();
        var data = new NestedPlainOuter.NestedData { Id = 1 };

        // Act & Assert
        Assert.NotNull(accessor);
        Assert.Equal(1, accessor.GetValue(data, nameof(NestedPlainOuter.NestedData.Id)));
    }
}
