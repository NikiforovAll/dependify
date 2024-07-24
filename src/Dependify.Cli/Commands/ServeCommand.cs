namespace Dependify.Cli.Commands;

using Dependify.Cli.Commands.Settings;
using Microsoft.Extensions.Logging;

internal class ServeCommand() : AsyncCommand<ServeCommandSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ServeCommandSettings settings)
    {
        var isLoggingEnabled = settings.LogLevel.HasValue && settings.LogLevel.Value != LogLevel.None;

        var taskRun = Web.Program.Run(
            new WebApplicationOptions() { },
            webBuilder: builder =>
                builder.Services.AddLogging(l =>
                {
                    l.ClearProviders().AddDebug();

                    if (isLoggingEnabled)
                    {
                        l.SetMinimumLevel(settings.LogLevel!.Value);
                        l.AddSimpleConsole();
                    }
                })
        );

        if (!isLoggingEnabled)
        {
            AnsiConsole.Write(new FigletText("Dependify").LeftJustified().Color(Color.Olive));
            AnsiConsole.MarkupLine(
                $"{Environment.NewLine}{Environment.NewLine}Now listening on: [olive]http://localhost:5000[/]{Environment.NewLine}{Environment.NewLine}"
            );
            AnsiConsole.MarkupLine("Press [green]Ctrl+C[/] to stop the server");
        }

        await taskRun;

        return 0;
    }
}

internal class ServeCommandSettings : BaseAnalyzeCommandSettings
{
    [CommandOption("--full-scan")]
    public bool? FullScan { get; set; } = false;

    [CommandOption("--exclude-sln")]
    public bool? ExcludeSln { get; set; } = false;
}
