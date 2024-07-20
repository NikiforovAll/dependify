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
            ctx =>
            {
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

                return msBuildService.AnalyzeReferences(
                    nodes.OfType<ProjectReferenceNode>(),
                    new(settings.IncludePackages!.Value, settings.FullScan!.Value, settings.Framework)
                );
            },
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
                ctx =>
                {
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

                    return msBuildService.AnalyzeReferences(
                        solution,
                        new(settings.IncludePackages!.Value, settings.FullScan!.Value, settings.Framework)
                    );
                },
                $"Analyzing {solution.Id}...",
                settings
            );
            this.PrintResult(graph, settings, solution.Path, prefix);
        }
    }

    private static T DoSomeWork<T>(Func<StatusContext?, T> func, string message, ScanCommandSettings settings)
    {
        if (!settings.Interactive!.Value)
        {
            return func(null);
        }

        if (
            (settings.LogLevel is LogLevel.None && settings.Format is OutputFormat.Tui)
            || (!string.IsNullOrWhiteSpace(settings.OutputPath))
        )
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

            // TODO: refactor

            table.AddColumn("[italic]Name[/]");
            table.AddColumn("[italic]Type[/]");
            table.AddColumn(
                new TableColumn(
                    "[italic]Depends On ([darkgreen]projects[/]/[royalblue1]packages[/]) count[/]"
                ).Centered()
            );
            table.AddColumn(new TableColumn("[italic]Used By ([darkgreen]projects[/]) count[/]").Centered());
            table.AddColumn($"Path [italic]{prefix}[/]");

            foreach (var node in nodes)
            {
                var displayPath = node.Path.RemovePrefix(prefix).TrimStart('/', '\\');
                var descendants = graph.FindDescendants(node);
                var ascendantsCount = graph.FindAscendants(node).OfType<ProjectReferenceNode>().Count();

                var type = node.Type switch
                {
                    "Project" => "[aquamarine3]Project[/]",
                    "Solution" => "[red3]Solution[/]",
                    "Package" => "[skyblue1]Package[/]",
                    _ => "Unknown"
                };

                var packagesCountLabel = settings.IncludePackages!.Value
                    ? $"/[royalblue1]{descendants.OfType<PackageReferenceNode>().Count()}[/]"
                    : string.Empty;

                var descendantsLabel = node.Type is not "Package"
                    ? $"[darkgreen]{descendants.OfType<ProjectReferenceNode>().Count()}{packagesCountLabel}[/]"
                    : string.Empty;

                var ascendantsLabel = node.Type is not "Package" ? $"[darkgreen]{ascendantsCount}[/]" : string.Empty;
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
