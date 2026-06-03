namespace BunnyTail.MemberAccessor;

public class MembersTest
{
    [Fact]
    public void TestMembersDescriptors()
    {
        var factory = AccessorRegistry.FindFactory<Data>();

        Assert.NotNull(factory);

        var members = factory.Members;

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
        var factory = AccessorRegistry.FindFactory<DerivedData>();

        Assert.NotNull(factory);

        var members = factory.Members;

        Assert.NotNull(members);

        var names = members.Select(m => m.Name).ToArray();
        Assert.Contains(nameof(BaseData.Id), names);
        Assert.Contains(nameof(DerivedData.Name), names);
    }

    [Fact]
    public void TestInheritedPropertyAccess()
    {
        var accessor = AccessorRegistry.FindAccessor<DerivedData>();

        Assert.NotNull(accessor);

        var data = new DerivedData { Id = 5, Name = "derived" };

        Assert.Equal(5, accessor.GetValue(data, nameof(BaseData.Id)));
        Assert.Equal("derived", accessor.GetValue(data, nameof(DerivedData.Name)));

        accessor.SetValue(data, nameof(BaseData.Id), 10);

        Assert.Equal(10, data.Id);
    }
}
