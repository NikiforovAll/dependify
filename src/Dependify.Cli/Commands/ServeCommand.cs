namespace Dependify.Cli.Commands;

using System.Reactive.Linq;
using System.Threading;
using Dependify.Cli.Commands.Settings;
using Dependify.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Web;

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

        await Program.Run(
            webBuilder: builder =>
            {
                builder
                    .WebHost.UseSetting(WebHostDefaults.ApplicationKey, "Dependify.Cli")
                    .UseSetting(WebHostDefaults.HttpPortsKey, Port);

                builder.Services.AddSingleton<IFileProvider>(new PhysicalFileProvider(Path.GetFullPath(directory)));
                builder.Services.AddSingleton<FileProviderProjectLocator>();
                builder.Services.AddTransient<MsBuildService>();
                builder.Services.AddSingleton<SolutionRegistry>();

                var aiOptions = builder.Configuration.GetSection("DEPENDIFY:AI").Get<OpenAIOptions>() ?? new();

                if (!string.IsNullOrWhiteSpace(settings.DeploymentName))
                {
                    aiOptions.DeploymentName = settings.DeploymentName;
                }
                if (!string.IsNullOrWhiteSpace(settings.AIEndpoint))
                {
                    aiOptions.Endpoint = settings.AIEndpoint;
                }
                if (!string.IsNullOrWhiteSpace(settings.AIApiKey))
                {
                    aiOptions.ApiKey = settings.AIApiKey;
                }
                if (!string.IsNullOrWhiteSpace(settings.ModelId))
                {
                    aiOptions.ModelId = settings.ModelId;
                }

                if (aiOptions is { IsEnabled: true })
                {
                    var kernelBuilder = builder.Services.AddKernel();

                    if (aiOptions.IsAzureOpenAI)
                    {
                        kernelBuilder.AddAzureOpenAIChatCompletion(
                            deploymentName: aiOptions!.DeploymentName,
                            endpoint: aiOptions.Endpoint,
                            apiKey: aiOptions.ApiKey
                        );
                    }
                    else
                    {
                        kernelBuilder.AddOpenAIChatCompletion(
                            aiOptions!.ModelId,
                            new Uri(aiOptions.Endpoint),
                            aiOptions.ApiKey
                        );
                    }
                }

                builder.Services.Configure<OpenAIOptions>(config =>
                {
                    config.DeploymentName = aiOptions.DeploymentName;
                    config.Endpoint = aiOptions.Endpoint;
                    config.ApiKey = aiOptions.ApiKey;
                    config.ModelId = aiOptions.ModelId;
                });

                builder.Services.AddHostedService(sp => new SolutionRegistryService(
                    sp.GetRequiredService<SolutionRegistry>(),
                    sp.GetRequiredService<IOptions<MsBuildConfig>>(),
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

internal class ServeCommandSettings : BaseAnalyzeCommandSettings
{
    [CommandOption("--endpoint")]
    public string AIEndpoint { get; set; } = default!;

    [CommandOption("--deployment-name")]
    public string DeploymentName { get; set; } = default!;

    [CommandOption("--model-id")]
    public string ModelId { get; set; } = default!;

    [CommandOption("--api-key")]
    public string AIApiKey { get; set; } = default!;
}

internal class SolutionRegistryService(
    SolutionRegistry solutionRegistry,
    IOptions<MsBuildConfig> msBuildConfig,
    bool isLoggingEnabled
) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Task.Run(
                () =>
                {
                    if (!isLoggingEnabled)
                    {
                        solutionRegistry.OnLoadingEvents.SubscribeToLoadingEvents(default!);
                    }

                    solutionRegistry.LoadRegistry();
                    solutionRegistry.LoadSolutionsAsync(msBuildConfig.Value);
                },
                stoppingToken
            )
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
