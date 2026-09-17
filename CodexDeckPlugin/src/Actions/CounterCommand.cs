namespace Loupedeck.CodexDeckPlugin
{
    using System;
    using CodexDeck.Protocol;

    // Opens the Codex desktop app without fabricating a task result.

    public class OpenCodexCommand : PluginDynamicCommand
    {
        private readonly ICodexAdapter _adapter;
        private string _status = "idle";
        private Timer _statusTimer;

        public OpenCodexCommand()
            : base(displayName: "Open Codex", description: "Open or focus the Codex desktop app", groupName: "Commands")
        {
            _adapter = new CompanionAdapter();
        }

        protected override void RunCommand(String actionParameter)
        {
            try
            {
                var receipt = _adapter.DispatchAsync(new ActionIntent(
                    ProtocolConstants.ActionIntentType,
                    ProtocolConstants.CurrentVersion,
                    Guid.NewGuid(),
                    "open_codex"), CancellationToken.None).GetAwaiter().GetResult();
                _status = receipt.Status;
                ActionImageChanged();
                _statusTimer?.Dispose();
                _statusTimer = new Timer(_ =>
                {
                    _status = "idle";
                    ActionImageChanged();
                }, null, TimeSpan.FromSeconds(3), Timeout.InfiniteTimeSpan);
                PluginLog.Info($"Open Codex receipt: {receipt.Status}");
            }
            catch (Exception exception)
            {
                PluginLog.Error($"Open Codex failed: {exception.Message}");
            }
        }

        protected override String GetCommandDisplayName(String actionParameter, PluginImageSize imageSize) =>
            ActionViewStateMapper.Map("Open Codex", _status).Label + Environment.NewLine + ActionViewStateMapper.Map("Open Codex", _status).Feedback;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) =>
            PluginResources.ReadImage("openai.png");
    }

}
