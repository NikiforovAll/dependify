var app = new CommandApp(ConfigureServices(out var configuration));

app.Configure(config =>
{
    config.AddBranch(
        "graph",
        c =>
            c.AddCommand<GenerateDependenciesCommand>("generate")
                .WithDescription("Generates a plan for the dependencies of a project or solution.")
                .WithExample("graph", "generate", "./path/to/project")
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

static TypeRegistrar ConfigureServices(out IConfiguration configuration)
{
    var services = new ServiceCollection();

    using var configurationManager = new ConfigurationManager();

    configuration = configurationManager.AddEnvironmentVariables("DEPENDIFY_").Build();

    services.AddSingleton<IConfiguration>(configuration);

    return new TypeRegistrar(services);
}
