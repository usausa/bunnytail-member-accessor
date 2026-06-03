#pragma warning disable CA1815
namespace BunnyTail.MemberAccessor;

[GenerateAccessor]
public class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

[GenerateAccessor]
public class NullableData
{
    public int? Id { get; set; }

    public string? Name { get; set; }
}

[GenerateAccessor]
[TypedAccessor(typeof(GenericData<DateTime>))]
[TypedAccessor(typeof(GenericData<short>))]
public class GenericData<T>
{
    public T Value { get; set; } = default!;
}

[GenerateAccessor]
[TypedAccessor(typeof(MultiGenericData<string, string>))]
public class MultiGenericData<T1, T2>
{
    public T1 Value1 { get; set; } = default!;

    public T2 Value2 { get; set; } = default!;
}

// Struct support
[GenerateAccessor]
public struct StructData
{
    public int Id { get; set; }

    public string Name { get; set; }
}

// Constructor accessor test data
[GenerateAccessor]
public class CtorData0
{
    public int Id { get; set; }
}

[GenerateAccessor]
public class CtorData1
{
    public int Id { get; }

    public CtorData1(int id) => Id = id;
}

[GenerateAccessor]
public class CtorData2
{
    public int Id { get; }

    public string Name { get; }

    public CtorData2(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

// Inherited properties test data
[GenerateAccessor]
public class BaseData
{
    public int Id { get; set; }
}

[GenerateAccessor]
public class DerivedData : BaseData
{
    public string Name { get; set; } = default!;
}
