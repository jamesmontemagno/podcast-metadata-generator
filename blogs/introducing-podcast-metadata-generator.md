# Introducing Podcast Metadata Generator: AI-Powered Show Notes from Your Transcripts

Hey friends! I'm excited to share a new tool I built that solves a problem I face every single week: generating all the metadata for my podcast episodes. If you've ever had to write titles, descriptions, chapters, and create SRT subtitles for an episode, you know it's tedious work. Enter [Podcast Metadata Generator](https://github.com/jamesmontemagno/podcast-metadata-generator) — a CLI tool powered by the GitHub Copilot SDK that does it all in seconds!

- Check out the tool: [https://github.com/jamesmontemagno/podcast-metadata-generator](https://github.com/jamesmontemagno/podcast-metadata-generator)
- Install it: `dotnet tool install -g PodcastMetadataGenerator`

## The Problem

Look, I love podcasting. What I don't love is spending 20-30 minutes after every recording doing the metadata dance:

1. Listening back to figure out a good title
2. Writing three versions of the description (short for Twitter, medium for podcast apps, long for the website)
3. Manually creating YouTube chapter markers with timestamps
4. Converting transcripts to SRT subtitle format

It's not hard work, but it's repetitive and time-consuming. And if you host multiple podcasts like I do (shoutout [Merge Conflict](https://www.mergeconflict.fm/) and [The VS Code Podcast](https://www.vscodepodcast.com/)), it really adds up.

I wanted a tool that could:
- Take a transcript from my recording software (Zencastr, Descript, whatever)
- Automatically generate multiple title suggestions
- Create descriptions of various lengths
- Generate properly timestamped YouTube chapters
- Convert to valid SRT subtitles

And here's the kicker — I wanted to build it with C# and .NET, using AI that I already have access to through GitHub Copilot.

## What is the GitHub Copilot CLI?

Before we dive into the SDK, let me explain the Copilot CLI. You probably know GitHub Copilot as the AI assistant in VS Code or Visual Studio that helps you write code. But there's also a command-line interface that lets you interact with Copilot directly from your terminal — and it's incredibly powerful.

### Key Features of Copilot CLI

**Interactive Chat Mode**
Run `copilot` to start an interactive session where you can have back-and-forth conversations with AI. It maintains context across messages, so you can iterate on ideas, debug issues, or brainstorm solutions without leaving your terminal. Use slash commands like `/help` to see what's available.

**Conversational Context**
Unlike one-off API calls, the CLI maintains conversation history within a session. Ask a follow-up question, refine your previous request, or build on earlier responses — just like chatting with a colleague.

**Multiple AI Models**
The CLI gives you access to the same cutting-edge models available in VS Code:
- GPT-4o and GPT-5 from OpenAI
- Claude Sonnet and Opus from Anthropic  
- Gemini from Google

You can switch models on the fly with `/model` in interactive mode to pick the best model for your task.

**Secure Authentication**
The CLI handles OAuth authentication with GitHub, so your credentials are managed securely. No API keys to store in environment variables or accidentally commit to repos. On first launch, you'll be prompted to use `/login` and authenticate via your browser.

**Local Process Architecture**
Here's what makes the CLI special for developers: when you run `copilot`, it starts a local process that exposes a JSON-RPC interface. This means other applications can connect to it and leverage Copilot's capabilities programmatically — which is exactly what I needed for my tool.

**Coding Capabilities**
The CLI isn't just for chatting — it's a full-featured coding agent. It can:
- **Read and modify files** — Ask it to refactor code, add features, or fix bugs directly in your codebase
- **Run shell commands** — Execute builds, tests, or any terminal command with your approval
- **Include file context** — Use `@path/to/file.js` in your prompts to give Copilot specific file context
- **Use custom instructions** — Add `.github/copilot-instructions.md` to your repo for project-specific guidance
- **Custom agents** — Built-in agents for exploring code, running tasks, planning implementations, and code review
- **Delegate to cloud** — Hand off complex tasks to the Copilot coding agent on GitHub with `/delegate`
- **MCP servers** — Extend functionality with Model Context Protocol servers (GitHub's MCP server is built-in)

The CLI uses a smart permissions system — it asks for approval before modifying files or running commands, so you stay in control.

### Installing Copilot CLI

There are several ways to install depending on your platform:

**macOS and Linux (Homebrew):**
```bash
brew install copilot-cli
```

**Windows (WinGet):**
```powershell
winget install GitHub.Copilot
```

**All platforms (npm, requires Node.js 22+):**
```bash
npm install -g @github/copilot
```

**macOS and Linux (install script):**
```bash
curl -fsSL https://gh.io/copilot-install | bash
```

You can also download executables directly from the [copilot-cli releases page](https://github.com/github/copilot-cli/releases/).

Then authenticate by running `copilot` and typing `/login`. You'll be directed to GitHub to authorize the CLI, and you're good to go. Alternatively, you can authenticate using a fine-grained personal access token with the "Copilot Requests" permission via the `GH_TOKEN` environment variable.

### Why Build on the CLI?

You might wonder: "Why not just call OpenAI's API directly?" A few reasons:

1. **No API key management** — Your Copilot subscription handles everything
2. **Unified billing** — No surprise charges; it's part of your existing plan
3. **Model flexibility** — Access multiple providers through one interface
4. **Enterprise compliance** — Uses your organization's Copilot policies and data handling
5. **Always up-to-date** — New models appear automatically as GitHub adds them

For my Podcast Metadata Generator, this architecture means anyone with a Copilot subscription can use the tool immediately — no additional setup required.

## What Does the GitHub Copilot SDK Enable?

Here's where it gets really cool. The [GitHub Copilot SDK](https://github.com/github/copilot-sdk) lets your applications communicate with the Copilot CLI programmatically. Instead of parsing terminal output or making direct API calls (and managing your own API keys), you get a clean, typed interface.

The SDK is available in multiple languages:
- **.NET** (NuGet) — What I used for this project
- **TypeScript/JavaScript** (npm)
- **Python** (PyPI)
- **Go**

> **Note:** The Copilot SDK is currently in **technical preview**. Check out the [GitHub repo](https://github.com/github/copilot-sdk) for the latest updates and to provide feedback.

The SDK provides:

- **CopilotClient** — Connects to the local Copilot CLI process
- **Session management** — Create conversational sessions with specific models
- **Streaming support** — Get responses token-by-token for responsive UIs
- **Model selection** — Choose from available models dynamically

Here's how simple it is to send a prompt:

```csharp
using GitHub.Copilot.SDK;

var client = new CopilotClient();
await client.StartAsync();

var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "gpt-4o",
    Streaming = true,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Replace,
        Content = "You are a helpful assistant."
    }
});

session.On(evt =>
{
    if (evt is AssistantMessageDeltaEvent delta)
    {
        Console.Write(delta.Data.DeltaContent);
    }
});

await session.SendAsync(new MessageOptions { Prompt = "Hello!" });
```

That's it! No API keys to manage, no token counting, no billing surprises (beyond your existing Copilot subscription). The SDK handles all the communication with the CLI, which handles all the authentication with GitHub.

## How the Podcast Metadata Generator Works

I built this tool as a .NET 10 console application using the Copilot SDK and [Spectre.Console](https://spectreconsole.net/) for a beautiful terminal UI. Here's the architecture:

```
podcast-metadata-generator/
├── Models/           # Data structures for settings, transcripts, results
├── Services/
│   ├── CopilotAuthService.cs   # Checks if CLI is ready
│   ├── MetadataGenerator.cs    # Sends prompts via SDK
│   ├── TranscriptParser.cs     # Handles multiple transcript formats
│   └── SrtConverter.cs         # Creates valid SRT files
├── Prompts/
│   └── PromptTemplates.cs      # All the AI prompts
└── UI/
    ├── AppWorkflow.cs          # Main interactive loop
    └── ConsoleUI.cs            # Spectre.Console helpers
```

### Supported Transcript Formats

Not everyone uses the same transcription service, so I built parsers for multiple formats:

**Zencastr Format:**
```
00:00.00 James: Hey everyone, welcome to the show!
00:15.50 Frank: Thanks for having me!
```

**Time-Range Format:**
```
00:00:00 - 00:00:15
Hey everyone, welcome to the show!
```

**SRT Format:**
```
1
00:00:00,000 --> 00:00:15,000
Hey everyone, welcome to the show!
```

**Plain Text:**
Just paste any transcript — it'll work for titles and descriptions, though chapters and SRT conversion need timestamps.

### The Generation Process

When you load a transcript and hit "Generate All Metadata," here's what happens:

1. **Title Generation** — The AI analyzes the transcript to find the main topics (not every tangent!) and generates multiple title suggestions. I typically ask for 5 options.

2. **Description Generation** — Three versions: short (~50 words for social media), medium (~150 words for podcast apps), and long (~300 words for show notes).

3. **Chapter Generation** — This is my favorite part. The AI identifies major topic transitions and creates YouTube-compatible chapter markers. It even ensures the first chapter starts at 00:00!

4. **SRT Conversion** — Takes the timestamped transcript and converts it to valid SRT subtitle format.

All generation happens with streaming, so you watch the AI response appear in real-time. It's satisfying to see, and it lets you know something's actually happening.

## What It Looks Like

The UI is all terminal-based but beautifully styled thanks to Spectre.Console:

```
────────────────────────────────────────────────────────
            🎙️ Podcast Metadata Generator
────────────────────────────────────────────────────────

┌──────────────────────────────────────────────────────┐
│ File    │ mergeconflict498.txt                       │
│ Format  │ Zencastr                                   │
│ Duration│ 45.3 minutes                               │
│ Segments│ 847                                        │
└──────────────────────────────────────────────────────┘

Main Menu
> 📂 Load Different Transcript
  🚀 Generate All Metadata
  📝 Generate Titles
  📄 Generate Descriptions
  📑 Generate Chapters
  🎬 Convert to SRT
  ⚙️ Settings
  ❌ Exit
```

When generating, you see the response stream in:

```
┌─────────────── Generating Titles ────────────────────┐
│ 1. .NET MAUI Performance Deep Dive                   │
│ 2. Building Faster Apps with MAUI Compiled Bindings  │
│ 3. The Complete Guide to MAUI Optimization           │
│ 4. Why Your MAUI App is Slow (And How to Fix It)     │
│ 5. Performance Secrets Every MAUI Developer Needs   │
└──────────────────────────────────────────────────────┘
✓ Generated 5 title suggestions
```

## Customization Options

Every podcaster has different needs, so I made everything configurable:

### General Settings
- **AI Model** — Pick from available models (dynamically fetched from the CLI)
- **Output Directory** — Where to save generated files
- **Podcast Name** — Added to prompts for better context
- **Host Names** — Helps the AI understand who's speaking
- **Episode Context** — Add guest names or topics before generation

### Generation Settings
- **Title Count** — How many suggestions (default: 5)
- **Title Max Words** — Keep titles concise (default: 10)
- **Description Lengths** — Customize word counts for short/medium/long
- **Chapter Range** — Min/max chapters to generate
- **Chapters per 30 min** — Target density

Settings persist to `~/.config/podcast-metadata-generator/settings.json`, so your preferences are saved across sessions.

## Installation and Usage

### Install as a .NET Tool

The easiest way:

```bash
dotnet tool install -g PodcastMetadataGenerator
```

Then just run:

```bash
podcast-metadata
```

Or pass a transcript directly:

```bash
podcast-metadata /path/to/transcript.txt
```

### Build from Source

```bash
git clone https://github.com/jamesmontemagno/podcast-metadata-generator.git
cd podcast-metadata-generator
dotnet build
dotnet run
```

### Prerequisites

You'll need:
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [GitHub Copilot CLI](https://docs.github.com/en/copilot/how-tos/set-up/install-copilot-cli) installed and authenticated
- An active GitHub Copilot subscription

## Output Files

When you save results, you get a complete package:

| File | Description |
|------|-------------|
| `titles.txt` | All title suggestions |
| `description-short.txt` | Short description (~50 words) |
| `description-medium.txt` | Medium description (~150 words) |
| `description-long.txt` | Long description (~300 words) |
| `chapters.txt` | YouTube-compatible chapter markers |
| `subtitles.srt` | SRT subtitle file |
| `manifest.json` | JSON manifest with all metadata |

The `manifest.json` is particularly useful if you want to integrate this into an automation pipeline.

## The Power of Local AI

What I love about this approach is that everything runs locally (well, the AI inference happens in the cloud, but the orchestration is local). There's no web service to hit, no API keys to rotate, no per-request billing to worry about. If you have Copilot, you have this.

The SDK is still in preview (I'm using version 0.1.13), but it's been rock solid for my use case. The team at GitHub is actively developing it, and I'm excited to see where it goes.

## What's Next?

I have a few ideas for future improvements:

- **Batch processing** — Generate metadata for multiple episodes at once
- **Template system** — Define your own prompt templates
- **Direct upload** — Send chapters directly to YouTube
- **VS Code extension** — Generate metadata without leaving the editor

But honestly, it already does exactly what I need. Every week after recording, I run this tool and have all my metadata ready in about 30 seconds. That's hours saved over the course of a year.

## Try It Out!

If you're a podcaster (or work with any kind of timestamped transcripts), give it a try:

```bash
dotnet tool install -g PodcastMetadataGenerator
podcast-metadata
```

The code is open source on [GitHub](https://github.com/jamesmontemagno/podcast-metadata-generator), and I'd love contributions. Found a bug? Have a transcript format I don't support? Open an issue or send a PR!

And if you're interested in building your own tools with the GitHub Copilot SDK, this project is a great starting point. The SDK makes it remarkably easy to add AI capabilities to .NET applications.

Happy coding, happy podcasting!

Cheers,
James
