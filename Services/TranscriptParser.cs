using System.Text.RegularExpressions;
using PodcastMetadataGenerator.Models;

namespace PodcastMetadataGenerator.Services;

/// <summary>
/// Parses transcript files in various formats into a unified Transcript model.
/// </summary>
public partial class TranscriptParser
{
    private readonly AppSettings _settings;
    
    // Zencastr format: MM:SS.ss or HH:MM:SS.ss (centiseconds)
    [GeneratedRegex(@"^(\d{1,2}):(\d{2})(?::(\d{2}))?\.(\d{2})$", RegexOptions.Compiled)]
    private static partial Regex ZencastrTimestampRegex();
    
    // Time range format: HH:MM:SS - HH:MM:SS or with arrows
    [GeneratedRegex(@"(\d{1,2}):(\d{2}):(\d{2})\s*[-–>]+\s*(\d{1,2}):(\d{2}):(\d{2})", RegexOptions.Compiled)]
    private static partial Regex TimeRangeRegex();
    
    // SRT timestamp format: HH:MM:SS,mmm --> HH:MM:SS,mmm
    [GeneratedRegex(@"(\d{2}):(\d{2}):(\d{2}),(\d{3})\s*-->\s*(\d{2}):(\d{2}):(\d{2}),(\d{3})", RegexOptions.Compiled)]
    private static partial Regex SrtTimestampRegex();
    
    // Speaker detection: short line, capitalized, no punctuation at start
    [GeneratedRegex(@"^[A-Z][a-zA-Z\s]{0,30}$", RegexOptions.Compiled)]
    private static partial Regex SpeakerRegex();
    
    public TranscriptParser(AppSettings settings)
    {
        _settings = settings;
    }
    
    /// <summary>
    /// Parses a transcript file and returns a Transcript object.
    /// </summary>
    public async Task<Transcript> ParseAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Transcript file not found: {filePath}");
        
        var lines = await File.ReadAllLinesAsync(filePath);
        var format = DetectFormat(lines);
        
        var segments = format switch
        {
            TranscriptFormat.Zencastr => ParseZencastr(lines),
            TranscriptFormat.TimeRange => ParseTimeRange(lines),
            TranscriptFormat.Srt => ParseSrt(lines),
            _ => throw new InvalidOperationException($"Unable to detect transcript format for: {filePath}")
        };
        
        return new Transcript
        {
            FilePath = filePath,
            Format = format,
            Segments = segments
        };
    }
    
    /// <summary>
    /// Detects the format of the transcript by scanning the first 50 lines.
    /// </summary>
    private TranscriptFormat DetectFormat(string[] lines)
    {
        var linesToCheck = lines.Take(50).ToArray();
        
        // Check for SRT format first (most specific)
        if (linesToCheck.Any(l => SrtTimestampRegex().IsMatch(l)))
            return TranscriptFormat.Srt;
        
        // Check for Zencastr format
        if (linesToCheck.Any(l => ZencastrTimestampRegex().IsMatch(l.Trim())))
            return TranscriptFormat.Zencastr;
        
        // Check for time range format
        if (linesToCheck.Any(l => TimeRangeRegex().IsMatch(l)))
            return TranscriptFormat.TimeRange;
        
        return TranscriptFormat.Unknown;
    }
    
    /// <summary>
    /// Parses Zencastr format transcripts.
    /// Format:
    /// 00:00.29
    /// James
    /// Welcome back everyone...
    /// </summary>
    private List<TranscriptSegment> ParseZencastr(string[] lines)
    {
        var segments = new List<TranscriptSegment>();
        var i = 0;
        
        while (i < lines.Length)
        {
            var line = lines[i].Trim();
            
            // Look for timestamp
            var match = ZencastrTimestampRegex().Match(line);
            if (!match.Success)
            {
                i++;
                continue;
            }
            
            var startTimeMs = ParseZencastrTimestamp(match);
            i++;
            
            // Next line might be speaker
            string? speaker = null;
            if (i < lines.Length)
            {
                var potentialSpeaker = lines[i].Trim();
                if (SpeakerRegex().IsMatch(potentialSpeaker) && potentialSpeaker.Length < 30)
                {
                    speaker = potentialSpeaker;
                    i++;
                }
            }
            
            // Collect text until next timestamp or empty line
            var textLines = new List<string>();
            while (i < lines.Length)
            {
                var textLine = lines[i].Trim();
                if (string.IsNullOrEmpty(textLine) || ZencastrTimestampRegex().IsMatch(textLine))
                    break;
                
                // Skip if it looks like a speaker line for the next segment
                if (SpeakerRegex().IsMatch(textLine) && textLine.Length < 30 && 
                    i + 1 < lines.Length && !string.IsNullOrEmpty(lines[i + 1].Trim()) &&
                    !ZencastrTimestampRegex().IsMatch(lines[i + 1].Trim()))
                {
                    // This is actually text, not a speaker
                    textLines.Add(textLine);
                }
                else if (SpeakerRegex().IsMatch(textLine) && textLine.Length < 30)
                {
                    break;
                }
                else
                {
                    textLines.Add(textLine);
                }
                i++;
            }
            
            if (textLines.Count > 0)
            {
                var text = string.Join(" ", textLines);
                var endTimeMs = startTimeMs + _settings.DefaultSegmentDurationMs;
                
                segments.Add(new TranscriptSegment
                {
                    StartTimeMs = startTimeMs,
                    EndTimeMs = endTimeMs,
                    Speaker = speaker,
                    Text = text
                });
            }
        }
        
        // Adjust end times based on next segment's start time
        for (var j = 0; j < segments.Count - 1; j++)
        {
            var current = segments[j];
            var next = segments[j + 1];
            
            segments[j] = current with { EndTimeMs = next.StartTimeMs };
        }
        
        return segments;
    }
    
    /// <summary>
    /// Parses time range format transcripts.
    /// Format: 00:00:00 - 00:00:05 Welcome to the podcast
    /// </summary>
    private List<TranscriptSegment> ParseTimeRange(string[] lines)
    {
        var segments = new List<TranscriptSegment>();
        
        foreach (var line in lines)
        {
            var match = TimeRangeRegex().Match(line);
            if (!match.Success) continue;
            
            var startTimeMs = ParseTimeRangeTimestamp(
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value));
            
            var endTimeMs = ParseTimeRangeTimestamp(
                int.Parse(match.Groups[4].Value),
                int.Parse(match.Groups[5].Value),
                int.Parse(match.Groups[6].Value));
            
            // Text is everything after the timestamp
            var text = line[(match.Index + match.Length)..].Trim();
            
            // Try to extract speaker from text (Speaker: text)
            string? speaker = null;
            var colonIndex = text.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 30)
            {
                var potentialSpeaker = text[..colonIndex];
                if (SpeakerRegex().IsMatch(potentialSpeaker))
                {
                    speaker = potentialSpeaker;
                    text = text[(colonIndex + 1)..].Trim();
                }
            }
            
            if (!string.IsNullOrEmpty(text))
            {
                segments.Add(new TranscriptSegment
                {
                    StartTimeMs = startTimeMs,
                    EndTimeMs = endTimeMs,
                    Speaker = speaker,
                    Text = text
                });
            }
        }
        
        return segments;
    }
    
    /// <summary>
    /// Parses SRT format transcripts.
    /// </summary>
    private List<TranscriptSegment> ParseSrt(string[] lines)
    {
        var segments = new List<TranscriptSegment>();
        var i = 0;
        
        while (i < lines.Length)
        {
            // Skip sequence number
            if (int.TryParse(lines[i].Trim(), out _))
            {
                i++;
                if (i >= lines.Length) break;
            }
            
            // Look for timestamp line
            var match = SrtTimestampRegex().Match(lines[i]);
            if (!match.Success)
            {
                i++;
                continue;
            }
            
            var startTimeMs = ParseSrtTimestamp(
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value),
                int.Parse(match.Groups[3].Value),
                int.Parse(match.Groups[4].Value));
            
            var endTimeMs = ParseSrtTimestamp(
                int.Parse(match.Groups[5].Value),
                int.Parse(match.Groups[6].Value),
                int.Parse(match.Groups[7].Value),
                int.Parse(match.Groups[8].Value));
            
            i++;
            
            // Collect text lines until blank line
            var textLines = new List<string>();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                textLines.Add(lines[i]);
                i++;
            }
            
            var text = string.Join(" ", textLines);
            
            // Try to extract speaker from text
            string? speaker = null;
            var colonIndex = text.IndexOf(':');
            if (colonIndex > 0 && colonIndex < 30)
            {
                var potentialSpeaker = text[..colonIndex];
                if (SpeakerRegex().IsMatch(potentialSpeaker))
                {
                    speaker = potentialSpeaker;
                    text = text[(colonIndex + 1)..].Trim();
                }
            }
            
            if (!string.IsNullOrEmpty(text))
            {
                segments.Add(new TranscriptSegment
                {
                    StartTimeMs = startTimeMs,
                    EndTimeMs = endTimeMs,
                    Speaker = speaker,
                    Text = text
                });
            }
            
            i++; // Skip blank line
        }
        
        return segments;
    }
    
    private static long ParseZencastrTimestamp(Match match)
    {
        // Groups: 1=first number, 2=second number, 3=optional third number, 4=centiseconds
        var g1 = int.Parse(match.Groups[1].Value);
        var g2 = int.Parse(match.Groups[2].Value);
        var g3 = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : -1;
        var centiseconds = int.Parse(match.Groups[4].Value);
        
        int hours, minutes, seconds;
        if (g3 >= 0)
        {
            // HH:MM:SS.cc format
            hours = g1;
            minutes = g2;
            seconds = g3;
        }
        else
        {
            // MM:SS.cc format
            hours = 0;
            minutes = g1;
            seconds = g2;
        }
        
        return (hours * 3600000L) + (minutes * 60000L) + (seconds * 1000L) + (centiseconds * 10L);
    }
    
    private static long ParseTimeRangeTimestamp(int hours, int minutes, int seconds)
    {
        return (hours * 3600000L) + (minutes * 60000L) + (seconds * 1000L);
    }
    
    private static long ParseSrtTimestamp(int hours, int minutes, int seconds, int milliseconds)
    {
        return (hours * 3600000L) + (minutes * 60000L) + (seconds * 1000L) + milliseconds;
    }
}
