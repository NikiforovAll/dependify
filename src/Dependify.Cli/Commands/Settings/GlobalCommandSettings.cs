namespace Dependify.Cli.Commands.Settings;

using System.ComponentModel;
using Microsoft.Extensions.Logging;

internal class GlobalCommandSettings : CommandSettings
{
    [Description("The output format to use")]
    [CommandOption("--format")]
    public OutputFormat? Format { get; set; } = OutputFormat.Tui;

    [Description("The output path")]
    [CommandOption("-o|--output")]
    public string? OutputPath { get; set; }

    [Description("Log level")]
    [CommandOption("-l|--log-level")]
    public LogLevel? LogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.None;

    [CommandOption("--interactive")]
    public bool? Interactive { get; set; } = true;

    public override ValidationResult Validate()
    {
        if (this.Format!.Value is OutputFormat.Tui && !string.IsNullOrWhiteSpace(this.OutputPath))
        {
            return ValidationResult.Error("Output path is not supported with TUI output format");
        }

        return ValidationResult.Success();
    }
}

internal enum OutputFormat
{
    Tui,
    Json,
    Dot
}
