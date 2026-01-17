using System.Text.RegularExpressions;
using GitHub.Copilot.SDK;
using PodcastMetadataGenerator.Models;
using PodcastMetadataGenerator.Prompts;

namespace PodcastMetadataGenerator.Services;

/// <summary>
/// Generates podcast metadata using the GitHub Copilot SDK.
/// </summary>
public partial class MetadataGenerator : IAsyncDisposable
{
    private readonly AppSettings _settings;
    private CopilotClient? _client;
    private bool _isInitialized;
    
    [GeneratedRegex(@"^\d+\.\s*", RegexOptions.Multiline)]
    private static partial Regex NumberedListRegex();
    
    [GeneratedRegex(@"^(\d{1,2}:\d{2}(?::\d{2})?)\s+(.+)$", RegexOptions.Multiline)]
    private static partial Regex ChapterLineRegex();
    
    public MetadataGenerator(AppSettings settings)
    {
        _settings = settings;
    }
    
    /// <summary>
    /// Initializes the Copilot client.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        try
        {
            _client = new CopilotClient();
            await _client.StartAsync();
            _isInitialized = true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to initialize Copilot client: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Generates title suggestions for the podcast episode with streaming support.
    /// </summary>
    public async Task<List<string>> GenerateTitlesAsync(
        Transcript transcript,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        var response = await SendPromptWithStreamingAsync(
            PromptTemplates.TitleSystemPrompt,
            PromptTemplates.GetTitleUserPrompt(transcript.GetFullText(), _settings),
            onChunk,
            cancellationToken);
        
        // Parse numbered list response
        var titles = response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => NumberedListRegex().Replace(line.Trim(), ""))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(_settings.TitleCount)
            .ToList();
        
        return titles;
    }
    
    /// <summary>
    /// Generates a description of the specified length with streaming support.
    /// </summary>
    public async Task<string> GenerateDescriptionAsync(
        Transcript transcript,
        DescriptionLength length,
        string? selectedTitle = null,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        var response = await SendPromptWithStreamingAsync(
            PromptTemplates.DescriptionSystemPrompt,
            PromptTemplates.GetDescriptionUserPrompt(
                transcript.GetFullText(), 
                length, 
                _settings,
                selectedTitle),
            onChunk,
            cancellationToken);
        
        return response.Trim();
    }
    
    /// <summary>
    /// Generates all three description lengths.
    /// </summary>
    public async Task<Dictionary<DescriptionLength, string>> GenerateAllDescriptionsAsync(
        Transcript transcript,
        string? selectedTitle = null,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        var descriptions = new Dictionary<DescriptionLength, string>();
        
        foreach (var length in Enum.GetValues<DescriptionLength>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            descriptions[length] = await GenerateDescriptionAsync(
                transcript, length, selectedTitle, onChunk, cancellationToken);
        }
        
        return descriptions;
    }
    
    /// <summary>
    /// Generates YouTube chapters for the episode with streaming support.
    /// </summary>
    public async Task<List<Chapter>> GenerateChaptersAsync(
        Transcript transcript,
        Action<string>? onChunk = null,
        CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync();
        
        var response = await SendPromptWithStreamingAsync(
            PromptTemplates.ChapterSystemPrompt,
            PromptTemplates.GetChapterUserPrompt(
                transcript.GetTextWithTimestamps(),
                transcript.DurationSeconds,
                _settings),
            onChunk,
            cancellationToken);
        
        // Parse chapter response
        var chapters = new List<Chapter>();
        var matches = ChapterLineRegex().Matches(response);
        
        foreach (Match match in matches)
        {
            var timestamp = NormalizeTimestamp(match.Groups[1].Value);
            var title = match.Groups[2].Value.Trim();
            
            if (!string.IsNullOrEmpty(title))
            {
                chapters.Add(new Chapter
                {
                    Timestamp = timestamp,
                    Title = title
                });
            }
        }
        
        // Ensure first chapter starts at 00:00
        if (chapters.Count > 0 && chapters[0].Timestamp != "00:00" && chapters[0].Timestamp != "00:00:00")
        {
            chapters.Insert(0, new Chapter
            {
                Timestamp = "00:00",
                Title = "Introduction"
            });
        }
        
        return chapters;
    }
    
    private async Task EnsureInitializedAsync()
    {
        if (!_isInitialized || _client == null)
        {
            await InitializeAsync();
        }
    }
    
    private async Task<string> SendPromptWithStreamingAsync(
        string systemPrompt,
        string userPrompt,
        Action<string>? onChunk,
        CancellationToken cancellationToken)
    {
        if (_client == null)
            throw new InvalidOperationException("Client not initialized");
        
        // Sanitize the user prompt to avoid serialization issues
        // Remove or replace any characters that might cause JSON serialization problems
        var sanitizedPrompt = SanitizeForJson(userPrompt);
        
        CopilotSession? session = null;
        try
        {
            // Note: Streaming set to true causes serialization issues with large prompts
            // in some SDK versions, so we use streaming only when an onChunk handler is provided
            session = await _client.CreateSessionAsync(new SessionConfig
            {
                Model = _settings.Model,
                Streaming = true, // Always use streaming for better responsiveness
                SystemMessage = new SystemMessageConfig
                {
                    Mode = SystemMessageMode.Replace,
                    Content = SanitizeForJson(systemPrompt)
                }
            });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to create session (model: {_settings.Model}): {ex.Message}", ex);
        }
        
        await using var _ = session;
        
        var responseBuilder = new System.Text.StringBuilder();
        var done = new TaskCompletionSource();
        
        // Register cancellation
        await using var registration = cancellationToken.Register(() => 
        {
            done.TrySetCanceled(cancellationToken);
        });
        
        session.On(evt =>
        {
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    responseBuilder.Append(delta.Data.DeltaContent);
                    onChunk?.Invoke(delta.Data.DeltaContent ?? "");
                    break;
                case AssistantMessageEvent msg:
                    if (onChunk == null)
                    {
                        responseBuilder.Append(msg.Data.Content);
                    }
                    break;
                case SessionIdleEvent:
                    done.TrySetResult();
                    break;
                case SessionErrorEvent error:
                    done.TrySetException(new Exception($"Session error: {error.Data.Message}"));
                    break;
            }
        });
        
        try
        {
            await session.SendAsync(new MessageOptions { Prompt = sanitizedPrompt });
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send message (prompt length: {sanitizedPrompt.Length} chars): {ex.Message}", ex);
        }
        
        await done.Task;
        
        return responseBuilder.ToString();
    }
    
    /// <summary>
    /// Sanitizes text to avoid JSON serialization issues.
    /// </summary>
    private static string SanitizeForJson(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        // Replace problematic characters that might cause JSON serialization issues
        var result = new System.Text.StringBuilder(text.Length);
        
        foreach (var c in text)
        {
            // Replace control characters (except standard whitespace)
            if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t')
            {
                result.Append(' ');
            }
            // Keep other characters as-is
            else
            {
                result.Append(c);
            }
        }
        
        return result.ToString();
    }
    
    /// <summary>
    /// Normalizes timestamp to MM:SS or HH:MM:SS format.
    /// </summary>
    private static string NormalizeTimestamp(string timestamp)
    {
        var parts = timestamp.Split(':');
        
        return parts.Length switch
        {
            2 => $"{int.Parse(parts[0]):D2}:{int.Parse(parts[1]):D2}",
            3 => $"{int.Parse(parts[0]):D2}:{int.Parse(parts[1]):D2}:{int.Parse(parts[2]):D2}",
            _ => timestamp
        };
    }
    
    public async ValueTask DisposeAsync()
    {
        if (_client != null)
        {
            await _client.DisposeAsync();
            _client = null;
        }
        _isInitialized = false;
        GC.SuppressFinalize(this);
    }
}
