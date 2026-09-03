namespace BunnyTail.MemberAccessor;

using SourceGenerateHelper.Testing;

public sealed class PipelineCacheTests
{
    private const string Source =
        """
        using BunnyTail.MemberAccessor;

        namespace Test;

        [GenerateAccessor]
        public partial class Data
        {
            public int Id { get; set; }
        }
        """;

    private const string UnrelatedSource =
        """
        namespace Other;

        internal sealed class Unrelated;
        """;

    private const string AddedTargetSource =
        """
        using BunnyTail.MemberAccessor;

        namespace Test;

        [GenerateAccessor]
        public partial class AddedData
        {
            public int Id { get; set; }
        }
        """;

    // ------------------------------------------------------------
    // Cache
    // ------------------------------------------------------------

    [Fact]
    public void UnrelatedEditKeepsModelCached()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, UnrelatedSource);

        // Assert
        Assert.Equal(result.FirstGeneratedText, result.SecondGeneratedText);
        Assert.NotEmpty(result.OutputReasons);
        Assert.DoesNotContain(result.OutputReasons, static x => x.IsChanged());
    }

    [Fact]
    public void TargetEditRebuildsModel()
    {
        // Arrange & Act
        var result = GeneratorTestHelper.RunIncremental(Source, AddedTargetSource);

        // Assert
        Assert.Contains(result.OutputReasons, static x => x.IsChanged());
    }
}
