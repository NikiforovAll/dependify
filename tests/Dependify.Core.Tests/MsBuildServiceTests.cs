namespace Dependify.Core.Tests;

using Dependify.Core;
using Dependify.Core.Graph;
using Microsoft.Extensions.DependencyInjection;

public class UnitTest1
{
    [Fact(Skip = "Requires a file system seed")]
    public void MsBuildServiceTests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ProjectLocator>()
            .AddSingleton<MsBuildService>();

        var provider = services.BuildServiceProvider();

        var locator = provider.GetRequiredService<ProjectLocator>();
        var msBuildService = provider.GetRequiredService<MsBuildService>();

        var nodes = locator.FullScan("C:\\Users\\joe\\source\\repos\\Dependify");

        var solution = nodes.OfType<SolutionReferenceNode>().FirstOrDefault();

        var graph = msBuildService.AnalyzeReferences(solution, MsBuildConfig.Default);

        var subgraph = graph.SubGraph(n => n.Id.Contains("AwesomeProjectName"));

        subgraph.Nodes.Count().Should().Be(1);
    }
}
