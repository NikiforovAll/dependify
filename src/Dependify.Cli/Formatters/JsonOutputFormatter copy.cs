namespace Dependify.Cli.Formatters;

using Dependify.Core.Graph;
using Dependify.Core.Serializers;

internal class MermaidOutputFormatter(TextWriter textWriter) : IOutputFormatter
{
    public void Dispose() => textWriter.Dispose();

    public void Write<T>(T data)
    {
        textWriter.WriteLine(MermaidSerializer.ToString(data as DependencyGraph));

        textWriter.Flush();
    }
}
