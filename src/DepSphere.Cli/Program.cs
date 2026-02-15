using DepSphere.Analyzer;
using System.Text;

var exitCode = await CliProgram.RunAsync(args);
return exitCode;

internal static class CliProgram
{
    public static async Task<int> RunAsync(string[] args)
    {
        if (!CliOptions.TryParse(args, out var options, out var errorMessage))
        {
            Console.Error.WriteLine($"引数エラー: {errorMessage}");
            PrintUsage();
            return 1;
        }

        if (options.ShowHelp)
        {
            PrintUsage();
            return 0;
        }

        try
        {
            var outputDirectory = Path.GetFullPath(options.OutputDirectory);
            Directory.CreateDirectory(outputDirectory);

            Console.WriteLine($"入力: {options.InputPath}");
            Console.WriteLine($"出力先: {outputDirectory}");
            Console.WriteLine($"進捗更新間隔: {options.ProgressInterval}");

            var progress = new Progress<AnalysisProgress>(item =>
            {
                Console.WriteLine(FormatProgress(item));
            });
            var analyzerOptions = new AnalysisOptions
            {
                MetricsProgressReportInterval = options.ProgressInterval
            };

            var graph = await DependencyAnalyzer.AnalyzePathAsync(
                options.InputPath,
                analyzerOptions,
                progress,
                CancellationToken.None);
            var view = GraphViewBuilder.Build(graph);

            var jsonPath = ResolveOutputPath(outputDirectory, options.JsonOutputPath);
            var htmlPath = ResolveOutputPath(outputDirectory, options.HtmlOutputPath);

            CreateParentDirectoryIfNeeded(jsonPath);
            CreateParentDirectoryIfNeeded(htmlPath);

            await File.WriteAllTextAsync(jsonPath, GraphViewJsonSerializer.Serialize(view), Encoding.UTF8);
            await File.WriteAllTextAsync(htmlPath, GraphViewHtmlBuilder.Build(view), Encoding.UTF8);

            Console.WriteLine("保存完了");
            Console.WriteLine($"JSON: {jsonPath}");
            Console.WriteLine($"HTML: {htmlPath}");
            Console.WriteLine($"ノード数: {view.Nodes.Count}");
            Console.WriteLine($"エッジ数: {view.Edges.Count}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("解析失敗: " + ex.Message);
            return 1;
        }
    }

    private static string ResolveOutputPath(string outputDirectory, string outputPath)
    {
        if (Path.IsPathRooted(outputPath))
        {
            return outputPath;
        }

        return Path.Combine(outputDirectory, outputPath);
    }

    private static void CreateParentDirectoryIfNeeded(string path)
    {
        var parentDirectory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(parentDirectory))
        {
            Directory.CreateDirectory(parentDirectory);
        }
    }

    private static string FormatProgress(AnalysisProgress progress)
    {
        if (progress.Current is int current && progress.Total is int total && total > 0)
        {
            return $"[{progress.Stage}] {progress.Message} ({current}/{total})";
        }

        return $"[{progress.Stage}] {progress.Message}";
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            """
            使い方:
              depsphere-cli --input <path-to-sln-or-csproj> [options]

            options:
              --input <path>               解析対象の .sln または .csproj（必須）
              --out <directory>            出力先ディレクトリ（既定: artifacts/depsphere）
              --json <path>                JSON出力パス（既定: graph.json）
              --html <path>                HTML出力パス（既定: graph.html）
              --progress-interval <int>    進捗更新間隔（型件数、既定: 25）
              -h, --help                   ヘルプ表示

            例:
              dotnet run --project src/DepSphere.Cli -- --input ./DepSphere.sln --out ./artifacts/run1
            """);
    }
}

internal sealed record CliOptions(
    string InputPath,
    string OutputDirectory,
    string JsonOutputPath,
    string HtmlOutputPath,
    int ProgressInterval,
    bool ShowHelp)
{
    private const string DefaultOutputDirectory = "artifacts/depsphere";
    private const string DefaultJsonOutputPath = "graph.json";
    private const string DefaultHtmlOutputPath = "graph.html";

    public static bool TryParse(string[] args, out CliOptions options, out string? errorMessage)
    {
        var inputPath = string.Empty;
        var outputDirectory = DefaultOutputDirectory;
        var jsonOutputPath = DefaultJsonOutputPath;
        var htmlOutputPath = DefaultHtmlOutputPath;
        var progressInterval = AnalysisOptions.DefaultMetricsProgressReportInterval;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "-h":
                case "--help":
                    showHelp = true;
                    continue;
                case "--input":
                    if (!TryReadNext(args, ref i, out inputPath))
                    {
                        options = Default();
                        errorMessage = "--input の値が不足しています。";
                        return false;
                    }

                    continue;
                case "--out":
                    if (!TryReadNext(args, ref i, out outputDirectory))
                    {
                        options = Default();
                        errorMessage = "--out の値が不足しています。";
                        return false;
                    }

                    continue;
                case "--json":
                    if (!TryReadNext(args, ref i, out jsonOutputPath))
                    {
                        options = Default();
                        errorMessage = "--json の値が不足しています。";
                        return false;
                    }

                    continue;
                case "--html":
                    if (!TryReadNext(args, ref i, out htmlOutputPath))
                    {
                        options = Default();
                        errorMessage = "--html の値が不足しています。";
                        return false;
                    }

                    continue;
                case "--progress-interval":
                    if (!TryReadNext(args, ref i, out var progressText))
                    {
                        options = Default();
                        errorMessage = "--progress-interval の値が不足しています。";
                        return false;
                    }

                    if (!int.TryParse(progressText, out progressInterval) || progressInterval < 1 || progressInterval > 10000)
                    {
                        options = Default();
                        errorMessage = "--progress-interval は 1 から 10000 の整数で指定してください。";
                        return false;
                    }

                    continue;
            }

            if (arg.StartsWith("-", StringComparison.Ordinal))
            {
                options = Default();
                errorMessage = $"未対応オプションです: {arg}";
                return false;
            }

            if (string.IsNullOrWhiteSpace(inputPath))
            {
                inputPath = arg;
                continue;
            }

            options = Default();
            errorMessage = $"余剰引数です: {arg}";
            return false;
        }

        if (!showHelp && string.IsNullOrWhiteSpace(inputPath))
        {
            options = Default();
            errorMessage = "--input を指定してください。";
            return false;
        }

        options = new CliOptions(
            inputPath,
            outputDirectory,
            jsonOutputPath,
            htmlOutputPath,
            progressInterval,
            showHelp);
        errorMessage = null;
        return true;
    }

    private static bool TryReadNext(string[] args, ref int index, out string value)
    {
        value = string.Empty;
        var nextIndex = index + 1;
        if (nextIndex >= args.Length)
        {
            return false;
        }

        value = args[nextIndex];
        index = nextIndex;
        return true;
    }

    private static CliOptions Default()
    {
        return new CliOptions(
            InputPath: string.Empty,
            OutputDirectory: DefaultOutputDirectory,
            JsonOutputPath: DefaultJsonOutputPath,
            HtmlOutputPath: DefaultHtmlOutputPath,
            ProgressInterval: AnalysisOptions.DefaultMetricsProgressReportInterval,
            ShowHelp: false);
    }
}
