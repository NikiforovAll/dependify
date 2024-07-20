namespace Dependify.Cli.Commands;

using Dependify.Cli;
using Dependify.Cli.Commands.Settings;
using Dependify.Cli.Formatters;
using Dependify.Core;
using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Extensions.Logging;

internal class ScanCommand(
    ProjectLocator projectLocator,
    MsBuildService msBuildService,
    FormatterFactory formatterFactory,
    ILogger<ScanCommand> logger
) : Command<ScanCommandSettings>
{
    public override int Execute(CommandContext context, ScanCommandSettings settings)
    {
        var path = GetFullPath(settings);

        logger.LogDebug("Scanning {Path}", path);

        var nodes = projectLocator.FullScan(path);

        var prefix = CalculateCommonPrefix(nodes);

        var solutionNodes = nodes.OfType<SolutionReferenceNode>().Where(n => n is not null).ToList();
        var solutionCount = solutionNodes.Count;

        if (solutionCount > 0)
        {
            this.DisplaySolutions(settings, prefix, solutionNodes, solutionCount);
        }
        else
        {
            this.DisplayProjects(settings, nodes, prefix);
        }

        return 0;
    }

    private void DisplayProjects(ScanCommandSettings settings, IEnumerable<Node> nodes, string prefix)
    {
        var graph = DoSomeWork(
            () =>
                msBuildService.AnalyzeReferences(
                    nodes.OfType<ProjectReferenceNode>(),
                    new(settings.IncludePackages!.Value, settings.FullScan!.Value, settings.Framework)
                ),
            $"Analyzing {prefix}...",
            settings
        );

        this.PrintResult(graph, settings, prefix, prefix);
    }

    private void DisplaySolutions(
        ScanCommandSettings settings,
        string prefix,
        List<SolutionReferenceNode> solutionNodes,
        int solutionCount
    )
    {
        var selectedSolutions =
            settings.Interactive!.Value && solutionCount > 1
                ? AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Please select the solutions to [green]analyze[/].")
                        .PageSize(10)
                        .AddChoices(solutionNodes.Select(n => n.Id).ToList())
                )
                : solutionNodes.Select(n => n.Id);

        foreach (var solution in solutionNodes.Where(n => selectedSolutions.Contains(n.Id)))
        {
            var graph = DoSomeWork(
                () =>
                    msBuildService.AnalyzeReferences(
                        solution,
                        new(settings.IncludePackages!.Value, settings.FullScan!.Value, settings.Framework)
                    ),
                $"Analyzing {solution.Id}...",
                settings
            );
            this.PrintResult(graph, settings, solution.Path, prefix);
        }
    }

    private static T DoSomeWork<T>(Func<T> func, string message, ScanCommandSettings settings)
    {
        if (settings.LogLevel is LogLevel.None)
        {
            return AnsiConsole.Status().Start(message, _ => func());
        }

        return func();
    }

    private static string GetFullPath(ScanCommandSettings settings)
    {
        FileSystemInfo fileSystemInfo;

        var pathArg = settings.Path;
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

    private void PrintResult(DependencyGraph graph, ScanCommandSettings settings, string title, string prefix)
    {
        if (settings.ExcludeSln!.Value)
        {
            graph = graph.CopyNoRoot();
        }

        if (settings.Format is OutputFormat.Tui)
        {
            var table = new Table() { Caption = new TableTitle(title) };

            var nodes = graph.Nodes.AsEnumerable();

            nodes = nodes.OrderByDescending(n => n.Type).ThenBy(n => n.DirectoryPath);

            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Dependencies (d/a) count");
            // TODO: consider using text TextPath(
            table.AddColumn($"Path {prefix}");

            foreach (var node in nodes)
            {
                var displayPath = node.Path.RemovePrefix(prefix).TrimStart('/', '\\');
                var descendantsCount = graph.FindDescendants(node).Count();
                var ascendantsCount = graph.FindAscendants(node).Count();

                table.AddRow(node.Id, node.Type, $"{descendantsCount}/{ascendantsCount}", displayPath);
            }

            AnsiConsole.Write(table);
        }
        else
        {
            using var formatter = formatterFactory.Create(settings);

            formatter.Write(graph);
        }
    }

    public static string CalculateCommonPrefix(IEnumerable<Node> nodes)
    {
        var prefix = nodes
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

internal class ScanCommandSettings : BaseAnalyzeCommandSettings
{
    [CommandOption("--full-scan")]
    public bool? FullScan { get; set; } = false;

    [CommandOption("--exclude-sln")]
    public bool? ExcludeSln { get; set; } = false;
}
