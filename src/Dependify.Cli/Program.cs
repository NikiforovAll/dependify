using Dependify.Cli.Formatters;
using Dependify.Core;
using Microsoft.Extensions.Logging;

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
        }
    );

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
