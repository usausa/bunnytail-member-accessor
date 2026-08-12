# BunnyTail.MemberAccessor

[![NuGet](https://img.shields.io/nuget/v/BunnyTail.MemberAccessor.svg)](https://www.nuget.org/packages/BunnyTail.MemberAccessor)

AOT-safe source-generated member accessor for .NET. A reflection-free alternative for property get/set, constructor invocation, and member enumeration.

## MemberAccessor

### Basic Usage

```csharp
using BunnyTail.MemberAccessor;

[GenerateAccessor]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
```

```csharp
using BunnyTail.MemberAccessor;

var factory = AccessorProvider.GetFactory<Data>();
var getter = factory.CreateGetter<int>(nameof(Data.Id));
var setter = factory.CreateSetter<int>(nameof(Data.Id));

var data = new Data();
setter(ref data, 123);
var id = getter(ref data);
```

`Read` / `Write` take the target by value, for use where `ref` cannot be applied:

```csharp
var list = new List<Data> { new() { Id = 1 } };
var id = getter.Read(list[0]);
setter.Write(list[0], 2);
```

### Member Enumeration

```csharp
var factory = AccessorProvider.GetFactory<Data>();
foreach (var member in factory.Members)
{
    Console.WriteLine($"{member.Name}: {member.Type} CanRead={member.CanRead} CanWrite={member.CanWrite}");
}
```

### Constructor Accessor

Arity 0–16, including generic types.

```csharp
var ctor = AccessorProvider.GetConstructor<Data>();
var instance = ctor.Create();          // parameterless
var instance2 = ctor.Create<int>(42);  // 1-arg constructor

var generic = AccessorProvider.GetConstructor<GenericHolder<int>>();
var instance3 = generic.Create<int>(42);
```

Same-arity overloads are selected at runtime by argument type — pass the exact parameter type:

```csharp
// class Sample { Sample(int v); Sample(string v); }
var ctor = AccessorProvider.GetConstructor<Sample>();
var a = ctor.Create(42);      // -> Sample(int)
var b = ctor.Create("text");  // -> Sample(string)
var c = ctor.Create(1.0);     // -> NotSupportedException
```

Reflection-style, by `object` array:

```csharp
var ctor = AccessorProvider.FindConstructor(type)!;
var instance = (Data)ctor.CreateInstance(99, "hello");
```

### Struct Support

```csharp
[GenerateAccessor]
public partial struct Point { public int X { get; set; } public int Y { get; set; } }

var accessor = AccessorProvider.GetAccessor<Point>();
object boxed = new Point { X = 1, Y = 2 };
accessor.SetValue(boxed, "X", 10); // modifies the boxed copy

var factory = AccessorProvider.GetFactory<Point>();
var setX = factory.CreateSetter<int>(nameof(Point.X));
var point = new Point();
setX(ref point, 10);    // mutates in place, no boxing
setX.Write(point, 20);  // extension: same syntax as class

var list = new List<Point> { point };
setX.Write(list[0], 30); // lost: the indexer returns a copy
```

## Attributes

| Attribute | Target | Description |
| --- | --- | --- |
| `[GenerateAccessor]` | class, struct | Generates the accessor, factory and constructor accessor for the annotated type |
| `[TypedAccessor(typeof(Foo<int>))]` | assembly, class | Pre-registers a closed instantiation of an open generic type (required for Native AOT) |
| `[AccessorMember]` | property, field | Opts a non-public member in; `Ignore = true` excludes the member instead |
| `[GenerateAccessorFor(typeof(Target))]` | assembly, class | Generates accessors for an external type that cannot be annotated |

### GenerateAccessor

```csharp
[GenerateAccessor]
public partial class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
```

A `partial` type also implements `IAccessorProvider<T>` (and `IConstructorProvider<T>` if it has public constructors), enabling `GetXxx`.

### TypedAccessor

`FindXxx` instantiates open generic types with `MakeGenericType`, which is not AOT-safe. Pre-register the closed types used by the application; `GetXxx` needs no pre-registration.

```csharp
[GenerateAccessor]
[TypedAccessor(typeof(GenericData<int>))]
[TypedAccessor(typeof(GenericData<string>))]
public partial class GenericData<T>
{
    public T Value { get; set; } = default!;
}
```

Assembly level:

```csharp
[assembly: TypedAccessor(typeof(GenericData<DateTime>))]
```

### AccessorMember

Non-public members are excluded by default. Opted-in members use `UnsafeAccessor`, which requires .NET 9 or later for generic types.

```csharp
[GenerateAccessor]
public partial class HiddenData
{
    public int Id { get; set; }

    [AccessorMember]
    private int counter;

    [AccessorMember(Ignore = true)]
    public string Secret { get; set; } = default!;
}
```

### GenerateAccessorFor

For types that cannot be annotated, such as BCL types or types in other assemblies. The assembly-level form is resolved with `FindXxx`:

```csharp
[assembly: GenerateAccessorFor(typeof(Version))]
```

```csharp
var factory = AccessorProvider.FindFactory<Version>()!;
var getMajor = factory.CreateGetter<int>(nameof(Version.Major))!;
```

On a `partial` class, that class implements the provider interfaces for each target, enabling `GetXxx`:

```csharp
[GenerateAccessorFor(typeof(PlainData))]
[GenerateAccessorFor(typeof(PlainGenericData<int>))]
internal sealed partial class AccessorProviders;

var factory = AccessorProvider.GetFactory<PlainData, AccessorProviders>();
```

## Resolution

```csharp
// Compile time, via static abstract members
var factory = AccessorProvider.GetFactory<Data>();

// Runtime lookup by type
var factory1 = AccessorProvider.FindFactory<Data>();
var factory2 = AccessorProvider.FindFactory(typeof(Data));
```

| | `GetXxx` | `FindXxx` |
| --- | --- | --- |
| Resolution | Compile time | Per-type static cache, dictionary for the `Type` overload |
| Result | Non-null | Nullable |
| `System.Type` lookup | ❌ | ✅ |
| Generic types on Native AOT | Any closed instantiation | Closed types need `[TypedAccessor]` |
| Requirement | `partial` type with `[GenerateAccessor]`, or a provider class with `[GenerateAccessorFor]` | Type registered via attributes |

## Supported Members

- `readonly` fields and read-only / `init`-only properties are read-only (typed setter returns `null`)
- Inherited properties are flattened
- `record` / `record struct` are treated as class / struct
- `static` members and indexers are not supported

## Benchmark

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8973/25H2/2025Update/HudsonValley2)
AMD Ryzen AI 9 HX 370 w/ Radeon 890M 2.00GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.302
  [Host]    : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.10 (10.0.10, 10.0.1026.32716), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2
WarmupCount=10
```

| Suffix | Technique |
| --- | --- |
| `Direct` | Hand-written property access (baseline) |
| `Factory` | Generated typed delegate from `CreateGetter<T>` / `CreateSetter<T>` |
| `FactoryExtension` | The same delegate invoked through the `Read` / `Write` extensions |
| `AccessorCached` | Object-based `IAccessor.GetValue` / `SetValue` on a cached instance |
| `Accessor` | The same, resolving the accessor on every call |
| `Expression` | Compiled expression-tree delegate |
| `ReflectionCached` | Cached `PropertyInfo` |
| `Reflection` | `GetProperty` followed by `GetValue` / `SetValue` on every call |

### Resolution Cost

| Method | Mean | Error | StdDev | Ratio | Code Size | Allocated |
|--------------------------- |-----------:|----------:|----------:|------:|----------:|----------:|
| FactoryResolveCached | 0.3221 ns | 0.0016 ns | 0.0024 ns | 1.00 | 20 B | - |
| FactoryResolveProvider | 0.3213 ns | 0.0012 ns | 0.0018 ns | 1.00 | 32 B | - |
| FactoryResolveFind | 0.3213 ns | 0.0016 ns | 0.0023 ns | 1.00 | 945 B | - |
| FactoryResolveFindType | 5.2756 ns | 0.0496 ns | 0.0712 ns | 16.38 | 1,589 B | - |
| | | | | | | |
| ConstructorFindCached | 0.3211 ns | 0.0012 ns | 0.0018 ns | 1.00 | 20 B | - |
| ConstructorFindProvider | 0.3218 ns | 0.0013 ns | 0.0019 ns | 1.00 | 32 B | - |
| ConstructorFind | 0.3273 ns | 0.0043 ns | 0.0061 ns | 1.02 | 1,006 B | - |

### Member Access

| Method | Mean | Error | StdDev | Ratio | Code Size | Allocated |
|---------------------------- |-----------:|----------:|----------:|------:|----------:|----------:|
| ClassIntGetDirect | 0.2056 ns | 0.0017 ns | 0.0026 ns | 1.00 | 21 B | - |
| ClassIntGetFactory | 0.2082 ns | 0.0006 ns | 0.0009 ns | 1.01 | 143 B | - |
| ClassIntGetFactoryExtension | 0.2094 ns | 0.0008 ns | 0.0012 ns | 1.02 | 143 B | - |
| ClassIntGetExpression | 1.2028 ns | 0.0075 ns | 0.0108 ns | 5.85 | 45 B | - |
| ClassIntGetAccessorCached | 1.5300 ns | 0.0238 ns | 0.0356 ns | 7.44 | 165 B | 24 B |
| ClassIntGetAccessor | 1.7438 ns | 0.0379 ns | 0.0518 ns | 8.48 | 790 B | 24 B |
| ClassIntGetReflectionCached | 6.5102 ns | 0.1701 ns | 0.2494 ns | 31.67 | 3,260 B | 24 B |
| ClassIntGetReflection | 11.2155 ns | 0.2944 ns | 0.4407 ns | 54.57 | 7,699 B | 24 B |
| | | | | | | |
| ClassIntSetDirect | 0.2081 ns | 0.0016 ns | 0.0023 ns | 1.00 | 19 B | - |
| ClassIntSetFactoryExtension | 0.2085 ns | 0.0007 ns | 0.0010 ns | 1.00 | 153 B | - |
| ClassIntSetFactory | 0.2097 ns | 0.0013 ns | 0.0018 ns | 1.01 | 153 B | - |
| ClassIntSetExpression | 1.2061 ns | 0.0046 ns | 0.0065 ns | 5.80 | 48 B | - |
| ClassIntSetAccessor | 1.5408 ns | 0.0167 ns | 0.0250 ns | 7.40 | 808 B | 24 B |
| ClassIntSetAccessorCached | 1.5952 ns | 0.0380 ns | 0.0532 ns | 7.66 | 182 B | 24 B |
| ClassIntSetReflectionCached | 7.9565 ns | 0.0847 ns | 0.1268 ns | 38.23 | 8,557 B | 24 B |
| ClassIntSetReflection | 13.0950 ns | 0.1302 ns | 0.1867 ns | 62.92 | 8,377 B | 24 B |

### Type Scenarios

| Scenario | Operation | Direct | Factory | AccessorCached | ReflectionCached | Reflection |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| class / int | Get | 0.2056 ns | 0.2082 ns | 1.5300 ns | 6.5102 ns | 11.2155 ns |
| class / int | Set | 0.2081 ns | 0.2097 ns | 1.5952 ns | 7.9565 ns | 13.0950 ns |
| class / string | Get | 0.2037 ns | 0.2283 ns | 0.2096 ns | 4.5291 ns | 8.4370 ns |
| class / string | Set | 0.2066 ns | 0.3244 ns | 0.2072 ns | 10.5444 ns | 15.2878 ns |
| struct / int | Get | 0.3231 ns | 0.2097 ns | 2.7881 ns | 6.5762 ns | 11.7201 ns |
| struct / int | Set | 0.2102 ns | 0.2114 ns | 1.7996 ns | 8.2455 ns | 13.5084 ns |
| generic / int | Get | 0.3214 ns | 0.3217 ns | 2.3909 ns | 9.4616 ns | 16.7859 ns |
| generic / int | Set | 0.3190 ns | 0.3229 ns | 2.3493 ns | 12.7049 ns | 20.0338 ns |
| large class / int | Get | 0.3212 ns | 0.3236 ns | 2.3160 ns | 9.5359 ns | 17.2085 ns |
| large class / int | Set | 0.3219 ns | 0.3230 ns | 2.3935 ns | 13.1951 ns | 21.5638 ns |

### Ref-Free Extensions

| Method | Mean | Error | StdDev | Code Size | Allocated |
|----------------------------- |----------:|----------:|----------:|----------:|----------:|
| ClassIntPropertyGetTemp | 0.2395 ns | 0.0043 ns | 0.0062 ns | 158 B | - |
| ClassIntPropertyGetExtension | 0.2332 ns | 0.0025 ns | 0.0036 ns | 158 B | - |
| | | | | | |
| ClassIntGetFactory | 0.2082 ns | 0.0006 ns | 0.0009 ns | 143 B | - |
| ClassIntGetFactoryExtension | 0.2094 ns | 0.0008 ns | 0.0012 ns | 143 B | - |
| | | | | | |
| StructGetFactory | 0.2097 ns | 0.0032 ns | 0.0046 ns | 137 B | - |
| StructGetFactoryExtension | 0.2094 ns | 0.0020 ns | 0.0029 ns | 137 B | - |
| | | | | | |
| StructSetFactory | 0.2114 ns | 0.0020 ns | 0.0030 ns | 179 B | - |
| StructSetFactoryExtension | 0.2119 ns | 0.0019 ns | 0.0029 ns | 179 B | - |
