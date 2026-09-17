namespace Loupedeck.CodexDeckPlugin;

using CodexDeck.Protocol;

public sealed class InterruptCommand : PluginDynamicCommand
{
    private readonly CompanionAdapter _adapter = new();
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
    private readonly CompanionAdapter _adapter = new();
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
