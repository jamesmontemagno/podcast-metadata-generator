# 🎙️ Podcast Metadata Generator

Generate podcast metadata (titles, descriptions, chapters, SRT subtitles) from transcripts using AI powered by the GitHub Copilot SDK.

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![License](https://img.shields.io/badge/license-MIT-green)
![GitHub Copilot](https://img.shields.io/badge/GitHub%20Copilot-SDK-000?logo=github)

## ✨ Features

- **🎯 Title Generation** - Get 5 creative title suggestions for your episode
- **📝 Description Generation** - Create short, medium, and long descriptions optimized for different platforms
- **📑 Chapter Generation** - Auto-generate YouTube-compatible chapter markers with timestamps
- **🎬 SRT Conversion** - Convert transcripts to valid SRT subtitle format
- **🔄 Multiple Transcript Formats** - Support for Zencastr, time-range, and SRT formats
- **📂 File Browser** - Built-in file browser or drag-and-drop support
- **⚡ Streaming Responses** - Watch AI responses generate in real-time
- **🤖 Model Selection** - Choose from multiple AI models (GPT-5, Claude, Gemini)

## 📋 Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- [GitHub Copilot CLI](https://docs.github.com/en/copilot/github-copilot-in-the-cli) installed and authenticated

### Installing GitHub Copilot CLI

```bash
# Using GitHub CLI extension
gh extension install github/gh-copilot

# Or via npm
npm install -g @github/copilot-cli
```

Then authenticate:
```bash
copilot
# Type /login and follow the prompts
```

## 🚀 Installation

### As a .NET Tool (Recommended)

```bash
dotnet tool install -g PodcastMetadataGenerator
```

### From Source

```bash
git clone https://github.com/jamesmontemagno/podcast-metadata-generator.git
cd podcast-metadata-generator
dotnet build
```

## 📖 Usage

### Interactive Mode

```bash
# If installed as a tool:
podcast-metadata

# Or from source:
dotnet run
```

### With a Transcript File

```bash
podcast-metadata /path/to/transcript.txt

# Or from source:
dotnet run -- /path/to/transcript.txt
```

## 🎯 Supported Transcript Formats

### Zencastr Format
```
00:00.00 Speaker 1: Hello and welcome to the show.
00:15.50 Speaker 2: Thanks for having me!
```

### Time-Range Format
```
00:00:00 - 00:00:15
Hello and welcome to the show.

00:00:15 - 00:00:30
Thanks for having me!
```

### SRT Format
```
1
00:00:00,000 --> 00:00:15,000
Hello and welcome to the show.

2
00:00:15,000 --> 00:00:30,000
Thanks for having me!
```

## 📁 Output Files

When you save results, the following files are generated:

| File | Description |
|------|-------------|
| `titles.txt` | List of generated title suggestions |
| `description-short.txt` | Short description (~50 words) |
| `description-medium.txt` | Medium description (~150 words) |
| `description-long.txt` | Long description (~300 words) |
| `chapters.txt` | YouTube-compatible chapter markers |
| `subtitles.srt` | SRT subtitle file |
| `manifest.json` | JSON manifest with all metadata |

## ⚙️ Configuration

Access settings from the main menu to configure:

- **AI Model** - Select from available Copilot models (dynamically fetched from CLI)
- **Output Directory** - Default location for saved files
- **Episode Context** - Add guest names, topics, or other context to improve generation

## 🏗️ Project Structure

```
podcast-metadata-generator/
├── Models/
│   ├── AppSettings.cs       # Configuration and available models
│   ├── GenerationResult.cs  # Results container
│   ├── Manifest.cs          # JSON manifest structure
│   └── Transcript.cs        # Transcript and segment models
├── Services/
│   ├── CopilotAuthService.cs   # CLI authentication checks
│   ├── MetadataGenerator.cs    # AI generation via Copilot SDK
│   ├── OutputService.cs        # File output handling
│   ├── SrtConverter.cs         # SRT format conversion
│   └── TranscriptParser.cs     # Multi-format transcript parsing
├── Prompts/
│   └── PromptTemplates.cs      # AI prompt templates
├── UI/
│   ├── AppWorkflow.cs          # Main application workflow
│   └── ConsoleUI.cs            # Spectre.Console UI helpers
└── Program.cs                  # Entry point
```

## 🤝 Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- [GitHub Copilot SDK](https://github.com/github/copilot-sdk) for AI integration
- [Spectre.Console](https://spectreconsole.net/) for the beautiful terminal UI
- Inspired by [jamesmontemagno/app-podcast-assistant](https://github.com/jamesmontemagno/app-podcast-assistant)
