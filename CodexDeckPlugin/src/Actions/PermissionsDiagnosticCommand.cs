namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.Diagnostics;
    using CodexDeck.Protocol;

    public sealed class PermissionsDiagnosticCommand : PluginDynamicCommand
    {
        private PermissionState _state = PermissionState.Unknown;

        public PermissionsDiagnosticCommand() : base("Permissions & Connection", "Open macOS permissions and pairing guidance", "Diagnostics") { }

        protected override void RunCommand(string actionParameter)
        {
            var probeInfo = new ProcessStartInfo { FileName = "osascript", UseShellExecute = false, CreateNoWindow = true };
            probeInfo.ArgumentList.Add("-e");
            probeInfo.ArgumentList.Add("tell application \"System Events\" to get name of first process whose frontmost is true");
            using var probe = Process.Start(probeInfo);
            probe?.WaitForExit(2000);
            if (probe is not null && probe.HasExited && probe.ExitCode == 0)
            {
                _state = PermissionState.Ready;
                PluginLog.Info("Permissions probe succeeded");
                ActionImageChanged();
                return;
            }

            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = "x-apple.systempreferences:com.apple.preference.security?Privacy_Accessibility",
                UseShellExecute = false,
                CreateNoWindow = true,
            });
            process?.WaitForExit(2000);
            var opened = process is not null && process.HasExited && process.ExitCode == 0;
            PluginLog.Info($"Permissions settings launch: {(opened ? "accepted" : "unavailable")}");
            _state = opened ? PermissionState.AutomationNeeded : PermissionState.Unknown;
            ActionImageChanged();
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            var view = PermissionViewMapper.Map(_state);
            return view.Label + Environment.NewLine + view.Feedback;
        }
    }
}
