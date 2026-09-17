namespace Loupedeck.CodexDeckPlugin;

using System.Diagnostics;
using CodexDeck.Protocol;

public sealed class InterruptCommand : PluginDynamicCommand
{
    private readonly ShortcutAdapter _adapter = new(new MacInterruptDispatcher(), new MacCodexFocusGuard());
    private string _status = "idle";
    private Timer _statusTimer;

    public InterruptCommand()
        : base("Interrupt Codex", "Cancel the active Codex operation", "Commands")
    {
    }

    protected override void RunCommand(string actionParameter)
    {
        var receipt = _adapter.DispatchAsync(new ActionIntent(
            ProtocolConstants.ActionIntentType,
            ProtocolConstants.CurrentVersion,
            Guid.NewGuid(),
            "interrupt"), CancellationToken.None).GetAwaiter().GetResult();
        _status = receipt.Status == ProtocolConstants.Accepted ? "interrupt_again" : receipt.Status;
        ActionImageChanged();
        _statusTimer?.Dispose();
        _statusTimer = new Timer(_ =>
        {
            _status = "idle";
            ActionImageChanged();
        }, null, TimeSpan.FromSeconds(5), Timeout.InfiniteTimeSpan);
        PluginLog.Info($"Interrupt Codex receipt: {receipt.Status}{(receipt.Reason is null ? string.Empty : $" ({receipt.Reason})")}");
    }

    protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
    {
        if (_status == "interrupt_again") return "Interrupt Codex\nPress again to interrupt";
        var view = ActionViewStateMapper.Map("Interrupt Codex", _status);
        return view.Label + Environment.NewLine + view.Feedback;
    }

    protected override BitmapImage GetCommandImage(string actionParameter, PluginImageSize imageSize)
    {
        var resource = _status == "interrupt_again" ? "interrupt-attention.png" : "openai.png";
        try { return PluginResources.ReadImage(resource); }
        catch (FileNotFoundException) { return PluginResources.ReadImage("openai.png"); }
    }
}

public sealed class InterruptDoubleCommand : PluginDynamicCommand
{
    private readonly ShortcutAdapter _adapter = new(new MacDoubleInterruptDispatcher(), new MacCodexFocusGuard());
    private string _status = "idle";

    public InterruptDoubleCommand()
        : base("Interrupt Codex (Double Press)", "Send the verified two-stage macOS interrupt", "Commands")
    {
    }

    protected override void RunCommand(string actionParameter)
    {
        var receipt = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, ProtocolConstants.CurrentVersion, Guid.NewGuid(), "interrupt_double"), CancellationToken.None).GetAwaiter().GetResult();
        _status = receipt.Status;
        ActionImageChanged();
        PluginLog.Info($"Interrupt Codex (Double Press) receipt: {receipt.Status}{(receipt.Reason is null ? string.Empty : $" ({receipt.Reason})")}");
    }

    protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize) =>
        ActionViewStateMapper.Map("Interrupt Codex (Double Press)", _status).Label + Environment.NewLine + ActionViewStateMapper.Map("Interrupt Codex (Double Press)", _status).Feedback;
}

internal sealed class MacCodexFocusGuard : IFocusGuard
{
    public bool IsCodexFocused()
    {
        try
        {
            var startInfo = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("tell application \"System Events\" to get bundle identifier of first process whose frontmost is true");
            using var process = Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd().Trim();
            process?.WaitForExit();
            return process?.ExitCode == 0 && string.Equals(output, "com.openai.codex", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class MacInterruptDispatcher : IShortcutDispatcher
{
    public bool CanDispatch(string action) => action == "interrupt";

    public bool Dispatch(string action)
    {
        try
        {
            var startInfo = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false, CreateNoWindow = true };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("tell application \"System Events\" to key code 53");
            using var process = Process.Start(startInfo);
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}

internal sealed class MacDoubleInterruptDispatcher : IShortcutDispatcher
{
    public bool CanDispatch(string action) => action == "interrupt_double";

    public bool Dispatch(string action)
    {
        try
        {
            var startInfo = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false, CreateNoWindow = true };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("tell application \"System Events\" to key code 53");
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("delay 0.35");
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("tell application \"System Events\" to key code 53");
            using var process = Process.Start(startInfo);
            process?.WaitForExit(5000);
            return process is not null && process.HasExited && process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
