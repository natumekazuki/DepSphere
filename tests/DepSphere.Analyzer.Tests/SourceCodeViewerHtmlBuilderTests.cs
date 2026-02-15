using DepSphere.Analyzer;

namespace DepSphere.Analyzer.Tests;

public class SourceCodeViewerHtmlBuilderTests
{
    [Fact]
    public void 読み取り専用ビューアHTMLを生成できる()
    {
        var doc = new SourceCodeDocument(
            "/tmp/Sample.cs",
            10,
            20,
            "public class Sample {}\n");

        var html = SourceCodeViewerHtmlBuilder.Build(doc);

        Assert.Contains("dep-source-viewer", html);
        Assert.Contains("readonly", html);
        Assert.Contains("public class Sample", html);
    }
}
