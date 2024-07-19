namespace Dependify.Core.Graph;

using System.Text.Json.Serialization;


public abstract record Node
{
    public string Id { get; protected set; } = default!;

    public string Path { get; protected set; } = string.Empty;

    [JsonIgnore]
    public string DirectoryPath { get; protected set; } = string.Empty;

    public abstract string Type { get; }
}
