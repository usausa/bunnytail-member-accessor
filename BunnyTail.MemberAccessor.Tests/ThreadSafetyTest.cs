namespace BunnyTail.MemberAccessor;

using System.Collections.Concurrent;
using System.Threading.Tasks;

public class ThreadSafetyTest
{
    private const int Parallelism = 64;

    [Fact]
    public void TestConcurrentAccessorResolution()
    {
        // GenericData<long> is not pre-registered, so the first concurrent batch races on the on-demand resolution path.
        var results = new ConcurrentBag<IAccessor?>();

        Parallel.For(0, Parallelism, _ => results.Add(AccessorRegistry.FindAccessor<GenericData<long>>()));

        var expected = AccessorRegistry.FindAccessor<GenericData<long>>();

        Assert.NotNull(expected);
        Assert.Equal(Parallelism, results.Count);
        Assert.All(results, x => Assert.Same(expected, x));
    }

    [Fact]
    public void TestConcurrentFactoryResolution()
    {
        var results = new ConcurrentBag<IAccessorFactory<GenericData<long>>?>();

        Parallel.For(0, Parallelism, _ => results.Add(AccessorRegistry.FindFactory<GenericData<long>>()));

        var expected = AccessorRegistry.FindFactory<GenericData<long>>();

        Assert.NotNull(expected);
        Assert.Equal(Parallelism, results.Count);
        Assert.All(results, x => Assert.Same(expected, x));
    }

    [Fact]
    public void TestConcurrentConstructorResolution()
    {
        var results = new ConcurrentBag<IConstructorAccessor<GenericHolder<long>>?>();

        Parallel.For(0, Parallelism, _ => results.Add(AccessorRegistry.FindConstructor<GenericHolder<long>>()));

        var expected = AccessorRegistry.FindConstructor<GenericHolder<long>>();

        Assert.NotNull(expected);
        Assert.Equal(Parallelism, results.Count);
        Assert.All(results, x => Assert.Same(expected, x));
    }
}
