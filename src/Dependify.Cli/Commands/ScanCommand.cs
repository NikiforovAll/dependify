namespace Dependify.Cli.Commands;

using Dependify.Cli;
using Dependify.Cli.Commands.Settings;
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

    private void DisplayProjects(ScanCommandSettings settings, IEnumerable<Node> nodes, string prefix) =>
        AnsiConsole
            .Status()
            .Start(
                $"Analyzing {prefix}...",
                ctx =>
                {
                    var graph = msBuildService.AnalyzeReferences(
                        nodes.OfType<ProjectReferenceNode>(),
                        settings.IncludePackages!.Value,
                        settings.Framework
                    );

                    this.PrintResult(graph, settings, prefix, prefix);
                }
            );

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
                        .Title("Please select the solutions to [green]analyze[/]?")
                        .PageSize(10)
                        .AddChoices(solutionNodes.Select(n => n.Id).ToList())
                )
                : solutionNodes.Select(n => n.Id);

        foreach (var solution in solutionNodes.Where(n => selectedSolutions.Contains(n.Id)))
        {
            AnsiConsole
                .Status()
                .Start(
                    $"Analyzing {solution.Id}...",
                    ctx =>
                    {
                        var graph = msBuildService.AnalyzeReferences(
                            solution,
                            settings.IncludePackages!.Value,
                            settings.Framework
                        );

                        ctx.SpinnerStyle(Style.Parse("green"));

                        this.PrintResult(graph, settings, solution.Path, prefix);
                    }
                );
        }
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
        if (settings.Format is OutputFormat.Tui)
        {
            var table = new Table() { Caption = new TableTitle(title) };

            var nodes = graph.Nodes.AsEnumerable();

            nodes = nodes.OrderByDescending(n => n.Type).ThenBy(n => n.DirectoryPath);

            table.AddColumn("Name");
            table.AddColumn("Type");
            table.AddColumn("Dependencies (d/a) count");
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
            var formatter = formatterFactory.Create(settings);

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

internal class ScanCommandSettings : BaseAnalyzeCommandSettings { }
