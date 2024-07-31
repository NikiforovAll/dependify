namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a PostgreSQL container.
/// </summary>
public class DependifyResource : ContainerResource, IResourceWithEnvironment, IResourceWithServiceDiscovery
{
    internal const string PrimaryEndpointName = "http";

    /// <summary>
    /// Initializes a new instance of the <see cref="DependifyResource"/> class.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    public DependifyResource(string name)
        : base(name)
    {
        this.PrimaryEndpoint = new(this, PrimaryEndpointName);
    }

    /// <summary>
    /// Gets the primary endpoint for the Dependify server.
    /// </summary>
    public EndpointReference PrimaryEndpoint { get; }
}
