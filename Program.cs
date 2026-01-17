using Spectre.Console;
using PodcastMetadataGenerator.Services;
using PodcastMetadataGenerator.UI;

// Show header
ConsoleUI.ShowHeader();

// Check Copilot CLI authentication
var authService = new CopilotAuthService();
var isReady = await authService.EnsureCopilotReadyAsync();

if (!isReady)
{
    AnsiConsole.MarkupLine("[red]Exiting due to missing Copilot CLI.[/]");
    return 1;
}

AnsiConsole.WriteLine();

// Run the application workflow
var workflow = new AppWorkflow();
await workflow.RunAsync(args);

return 0;
