using System.Text.Json;
using PodcastMetadataGenerator.Models;

namespace PodcastMetadataGenerator.Services;

/// <summary>
/// Handles saving generated outputs to files.
/// </summary>
public class OutputService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    
    /// <summary>
    /// Saves all generated outputs to the specified directory.
    /// </summary>
    public async Task<SaveResult> SaveAllAsync(
        string outputDirectory,
        Transcript transcript,
        GenerationResult result,
        AppSettings settings)
    {
        Directory.CreateDirectory(outputDirectory);
        
        var savedFiles = new List<string>();
        var errors = new List<string>();
        
        // Save titles
        if (result.Titles.Count > 0)
        {
            var titlesPath = Path.Combine(outputDirectory, "titles.txt");
            try
            {
                var titlesContent = string.Join(Environment.NewLine, 
                    result.Titles.Select((t, i) => $"{i + 1}. {t}"));
                if (!string.IsNullOrEmpty(result.SelectedTitle))
                {
                    titlesContent = $"Selected: {result.SelectedTitle}\n\n{titlesContent}";
                }
                await File.WriteAllTextAsync(titlesPath, titlesContent);
                savedFiles.Add(titlesPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to save titles: {ex.Message}");
            }
        }
        
        // Save descriptions
        foreach (var (length, description) in result.Descriptions)
        {
            var descPath = Path.Combine(outputDirectory, $"description-{length.ToString().ToLower()}.txt");
            try
            {
                await File.WriteAllTextAsync(descPath, description);
                savedFiles.Add(descPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to save {length} description: {ex.Message}");
            }
        }
        
        // Save chapters
        if (result.Chapters.Count > 0)
        {
            var chaptersPath = Path.Combine(outputDirectory, "chapters.txt");
            try
            {
                var srtConverter = new SrtConverter();
                var chaptersContent = srtConverter.FormatChaptersForYouTube(result.Chapters);
                await File.WriteAllTextAsync(chaptersPath, chaptersContent);
                savedFiles.Add(chaptersPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to save chapters: {ex.Message}");
            }
        }
        
        // Save SRT
        string? srtPath = null;
        if (!string.IsNullOrEmpty(result.SrtContent))
        {
            srtPath = Path.Combine(outputDirectory, 
                Path.GetFileNameWithoutExtension(transcript.FilePath) + ".srt");
            try
            {
                await File.WriteAllTextAsync(srtPath, result.SrtContent);
                savedFiles.Add(srtPath);
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to save SRT: {ex.Message}");
                srtPath = null;
            }
        }
        
        // Save manifest
        var manifest = new Manifest
        {
            TranscriptPath = transcript.FilePath,
            GeneratedAt = DateTime.UtcNow,
            Model = settings.Model,
            EpisodeContext = settings.EpisodeContext,
            Titles = result.Titles,
            SelectedTitle = result.SelectedTitle,
            Descriptions = new ManifestDescriptions
            {
                Short = result.Descriptions.GetValueOrDefault(DescriptionLength.Short),
                Medium = result.Descriptions.GetValueOrDefault(DescriptionLength.Medium),
                Long = result.Descriptions.GetValueOrDefault(DescriptionLength.Long)
            },
            Chapters = result.Chapters.Select(c => new ManifestChapter
            {
                Timestamp = c.Timestamp,
                Title = c.Title,
                Summary = c.Summary
            }).ToList(),
            SrtPath = srtPath,
            DurationSeconds = transcript.DurationSeconds
        };
        
        var manifestPath = Path.Combine(outputDirectory, "manifest.json");
        try
        {
            var manifestJson = JsonSerializer.Serialize(manifest, JsonOptions);
            await File.WriteAllTextAsync(manifestPath, manifestJson);
            savedFiles.Add(manifestPath);
        }
        catch (Exception ex)
        {
            errors.Add($"Failed to save manifest: {ex.Message}");
        }
        
        return new SaveResult(savedFiles, errors);
    }
    
    public record SaveResult(List<string> SavedFiles, List<string> Errors);
}
