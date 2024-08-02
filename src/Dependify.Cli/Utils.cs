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

    public static void SubscribeToLoadingEvents(this IObservable<NodeEvent> observable, StatusContext? ctx) =>
        observable.Subscribe(node =>
        {
            switch (node.EventType)
            {
                case NodeEventType.ProjectLoading:
                    ctx?.Status($"[yellow]Loading...[/] [grey]{node.Path}[/]");
                    break;
                case NodeEventType.SolutionLoading:
                    ctx?.Status($"[yellow]Loading...[/] [grey]{node.Path}[/]");
                    break;
                case NodeEventType.ProjectLoaded:
                    AnsiConsole.MarkupLine($"[green] Loaded: [/] [grey]{node.Path}[/]");
                    break;
                case NodeEventType.SolutionLoaded:
                    AnsiConsole.MarkupLine($"[green] Loaded: [/] [grey]{node.Path}[/]");
                    break;
                default:
                    break;
            }
        });
}
