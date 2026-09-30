namespace BunnyTail.MemberAccessor;

using System.Collections.Generic;
using System.Runtime.Loader;

using BunnyTail.MemberAccessor.Generator;

using Microsoft.CodeAnalysis;

using SourceGenerateHelper.Testing;

internal static class GeneratorTestHelper
{
    private static GeneratorTestRunner Runner => GeneratorTestRunner
        .For<AccessorGenerator>()
        .WithReference(typeof(GenerateAccessorAttribute).Assembly)
        .WithDiagnosticPrefix("BTMA");

    public static IReadOnlyList<Diagnostic> GetDiagnostics(string source) => Runner.GetDiagnostics(source);

    public static IReadOnlyList<Diagnostic> GetDiagnosticsAll(string source) => Runner.GetDiagnosticsAll(source);

    public static string GetGeneratedSource(string source) => Runner.GetGeneratedSource(source);

    public static IReadOnlyList<string> GetProblemIds(string source) =>
        [.. Runner.GetProblems(source).Select(static x => x.Id)];

    // Compiles the source with the generator and returns the result of Test.Program.Run()
    public static string Execute(string source)
    {
        var result = Runner.Run(source);
        Assert.True(result.Problems.Count == 0, String.Join(Environment.NewLine, result.Problems));

        using var stream = new MemoryStream();
        var emitResult = result.OutputCompilation.Emit(stream);
        Assert.True(emitResult.Success, String.Join(Environment.NewLine, emitResult.Diagnostics));

        stream.Position = 0;
        var assembly = new AssemblyLoadContext(null, isCollectible: true).LoadFromStream(stream);
        return (string)assembly.GetType("Test.Program", throwOnError: true)!.GetMethod("Run")!.Invoke(null, null)!;
    }

    public static IncrementalRunResult RunIncremental(string source, string addedSource) =>
        Runner.WithTracking().RunIncremental(source, addedSource);
}
