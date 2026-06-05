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
public record struct StructData
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

// Same-arity constructor overload test data
[GenerateAccessor]
public class OverloadCtorData
{
    public int IntValue { get; }

    public string? StringValue { get; }

    public OverloadCtorData(int intValue) => IntValue = intValue;

    public OverloadCtorData(string stringValue) => StringValue = stringValue;
}

// Generic constructor accessor test data
[GenerateAccessor]
[TypedAccessor(typeof(GenericHolder<int>))]
public class GenericHolder<T>
{
    public T Value { get; }

    public GenericHolder() => Value = default!;

    public GenericHolder(T value) => Value = value;
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

// Property collection filtering test data (public instance properties only)
[GenerateAccessor]
public class FilterData
{
    public int Value { get; set; }

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    public int ReadOnly { get; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Local
    public int ReadPublicWritePrivate { get; private set; }

    public static int Shared { get; set; }

    internal int Internal { get; set; }

    public int this[int index] => index;
}
