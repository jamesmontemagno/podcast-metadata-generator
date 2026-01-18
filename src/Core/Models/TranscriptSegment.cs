namespace PodcastMetadataGenerator.Core.Models;

/// <summary>
/// Represents a single segment of a transcript with timestamp, speaker, and text.
/// </summary>
public record TranscriptSegment
{
    /// <summary>
    /// Start time of the segment in total milliseconds.
    /// </summary>
    public long StartTimeMs { get; init; }
    
    /// <summary>
    /// End time of the segment in total milliseconds.
    /// </summary>
    public long EndTimeMs { get; init; }
    
    /// <summary>
    /// Speaker name, if available.
    /// </summary>
    public string? Speaker { get; init; }
    
    /// <summary>
    /// The transcript text for this segment.
    /// </summary>
    public required string Text { get; init; }
    
    /// <summary>
    /// Gets the start time formatted as HH:MM:SS,mmm for SRT.
    /// </summary>
    public string StartTimeSrt => FormatTimeForSrt(StartTimeMs);
    
    /// <summary>
    /// Gets the end time formatted as HH:MM:SS,mmm for SRT.
    /// </summary>
    public string EndTimeSrt => FormatTimeForSrt(EndTimeMs);
    
    /// <summary>
    /// Gets the start time formatted as HH:MM:SS for YouTube chapters.
    /// </summary>
    public string StartTimeYouTube => FormatTimeForYouTube(StartTimeMs);
    
    private static string FormatTimeForSrt(long totalMs)
    {
        var hours = totalMs / 3600000;
        var minutes = (totalMs % 3600000) / 60000;
        var seconds = (totalMs % 60000) / 1000;
        var milliseconds = totalMs % 1000;
        
        return $"{hours:D2}:{minutes:D2}:{seconds:D2},{milliseconds:D3}";
    }
    
    private static string FormatTimeForYouTube(long totalMs)
    {
        var hours = totalMs / 3600000;
        var minutes = (totalMs % 3600000) / 60000;
        var seconds = (totalMs % 60000) / 1000;
        
        if (hours > 0)
            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        return $"{minutes:D2}:{seconds:D2}";
    }
}
