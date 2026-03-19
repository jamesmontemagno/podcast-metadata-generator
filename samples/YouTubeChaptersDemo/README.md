# YouTube Chapters Demo

Small, self-contained demo for a short video.

What it shows:
- Start a Copilot SDK session
- Listen to streaming events
- Send one prompt
- Generate YouTube chapters from an SRT transcript
- Auto-approve permission requests to keep the flow simple

## Run

From repo root:

```powershell
dotnet run --project samples/YouTubeChaptersDemo -- src/Console/mergeconflict498.srt
```

If Copilot is not authenticated:

```powershell
copilot auth login
```
