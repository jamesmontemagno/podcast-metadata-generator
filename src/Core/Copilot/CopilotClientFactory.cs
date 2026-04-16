using GitHub.Copilot.SDK;

namespace PodcastMetadataGenerator.Core.Copilot;

/// <summary>
/// Creates Copilot SDK clients with deterministic CLI path resolution.
/// </summary>
public static class CopilotClientFactory
{
    /// <summary>
    /// Creates a CopilotClient using an explicit CLI path to avoid incorrect bundled runtime resolution.
    /// </summary>
    public static CopilotClient CreateClient()
    {
        var cliPath = ResolveCliPath();

        return new CopilotClient(new CopilotClientOptions
        {
            CliPath = cliPath
        });
    }

    private static string ResolveCliPath()
    {
        // Allow explicit override while keeping a cross-platform default that resolves from PATH.
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

        return "copilot";
    }
}