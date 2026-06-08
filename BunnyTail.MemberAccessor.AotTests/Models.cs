#pragma warning disable CA1815
namespace BunnyTail.MemberAccessor.AotTests;

//--------------------------------------------------------------------------------
// Reference type
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Value type
//--------------------------------------------------------------------------------

[GenerateAccessor]
public record struct StructData
{
    public int Id { get; set; }

    public string Name { get; set; }
}

//--------------------------------------------------------------------------------
// init-only property (treated as read-only)
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed class InitOnlyData
{
    public int Id { get; set; }

    public string Name { get; init; } = default!;
}

//--------------------------------------------------------------------------------
// Inheritance (inherited properties are collected)
//--------------------------------------------------------------------------------

[GenerateAccessor]
public class BaseData
{
    public int Id { get; set; }
}

[GenerateAccessor]
public sealed class DerivedData : BaseData
{
    public string Name { get; set; } = default!;
}

//--------------------------------------------------------------------------------
// Constructor accessor
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed class CtorData
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
public sealed class OverloadCtorData
{
    public int IntValue { get; }

    public string? StringValue { get; }

    public OverloadCtorData(int intValue) => IntValue = intValue;

    public OverloadCtorData(string stringValue) => StringValue = stringValue;
}

//--------------------------------------------------------------------------------
// Generic (closed types pre-registered via assembly-level [TypedAccessor])
//--------------------------------------------------------------------------------

[GenerateAccessor]
public sealed class GenericData<T>
{
    public T Value { get; set; } = default!;
}

[GenerateAccessor]
public sealed class GenericHolder<T>
{
    public T Value { get; }

    public GenericHolder() => Value = default!;

    public GenericHolder(T value) => Value = value;
}
