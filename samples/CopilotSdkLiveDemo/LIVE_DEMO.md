# Merge Conflict Live Demo

## Before The Session

1. Run `copilot auth login` if this machine is not already authenticated.
2. From the repository root, run:

```powershell
dotnet run --project samples/CopilotSdkLiveDemo
```

## Act One: Hello World

Start with [Program.cs](Program.cs). It deliberately has named placeholders for `client`, `isAuthenticated`, and `session`. Leave the streaming event handler and completion wait in place.

### 1. Start The Client

At `CopilotClient client;`, type:

```csharp
client = new CopilotClient();
await client.StartAsync();
```

Say: "The client is my connection to the Copilot runtime. I start it explicitly, so the application owns its lifecycle."

### 2. Check Authentication

Replace `var isAuthenticated = false;` with:

```csharp
var isAuthenticated = (await client.GetAuthStatusAsync()).IsAuthenticated;
```

Say: "Before creating a session, I can ask the runtime whether this machine is signed in."

### 3. Create The Session

At `CopilotSession session = null!;`, type:

```csharp
session = await client.CreateSessionAsync(new SessionConfig
{
    Model = Model,
    Streaming = true
});
```

Say: "The session is the conversation. I chose the model, enabled streaming, and the event handler below already prints each text fragment as it arrives."

### 4. Send Hello World

Under `// Send the first message.`, type:

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = "Hello world! In one sentence, say what the Copilot SDK helps a .NET app do."
});
```

Say: "That is the basic shape: start a client, create a session, listen for events, and send a message."

Expected output: a streamed one-sentence answer, followed by the existing `SessionIdleEvent` completing the program.

## Act Two: Turn It Into A Podcast Assistant

After Hello World, add the prewritten helpers in `Helpers` and `Tools` to turn the same session into a grounded podcast workflow.

### 1. Let The Presenter Choose

Replace the fixed `Model` use with a picker, then load and select one of the ten newest episodes:

```csharp
var model = await ModelSelector.PickAsync(client, "gpt-5.4-mini");
var latestEpisodes = await MergeConflictEpisodeTool.GetLatestAsync();
var selectedEpisode = EpisodeSelector.Pick(latestEpisodes);
var selectedEpisodeNumber = selectedEpisode.EpisodeNumber;
```

Say: "This keeps the demo live. I can choose a model in the room, then choose from the real ten newest Merge Conflict episodes."

### 2. Give The Session Capabilities

Create the application-owned tools:

```csharp
var episodeTool = MergeConflictEpisodeTool.CreateEpisodeTool();
var latestEpisodesTool = MergeConflictEpisodeTool.CreateLatestEpisodesTool();
```

Extend `SessionConfig` with:

```csharp
Model = model,
Tools = [episodeTool, latestEpisodesTool],
OnPermissionRequest = PermissionPrompt.RequestAsync,
SystemMessage = new SystemMessageConfig
{
    Mode = SystemMessageMode.Replace,
    Content = "You are the launch assistant for the Merge Conflict podcast. Use supplied episode facts only."
}
```

Say: "The model does not get arbitrary access to my application. I grant two narrow, typed capabilities and remain the approval point before a tool executes."

### 3. Replace The Prompt

Replace Hello World with the selected, grounded episode request:

```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = $"Use get_merge_conflict_episode for episode {selectedEpisodeNumber}. Return exactly a social headline and a sponsor-safe post under 280 characters. Use only facts returned by the tool; do not invent guests, sponsors, topics, or links."
});
```

Say: "The agent decides to call the episode tool, I approve the read-only lookup, and its response is grounded in the official feed rather than invented details."

Expected milestones: model selection, ten-episode selection, `[Tool call started]`, approval prompt, `[Tool call complete]`, then streamed launch copy.