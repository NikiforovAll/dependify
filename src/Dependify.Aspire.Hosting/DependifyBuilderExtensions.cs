namespace Aspire.Hosting;

using Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for adding Dependify resources to an <see cref="IDistributedApplicationBuilder"/>.
/// </summary>
public static class DependifyBuilderExtensions
{
    private const int DefaultContainerPort = 9999;

    /// <summary>
    /// Adds a Dependify resource to the application model. A container is used for local development.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="serveFrom"></param>
    /// <param name="tag"></param>
    /// <param name="port">The host port used when launching the container.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<DependifyResource> AddDependify(
        this IDistributedApplicationBuilder builder,
        string name = "dependify",
        string? tag = null,
        int? port = null
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        var server = new DependifyResource(name);

        return builder
            .AddResource(server)
            .WithHttpEndpoint(
                port: port ?? DefaultContainerPort,
                targetPort: DefaultContainerPort,
                name: DependifyResource.PrimaryEndpointName
            )
            .WithImage(DependifyContainerImageTags.Image, tag ?? DependifyContainerImageTags.Tag)
            .WithImageRegistry(DependifyContainerImageTags.Registry)
            .WithAnnotation(
                new CommandLineArgsCallbackAnnotation(args =>
                {
                    args.Clear();

                    args.Add("serve");
                    args.Add("/workspace/");
                    args.Add("--log-level");
                    args.Add("Debug");
                })
            );
    }

    public static IResourceBuilder<DependifyResource> ServeFrom(
        this IResourceBuilder<DependifyResource> builder,
        string serveFrom,
        bool isReadOnly = true
    )
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithBindMount(serveFrom, "/workspace/", isReadOnly);
    }
}

internal static class DependifyContainerImageTags
{
    public const string Registry = "ghcr.io";
    public const string Image = "nikiforovall/dependify";
    public const string Tag = "main";
}
