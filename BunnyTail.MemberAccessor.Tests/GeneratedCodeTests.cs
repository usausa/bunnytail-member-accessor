namespace BunnyTail.MemberAccessor;

public sealed class GeneratedCodeTests
{
    // ------------------------------------------------------------
    // Generic type
    // ------------------------------------------------------------

    [Fact]
    public void ConstraintsOfTypeParametersAreRepeated()
    {
        // Arrange
        const string source =
            """
            using System;

            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Repo<T>
                where T : class
            {
                public T? Item { get; set; }
            }

            [GenerateAccessor]
            public partial class Entity<TKey>
                where TKey : struct, IEquatable<TKey>
            {
                public TKey Id { get; set; }
            }

            [GenerateAccessor]
            public partial class Factory<T>
                where T : new()
            {
                public T Value { get; set; } = new();
            }

            [GenerateAccessor]
            public partial class Keyed<T>
                where T : notnull
            {
                public T Value { get; set; } = default!;
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }

    [Fact]
    public void TypeParameterWithNameOfGeneratedOneWorks()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Pair<TProperty, TArg1>
            {
                public TProperty First { get; set; } = default!;

                public TArg1 Second { get; set; } = default!;

                public Pair(TProperty first, TArg1 second)
                {
                    First = first;
                    Second = second;
                }
            }

            public static class Program
            {
                public static string Run()
                {
                    var pair = AccessorProvider.GetConstructor<Pair<int, string>>().Create(1, "a");
                    var getter = AccessorProvider.GetFactory<Pair<int, string>>().CreateGetter<string>("Second")!;
                    return $"{pair.First}:{getter(ref pair)}";
                }
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("1:a", result);
    }

    [Fact]
    public void SameTypeWithDifferentTupleNamesIsImplementedOnce()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public class Holder<T>
            {
                public T Value { get; set; } = default!;
            }

            [GenerateAccessorFor(typeof(Holder<(int A, int B)>))]
            [GenerateAccessorFor(typeof(Holder<(int, int)>))]
            internal sealed partial class Providers;
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }

    // ------------------------------------------------------------
    // Member
    // ------------------------------------------------------------

    [Fact]
    public void MemberHiddenByDerivedMemberIsExcluded()
    {
        // Arrange
        const string source =
            """
            using System.Linq;

            using BunnyTail.MemberAccessor;

            namespace Test;

            public class BaseData
            {
                public string Name { get; set; } = string.Empty;

                public int Value { get; set; }

                public int Count { get; set; }

                public int Id { get; set; }
            }

            [GenerateAccessor]
            public partial class Data : BaseData
            {
                internal new int Name { get; set; }

                public static new int Value { get; set; }

                public new int Count() => 0;
            }

            public static class Program
            {
                public static string Run() =>
                    string.Join(",", AccessorProvider.GetFactory<Data>().Members.Select(static x => x.Name));
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("Id", result);
    }

    [Fact]
    public void MemberAndConstructorThatCannotTakeObjectAreExcluded()
    {
        // Arrange
        const string source =
            """
            using System;
            using System.Linq;

            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Buffer
            {
                private readonly int[] values = [1, 2];

                public Span<int> Values => values;

                public int Length { get; set; }

                public Buffer()
                {
                }

                public Buffer(ReadOnlySpan<char> text)
                {
                    Length = text.Length;
                }

                public Buffer(ref int length)
                {
                    Length = length;
                }
            }

            public static class Program
            {
                public static string Run() =>
                    string.Join(",", AccessorProvider.GetFactory<Buffer>().Members.Select(static x => x.Name)) + ":" +
                    AccessorProvider.GetConstructor<Buffer>().Create().Length;
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("Length:0", result);
    }

    [Fact]
    public void ConstructorLeavingRequiredMembersIsExcluded()
    {
        // Arrange
        const string source =
            """
            using System.Diagnostics.CodeAnalysis;

            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Required
            {
                public required string Name { get; set; }
            }

            [GenerateAccessor]
            public partial class RequiredWithConstructor
            {
                public required string Name { get; set; }

                public RequiredWithConstructor()
                {
                }

                [SetsRequiredMembers]
                public RequiredWithConstructor(string name)
                {
                    Name = name;
                }
            }

            public static class Program
            {
                public static string Run()
                {
                    var hasConstructor = AccessorProvider.FindConstructor(typeof(Required)) is not null;
                    var created = AccessorProvider.GetConstructor<RequiredWithConstructor>().Create("a");
                    return $"{hasConstructor}:{created.Name}";
                }
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("False:a", result);
    }

    [Fact]
    public void DynamicMemberAndConstructorCompile()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Dyn
            {
                public dynamic Value { get; set; } = default!;

                public Dyn(dynamic value)
                {
                }

                public Dyn(string value)
                {
                }
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }

    [Fact]
    public void ObsoleteMemberIsAccessedWithoutWarning()
    {
        // Arrange
        const string source =
            """
            using System;
            using System.Linq;

            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Old
            {
                [Obsolete("old")]
                public int Legacy { get; set; }

                [Obsolete("gone", true)]
                public int Gone { get; set; }

                public int Id { get; set; }

                [Obsolete("ctor")]
                public Old()
                {
                }
            }

            public static class Program
            {
                public static string Run() =>
                    string.Join(",", AccessorProvider.GetFactory<Old>().Members.Select(static x => x.Name));
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("Legacy,Id", result);
    }

    // ------------------------------------------------------------
    // Name
    // ------------------------------------------------------------

    [Fact]
    public void KeywordNamesAndTupleArgumentsWork()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace @event.Inner
            {
                [GenerateAccessor]
                public partial class Data
                {
                    public int @class { get; set; }

                    public Data()
                    {
                    }

                    public Data((int A, int B) pair, in long value)
                    {
                        @class = pair.A + pair.B + (int)value;
                    }
                }
            }

            namespace Test
            {
                public static class Program
                {
                    public static string Run()
                    {
                        var accessor = AccessorProvider.GetAccessor<@event.Inner.Data>();
                        var data = (@event.Inner.Data)AccessorProvider.FindConstructor(typeof(@event.Inner.Data))!.CreateInstance((1, 2), 3L);
                        var before = accessor.GetValue(data, "class");
                        accessor.SetValue(data, "class", 10);
                        return $"{before}:{data.@class}";
                    }
                }
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("6:10", result);
    }

    [Fact]
    public void MemberNamedLikeGeneratedClassCompiles()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Foo
            {
                public int Foo_Accessor { get; set; }

                public int Foo_AccessorFactory { get; set; }

                public int Foo_ConstructorAccessor { get; set; }
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }

    // ------------------------------------------------------------
    // Constructor
    // ------------------------------------------------------------

    [Fact]
    public void TypeWithOnlyConstructorGetsConstructorAccessor()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Command
            {
                private readonly string name;

                public Command(string name)
                {
                    this.name = name;
                }

                public override string ToString() => name;
            }

            public static class Program
            {
                public static string Run() => AccessorProvider.FindConstructor(typeof(Command))!.CreateInstance("run").ToString()!;
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("run", result);
    }

    [Fact]
    public void CreateInstancePrefersMoreSpecificConstructor()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Pick
            {
                public Pick(object value)
                {
                    Kind = "object";
                }

                public Pick(string value)
                {
                    Kind = "string";
                }

                public string Kind { get; }
            }

            public static class Program
            {
                public static string Run()
                {
                    var constructor = AccessorProvider.FindConstructor(typeof(Pick))!;
                    return $"{((Pick)constructor.CreateInstance("x")).Kind},{((Pick)constructor.CreateInstance(1)).Kind}";
                }
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("string,object", result);
    }

    // ------------------------------------------------------------
    // Provider
    // ------------------------------------------------------------

    [Fact]
    public void NestedProviderImplementsProviderInterfaces()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public class Data
            {
                public int Id { get; set; }
            }

            public static partial class Outer
            {
                [GenerateAccessorFor(typeof(Data))]
                internal sealed partial class Providers;
            }

            public static class Program
            {
                public static string Run()
                {
                    var data = new Data();
                    var setter = AccessorProvider.GetFactory<Data, Outer.Providers>().CreateSetter<int>("Id")!;
                    setter(ref data, 5);
                    return data.Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            """;

        // Act
        var result = GeneratorTestHelper.Execute(source);

        // Assert
        Assert.Equal("5", result);
    }

    // ------------------------------------------------------------
    // Nullable
    // ------------------------------------------------------------

    [Fact]
    public void NullableTypeArgumentsCompileWithoutWarning()
    {
        // Arrange
        const string source =
            """
            using System.Collections.Generic;

            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class NullableData
            {
                public NullableData()
                {
                }

                public NullableData(List<string?> items, string? name)
                {
                    Items = items;
                    Name = name;
                }

                public List<string?> Items { get; set; } = new();

                public string? Name { get; set; }

                public Dictionary<string, object?> Map { get; set; } = new();

                public (string? A, int B) Pair { get; set; }
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }
}
