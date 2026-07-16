# Copilot SDK Live Demo

A small .NET console demonstration of a GitHub Copilot SDK session calling a typed application tool.

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
- `CopilotTool.DefineTool` to expose a host-owned .NET capability.
- A host-controlled approval prompt before the tool executes.
- Tool output grounded in a public, current RSS source.

The live tool defaults to the latest episode and accepts an optional episode number for repeatable rehearsals. See [LIVE_DEMO.md](LIVE_DEMO.md) for the short stage typing script.