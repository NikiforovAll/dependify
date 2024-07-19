namespace Depends.Core.Graph;

using Dependify.Core;
using Dependify.Core.Graph;

public sealed record SolutionReferenceNode : Node
{
    public SolutionReferenceNode(string? path = default)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            var file = new FileInfo(path);
            this.DirectoryPath = file.Directory!.FullName.NormalizePath();
            this.Path = file.FullName.NormalizePath();
            this.Id = file.Name;
        }
        else
        {
            this.Id = "<empty>";
        }
    }

    public override string Type { get; } = "Solution";
}
