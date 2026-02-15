using System.Net;
using System.Text;
using System.Text.Json;

namespace DepSphere.Analyzer;

public static class SourceCodeViewerHtmlBuilder
{
    public static string Build(SourceCodeDocument document, IReadOnlyDictionary<string, string>? symbolLinks = null)
    {
        var path = WebUtility.HtmlEncode(document.FilePath);
        var content = WebUtility.HtmlEncode(document.Content);
        var linksJson = JsonSerializer.Serialize(symbolLinks ?? new Dictionary<string, string>());
        var linksBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(linksJson));

        var builder = new StringBuilder();
        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"ja\">");
        builder.AppendLine("<head>");
        builder.AppendLine("  <meta charset=\"utf-8\" />");
        builder.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        builder.AppendLine("  <title>DepSphere Source Viewer</title>");
        builder.AppendLine("  <style>");
        builder.AppendLine("    html, body { margin: 0; padding: 0; background: #0f172a; color: #e2e8f0; }");
        builder.AppendLine("    #header { padding: 8px 12px; font: 12px ui-monospace, SFMono-Regular, Menlo, monospace; background: #111827; border-bottom: 1px solid #334155; }");
        builder.AppendLine("    #dep-source-viewer { width: 100vw; height: calc(100vh - 38px); box-sizing: border-box; border: 0; resize: none; background: #020617; color: #e2e8f0; font: 13px/1.45 ui-monospace, SFMono-Regular, Menlo, monospace; padding: 12px; }");
        builder.AppendLine("    #hint { padding: 4px 12px; color: #94a3b8; font: 11px ui-monospace, SFMono-Regular, Menlo, monospace; border-top: 1px solid #1e293b; }");
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine($"  <div id=\"header\">{path} (L{document.StartLine}-L{document.EndLine})</div>");
        builder.AppendLine($"  <textarea id=\"dep-source-viewer\" readonly>{content}</textarea>");
        builder.AppendLine("  <div id=\"hint\">シンボルをダブルクリックで関連ノードへジャンプ</div>");
        builder.AppendLine("  <script>");
        builder.AppendLine($"    const depSymbolLinks = JSON.parse(atob(\"{linksBase64}\"));");
        builder.AppendLine("    const viewer = document.getElementById('dep-source-viewer');");
        builder.AppendLine("    function postNodeSelected(nodeId) {");
        builder.AppendLine("      const payload = { type: 'nodeSelected', nodeId: nodeId };");
        builder.AppendLine("      if (window.chrome && window.chrome.webview && window.chrome.webview.postMessage) {");
        builder.AppendLine("        window.chrome.webview.postMessage(payload);");
        builder.AppendLine("      }");
        builder.AppendLine("      if (window.__depSphereHost && typeof window.__depSphereHost.onNodeSelected === 'function') {");
        builder.AppendLine("        window.__depSphereHost.onNodeSelected(nodeId);");
        builder.AppendLine("      }");
        builder.AppendLine("    }");
        builder.AppendLine("    viewer.addEventListener('dblclick', () => {");
        builder.AppendLine("      const start = viewer.selectionStart || 0;");
        builder.AppendLine("      const end = viewer.selectionEnd || 0;");
        builder.AppendLine("      if (end <= start) return;");
        builder.AppendLine("      const token = viewer.value.substring(start, end).trim();");
        builder.AppendLine("      if (!token) return;");
        builder.AppendLine("      const nodeId = depSymbolLinks[token];");
        builder.AppendLine("      if (!nodeId) return;");
        builder.AppendLine("      postNodeSelected(nodeId);");
        builder.AppendLine("    });");
        builder.AppendLine("  </script>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }
}
