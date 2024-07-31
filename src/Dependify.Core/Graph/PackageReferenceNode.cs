namespace Dependify.Core.Graph;
public sealed record PackageReferenceNode : Node
{
    public PackageReferenceNode(string name, string? version = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(null, nameof(name));
        }
        this.Id = name;
        this.Version = version;
        this.Path = $"https://www.nuget.org/packages/{name}/{version}";
    }

    public override string Type { get; } = NodeConstants.Package;
    public string? Version { get; }
}
