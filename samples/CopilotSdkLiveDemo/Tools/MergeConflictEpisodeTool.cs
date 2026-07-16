using System.ComponentModel;
using System.Text;
using System.Xml.Linq;
using GitHub.Copilot;
using Microsoft.Extensions.AI;

namespace CopilotSdkLiveDemo.Tools;

internal static class MergeConflictEpisodeTool
{
    private const string FeedUrl = "https://feeds.fireside.fm/mergeconflict/rss";

    internal static AIFunction CreateEpisodeTool() =>
        CopilotTool.DefineTool(
            ([Description("Optional Merge Conflict episode number. Omit for the latest episode.")] int? episodeNumber) =>
                GetAsync(episodeNumber),
            factoryOptions: new AIFunctionFactoryOptions
            {
                Name = "get_merge_conflict_episode",
                Description = "Gets a Merge Conflict episode from the official RSS feed."
            });

    internal static AIFunction CreateLatestEpisodesTool() =>
        CopilotTool.DefineTool(
            () => GetLatestAsync(),
            factoryOptions: new AIFunctionFactoryOptions
            {
                Name = "get_latest_merge_conflict_episodes",
                Description = "Gets the ten newest Merge Conflict episodes from the official RSS feed."
            });

    internal static async Task<IReadOnlyList<EpisodeBrief>> GetLatestAsync()
    {
        var items = await GetItemsAsync();
        return items.Take(10).Select(ToEpisodeBrief).ToList();
    }

    private static async Task<EpisodeBrief> GetAsync(int? episodeNumber)
    {
        var items = await GetItemsAsync();

        var item = episodeNumber is null
            ? items.FirstOrDefault()
            : items.FirstOrDefault(candidate => GetEpisodeNumber(candidate) == episodeNumber);

        if (item is null)
        {
            throw new InvalidOperationException(episodeNumber is null
                ? "The Merge Conflict RSS feed contained no episodes."
                : $"Episode {episodeNumber} was not found in the Merge Conflict RSS feed.");
        }

            return ToEpisodeBrief(item);
        }

        private static async Task<List<XElement>> GetItemsAsync()
        {
            using var httpClient = new HttpClient();
            var feed = await httpClient.GetStringAsync(FeedUrl);
            var document = XDocument.Parse(feed);
            return document.Descendants("item").ToList();
        }

        private static EpisodeBrief ToEpisodeBrief(XElement item) =>
            new(
            GetEpisodeNumber(item),
            Value(item, "title"),
            Value(item, "pubDate"),
            item.Elements().FirstOrDefault(element => element.Name.LocalName == "duration")?.Value ?? "Unknown",
            StripHtml(Value(item, "description")),
            Value(item, "link"),
            FeedUrl);

    private static int? GetEpisodeNumber(XElement item)
    {
        var title = Value(item, "title");
        var separator = title.IndexOf(':');
        return separator > 0 && int.TryParse(title[..separator], out var episodeNumber)
            ? episodeNumber
            : null;
    }

    private static string Value(XElement item, string name) => item.Element(name)?.Value.Trim() ?? string.Empty;

    private static string StripHtml(string value)
    {
        var builder = new StringBuilder(value.Length);
        var insideTag = false;

        foreach (var character in value)
        {
            if (character == '<')
            {
                insideTag = true;
            }
            else if (character == '>')
            {
                insideTag = false;
            }
            else if (!insideTag)
            {
                builder.Append(character);
            }
        }

        return System.Net.WebUtility.HtmlDecode(builder.ToString()).Trim();
    }
}

internal sealed record EpisodeBrief(
    int? EpisodeNumber,
    string Title,
    string Published,
    string Duration,
    string Description,
    string EpisodeUrl,
    string SourceFeedUrl);