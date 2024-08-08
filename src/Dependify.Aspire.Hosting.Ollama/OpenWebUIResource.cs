namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents an Open WebUI resource
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OpenWebUIResource"/> class.
/// </remarks>
/// <param name="name">The name of the resource.</param>
public class OpenWebUIResource(string name) : ContainerResource(name), IResourceWithConnectionString
{
    internal const string PrimaryEndpointName = "http";

    private EndpointReference? primaryEndpoint;

    /// <summary>
    /// Gets the http endpoint for the Open WebUI resource.
    /// </summary>
    public EndpointReference PrimaryEndpoint => this.primaryEndpoint ??= new(this, PrimaryEndpointName);

    /// <summary>
    /// Gets the connection string expression for the Open WebUI endpoint.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        ReferenceExpression.Create($"{this.PrimaryEndpoint.Property(EndpointProperty.Url)}");
}
