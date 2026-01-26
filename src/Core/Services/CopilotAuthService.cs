using System.Diagnostics;
using GitHub.Copilot.SDK;

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
        string? ErrorMessage,
        string? AuthType = null,
        string? Host = null,
        string? Login = null);
    
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
        
        // Check authentication status using SDK
        var authResult = await CheckCopilotAuthAsync();
        
        return new CopilotStatus(
            IsInstalled: true,
            IsTokenSet: isTokenSet,
            IsAuthenticated: authResult.isAuthenticated,
            ErrorMessage: authResult.error,
            AuthType: authResult.authType,
            Host: authResult.host,
            Login: authResult.login);
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
    
    private static async Task<(bool isAuthenticated, string? error, string? authType, string? host, string? login)> CheckCopilotAuthAsync()
    {
        CopilotClient? client = null;
        try
        {
            client = new CopilotClient();
            await client.StartAsync();
            
            var authResponse = await client.GetAuthStatusAsync();
            
            if (!authResponse.IsAuthenticated)
            {
                var statusMsg = authResponse.StatusMessage ?? "Not authenticated";
                return (false, $"{statusMsg}. Run 'copilot auth login' or set GH_TOKEN environment variable.", null, null, null);
            }
            
            return (true, null, authResponse.AuthType, authResponse.Host, authResponse.Login);
        }
        catch (Exception ex)
        {
            return (false, $"Failed to check authentication: {ex.Message}", null, null, null);
        }
        finally
        {
            if (client != null)
            {
                await client.DisposeAsync();
            }
        }
    }
}
