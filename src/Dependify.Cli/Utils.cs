namespace Dependify.Cli;

using Dependify.Cli.Commands.Settings;
using Dependify.Core;
using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Extensions.Logging;

internal static class Utils
{
    public static string RemovePrefix(this string value, string prefix)
    {
        if (value.StartsWith(prefix, StringComparison.InvariantCulture))
        {
            return value[prefix.Length..].TrimStart('/', '\\');
        }

        return value;
    }

    public static string GetFullPath(string pathArg)
    {
        FileSystemInfo fileSystemInfo;

        if (File.Exists(pathArg))
        {
            fileSystemInfo = new FileInfo(pathArg);
        }
        else if (Directory.Exists(pathArg))
        {
            fileSystemInfo = new DirectoryInfo(pathArg);
        }
        else
        {
            throw new ArgumentException("The specified path does not exist.", nameof(pathArg));
        }

        var path = fileSystemInfo.FullName;
        return path;
    }

    public static bool ShouldOutputTui(GlobalCommandSettings settings) =>
        (settings.LogLevel is LogLevel.None && settings.Format is OutputFormat.Tui)
        || (!string.IsNullOrWhiteSpace(settings.OutputPath));

    public static T DoSomeWork<T>(Func<StatusContext?, T> func, string message, GlobalCommandSettings settings)
    {
        if (!settings.Interactive!.Value)
        {
            return func(null);
        }

        if (ShouldOutputTui(settings))
        {
            return AnsiConsole
                .Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Dots)
                .Start(
                    message,
                    ctx =>
                    {
                        var result = func(ctx);

                        return result;
                    }
                );
        }

        return func(null);
    }

    public static void SetDiagnosticSource(MsBuildService msBuildService, StatusContext? ctx) =>
        msBuildService.SetDiagnosticSource(
            new(
                project => ctx?.Status($"[yellow]Loading...[/] [grey]{project.Path}[/]"),
                project =>
                {
                    if (ctx is not null)
                    {
                        AnsiConsole.MarkupLine($"[green] Loaded: [/] [grey]{project.ProjectFilePath}[/]");
                    }
                }
            )
        );

    public static string CalculateCommonPrefix(IEnumerable<Node> nodes)
    {
        var prefix = nodes
            .OfType<ProjectReferenceNode>()
            .Select(n => n.Path)
            .Aggregate(
                (a, b) =>
                    a.Zip(b)
                        .TakeWhile(p => p.First == p.Second)
                        .Select(p => p.First)
                        .Aggregate(string.Empty, (a, b) => a + b)
            );

        prefix = prefix[..prefix.LastIndexOfAny(['/', '\\'])];

        return prefix;
    }
}
