using System.Diagnostics;
using CodexDeck.Protocol;

namespace CodexDeck.Companion;

/// <summary>Dispatches only the shortcut-mode actions verified on macOS.</summary>
public sealed class MacCodexAdapter : ICodexAdapter
{
    private static readonly HashSet<string> Supported = new(StringComparer.Ordinal)
    {
        "open_codex", "focus_codex", "new_task", "interrupt", "interrupt_double", "test_permissions", "open_permissions"
    };

    public Task<CapabilityProbe> ProbeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new CapabilityProbe(OperatingMode.Shortcut, Supported));
    }

    public Task<ActionReceipt> DispatchAsync(ActionIntent intent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Supported.Contains(intent.Action))
            return Task.FromResult(Receipt(intent, ProtocolConstants.Unavailable, ProtocolConstants.NotSupported));
        if (string.Equals(Environment.GetEnvironmentVariable("CODEX_DECK_DISABLE_AUTOMATION"), "1", StringComparison.Ordinal))
            return Task.FromResult(Receipt(intent, ProtocolConstants.Failed, ProtocolConstants.ExecutionFailed));

        var (file, args) = intent.Action switch
        {
            "open_codex" or "focus_codex" => ("open", new[] { "-a", "Codex" }),
            "new_task" => ("osascript", new[] { "-e", "tell application \"Codex\" to activate", "-e", "delay 0.25", "-e", "tell application \"System Events\" to keystroke \"n\" using command down" }),
            "interrupt" => ("osascript", new[] { "-e", "tell application \"System Events\" to key code 53" }),
            "interrupt_double" => ("osascript", new[] { "-e", "tell application \"System Events\" to key code 53", "-e", "delay 0.35", "-e", "tell application \"System Events\" to key code 53" }),
            "test_permissions" => ("osascript", new[] { "-e", "tell application \"System Events\" to get name of first process whose frontmost is true" }),
            "open_permissions" => ("open", new[] { "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility" }),
            _ => throw new InvalidOperationException()
        };

        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = file,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            foreach (var argument in args) startInfo.ArgumentList.Add(argument);
            using var process = Process.Start(startInfo);
            if (process is null) return Task.FromResult(Receipt(intent, ProtocolConstants.Failed, ProtocolConstants.ExecutionFailed));
            return Task.FromResult(Receipt(intent, ProtocolConstants.Accepted, null));
        }
        catch
        {
            return Task.FromResult(Receipt(intent, ProtocolConstants.Failed, ProtocolConstants.ExecutionFailed));
        }
    }

    private static ActionReceipt Receipt(ActionIntent intent, string status, string? reason) =>
        new(ProtocolConstants.ActionReceiptType, ProtocolConstants.CurrentVersion, intent.Id, status, reason);
}
