namespace Dependify.Cli.Commands;

using Dependify.Cli.Commands.Settings;

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
    [CommandArgument(0, "<path>")]
    public string? Path { get; set; }
}
