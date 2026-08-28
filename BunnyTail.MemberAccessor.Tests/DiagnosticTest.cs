namespace BunnyTail.MemberAccessor;

using Microsoft.CodeAnalysis;

public class DiagnosticTest
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
}
