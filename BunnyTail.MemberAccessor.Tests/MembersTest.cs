namespace BunnyTail.MemberAccessor;

public class MembersTest
{
    [Fact]
    public void TestMembersDescriptors()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<Data>();
        Assert.NotNull(factory);

        // Act
        var members = factory.Members;

        // Assert
        Assert.NotNull(members);
        Assert.Equal(2, members.Count);

        var idMember = members.FirstOrDefault(m => m.Name == nameof(Data.Id));
        Assert.NotNull(idMember);
        Assert.Equal(typeof(int), idMember.Type);
        Assert.True(idMember.CanRead);
        Assert.True(idMember.CanWrite);

        var nameMember = members.FirstOrDefault(m => m.Name == nameof(Data.Name));
        Assert.NotNull(nameMember);
        Assert.Equal(typeof(string), nameMember.Type);
        Assert.True(nameMember.CanRead);
        Assert.True(nameMember.CanWrite);
    }

    [Fact]
    public void TestInheritedPropertiesIncluded()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<DerivedData>();
        Assert.NotNull(factory);

        // Act
        var members = factory.Members;

        // Assert
        Assert.NotNull(members);

        var names = members.Select(m => m.Name).ToArray();
        Assert.Contains(nameof(BaseData.Id), names);
        Assert.Contains(nameof(DerivedData.Name), names);
    }

    [Fact]
    public void TestInheritedPropertyAccess()
    {
        // Arrange
        var accessor = AccessorRegistry.FindAccessor<DerivedData>();
        Assert.NotNull(accessor);
        var data = new DerivedData { Id = 5, Name = "derived" };

        // Act & Assert
        Assert.Equal(5, accessor.GetValue(data, nameof(BaseData.Id)));
        Assert.Equal("derived", accessor.GetValue(data, nameof(DerivedData.Name)));

        // Act
        accessor.SetValue(data, nameof(BaseData.Id), 10);

        // Assert
        Assert.Equal(10, data.Id);
    }

    [Fact]
    public void TestPublicInstancePropertiesOnly()
    {
        // Arrange
        var factory = AccessorRegistry.FindFactory<FilterData>();
        Assert.NotNull(factory);

        // Act
        var members = factory.Members;

        // Assert
        Assert.NotNull(members);

        var names = members.Select(m => m.Name).ToArray();

        // Included: public instance properties
        Assert.Equal(3, members.Count);
        Assert.Contains(nameof(FilterData.Value), names);
        Assert.Contains(nameof(FilterData.ReadOnly), names);
        Assert.Contains(nameof(FilterData.ReadPublicWritePrivate), names);

        // Excluded: static, non-public, indexer
        Assert.DoesNotContain("Shared", names);
        Assert.DoesNotContain("Internal", names);
        Assert.DoesNotContain("Item", names);

        var value = members.First(m => m.Name == nameof(FilterData.Value));
        Assert.True(value.CanRead);
        Assert.True(value.CanWrite);

        var readOnly = members.First(m => m.Name == nameof(FilterData.ReadOnly));
        Assert.True(readOnly.CanRead);
        Assert.False(readOnly.CanWrite);

        // Public getter, non-public setter: only the public accessor counts.
        var readPublicWritePrivate = members.First(m => m.Name == nameof(FilterData.ReadPublicWritePrivate));
        Assert.True(readPublicWritePrivate.CanRead);
        Assert.False(readPublicWritePrivate.CanWrite);
    }
}
