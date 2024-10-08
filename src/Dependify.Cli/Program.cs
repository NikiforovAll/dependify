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
                .WithDescription("Scans for projects and solutions and retrives their dependencies")
                .WithExample("graph", "scan", "./path/to/folder", "--framework", "net8");

            c.AddCommand<ShowCommand>("show")
                .WithDescription("Shows the dependencies of a project or solution located in the specified path")
                .WithExample("graph", "show", "./path/to/project", "--framework", "net8");
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

return app.Run(args);

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
