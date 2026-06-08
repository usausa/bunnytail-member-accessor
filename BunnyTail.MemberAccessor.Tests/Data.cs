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

// record (class) support - mutable properties, treated as a reference type
[GenerateAccessor]
public record RecordData
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

// Positional record (class) - primary constructor with init-only properties
[GenerateAccessor]
public record PositionalRecord(int Id, string Name);

// init-only property support - init setters are treated as read-only
[GenerateAccessor]
public class InitOnlyData
{
    public int Id { get; set; }

    public string Name { get; init; } = default!;
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

// Higher-arity constructor accessor test data (5-8 parameters)
[GenerateAccessor]
public class MultiArgCtorData
{
    public int P1 { get; }

    public int P2 { get; }

    public int P3 { get; }

    public int P4 { get; }

    public int P5 { get; }

    public int P6 { get; }

    public int P7 { get; }

    public int P8 { get; }

    public MultiArgCtorData(int p1, int p2, int p3, int p4, int p5)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        P5 = p5;
    }

    public MultiArgCtorData(int p1, int p2, int p3, int p4, int p5, int p6)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        P5 = p5;
        P6 = p6;
    }

    public MultiArgCtorData(int p1, int p2, int p3, int p4, int p5, int p6, int p7)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        P5 = p5;
        P6 = p6;
        P7 = p7;
    }

    public MultiArgCtorData(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        P5 = p5;
        P6 = p6;
        P7 = p7;
        P8 = p8;
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
