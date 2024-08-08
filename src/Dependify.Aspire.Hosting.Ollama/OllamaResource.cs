namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Ollama server
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OllamaResource"/> class.
/// </remarks>
/// <param name="name">The name of the resource.</param>
/// <param name="enableGpu">Whether or not to enable GPU support.</param>
public class OllamaResource(string name, bool enableGpu = false)
    : ContainerResource(name),
        IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";
    internal bool EnableGpu { get; set; } = enableGpu;

    private EndpointReference? primaryEndpoint;

    /// <summary>
    /// Gets the http endpoint for the Ollama database.
    /// </summary>
    public EndpointReference PrimaryEndpoint => this.primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Ollama http endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{this.PrimaryEndpoint.Property(EndpointProperty.Url)}");

    /// <summary>
    /// A dictionary where the key is the resource name and the value is the model name.
    /// </summary>
    public List<string> Models { get; } = [];

    internal void AddModel(string modelName)
    {
        this.Models.Add(modelName);
    }
}
