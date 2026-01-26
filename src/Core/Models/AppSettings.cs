using System.Diagnostics;
using GitHub.Copilot.SDK;

namespace PodcastMetadataGenerator.Core.Models;

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
    /// Fetches the list of available models from the Copilot SDK.
    /// Falls back to the hardcoded list if the SDK is unavailable.
    /// </summary>
    public static async Task<string[]> GetModelsFromCliAsync()
    {
        try
        {
            var models = await GetModelsWithMetadataAsync();
            return models.Select(m => m.Id).ToArray();
        }
        catch
        {
            return All;
        }
    }
    
    /// <summary>
    /// Fetches the list of available models with full metadata from the Copilot SDK.
    /// Falls back to a basic list if the SDK is unavailable.
    /// </summary>
    public static async Task<List<ModelInfo>> GetModelsWithMetadataAsync()
    {
        CopilotClient? client = null;
        try
        {
            client = new CopilotClient();
            await client.StartAsync();
            
            var models = await client.ListModelsAsync();
            
            // Return models if we got any
            if (models.Count > 0)
            {
                return models;
            }
            
            // Fall back to creating basic ModelInfo from hardcoded list
            return All.Select(id => new ModelInfo 
            { 
                Id = id, 
                Name = id 
            }).ToList();
        }
        catch
        {
            // Fall back to creating basic ModelInfo from hardcoded list
            return All.Select(id => new ModelInfo 
            { 
                Id = id, 
                Name = id 
            }).ToList();
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

/// <summary>
/// Application settings and user preferences.
/// Persisted to disk via SettingsService.
/// </summary>
public class AppSettings
{
    #region AI Settings
    
    /// <summary>
    /// Selected AI model for generation.
    /// </summary>
    public string Model { get; set; } = AvailableModels.Default;
    
    #endregion
    
    #region Output Settings
    
    /// <summary>
    /// Output directory for generated files.
    /// </summary>
    public string OutputDirectory { get; set; } = Environment.CurrentDirectory;
    
    #endregion
    
    #region Title Settings
    
    /// <summary>
    /// Number of title suggestions to generate.
    /// </summary>
    public int TitleCount { get; set; } = 5;
    
    /// <summary>
    /// Maximum words per title.
    /// </summary>
    public int TitleMaxWords { get; set; } = 10;
    
    #endregion
    
    #region Description Settings
    
    /// <summary>
    /// Word count target for short descriptions.
    /// </summary>
    public int ShortDescriptionWords { get; set; } = 50;
    
    /// <summary>
    /// Word count target for medium descriptions.
    /// </summary>
    public int MediumDescriptionWords { get; set; } = 150;
    
    /// <summary>
    /// Word count target for long descriptions.
    /// </summary>
    public int LongDescriptionWords { get; set; } = 300;
    
    #endregion
    
    #region Chapter Settings
    
    /// <summary>
    /// Minimum number of chapters to generate.
    /// </summary>
    public int MinChapters { get; set; } = 3;
    
    /// <summary>
    /// Maximum number of chapters to generate.
    /// </summary>
    public int MaxChapters { get; set; } = 12;
    
    /// <summary>
    /// Target chapters per 30 minutes of content.
    /// </summary>
    public int ChaptersPer30Min { get; set; } = 5;
    
    /// <summary>
    /// Maximum words per chapter title.
    /// </summary>
    public int ChapterTitleMaxWords { get; set; } = 8;
    
    #endregion
    
    #region Episode Context
    
    /// <summary>
    /// Optional episode context provided by the user (guest names, topics, etc.).
    /// This is per-session and not persisted.
    /// </summary>
    public string? EpisodeContext { get; set; }
    
    /// <summary>
    /// Default podcast name (persisted for reuse).
    /// </summary>
    public string? PodcastName { get; set; }
    
    /// <summary>
    /// Default host name(s) (persisted for reuse).
    /// </summary>
    public string? HostNames { get; set; }
    
    #endregion
    
    #region Parser Settings
    
    /// <summary>
    /// Default duration to add if end time is not available (in ms).
    /// </summary>
    public long DefaultSegmentDurationMs { get; set; } = 5000;
    
    #endregion
    
    #region Prompt Settings
    
    /// <summary>
    /// Whether to prompt for episode context when loading a transcript.
    /// </summary>
    public bool PromptForContextOnLoad { get; set; } = true;
    
    #endregion
    
    /// <summary>
    /// Calculates the target number of chapters based on episode duration.
    /// </summary>
    public int CalculateTargetChapters(double durationMinutes)
    {
        var target = (int)(durationMinutes / 30.0 * ChaptersPer30Min);
        return Math.Max(MinChapters, Math.Min(MaxChapters, target));
    }
}
