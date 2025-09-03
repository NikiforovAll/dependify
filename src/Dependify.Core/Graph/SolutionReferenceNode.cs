namespace Dependify.Core.Graph;

using Dependify.Core;

public sealed record SolutionReferenceNode : Node
{
    public bool IsEmpty => this.Id == "$default.sln";
    
    public bool IsXmlFormat { get; private init; }

    public SolutionReferenceNode(string? path = default)
    {
        if (!string.IsNullOrWhiteSpace(path))
        {
            var file = new FileInfo(path);
            this.DirectoryPath = file.Directory!.FullName.NormalizePath();
            this.Path = file.FullName.NormalizePath();
            this.Id = file.Name;
            this.IsXmlFormat = file.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            this.Id = "$default.sln";
            this.IsXmlFormat = false;
        }
    }

    public override string Type { get; } = NodeConstants.Solution;
}
