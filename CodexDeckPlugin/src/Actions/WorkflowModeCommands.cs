using System;
using System.Threading;
using CodexDeck.Protocol;

namespace Loupedeck.CodexDeckPlugin;

public sealed class FastModeCommand : PluginDynamicCommand
{
    private readonly CompanionAdapter _adapter = new(); private string _status = "unavailable";
    public FastModeCommand() : base("Fast Mode", "Toggle the configured Fast Mode shortcut", "Commands###Core") { ActionImageChanged(); }
    protected override void RunCommand(string p) { _status = "checking"; ActionImageChanged(); var r = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, 1, Guid.NewGuid(), "fast_mode_toggle"), CancellationToken.None).GetAwaiter().GetResult(); _status = r.Status; ActionImageChanged(); }
    protected override string GetCommandDisplayName(string p, PluginImageSize s) => ActionViewStateMapper.Map("Fast Mode", _status).Label + Environment.NewLine + ActionViewStateMapper.Map("Fast Mode", _status).Feedback;
    protected override BitmapImage GetCommandImage(string p, PluginImageSize s) => StatusImages.For(_status);
}

public sealed class ContinueNewTaskCommand : PluginDynamicCommand
{
    private readonly CompanionAdapter _adapter = new(); private string _status = "unavailable";
    public ContinueNewTaskCommand() : base("Continue in New Task", "Continue using a new Codex task", "Commands###Core") { ActionImageChanged(); }
    protected override void RunCommand(string p) { _status = "checking"; ActionImageChanged(); var r = _adapter.DispatchAsync(new ActionIntent(ProtocolConstants.ActionIntentType, 1, Guid.NewGuid(), "continue_new_task"), CancellationToken.None).GetAwaiter().GetResult(); _status = r.Status; ActionImageChanged(); }
    protected override string GetCommandDisplayName(string p, PluginImageSize s) => ActionViewStateMapper.Map("Continue in New Task", _status).Label + Environment.NewLine + ActionViewStateMapper.Map("Continue in New Task", _status).Feedback;
    protected override BitmapImage GetCommandImage(string p, PluginImageSize s) => StatusImages.For(_status);
}
