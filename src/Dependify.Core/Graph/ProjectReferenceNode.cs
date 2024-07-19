namespace Depends.Core.Graph;

using Dependify.Core;
using Dependify.Core.Graph;

public sealed record ProjectReferenceNode : Node
{
    public ProjectReferenceNode(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(null, nameof(path));
        }
        var file = new FileInfo(path);
        this.DirectoryPath = file.Directory!.FullName.NormalizePath();
        this.Path = file.FullName.NormalizePath();
        this.Id = file.Name;
    }

    public override string Type { get; } = "Project";
}
