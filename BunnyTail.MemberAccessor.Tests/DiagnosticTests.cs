namespace BunnyTail.MemberAccessor;

using System.Globalization;
using System.Reflection;

using BunnyTail.MemberAccessor.Generator;

using Microsoft.CodeAnalysis;

public class DiagnosticTests
{
    // ------------------------------------------------------------
    // External target
    // ------------------------------------------------------------

    [Fact]
    public void Btma0002InvalidAttributeLocationEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Foo<T>
            {
                public T Value { get; set; } = default!;
            }

            [TypedAccessor(typeof(Foo<int>))]
            public partial class Bar<T>
            {
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTMA0002");
    }

    [Fact]
    public void Btma0006TypeNotPartialEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public sealed class Target
            {
                public int Id { get; set; }
            }

            [GenerateAccessorFor(typeof(Target))]
            public sealed class Provider
            {
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTMA0006");
    }

    // ------------------------------------------------------------
    // Generic unsafe accessor
    // ------------------------------------------------------------

    [Fact]
    public void Btma0009GenericUnsafeAccessorNotSupportedEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Foo<T>
            {
                public int Id { get; set; }

                [AccessorMember]
                private T Value { get; set; } = default!;
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTMA0009");
    }

    // ------------------------------------------------------------
    // BTMA
    // ------------------------------------------------------------
    [Fact]
    public void Btma0005UnsupportedConstructorArityEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Data
            {
                public Data()
                {
                }

                public Data(int p1, int p2, int p3, int p4, int p5, int p6, int p7, int p8, int p9, int p10, int p11, int p12, int p13, int p14, int p15, int p16, int p17)
                {
                    Id = p1;
                }

                public int Id { get; set; }
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTMA0005");
    }

    [Fact]
    public void Btma0001NonGenericTypedAccessorEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Simple
            {
                public int Id { get; set; }
            }

            [TypedAccessor(typeof(Simple))]
            public static partial class Registration
            {
            }
            """);

        Assert.Contains(diagnostics, static x => x.Id == "BTMA0001");
    }

    [Fact]
    public void Btma0003NoAccessibleMemberEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Empty
            {
            }
            """);

        Assert.Contains(diagnostics, static x => x.Id == "BTMA0003");
    }

    [Fact]
    public void Btma0004TargetWithoutGenerateAccessorEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [TypedAccessor(typeof(Plain<int>))]
            public partial class Plain<T>
            {
                public T? Value { get; set; }
            }
            """);

        Assert.Contains(diagnostics, static x => x.Id == "BTMA0004");
    }

    [Fact]
    public void Btma0007UnsupportedExternalTargetEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public interface IContract
            {
                int Id { get; set; }
            }

            [GenerateAccessorFor(typeof(IContract))]
            public static partial class Provider
            {
            }
            """);

        Assert.Contains(diagnostics, static x => x.Id == "BTMA0007");
    }

    [Fact]
    public void Btma0008AlreadyGeneratedTargetEmitsDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Data
            {
                public int Id { get; set; }
            }

            [GenerateAccessorFor(typeof(Data))]
            public static partial class Provider
            {
            }
            """);

        Assert.Contains(diagnostics, static x => x.Id == "BTMA0008");
    }

    // ------------------------------------------------------------
    // Nested type
    // ------------------------------------------------------------

    [Fact]
    public void Btma0010GenericContainingTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public partial class Outer<T>
            {
                [GenerateAccessor]
                public partial class Inner
                {
                    public int Id { get; set; }
                }
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTMA0010");
    }

    [Fact]
    public void Btma0010GenericNestedTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public partial class Outer
            {
                [GenerateAccessor]
                public partial class Inner<T>
                {
                    public T Value { get; set; } = default!;
                }
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTMA0010");
    }

    [Fact]
    public void Btma0011PrivateNestedTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public partial class Outer
            {
                [GenerateAccessor]
                private partial class Inner
                {
                    public int Id { get; set; }
                }
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Contains(diagnostics, static x => x.Id == "BTMA0011");
    }

    // ------------------------------------------------------------
    // Valid
    // ------------------------------------------------------------

    [Fact]
    public void ValidAccessorEmitsNoDiagnostic()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnostics(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Data
            {
                public int Id { get; set; }

                public string Name { get; set; } = default!;
            }
            """);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ValidAccessorGeneratesSource()
    {
        var generated = GeneratorTestHelper.GetGeneratedSource(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Data
            {
                public int Id { get; set; }
            }
            """);

        Assert.Contains("Data", generated, StringComparison.Ordinal);
    }

    [Fact]
    public void ValidAccessorProducesNoCompilationError()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Data
            {
                public int Id { get; set; }
            }
            """);

        Assert.DoesNotContain(diagnostics, static x => x.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ValidNestedAccessorProducesNoCompilationError()
    {
        var diagnostics = GeneratorTestHelper.GetDiagnosticsAll(
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public partial class Outer
            {
                [GenerateAccessor]
                public partial class Inner
                {
                    public int Id { get; set; }
                }
            }

            [GenerateAccessor]
            public partial class Outer_Inner
            {
                public int Id { get; set; }
            }

            public static class Usage
            {
                public static object Get(Outer.Inner inner) =>
                    global::BunnyTail.MemberAccessor.AccessorProvider.GetAccessor<Outer.Inner>().GetValue(inner, "Id")!;

                public static object Get(Outer_Inner inner) =>
                    global::BunnyTail.MemberAccessor.AccessorProvider.GetAccessor<Outer_Inner>().GetValue(inner, "Id")!;
            }
            """);

        Assert.DoesNotContain(diagnostics, static x => x.Severity == DiagnosticSeverity.Error);
        Assert.DoesNotContain(diagnostics, static x => x.Id == "CS8785");
    }

    // ------------------------------------------------------------
    // Unsupported type
    // ------------------------------------------------------------

    [Fact]
    public void Btma0012StaticClassAndRefStructEmitDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public static partial class StaticData
            {
                public static int Id { get; set; }
            }

            [GenerateAccessor]
            public ref partial struct RefData
            {
                public int Id;
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["BTMA0012", "BTMA0012"], problems);
    }

    // ------------------------------------------------------------
    // Provider
    // ------------------------------------------------------------

    [Fact]
    public void Btma0006StaticProviderEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            public sealed class Target
            {
                public int Id { get; set; }
            }

            [GenerateAccessorFor(typeof(Target))]
            public static partial class Provider
            {
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["BTMA0006"], problems);
    }

    [Fact]
    public void Btma0008IsNotReportedWhenProviderIsImplemented()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Data
            {
                public int Id { get; set; }
            }

            [GenerateAccessorFor(typeof(Data))]
            internal sealed partial class Providers;

            public static class Usage
            {
                public static IAccessorFactory<Data> Get() => AccessorProvider.GetFactory<Data, Providers>();
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Empty(problems);
    }

    // ------------------------------------------------------------
    // Reporting
    // ------------------------------------------------------------

    [Fact]
    public void DiagnosticIsReportedOnTypeName()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Empty
            {
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BTMA0003", diagnostic.Id);
        Assert.True(diagnostic.Location.IsInSource);
        Assert.Equal("Empty", diagnostic.Location.SourceTree.GetText(TestContext.Current.CancellationToken).ToString(diagnostic.Location.SourceSpan));
    }

    [Fact]
    public void DiagnosticOfAttributeIsReportedOnceOnAttribute()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            [TypedAccessor(typeof(Gen<int>))]
            public partial class Gen<T>
            {
                public T Value { get; set; } = default!;
            }

            [TypedAccessor(typeof(System.Version))]
            public partial class Gen<T>
            {
            }

            public interface IContract
            {
                int Id { get; set; }
            }

            [GenerateAccessorFor(typeof(IContract))]
            internal sealed partial class Providers;

            [GenerateAccessorFor(typeof(System.Version))]
            internal sealed partial class Providers;
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);

        // Assert
        Assert.Equal(["BTMA0001", "BTMA0007"], diagnostics.Select(static x => x.Id).Order());
        Assert.All(diagnostics, static x => Assert.Contains("(typeof(", x.Location.SourceTree!.GetText(TestContext.Current.CancellationToken).ToString(x.Location.SourceSpan), StringComparison.Ordinal));
    }

    [Fact]
    public void ErrorsCannotBeSuppressed()
    {
        // Arrange
        var descriptors = typeof(AccessorGenerator).Assembly.GetType("BunnyTail.MemberAccessor.Generator.Diagnostics", throwOnError: true)!
            .GetProperties(BindingFlags.Public | BindingFlags.Static)
            .Where(static x => x.PropertyType == typeof(DiagnosticDescriptor))
            .Select(static x => (DiagnosticDescriptor)x.GetValue(null)!)
            .ToList();

        // Assert
        Assert.All(
            descriptors.Where(static x => x.DefaultSeverity == DiagnosticSeverity.Error),
            static x => Assert.Equal([WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.Compiler], x.CustomTags));
    }

    [Fact]
    public void Btma0006NestedProviderInNonPartialTypeEmitsDiagnostic()
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

            public static class Outer
            {
                [GenerateAccessorFor(typeof(Data))]
                internal sealed partial class Providers;
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["BTMA0006"], problems);
    }

    [Fact]
    public void Btma0011FileLocalTypeEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            file partial class Hidden
            {
                public int Id { get; set; }
            }
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["BTMA0011"], problems);
    }

    [Fact]
    public void Btma0007FileLocalExternalTargetEmitsDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            file sealed class Hidden
            {
                public int Id { get; set; }
            }

            [GenerateAccessorFor(typeof(Hidden))]
            internal sealed partial class Providers;
            """;

        // Act
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        Assert.Equal(["BTMA0007"], problems);
    }

    [Fact]
    public void Btma0013TypeNamesDifferingOnlyInCaseEmitDiagnostic()
    {
        // Arrange
        const string source =
            """
            using BunnyTail.MemberAccessor;

            namespace Test;

            [GenerateAccessor]
            public partial class Data
            {
                public int Id { get; set; }
            }

            [GenerateAccessor]
            public partial class data
            {
                public int Id { get; set; }
            }
            """;

        // Act
        var diagnostics = GeneratorTestHelper.GetDiagnostics(source);
        var problems = GeneratorTestHelper.GetProblemIds(source);

        // Assert
        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal("BTMA0013", diagnostic.Id);
        Assert.Contains("type=[Test.data], other=[Test.Data]", diagnostic.GetMessage(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        Assert.Equal(["BTMA0013"], problems);
    }
}
