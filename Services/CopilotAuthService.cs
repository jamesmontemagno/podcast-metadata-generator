using System.Diagnostics;
using Spectre.Console;

namespace PodcastMetadataGenerator.Services;

/// <summary>
/// Handles GitHub Copilot CLI authentication checks and setup guidance.
/// </summary>
public class CopilotAuthService
{
    /// <summary>
    /// Result of the Copilot CLI check.
    /// </summary>
    public record CopilotCheckResult(bool IsInstalled, bool HasToken, string? Version, string? Error);
    
    /// <summary>
    /// Checks if GitHub Copilot CLI is installed and potentially authenticated.
    /// </summary>
    public async Task<CopilotCheckResult> CheckCopilotAsync()
    {
        // Check for environment token (programmatic auth)
        var hasToken = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GH_TOKEN")) ||
                       !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GITHUB_TOKEN"));
        
        // Check if copilot CLI is installed
        try
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "copilot",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            process.Start();
            var version = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode == 0)
            {
                return new CopilotCheckResult(true, hasToken, version.Trim(), null);
            }
            
            var error = await process.StandardError.ReadToEndAsync();
            return new CopilotCheckResult(false, hasToken, null, error.Trim());
        }
        catch (Exception ex)
        {
            return new CopilotCheckResult(false, hasToken, null, ex.Message);
        }
    }
    
    /// <summary>
    /// Displays installation and authentication instructions.
    /// </summary>
    public void ShowSetupInstructions(CopilotCheckResult result)
    {
        if (!result.IsInstalled)
        {
            ShowInstallationInstructions();
        }
        else if (!result.HasToken)
        {
            ShowAuthenticationInstructions(result.Version);
        }
    }
    
    private void ShowInstallationInstructions()
    {
        AnsiConsole.WriteLine();
        
        var panel = new Panel(
            new Markup("""
                [red]GitHub Copilot CLI is not installed or not in PATH.[/]
                
                [bold]Installation Options:[/]
                
                [blue]macOS (Homebrew):[/]
                  [grey]brew install gh[/]
                  [grey]gh extension install github/gh-copilot[/]
                
                [blue]npm (requires Node.js):[/]
                  [grey]npm install -g @githubnext/github-copilot-cli[/]
                
                [blue]Or download from:[/]
                  [link]https://github.com/github/copilot-cli[/]
                
                After installation, run [yellow]copilot[/] and use [yellow]/login[/] to authenticate.
                """))
        {
            Header = new PanelHeader("[bold red] Copilot CLI Not Found [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Red),
            Padding = new Padding(2, 1)
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
    
    private void ShowAuthenticationInstructions(string? version)
    {
        AnsiConsole.WriteLine();
        
        var versionText = !string.IsNullOrEmpty(version) ? $"[green]✓[/] Copilot CLI installed: [grey]{Markup.Escape(version)}[/]\n\n" : "";
        
        var panel = new Panel(
            new Markup($$"""
                {{versionText}}[yellow]No authentication token detected.[/]
                
                [bold]Authentication Options:[/]
                
                [blue]Interactive (recommended):[/]
                  1. Run [grey]copilot[/] in your terminal
                  2. Type [grey]/login[/] and follow the prompts
                  3. Restart this application
                
                [blue]Environment Variable:[/]
                  Set [grey]GH_TOKEN[/] or [grey]GITHUB_TOKEN[/] with a GitHub PAT
                  that has [grey]Copilot[/] permissions.
                
                [dim]Note: The SDK may still work if you've authenticated
                interactively before. Try continuing![/]
                """))
        {
            Header = new PanelHeader("[bold yellow] Authentication Recommended [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow),
            Padding = new Padding(2, 1)
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
    
    /// <summary>
    /// Performs the full check and prompts user if there are issues.
    /// Returns true if we should continue, false to exit.
    /// </summary>
    public async Task<bool> EnsureCopilotReadyAsync()
    {
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Checking GitHub Copilot CLI...", async ctx =>
            {
                return await CheckCopilotAsync();
            });
        
        if (result.IsInstalled && result.HasToken)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] GitHub Copilot CLI ready [grey]({Markup.Escape(result.Version ?? "unknown version")})[/]");
            return true;
        }
        
        if (!result.IsInstalled)
        {
            ShowSetupInstructions(result);
            return false;
        }
        
        // Installed but no token - show warning but allow continuing
        ShowSetupInstructions(result);
        
        return AnsiConsole.Confirm("Continue anyway?", defaultValue: true);
    }
}
