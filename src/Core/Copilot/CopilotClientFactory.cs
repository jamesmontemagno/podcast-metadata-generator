using GitHub.Copilot.SDK;

namespace PodcastMetadataGenerator.Core.Copilot;

/// <summary>
/// Creates Copilot SDK clients.
/// </summary>
public static class CopilotClientFactory
{
    /// <summary>
    /// Creates a CopilotClient, using the SDK-bundled CLI unless an explicit CLI path is configured.
    /// </summary>
    public static CopilotClient CreateClient()
    {
        var cliPath = ResolveCliPath();

        if (string.IsNullOrWhiteSpace(cliPath))
        {
            return new CopilotClient();
        }

        return new CopilotClient(new CopilotClientOptions
        {
            CliPath = cliPath
        });
    }

    private static string ResolveCliPath()
    {
        var explicitPath = Environment.GetEnvironmentVariable("COPILOT_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }

        var legacyPath = Environment.GetEnvironmentVariable("GITHUB_COPILOT_CLI_PATH");
        if (!string.IsNullOrWhiteSpace(legacyPath))
        {
            return legacyPath;
        }

        return string.Empty;
    }
}