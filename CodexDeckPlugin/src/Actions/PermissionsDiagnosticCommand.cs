namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.Threading;
    using CodexDeck.Protocol;

    public sealed class PermissionsDiagnosticCommand : PluginDynamicCommand
    {
        private PermissionState _state = PermissionState.Unknown;

        public PermissionsDiagnosticCommand() : base("Permissions & Connection", "Open macOS permissions and pairing guidance", "Diagnostics") { }

        protected override void RunCommand(string actionParameter)
        {
            var receipt = new CompanionAdapter().DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, ProtocolConstants.CurrentVersion, Guid.NewGuid(), "test_permissions"), CancellationToken.None).GetAwaiter().GetResult();
            if (receipt.Status == ProtocolConstants.Accepted)
            {
                _state = PermissionState.Ready;
                PluginLog.Info("Permissions probe succeeded");
                ActionImageChanged();
                return;
            }

            var openedReceipt = new CompanionAdapter().DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, ProtocolConstants.CurrentVersion, Guid.NewGuid(), "open_permissions"), CancellationToken.None).GetAwaiter().GetResult();
            var opened = openedReceipt.Status == ProtocolConstants.Accepted;
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
