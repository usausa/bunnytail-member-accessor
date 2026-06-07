namespace Benchmark;

using System.Linq.Expressions;
using System.Reflection;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;

using BunnyTail.MemberAccessor;

public static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromTypes([typeof(Benchmark), typeof(TypeScenarioBenchmark)]).Run(args);
    }
}

public class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddDiagnoser(MemoryDiagnoser.Default, new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
        AddJob(Job.MediumRun);
    }
}

#pragma warning disable CA1822
[Config(typeof(BenchmarkConfig))]
public class Benchmark
{
    private const int N = 1000;

    private static readonly Data Data = new();

    private PropertyInfo property = default!;

    private IAccessor accessor = default!;

    private Func<Data, int> expressionGetter = default!;
    private Func<Data, int> generatorGetter = default!;
    private Action<Data, int> expressionSetter = default!;
    private Action<Data, int> generatorSetter = default!;

    [GlobalSetup]
    public void Setup()
    {
        property = typeof(Data).GetProperty(nameof(Data.Id))!;

        accessor = AccessorRegistry.FindAccessor<Data>()!;

        expressionGetter = ExpressionHelper.CreateGetter<Data, int>(nameof(Data.Id));
        expressionSetter = ExpressionHelper.CreateSetter<Data, int>(nameof(Data.Id));

        var accessorFactory = AccessorRegistry.FindFactory<Data>()!;
        generatorGetter = accessorFactory.CreateGetter<int>(nameof(Data.Id))!;
        generatorSetter = accessorFactory.CreateSetter<int>(nameof(Data.Id))!;
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void DirectGetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            _ = o.Id;
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void PropertyGetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Id))!;
            _ = pi.GetValue(o);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void PropertyGetterCashed()
    {
        var o = Data;
        var pi = property;
        for (var i = 0; i < N; i++)
        {
            _ = pi.GetValue(o);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void AccessorGetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            var access = AccessorRegistry.FindAccessor<Data>()!;
            _ = access.GetValue(o, nameof(Data.Id));
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void AccessorGetterCached()
    {
        var o = Data;
        var access = accessor;
        for (var i = 0; i < N; i++)
        {
            _ = access.GetValue(o, nameof(Data.Id));
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void ExpressionGetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            _ = expressionGetter(o);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void GeneratorGetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            _ = generatorGetter(o);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void DirectSetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            o.Id = 0;
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void PropertySetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Id))!;
            pi.SetValue(o, 0);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void PropertySetterCashed()
    {
        var o = Data;
        var pi = property;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(o, 0);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void AccessorSetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            var access = AccessorRegistry.FindAccessor<Data>()!;
            access.SetValue(o, nameof(Data.Id), 0);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void AccessorSetterCached()
    {
        var o = Data;
        var access = accessor;
        for (var i = 0; i < N; i++)
        {
            access.SetValue(o, nameof(Data.Id), 0);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void ExpressionSetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            expressionSetter(o, 0);
        }
    }

    [Benchmark(OperationsPerInvoke = N)]
    public void GeneratorSetter()
    {
        var o = Data;
        for (var i = 0; i < N; i++)
        {
            generatorSetter(o, 0);
        }
    }
}

[GenerateAccessor]
public class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
}

public static class ExpressionHelper
{
    public static Func<T, TProperty> CreateGetter<T, TProperty>(string name)
    {
        var type = typeof(T);
        var pi = type.GetProperty(name)!;

        var target = Expression.Parameter(type, "target");
        var property = Expression.Property(target, pi);
        var lambda = Expression.Lambda<Func<T, TProperty>>(property, target);
        return lambda.Compile();
    }

    public static Action<T, TProperty> CreateSetter<T, TProperty>(string name)
    {
        var type = typeof(T);
        var pi = type.GetProperty(name)!;

        var target = Expression.Parameter(type, "target");
        var property = Expression.Property(target, pi);
        var value = Expression.Parameter(typeof(TProperty), "value");
        var assign = Expression.Assign(property, value);
        var lambda = Expression.Lambda<Action<T, TProperty>>(assign, target, value);
        return lambda.Compile();
    }
}

public class ScenarioConfig : ManualConfig
{
    public ScenarioConfig()
    {
        AddExporter(MarkdownExporter.GitHub);
        AddColumn(
            StatisticColumn.Mean,
            StatisticColumn.Min,
            StatisticColumn.Max,
            StatisticColumn.P90,
            StatisticColumn.Error,
            StatisticColumn.StdDev);
        AddDiagnoser(MemoryDiagnoser.Default);
        AddJob(Job.MediumRun);
    }
}

// Compares the full access ladder across property and declaring-type kinds:
// direct access, reflection (uncached / cached), and the IAccessor API (uncached / cached),
// for int / string properties, a value type (struct), a large class, and a generic type.
[Config(typeof(ScenarioConfig))]
public class TypeScenarioBenchmark
{
    private const int N = 1000;

    private static readonly Data ClassData = new() { Id = 1, Name = "abc" };
    private static readonly object StructBoxed = new StructData { Id = 1, Name = "abc" };
    private static readonly LargeData Large = new() { Value10 = 1 };
    private static readonly GenericData<int> Generic = new() { Value = 1 };

    private PropertyInfo intProperty = default!;
    private PropertyInfo stringProperty = default!;
    private PropertyInfo structProperty = default!;
    private PropertyInfo largeProperty = default!;
    private PropertyInfo genericProperty = default!;

    private IAccessor dataAccessor = default!;
    private IAccessor structAccessor = default!;
    private IAccessor largeAccessor = default!;
    private IAccessor genericAccessor = default!;

    [GlobalSetup]
    public void Setup()
    {
        intProperty = typeof(Data).GetProperty(nameof(Data.Id))!;
        stringProperty = typeof(Data).GetProperty(nameof(Data.Name))!;
        structProperty = typeof(StructData).GetProperty(nameof(StructData.Id))!;
        largeProperty = typeof(LargeData).GetProperty(nameof(LargeData.Value10))!;
        genericProperty = typeof(GenericData<int>).GetProperty(nameof(GenericData<int>.Value))!;

        dataAccessor = AccessorRegistry.FindAccessor<Data>()!;
        structAccessor = AccessorRegistry.FindAccessor<StructData>()!;
        largeAccessor = AccessorRegistry.FindAccessor<LargeData>()!;
        genericAccessor = AccessorRegistry.FindAccessor<GenericData<int>>()!;
    }

    // Int getter
    [BenchmarkCategory("IntGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntGetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            _ = ClassData.Id;
        }
    }

    [BenchmarkCategory("IntGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntGetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Id))!;
            _ = pi.GetValue(ClassData);
        }
    }

    [BenchmarkCategory("IntGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntGetReflectionCached()
    {
        var pi = intProperty;
        for (var i = 0; i < N; i++)
        {
            _ = pi.GetValue(ClassData);
        }
    }

    [BenchmarkCategory("IntGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntGetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            _ = accessor.GetValue(ClassData, nameof(Data.Id));
        }
    }

    [BenchmarkCategory("IntGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntGetAccessorCached()
    {
        var accessor = dataAccessor;
        for (var i = 0; i < N; i++)
        {
            _ = accessor.GetValue(ClassData, nameof(Data.Id));
        }
    }

    // Int setter
    [BenchmarkCategory("IntSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntSetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            ClassData.Id = 0;
        }
    }

    [BenchmarkCategory("IntSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntSetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Id))!;
            pi.SetValue(ClassData, 0);
        }
    }

    [BenchmarkCategory("IntSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntSetReflectionCached()
    {
        var pi = intProperty;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(ClassData, 0);
        }
    }

    [BenchmarkCategory("IntSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntSetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            accessor.SetValue(ClassData, nameof(Data.Id), 0);
        }
    }

    [BenchmarkCategory("IntSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void IntSetAccessorCached()
    {
        var accessor = dataAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(ClassData, nameof(Data.Id), 0);
        }
    }

    // String getter
    [BenchmarkCategory("StringGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringGetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            _ = ClassData.Name;
        }
    }

    [BenchmarkCategory("StringGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringGetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Name))!;
            _ = pi.GetValue(ClassData);
        }
    }

    [BenchmarkCategory("StringGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringGetReflectionCached()
    {
        var pi = stringProperty;
        for (var i = 0; i < N; i++)
        {
            _ = pi.GetValue(ClassData);
        }
    }

    [BenchmarkCategory("StringGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringGetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            _ = accessor.GetValue(ClassData, nameof(Data.Name));
        }
    }

    [BenchmarkCategory("StringGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringGetAccessorCached()
    {
        var accessor = dataAccessor;
        for (var i = 0; i < N; i++)
        {
            _ = accessor.GetValue(ClassData, nameof(Data.Name));
        }
    }

    // String setter
    [BenchmarkCategory("StringSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringSetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            ClassData.Name = "x";
        }
    }

    [BenchmarkCategory("StringSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringSetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Name))!;
            pi.SetValue(ClassData, "x");
        }
    }

    [BenchmarkCategory("StringSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringSetReflectionCached()
    {
        var pi = stringProperty;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(ClassData, "x");
        }
    }

    [BenchmarkCategory("StringSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringSetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            accessor.SetValue(ClassData, nameof(Data.Name), "x");
        }
    }

    [BenchmarkCategory("StringSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StringSetAccessorCached()
    {
        var accessor = dataAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(ClassData, nameof(Data.Name), "x");
        }
    }

    // Struct getter (set requires a boxed instance; see IAccessor.SetValue)
    [BenchmarkCategory("StructGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructGetDirect()
    {
        var value = (StructData)StructBoxed;
        for (var i = 0; i < N; i++)
        {
            _ = value.Id;
        }
    }

    [BenchmarkCategory("StructGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructGetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(StructData).GetProperty(nameof(StructData.Id))!;
            _ = pi.GetValue(StructBoxed);
        }
    }

    [BenchmarkCategory("StructGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructGetReflectionCached()
    {
        var pi = structProperty;
        for (var i = 0; i < N; i++)
        {
            _ = pi.GetValue(StructBoxed);
        }
    }

    [BenchmarkCategory("StructGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructGetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<StructData>()!;
            _ = accessor.GetValue(StructBoxed, nameof(StructData.Id));
        }
    }

    [BenchmarkCategory("StructGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructGetAccessorCached()
    {
        var accessor = structAccessor;
        for (var i = 0; i < N; i++)
        {
            _ = accessor.GetValue(StructBoxed, nameof(StructData.Id));
        }
    }

    // Large class getter
    [BenchmarkCategory("LargeGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeGetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            _ = Large.Value10;
        }
    }

    [BenchmarkCategory("LargeGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeGetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(LargeData).GetProperty(nameof(LargeData.Value10))!;
            _ = pi.GetValue(Large);
        }
    }

    [BenchmarkCategory("LargeGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeGetReflectionCached()
    {
        var pi = largeProperty;
        for (var i = 0; i < N; i++)
        {
            _ = pi.GetValue(Large);
        }
    }

    [BenchmarkCategory("LargeGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeGetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<LargeData>()!;
            _ = accessor.GetValue(Large, nameof(LargeData.Value10));
        }
    }

    [BenchmarkCategory("LargeGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeGetAccessorCached()
    {
        var accessor = largeAccessor;
        for (var i = 0; i < N; i++)
        {
            _ = accessor.GetValue(Large, nameof(LargeData.Value10));
        }
    }

    // Large class setter
    [BenchmarkCategory("LargeSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            Large.Value10 = 0;
        }
    }

    [BenchmarkCategory("LargeSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(LargeData).GetProperty(nameof(LargeData.Value10))!;
            pi.SetValue(Large, 0);
        }
    }

    [BenchmarkCategory("LargeSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetReflectionCached()
    {
        var pi = largeProperty;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(Large, 0);
        }
    }

    [BenchmarkCategory("LargeSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<LargeData>()!;
            accessor.SetValue(Large, nameof(LargeData.Value10), 0);
        }
    }

    [BenchmarkCategory("LargeSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetAccessorCached()
    {
        var accessor = largeAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(Large, nameof(LargeData.Value10), 0);
        }
    }

    // Generic type getter
    [BenchmarkCategory("GenericGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericGetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            _ = Generic.Value;
        }
    }

    [BenchmarkCategory("GenericGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericGetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(GenericData<int>).GetProperty(nameof(GenericData<int>.Value))!;
            _ = pi.GetValue(Generic);
        }
    }

    [BenchmarkCategory("GenericGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericGetReflectionCached()
    {
        var pi = genericProperty;
        for (var i = 0; i < N; i++)
        {
            _ = pi.GetValue(Generic);
        }
    }

    [BenchmarkCategory("GenericGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericGetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<GenericData<int>>()!;
            _ = accessor.GetValue(Generic, nameof(GenericData<int>.Value));
        }
    }

    [BenchmarkCategory("GenericGet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericGetAccessorCached()
    {
        var accessor = genericAccessor;
        for (var i = 0; i < N; i++)
        {
            _ = accessor.GetValue(Generic, nameof(GenericData<int>.Value));
        }
    }

    // Generic type setter
    [BenchmarkCategory("GenericSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetDirect()
    {
        for (var i = 0; i < N; i++)
        {
            Generic.Value = 0;
        }
    }

    [BenchmarkCategory("GenericSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetReflection()
    {
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(GenericData<int>).GetProperty(nameof(GenericData<int>.Value))!;
            pi.SetValue(Generic, 0);
        }
    }

    [BenchmarkCategory("GenericSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetReflectionCached()
    {
        var pi = genericProperty;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(Generic, 0);
        }
    }

    [BenchmarkCategory("GenericSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetAccessor()
    {
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<GenericData<int>>()!;
            accessor.SetValue(Generic, nameof(GenericData<int>.Value), 0);
        }
    }

    [BenchmarkCategory("GenericSet")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetAccessorCached()
    {
        var accessor = genericAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(Generic, nameof(GenericData<int>.Value), 0);
        }
    }
}

[GenerateAccessor]
public record struct StructData
{
    public int Id { get; set; }

    public string Name { get; set; }
}

[GenerateAccessor]
[TypedAccessor(typeof(GenericData<int>))]
public class GenericData<T>
{
    public T Value { get; set; } = default!;
}

[GenerateAccessor]
public class LargeData
{
    public int Value0 { get; set; }

    public int Value1 { get; set; }

    public int Value2 { get; set; }

    public int Value3 { get; set; }

    public int Value4 { get; set; }

    public int Value5 { get; set; }

    public int Value6 { get; set; }

    public int Value7 { get; set; }

    public int Value8 { get; set; }

    public int Value9 { get; set; }

    public int Value10 { get; set; }

    public int Value11 { get; set; }

    public int Value12 { get; set; }

    public int Value13 { get; set; }

    public int Value14 { get; set; }

    public int Value15 { get; set; }

    public int Value16 { get; set; }

    public int Value17 { get; set; }

    public int Value18 { get; set; }

    public int Value19 { get; set; }
}
