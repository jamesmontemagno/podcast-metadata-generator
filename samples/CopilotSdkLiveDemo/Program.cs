using CopilotSdkLiveDemo.Helpers;
using CopilotSdkLiveDemo.Tools;
using GitHub.Copilot;

Console.WriteLine("Merge Conflict launch assistant\n");

await using var client = new CopilotClient();
await client.StartAsync();

var authStatus = await client.GetAuthStatusAsync();
if (!authStatus.IsAuthenticated)
{
    Console.WriteLine("Copilot is not authenticated. Run 'copilot auth login' and try again.");
    return;
}

var model = await ModelSelector.PickAsync(client, "gpt-5.4-mini");
if (string.IsNullOrWhiteSpace(model))
{
    Console.WriteLine("No Copilot models are available in this environment.");
    return;
}

var latestEpisodes = await MergeConflictEpisodeTool.GetLatestAsync();
var selectedEpisode = EpisodeSelector.Pick(latestEpisodes);
var selectedEpisodeNumber = selectedEpisode.EpisodeNumber
    ?? throw new InvalidOperationException("The selected episode does not have an episode number.");

var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

// Part 1: Give the agent a capability owned by this application.
var episodeTool = MergeConflictEpisodeTool.CreateEpisodeTool();
var latestEpisodesTool = MergeConflictEpisodeTool.CreateLatestEpisodesTool();

// Part 2: Create the agent session.
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = model,
    Streaming = true,
    Tools = [episodeTool, latestEpisodesTool],
    OnPermissionRequest = PermissionPrompt.RequestAsync,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Replace,
        Content = "You are the launch assistant for the Merge Conflict podcast. Use supplied episode facts only."
    }
});

session.On<SessionEvent>(evt =>
{
    switch (evt)
    {
        case AssistantMessageDeltaEvent delta:
            Console.Write(delta.Data.DeltaContent);
            break;
        case ToolExecutionStartEvent:
            Console.WriteLine("\n[Tool call started]");
            break;
        case ToolExecutionCompleteEvent:
            Console.WriteLine("\n[Tool call complete]\n");
            break;
        case SessionErrorEvent error:
            complete.TrySetException(new InvalidOperationException(error.Data.Message));
            break;
        case SessionIdleEvent:
            complete.TrySetResult();
            break;
    }
});

Console.WriteLine($"\nUsing model: {model}");
Console.WriteLine($"Selected episode: {selectedEpisode.Title}\n");

// Part 3: Ask the agent to use its new capability.
Console.WriteLine("Streaming launch copy...\n");
await session.SendAsync(new MessageOptions
{
    Prompt = $"Use get_merge_conflict_episode for episode {selectedEpisodeNumber}. Return exactly a social headline and a sponsor-safe post under 280 characters. Use only facts returned by the tool; do not invent guests, sponsors, topics, or links."
});
await complete.Task;
