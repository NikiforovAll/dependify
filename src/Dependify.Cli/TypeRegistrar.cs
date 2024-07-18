namespace Dependify.Cli;

using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

internal sealed class TypeRegistrar(IServiceCollection builder) : ITypeRegistrar
{
    private readonly IServiceCollection builder = builder;

    public ITypeResolver Build() => new TypeResolver(this.builder.BuildServiceProvider());

    public void Register(Type service, Type implementation) => this.builder.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) =>
        this.builder.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        this.builder.AddSingleton(service, _ => factory());
    }
}

internal sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
{
    private readonly IServiceProvider provider = provider ?? throw new ArgumentNullException(nameof(provider));

    public object? Resolve(Type? type) => type == null ? null : this.provider.GetService(type);

    public void Dispose()
    {
        if (this.provider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
