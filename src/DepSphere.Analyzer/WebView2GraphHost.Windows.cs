#if WINDOWS
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DepSphere.Analyzer;

public sealed class WebView2GraphHost
{
    private readonly WebView2 _webView;

    public WebView2GraphHost(WebView2 webView)
    {
        _webView = webView;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _webView.EnsureCoreWebView2Async();
    }

    public async Task RenderAsync(GraphView view, CancellationToken cancellationToken = default)
    {
        await InitializeAsync(cancellationToken);
        var html = GraphViewHtmlBuilder.Build(view);
        _webView.NavigateToString(html);
    }

    public void SubscribeNodeSelected(Action<string> onSelected)
    {
        _webView.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            if (!GraphHostMessageParser.TryParse(args.WebMessageAsJson, out var message) || message is null)
            {
                return;
            }

            if (!string.Equals(message.Type, "nodeSelected", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(message.NodeId))
            {
                return;
            }

            onSelected(message.NodeId);
        };
    }
}
#endif
