using CopilotSdkLiveDemo.Tools;

namespace CopilotSdkLiveDemo.Helpers;

internal static class EpisodeSelector
{
    internal static EpisodeBrief Pick(IReadOnlyList<EpisodeBrief> episodes)
    {
        if (episodes.Count == 0)
        {
            throw new InvalidOperationException("No Merge Conflict episodes are available to select.");
        }

        Console.WriteLine("\nChoose a Merge Conflict episode:");
        for (var index = 0; index < episodes.Count; index++)
        {
            Console.WriteLine($"  {index + 1}. {episodes[index].Title}");
        }

        var selectedIndex = SelectionPrompt.ReadIndex("Episode [1]: ", episodes.Count, 0);
        return episodes[selectedIndex];
    }
}