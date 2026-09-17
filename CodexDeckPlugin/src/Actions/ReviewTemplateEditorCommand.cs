using Loupedeck;

namespace Loupedeck.CodexDeckPlugin;

/// Minimal host-registration probe for the verified Action Editor API.
public sealed class ReviewTemplateEditorCommand : ActionEditorCommand
{
    public ReviewTemplateEditorCommand() : base(DeviceType.All)
    {
        Name = "WorkflowTemplate";
        DisplayName = "Workflow Template";
        GroupName = "Commands###Workflows";
        Description = "Choose a configured workflow template.";
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
        e.AddItem("debug", "Debug", "Configured debug workflow");
        e.AddItem("refactor", "Refactor", "Configured refactor workflow");
        e.SetSelectedItemName("review");
    }
}
