namespace CodexDeck.Protocol;

public sealed record ShortcutBinding(string Action, string? KeyStroke, bool Enabled = true);

public static class ShortcutBindingDefaults
{
    public static readonly IReadOnlyList<ShortcutBinding> Actions = new[]
    {
        new ShortcutBinding("open_codex", null),
        new ShortcutBinding("interrupt", null),
        new ShortcutBinding("accept", null),
        new ShortcutBinding("reject", null),
        new ShortcutBinding("continue_new_task", null),
    };
}

public static class ShortcutBindingValidator
{
    public static bool IsValid(ShortcutBinding binding) =>
        !string.IsNullOrWhiteSpace(binding.Action)
        && (binding.KeyStroke is null || binding.KeyStroke.Length <= 80)
        && binding.Action.All(character => char.IsLetterOrDigit(character) || character == '_');
}
