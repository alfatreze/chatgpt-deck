# Shared protocol

`protocol-v1.schema.json` is the source-of-truth envelope for the local companion contract.

The current fixtures deliberately cover two important receipt states:

- `accepted`: the companion dispatched a shortcut, without claiming Codex completed it.
- `unavailable/not_focused`: the focus guard prevented a potentially misdirected action.

Run the dependency-free contract check with:

```text
python3 shared/validate_fixtures.py
```

`mock_bridge.py` provides a dependency-free newline-delimited JSON loopback server. It currently responds to authenticated `hello` with a shortcut-mode snapshot, accepts `action.intent`, and rejects unknown or malformed messages. Full snapshot validation and real pairing persistence remain.

`CodexDeck.Protocol` contains the matching `net10.0` C# records and JSON options used by the production companion/plugin boundary.

Run protocol smoke tests with:

```text
python3 shared/test_protocol.py
```
