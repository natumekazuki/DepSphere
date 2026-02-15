using DepSphere.Analyzer;
using System.Text;
using System.Text.RegularExpressions;

namespace DepSphere.Analyzer.Tests;

public class GraphViewHtmlBuilderTests
{
    [Fact]
    public void HTMLにグラフデータと描画要素を含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("dep-graph-canvas", html);
        Assert.Contains("three.min.js", html);
        Assert.Contains("window.__depSphereGraph", html);

        var match = Regex.Match(html, "atob\\(\\\"(?<base64>[A-Za-z0-9+/=]+)\\\"\\)");
        Assert.True(match.Success);

        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(match.Groups["base64"].Value));
        Assert.Contains("Sample.Impl", decoded);
    }

    [Fact]
    public void ノード選択通知スクリプトを含める()
    {
        var view = BuildSampleView();

        var html = GraphViewHtmlBuilder.Build(view);

        Assert.Contains("chrome.webview.postMessage", html);
        Assert.Contains("nodeSelected", html);
    }

    [Fact]
    public void WebViewメッセージを解析できる()
    {
        var raw = "{\"type\":\"nodeSelected\",\"nodeId\":\"Sample.Impl\"}";

        var ok = GraphHostMessageParser.TryParse(raw, out var message);

        Assert.True(ok);
        Assert.NotNull(message);
        Assert.Equal("nodeSelected", message!.Type);
        Assert.Equal("Sample.Impl", message.NodeId);
    }

    private static GraphView BuildSampleView()
    {
        return new GraphView(
            new[]
            {
                new GraphViewNode(
                    "Sample.Impl",
                    "Sample.Impl",
                    0,
                    0,
                    0,
                    16,
                    "#ef4444",
                    "critical",
                    new TypeMetrics(3, 20, 4, 6, 2, 1, 0.92))
            },
            new[]
            {
                new GraphViewEdge("Sample.Impl", "Sample.Base", "inherit", "#22c55e")
            });
    }
}
