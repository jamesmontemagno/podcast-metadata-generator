using System.Diagnostics;

namespace PodcastMetadataGenerator.Models;

/// <summary>
/// Available AI models for generation from GitHub Copilot CLI.
/// </summary>
public static class AvailableModels
{
    // Models from Copilot CLI --model choices (run `copilot --help` to see current list)
    // GPT Models
    public const string Gpt5 = "gpt-5";
    public const string Gpt51 = "gpt-5.1";
    public const string Gpt52 = "gpt-5.2";
    public const string Gpt5Mini = "gpt-5-mini";
    public const string Gpt51Codex = "gpt-5.1-codex";
    public const string Gpt51CodexMax = "gpt-5.1-codex-max";
    public const string Gpt51CodexMini = "gpt-5.1-codex-mini";
    public const string Gpt52Codex = "gpt-5.2-codex";
    public const string Gpt41 = "gpt-4.1";
    
    // Claude Models
    public const string ClaudeSonnet45 = "claude-sonnet-4.5";
    public const string ClaudeHaiku45 = "claude-haiku-4.5";
    public const string ClaudeOpus45 = "claude-opus-4.5";
    public const string ClaudeSonnet4 = "claude-sonnet-4";
    
    // Gemini Models
    public const string Gemini3ProPreview = "gemini-3-pro-preview";
    
    /// <summary>
    /// Default hardcoded list of models. Use GetModelsAsync() to fetch from CLI.
    /// </summary>
    public static readonly string[] All =
    [
        // Fast/Default models first
        Gpt5,
        Gpt51,
        Gpt5Mini,
        ClaudeSonnet45,
        ClaudeHaiku45,
        // More powerful models
        Gpt52,
        Gpt51Codex,
        Gpt51CodexMax,
        Gpt52Codex,
        ClaudeOpus45,
        ClaudeSonnet4,
        Gpt41,
        Gpt51CodexMini,
        Gemini3ProPreview
    ];
    
    public static string Default => Gpt5;
    
    /// <summary>
    /// Fetches the list of available models from the Copilot CLI.
    /// Falls back to the hardcoded list if the CLI is unavailable.
    /// </summary>
    public static async Task<string[]> GetModelsFromCliAsync()
    {
        try
        {
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
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();
            
            if (process.ExitCode != 0)
                return All;
            
            // Parse the --model line from help output
            // Format: --model <model>  Set the AI model to use (choices: "model1", "model2", ...)
            var modelLine = output.Split('\n')
                .FirstOrDefault(line => line.Contains("--model") && line.Contains("choices:"));
            
            if (modelLine == null)
                return All;
            
            // Extract the choices part
            var choicesStart = modelLine.IndexOf("choices:", StringComparison.OrdinalIgnoreCase);
            if (choicesStart < 0)
                return All;
            
            var choicesText = modelLine[(choicesStart + 8)..];
            
            // Parse quoted model names
            var models = new List<string>();
            var inQuote = false;
            var current = new System.Text.StringBuilder();
            
            foreach (var c in choicesText)
            {
                if (c == '"')
                {
                    if (inQuote && current.Length > 0)
                    {
                        models.Add(current.ToString());
                        current.Clear();
                    }
                    inQuote = !inQuote;
                }
                else if (inQuote)
                {
                    current.Append(c);
                }
            }
            
            return models.Count > 0 ? [.. models] : All;
        }
        catch
        {
            return All;
        }
    }
}

/// <summary>
/// Application settings and user preferences.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Selected AI model for generation.
    /// </summary>
    public string Model { get; set; } = AvailableModels.Default;
    
    /// <summary>
    /// Output directory for generated files.
    /// </summary>
    public string OutputDirectory { get; set; } = Environment.CurrentDirectory;
    
    /// <summary>
    /// Optional episode context provided by the user.
    /// </summary>
    public string? EpisodeContext { get; set; }
    
    /// <summary>
    /// Default duration to add if end time is not available (in ms).
    /// </summary>
    public long DefaultSegmentDurationMs { get; set; } = 5000;
}
