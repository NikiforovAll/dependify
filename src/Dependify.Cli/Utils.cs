namespace Dependify.Cli;

using Dependify.Cli.Commands.Settings;
using Dependify.Core;
using Microsoft.Extensions.Logging;

internal static class Utils
{


    public static bool ShouldOutputTui(GlobalCommandSettings settings) =>
        (settings.LogLevel is LogLevel.None && settings.Format is OutputFormat.Tui)
        || (!string.IsNullOrWhiteSpace(settings.OutputPath));

    public static T DoSomeWork<T>(Func<StatusContext?, T> func, string message, GlobalCommandSettings settings)
    {
        if (!settings.Interactive!.Value)
        {
            return func(null);
        }

        if (ShouldOutputTui(settings))
        {
            return AnsiConsole
                .Status()
                .AutoRefresh(true)
                .Spinner(Spinner.Known.Dots)
                .Start(
                    message,
                    ctx =>
                    {
                        var result = func(ctx);

                        return result;
                    }
                );
        }

        return func(null);
    }

    public static void SetDiagnosticSource(MsBuildService msBuildService, StatusContext? ctx) =>
        msBuildService.SetDiagnosticSource(
            new(
                project => ctx?.Status($"[yellow]Loading...[/] [grey]{project.Path}[/]"),
                project =>
                {
                    if (ctx is not null)
                    {
                        AnsiConsole.MarkupLine($"[green] Loaded: [/] [grey]{project.ProjectFilePath}[/]");
                    }
                }
            )
        );


}
