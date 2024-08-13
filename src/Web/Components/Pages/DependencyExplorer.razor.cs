namespace Web.Components.Pages;

using Dependify.Core;
using Dependify.Core.Graph;
using Dependify.Core.Serializers;
using Microsoft.JSInterop;
using MudBlazor;

public partial class DependencyExplorer
{
    private bool PackagesIncluded { get; set; }
    private bool Loaded { get; set; }

    private HashSet<string> NodeIds { get; set; } = [];
    private HashSet<string> SelectedNodeIds { get; set; } = [];

    private string DiagramContent { get; set; } = "graph LR \n empty[👋]";

    protected override void OnInitialized()
    {
        this.Loaded = this.SolutionRegistry.IsLoaded;

        if (this.Loaded)
        {
            this.FullLoadRegistry();
        }

        this.SolutionRegistry.OnLoadingEvents.Subscribe(node =>
        {
            switch (node.EventType)
            {
                case NodeEventType.RegistryLoaded:
                    this.FullLoadRegistry();
                    this.Loaded = true;
                    break;
                default:
                    this.NodeIds.Add(node.Id);
                    break;
            }
            this.InvokeAsync(this.StateHasChanged);
        });
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await this.JSRuntime.InvokeVoidAsync("mermaid.init");
        }
    }

    private void FullLoadRegistry()
    {
        this.NodeIds = this.SolutionRegistry.ProjectsAndSolutions.Select(n => n.Id).ToHashSet();
    }

    private async Task ToggleIncludeAsync(string nodeId)
    {
        if (this.SelectedNodeIds.Contains(nodeId))
        {
            this.SelectedNodeIds.Remove(nodeId);
        }
        else
        {
            this.SelectedNodeIds.Add(nodeId);
        }

        this.RefreshDiagram();

        await this.JSRuntime.InvokeVoidAsync("redrawMermaidDiagram", this.DiagramContent);
        await this.InvokeAsync(this.StateHasChanged);
    }

    private async Task IncludeDependencies(string nodeId)
    {
        var includePackages = false;

        var graph = this.SolutionRegistry.GetFullGraph();

        var node = graph.Nodes.FirstOrDefault(n => n.Id == nodeId);

        if (node is null)
        {
            return;
        }

        var nodes = graph.FindDescendants(node).OfType<ProjectReferenceNode>();

        this.SelectedNodeIds.Add(nodeId);

        foreach (var n in nodes)
        {
            this.SelectedNodeIds.Add(n.Id);
        }

        this.RefreshDiagram();

        await this.JSRuntime.InvokeVoidAsync("redrawMermaidDiagram", this.DiagramContent);
        await this.InvokeAsync(this.StateHasChanged);
    }

    private void RefreshDiagram()
    {
        var subGraph = this.GetSubGraph(this.SelectedNodeIds);

        this.DiagramContent = MermaidSerializer.ToString(subGraph);
    }

    private async Task OnPackagesIncludedChangedAsync(bool value)
    {
        this.PackagesIncluded = value;
        this.RefreshDiagram();

        await this.JSRuntime.InvokeVoidAsync("redrawMermaidDiagram", this.DiagramContent);
        await this.InvokeAsync(this.StateHasChanged);
    }

    private DependencyGraph? GetSubGraph(HashSet<string> nodeIds)
    {
        var graph = this.SolutionRegistry.GetFullGraph();

        var subGraph = graph.SubGraph(n =>
        {
            var isPackageIncluded =
                this.PackagesIncluded
                && n.Type == NodeConstants.Package
                && graph.FindAscendants(n).Any(n2 => nodeIds.Contains(n2.Id));

            return nodeIds.Contains(n.Id) || isPackageIncluded;
        });

        return subGraph;
    }

    private async Task CopyDiagramToClipboard()
    {
        await this.JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", this.DiagramContent);
        this.Snackbar.Add($"Copied to clipboard", Severity.Normal);
    }

    private async Task UnselectAll()
    {
        this.SelectedNodeIds.Clear();

        this.RefreshDiagram();

        await this.JSRuntime.InvokeVoidAsync("redrawMermaidDiagram", this.DiagramContent);
        await this.InvokeAsync(this.StateHasChanged);
    }
}
