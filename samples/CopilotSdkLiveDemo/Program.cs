using GitHub.Copilot;

const string Model = "gpt-5.4-mini";

Console.WriteLine("Copilot SDK hello world\n");

// Step 1: Start the Copilot client.
CopilotClient client;

// Step 2: Check that Copilot is authenticated.
var isAuthenticated = false;

if (!isAuthenticated)
{
    Console.WriteLine("Copilot is not authenticated. Run 'copilot auth login' and try again.");
    return;
}

var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

// Step 3: Create a streaming session.
CopilotSession session = null!;

// Step 4: Stream events from the assistant.
session.On<SessionEvent>(evt =>
{
    switch (evt)
    {
        case AssistantMessageDeltaEvent delta:
            Console.Write(delta.Data.DeltaContent);
            break;
        case ToolExecutionStartEvent:
            Console.WriteLine("\n[Tool call started]");
            break;
        case ToolExecutionCompleteEvent:
            Console.WriteLine("\n[Tool call complete]\n");
            break;
        case SessionErrorEvent error:
            complete.TrySetException(new InvalidOperationException(error.Data.Message));
            break;
        case SessionIdleEvent:
            complete.TrySetResult();
            break;
    }
});

Console.WriteLine($"Using model: {Model}\n");

// Step 5: Send the first message.


await complete.Task;
