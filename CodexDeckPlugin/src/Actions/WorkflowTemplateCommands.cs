using System;
using System.Threading;
using CodexDeck.Protocol;

namespace Loupedeck.CodexDeckPlugin;

/// Capability-gated workflow placeholders. They remain visibly unavailable until a verified adapter is configured.
public sealed class ReviewWorkflowCommand : PluginDynamicCommand
{
    private readonly CompanionAdapter _adapter = new();
    private string _status = "idle";
    public ReviewWorkflowCommand() : base("Review", "Launch the configured review workflow", "Commands###Workflows") { }
    protected override void RunCommand(string actionParameter) => Dispatch("launch_review");
    private void Dispatch(string action) { var r = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, 1, Guid.NewGuid(), action), CancellationToken.None).GetAwaiter().GetResult(); _status = r.Status; ActionImageChanged(); }
    protected override string GetCommandDisplayName(string p, PluginImageSize s) => ActionViewStateMapper.Map("Review", _status).Label + Environment.NewLine + ActionViewStateMapper.Map("Review", _status).Feedback;
}

public sealed class DebugWorkflowCommand : PluginDynamicCommand
{
    private readonly CompanionAdapter _adapter = new(); private string _status = "idle";
    public DebugWorkflowCommand() : base("Debug", "Launch the configured debug workflow", "Commands###Workflows") { }
    protected override void RunCommand(string p) { var r = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, 1, Guid.NewGuid(), "launch_debug"), CancellationToken.None).GetAwaiter().GetResult(); _status = r.Status; ActionImageChanged(); }
    protected override string GetCommandDisplayName(string p, PluginImageSize s) => ActionViewStateMapper.Map("Debug", _status).Label + Environment.NewLine + ActionViewStateMapper.Map("Debug", _status).Feedback;
}

public sealed class RefactorWorkflowCommand : PluginDynamicCommand
{
    private readonly CompanionAdapter _adapter = new(); private string _status = "idle";
    public RefactorWorkflowCommand() : base("Refactor", "Launch the configured refactor workflow", "Commands###Workflows") { }
    protected override void RunCommand(string p) { var r = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, 1, Guid.NewGuid(), "launch_refactor"), CancellationToken.None).GetAwaiter().GetResult(); _status = r.Status; ActionImageChanged(); }
    protected override string GetCommandDisplayName(string p, PluginImageSize s) => ActionViewStateMapper.Map("Refactor", _status).Label + Environment.NewLine + ActionViewStateMapper.Map("Refactor", _status).Feedback;
}
