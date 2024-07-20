namespace Dependify.Cli.Formatters;

internal interface IOutputFormatter : IDisposable
{
    void Write<T>(T data);
}
