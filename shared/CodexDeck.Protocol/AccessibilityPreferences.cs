namespace CodexDeck.Protocol;

public sealed record AccessibilityPreferences(
    bool ReducedMotion = false,
    bool HighContrast = false,
    bool TextFeedback = true);
