namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using CodexDeck.Protocol;

    public sealed class FocusCodexCommand : PluginDynamicCommand
    {
        public FocusCodexCommand() : base("Focus Codex", "Bring Codex to the foreground (same as Open Codex on macOS)", "Commands###Core") { }

        protected override void RunCommand(string actionParameter)
        {
            using var process = Process.Start(new ProcessStartInfo { FileName = "open", Arguments = "-a Codex", UseShellExecute = false, CreateNoWindow = true });
            PluginLog.Info($"Focus Codex receipt: {(process is null ? "unavailable" : "accepted")}");
        }
    }

    public sealed class NewTaskCommand : PluginDynamicCommand
    {
        private readonly ShortcutAdapter _adapter = new(new NewTaskDispatcher(), new MacCodexFocusGuard());
        private string _status = "idle";

        public NewTaskCommand() : base("New Task", "Start a new Codex task", "Commands###Core") { }

        protected override void RunCommand(string actionParameter)
        {
            var receipt = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, ProtocolConstants.CurrentVersion, Guid.NewGuid(), "new_task"), CancellationToken.None).GetAwaiter().GetResult();
            _status = receipt.Reason == ProtocolConstants.NotFocused ? ProtocolConstants.Unavailable : receipt.Status;
            ActionImageChanged();
            PluginLog.Info($"New Task receipt: {receipt.Status}{(receipt.Reason is null ? string.Empty : $" ({receipt.Reason})")}");
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            var view = ActionViewStateMapper.Map("New Task", _status);
            var feedback = _status == ProtocolConstants.Unavailable ? "Permission needed — Open Settings" : view.Feedback;
            return view.Label + Environment.NewLine + feedback;
        }
    }

    internal sealed class NewTaskDispatcher : IShortcutDispatcher
    {
        public bool CanDispatch(string action) => action == "new_task";

        public bool Dispatch(string action)
        {
            var startInfo = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true };
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("tell application \"Codex\" to activate");
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("delay 0.25");
            startInfo.ArgumentList.Add("-e");
            startInfo.ArgumentList.Add("tell application \"System Events\" to keystroke \"n\" using command down");
            using var process = Process.Start(startInfo);
            if (process is null) return false;
            process.WaitForExit(5000);
            var succeeded = process.HasExited && process.ExitCode == 0;
            var error = process.HasExited ? process.StandardError.ReadToEnd().Trim() : string.Empty;
            PluginLog.Info($"New Task macOS automation: exited={process.HasExited}, code={(process.HasExited ? process.ExitCode : -1)}, success={succeeded}{(string.IsNullOrEmpty(error) ? string.Empty : $", error={error}")}");
            return succeeded;
        }
    }
}
