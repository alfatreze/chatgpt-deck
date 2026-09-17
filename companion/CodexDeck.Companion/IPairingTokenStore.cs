namespace CodexDeck.Companion;

public interface IPairingTokenStore
{
    string GetOrCreate(string path);
}
