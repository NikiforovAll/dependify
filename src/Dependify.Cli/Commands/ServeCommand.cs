namespace Dependify.Cli.Commands;

using System.Threading;
using Dependify.Cli.Commands.Settings;
using Dependify.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal class ServeCommand() : AsyncCommand<ServeCommandSettings>
{
    private const string Port = "9999";
    private const string Host = $"http://localhost:{Port}";

    public override async Task<int> ExecuteAsync(CommandContext context, ServeCommandSettings settings)
    {
        var isLoggingEnabled = settings.LogLevel.HasValue && settings.LogLevel.Value != LogLevel.None;

        var directory = Path.GetDirectoryName($"{settings.Path.TrimEnd('/')}/").NormalizePath();

        if (!Directory.Exists(directory))
        {
            AnsiConsole.MarkupLine($"[red]The specified path does not exist: {directory}[/]");

            return 1;
        }

        await Web.Program.Run(
            webBuilder: builder =>
            {
                builder
                    .WebHost.UseSetting(WebHostDefaults.ApplicationKey, "Dependify.Cli")
                    .UseSetting(WebHostDefaults.HttpPortsKey, Port);

                builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.GetFullPath(directory)));
                builder.Services.AddSingleton<FileProviderProjectLocator>();
                builder.Services.AddTransient<MsBuildService>();
                builder.Services.AddSingleton<SolutionRegistry>();

                builder.Services.AddHostedService(sp => new SolutionRegistryService(
                    sp.GetRequiredService<SolutionRegistry>(),
                    sp.GetRequiredService<IOptions<MsBuildConfig>>(),
                    new MsBuildServiceListener(
                        project => { },
                        project =>
                        {
                            if (!isLoggingEnabled)
                            {
                                AnsiConsole.MarkupLine($"[green] Loaded: [/] [grey]{project.ProjectFilePath}[/]");
                            }
                        }
                    ),
                    isLoggingEnabled
                ));

                builder.Services.Configure<MsBuildConfig>(config =>
                {
                    config.IncludePackages = true;
                    config.FullScan = true;
                    config.Framework = settings.Framework;
                });

                builder.Services.AddLogging(l =>
                {
                    l.ClearProviders().AddDebug();

                    if (isLoggingEnabled)
                    {
                        l.SetMinimumLevel(settings.LogLevel!.Value);
                        l.AddSimpleConsole();
                    }
                });
            },
            webApp: app =>
            {
                app.Lifetime.ApplicationStarted.Register(() =>
                {
                    if (!isLoggingEnabled)
                    {
                        AnsiConsole.Write(new FigletText("Dependify").LeftJustified().Color(Color.Olive));
                        AnsiConsole.MarkupLine(
                            $"{Environment.NewLine}{Environment.NewLine}Now listening on: [olive]{Host}[/]{Environment.NewLine}{Environment.NewLine}"
                        );
                        AnsiConsole.MarkupLine(
                            $"Serving files from: [green]{directory}[/]{Environment.NewLine}{Environment.NewLine}"
                        );
                        AnsiConsole.MarkupLine(
                            $"Press [yellow]Ctrl+C[/] to stop the server{Environment.NewLine}{Environment.NewLine}"
                        );
                    }
                });

                app.Lifetime.ApplicationStopped.Register(() =>
                {
                    if (!isLoggingEnabled)
                    {
                        AnsiConsole.WriteLine();

                        var rule = new Rule("End of session...") { Style = Style.Parse("olive dim") };
                        AnsiConsole.Write(rule);
                    }
                });
            }
        );

        return 0;
    }
}

internal class ServeCommandSettings : BaseAnalyzeCommandSettings { }

internal class SolutionRegistryService(
    SolutionRegistry solutionRegistry,
    IOptions<MsBuildConfig> msBuildConfig,
    MsBuildServiceListener? listener,
    bool isLoggingEnabled
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(() =>
            {
                solutionRegistry.SetBuildServiceDiagnosticSource(listener);
                solutionRegistry.LoadRegistry();
                solutionRegistry.LoadSolutionsAsync(msBuildConfig.Value);
            })
            .ContinueWith(
                _ =>
                {
                    if (!isLoggingEnabled)
                    {
                        AnsiConsole.MarkupLine(
                            $"{Environment.NewLine}[olive]Loaded[/] [grey]{solutionRegistry.Solutions.Count()}[/] [olive]solutions[/]{Environment.NewLine}"
                        );
                    }
                },
                stoppingToken
            );

        return Task.CompletedTask;
    }
}
