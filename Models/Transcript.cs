namespace PodcastMetadataGenerator.Models;

/// <summary>
/// Detected format of the transcript file.
/// </summary>
public enum TranscriptFormat
{
    Unknown,
    Zencastr,
    TimeRange,
    Srt
}

/// <summary>
/// Represents a parsed transcript with all its segments.
/// </summary>
public class Transcript
{
    /// <summary>
    /// The original file path of the transcript.
    /// </summary>
    public required string FilePath { get; init; }
    
    /// <summary>
    /// The detected format of the transcript.
    /// </summary>
    public TranscriptFormat Format { get; init; }
    
    /// <summary>
    /// All segments in the transcript.
    /// </summary>
    public List<TranscriptSegment> Segments { get; init; } = [];
    
    /// <summary>
    /// Total duration in milliseconds based on the last segment's end time.
    /// </summary>
    public long DurationMs => Segments.Count > 0 ? Segments[^1].EndTimeMs : 0;
    
    /// <summary>
    /// Total duration in seconds.
    /// </summary>
    public double DurationSeconds => DurationMs / 1000.0;
    
    /// <summary>
    /// Total duration in minutes.
    /// </summary>
    public double DurationMinutes => DurationSeconds / 60.0;
    
    /// <summary>
    /// Gets the full transcript text with speaker labels.
    /// </summary>
    public string GetFullText()
    {
        return string.Join("\n\n", Segments.Select(s => 
            string.IsNullOrEmpty(s.Speaker) 
                ? s.Text 
                : $"{s.Speaker}: {s.Text}"));
    }
    
    /// <summary>
    /// Gets the full transcript with timestamps for chapter generation.
    /// </summary>
    public string GetTextWithTimestamps()
    {
        return string.Join("\n", Segments.Select(s =>
            $"[{s.StartTimeYouTube} - {FormatTimeForYouTube(s.EndTimeMs)}] " +
            (string.IsNullOrEmpty(s.Speaker) ? s.Text : $"{s.Speaker}: {s.Text}")));
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
