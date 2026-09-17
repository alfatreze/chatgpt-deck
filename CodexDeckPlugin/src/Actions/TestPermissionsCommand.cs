#nullable enable

namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.Diagnostics;

    public sealed class TestPermissionsCommand : PluginDynamicCommand
    {
        private string _status = "idle";
        private Timer? _statusTimer;
        public TestPermissionsCommand() : base("Test Permissions", "Recheck macOS automation access", "Diagnostics") { }

        protected override void RunCommand(string actionParameter)
        {
            var info = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false, CreateNoWindow = true };
            info.ArgumentList.Add("-e");
            info.ArgumentList.Add("tell application \"System Events\" to get name of first process whose frontmost is true");
            using var process = Process.Start(info);
            process?.WaitForExit(2000);
            var ready = process is not null && process.HasExited && process.ExitCode == 0;
            _status = ready ? "succeeded" : "unavailable";
            ActionImageChanged();
            _statusTimer?.Dispose();
            _statusTimer = new Timer(_ => { _status = "idle"; ActionImageChanged(); }, null, TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
            PluginLog.Info($"Permissions test: {(ready ? "ready" : "permission_needed")}");
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            var view = CodexDeck.Protocol.ActionViewStateMapper.Map("Test Permissions", _status);
            return view.Label + Environment.NewLine + (_status == "succeeded" ? "Permissions checked" : view.Feedback);
        }
    }
}
