using System.Diagnostics;

namespace PodcastMetadataGenerator.Core.Services;

/// <summary>
/// Checks for Copilot CLI installation and authentication status.
/// </summary>
public class CopilotAuthService
{
    /// <summary>
    /// Result of the Copilot readiness check.
    /// </summary>
    public record CopilotStatus(
        bool IsInstalled,
        bool IsTokenSet,
        bool IsAuthenticated,
        string? ErrorMessage);
    
    /// <summary>
    /// Checks if Copilot CLI is ready to use.
    /// Returns detailed status information.
    /// </summary>
    public async Task<CopilotStatus> CheckStatusAsync()
    {
        // Check GH_TOKEN environment variable
        var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        var isTokenSet = !string.IsNullOrEmpty(ghToken);
        
        // Check if copilot CLI is installed
        var isInstalled = await CheckCopilotInstalledAsync();
        
        if (!isInstalled)
        {
            return new CopilotStatus(
                IsInstalled: false,
                IsTokenSet: isTokenSet,
                IsAuthenticated: false,
                ErrorMessage: "Copilot CLI is not installed. Install via: npm install -g @github/copilot or brew install copilot");
        }
        
        // Check authentication status
        var authResult = await CheckCopilotAuthAsync();
        
        return new CopilotStatus(
            IsInstalled: true,
            IsTokenSet: isTokenSet,
            IsAuthenticated: authResult.isAuthenticated,
            ErrorMessage: authResult.error);
    }
    
    /// <summary>
    /// Quick check if Copilot is ready (installed + authenticated).
    /// </summary>
    public async Task<bool> IsReadyAsync()
    {
        var status = await CheckStatusAsync();
        return status.IsInstalled && (status.IsTokenSet || status.IsAuthenticated);
    }
    
    private static async Task<bool> CheckCopilotInstalledAsync()
    {
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
            await process.WaitForExitAsync();
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
    
    private static async Task<(bool isAuthenticated, string? error)> CheckCopilotAuthAsync()
    {
        // If GH_TOKEN is set, assume authenticated
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GH_TOKEN")))
        {
            return (true, null);
        }
        
        try
        {
            // Try running a minimal copilot command to check auth
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "copilot",
                Arguments = "--help",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            process.Start();
            var stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            // Check for authentication errors in stderr
            if (stderr.Contains("not authenticated", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                stderr.Contains("login required", StringComparison.OrdinalIgnoreCase))
            {
                return (false, "Not authenticated. Run 'copilot auth login' or set GH_TOKEN environment variable.");
            }
            
            // If --help works without auth errors, consider it ready
            return process.ExitCode == 0 ? (true, null) : (false, "Copilot CLI returned an error.");
        }
        catch (Exception ex)
        {
            return (false, $"Failed to check authentication: {ex.Message}");
        }
    }
}
