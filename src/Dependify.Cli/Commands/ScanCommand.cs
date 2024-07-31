namespace Dependify.Cli.Commands;

using Dependify.Cli.Commands.Settings;
using Dependify.Cli.Formatters;
using Dependify.Core;
using Dependify.Core.Graph;
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
        var path = Utils.GetFullPath(settings.Path);

        logger.LogDebug("Scanning {Path}", path);

        var nodes = projectLocator.FullScan(path);

        var solutionNodes = nodes.OfType<SolutionReferenceNode>().ToList();
        var solutionCount = solutionNodes.Count;

        if (solutionCount > 0)
        {
            this.DisplaySolutions(settings, solutionNodes, solutionCount);
        }
        else
        {
            this.DisplayProjects(settings, nodes);
        }

        return 0;
    }

    private void DisplayProjects(ScanCommandSettings settings, IEnumerable<Node> nodes)
    {
        var prefix = Utils.CalculateCommonPrefix(nodes);
        var graph = Cli.Utils.DoSomeWork(
            ctx =>
            {
                Cli.Utils.SetDiagnosticSource(msBuildService, ctx);

                return msBuildService.AnalyzeReferences(
                    nodes.OfType<ProjectReferenceNode>(),
                    new(settings.IncludePackages!.Value, settings.FullScan!.Value, settings.Framework)
                );
            },
            $"Analyzing {prefix}...",
            settings
        );

        prefix = Utils.CalculateCommonPrefix(graph.Nodes);

        this.PrintResult(graph, settings, prefix, prefix);
    }

    private void DisplaySolutions(
        ScanCommandSettings settings,
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
            var graph = Cli.Utils.DoSomeWork(
                ctx =>
                {
                    Cli.Utils.SetDiagnosticSource(msBuildService, ctx);

                    return msBuildService.AnalyzeReferences(
                        solution,
                        new(settings.IncludePackages!.Value, settings.FullScan!.Value, settings.Framework)
                    );
                },
                $"Analyzing {solution.Id}...",
                settings
            );

            var prefix = Utils.CalculateCommonPrefix(graph.Nodes);
            this.PrintResult(graph, settings, solution.Path, prefix);
        }
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

            // TODO: refactor

            table.AddColumn("[italic]Name[/]");
            table.AddColumn("[italic]Type[/]");
            table.AddColumn(
                new TableColumn("[italic]Depends On ([darkgreen]projects[/]/[skyblue1]packages[/])[/]").Centered()
            );
            table.AddColumn(new TableColumn("[italic]Used By ([darkgreen]projects[/])[/]").Centered());
            table.AddColumn($"Path [italic]{prefix}[/]");

            foreach (var node in nodes)
            {
                var displayPath = node.Path.RemovePrefix(prefix);
                var descendants = graph.FindDescendants(node);
                var ascendantsCount = graph.FindAscendants(node).OfType<ProjectReferenceNode>().Count();

                var type = node.Type switch
                {
                    NodeConstants.Project => "[aquamarine3]Project[/]",
                    NodeConstants.Solution => "[red3]Solution[/]",
                    NodeConstants.Package => "[skyblue1]Package[/]",
                    _ => "Unknown"
                };

                var packagesCountLabel = settings.IncludePackages!.Value
                    ? $"/[royalblue1]{descendants.OfType<PackageReferenceNode>().Count()}[/]"
                    : string.Empty;

                var descendantsLabel = node.Type is not NodeConstants.Package
                    ? $"[darkgreen]{descendants.OfType<ProjectReferenceNode>().Count()}{packagesCountLabel}[/]"
                    : string.Empty;

                var ascendantsLabel = $"[darkgreen]{ascendantsCount}[/]";
                table.AddRow(node.Id, type, descendantsLabel, ascendantsLabel, displayPath);
            }

            AnsiConsole.Write(table);
        }
        else
        {
            using var formatter = formatterFactory.Create(settings);

            formatter.Write(graph);
        }
    }
}

internal class ScanCommandSettings : BaseAnalyzeCommandSettings
{
    [CommandOption("--full-scan")]
    public bool? FullScan { get; set; } = false;

    [CommandOption("--exclude-sln")]
    public bool? ExcludeSln { get; set; } = false;
}
