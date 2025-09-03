using Dependify.Cli.Formatters;
using Dependify.Core;

var app = new CommandApp(ConfigureServices(out var configuration));

app.Configure(config =>
{
    config.AddBranch(
        "graph",
        c =>
        {
            c.AddCommand<ScanCommand>("scan")
                .WithDescription(
                    "Recursively scans for all projects and solutions, providing a high-level overview with dependency statistics. Use for analyzing entire repositories or getting an overview of multiple solutions."
                )
                .WithExample("graph", "scan", "./path/to/folder", "--framework", "net8", "--full-scan")
                .WithExample("graph", "scan", "./path/to/folder", "--include-packages", "--exclude-sln");

            c.AddCommand<ShowCommand>("show")
                .WithDescription(
                    "Shows detailed dependency tree visualization for a single project or solution. Use for deep-diving into specific dependency chains."
                )
                .WithExample("graph", "show", "./path/to/project", "--framework", "net8", "--display", "tree")
                .WithExample("graph", "show", "./path/to/solution", "--display", "box");
        }
    );

    config.AddCommand<ServeCommand>("serve");

#if DEBUG
    config.PropagateExceptions();
    config.ValidateExamples();
#endif
});

if (args.Length == 0)
{
    WelcomeMessage.Print(configuration);
}

var exitCode = app.Run(args);

return exitCode;

TypeRegistrar ConfigureServices(out IConfiguration configuration)
{
    var services = new ServiceCollection();

    using var configurationManager = new ConfigurationManager();

    var logLevelArg = args.Contains("--log-level")
        ? args.SkipWhile(a => a is not "--log-level").Skip(1).FirstOrDefault()
        : LogLevel.None.ToString();

    if (!Enum.TryParse<LogLevel>(logLevelArg, out var logLevel))
    {
        logLevel = LogLevel.None;
    }

    configuration = configurationManager.AddEnvironmentVariables("DEPENDIFY_").Build();

    services.AddSingleton<IConfiguration>(configuration);
    services.AddSingleton<ProjectLocator>();
    services.AddScoped<MsBuildService>();
    services.AddSingleton<FormatterFactory>();

    services.AddLogging(builder => builder.AddSimpleConsole().SetMinimumLevel(logLevel));

    return new TypeRegistrar(services);
}
