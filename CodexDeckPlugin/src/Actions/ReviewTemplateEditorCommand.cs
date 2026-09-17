using Loupedeck;

namespace Loupedeck.CodexDeckPlugin;

/// Minimal host-registration probe for the verified Action Editor API.
public sealed class ReviewTemplateEditorCommand : ActionEditorCommand
{
    public ReviewTemplateEditorCommand() : base(DeviceType.All)
    {
        Name = "ReviewTemplate";
        DisplayName = "Review Workflow";
        GroupName = "Commands###Workflows";
        Description = "Choose the configured review workflow.";
        ActionEditor.AddControlEx(new ActionEditorListbox("Template", "Template", "Workflow template"));
        ActionEditor.ListboxItemsRequested += OnListboxItemsRequested;
    }

    protected override bool RunCommand(ActionEditorActionParameters actionParameters)
    {
        return actionParameters.TryGetString("Template", out var template) && !string.IsNullOrWhiteSpace(template);
    }

    private static void OnListboxItemsRequested(object sender, ActionEditorListboxItemsRequestedEventArgs e)
    {
        if (!e.ControlName.Equals("Template", StringComparison.OrdinalIgnoreCase)) return;
        e.AddItem("review", "Review", "Configured review workflow");
        e.SetSelectedItemName("review");
    }
}

public sealed class DebugTemplateEditorCommand : ReviewTemplateEditorCommandBase
{
    public DebugTemplateEditorCommand() : base("DebugTemplate", "Debug Workflow", "debug") { }
}

public sealed class RefactorTemplateEditorCommand : ReviewTemplateEditorCommandBase
{
    public RefactorTemplateEditorCommand() : base("RefactorTemplate", "Refactor Workflow", "refactor") { }
}

public abstract class ReviewTemplateEditorCommandBase : ActionEditorCommand
{
    private readonly string _default;
    protected ReviewTemplateEditorCommandBase(string name, string displayName, string defaultTemplate) : base(DeviceType.All)
    {
        _default = defaultTemplate; Name = name; DisplayName = displayName; GroupName = "Commands###Workflows";
        Description = "Choose the configured workflow template.";
        ActionEditor.AddControlEx(new ActionEditorListbox("Template", "Template", "Workflow template"));
        ActionEditor.ListboxItemsRequested += OnItems;
    }
    private void OnItems(object sender, ActionEditorListboxItemsRequestedEventArgs e)
    {
        if (!e.ControlName.Equals("Template", StringComparison.OrdinalIgnoreCase)) return;
        e.AddItem(_default, _default, "Configured workflow template"); e.SetSelectedItemName(_default);
    }
    protected override bool RunCommand(ActionEditorActionParameters p) => p.TryGetString("Template", out var value) && !string.IsNullOrWhiteSpace(value);
}
