namespace Dependify.Cli.Commands;

internal class GenerateDependenciesCommand : Command<GenerateCommandSettings>
{
    public override int Execute(CommandContext context, GenerateCommandSettings settings)
    {
        // Omitted
        return 0;
    }
}

internal class GenerateCommandSettings : GlobalCommandSettings
{
    [CommandArgument(0, "[PROJECT|SOLUTION]")]
    public string Source { get; set; } = ".";
}
