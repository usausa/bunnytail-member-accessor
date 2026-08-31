#pragma warning disable CA1815
namespace BunnyTail.MemberAccessor.AotTests;

//--------------------------------------------------------------------------------
// Reference type
//--------------------------------------------------------------------------------

#pragma warning disable CA1724
[GenerateAccessor]
public sealed partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
#pragma warning restore CA1724

//--------------------------------------------------------------------------------
// Value type
//--------------------------------------------------------------------------------

[GenerateAccessor]
public partial record struct StructData
{
    public int Id { get; set; }

    public string Name { get; set; }
}

//--------------------------------------------------------------------------------
// init-only property (treated as read-only)
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed partial class InitOnlyData
{
    public int Id { get; set; }

    public string Name { get; init; } = default!;
}

//--------------------------------------------------------------------------------
// Inheritance (inherited properties are collected)
//--------------------------------------------------------------------------------

[GenerateAccessor]
public partial class BaseData
{
    public int Id { get; set; }
}

[GenerateAccessor]
public sealed partial class DerivedData : BaseData
{
    public string Name { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Constructor accessor
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed partial class CtorData
{
    public int Id { get; }

    public string Name { get; }

    public CtorData()
    {
        Id = 0;
        Name = "default";
    }

    public CtorData(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

//--------------------------------------------------------------------------------
// Same-arity overloaded constructor (resolved by argument type)
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed partial class OverloadCtorData
{
    public int IntValue { get; }

    public string? StringValue { get; }

    public OverloadCtorData(int intValue)
    {
        IntValue = intValue;
    }

    public OverloadCtorData(string stringValue)
    {
        StringValue = stringValue;
    }
}

//--------------------------------------------------------------------------------
// Generic (closed types pre-registered via assembly-level [TypedAccessor])
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed partial class GenericData<T>
{
    public T Value { get; set; } = default!;
}

[GenerateAccessor]
public sealed partial class GenericHolder<T>
{
    public T Value { get; }

    public GenericHolder()
    {
        Value = default!;
    }

    public GenericHolder(T value)
    {
        Value = value;
    }
}
