namespace Dependify.Cli.Formatters;

using Dependify.Cli.Commands.Settings;

internal class FormatterFactory
{
    public IOutputFormatter Create(GlobalCommandSettings settings)
    {
        var writer = SelectOutputWriter(settings);

        return settings.Format switch
        {
            OutputFormat.Json => new JsonOutputFormatter(writer),
            OutputFormat.Dot => new DotOutputFormatter(writer),
            _ => throw new NotImplementedException(),
        };
    }

    private static TextWriter SelectOutputWriter(GlobalCommandSettings settings) =>
        string.IsNullOrWhiteSpace(settings.OutputPath) ? Console.Out : new StreamWriter(settings.OutputPath);
}
