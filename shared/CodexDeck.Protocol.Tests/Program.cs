using CodexDeck.Protocol;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

var dispatcher = new FakeDispatcher();
var focus = new FakeFocus { IsFocused = false };
var adapter = new ShortcutAdapter(dispatcher, focus);

var probe = await adapter.ProbeAsync(CancellationToken.None);
Assert(probe.Mode == OperatingMode.Shortcut, "probe should select shortcut mode");
Assert(probe.Capabilities.Contains("accept"), "accept should be advertised");

var blocked = await adapter.DispatchAsync(Intent("accept"), CancellationToken.None);
Assert(blocked.Status == "unavailable" && blocked.Reason == "not_focused", "focus guard should block accept");
Assert(dispatcher.DispatchCount == 0, "blocked action must not dispatch");

focus.IsFocused = true;
var accepted = await adapter.DispatchAsync(Intent("accept"), CancellationToken.None);
Assert(accepted.Status == "accepted", "focused action should be accepted");
Assert(dispatcher.DispatchCount == 1, "focused action should dispatch once");

var unsupported = await adapter.DispatchAsync(Intent("new_task"), CancellationToken.None);
Assert(unsupported.Status == "unavailable" && unsupported.Reason == "not_supported", "unsupported action should be unavailable");

var store = new ShortcutBindingStore();
Assert(ShortcutBindingValidator.IsValid(new ShortcutBinding("open_codex", null)), "default binding should be valid");
Assert(!ShortcutBindingValidator.IsValid(new ShortcutBinding("bad action", "Escape")), "spaces in action IDs should be rejected");
Assert(!ShortcutBindingValidator.IsValid(new ShortcutBinding("open_codex", new string('x', 81))), "oversized keystrokes should be rejected");
var configPath = Path.Combine(Path.GetTempPath(), $"codex-deck-{Guid.NewGuid():N}.json");
store.Save(configPath, new[] { new ShortcutBinding("open_codex", "Command+Space") });
Assert(store.Load(configPath)[0].KeyStroke == "Command+Space", "saved binding should round-trip");
store.Save(configPath, new[] { new ShortcutBinding("open_codex", "Command+O") });
Assert(File.Exists(configPath + ".bak"), "save should preserve a backup");
var exportPath = configPath + ".export.json";
store.Export(configPath, exportPath);
Assert(store.Load(exportPath)[0].KeyStroke == "Command+O", "export should round-trip");
File.WriteAllText(configPath, "[{\"action\":\"open_codex\",\"keyStroke\":\"Command+L\",\"enabled\":true}]");
Assert(store.Load(configPath)[0].KeyStroke == "Command+L", "legacy list format should load");
store.Save(configPath, new[] { new ShortcutBinding("open_codex", "Command+O") });
File.WriteAllText(configPath, "not-json");
Assert(store.Load(configPath)[0].KeyStroke == "Command+L", "malformed config should recover from backup");
File.WriteAllText(configPath, "{\"schemaVersion\":99,\"bindings\":[]}");
Assert(store.Load(configPath).Count > 0, "unsupported schema should recover to defaults");
File.Delete(configPath);
File.Delete(configPath + ".bak");
File.Delete(exportPath);

var reducer = new StateSnapshotReducer();
var currentSnapshot = new StateSnapshot("state.snapshot", 1, "shortcut", Array.Empty<string>(), DateTimeOffset.UtcNow, new Dictionary<string, ActionState>());
var olderSnapshot = currentSnapshot with { UpdatedAt = currentSnapshot.UpdatedAt.AddSeconds(-1) };
var invalidSnapshot = currentSnapshot with { Actions = new Dictionary<string, ActionState> { ["open_codex"] = new(true, "bogus") } };
Assert(!reducer.Apply(invalidSnapshot), "invalid action status should be rejected");
var unadvertisedSnapshot = currentSnapshot with { Actions = new Dictionary<string, ActionState> { ["hidden"] = new(true, "idle") } };
Assert(!reducer.Apply(unadvertisedSnapshot), "unadvertised enabled action should be rejected");
Assert(reducer.Apply(currentSnapshot), "current snapshot should apply");
Assert(!reducer.Apply(olderSnapshot), "older snapshot should be rejected");
Assert(!reducer.IsStale(currentSnapshot.UpdatedAt.AddSeconds(1), TimeSpan.FromSeconds(10)), "fresh snapshot should not be stale");
Assert(reducer.IsStale(currentSnapshot.UpdatedAt.AddSeconds(11), TimeSpan.FromSeconds(10)), "old snapshot should be stale");
Assert(reducer.GetFreshness(currentSnapshot.UpdatedAt.AddSeconds(1)) == "fresh", "freshness should be fresh");
Assert(reducer.GetFreshness(currentSnapshot.UpdatedAt.AddSeconds(31)) == "unknown", "freshness should be unknown");
reducer.Clear();
Assert(reducer.Current is null && reducer.IsStale(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(10)), "disconnect should clear state");
Assert(reducer.GetFreshness(DateTimeOffset.UtcNow) == "disconnected", "cleared state should be disconnected");

var receiptReducer = new ActionReceiptReducer();
var trackedIntent = Intent("open_codex");
receiptReducer.Begin(trackedIntent);
Assert(receiptReducer.Apply(new ActionReceipt("action.receipt", 1, trackedIntent.Id, "accepted")), "matching receipt should apply");
Assert(receiptReducer.Get(trackedIntent.Id) == "accepted", "receipt status should be tracked");
Assert(!receiptReducer.Apply(new ActionReceipt("action.receipt", 1, Guid.NewGuid(), "accepted")), "unknown receipt should be rejected");
var incompatibleIntent = Intent("interrupt");
receiptReducer.Begin(incompatibleIntent);
Assert(!receiptReducer.Apply(new ActionReceipt("action.receipt", 99, incompatibleIntent.Id, "accepted")), "incompatible receipt should be rejected");
Assert(!ActionViewStateMapper.Map("Interrupt Codex", "unavailable").Enabled, "unavailable action should be disabled");
Assert(ActionViewStateMapper.Map("Open Codex", "accepted").Feedback == "Accepted", "accepted action should have text feedback");
Assert(!ActionViewStateMapper.Map("Open Codex", "unknown").Enabled, "unknown status should fail closed");
Assert(ActionViewStateMapper.Map("Open Codex", "succeeded").Feedback == "Complete", "succeeded status should map to complete");
var accessibility = new AccessibilityPreferences(ReducedMotion: true, HighContrast: true);
Assert(accessibility.ReducedMotion && accessibility.TextFeedback, "accessibility defaults should preserve text feedback");
Assert(PermissionViewMapper.Map(PermissionState.AccessibilityNeeded).Label == "Allow Accessibility", "permission mapper should guide accessibility setup");
Assert(PermissionViewMapper.Map(PermissionState.AutomationNeeded).Label == "Allow Automation", "permission mapper should guide automation setup");
Assert(PermissionViewMapper.Map(PermissionState.Ready).Feedback == "Connected", "permission mapper should show connected state");
var now = DateTimeOffset.UtcNow;
Assert(UsageAlertMapper.Map(80, true, now, now) == UsageAlertLevel.Healthy, "usage healthy threshold");
Assert(UsageAlertMapper.Map(20, true, now, now) == UsageAlertLevel.Warning, "usage warning threshold");
Assert(UsageAlertMapper.Map(0, true, now, now) == UsageAlertLevel.Exhausted, "usage exhausted threshold");
Assert(UsageAlertMapper.Map(80, false, now, now) == UsageAlertLevel.Unknown, "unsupported usage is unknown");
Assert(UsageAlertMapper.Map(80, true, now.AddMinutes(-6), now) == UsageAlertLevel.Unknown, "stale usage is unknown");
Assert(UsageLayoutValidator.IsValid("combined") && UsageLayoutValidator.IsValid("separate"), "usage layouts should validate");
Assert(!UsageLayoutValidator.IsValid("daily"), "unknown usage layout should be rejected");
Assert(UsageAlertMapper.Map(30, true, now, now, 35) == UsageAlertLevel.Warning, "custom usage threshold should apply");
Assert(DiagnosticsViewMapper.Map(new DiagnosticsSnapshot(PermissionState.Ready, true, now), now) == "Connected", "diagnostics connected label");
Assert(DiagnosticsViewMapper.Map(new DiagnosticsSnapshot(PermissionState.AutomationNeeded, false, now), now) == "Allow Automation", "diagnostics permission label");

var server = new TcpListener(IPAddress.Loopback, 0);
server.Start();
var serverTask = Task.Run(async () =>
{
    using var client = await server.AcceptTcpClientAsync();
    using var reader = new StreamReader(client.GetStream());
    await using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
    await reader.ReadLineAsync();
    await writer.WriteLineAsync("{\"type\":\"hello.ack\",\"protocolVersion\":1,\"snapshot\":{\"type\":\"state.snapshot\",\"protocolVersion\":1,\"mode\":\"shortcut\",\"capabilities\":[\"open_codex\"],\"updatedAt\":\"2026-09-17T00:00:00Z\",\"actions\":{\"open_codex\":{\"enabled\":true,\"status\":\"idle\"}}}}");
    var intent = JsonDocument.Parse(await reader.ReadLineAsync() ?? "{}");
    await writer.WriteLineAsync($"{{\"type\":\"action.receipt\",\"protocolVersion\":1,\"id\":\"{intent.RootElement.GetProperty("id").GetString()}\",\"status\":\"accepted\"}}");
});
await using (var client = new LoopbackClient())
{
    await client.ConnectAsync(((IPEndPoint)server.LocalEndpoint).Port, null, CancellationToken.None);
    var snapshot = await client.ReadHelloSnapshotAsync(CancellationToken.None);
    Assert(snapshot.Mode == "shortcut", "hello snapshot should select shortcut mode");
    var receipt = await client.SendIntentAsync(Intent("open_codex"), CancellationToken.None);
    Assert(receipt.Status == "accepted", "integration receipt should be accepted");
}
await serverTask;
server.Stop();

Console.WriteLine("Validated ShortcutAdapter, binding store, and loopback client integration");

static ActionIntent Intent(string action) => new(
    ProtocolConstants.ActionIntentType,
    ProtocolConstants.CurrentVersion,
    Guid.NewGuid(),
    action);

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

sealed class FakeDispatcher : IShortcutDispatcher
{
    public int DispatchCount { get; private set; }
    public bool CanDispatch(string action) => action is "open_codex" or "interrupt" or "accept" or "reject";
    public bool Dispatch(string action) { DispatchCount++; return true; }
}

sealed class FakeFocus : IFocusGuard
{
    public bool IsFocused { get; set; }
    public bool IsCodexFocused() => IsFocused;
}
