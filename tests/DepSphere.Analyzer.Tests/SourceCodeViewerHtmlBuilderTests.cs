using DepSphere.Analyzer;
using System.Text;
using System.Text.RegularExpressions;

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
        Assert.Contains("dep-source-viewer-fallback", html);
        Assert.Contains("readonly", html);
        Assert.Contains("monaco-editor", html);
        Assert.Contains("initializeMonaco", html);
        Assert.Contains("depSymbolLinks", html);
        Assert.Contains("dblclick", html);

        var sourceMatch = Regex.Match(html, "const depSourceBase64 = \"(?<base64>[A-Za-z0-9+/=]+)\";");
        Assert.True(sourceMatch.Success);

        var sourceDecoded = Encoding.UTF8.GetString(Convert.FromBase64String(sourceMatch.Groups["base64"].Value));
        Assert.Contains("public class Sample", sourceDecoded);
    }

    [Fact]
    public void シンボルリンクがある場合はダブルクリックジャンプスクリプトを含める()
    {
        var doc = new SourceCodeDocument(
            "/tmp/Sample.cs",
            1,
            5,
            "public class Consumer { public Impl Build() => new Impl(); }");
        var links = new Dictionary<string, string>
        {
            ["Impl"] = "Demo.Impl"
        };

        var html = SourceCodeViewerHtmlBuilder.Build(doc, links);

        Assert.Contains("JSON.parse(atob(", html);
        Assert.Contains("postNodeSelected", html);
        Assert.Contains("depSymbolLinks[token]", html);

        var match = Regex.Match(html, "JSON\\.parse\\(atob\\(\"(?<base64>[A-Za-z0-9+/=]+)\"\\)\\)");
        Assert.True(match.Success);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups["base64"].Value));
        Assert.Contains("Demo.Impl", decoded);
    }
}
