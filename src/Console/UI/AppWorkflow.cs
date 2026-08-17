using GitHub.Copilot;
using Spectre.Console;
using PodcastMetadataGenerator.Core.Models;
using PodcastMetadataGenerator.Core.Services;
using System.Text;

namespace PodcastMetadataGenerator.Console.UI;

/// <summary>
/// Main application workflow using Spectre.Console.
/// </summary>
public class AppWorkflow
{
    private AppSettings _settings;
    private readonly SettingsService _settingsService;
    private readonly TranscriptParser _parser;
    private readonly SrtConverter _srtConverter;
    private readonly OutputService _outputService;
    private readonly WhisperModelService _whisperModelService;
    private MetadataGenerator? _generator;
    
    private Transcript? _transcript;
    private GenerationResult _result = new();

    private static string BuildStreamSeparator()
    {
        var width = Math.Max(20, AnsiConsole.Profile.Width);
        return new string('-', width);
    }

    private static string BuildStreamBlock(string content)
    {
        var separator = BuildStreamSeparator();
        return $"{separator}{Environment.NewLine}{content}{Environment.NewLine}{separator}";
    }

    private static Panel CreateStreamingPanel(string header, string content, bool isMarkupContent)
    {
        var block = BuildStreamBlock(content);
        if (isMarkupContent)
        {
            return new Panel(new Markup(block))
            {
                Header = new PanelHeader($"[bold] {Markup.Escape(header)} [/]"),
                Border = BoxBorder.None,
                Padding = new Padding(0, 0),
                Expand = true
            };
        }

        return new Panel(new Text(block))
        {
            Header = new PanelHeader($"[bold] {Markup.Escape(header)} [/]"),
            Border = BoxBorder.None,
            Padding = new Padding(0, 0),
            Expand = true
        };
    }
    
    public AppWorkflow()
    {
        _settings = new AppSettings();
        _settingsService = new SettingsService();
        _parser = new TranscriptParser(_settings);
        _srtConverter = new SrtConverter();
        _outputService = new OutputService(_srtConverter);
        _whisperModelService = new WhisperModelService();
    }
    
    /// <summary>
    /// Runs the application with optional CLI arguments.
    /// </summary>
    public async Task RunAsync(string[] args, CopilotAuthService.CopilotStatus? copilotStatus = null)
    {
        // Load saved settings
        await LoadSettingsAsync();
        
        // Show header with ASCII art and Copilot status
        ConsoleUI.ShowHeader(copilotStatus);
        
        // Check if Copilot is not ready
        if (copilotStatus != null && (!copilotStatus.IsInstalled || (!copilotStatus.IsTokenSet && !copilotStatus.IsAuthenticated)))
        {
            AnsiConsole.MarkupLine("[yellow]Press any key to exit...[/]");
            System.Console.ReadKey(true);
            return;
        }
        
        // If an input path is provided as an argument, route it through the matching flow.
        if (args.Length > 0 && File.Exists(args[0]))
        {
            if (IsTranscriptPath(args[0]))
            {
                await LoadTranscriptAsync(args[0]);
            }
            else if (IsVideoPath(args[0]))
            {
                await ProcessVideoAsync(args[0]);
            }
            else
            {
                ConsoleUI.ShowWarning($"Unsupported input file type: {Path.GetExtension(args[0])}");
            }
        }
        
        await MainMenuLoopAsync();
    }
    
    private async Task LoadSettingsAsync()
    {
        try
        {
            _settings = await _settingsService.LoadAsync();
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowWarning($"Could not load settings: {ex.Message}. Using defaults.");
        }
    }
    
    private async Task SaveSettingsAsync()
    {
        try
        {
            await _settingsService.SaveAsync(_settings);
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowWarning($"Could not save settings: {ex.Message}");
        }
    }
    
    private async Task MainMenuLoopAsync()
    {
        while (true)
        {
            var choices = new List<string>();
            
            if (_transcript == null)
            {
                choices.Add("📂 Load Transcript or Video");
            }
            else
            {
                choices.Add("📂 Load Different Transcript or Video");
                choices.Add("🚀 Generate All Metadata");
                choices.Add("📝 Generate Titles");
                choices.Add("📄 Generate Descriptions");
                choices.Add("📑 Generate Chapters");
                choices.Add("🎬 Convert to SRT");
            }
            
            if (_result.Titles.Count > 0 || _result.Descriptions.Count > 0 || _result.Chapters.Count > 0)
            {
                choices.Add("👁️ View Results");
                choices.Add("💾 Save Results");
            }
            
            choices.Add("⚙️ Settings");
            choices.Add("❌ Exit");
            
            AnsiConsole.WriteLine();
            var action = ConsoleUI.SelectFromList("[bold]Main Menu[/]", choices);
            
            switch (action)
            {
                case "📂 Load Transcript or Video":
                case "📂 Load Different Transcript or Video":
                    await PromptAndLoadInputAsync();
                    break;
                    
                case "🚀 Generate All Metadata":
                    await GenerateAllAsync();
                    break;
                    
                case "📝 Generate Titles":
                    await GenerateTitlesAsync();
                    break;
                    
                case "📄 Generate Descriptions":
                    await GenerateDescriptionsAsync();
                    break;
                    
                case "📑 Generate Chapters":
                    await GenerateChaptersAsync();
                    break;
                    
                case "🎬 Convert to SRT":
                    ConvertToSrt();
                    break;
                    
                case "👁️ View Results":
                    await ViewResultsMenuAsync();
                    break;
                    
                case "💾 Save Results":
                    await SaveResultsAsync();
                    break;
                    
                case "⚙️ Settings":
                    await SettingsMenuAsync();
                    break;
                    
                case "❌ Exit":
                    if (AnsiConsole.Confirm("Are you sure you want to exit?"))
                    {
                        await CleanupAsync();
                        return;
                    }
                    break;
            }
        }
    }
    
    private async Task PromptAndLoadInputAsync()
    {
        var inputType = ConsoleUI.SelectFromList(
            "What would you like to provide?",
            new[] { "📄 Transcript file", "🎥 Video file" });

        if (inputType == "📄 Transcript file")
        {
            var transcriptPath = ConsoleUI.AskFilePath(
                "Select a transcript file:",
                mustExist: true,
                discoveryType: ConsoleUI.FileDiscoveryType.Transcript);
            await LoadTranscriptAsync(transcriptPath);
            return;
        }

        var videoPath = ConsoleUI.AskFilePath(
            "Select a video file:",
            mustExist: true,
            discoveryType: ConsoleUI.FileDiscoveryType.Video);
        await ProcessVideoAsync(videoPath);
    }

    private async Task ProcessVideoAsync(string videoPath)
    {
        try
        {
            if (_whisperModelService.GetInstalledModelPath(_settings) is null)
            {
                ConsoleUI.ShowWarning("A Whisper model must be installed before a video can be transcribed.");
                if (!AnsiConsole.Confirm("Open video transcription settings now?", defaultValue: true))
                {
                    return;
                }

                await EditVideoTranscriptionSettingsAsync();
                if (_whisperModelService.GetInstalledModelPath(_settings) is null)
                {
                    ConsoleUI.ShowWarning("Video transcription was cancelled because no Whisper model is installed.");
                    return;
                }
            }

            var videoTranscriptService = new VideoTranscriptService(_settings, _whisperModelService);
            var isVideo = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Confirming the selected file is a video...", _ =>
                    videoTranscriptService.IsVideoFileAsync(videoPath));

            if (!isVideo)
            {
                ConsoleUI.ShowError("The selected file does not contain a readable video stream.");
                return;
            }

            ConsoleUI.ShowSuccess($"Confirmed video file: {Path.GetFileName(videoPath)}");

            var srt = await AnsiConsole.Progress()
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new RemainingTimeColumn(),
                    new SpinnerColumn())
                .StartAsync(async context =>
                {
                    var task = context.AddTask(
                        "[blue]Preparing audio for Whisper...[/]",
                        maxValue: 100);
                    task.IsIndeterminate = true;

                    var progress = new InlineProgress<VideoTranscriptionProgress>(update =>
                    {
                        task.IsIndeterminate = false;
                        task.Value = update.Percentage;
                        task.Description =
                            $"[blue]Transcribing {FormatDuration(update.Position)} / {FormatDuration(update.Duration)}[/]";
                    });

                    return await videoTranscriptService.TranscribeToSrtAsync(videoPath, progress);
                });

            var defaultDirectory = Path.GetDirectoryName(videoPath) ?? Environment.CurrentDirectory;
            var defaultPath = Path.Combine(defaultDirectory, $"{Path.GetFileNameWithoutExtension(videoPath)}.srt");
            var transcriptPath = ConsoleUI.AskSaveFilePath(
                "Where should the transcript be saved?",
                defaultPath);

            await File.WriteAllTextAsync(transcriptPath, srt, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            ConsoleUI.ShowSuccess($"Saved transcript: {transcriptPath}");
            await LoadTranscriptAsync(transcriptPath);
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Video transcription failed: {ex.Message}");
        }
    }
    
    private async Task LoadTranscriptAsync(string path)
    {
        try
        {
            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Loading transcript...", async ctx =>
                {
                    _transcript = await _parser.ParseAsync(path);
                });
            
            _result = new GenerationResult(); // Reset results
            
            ConsoleUI.ShowSuccess($"Loaded transcript: {Path.GetFileName(path)}");
            ConsoleUI.ShowTranscriptInfo(_transcript!);
            
            // Prompt for episode context based on settings
            if (_settings.PromptForContextOnLoad)
            {
                if (AnsiConsole.Confirm("Would you like to add episode context (guest names, topics, etc.)?", defaultValue: false))
                {
                    _settings.EpisodeContext = ConsoleUI.AskText(
                        "Enter episode context:",
                        validator: _ => true);
                }
                else
                {
                    _settings.EpisodeContext = null;
                }
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Failed to load transcript: {ex.Message}");
        }
    }
    
    private async Task GenerateAllAsync()
    {
        if (!EnsureTranscriptLoaded()) return;
        
        try
        {
            await EnsureGeneratorInitializedAsync();
            
            // Generate titles
            ConsoleUI.ShowInfo("Generating titles...");
            await GenerateTitlesInternalAsync();
            
            // Select a title for description context
            if (_result.Titles.Count > 0 && string.IsNullOrEmpty(_result.SelectedTitle))
            {
                _result.SelectedTitle = ConsoleUI.SelectTitle(_result.Titles);
            }
            
            // Generate descriptions
            ConsoleUI.ShowInfo("Generating descriptions...");
            await GenerateDescriptionsInternalAsync();
            
            // Generate chapters
            ConsoleUI.ShowInfo("Generating chapters...");
            await GenerateChaptersInternalAsync();
            
            // Convert to SRT
            ConsoleUI.ShowInfo("Converting to SRT...");
            ConvertToSrt();
            
            AnsiConsole.WriteLine();
            ConsoleUI.ShowSuccess("All metadata generated successfully!");
            
            // Show summary
            var summaryTable = new Table()
                .RoundedBorder()
                .BorderColor(Color.Green)
                .Title("[bold green]Generation Summary[/]")
                .AddColumn("Item")
                .AddColumn("Status");
            
            summaryTable.AddRow("Titles", $"[green]{_result.Titles.Count} generated[/]");
            summaryTable.AddRow("Descriptions", $"[green]{_result.Descriptions.Count} generated[/]");
            summaryTable.AddRow("Chapters", $"[green]{_result.Chapters.Count} generated[/]");
            summaryTable.AddRow("SRT", string.IsNullOrEmpty(_result.SrtContent) ? "[red]Not generated[/]" : "[green]Ready[/]");
            
            AnsiConsole.Write(summaryTable);
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Generation failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                ConsoleUI.ShowError($"  Inner: {ex.InnerException.Message}");
            }
#if DEBUG
            AnsiConsole.WriteException(ex);
#endif
        }
    }
    
    private async Task GenerateTitlesAsync()
    {
        if (!EnsureTranscriptLoaded()) return;
        
        try
        {
            await EnsureGeneratorInitializedAsync();
            await GenerateTitlesInternalAsync();
            
            // Show titles and allow selection
            _result.SelectedTitle = ConsoleUI.SelectTitle(_result.Titles);
            
            if (_result.SelectedTitle != null)
            {
                ConsoleUI.ShowSuccess($"Selected: {_result.SelectedTitle}");
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Failed to generate titles: {ex.Message}");
        }
    }
    
    private async Task GenerateTitlesInternalAsync()
    {
        var responseText = "";
        var lockObj = new object();
        var generationTask = default(Task<List<string>>);
        var animationFrame = 0;
        
        await AnsiConsole.Live(new Panel(""))
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                var panel = CreateStreamingPanel("Generating Titles", "[grey]Waiting for response...[/]", true);
                ctx.UpdateTarget(panel);
                
                // Start generation on a background thread
                generationTask = Task.Run(async () =>
                {
                    return await _generator!.GenerateTitlesAsync(
                        _transcript!,
                        chunk =>
                        {
                            lock (lockObj)
                            {
                                responseText += chunk;
                            }
                        });
                });
                
                // Poll and update UI while generation is running
                while (!generationTask.IsCompleted)
                {
                    string currentText;
                    lock (lockObj)
                    {
                        currentText = responseText;
                    }
                    
                    if (!string.IsNullOrEmpty(currentText))
                    {
                        panel = CreateStreamingPanel("Generating Titles", currentText, false);
                        ctx.UpdateTarget(panel);
                    }
                    else
                    {
                        // Animate waiting message
                        var dots = new string('.', (animationFrame % 3) + 1).PadRight(3);
                        panel = CreateStreamingPanel("Generating Titles", $"[grey]Waiting for response{dots}[/]", true);
                        ctx.UpdateTarget(panel);
                        animationFrame++;
                    }
                    
                    await Task.Delay(50); // Update every 50ms
                }
                
                // Final update
                _result.Titles = await generationTask;
                
                panel = CreateStreamingPanel("Generating Titles", responseText, false);
                ctx.UpdateTarget(panel);
            });
        
        AnsiConsole.WriteLine();
        ConsoleUI.ShowSuccess($"Generated {_result.Titles.Count} title suggestions");
    }
    
    private async Task GenerateDescriptionsAsync()
    {
        if (!EnsureTranscriptLoaded()) return;
        
        try
        {
            await EnsureGeneratorInitializedAsync();
            await GenerateDescriptionsInternalAsync();
            
            ConsoleUI.ShowDescriptions(_result.Descriptions);
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Failed to generate descriptions: {ex.Message}");
        }
    }
    
    private async Task GenerateDescriptionsInternalAsync()
    {
        foreach (var length in Enum.GetValues<DescriptionLength>())
        {
            var responseText = "";
            var lockObj = new object();
            var generationTask = default(Task<string>);
            var animationFrame = 0;
            
            await AnsiConsole.Live(new Panel(""))
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    var panel = CreateStreamingPanel($"Generating {length} Description", "[grey]Waiting for response...[/]", true);
                    ctx.UpdateTarget(panel);
                    
                    // Start generation on a background thread
                    generationTask = Task.Run(async () =>
                    {
                        return await _generator!.GenerateDescriptionAsync(
                            _transcript!,
                            length,
                            _result.SelectedTitle,
                            chunk =>
                            {
                                lock (lockObj)
                                {
                                    responseText += chunk;
                                }
                            });
                    });
                    
                    // Poll and update UI while generation is running
                    while (!generationTask.IsCompleted)
                    {
                        string currentText;
                        lock (lockObj)
                        {
                            currentText = responseText;
                        }
                        
                        if (!string.IsNullOrEmpty(currentText))
                        {
                            panel = CreateStreamingPanel($"Generating {length} Description", currentText, false);
                            ctx.UpdateTarget(panel);
                        }
                        else
                        {
                            // Animate waiting message
                            var dots = new string('.', (animationFrame % 3) + 1).PadRight(3);
                            panel = CreateStreamingPanel($"Generating {length} Description", $"[grey]Waiting for response{dots}[/]", true);
                            ctx.UpdateTarget(panel);
                            animationFrame++;
                        }
                        
                        await Task.Delay(50); // Update every 50ms
                    }
                    
                    // Final update
                    _result.Descriptions[length] = await generationTask;
                    
                    panel = CreateStreamingPanel($"Generating {length} Description", responseText, false);
                    ctx.UpdateTarget(panel);
                });
            
            AnsiConsole.WriteLine();
            ConsoleUI.ShowSuccess($"Generated {length.ToString().ToLower()} description");
        }
    }
    
    private async Task GenerateChaptersAsync()
    {
        if (!EnsureTranscriptLoaded()) return;
        
        try
        {
            await EnsureGeneratorInitializedAsync();
            await GenerateChaptersInternalAsync();
            
            ConsoleUI.ShowChapters(_result.Chapters);
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Failed to generate chapters: {ex.Message}");
        }
    }
    
    private async Task GenerateChaptersInternalAsync()
    {
        var responseText = "";
        var lockObj = new object();
        var generationTask = default(Task<List<Chapter>>);
        var animationFrame = 0;
        
        await AnsiConsole.Live(new Panel(""))
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                var panel = CreateStreamingPanel("Generating Chapters", "[grey]Waiting for response...[/]", true);
                ctx.UpdateTarget(panel);
                
                // Start generation on a background thread
                generationTask = Task.Run(async () =>
                {
                    return await _generator!.GenerateChaptersAsync(
                        _transcript!,
                        chunk =>
                        {
                            lock (lockObj)
                            {
                                responseText += chunk;
                            }
                        });
                });
                
                // Poll and update UI while generation is running
                while (!generationTask.IsCompleted)
                {
                    string currentText;
                    lock (lockObj)
                    {
                        currentText = responseText;
                    }
                    
                    if (!string.IsNullOrEmpty(currentText))
                    {
                        panel = CreateStreamingPanel("Generating Chapters", currentText, false);
                        ctx.UpdateTarget(panel);
                    }
                    else
                    {
                        // Animate waiting message
                        var dots = new string('.', (animationFrame % 3) + 1).PadRight(3);
                        panel = CreateStreamingPanel("Generating Chapters", $"[grey]Waiting for response{dots}[/]", true);
                        ctx.UpdateTarget(panel);
                        animationFrame++;
                    }
                    
                    await Task.Delay(50); // Update every 50ms
                }
                
                // Final update
                _result.Chapters = await generationTask;
                
                panel = CreateStreamingPanel("Generating Chapters", responseText, false);
                ctx.UpdateTarget(panel);
            });
        
        AnsiConsole.WriteLine();
        ConsoleUI.ShowSuccess($"Generated {_result.Chapters.Count} chapters");
    }
    
    private void ConvertToSrt()
    {
        if (!EnsureTranscriptLoaded()) return;
        
        try
        {
            var result = _srtConverter.ConvertToSrt(_transcript!);
            _result.SrtContent = result.Content;
            _result.SrtValidationErrors = result.Errors;
            
            if (result.Errors.Count > 0)
            {
                ConsoleUI.ShowWarning($"SRT converted with {result.Errors.Count} warnings");
                foreach (var error in result.Errors.Take(5))
                {
                    AnsiConsole.MarkupLine($"  [grey]• {Markup.Escape(error)}[/]");
                }
            }
            else
            {
                ConsoleUI.ShowSuccess("SRT converted successfully");
            }
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Failed to convert to SRT: {ex.Message}");
        }
    }
    
    private async Task ViewResultsMenuAsync()
    {
        while (true)
        {
            var choices = new List<string>();
            
            if (_result.Titles.Count > 0)
                choices.Add("📝 View Titles");
            if (_result.Descriptions.Count > 0)
                choices.Add("📄 View Descriptions");
            if (_result.Chapters.Count > 0)
                choices.Add("📑 View Chapters");
            if (!string.IsNullOrEmpty(_result.SrtContent))
                choices.Add("🎬 View SRT Preview");
            
            choices.Add("⬅️ Back to Main Menu");
            
            var action = ConsoleUI.SelectFromList("[bold]View Results[/]", choices);
            
            switch (action)
            {
                case "📝 View Titles":
                    _result.SelectedTitle = ConsoleUI.SelectTitle(_result.Titles);
                    break;
                    
                case "📄 View Descriptions":
                    ConsoleUI.ShowDescriptions(_result.Descriptions);
                    ConsoleUI.WaitForKey();
                    break;
                    
                case "📑 View Chapters":
                    ConsoleUI.ShowChapters(_result.Chapters);
                    ConsoleUI.WaitForKey();
                    break;
                    
                case "🎬 View SRT Preview":
                    var preview = string.Join("\n", _result.SrtContent!.Split('\n').Take(30));
                    if (_result.SrtContent!.Split('\n').Length > 30)
                        preview += "\n\n[grey]... (truncated)[/]";
                    ConsoleUI.ShowMarkupPanel("SRT Preview", Markup.Escape(preview), Color.Purple);
                    ConsoleUI.WaitForKey();
                    break;
                    
                case "⬅️ Back to Main Menu":
                    return;
            }
        }
    }
    
    private async Task SaveResultsAsync()
    {
        if (_transcript == null)
        {
            ConsoleUI.ShowError("No transcript loaded.");
            return;
        }
        
        var outputDir = ConsoleUI.AskText(
            "Enter output directory:",
            defaultValue: _settings.OutputDirectory,
            validator: path => !string.IsNullOrWhiteSpace(path));
        
        try
        {
            var savedFiles = await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .SpinnerStyle(Style.Parse("blue"))
                .StartAsync("Saving results...", async ctx =>
                {
                    return await _outputService.SaveAllAsync(
                        outputDir,
                        _transcript,
                        _result,
                        _settings);
                });
            
            ConsoleUI.ShowSuccess($"Saved {savedFiles.Count} files to: {outputDir}");
            
            var table = new Table()
                .RoundedBorder()
                .BorderColor(Color.Green)
                .AddColumn("Saved Files");
            
            foreach (var file in savedFiles)
            {
                table.AddRow(Markup.Escape(Path.GetFileName(file)));
            }
            
            AnsiConsole.Write(table);
        }
        catch (Exception ex)
        {
            ConsoleUI.ShowError($"Failed to save: {ex.Message}");
        }
    }
    
    private async Task SettingsMenuAsync()
    {
        while (true)
        {
            AnsiConsole.WriteLine();
            
            // Show current settings grouped by category
            var generalTable = new Table()
                .RoundedBorder()
                .BorderColor(Color.Blue)
                .Title("[bold]General Settings[/]")
                .HideHeaders()
                .AddColumn("Setting")
                .AddColumn("Value");
            
            generalTable.AddRow("[blue]Model[/]", Markup.Escape(_settings.Model));
            generalTable.AddRow("[blue]Output Directory[/]", Markup.Escape(_settings.OutputDirectory));
            generalTable.AddRow("[blue]ffmpeg[/]", Markup.Escape(_settings.FfmpegPath));
            var whisperModel = WhisperModelCatalog.Get(_settings.WhisperModel);
            var installedModelPath = _whisperModelService.GetInstalledModelPath(_settings);
            generalTable.AddRow(
                "[blue]Whisper Model[/]",
                installedModelPath is null
                    ? $"{Markup.Escape(whisperModel.DisplayName)} [yellow](not installed)[/]"
                    : $"{Markup.Escape(whisperModel.DisplayName)} [green](initialized)[/]");
            generalTable.AddRow("[blue]Podcast Name[/]", 
                string.IsNullOrEmpty(_settings.PodcastName) 
                    ? "[grey](not set)[/]" 
                    : Markup.Escape(_settings.PodcastName));
            generalTable.AddRow("[blue]Host Names[/]", 
                string.IsNullOrEmpty(_settings.HostNames) 
                    ? "[grey](not set)[/]" 
                    : Markup.Escape(_settings.HostNames));
            generalTable.AddRow("[blue]Prompt for Context[/]", _settings.PromptForContextOnLoad ? "[green]Yes[/]" : "[grey]No[/]");
            generalTable.AddRow("[blue]Episode Context[/]", 
                string.IsNullOrEmpty(_settings.EpisodeContext) 
                    ? "[grey](not set)[/]" 
                    : Markup.Escape(_settings.EpisodeContext.Length > 40 
                        ? _settings.EpisodeContext[..40] + "..." 
                        : _settings.EpisodeContext));
            
            AnsiConsole.Write(generalTable);
            
            var generationTable = new Table()
                .RoundedBorder()
                .BorderColor(Color.Yellow)
                .Title("[bold]Generation Settings[/]")
                .HideHeaders()
                .AddColumn("Setting")
                .AddColumn("Value");
            
            generationTable.AddRow("[yellow]Title Count[/]", $"{_settings.TitleCount} suggestions");
            generationTable.AddRow("[yellow]Title Max Words[/]", $"{_settings.TitleMaxWords} words");
            generationTable.AddRow("[yellow]Short Description[/]", $"~{_settings.ShortDescriptionWords} words");
            generationTable.AddRow("[yellow]Medium Description[/]", $"~{_settings.MediumDescriptionWords} words");
            generationTable.AddRow("[yellow]Long Description[/]", $"~{_settings.LongDescriptionWords} words");
            generationTable.AddRow("[yellow]Chapter Range[/]", $"{_settings.MinChapters}-{_settings.MaxChapters} chapters");
            generationTable.AddRow("[yellow]Chapters per 30min[/]", $"~{_settings.ChaptersPer30Min}");
            generationTable.AddRow("[yellow]Chapter Title Words[/]", $"max {_settings.ChapterTitleMaxWords} words");
            
            AnsiConsole.Write(generationTable);
            
            var action = ConsoleUI.SelectFromList(
                "[bold]Settings Menu[/]",
                new[] 
                { 
                    "🤖 Change Model", 
                    "🎥 Video Transcription Settings",
                    "📁 Change Output Directory", 
                    "🎙️ Podcast Info (Name & Hosts)",
                    "📝 Episode Context",
                    "🔧 Generation Settings (Titles, Descriptions, Chapters)",
                    "💾 Save Settings",
                    "🔄 Reset to Defaults",
                    "⬅️ Back to Main Menu" 
                });
            
            switch (action)
            {
                case "🤖 Change Model":
                    var modelInfos = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("blue"))
                        .StartAsync("Fetching available models from Copilot SDK...", async ctx =>
                        {
                            return await AvailableModels.GetModelsWithMetadataAsync();
                        });

                    if (modelInfos.Count == 0)
                    {
                        ConsoleUI.ShowWarning("No models were returned by the Copilot SDK.");
                        break;
                    }
                    
                    // Create display mapping
                    var modelChoices = modelInfos.ToDictionary(
                        m => FormatModelName(m),
                        m => m.Id);
                    
                    var selectedDisplay = ConsoleUI.SelectFromList(
                        "Select AI Model (multiplier shows relative cost):",
                        modelChoices.Keys);
                    
                    _settings.Model = modelChoices[selectedDisplay];
                    ConsoleUI.ShowSuccess($"Model set to: {_settings.Model}");
                    break;
                    
                case "📁 Change Output Directory":
                    _settings.OutputDirectory = ConsoleUI.AskText(
                        "Enter output directory:",
                        defaultValue: _settings.OutputDirectory);
                    ConsoleUI.ShowSuccess($"Output directory set to: {_settings.OutputDirectory}");
                    break;

                case "🎥 Video Transcription Settings":
                    await EditVideoTranscriptionSettingsAsync();
                    break;
                    
                case "🎙️ Podcast Info (Name & Hosts)":
                    EditPodcastInfo();
                    break;
                    
                case "📝 Episode Context":
                    _settings.PromptForContextOnLoad = AnsiConsole.Confirm(
                        "Prompt for episode context when loading transcripts?",
                        defaultValue: _settings.PromptForContextOnLoad);
                    
                    _settings.EpisodeContext = ConsoleUI.AskText(
                        "Enter default episode context (guest names, topics, etc.):",
                        defaultValue: _settings.EpisodeContext ?? "",
                        validator: _ => true);
                    if (string.IsNullOrWhiteSpace(_settings.EpisodeContext))
                        _settings.EpisodeContext = null;
                    ConsoleUI.ShowSuccess("Episode context settings updated");
                    break;
                    
                case "🔧 Generation Settings (Titles, Descriptions, Chapters)":
                    EditGenerationSettings();
                    break;
                    
                case "💾 Save Settings":
                    await SaveSettingsAsync();
                    ConsoleUI.ShowSuccess($"Settings saved to: {SettingsService.GetDefaultSettingsPath()}");
                    break;
                    
                case "🔄 Reset to Defaults":
                    if (AnsiConsole.Confirm("Reset all settings to defaults?", defaultValue: false))
                    {
                        _settings = new AppSettings();
                        _settings.Model = await AvailableModels.ResolveModelAsync(_settings.Model);
                        ConsoleUI.ShowSuccess("Settings reset to defaults");
                    }
                    break;
                    
                case "⬅️ Back to Main Menu":
                    // Auto-save on exit from settings
                    await SaveSettingsAsync();
                    return;
            }
        }
    }

    private async Task EditVideoTranscriptionSettingsAsync()
    {
        while (true)
        {
            var model = WhisperModelCatalog.Get(_settings.WhisperModel);
            var installedPath = _whisperModelService.GetInstalledModelPath(_settings);
            var action = ConsoleUI.SelectFromList(
                $"[bold]Video Transcription[/]\nffmpeg: [blue]{Markup.Escape(_settings.FfmpegPath)}[/]\n" +
                $"Model: [blue]{Markup.Escape(model.DisplayName)}[/] ({model.ApproximateSize}) " +
                (installedPath is null ? "[yellow]not installed[/]" : "[green]initialized[/]"),
                new[]
                {
                    "🛠️ Configure ffmpeg",
                    "🧠 Choose Whisper GGML Model",
                    "⬇️ Download and Initialize Selected Model",
                    "⬅️ Back"
                });

            switch (action)
            {
                case "🛠️ Configure ffmpeg":
                    _settings.FfmpegPath = ConsoleUI.AskText(
                        "Enter the ffmpeg executable path or command:",
                        defaultValue: _settings.FfmpegPath);
                    break;

                case "🧠 Choose Whisper GGML Model":
                    var selected = ConsoleUI.SelectFromList(
                        "Choose a GGML model (English variants only transcribe English):",
                        WhisperModelCatalog.All,
                        option => $"{option.DisplayName} - {option.ApproximateSize} - {option.Guidance}");
                    if (!string.Equals(_settings.WhisperModel, selected.Id, StringComparison.Ordinal))
                    {
                        _settings.WhisperModel = selected.Id;
                        _settings.WhisperModelPath = null;
                    }
                    break;

                case "⬇️ Download and Initialize Selected Model":
                    try
                    {
                        var modelPath = await AnsiConsole.Status()
                            .Spinner(Spinner.Known.Dots)
                            .SpinnerStyle(Style.Parse("blue"))
                            .StartAsync(
                                $"Downloading and initializing {model.DisplayName} ({model.ApproximateSize})...",
                                _ => _whisperModelService.DownloadAndInitializeAsync(_settings));
                        await SaveSettingsAsync();
                        ConsoleUI.ShowSuccess($"Whisper model initialized: {modelPath}");
                    }
                    catch (Exception ex)
                    {
                        ConsoleUI.ShowError($"Could not install the Whisper model: {ex.Message}");
                    }
                    break;

                case "⬅️ Back":
                    await SaveSettingsAsync();
                    return;
            }
        }
    }

    private static bool IsTranscriptPath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".txt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".srt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".vtt", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".json", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".md", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsVideoPath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".mp4", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mov", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mkv", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".avi", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".webm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".m4v", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".wmv", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mpeg", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".mpg", StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return duration.TotalHours >= 1
            ? duration.ToString(@"h\:mm\:ss")
            : duration.ToString(@"m\:ss");
    }

    private sealed class InlineProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
    
    private void EditPodcastInfo()
    {
        _settings.PodcastName = ConsoleUI.AskText(
            "Enter podcast name (used in prompts for context):",
            defaultValue: _settings.PodcastName ?? "",
            validator: _ => true);
        if (string.IsNullOrWhiteSpace(_settings.PodcastName))
            _settings.PodcastName = null;
        
        _settings.HostNames = ConsoleUI.AskText(
            "Enter host names (comma-separated, used in prompts):",
            defaultValue: _settings.HostNames ?? "",
            validator: _ => true);
        if (string.IsNullOrWhiteSpace(_settings.HostNames))
            _settings.HostNames = null;
        
        ConsoleUI.ShowSuccess("Podcast info updated");
    }
    
    private void EditGenerationSettings()
    {
        while (true)
        {
            var action = ConsoleUI.SelectFromList(
                "[bold]Generation Settings[/]",
                new[]
                {
                    "📝 Title Settings",
                    "📄 Description Lengths",
                    "📑 Chapter Settings",
                    "⬅️ Back"
                });
            
            switch (action)
            {
                case "📝 Title Settings":
                    _settings.TitleCount = AnsiConsole.Prompt(
                        new TextPrompt<int>("Number of title suggestions to generate:")
                            .DefaultValue(_settings.TitleCount)
                            .Validate(n => n is >= 1 and <= 20 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 1 and 20")));
                    
                    _settings.TitleMaxWords = AnsiConsole.Prompt(
                        new TextPrompt<int>("Maximum words per title:")
                            .DefaultValue(_settings.TitleMaxWords)
                            .Validate(n => n is >= 3 and <= 25 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 3 and 25")));
                    
                    ConsoleUI.ShowSuccess("Title settings updated");
                    break;
                    
                case "📄 Description Lengths":
                    _settings.ShortDescriptionWords = AnsiConsole.Prompt(
                        new TextPrompt<int>("Short description word count:")
                            .DefaultValue(_settings.ShortDescriptionWords)
                            .Validate(n => n is >= 20 and <= 100 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 20 and 100")));
                    
                    _settings.MediumDescriptionWords = AnsiConsole.Prompt(
                        new TextPrompt<int>("Medium description word count:")
                            .DefaultValue(_settings.MediumDescriptionWords)
                            .Validate(n => n is >= 50 and <= 300 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 50 and 300")));
                    
                    _settings.LongDescriptionWords = AnsiConsole.Prompt(
                        new TextPrompt<int>("Long description word count:")
                            .DefaultValue(_settings.LongDescriptionWords)
                            .Validate(n => n is >= 100 and <= 1000 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 100 and 1000")));
                    
                    ConsoleUI.ShowSuccess("Description lengths updated");
                    break;
                    
                case "📑 Chapter Settings":
                    _settings.MinChapters = AnsiConsole.Prompt(
                        new TextPrompt<int>("Minimum number of chapters:")
                            .DefaultValue(_settings.MinChapters)
                            .Validate(n => n is >= 1 and <= 10 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 1 and 10")));
                    
                    _settings.MaxChapters = AnsiConsole.Prompt(
                        new TextPrompt<int>("Maximum number of chapters:")
                            .DefaultValue(_settings.MaxChapters)
                            .Validate(n => n >= _settings.MinChapters && n <= 50 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error($"Must be between {_settings.MinChapters} and 50")));
                    
                    _settings.ChaptersPer30Min = AnsiConsole.Prompt(
                        new TextPrompt<int>("Target chapters per 30 minutes:")
                            .DefaultValue(_settings.ChaptersPer30Min)
                            .Validate(n => n is >= 1 and <= 15 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 1 and 15")));
                    
                    _settings.ChapterTitleMaxWords = AnsiConsole.Prompt(
                        new TextPrompt<int>("Maximum words per chapter title:")
                            .DefaultValue(_settings.ChapterTitleMaxWords)
                            .Validate(n => n is >= 2 and <= 15 
                                ? ValidationResult.Success() 
                                : ValidationResult.Error("Must be between 2 and 15")));
                    
                    ConsoleUI.ShowSuccess("Chapter settings updated");
                    break;
                    
                case "⬅️ Back":
                    return;
            }
        }
    }
    
    private bool EnsureTranscriptLoaded()
    {
        if (_transcript == null)
        {
            ConsoleUI.ShowError("Please load a transcript first.");
            return false;
        }
        return true;
    }
    
    private static string FormatModelName(ModelInfo model)
    {
        var name = model.Name;
        if (model.Billing?.Multiplier > 0)
        {
            name = $"{model.Name} (×{model.Billing.Multiplier:0.##})";
        }
        return name;
    }
    
    private async Task EnsureGeneratorInitializedAsync()
    {
        if (_generator == null)
        {
            _generator = new MetadataGenerator(_settings, RequestPermissionAsync);
        }
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Initializing Copilot...", async ctx =>
            {
                await _generator.InitializeAsync();
            });
    }

    private Task<bool> RequestPermissionAsync(PermissionRequest request, PermissionInvocation invocation)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Panel(new Text(MetadataGenerator.DescribePermissionRequest(request, invocation)))
        {
            Header = new PanelHeader("[yellow]Copilot Permission Request[/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Yellow)
        });

        var approved = AnsiConsole.Confirm("Approve this request?", defaultValue: false);
        return Task.FromResult(approved);
    }
    
    private async Task CleanupAsync()
    {
        if (_generator != null)
        {
            await _generator.DisposeAsync();
        }
    }
}
