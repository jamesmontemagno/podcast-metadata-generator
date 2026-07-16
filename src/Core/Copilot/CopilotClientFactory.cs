using GitHub.Copilot;
using System.Runtime.InteropServices;

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
            Connection = RuntimeConnection.ForStdio(path: cliPath)
        });
    }

    private static string ResolveCliPath()
    {
        var explicitPath = TryGetValidatedPath(Environment.GetEnvironmentVariable("COPILOT_CLI_PATH"));
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            return explicitPath;
        }

        var legacyPath = TryGetValidatedPath(Environment.GetEnvironmentVariable("GITHUB_COPILOT_CLI_PATH"));
        if (!string.IsNullOrWhiteSpace(legacyPath))
        {
            return legacyPath;
        }

        var pathCandidate = FindExecutableOnPath();
        if (!string.IsNullOrWhiteSpace(pathCandidate))
        {
            return pathCandidate;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var winGetLinkPath = TryGetValidatedPath(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Microsoft",
                "WinGet",
                "Links",
                "copilot.exe"));

            if (!string.IsNullOrWhiteSpace(winGetLinkPath))
            {
                return winGetLinkPath;
            }
        }

        return string.Empty;
    }

    private static string? TryGetValidatedPath(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return null;
        }

        var trimmed = candidate.Trim();
        return File.Exists(trimmed) ? trimmed : null;
    }

    private static string? FindExecutableOnPath()
    {
        var path = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var pathExts = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? GetPathExtensions()
            : [string.Empty];

        foreach (var dir in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var ext in pathExts)
            {
                var candidate = TryGetValidatedPath(Path.Combine(dir, $"copilot{ext}"));
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private static string[] GetPathExtensions()
    {
        var pathExt = Environment.GetEnvironmentVariable("PATHEXT");
        if (string.IsNullOrWhiteSpace(pathExt))
        {
            return [".exe", ".cmd", ".bat"];
        }

        return pathExt
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ext => ext.StartsWith('.') ? ext : $".{ext}")
            .ToArray();
    }
}
