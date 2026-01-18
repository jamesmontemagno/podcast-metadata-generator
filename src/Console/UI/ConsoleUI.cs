using Spectre.Console;
using PodcastMetadataGenerator.Core.Models;
using PodcastMetadataGenerator.Core.Services;

namespace PodcastMetadataGenerator.Console.UI;

/// <summary>
/// Helper methods for Spectre.Console UI patterns.
/// </summary>
public static class ConsoleUI
{
    /// <summary>
    /// Shows the application header/banner with ASCII art.
    /// </summary>
    public static void ShowHeader(CopilotAuthService.CopilotStatus? copilotStatus = null)
    {
        AnsiConsole.Clear();
        
        // ASCII art logo
        var asciiArt = @"
[blue]    ██████╗  ██████╗ ██████╗  ██████╗ █████╗ ███████╗████████╗[/]
[blue]    ██╔══██╗██╔═══██╗██╔══██╗██╔════╝██╔══██╗██╔════╝╚══██╔══╝[/]
[cyan]    ██████╔╝██║   ██║██║  ██║██║     ███████║███████╗   ██║   [/]
[cyan]    ██╔═══╝ ██║   ██║██║  ██║██║     ██╔══██║╚════██║   ██║   [/]
[magenta]    ██║     ╚██████╔╝██████╔╝╚██████╗██║  ██║███████║   ██║   [/]
[magenta]    ╚═╝      ╚═════╝ ╚═════╝  ╚═════╝╚═╝  ╚═╝╚══════╝   ╚═╝   [/]
[grey]           ╔╦╗┌─┐┌┬┐┌─┐┌┬┐┌─┐┌┬┐┌─┐  ╔═╗┌─┐┌┐┌[/]
[grey]           ║║║├┤  │ ├─┤ ││├─┤ │ ├─┤  ║ ╦├┤ │││[/]
[grey]           ╩ ╩└─┘ ┴ ┴ ┴─┴┘┴ ┴ ┴ ┴ ┴  ╚═╝└─┘┘└┘[/]
";
        AnsiConsole.Markup(asciiArt);
        AnsiConsole.WriteLine();
        
        var rule = new Rule("[bold blue]🎙️ Podcast Metadata Generator[/]")
        {
            Justification = Justify.Center,
            Style = Style.Parse("blue")
        };
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
        
        // Show Copilot status if provided
        if (copilotStatus != null)
        {
            ShowCopilotStatus(copilotStatus);
        }
    }
    
    /// <summary>
    /// Shows Copilot CLI status.
    /// </summary>
    public static void ShowCopilotStatus(CopilotAuthService.CopilotStatus status)
    {
        AnsiConsole.MarkupLine("[bold]Copilot CLI Status[/]");
        AnsiConsole.WriteLine();
        
        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Check")
            .AddColumn("Status");
        
        table.AddRow(
            "Copilot CLI Installed",
            status.IsInstalled ? "[green]✓ Installed[/]" : "[red]✗ Not Installed[/]");
        
        table.AddRow(
            "GH_TOKEN Environment Variable",
            status.IsTokenSet ? "[green]✓ Set[/]" : "[grey]○ Not Set[/]");
        
        table.AddRow(
            "Authentication Status",
            status.IsAuthenticated ? "[green]✓ Authenticated[/]" : "[red]✗ Not Authenticated[/]");
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        
        // Show success message if ready
        if (status.IsInstalled && (status.IsTokenSet || status.IsAuthenticated))
        {
            AnsiConsole.MarkupLine("[green]✓ Copilot CLI is ready to use![/]");
            AnsiConsole.WriteLine();
            return;
        }
        
        if (!string.IsNullOrEmpty(status.ErrorMessage))
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {status.ErrorMessage}");
            AnsiConsole.WriteLine();
        }
        
        if (!status.IsInstalled)
        {
            AnsiConsole.MarkupLine("[yellow]To install Copilot CLI:[/]");
            AnsiConsole.MarkupLine("  [cyan]npm install -g @github/copilot[/]");
            AnsiConsole.MarkupLine("  [dim]or[/]");
            AnsiConsole.MarkupLine("  [cyan]brew install gh && gh extension install github/gh-copilot[/]");
            AnsiConsole.WriteLine();
        }
        
        if (status.IsInstalled && !status.IsAuthenticated && !status.IsTokenSet)
        {
            AnsiConsole.MarkupLine("[yellow]To authenticate:[/]");
            AnsiConsole.MarkupLine("  [cyan]copilot auth login[/]");
            AnsiConsole.MarkupLine("  [dim]or set the GH_TOKEN environment variable:[/]");
            AnsiConsole.MarkupLine("  [cyan]export GH_TOKEN=your_github_token[/]");
            AnsiConsole.WriteLine();
        }
    }
    
    /// <summary>
    /// Shows a success message.
    /// </summary>
    public static void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(message)}");
    }
    
    /// <summary>
    /// Shows an error message.
    /// </summary>
    public static void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(message)}");
    }
    
    /// <summary>
    /// Shows a warning message.
    /// </summary>
    public static void ShowWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]![/] {Markup.Escape(message)}");
    }
    
    /// <summary>
    /// Shows an info message.
    /// </summary>
    public static void ShowInfo(string message)
    {
        AnsiConsole.MarkupLine($"[blue]ℹ[/] {Markup.Escape(message)}");
    }
    
    /// <summary>
    /// Shows a panel with content.
    /// </summary>
    public static void ShowPanel(string title, string content, Color borderColor = default)
    {
        var color = borderColor == default ? Color.Blue : borderColor;
        
        var panel = new Panel(Markup.Escape(content))
        {
            Header = new PanelHeader($"[bold] {Markup.Escape(title)} [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(color),
            Padding = new Padding(2, 1),
            Expand = true
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
    
    /// <summary>
    /// Shows a panel with markup content.
    /// </summary>
    public static void ShowMarkupPanel(string title, string markupContent, Color borderColor = default)
    {
        var color = borderColor == default ? Color.Blue : borderColor;
        
        var panel = new Panel(new Markup(markupContent))
        {
            Header = new PanelHeader($"[bold] {Markup.Escape(title)} [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(color),
            Padding = new Padding(2, 1),
            Expand = true
        };
        
        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }
    
    /// <summary>
    /// Prompts user to select from a list of options.
    /// </summary>
    public static T SelectFromList<T>(string title, IEnumerable<T> choices, Func<T, string>? displaySelector = null) where T : notnull
    {
        var prompt = new SelectionPrompt<T>()
            .Title(title)
            .PageSize(10)
            .MoreChoicesText("[grey](Move up/down to see more)[/]")
            .HighlightStyle(Style.Parse("blue"))
            .AddChoices(choices);
        
        if (displaySelector != null)
        {
            prompt.UseConverter(displaySelector);
        }
        
        return AnsiConsole.Prompt(prompt);
    }
    
    /// <summary>
    /// Prompts user to select multiple items from a list.
    /// </summary>
    public static List<T> SelectMultiple<T>(string title, IEnumerable<T> choices, Func<T, string>? displaySelector = null) where T : notnull
    {
        var prompt = new MultiSelectionPrompt<T>()
            .Title(title)
            .PageSize(10)
            .MoreChoicesText("[grey](Move up/down to see more)[/]")
            .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
            .AddChoices(choices);
        
        if (displaySelector != null)
        {
            prompt.UseConverter(displaySelector);
        }
        
        return AnsiConsole.Prompt(prompt);
    }
    
    /// <summary>
    /// Prompts for text input with optional validation.
    /// </summary>
    public static string AskText(string prompt, string? defaultValue = null, Func<string, bool>? validator = null, string? validationMessage = null)
    {
        var textPrompt = new TextPrompt<string>(prompt);
        
        if (defaultValue != null)
        {
            textPrompt.DefaultValue(defaultValue);
        }
        
        if (validator != null)
        {
            textPrompt.Validate(value =>
            {
                if (validator(value))
                    return ValidationResult.Success();
                return ValidationResult.Error(validationMessage ?? "Invalid input");
            });
        }
        
        return AnsiConsole.Prompt(textPrompt);
    }
    
    /// <summary>
    /// Prompts for file path with existence validation and file browser option.
    /// Handles drag-and-drop paths that may have quotes or escape characters.
    /// </summary>
    public static string AskFilePath(string prompt, bool mustExist = true, string? startDirectory = null)
    {
        while (true)
        {
            // Offer browse option or direct input
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(prompt)
                    .AddChoices("📂 Browse for file", "⌨️ Type or paste path"));
            
            string path;
            
            if (choice.StartsWith("📂"))
            {
                var browsedPath = BrowseForFile(startDirectory);
                if (browsedPath == null)
                    continue; // User cancelled, show menu again
                path = browsedPath;
            }
            else
            {
                path = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter file path (drag & drop supported):")
                        .AllowEmpty());
                
                if (string.IsNullOrWhiteSpace(path))
                    continue;
                
                // Clean up drag-and-drop paths
                path = CleanFilePath(path);
            }
            
            if (mustExist && !File.Exists(path))
            {
                ShowError($"File not found: {path}");
                continue;
            }
            
            if (!mustExist || File.Exists(path))
            {
                return path;
            }
        }
    }
    
    /// <summary>
    /// Cleans a file path that may have been drag-and-dropped.
    /// Handles quotes, escaped spaces, and other common issues.
    /// </summary>
    private static string CleanFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;
        
        // Trim whitespace
        path = path.Trim();
        
        // Remove surrounding quotes (single or double)
        if ((path.StartsWith('"') && path.EndsWith('"')) ||
            (path.StartsWith('\'') && path.EndsWith('\'')))
        {
            path = path[1..^1];
        }
        
        // Handle escaped spaces (\ followed by space)
        path = path.Replace("\\ ", " ");
        
        // Handle other common escape sequences from terminal
        path = path.Replace("\\(", "(")
                   .Replace("\\)", ")")
                   .Replace("\\[", "[")
                   .Replace("\\]", "]")
                   .Replace("\\'", "'");
        
        return path;
    }
    
    /// <summary>
    /// Simple file browser using selection prompts.
    /// </summary>
    private static string? BrowseForFile(string? startDirectory = null)
    {
        var currentDir = startDirectory ?? Environment.CurrentDirectory;
        
        // Ensure directory exists
        if (!Directory.Exists(currentDir))
        {
            currentDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        
        while (true)
        {
            AnsiConsole.MarkupLine($"[grey]Current: {Markup.Escape(currentDir)}[/]");
            
            var items = new List<string> { "📁 .." };
            
            try
            {
                // Add directories
                var dirs = Directory.GetDirectories(currentDir)
                    .Select(d => new DirectoryInfo(d))
                    .Where(d => !d.Name.StartsWith('.')) // Hide hidden directories
                    .OrderBy(d => d.Name)
                    .Select(d => $"📁 {d.Name}");
                items.AddRange(dirs);
                
                // Add transcript files (common extensions)
                var files = Directory.GetFiles(currentDir)
                    .Select(f => new FileInfo(f))
                    .Where(f => !f.Name.StartsWith('.') && IsTranscriptFile(f.Name))
                    .OrderBy(f => f.Name)
                    .Select(f => $"📄 {f.Name}");
                items.AddRange(files);
            }
            catch (UnauthorizedAccessException)
            {
                ShowError("Access denied to this directory");
                currentDir = Directory.GetParent(currentDir)?.FullName ?? currentDir;
                continue;
            }
            
            items.Add("❌ Cancel");
            
            var selection = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Select a file or navigate:[/]")
                    .PageSize(15)
                    .MoreChoicesText("[grey](Move up/down to see more)[/]")
                    .HighlightStyle(Style.Parse("blue"))
                    .AddChoices(items));
            
            if (selection == "❌ Cancel")
            {
                return null;
            }
            else if (selection == "📁 ..")
            {
                var parent = Directory.GetParent(currentDir);
                if (parent != null)
                {
                    currentDir = parent.FullName;
                }
            }
            else if (selection.StartsWith("📁 "))
            {
                var dirName = selection[3..]; // Remove "📁 " prefix
                currentDir = Path.Combine(currentDir, dirName);
            }
            else if (selection.StartsWith("📄 "))
            {
                var fileName = selection[3..]; // Remove "📄 " prefix
                return Path.Combine(currentDir, fileName);
            }
        }
    }
    
    /// <summary>
    /// Checks if a file is likely a transcript file based on extension.
    /// </summary>
    private static bool IsTranscriptFile(string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext is ".txt" or ".srt" or ".vtt" or ".json" or ".md" or ".csv";
    }
    
    /// <summary>
    /// Shows a table of titles for selection.
    /// </summary>
    public static string? SelectTitle(List<string> titles)
    {
        if (titles.Count == 0) return null;
        
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Blue)
            .Title("[bold]Generated Titles[/]")
            .AddColumn("#", col => col.Centered())
            .AddColumn("Title");
        
        for (int i = 0; i < titles.Count; i++)
        {
            table.AddRow($"[blue]{i + 1}[/]", Markup.Escape(titles[i]));
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        
        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a title:")
                .AddChoices(titles.Concat(["[grey]Skip selection[/]"])));
        
        return choice.StartsWith("[grey]") ? null : choice;
    }
    
    /// <summary>
    /// Shows descriptions in a formatted way.
    /// </summary>
    public static void ShowDescriptions(Dictionary<DescriptionLength, string> descriptions)
    {
        foreach (var (length, description) in descriptions)
        {
            var color = length switch
            {
                DescriptionLength.Short => Color.Green,
                DescriptionLength.Medium => Color.Yellow,
                DescriptionLength.Long => Color.Blue,
                _ => Color.White
            };
            
            ShowPanel($"{length} Description", description, color);
        }
    }
    
    /// <summary>
    /// Shows chapters in a formatted table.
    /// </summary>
    public static void ShowChapters(List<Chapter> chapters)
    {
        if (chapters.Count == 0)
        {
            ShowWarning("No chapters generated.");
            return;
        }
        
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Green)
            .Title("[bold]YouTube Chapters[/]")
            .AddColumn("Timestamp", col => col.Centered())
            .AddColumn("Title");
        
        foreach (var chapter in chapters)
        {
            table.AddRow($"[blue]{Markup.Escape(chapter.Timestamp)}[/]", Markup.Escape(chapter.Title));
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        
        // Also show copy-paste format
        AnsiConsole.MarkupLine("[dim]Copy-paste format for YouTube:[/]");
        var rule = new Rule() { Style = Style.Parse("grey") };
        AnsiConsole.Write(rule);
        
        foreach (var chapter in chapters)
        {
            AnsiConsole.WriteLine($"{chapter.Timestamp} {chapter.Title}");
        }
        
        AnsiConsole.Write(rule);
        AnsiConsole.WriteLine();
    }
    
    /// <summary>
    /// Shows transcript info.
    /// </summary>
    public static void ShowTranscriptInfo(Transcript transcript)
    {
        var table = new Table()
            .RoundedBorder()
            .BorderColor(Color.Blue)
            .HideHeaders()
            .AddColumn("Property")
            .AddColumn("Value");
        
        table.AddRow("[blue]File[/]", Markup.Escape(Path.GetFileName(transcript.FilePath)));
        table.AddRow("[blue]Format[/]", transcript.Format.ToString());
        
        if (transcript.HasTimestamps)
        {
            table.AddRow("[blue]Duration[/]", $"{transcript.DurationMinutes:F1} minutes");
            table.AddRow("[blue]Segments[/]", transcript.Segments.Count.ToString());
        }
        else
        {
            // For plain text, show word count and estimated duration
            var wordCount = transcript.RawContent.Split([' ', '\n', '\r', '\t'], StringSplitOptions.RemoveEmptyEntries).Length;
            table.AddRow("[blue]Words[/]", $"{wordCount:N0}");
            table.AddRow("[blue]Est. Duration[/]", $"~{transcript.DurationMinutes:F0} minutes");
            table.AddRow("[grey]Note[/]", "[grey]Plain text - no timestamps detected[/]");
        }
        
        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }
    
    /// <summary>
    /// Shows a live display for streaming text.
    /// </summary>
    public static async Task<string> ShowStreamingResponseAsync(
        string title,
        Func<Action<string>, Task<string>> generator)
    {
        var result = string.Empty;
        var panel = new Panel("")
        {
            Header = new PanelHeader($"[bold] {Markup.Escape(title)} [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Blue),
            Padding = new Padding(1, 0),
            Expand = true
        };
        
        await AnsiConsole.Live(panel)
            .AutoClear(false)
            .StartAsync(async ctx =>
            {
                result = await generator(chunk =>
                {
                    panel = new Panel(Markup.Escape(result + chunk))
                    {
                        Header = new PanelHeader($"[bold] {Markup.Escape(title)} [/]"),
                        Border = BoxBorder.Rounded,
                        BorderStyle = new Style(Color.Blue),
                        Padding = new Padding(1, 0),
                        Expand = true
                    };
                    ctx.UpdateTarget(panel);
                });
            });
        
        return result;
    }
    
    /// <summary>
    /// Waits for user to press any key.
    /// </summary>
    public static void WaitForKey(string message = "Press any key to continue...")
    {
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(message)}[/]");
        System.Console.ReadKey(true);
    }
}
