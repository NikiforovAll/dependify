namespace Dependify.Cli.Commands;

using System.ComponentModel;
using Dependify.Cli;
using Dependify.Cli.Commands.Settings;
using Dependify.Cli.Formatters;
using Dependify.Core;
using Dependify.Core.Graph;
using Depends.Core.Graph;
using Microsoft.Extensions.Logging;

internal class ShowCommand(
    ProjectLocator projectLocator,
    MsBuildService msBuildService,
    FormatterFactory formatterFactory,
    ILogger<ScanCommand> logger
) : Command<ShowCommandSettings>
{
    public override int Execute(CommandContext context, ShowCommandSettings settings)
    {
        var path = Utils.GetFullPath(settings.Path);

        logger.LogDebug("Scanning {Path}", path);

        var nodes = projectLocator.FolderScan(path);

        var nodesCount = nodes.Count();

        if (nodesCount is 0)
        {
            logger.LogWarning("No projects or solutions found in {Path}", path);

            AnsiConsole.MarkupLine($"[red] No projects or solutions found in [/] [grey]{path}[/]");

            return 1;
        }

        var selected = SelectNode(settings, nodes, nodesCount);

        if (Utils.ShouldOutputTui(settings))
        {
            AnsiConsole.MarkupLine($"[green] Found: [/] [grey]{selected.Path}[/]");
        }

        var graph = GetGraph(msBuildService, settings, selected);

        if (settings.Format is not OutputFormat.Tui)
        {
            using var formatter = formatterFactory.Create(settings);

            formatter.Write(graph);
        }
        else if (settings.DisplayFormat is DependencyDisplayFormat.Box)
        {
            this.DisplayBoxResult(graph);
        }
        else
        {
            this.DisplayTreeResult(graph);
        }

        return 0;
    }

    private void DisplayTreeResult(DependencyGraph graph)
    {
        var prefix = Utils.CalculateCommonPrefix(graph.Nodes);

        var tree = new Tree(".");

        var root = tree.AddNode(graph.Root.Path.RemovePrefix(prefix));

        var alreadyProcessed = new HashSet<Node>();

        graph
            .FindDescendants(graph.Root)
            .OrderBy(OrderByType)
            .ToList()
            .ForEach(n => BuildTree(n, graph, alreadyProcessed, root, prefix, 0));

        AnsiConsole.Write(tree);
    }

    private static int OrderByType(Node n) =>
        n switch
        {
            ProjectReferenceNode => 0,
            _ => 1
        };

    private static void BuildTree(
        Node node,
        DependencyGraph graph,
        HashSet<Node> alreadyProcessed,
        TreeNode parent,
        string prefix,
        int depth
    )
    {
        TreeNode treeNode;

        if (alreadyProcessed.Contains(node))
        {
            treeNode = parent.AddNode($"[underline]{PrintProjectPath(node, prefix, depth)}[/]").Collapse();
        }
        else if (node is ProjectReferenceNode)
        {
            treeNode = parent.AddNode(PrintProjectPath(node, prefix, depth));
        }
        else if (node is PackageReferenceNode package)
        {
            treeNode = parent.AddNode($"[skyblue1]{package.Id}[/] [dim]{package.Version}[/]");
        }
        else
        {
            treeNode = parent.AddNode(node.Id);
        }

        alreadyProcessed.Add(node);

        foreach (var child in graph.FindDescendants(node).OrderBy(OrderByType))
        {
            BuildTree(child, graph, alreadyProcessed, treeNode, prefix, depth + 1);
        }
    }

    private void DisplayBoxResult(DependencyGraph graph)
    {
        var root = graph.Root;

        var prefix = Utils.CalculateCommonPrefix(graph.Nodes);

        var panel = BuildPanel(root, graph, prefix, 0);

        AnsiConsole.Write(panel);
    }

    private static Panel BuildPanel(Node node, DependencyGraph graph, string prefix, int depth)
    {
        var depthLabel = $"[{depth}]".EscapeMarkup();

        var panel = new Panel(BuildTable(graph.FindDescendants(node), graph, prefix, depth))
        {
            Header = new PanelHeader($"{depthLabel} {PrintProjectPath(node, prefix, depth)}"),
            Border = BoxBorder.Rounded,
            Expand = true,
        };

        return panel;
    }

    private static string PrintProjectPath(Node node, string prefix, int depth)
    {
        return $"[{SelectColor(depth)}]{node.Path.RemovePrefix(prefix)}[/]";
    }

    private static string SelectColor(int depth) => (
            depth switch
            {
                0 => Color.Grey93,
                1 => Color.Grey82,
                2 => Color.Grey70,
                3 => Color.Grey62,
                4 => Color.Grey50,
                _ => Color.Grey42
            }
        ).ToString().ToLowerInvariant();

    private static Table BuildTable(IEnumerable<Node> nodes, DependencyGraph graph, string prefix, int depth)
    {
        var table = new Table() { Expand = true }.Border(TableBorder.None);
        table.AddColumn("Dependencies").HideHeaders();

        var packagesTable = BuildPackagesTable(nodes.OfType<PackageReferenceNode>());

        table.AddRow(packagesTable);

        foreach (var node in nodes.OfType<ProjectReferenceNode>())
        {
            var panel = BuildPanel(node, graph, prefix, depth + 1);

            table.AddRow(panel);
        }

        return table;
    }

    private static Table BuildPackagesTable(IEnumerable<PackageReferenceNode> nodes)
    {
        var packagesTable = new Table() { Expand = false };

        packagesTable.AddColumn("Packages").HideHeaders();
        packagesTable.AddColumn("Version").HideHeaders();

        foreach (var node in nodes.OrderBy(n => n.Id))
        {
            packagesTable.AddRow($"[skyblue1]{node.Id}[/]", $"[dim]{node.Version}[/]");
        }

        return packagesTable;
    }

    private static DependencyGraph GetGraph(MsBuildService msBuildService, ShowCommandSettings settings, Node selected)
    {
        if (selected is SolutionReferenceNode solution)
        {
            return Utils.DoSomeWork(
                ctx =>
                {
                    Utils.SetDiagnosticSource(msBuildService, ctx);

                    return msBuildService.AnalyzeReferences(
                        solution,
                        new(settings.IncludePackages!.Value, true, settings.Framework)
                    );
                },
                $"Analyzing {solution.Id}...",
                settings
            );
        }
        else if (selected is ProjectReferenceNode project)
        {
            return Utils.DoSomeWork(
                ctx =>
                {
                    Utils.SetDiagnosticSource(msBuildService, ctx);

                    return msBuildService.AnalyzeReferences(
                        project,
                        new(settings.IncludePackages!.Value, true, settings.Framework)
                    );
                },
                $"Analyzing {project.Id}...",
                settings
            );
        }

        throw new NotImplementedException("Unknown node type.");
    }

    private static Node SelectNode(ShowCommandSettings settings, IEnumerable<Node> nodes, int nodesCount)
    {
        var selected =
            settings.Interactive!.Value && nodesCount > 1
                ? AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("Please select the item to [green]show[/].")
                        .PageSize(10)
                        .AddChoices(nodes.Select(n => n.Id).ToList())
                )
                : nodes.Select(n => n.Id);

        return nodes.First(x => x.Id == selected.First());
    }
}

internal class ShowCommandSettings : BaseAnalyzeCommandSettings
{
    [Description("The visualization style.")]
    [CommandOption("--display")]
    public DependencyDisplayFormat? DisplayFormat { get; set; }

    public override ValidationResult Validate()
    {
        if (this.Format!.Value is not OutputFormat.Tui && this.DisplayFormat.HasValue)
        {
            return ValidationResult.Error("Display format is only supported with TUI output format");
        }

        return ValidationResult.Success();
    }
}

public enum DependencyDisplayFormat
{
    Box,
    Tree
}
