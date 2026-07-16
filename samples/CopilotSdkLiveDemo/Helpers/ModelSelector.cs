using GitHub.Copilot;

namespace CopilotSdkLiveDemo.Helpers;

internal static class ModelSelector
{
    internal static async Task<string> PickAsync(CopilotClient client, string preferredModel)
    {
        var models = (await client.ListModelsAsync()).ToList();
        if (models.Count == 0)
        {
            return string.Empty;
        }

        var defaultIndex = models.FindIndex(model =>
            string.Equals(model.Id, preferredModel, StringComparison.OrdinalIgnoreCase));
        defaultIndex = defaultIndex >= 0 ? defaultIndex : 0;

        Console.WriteLine("Choose a Copilot model:");
        for (var index = 0; index < models.Count; index++)
        {
            Console.WriteLine($"  {index + 1}. {models[index].Id}");
        }

        var selectedIndex = SelectionPrompt.ReadIndex(
            $"Model [{defaultIndex + 1}]: ",
            models.Count,
            defaultIndex);

        return models[selectedIndex].Id;
    }
}