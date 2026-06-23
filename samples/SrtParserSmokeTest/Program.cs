using PodcastMetadataGenerator.Core.Models;
using PodcastMetadataGenerator.Core.Services;

var filePath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Console", "mergeconflict498.srt"));

if (!File.Exists(filePath))
{
    Console.Error.WriteLine($"SRT file not found: {filePath}");
    return;
}

var settings = new AppSettings { DefaultSegmentDurationMs = 5000 };
var parser = new TranscriptParser(settings);
var transcript = await parser.ParseAsync(filePath);

Console.WriteLine($"Detected format: {transcript.Format}");
Console.WriteLine($"Segment count: {transcript.Segments.Count}");
Console.WriteLine($"First segment: [{transcript.Segments.First().StartTimeMs}..{transcript.Segments.First().EndTimeMs}] {transcript.Segments.First().Text}");
Console.WriteLine($"Last segment:  [{transcript.Segments.Last().StartTimeMs}..{transcript.Segments.Last().EndTimeMs}] {transcript.Segments.Last().Text}");
