namespace Dependify.Cli.Commands.Settings;

using System.ComponentModel;

internal class BaseAnalyzeCommandSettings : GlobalCommandSettings
{
    [CommandArgument(0, "<path>")]
    public string Path { get; set; } = default!;

    [Description("Framework RTF version")]
    [CommandOption("-f|--framework")]
    public string Framework { get; set; } = default!;

    [CommandOption("--include-packages")]
    public bool? IncludePackages { get; set; } = false;
}
