# BunnyTail.MemberAccessor

[![NuGet](https://img.shields.io/nuget/v/BunnyTail.MemberAccessor.svg)](https://www.nuget.org/packages/BunnyTail.MemberAccessor)

AOT-safe source-generated member accessor for .NET. A reflection-free alternative for property get/set, constructor invocation, and member enumeration.

## Support Matrix

| Feature | Supported | Notes |
| --- | :---: | --- |
| `class` | ✅ | Full support |
| `struct` | ✅ | Boxed instance required for `IAccessor.SetValue`; typed `CreateSetter<T>` returns `null` (see Struct Support) |
| `record` (class) | ✅ | Treated as class |
| `record struct` | ✅ | Treated as struct |
| Open generic (`Foo<T>`) | ✅ | On-demand closed-type instantiation |
| Closed generic pre-registration | ✅ | `[TypedAccessor(typeof(Foo<int>))]` |
| Inherited properties | ✅ | Flattened from base classes |
| Public instance properties | ✅ | Read/write; `static`, non-public and indexers are ignored |
| Read-only properties | ✅ | Setter returns `null` |
| Constructor accessor | ✅ | Arity 0–4; AOT-safe; generic types supported |
| Same-arity constructor overloads | ✅ | Resolved by argument type at runtime (see Constructor Accessor) |
| `IAccessorFactory.Members` | ✅ | `IReadOnlyList<MemberDescriptor>` (public instance properties only) |
| `static` members | ❌ | Not yet supported |
| Non-public members | ❌ | Public only |
| Fields | ❌ | Properties only |
| `init`-only properties | ✅ | Readable; `init` setters are treated as read-only (`CanWrite` = `false`, typed setter returns `null`) |

## Reference

Add reference to BunnyTail.MemberAccessor to csproj.

```xml
  <ItemGroup>
    <PackageReference Include="BunnyTail.MemberAccessor" Version="1.2.0" />
  </ItemGroup>
```

## MemberAccessor

### Source

```csharp
using BunnyTail.MemberAccessor;

[GenerateAccessor]
public class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}
```

```csharp
using BunnyTail.MemberAccessor;

var accessorFactory = AccessorRegistry.FindFactory<Data>();
var getter = accessorFactory.CreateGetter<int>(nameof(Data.Id));
var setter = accessorFactory.CreateSetter<int>(nameof(Data.Id));

var data = new Data();
setter(data, 123);
var id = getter(data);
```

### Member Enumeration

```csharp
var factory = AccessorRegistry.FindFactory<Data>();
foreach (var member in factory.Members)
{
    Console.WriteLine($"{member.Name}: {member.Type} CanRead={member.CanRead} CanWrite={member.CanWrite}");
}
```

### Constructor Accessor

```csharp
var ctor = AccessorRegistry.FindConstructor<Data>();
var instance = ctor.Create();          // parameterless
var instance2 = ctor.Create<int>(42); // 1-arg constructor
```

Constructor accessors are available for generic types as well (closed types are pre-registered
with `[TypedAccessor]`, others are created on demand):

```csharp
var ctor = AccessorRegistry.FindConstructor<GenericHolder<int>>();
var instance = ctor.Create<int>(42);
```

When a type declares multiple constructors with the **same arity**, the matching constructor is
selected at runtime by the argument type. Pass the exact parameter type as the type argument:

```csharp
// class Sample { Sample(int v); Sample(string v); }
var ctor = AccessorRegistry.FindConstructor<Sample>();
var a = ctor.Create(42);      // -> Sample(int)
var b = ctor.Create("text");  // -> Sample(string)
```

If no constructor matches the supplied argument type, `NotSupportedException` is thrown.

### Struct Support

```csharp
[GenerateAccessor]
public struct Point { public int X { get; set; } public int Y { get; set; } }

var accessor = AccessorRegistry.FindAccessor<Point>();
object boxed = new Point { X = 1, Y = 2 };
accessor.SetValue(boxed, "X", 10); // modifies boxed instance
```

> **Note:** For value types, the typed `IAccessorFactory<T>.CreateSetter<TProperty>` returns `null`,
> because a `delegate void(T, TProperty)` would receive a copy of the struct and could not mutate the
> caller's value. Use `IAccessor.SetValue` with a boxed instance to modify a struct.

## Benchmark

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.7171/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.100
  [Host]    : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  
```
| Method               | Mean       | Error     | StdDev    | Min        | Max        | P90        | Code Size | Gen0   | Allocated |
|--------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|----------:|-------:|----------:|
| DirectGetter         |  0.2243 ns | 0.0064 ns | 0.0095 ns |  0.2138 ns |  0.2538 ns |  0.2375 ns |      10 B |      - |         - |
| PropertyGetter       | 20.6895 ns | 0.5456 ns | 0.8166 ns | 19.6389 ns | 22.6418 ns | 21.8329 ns |   3,019 B | 0.0014 |      24 B |
| PropertyGetterCashed |  8.9811 ns | 0.2230 ns | 0.3338 ns |  8.5007 ns |  9.7118 ns |  9.3515 ns |   3,278 B | 0.0014 |      24 B |
| AccessorGetter       | 10.6687 ns | 0.2781 ns | 0.4163 ns |  9.9247 ns | 11.7124 ns | 11.1563 ns |   3,219 B | 0.0014 |      24 B |
| AccessorGetterCached |  2.3157 ns | 0.0976 ns | 0.1461 ns |  2.0956 ns |  2.5933 ns |  2.4920 ns |     174 B | 0.0014 |      24 B |
| ExpressionGetter     |  1.3618 ns | 0.0267 ns | 0.0392 ns |  1.2959 ns |  1.4362 ns |  1.4167 ns |      54 B |      - |         - |
| GeneratorGetter      |  0.2304 ns | 0.0055 ns | 0.0082 ns |  0.2172 ns |  0.2518 ns |  0.2416 ns |      76 B |      - |         - |
| DirectSetter         |  0.2291 ns | 0.0066 ns | 0.0099 ns |  0.2145 ns |  0.2458 ns |  0.2427 ns |      28 B |      - |         - |
| PropertySetter       | 19.3523 ns | 0.6403 ns | 0.9584 ns | 17.8336 ns | 21.3628 ns | 20.3991 ns |   8,536 B | 0.0014 |      24 B |
| PropertySetterCashed | 11.1574 ns | 0.2706 ns | 0.4051 ns | 10.5017 ns | 11.5931 ns | 11.5931 ns |   8,736 B | 0.0014 |      24 B |
| AccessorSetter       | 10.5961 ns | 0.2128 ns | 0.3120 ns | 10.1118 ns | 11.3181 ns | 11.0217 ns |   3,238 B | 0.0014 |      24 B |
| AccessorSetterCached |  2.2665 ns | 0.1085 ns | 0.1623 ns |  1.9878 ns |  2.5154 ns |  2.4811 ns |     191 B | 0.0014 |      24 B |
| ExpressionSetter     |  1.4610 ns | 0.0427 ns | 0.0599 ns |  1.3909 ns |  1.6234 ns |  1.5634 ns |      57 B |      - |         - |
| GeneratorSetter      |  0.5057 ns | 0.0181 ns | 0.0259 ns |  0.4630 ns |  0.5806 ns |  0.5321 ns |      85 B |      - |         - |

### Type Scenarios

Cached reflection (`PropertyInfo`) compared with the generated typed accessor (`CreateGetter<T>` /
`CreateSetter<T>`) across property types (`int` / `string`), a value type (`struct`), a large class,
and a generic type. The generated accessor is allocation-free and stays roughly constant regardless
of the member type or the declaring-type kind.

```
BenchmarkDotNet v0.15.8, Windows 11 (10.0.26200.8524/25H2/2025Update/HudsonValley2)
AMD Ryzen 9 5900X 3.70GHz, 1 CPU, 24 logical and 12 physical cores
.NET SDK 10.0.300
  [Host]    : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.8 (10.0.8, 10.0.826.23019), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  
```
| Method               | Mean       | Error     | StdDev    | Min        | Max        | P90        | Gen0   | Allocated |
|--------------------- |-----------:|----------:|----------:|-----------:|-----------:|-----------:|-------:|----------:|
| IntGetReflection     | 11.2540 ns | 0.5883 ns | 0.8805 ns | 10.1597 ns | 13.0197 ns | 12.3929 ns | 0.0014 |      24 B |
| IntGetGenerator      |  0.2751 ns | 0.0052 ns | 0.0074 ns |  0.2604 ns |  0.2916 ns |  0.2840 ns |      - |         - |
| IntSetReflection     | 13.5897 ns | 0.6378 ns | 0.9147 ns | 12.6522 ns | 16.0574 ns | 15.0835 ns | 0.0014 |      24 B |
| IntSetGenerator      |  0.2780 ns | 0.0108 ns | 0.0158 ns |  0.2614 ns |  0.3194 ns |  0.3024 ns |      - |         - |
| StringGetReflection  |  7.5344 ns | 0.1788 ns | 0.2620 ns |  7.0083 ns |  8.1777 ns |  7.9011 ns |      - |         - |
| StringGetGenerator   |  0.3142 ns | 0.0236 ns | 0.0354 ns |  0.2700 ns |  0.3841 ns |  0.3574 ns |      - |         - |
| StringSetReflection  | 12.0234 ns | 0.3071 ns | 0.4597 ns | 11.3507 ns | 13.1734 ns | 12.6080 ns |      - |         - |
| StringSetGenerator   |  0.2908 ns | 0.0139 ns | 0.0203 ns |  0.2281 ns |  0.3274 ns |  0.3096 ns |      - |         - |
| StructGetReflection  | 10.7348 ns | 0.3399 ns | 0.4765 ns |  9.9749 ns | 11.7202 ns | 11.3857 ns | 0.0014 |      24 B |
| StructGetGenerator   |  0.3123 ns | 0.0291 ns | 0.0435 ns |  0.2205 ns |  0.3754 ns |  0.3677 ns |      - |         - |
| LargeGetReflection   | 10.3250 ns | 0.3926 ns | 0.5631 ns |  9.7004 ns | 11.8913 ns | 11.1991 ns | 0.0014 |      24 B |
| LargeGetGenerator    |  0.2837 ns | 0.0225 ns | 0.0315 ns |  0.2514 ns |  0.3526 ns |  0.3394 ns |      - |         - |
| LargeSetReflection   | 13.8748 ns | 0.8492 ns | 1.2448 ns | 12.8041 ns | 17.1278 ns | 16.1829 ns | 0.0014 |      24 B |
| LargeSetGenerator    |  0.2960 ns | 0.0183 ns | 0.0274 ns |  0.2682 ns |  0.3671 ns |  0.3377 ns |      - |         - |
| GenericGetReflection | 11.0396 ns | 0.6136 ns | 0.8995 ns | 10.0449 ns | 13.5371 ns | 12.5900 ns | 0.0014 |      24 B |
| GenericGetGenerator  |  0.2951 ns | 0.0177 ns | 0.0265 ns |  0.2712 ns |  0.3638 ns |  0.3351 ns |      - |         - |
| GenericSetReflection | 13.9884 ns | 0.4783 ns | 0.7010 ns | 12.9126 ns | 15.8474 ns | 14.8361 ns | 0.0014 |      24 B |
| GenericSetGenerator  |  0.2836 ns | 0.0035 ns | 0.0049 ns |  0.2683 ns |  0.2925 ns |  0.2884 ns |      - |         - |
