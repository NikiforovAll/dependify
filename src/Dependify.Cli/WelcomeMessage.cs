namespace Dependify.Cli;

using Microsoft.Extensions.Configuration;
using Spectre.Console;

internal static class WelcomeMessage
{
    internal static int Print(IConfiguration configuration)
    {
        if (configuration["NO_LOGO"] == "true")
        {
            return 0;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new FigletText("Dependify").Color(Color.DarkKhaki));
        AnsiConsole.MarkupLine("[bold grey]Handle project dependencies[/]");
        AnsiConsole.WriteLine();

        return 0;
    }
}
