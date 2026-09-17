#!/usr/bin/env python3
import json, os, socket, subprocess, time, stat

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
port = 62680
env = dict(os.environ, CODEX_DECK_TOKEN="integration-test-token", CODEX_DECK_TOKEN_STORE="file", CODEX_DECK_DISABLE_AUTOMATION="1", CODEX_DECK_PORT=str(port))
binary = os.path.join(root, "companion/CodexDeck.Companion/bin/Debug/net10.0/CodexDeck.Companion.dll")
proc = subprocess.Popen(["dotnet", binary], cwd=root, env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True)
try:
    for _ in range(30):
        try:
            with socket.create_connection(("127.0.0.1", port), timeout=0.2): break
        except OSError: time.sleep(0.2)
    else: raise RuntimeError("companion did not start")
    endpoint = os.path.expanduser("~/Library/Application Support/CodexDeck/connection.json")
    if os.path.exists(endpoint):
        mode = stat.S_IMODE(os.stat(endpoint).st_mode)
        assert mode & 0o077 == 0, f"endpoint permissions too broad: {oct(mode)}"
    def exchange(payload):
        with socket.create_connection(("127.0.0.1", port), timeout=3) as s:
            s.sendall((json.dumps(payload) + "\n").encode())
            return json.loads(s.makefile().readline())
    hello = exchange({"type":"hello","protocolVersion":1,"token":"integration-test-token"})
    assert hello["type"] == "hello.ack"
    denied = exchange({"type":"hello","protocolVersion":1,"token":"wrong-token"})
    assert denied["type"] == "error" and denied["reason"] == "unauthorized"
    unknown = exchange({"type":"action.intent","protocolVersion":1,"id":"00000000-0000-0000-0000-000000000001","action":"unknown_action","token":"integration-test-token"})
    assert unknown["status"] == "unavailable" and unknown["reason"] == "not_supported"
    malformed = exchange({"type":"action.intent","protocolVersion":1,"action":"interrupt","token":"integration-test-token"})
    assert malformed["type"] == "error"
    bad_id = exchange({"type":"action.intent","protocolVersion":1,"id":"not-a-guid","action":"interrupt","token":"integration-test-token"})
    assert bad_id["type"] == "error" and bad_id["reason"] == "invalid_message"
    failed = exchange({"type":"action.intent","protocolVersion":1,"id":"00000000-0000-0000-0000-000000000002","action":"interrupt","token":"integration-test-token"})
    assert failed["status"] == "failed" and failed["reason"] == "execution_failed"
    reconnect = exchange({"type":"hello","protocolVersion":1,"token":"integration-test-token"})
    assert reconnect["type"] == "hello.ack"
    print("Companion integration smoke passed: hello, auth, unauthorized, unsupported, malformed-id, reconnect")
finally:
    proc.terminate()
    try: proc.wait(timeout=3)
    except subprocess.TimeoutExpired: proc.kill()
