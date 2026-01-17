using System.Text.Json;
using System.Text.Json.Serialization;
using PodcastMetadataGenerator.Models;

namespace PodcastMetadataGenerator.Services;

/// <summary>
/// Handles loading and saving user settings to disk.
/// </summary>
public class SettingsService
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "podcast-metadata-generator");
    
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "settings.json");
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    /// <summary>
    /// Loads settings from disk, or returns defaults if no settings file exists.
    /// </summary>
    public async Task<AppSettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();
            
            var json = await File.ReadAllTextAsync(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
            return settings ?? new AppSettings();
        }
        catch
        {
            // If settings are corrupted, return defaults
            return new AppSettings();
        }
    }
    
    /// <summary>
    /// Saves settings to disk.
    /// </summary>
    public async Task SaveAsync(AppSettings settings)
    {
        try
        {
            // Ensure directory exists
            Directory.CreateDirectory(SettingsDirectory);
            
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(SettingsFilePath, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }
    
    /// <summary>
    /// Gets the path to the settings file for display purposes.
    /// </summary>
    public static string GetSettingsPath() => SettingsFilePath;
}
