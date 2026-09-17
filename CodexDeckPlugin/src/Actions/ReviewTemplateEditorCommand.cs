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
    }

    protected override bool RunCommand(ActionEditorActionParameters actionParameters)
    {
        return actionParameters.TryGetString("Template", out var template) && !string.IsNullOrWhiteSpace(template);
    }
}
