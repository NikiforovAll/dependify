namespace Web.Components.Pages;

using Dependify.Core;
using Dependify.Core.Graph;
using Dependify.Core.Serializers;
using Microsoft.JSInterop;
using MudBlazor;

public partial class Home
{
    private double ProgressValue = 0;

    private enum DiagramStyle
    {
        Graph,
        C4
    }

    private List<NodeWithProgress> nodes = [];

    private class NodeWithProgress(Node node, bool loaded)
    {
        public Node Node { get; set; } = node;
        public bool Loaded { get; set; } = loaded;
    }

    private List<SolutionReferenceNode> solutionNodes = [];

    private string? selectedSolution;

    private List<NodeUsage> nodeUsageStatistics = [];

    private string? commonPrefix;

    private DisplayMode displayMode = DisplayMode.All;

    protected override async Task OnInitializedAsync()
    {
        this.SolutionRegistry.OnLoadingEvents.Subscribe(node =>
        {
            switch (node.EventType)
            {
                case NodeEventType.SolutionLoading:
                    this.Snackbar.Add($"Loading - {node.Id}", Severity.Normal);
                    break;
                case NodeEventType.SolutionLoaded:
                    this.Snackbar.Add($"Loaded - {node.Id}", Severity.Success);
                    break;
                case NodeEventType.ProjectLoaded:
                    break;
                case NodeEventType.RegistryLoaded:
                    this.Snackbar.Add(node.Message, Severity.Success);
                    break;
            }
        });

        this.SolutionRegistry.OnLoadingEvents.Subscribe(node =>
        {
            switch (node.EventType)
            {
                case NodeEventType.SolutionLoaded:
                case NodeEventType.ProjectLoaded:
                    var project = this.nodes.FirstOrDefault(n => n.Node.Id == node.Id);

                    if (project is not null)
                    {
                        project.Loaded = true;
                        this.InvokeAsync(this.StateHasChanged);
                    }
                    break;
            }
        });

        this.LoadSolutions();

        this.selectedSolution = this.SolutionRegistry.Solutions.FirstOrDefault()?.Id;
    }

    private async Task LoadSolutionsAsync(bool force = false)
    {
        if (force)
        {
            this.nodes = this.SolutionRegistry.Nodes.Select(n => new NodeWithProgress(n, false)).ToList();

            this.Snackbar.Add($"Re-syncing solutions", Severity.Warning);

            Task.Run(async () =>
            {
                await this.SolutionRegistry.LoadSolutionsAsync(this.MsBuildConfig.Value);
                this.LoadSolutions();
            });
        }
    }

    private void LoadSolutions()
    {
        this.nodes = this
            .SolutionRegistry.Nodes.Select(n => new NodeWithProgress(n, this.SolutionRegistry.IsLoaded))
            .ToList();

        this.commonPrefix = Utils.CalculateCommonPrefix(this.nodes.Select(n => n.Node));

        this.solutionNodes = this.SolutionRegistry.Solutions.ToList();
    }

    private void AnalyzeSolution()
    {
        var solution = this.solutionNodes.FirstOrDefault(n => n.Id == this.selectedSolution);

        if (solution is null)
        {
            this.displayMode = DisplayMode.All;
        }
        else if (this.SolutionRegistry.GetGraph(solution) is var graph && graph is not null)
        {
            var projectUsageStatistics = graph
                .Nodes.OfType<ProjectReferenceNode>()
                .Select(n => this.SolutionRegistry.GetDependencyCount(solution, n));

            this.nodeUsageStatistics =
            [
                this.SolutionRegistry.GetDependencyCount(solution, solution),
                .. projectUsageStatistics
            ];

            this.displayMode = DisplayMode.Solutions;
        }
        else
        {
            this.Snackbar.Add($"Analyzing - {this.selectedSolution}, please wait...", Severity.Normal);
        }
    }

    private async Task ShowDiagramModal(string nodeId, DiagramStyle style)
    {
        var solution = this.solutionNodes.FirstOrDefault(n => n.Id == this.selectedSolution);

        var subGraph = this.GetSubGraph(
            nodeId,
            style == DiagramStyle.C4 ? _ => true : n => n.Type is not NodeConstants.Package
        );

        if (subGraph is null)
        {
            return;
        }

        var diagramContent =
            style == DiagramStyle.C4 ? MermaidC4Serializer.ToString(subGraph) : MermaidSerializer.ToString(subGraph);

        var parameters = new DialogParameters { ["DiagramContent"] = diagramContent };
        var options = new DialogOptions
        {
            CloseButton = true,
            MaxWidth = MaxWidth.ExtraExtraLarge,
            FullWidth = true,
            FullScreen = false,
            CloseOnEscapeKey = true,
        };

        var dialog = this.DialogService.Show<DiagramModal>(subGraph.Root.Id, parameters, options);

        await dialog.Result;
    }

    private async Task CopyDiagramToClipboard(string nodeId)
    {
        var solution = this.solutionNodes.FirstOrDefault(n => n.Id == this.selectedSolution);

        var subGraph = this.GetSubGraph(nodeId, _ => true);

        if (subGraph is null)
        {
            return;
        }

        this.Snackbar.Add($"Copied to clipboard", Severity.Normal);

        await this.JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", JsonGraphSerializer.ToString(subGraph));
    }

    private DependencyGraph? GetSubGraph(string nodeId, Predicate<Node>? filter = default)
    {
        var solution = this.solutionNodes.FirstOrDefault(n => n.Id == this.selectedSolution);

        if (this.SolutionRegistry.GetGraph(solution) is var graph && graph is not null)
        {
            var project = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);

            this.displayMode = DisplayMode.Solutions;

            var subGraph = graph.SubGraph(project, filter);

            return subGraph;
        }

        return default;
    }

    private enum DisplayMode
    {
        All,
        Solutions,
    }
}
