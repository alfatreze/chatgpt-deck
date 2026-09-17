using System.Text.Json;

namespace CodexDeck.Protocol;

public sealed class ShortcutBindingStore
{
    private sealed record BindingDocument(int SchemaVersion, List<ShortcutBinding> Bindings);
    private readonly JsonSerializerOptions _options = ProtocolJson.Options;

    public IReadOnlyList<ShortcutBinding> Load(string path)
    {
        var loaded = TryLoad(path);
        if (loaded is not null) return loaded;
        loaded = TryLoad(path + ".bak");
        return loaded ?? ShortcutBindingDefaults.Actions;
    }

    private IReadOnlyList<ShortcutBinding>? TryLoad(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            List<ShortcutBinding>? bindings;
            if (json.TrimStart().StartsWith("[", StringComparison.Ordinal))
            {
                bindings = JsonSerializer.Deserialize<List<ShortcutBinding>>(json, _options);
            }
            else
            {
                var document = JsonSerializer.Deserialize<BindingDocument>(json, _options);
                bindings = document is { SchemaVersion: 1 } ? document.Bindings : null;
            }
            return bindings is { Count: > 0 } && bindings.All(ShortcutBindingValidator.IsValid)
                ? bindings
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void Save(string path, IEnumerable<ShortcutBinding> bindings)
    {
        var values = bindings.ToList();
        if (values.Count == 0 || values.Any(binding => !ShortcutBindingValidator.IsValid(binding)))
        {
            throw new ArgumentException("Binding set is empty or contains an invalid binding.", nameof(bindings));
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        if (File.Exists(path)) File.Copy(path, path + ".bak", true);
        var temporary = path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(new BindingDocument(1, values), _options));
        File.Move(temporary, path, true);
    }

    public void Export(string path, string destination)
    {
        var bindings = Load(path);
        Save(destination, bindings);
    }
}
