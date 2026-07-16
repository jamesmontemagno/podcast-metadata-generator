# Merge Conflict Live Demo

## Before The Session

1. Run `copilot auth login` if this machine is not already authenticated.
2. From the repository root, run:

```powershell
dotnet run --project samples/CopilotSdkLiveDemo
```

3. Choose a model and one of the ten latest episodes, then approve `get_merge_conflict_episode` with `y`.

## Live Story

The application asks the presenter which Copilot model and recent Merge Conflict episode to use. It then gives the agent two application-owned capabilities: list the ten newest episodes and retrieve one episode from the official RSS feed. The agent uses the chosen episode's data to create launch copy without inventing details.

## Type These Sections

`Program.cs` contains all console, authentication, selection, streaming, XML parsing, and permission boilerplate. Type only the three marked blocks.

### Part 1: Tool

```csharp
var episodeTool = MergeConflictEpisodeTool.CreateEpisodeTool();
var latestEpisodesTool = MergeConflictEpisodeTool.CreateLatestEpisodesTool();
```

Say: "The model cannot call arbitrary application code. I deliberately expose one typed, read-only capability."

### Part 2: Session

```csharp
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = model,
    Streaming = true,
    Tools = [episodeTool, latestEpisodesTool],
    OnPermissionRequest = RequestPermissionAsync
});
```

Say: "My application owns the session and explicitly grants that capability. The SDK manages the Copilot runtime connection."

### Part 3: Grounded Prompt

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = $"Use get_merge_conflict_episode for episode {selectedEpisodeNumber}. Return a headline and a sponsor-safe post. Use only tool facts."
});
```

Say: "The model decides to call the tool. I approve it, the app retrieves the official feed, and the response is grounded in that returned data."

## Rehearsal

For a repeatable rehearsal, choose episode `523` from the menu. The episode tool still accepts any optional episode number, while the new latest-episodes tool always returns the newest ten feed entries.

Expected console milestones: model selection, episode selection, `[Tool call started]`, approval prompt, `[Tool call complete]`, then streamed launch copy.