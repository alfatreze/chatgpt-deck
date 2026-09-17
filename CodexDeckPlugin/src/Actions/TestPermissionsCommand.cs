#nullable enable

namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.Threading;
    using CodexDeck.Protocol;

    public sealed class TestPermissionsCommand : PluginDynamicCommand
    {
        private string _status = "idle";
        private Timer? _statusTimer;
        public TestPermissionsCommand() : base("Test Permissions", "Recheck macOS automation access", "Diagnostics") { }

        protected override void RunCommand(string actionParameter)
        {
            var receipt = new CompanionAdapter().DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, ProtocolConstants.CurrentVersion, Guid.NewGuid(), "test_permissions"), CancellationToken.None).GetAwaiter().GetResult();
            _status = receipt.Status == ProtocolConstants.Accepted ? "succeeded" : receipt.Status;
            ActionImageChanged();
            _statusTimer?.Dispose();
            _statusTimer = new Timer(_ => { _status = "idle"; ActionImageChanged(); }, null, TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
            PluginLog.Info($"Permissions test: {receipt.Status}");
        }

        protected override string GetCommandDisplayName(string actionParameter, PluginImageSize imageSize)
        {
            var view = CodexDeck.Protocol.ActionViewStateMapper.Map("Test Permissions", _status);
            return view.Label + Environment.NewLine + (_status == "succeeded" ? "Permissions checked" : view.Feedback);
        }
    }
}
