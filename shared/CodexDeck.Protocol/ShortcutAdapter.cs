namespace CodexDeck.Protocol;

public interface IFocusGuard
{
    bool IsCodexFocused();
}

public interface IShortcutDispatcher
{
    bool CanDispatch(string action);
    bool Dispatch(string action);
}

public sealed class ShortcutAdapter : ICodexAdapter
{
    private readonly IShortcutDispatcher _dispatcher;
    private readonly IFocusGuard _focusGuard;

    public ShortcutAdapter(IShortcutDispatcher dispatcher, IFocusGuard focusGuard)
    {
        _dispatcher = dispatcher;
        _focusGuard = focusGuard;
    }

    public Task<CapabilityProbe> ProbeAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var capabilities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var action in new[] { "open_codex", "interrupt", "accept", "reject" })
        {
            if (_dispatcher.CanDispatch(action))
            {
                capabilities.Add(action);
            }
        }

        var mode = capabilities.Count == 0 ? OperatingMode.SetupNeeded : OperatingMode.Shortcut;
        return Task.FromResult(new CapabilityProbe(mode, capabilities));
    }

    public Task<ActionReceipt> DispatchAsync(ActionIntent intent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_dispatcher.CanDispatch(intent.Action))
        {
            return Task.FromResult(new ActionReceipt(
                ProtocolConstants.ActionReceiptType,
                ProtocolConstants.CurrentVersion,
                intent.Id,
                ProtocolConstants.Unavailable,
                ProtocolConstants.NotSupported));
        }

        if (intent.Action is "accept" or "reject" or "interrupt" && !_focusGuard.IsCodexFocused())
        {
            return Task.FromResult(new ActionReceipt(
                ProtocolConstants.ActionReceiptType,
                ProtocolConstants.CurrentVersion,
                intent.Id,
                ProtocolConstants.Unavailable,
                ProtocolConstants.NotFocused));
        }

        var dispatched = _dispatcher.Dispatch(intent.Action);
        return Task.FromResult(new ActionReceipt(
            ProtocolConstants.ActionReceiptType,
            ProtocolConstants.CurrentVersion,
            intent.Id,
            dispatched ? ProtocolConstants.Accepted : ProtocolConstants.Failed,
            dispatched ? null : ProtocolConstants.ExecutionFailed));
    }
}
