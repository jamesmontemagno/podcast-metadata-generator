using Spectre.Console;
using PodcastMetadataGenerator.Models;
using PodcastMetadataGenerator.Services;

namespace PodcastMetadataGenerator.UI;

/// <summary>
/// Main application workflow using Spectre.Console.
/// </summary>
public class AppWorkflow
{
    private readonly AppSettings _settings;
    private readonly TranscriptParser _parser;
    private readonly SrtConverter _srtConverter;
    private readonly OutputService _outputService;
    private MetadataGenerator? _generator;
    
    private Transcript? _transcript;
    private GenerationResult _result = new();
    
    public AppWorkflow()
    {
        _settings = new AppSettings();
        _parser = new TranscriptParser(_settings);
        _srtConverter = new SrtConverter();
        _outputService = new OutputService();
    }
    
    /// <summary>
    /// Runs the application with optional CLI arguments.
    /// </summary>
    public async Task RunAsync(string[] args)
    {
        ConsoleUI.ShowHeader();
        
        // If transcript path provided as argument, load it directly
        if (args.Length > 0 && File.Exists(args[0]))
        {
            await LoadTranscriptAsync(args[0]);
        }
        
        await MainMenuLoopAsync();
    }
    
    private async Task MainMenuLoopAsync()
    {
        while (true)
        {
            var choices = new List<string>();
            
            if (_transcript == null)
            {
                choices.Add("📂 Load Transcript");
            }
            else
            {
                choices.Add("📂 Load Different Transcript");
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
                case "📂 Load Transcript":
                case "📂 Load Different Transcript":
                    await PromptAndLoadTranscriptAsync();
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
    
    private async Task PromptAndLoadTranscriptAsync()
    {
        var path = ConsoleUI.AskFilePath(
            "Enter transcript file path:",
            mustExist: true);
        
        await LoadTranscriptAsync(path);
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
            
            // Prompt for episode context if not set
            if (string.IsNullOrEmpty(_settings.EpisodeContext))
            {
                if (AnsiConsole.Confirm("Would you like to add episode context (guest names, topics, etc.)?", defaultValue: false))
                {
                    _settings.EpisodeContext = ConsoleUI.AskText(
                        "Enter episode context:",
                        validator: _ => true);
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
        
        await AnsiConsole.Live(new Panel(""))
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                var panel = new Panel(new Markup("[grey]Waiting for response...[/]"))
                {
                    Header = new PanelHeader("[bold] Generating Titles [/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Blue),
                    Padding = new Padding(1, 0),
                    Expand = true
                };
                ctx.UpdateTarget(panel);
                
                _result.Titles = await _generator!.GenerateTitlesAsync(
                    _transcript!,
                    chunk =>
                    {
                        responseText += chunk;
                        panel = new Panel(Markup.Escape(responseText))
                        {
                            Header = new PanelHeader("[bold] Generating Titles [/]"),
                            Border = BoxBorder.Rounded,
                            BorderStyle = new Style(Color.Blue),
                            Padding = new Padding(1, 0),
                            Expand = true
                        };
                        ctx.UpdateTarget(panel);
                    });
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
            
            await AnsiConsole.Live(new Panel(""))
                .AutoClear(false)
                .StartAsync(async ctx =>
                {
                    var panel = new Panel(new Markup("[grey]Waiting for response...[/]"))
                    {
                        Header = new PanelHeader($"[bold] Generating {length} Description [/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Yellow),
                        Padding = new Padding(1, 0),
                        Expand = true
                    };
                    ctx.UpdateTarget(panel);
                    
                    var description = await _generator!.GenerateDescriptionAsync(
                        _transcript!,
                        length,
                        _result.SelectedTitle,
                        chunk =>
                        {
                            responseText += chunk;
                            panel = new Panel(Markup.Escape(responseText))
                            {
                                Header = new PanelHeader($"[bold] Generating {length} Description [/]"),
                                Border = BoxBorder.Rounded,
                                BorderStyle = new Style(Color.Yellow),
                                Padding = new Padding(1, 0),
                                Expand = true
                            };
                            ctx.UpdateTarget(panel);
                        });
                    
                    _result.Descriptions[length] = description;
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
        
        await AnsiConsole.Live(new Panel(""))
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                var panel = new Panel(new Markup("[grey]Waiting for response...[/]"))
                {
                    Header = new PanelHeader("[bold] Generating Chapters [/]"),
                    Border = BoxBorder.Rounded,
                    BorderStyle = new Style(Color.Green),
                    Padding = new Padding(1, 0),
                    Expand = true
                };
                ctx.UpdateTarget(panel);
                
                _result.Chapters = await _generator!.GenerateChaptersAsync(
                    _transcript!,
                    chunk =>
                    {
                        responseText += chunk;
                        panel = new Panel(Markup.Escape(responseText))
                        {
                            Header = new PanelHeader("[bold] Generating Chapters [/]"),
                            Border = BoxBorder.Rounded,
                            BorderStyle = new Style(Color.Green),
                            Padding = new Padding(1, 0),
                            Expand = true
                        };
                        ctx.UpdateTarget(panel);
                    });
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
            var saveResult = await AnsiConsole.Status()
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
            
            if (saveResult.Errors.Count > 0)
            {
                ConsoleUI.ShowWarning($"Saved with {saveResult.Errors.Count} errors");
                foreach (var error in saveResult.Errors)
                {
                    AnsiConsole.MarkupLine($"  [red]• {Markup.Escape(error)}[/]");
                }
            }
            else
            {
                ConsoleUI.ShowSuccess($"Saved {saveResult.SavedFiles.Count} files to: {outputDir}");
                
                var table = new Table()
                    .RoundedBorder()
                    .BorderColor(Color.Green)
                    .AddColumn("Saved Files");
                
                foreach (var file in saveResult.SavedFiles)
                {
                    table.AddRow(Markup.Escape(Path.GetFileName(file)));
                }
                
                AnsiConsole.Write(table);
            }
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
            
            // Show current settings
            var settingsTable = new Table()
                .RoundedBorder()
                .BorderColor(Color.Blue)
                .Title("[bold]Current Settings[/]")
                .HideHeaders()
                .AddColumn("Setting")
                .AddColumn("Value");
            
            settingsTable.AddRow("[blue]Model[/]", Markup.Escape(_settings.Model));
            settingsTable.AddRow("[blue]Output Directory[/]", Markup.Escape(_settings.OutputDirectory));
            settingsTable.AddRow("[blue]Episode Context[/]", 
                string.IsNullOrEmpty(_settings.EpisodeContext) 
                    ? "[grey](not set)[/]" 
                    : Markup.Escape(_settings.EpisodeContext.Length > 50 
                        ? _settings.EpisodeContext[..50] + "..." 
                        : _settings.EpisodeContext));
            
            AnsiConsole.Write(settingsTable);
            
            var action = ConsoleUI.SelectFromList(
                "[bold]Settings[/]",
                new[] { "🤖 Change Model", "📁 Change Output Directory", "📝 Set Episode Context", "⬅️ Back to Main Menu" });
            
            switch (action)
            {
                case "🤖 Change Model":
                    // Fetch models from Copilot CLI
                    var models = await AnsiConsole.Status()
                        .Spinner(Spinner.Known.Dots)
                        .SpinnerStyle(Style.Parse("blue"))
                        .StartAsync("Fetching available models from Copilot CLI...", async ctx =>
                        {
                            return await AvailableModels.GetModelsFromCliAsync();
                        });
                    
                    _settings.Model = ConsoleUI.SelectFromList(
                        "Select AI Model:",
                        models);
                    ConsoleUI.ShowSuccess($"Model set to: {_settings.Model}");
                    break;
                    
                case "📁 Change Output Directory":
                    _settings.OutputDirectory = ConsoleUI.AskText(
                        "Enter output directory:",
                        defaultValue: _settings.OutputDirectory);
                    ConsoleUI.ShowSuccess($"Output directory set to: {_settings.OutputDirectory}");
                    break;
                    
                case "📝 Set Episode Context":
                    _settings.EpisodeContext = ConsoleUI.AskText(
                        "Enter episode context (guest names, topics, etc.):",
                        defaultValue: _settings.EpisodeContext ?? "");
                    if (string.IsNullOrWhiteSpace(_settings.EpisodeContext))
                        _settings.EpisodeContext = null;
                    ConsoleUI.ShowSuccess("Episode context updated");
                    break;
                    
                case "⬅️ Back to Main Menu":
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
    
    private async Task EnsureGeneratorInitializedAsync()
    {
        if (_generator == null)
        {
            _generator = new MetadataGenerator(_settings);
        }
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync("Initializing Copilot...", async ctx =>
            {
                await _generator.InitializeAsync();
            });
    }
    
    private async Task CleanupAsync()
    {
        if (_generator != null)
        {
            await _generator.DisposeAsync();
        }
    }
}
