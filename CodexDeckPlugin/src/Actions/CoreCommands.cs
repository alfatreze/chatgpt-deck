namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using System.Threading;
    using CodexDeck.Protocol;

    public sealed class FocusCodexCommand : PluginDynamicCommand
    {
        private readonly CompanionAdapter _adapter = new();
        public FocusCodexCommand() : base("Focus Codex", "Bring Codex to the foreground (same as Open Codex on macOS)", "Commands###Core") { }

        protected override void RunCommand(string actionParameter)
        {
            var receipt = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, ProtocolConstants.CurrentVersion, Guid.NewGuid(), "focus_codex"), CancellationToken.None).GetAwaiter().GetResult();
            PluginLog.Info($"Focus Codex receipt: {receipt.Status}");
        }
    }

    public sealed class NewTaskCommand : PluginDynamicCommand
    {
        private readonly CompanionAdapter _adapter = new();
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

}
