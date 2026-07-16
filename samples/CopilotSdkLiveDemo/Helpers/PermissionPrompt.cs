using GitHub.Copilot;
using GitHub.Copilot.Rpc;

namespace CopilotSdkLiveDemo.Helpers;

internal static class PermissionPrompt
{
#pragma warning disable GHCP001 // The SDK's current permission callback returns this preview response type.
    internal static Task<PermissionDecision> RequestAsync(
        PermissionRequest request,
        PermissionInvocation invocation)
    {
        if (request is not PermissionRequestCustomTool tool)
        {
            return Task.FromResult(PermissionDecision.Reject(
                "This demo only permits its Merge Conflict episode lookup tool."));
        }

        Console.Write($"Approve {tool.ToolName}? [y/N] ");
        var answer = Console.ReadLine()?.Trim();

        return Task.FromResult(
            string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
                ? PermissionDecision.ApproveOnce()
                : PermissionDecision.Reject("The user did not approve the episode lookup."));
    }
#pragma warning restore GHCP001
}