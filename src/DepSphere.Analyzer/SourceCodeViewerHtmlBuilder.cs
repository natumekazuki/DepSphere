using System.Net;
using System.Text;

namespace DepSphere.Analyzer;

public static class SourceCodeViewerHtmlBuilder
{
    public static string Build(SourceCodeDocument document)
    {
        var path = WebUtility.HtmlEncode(document.FilePath);
        var content = WebUtility.HtmlEncode(document.Content);

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
        builder.AppendLine("  </style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine($"  <div id=\"header\">{path} (L{document.StartLine}-L{document.EndLine})</div>");
        builder.AppendLine($"  <textarea id=\"dep-source-viewer\" readonly>{content}</textarea>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }
}
