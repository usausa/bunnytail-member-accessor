namespace BunnyTail.MemberAccessor;

using Microsoft.CodeAnalysis;

public class DiagnosticTest
{
    //-----------------------------------------------------------------------
    // BTMA
    //-----------------------------------------------------------------------

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

    //-----------------------------------------------------------------------
    // Valid
    //-----------------------------------------------------------------------

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
