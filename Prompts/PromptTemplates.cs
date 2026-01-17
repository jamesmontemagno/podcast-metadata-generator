using PodcastMetadataGenerator.Models;

namespace PodcastMetadataGenerator.Prompts;

/// <summary>
/// Contains all prompt templates for AI generation.
/// Based on prompts from https://github.com/jamesmontemagno/app-podcast-assistant/
/// </summary>
public static class PromptTemplates
{
    #region Title Generation
    
    public const string TitleSystemPrompt = """
        You are a creative podcast producer who writes engaging, concise episode titles.
        You analyze transcripts carefully to identify the main topics and create titles that 
        accurately reflect the content while being compelling and SEO-friendly.
        """;
    
    public static string GetTitleUserPrompt(string transcript, string? episodeContext = null)
    {
        var contextSection = string.IsNullOrEmpty(episodeContext) 
            ? "" 
            : $"""

            Additional context about this episode:
            {episodeContext}

            """;
        
        return $$"""
            You are generating titles for a podcast episode based on its transcript.

            Analyze the transcript and identify the topics that are discussed the most.
            Focus primarily on the main topics that take up the majority of the conversation.
            The title should reflect what listeners will spend most of their time hearing about.
            You can mention secondary topics briefly, but prioritize the core subject matter.
            {{contextSection}}
            Generate 5 creative, concise titles for this podcast episode.
            Keep titles under 10 words each.
            Make them engaging, descriptive, and SEO-friendly.

            Return ONLY the 5 titles, one per line, numbered 1-5. No additional commentary.

            Episode Transcript:
            {{transcript}}
            """;
    }
    
    #endregion
    
    #region Description Generation
    
    public const string DescriptionSystemPrompt = """
        You are a podcast producer who writes compelling episode descriptions.
        You craft descriptions that accurately summarize the content, highlight key insights,
        and entice listeners to tune in. Your descriptions are well-structured and engaging.
        """;
    
    public static string GetDescriptionUserPrompt(
        string transcript, 
        DescriptionLength length, 
        string? selectedTitle = null,
        string? episodeContext = null)
    {
        var lengthGuidance = length switch
        {
            DescriptionLength.Short => "in 2-3 sentences (50-75 words)",
            DescriptionLength.Medium => "in 1-2 paragraphs (100-150 words)",
            DescriptionLength.Long => "in 3-4 paragraphs (200-300 words)",
            _ => "in 1-2 paragraphs (100-150 words)"
        };
        
        var titleSection = string.IsNullOrEmpty(selectedTitle) 
            ? "" 
            : $"Episode title: {selectedTitle}\n\n";
        
        var contextSection = string.IsNullOrEmpty(episodeContext) 
            ? "" 
            : $"""

            Additional context about this episode:
            {episodeContext}

            """;
        
        return $$"""
            You are writing a compelling podcast episode description based on its transcript.

            Analyze the transcript carefully and identify:
            - The main topics that dominate the conversation (what takes up most of the time)
            - Key insights, valuable takeaways, or unique perspectives shared
            - The overall narrative or flow of the discussion
            {{contextSection}}
            Write a description {{lengthGuidance}} that:
            - Focuses primarily on the main topics discussed
            - Highlights the value and key takeaways for listeners
            - Uses engaging, conversational language
            - Captures what makes this episode worth listening to

            {{titleSection}}Return ONLY the description text, no additional commentary or labels.

            Episode Transcript:
            {{transcript}}
            """;
    }
    
    #endregion
    
    #region Chapter Generation
    
    public const string ChapterSystemPrompt = """
        You are a podcast editor who creates chapter markers based on topic transitions.
        You identify natural breakpoints in conversations where topics shift significantly.
        You create descriptive chapter titles that help listeners navigate the episode.
        You must use actual timecodes from the transcript where topics shift.
        """;
    
    public static string GetChapterUserPrompt(
        string transcriptWithTimestamps, 
        double durationSeconds,
        int targetChapters,
        string? episodeContext = null)
    {
        var contextSection = string.IsNullOrEmpty(episodeContext) 
            ? "" 
            : $"""

            Additional context about this episode:
            {episodeContext}

            """;
        
        return $$"""
            Create chapter markers for this podcast episode from the timestamped transcript below.
            {{contextSection}}
            Each line shows a time range followed by the speaker and text for that segment.
            Identify where MAJOR topic shifts occur and create chapters at those points.

            IMPORTANT: Be VERY selective - only create chapters for significant topic changes.
            Think of chapters as major sections viewers would skip to, not every minor topic.

            Guidelines:
            - Create approximately {{targetChapters}} chapters (minimum 3, maximum 12)
            - Only mark MAJOR topic transitions - ignore minor shifts
            - Each chapter should represent 5-10+ minutes of distinct content
            - First chapter MUST start at 00:00 (Introduction/Opening)
            - For each chapter, use the START timestamp from where the topic begins
            - Format timestamps as MM:SS or HH:MM:SS
            - Episode duration is about {{(int)durationSeconds}} seconds; do not emit timestamps beyond that
            - Distribute chapters evenly; avoid any gap > 10 minutes between chapters
            - Create descriptive titles (under 8 words)

            Return chapters in this EXACT format, one per line:
            MM:SS Title Here
            or
            HH:MM:SS Title Here

            Do not include any other text, numbering, or commentary.

            Timestamped Transcript:
            {{transcriptWithTimestamps}}
            """;
    }
    
    /// <summary>
    /// Calculates the target number of chapters based on episode duration.
    /// Formula: ~5 chapters per 30 minutes, min 3, max 12.
    /// </summary>
    public static int CalculateTargetChapters(double durationMinutes)
    {
        var target = (int)(durationMinutes / 30.0 * 5);
        return Math.Max(3, Math.Min(12, target));
    }
    
    #endregion
}
