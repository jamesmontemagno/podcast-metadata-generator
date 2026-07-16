namespace CopilotSdkLiveDemo.Helpers;

internal static class SelectionPrompt
{
    internal static int ReadIndex(string prompt, int optionCount, int defaultIndex)
    {
        while (true)
        {
            Console.Write(prompt);
            var answer = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(answer))
            {
                return defaultIndex;
            }

            if (int.TryParse(answer, out var selection) && selection >= 1 && selection <= optionCount)
            {
                return selection - 1;
            }

            Console.WriteLine($"Enter a number from 1 to {optionCount}.");
        }
    }
}