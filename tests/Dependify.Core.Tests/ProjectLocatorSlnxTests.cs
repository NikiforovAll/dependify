namespace Dependify.Core.Tests;

using Dependify.Core;
using Dependify.Core.Graph;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

public class ProjectLocatorSlnxTests
{
    private readonly IServiceProvider serviceProvider;

    public ProjectLocatorSlnxTests()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<ProjectLocator>();

        this.serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void ProjectLocator_ShouldDetectSlnxFiles()
    {
        // Arrange
        var locator = this.serviceProvider.GetRequiredService<ProjectLocator>();
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        var slnxPath = Path.Combine(testDir, "test.slnx");
        File.WriteAllText(slnxPath, @"<Solution>
  <Folder Name=""src"">
    <Project Path=""src/TestProject/TestProject.csproj"" />
  </Folder>
</Solution>");

        try
        {
            // Act
            var nodes = locator.FullScan(testDir);

            // Assert
            var solutionNodes = nodes.OfType<SolutionReferenceNode>().ToList();
            solutionNodes.Should().HaveCount(1);

            var slnxNode = solutionNodes.First();
            slnxNode.IsXmlFormat.Should().BeTrue();
            slnxNode.Id.Should().Be("test.slnx");
            slnxNode.Path.Should().EndWith("test.slnx");
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void ProjectLocator_ShouldDetectBothSlnAndSlnxFiles()
    {
        // Arrange
        var locator = this.serviceProvider.GetRequiredService<ProjectLocator>();
        var tempDir = Path.GetTempPath();
        var testDir = Path.Combine(tempDir, Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDir);

        var slnPath = Path.Combine(testDir, "test.sln");
        File.WriteAllText(slnPath, @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio Version 17
VisualStudioVersion = 17.0.31903.59
MinimumVisualStudioVersion = 10.0.40219.1
Global
EndGlobal");

        var slnxPath = Path.Combine(testDir, "test.slnx");
        File.WriteAllText(slnxPath, @"<Solution>
  <Folder Name=""src"">
    <Project Path=""src/TestProject/TestProject.csproj"" />
  </Folder>
</Solution>");

        try
        {
            // Act
            var nodes = locator.FullScan(testDir);

            // Assert
            var solutionNodes = nodes.OfType<SolutionReferenceNode>().ToList();
            solutionNodes.Should().HaveCount(2);

            var slnNode = solutionNodes.FirstOrDefault(n => n.Id == "test.sln");
            var slnxNode = solutionNodes.FirstOrDefault(n => n.Id == "test.slnx");

            slnNode.Should().NotBeNull();
            slnNode!.IsXmlFormat.Should().BeFalse();

            slnxNode.Should().NotBeNull();
            slnxNode!.IsXmlFormat.Should().BeTrue();
        }
        finally
        {
            // Cleanup
            Directory.Delete(testDir, true);
        }
    }
}
