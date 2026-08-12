#pragma warning disable IDE0044
#pragma warning disable SA1401
#pragma warning disable CA1051
#pragma warning disable CA1812
namespace BunnyTail.MemberAccessor;

[GenerateAccessor]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

[GenerateAccessor]
public partial class NullableData
{
    public int? Id { get; set; }

    public string? Name { get; set; }
}

[GenerateAccessor]
[TypedAccessor(typeof(GenericData<DateTime>))]
[TypedAccessor(typeof(GenericData<short>))]
public partial class GenericData<T>
{
    public T Value { get; set; } = default!;
}

[GenerateAccessor]
[TypedAccessor(typeof(MultiGenericData<string, string>))]
public partial class MultiGenericData<T1, T2>
{
    public T1 Value1 { get; set; } = default!;

    public T2 Value2 { get; set; } = default!;
}

// Struct support
[GenerateAccessor]
public partial record struct StructData
{
    public int Id { get; set; }

    public string Name { get; set; }
}

// record (class) support - mutable properties, treated as a reference type
[GenerateAccessor]
public partial record RecordData
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

// Positional record (class) - primary constructor with init-only properties
[GenerateAccessor]
public partial record PositionalRecord(int Id, string Name);

// init-only property support - init setters are treated as read-only
[GenerateAccessor]
public partial class InitOnlyData
{
    public int Id { get; set; }

    public string Name { get; init; } = default!;
}

// Constructor accessor test data
[GenerateAccessor]
public partial class CtorData0
{
    public int Id { get; set; }
}

[GenerateAccessor]
public partial class CtorData1
{
    public int Id { get; }

    public CtorData1(int id) => Id = id;
}

[GenerateAccessor]
public partial class CtorData2
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
public partial class MultiArgCtorData
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

// Wide-arity constructor accessor test data (9 and 16 parameters)
[GenerateAccessor]
public partial class WideCtorData
{
    public int P1 { get; }

    public int P2 { get; }

    public int P3 { get; }

    public int P4 { get; }

    public int P5 { get; }

    public int P6 { get; }

    public int P7 { get; }

    public int P8 { get; }

    public int P9 { get; }

    public int P10 { get; }

    public int P11 { get; }

    public int P12 { get; }

    public int P13 { get; }

    public int P14 { get; }

    public int P15 { get; }

    public int P16 { get; }

    public WideCtorData(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8, int p9)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        P5 = p5;
        P6 = p6;
        P7 = p7;
        P8 = p8;
        P9 = p9;
    }

    public WideCtorData(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8, int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16)
    {
        P1 = p1;
        P2 = p2;
        P3 = p3;
        P4 = p4;
        P5 = p5;
        P6 = p6;
        P7 = p7;
        P8 = p8;
        P9 = p9;
        P10 = p10;
        P11 = p11;
        P12 = p12;
        P13 = p13;
        P14 = p14;
        P15 = p15;
        P16 = p16;
    }
}

// Same-arity constructor overload test data
[GenerateAccessor]
public partial class OverloadCtorData
{
    public int IntValue { get; }

    public string? StringValue { get; }

    public OverloadCtorData(int intValue) => IntValue = intValue;

    public OverloadCtorData(string stringValue) => StringValue = stringValue;
}

// Generic constructor accessor test data
[GenerateAccessor]
[TypedAccessor(typeof(GenericHolder<int>))]
public partial class GenericHolder<T>
{
    public T Value { get; }

    public GenericHolder() => Value = default!;

    public GenericHolder(T value) => Value = value;
}

// Inherited properties test data
[GenerateAccessor]
public partial class BaseData
{
    public int Id { get; set; }
}

[GenerateAccessor]
public partial class DerivedData : BaseData
{
    public string Name { get; set; } = default!;
}

// Property collection filtering test data (public instance properties only)
// This type is deliberately not partial: accessor generation must still work (BTMA0006 info)
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

// Field accessor test data
[GenerateAccessor]
public partial class FieldData
{
    public int Count;

    public string Tag = string.Empty;

    public readonly int Fixed;

    public int Value { get; set; }

    public FieldData()
    {
    }

    public FieldData(int fixedValue) => Fixed = fixedValue;
}

[GenerateAccessor]
public partial record struct StructFieldData
{
    public int X;

    public string Y;
}

// AccessorMember(Ignore) test data
[GenerateAccessor]
public partial class IgnoreData
{
    public int Value { get; set; }

    [AccessorMember(Ignore = true)]
    public string Secret { get; set; } = default!;

    [AccessorMember(Ignore = true)]
    public int IgnoredField;
}

// Non-public member opt-in test data
[GenerateAccessor]
public partial class HiddenData
{
    public int Id { get; set; }

    [AccessorMember]
    internal string InternalValue { get; set; } = default!;

    [AccessorMember]
    private string Secret { get; set; } = default!;

    [AccessorMember]
    private int counter;

    [AccessorMember]
    public int Score { get; private set; }

    public void SetValues(string secret, int count, int score)
    {
        Secret = secret;
        counter = count;
        Score = score;
    }

    public (string Secret, int Counter) ReadValues() => (Secret, counter);
}

[GenerateAccessor]
public partial record struct StructHiddenData
{
    public int Id { get; set; }

    [AccessorMember]
    private int hidden;

    public StructHiddenData(int id, int hidden)
    {
        Id = id;
        this.hidden = hidden;
    }

    public readonly int ReadHidden() => hidden;
}

// Non-public member opt-in on a generic type (UnsafeAccessor with generics requires .NET 9+)
[GenerateAccessor]
[TypedAccessor(typeof(GenericHiddenData<int>))]
public partial class GenericHiddenData<T>
{
    [AccessorMember]
    private T hidden = default!;

    public T ReadHidden() => hidden;
}

// External type test data (no [GenerateAccessor])
public class PlainData
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public class PlainGenericData<T>
{
    public T Value { get; set; } = default!;

    public PlainGenericData()
    {
    }

    public PlainGenericData(T value) => Value = value;
}

// Witness type: accessor generation for types it does not own + IAccessorProvider implementations
[GenerateAccessorFor(typeof(PlainData))]
[GenerateAccessorFor(typeof(PlainGenericData<int>))]
[GenerateAccessorFor(typeof(Data))]
internal sealed partial class AccessorWitness;
