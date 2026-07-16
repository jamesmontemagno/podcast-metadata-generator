# Copilot SDK Live Demo

A small .NET console demonstration that begins with a streaming GitHub Copilot SDK Hello World, then expands into typed application tools.

The application fetches an episode from the official [Merge Conflict RSS feed](https://feeds.fireside.fm/mergeconflict/rss). Copilot receives structured source data and creates a concise, sponsor-safe social post without relying on invented episode facts.

## Run

```powershell
dotnet run --project samples/CopilotSdkLiveDemo
```

The .NET SDK bundles its compatible Copilot runtime. Authenticate first when needed:

```powershell
copilot auth login
```

For local Debug runs, the repository uses the WinGet-installed `copilot.exe` when the SDK cannot download its bundled runtime because your network blocks npm. For another CLI installation, set `COPILOT_CLI_BINARY_PATH` to its native executable before running:

```powershell
$env:COPILOT_CLI_BINARY_PATH = (Get-Command copilot.exe).Source
dotnet run --project samples/CopilotSdkLiveDemo
```

## What It Demonstrates

- `CopilotClient` runtime startup and authentication.
- A streaming `CopilotSession`.
- A minimal Hello World prompt and response.
- `CopilotTool.DefineTool` to expose a host-owned .NET capability.
- A host-controlled approval prompt before the tool executes.
- Tool output grounded in a public, current RSS source.

The expanded demo asks the presenter to choose a model and one of the ten latest episodes. See [LIVE_DEMO.md](LIVE_DEMO.md) for the short stage typing script.