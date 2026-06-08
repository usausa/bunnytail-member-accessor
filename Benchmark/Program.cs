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
        BenchmarkRunner.Run<AccessorBenchmark>(args: args);
    }
}

public sealed class BenchmarkConfig : ManualConfig
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
        AddDiagnoser(
            MemoryDiagnoser.Default,
            new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig(maxDepth: 3, printSource: true, printInstructionAddresses: true, exportDiff: true)));
        AddJob(Job.MediumRun);
    }
}

// See BENCHMARK.md for the scenario / operation / processing-kind matrix.
// Methods are listed fastest-predicted first:
// Direct, AccessorCached, Accessor, Expression, Factory, ReflectionCached, Reflection.
[Config(typeof(BenchmarkConfig))]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class AccessorBenchmark
{
    private const int N = 1000;

    // Targets
    private Data classData = default!;
    private StructData structData;
    private object structBoxed = default!;
    private GenericData<int> generic = default!;
    private LargeData large = default!;

    // Cached PropertyInfo
    private PropertyInfo classIntPi = default!;
    private PropertyInfo classStringPi = default!;
    private PropertyInfo structPi = default!;
    private PropertyInfo genericPi = default!;
    private PropertyInfo largePi = default!;

    // Cached IAccessor (object, name-based)
    private IAccessor dataAccessor = default!;
    private IAccessor structAccessor = default!;
    private IAccessor genericAccessor = default!;
    private IAccessor largeAccessor = default!;

    // Generated typed delegates (Factory)
    private Func<Data, int> classIntFactoryGet = default!;
    private Action<Data, int> classIntFactorySet = default!;
    private Func<Data, string> classStringFactoryGet = default!;
    private Action<Data, string> classStringFactorySet = default!;
    private Func<StructData, int> structFactoryGet = default!;
    private Func<GenericData<int>, int> genericFactoryGet = default!;
    private Action<GenericData<int>, int> genericFactorySet = default!;
    private Func<LargeData, int> largeFactoryGet = default!;
    private Action<LargeData, int> largeFactorySet = default!;

    // Compiled expression delegates
    private Func<Data, int> classIntExprGet = default!;
    private Action<Data, int> classIntExprSet = default!;
    private Func<Data, string> classStringExprGet = default!;
    private Action<Data, string> classStringExprSet = default!;
    private Func<StructData, int> structExprGet = default!;
    private Func<GenericData<int>, int> genericExprGet = default!;
    private Action<GenericData<int>, int> genericExprSet = default!;
    private Func<LargeData, int> largeExprGet = default!;
    private Action<LargeData, int> largeExprSet = default!;

    [GlobalSetup]
    public void Setup()
    {
        classData = new Data { Id = 1, Name = "abc" };
        structData = new StructData { Id = 1, Name = "abc" };
        structBoxed = structData;
        generic = new GenericData<int> { Value = 1 };
        large = new LargeData { Value10 = 1 };

        classIntPi = typeof(Data).GetProperty(nameof(Data.Id))!;
        classStringPi = typeof(Data).GetProperty(nameof(Data.Name))!;
        structPi = typeof(StructData).GetProperty(nameof(StructData.Id))!;
        genericPi = typeof(GenericData<int>).GetProperty(nameof(GenericData<int>.Value))!;
        largePi = typeof(LargeData).GetProperty(nameof(LargeData.Value10))!;

        dataAccessor = AccessorRegistry.FindAccessor<Data>()!;
        structAccessor = AccessorRegistry.FindAccessor<StructData>()!;
        genericAccessor = AccessorRegistry.FindAccessor<GenericData<int>>()!;
        largeAccessor = AccessorRegistry.FindAccessor<LargeData>()!;

        var dataFactory = AccessorRegistry.FindFactory<Data>()!;
        classIntFactoryGet = dataFactory.CreateGetter<int>(nameof(Data.Id))!;
        classIntFactorySet = dataFactory.CreateSetter<int>(nameof(Data.Id))!;
        classStringFactoryGet = dataFactory.CreateGetter<string>(nameof(Data.Name))!;
        classStringFactorySet = dataFactory.CreateSetter<string>(nameof(Data.Name))!;
        structFactoryGet = AccessorRegistry.FindFactory<StructData>()!.CreateGetter<int>(nameof(StructData.Id))!;
        var genericFactory = AccessorRegistry.FindFactory<GenericData<int>>()!;
        genericFactoryGet = genericFactory.CreateGetter<int>(nameof(GenericData<int>.Value))!;
        genericFactorySet = genericFactory.CreateSetter<int>(nameof(GenericData<int>.Value))!;
        var largeFactory = AccessorRegistry.FindFactory<LargeData>()!;
        largeFactoryGet = largeFactory.CreateGetter<int>(nameof(LargeData.Value10))!;
        largeFactorySet = largeFactory.CreateSetter<int>(nameof(LargeData.Value10))!;

        classIntExprGet = ExpressionHelper.CreateGetter<Data, int>(nameof(Data.Id));
        classIntExprSet = ExpressionHelper.CreateSetter<Data, int>(nameof(Data.Id));
        classStringExprGet = ExpressionHelper.CreateGetter<Data, string>(nameof(Data.Name));
        classStringExprSet = ExpressionHelper.CreateSetter<Data, string>(nameof(Data.Name));
        structExprGet = ExpressionHelper.CreateGetter<StructData, int>(nameof(StructData.Id));
        genericExprGet = ExpressionHelper.CreateGetter<GenericData<int>, int>(nameof(GenericData<int>.Value));
        genericExprSet = ExpressionHelper.CreateSetter<GenericData<int>, int>(nameof(GenericData<int>.Value));
        largeExprGet = ExpressionHelper.CreateGetter<LargeData, int>(nameof(LargeData.Value10));
        largeExprSet = ExpressionHelper.CreateSetter<LargeData, int>(nameof(LargeData.Value10));
    }

    // ------------------------------------------------------------
    // ClassInt (class / int)
    // ------------------------------------------------------------

    [BenchmarkCategory("ClassInt-Get")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public int ClassIntGetDirect()
    {
        var o = classData;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = o.Id;
        }
        return v;
    }

    [BenchmarkCategory("ClassInt-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassIntGetAccessorCached()
    {
        var o = classData;
        var accessor = dataAccessor;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = accessor.GetValue(o, nameof(Data.Id));
        }
        return v;
    }

    [BenchmarkCategory("ClassInt-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassIntGetAccessor()
    {
        var o = classData;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            v = accessor.GetValue(o, nameof(Data.Id));
        }
        return v;
    }

    [BenchmarkCategory("ClassInt-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int ClassIntGetExpression()
    {
        var o = classData;
        var get = classIntExprGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassInt-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int ClassIntGetFactory()
    {
        var o = classData;
        var get = classIntFactoryGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassInt-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassIntGetReflectionCached()
    {
        var o = classData;
        var pi = classIntPi;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassInt-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassIntGetReflection()
    {
        var o = classData;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Id))!;
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassInt-Set")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public void ClassIntSetDirect()
    {
        var o = classData;
        for (var i = 0; i < N; i++)
        {
            o.Id = 0;
        }
    }

    [BenchmarkCategory("ClassInt-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassIntSetAccessorCached()
    {
        var o = classData;
        var accessor = dataAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(o, nameof(Data.Id), 0);
        }
    }

    [BenchmarkCategory("ClassInt-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassIntSetAccessor()
    {
        var o = classData;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            accessor.SetValue(o, nameof(Data.Id), 0);
        }
    }

    [BenchmarkCategory("ClassInt-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassIntSetExpression()
    {
        var o = classData;
        var set = classIntExprSet;
        for (var i = 0; i < N; i++)
        {
            set(o, 0);
        }
    }

    [BenchmarkCategory("ClassInt-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassIntSetFactory()
    {
        var o = classData;
        var set = classIntFactorySet;
        for (var i = 0; i < N; i++)
        {
            set(o, 0);
        }
    }

    [BenchmarkCategory("ClassInt-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassIntSetReflectionCached()
    {
        var o = classData;
        var pi = classIntPi;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(o, 0);
        }
    }

    [BenchmarkCategory("ClassInt-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassIntSetReflection()
    {
        var o = classData;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Id))!;
            pi.SetValue(o, 0);
        }
    }

    // ------------------------------------------------------------
    // ClassString (class / string)
    // ------------------------------------------------------------

    [BenchmarkCategory("ClassString-Get")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public string? ClassStringGetDirect()
    {
        var o = classData;
        string? v = null;
        for (var i = 0; i < N; i++)
        {
            v = o.Name;
        }
        return v;
    }

    [BenchmarkCategory("ClassString-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassStringGetAccessorCached()
    {
        var o = classData;
        var accessor = dataAccessor;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = accessor.GetValue(o, nameof(Data.Name));
        }
        return v;
    }

    [BenchmarkCategory("ClassString-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassStringGetAccessor()
    {
        var o = classData;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            v = accessor.GetValue(o, nameof(Data.Name));
        }
        return v;
    }

    [BenchmarkCategory("ClassString-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public string? ClassStringGetExpression()
    {
        var o = classData;
        var get = classStringExprGet;
        string? v = null;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassString-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public string? ClassStringGetFactory()
    {
        var o = classData;
        var get = classStringFactoryGet;
        string? v = null;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassString-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassStringGetReflectionCached()
    {
        var o = classData;
        var pi = classStringPi;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassString-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? ClassStringGetReflection()
    {
        var o = classData;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Name))!;
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("ClassString-Set")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public void ClassStringSetDirect()
    {
        var o = classData;
        for (var i = 0; i < N; i++)
        {
            o.Name = "x";
        }
    }

    [BenchmarkCategory("ClassString-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassStringSetAccessorCached()
    {
        var o = classData;
        var accessor = dataAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(o, nameof(Data.Name), "x");
        }
    }

    [BenchmarkCategory("ClassString-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassStringSetAccessor()
    {
        var o = classData;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<Data>()!;
            accessor.SetValue(o, nameof(Data.Name), "x");
        }
    }

    [BenchmarkCategory("ClassString-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassStringSetExpression()
    {
        var o = classData;
        var set = classStringExprSet;
        for (var i = 0; i < N; i++)
        {
            set(o, "x");
        }
    }

    [BenchmarkCategory("ClassString-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassStringSetFactory()
    {
        var o = classData;
        var set = classStringFactorySet;
        for (var i = 0; i < N; i++)
        {
            set(o, "x");
        }
    }

    [BenchmarkCategory("ClassString-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassStringSetReflectionCached()
    {
        var o = classData;
        var pi = classStringPi;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(o, "x");
        }
    }

    [BenchmarkCategory("ClassString-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void ClassStringSetReflection()
    {
        var o = classData;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(Data).GetProperty(nameof(Data.Name))!;
            pi.SetValue(o, "x");
        }
    }

    // ------------------------------------------------------------
    // Struct (struct / int) - value type, accessed via boxed object
    // ------------------------------------------------------------

    [BenchmarkCategory("Struct-Get")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public int StructGetDirect()
    {
        var s = structData;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = s.Id;
        }
        return v;
    }

    [BenchmarkCategory("Struct-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? StructGetAccessorCached()
    {
        var o = structBoxed;
        var accessor = structAccessor;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = accessor.GetValue(o, nameof(StructData.Id));
        }
        return v;
    }

    [BenchmarkCategory("Struct-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? StructGetAccessor()
    {
        var o = structBoxed;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<StructData>()!;
            v = accessor.GetValue(o, nameof(StructData.Id));
        }
        return v;
    }

    [BenchmarkCategory("Struct-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int StructGetExpression()
    {
        var s = structData;
        var get = structExprGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(s);
        }
        return v;
    }

    [BenchmarkCategory("Struct-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int StructGetFactory()
    {
        var s = structData;
        var get = structFactoryGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(s);
        }
        return v;
    }

    [BenchmarkCategory("Struct-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? StructGetReflectionCached()
    {
        var o = structBoxed;
        var pi = structPi;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("Struct-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? StructGetReflection()
    {
        var o = structBoxed;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(StructData).GetProperty(nameof(StructData.Id))!;
            v = pi.GetValue(o);
        }
        return v;
    }

    // Struct-Set: typed delegate (Factory/Expression) is not supported for value types.
    [BenchmarkCategory("Struct-Set")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public void StructSetDirect()
    {
        var s = structData;
        for (var i = 0; i < N; i++)
        {
            s.Id = 0;
        }
        structData = s;
    }

    [BenchmarkCategory("Struct-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructSetAccessorCached()
    {
        var o = structBoxed;
        var accessor = structAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(o, nameof(StructData.Id), 0);
        }
    }

    [BenchmarkCategory("Struct-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructSetAccessor()
    {
        var o = structBoxed;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<StructData>()!;
            accessor.SetValue(o, nameof(StructData.Id), 0);
        }
    }

    [BenchmarkCategory("Struct-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructSetReflectionCached()
    {
        var o = structBoxed;
        var pi = structPi;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(o, 0);
        }
    }

    [BenchmarkCategory("Struct-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void StructSetReflection()
    {
        var o = structBoxed;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(StructData).GetProperty(nameof(StructData.Id))!;
            pi.SetValue(o, 0);
        }
    }

    // ------------------------------------------------------------
    // Generic (GenericData<int> / int)
    // ------------------------------------------------------------

    [BenchmarkCategory("Generic-Get")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public int GenericGetDirect()
    {
        var o = generic;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = o.Value;
        }
        return v;
    }

    [BenchmarkCategory("Generic-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? GenericGetAccessorCached()
    {
        var o = generic;
        var accessor = genericAccessor;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = accessor.GetValue(o, nameof(GenericData<int>.Value));
        }
        return v;
    }

    [BenchmarkCategory("Generic-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? GenericGetAccessor()
    {
        var o = generic;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<GenericData<int>>()!;
            v = accessor.GetValue(o, nameof(GenericData<int>.Value));
        }
        return v;
    }

    [BenchmarkCategory("Generic-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int GenericGetExpression()
    {
        var o = generic;
        var get = genericExprGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("Generic-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int GenericGetFactory()
    {
        var o = generic;
        var get = genericFactoryGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("Generic-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? GenericGetReflectionCached()
    {
        var o = generic;
        var pi = genericPi;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("Generic-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? GenericGetReflection()
    {
        var o = generic;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(GenericData<int>).GetProperty(nameof(GenericData<int>.Value))!;
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("Generic-Set")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public void GenericSetDirect()
    {
        var o = generic;
        for (var i = 0; i < N; i++)
        {
            o.Value = 0;
        }
    }

    [BenchmarkCategory("Generic-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetAccessorCached()
    {
        var o = generic;
        var accessor = genericAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(o, nameof(GenericData<int>.Value), 0);
        }
    }

    [BenchmarkCategory("Generic-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetAccessor()
    {
        var o = generic;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<GenericData<int>>()!;
            accessor.SetValue(o, nameof(GenericData<int>.Value), 0);
        }
    }

    [BenchmarkCategory("Generic-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetExpression()
    {
        var o = generic;
        var set = genericExprSet;
        for (var i = 0; i < N; i++)
        {
            set(o, 0);
        }
    }

    [BenchmarkCategory("Generic-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetFactory()
    {
        var o = generic;
        var set = genericFactorySet;
        for (var i = 0; i < N; i++)
        {
            set(o, 0);
        }
    }

    [BenchmarkCategory("Generic-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetReflectionCached()
    {
        var o = generic;
        var pi = genericPi;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(o, 0);
        }
    }

    [BenchmarkCategory("Generic-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void GenericSetReflection()
    {
        var o = generic;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(GenericData<int>).GetProperty(nameof(GenericData<int>.Value))!;
            pi.SetValue(o, 0);
        }
    }

    // ------------------------------------------------------------
    // Large (20-property class / int)
    // ------------------------------------------------------------

    [BenchmarkCategory("Large-Get")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public int LargeGetDirect()
    {
        var o = large;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = o.Value10;
        }
        return v;
    }

    [BenchmarkCategory("Large-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? LargeGetAccessorCached()
    {
        var o = large;
        var accessor = largeAccessor;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = accessor.GetValue(o, nameof(LargeData.Value10));
        }
        return v;
    }

    [BenchmarkCategory("Large-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? LargeGetAccessor()
    {
        var o = large;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<LargeData>()!;
            v = accessor.GetValue(o, nameof(LargeData.Value10));
        }
        return v;
    }

    [BenchmarkCategory("Large-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int LargeGetExpression()
    {
        var o = large;
        var get = largeExprGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("Large-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public int LargeGetFactory()
    {
        var o = large;
        var get = largeFactoryGet;
        var v = 0;
        for (var i = 0; i < N; i++)
        {
            v = get(o);
        }
        return v;
    }

    [BenchmarkCategory("Large-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? LargeGetReflectionCached()
    {
        var o = large;
        var pi = largePi;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("Large-Get")]
    [Benchmark(OperationsPerInvoke = N)]
    public object? LargeGetReflection()
    {
        var o = large;
        object? v = null;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(LargeData).GetProperty(nameof(LargeData.Value10))!;
            v = pi.GetValue(o);
        }
        return v;
    }

    [BenchmarkCategory("Large-Set")]
    [Benchmark(OperationsPerInvoke = N, Baseline = true)]
    public void LargeSetDirect()
    {
        var o = large;
        for (var i = 0; i < N; i++)
        {
            o.Value10 = 0;
        }
    }

    [BenchmarkCategory("Large-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetAccessorCached()
    {
        var o = large;
        var accessor = largeAccessor;
        for (var i = 0; i < N; i++)
        {
            accessor.SetValue(o, nameof(LargeData.Value10), 0);
        }
    }

    [BenchmarkCategory("Large-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetAccessor()
    {
        var o = large;
        for (var i = 0; i < N; i++)
        {
            var accessor = AccessorRegistry.FindAccessor<LargeData>()!;
            accessor.SetValue(o, nameof(LargeData.Value10), 0);
        }
    }

    [BenchmarkCategory("Large-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetExpression()
    {
        var o = large;
        var set = largeExprSet;
        for (var i = 0; i < N; i++)
        {
            set(o, 0);
        }
    }

    [BenchmarkCategory("Large-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetFactory()
    {
        var o = large;
        var set = largeFactorySet;
        for (var i = 0; i < N; i++)
        {
            set(o, 0);
        }
    }

    [BenchmarkCategory("Large-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetReflectionCached()
    {
        var o = large;
        var pi = largePi;
        for (var i = 0; i < N; i++)
        {
            pi.SetValue(o, 0);
        }
    }

    [BenchmarkCategory("Large-Set")]
    [Benchmark(OperationsPerInvoke = N)]
    public void LargeSetReflection()
    {
        var o = large;
        for (var i = 0; i < N; i++)
        {
            var pi = typeof(LargeData).GetProperty(nameof(LargeData.Value10))!;
            pi.SetValue(o, 0);
        }
    }
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

[GenerateAccessor]
public class Data
{
    public int Id { get; set; }

    public string Name { get; set; } = default!;
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
