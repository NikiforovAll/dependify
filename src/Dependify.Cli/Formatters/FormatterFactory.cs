namespace Dependify.Cli.Formatters;

using Dependify.Cli.Commands.Settings;

internal sealed class FormatterFactory
{
    public IOutputFormatter Create(GlobalCommandSettings settings)
    {
#pragma warning disable CA2000 // Dispose objects before losing scope
        var writer = SelectOutputWriter(settings);
#pragma warning restore CA2000 // Dispose objects before losing scope

        return settings.Format switch
        {
            OutputFormat.Json => new JsonOutputFormatter(writer),
            OutputFormat.Dot => new DotOutputFormatter(writer),
            OutputFormat.Mermaid => new MermaidOutputFormatter(writer),
            _ => throw new NotImplementedException(),
        };
    }

    private static TextWriter SelectOutputWriter(GlobalCommandSettings settings) =>
        string.IsNullOrWhiteSpace(settings.OutputPath) ? Console.Out : new StreamWriter(settings.OutputPath);
}
