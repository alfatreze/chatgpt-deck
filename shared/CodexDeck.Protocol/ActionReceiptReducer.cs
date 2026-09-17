namespace CodexDeck.Protocol;

public sealed class ActionReceiptReducer
{
    private readonly Dictionary<Guid, string> _pending = new();

    public void Begin(ActionIntent intent) => _pending[intent.Id] = "sending";

    public bool Apply(ActionReceipt receipt)
    {
        if (receipt.ProtocolVersion != ProtocolConstants.CurrentVersion) return false;
        if (!_pending.ContainsKey(receipt.Id)) return false;
        if (receipt.Status is not (ProtocolConstants.Accepted or ProtocolConstants.Unavailable or ProtocolConstants.Failed)) return false;
        _pending[receipt.Id] = receipt.Status;
        return true;
    }

    public string? Get(Guid id) => _pending.TryGetValue(id, out var status) ? status : null;
}
