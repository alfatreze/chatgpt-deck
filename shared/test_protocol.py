#!/usr/bin/env python3
"""Dependency-free protocol smoke tests."""

import os
import importlib.util


spec = importlib.util.spec_from_file_location("mock_bridge", "shared/mock_bridge.py")
mock_bridge = importlib.util.module_from_spec(spec)
spec.loader.exec_module(mock_bridge)


def test_hello_and_snapshot():
    response = mock_bridge.Handler.respond({"type": "hello", "protocolVersion": 1})
    assert response["type"] == "hello.ack"
    snapshot = response["snapshot"]
    assert snapshot["protocolVersion"] == 1
    assert snapshot["mode"] in {"setup_needed", "shortcut", "live", "degraded"}
    assert isinstance(snapshot["capabilities"], list)
    assert isinstance(snapshot["actions"], dict)
    assert {"open_codex", "interrupt", "new_task"}.issubset(snapshot["capabilities"])
    assert {"open_codex", "interrupt", "new_task"}.issubset(snapshot["actions"])


def test_action_receipt():
    response = mock_bridge.Handler.respond({
        "type": "action.intent",
        "protocolVersion": 1,
        "id": "00000000-0000-4000-8000-000000000004",
        "action": "open_codex",
    })
    assert response["status"] == "accepted"


def test_authentication():
    os.environ["CODEX_DECK_MOCK_TOKEN"] = "test-token"
    try:
        denied = mock_bridge.Handler.respond({"type": "hello", "protocolVersion": 1})
        allowed = mock_bridge.Handler.respond({"type": "hello", "protocolVersion": 1, "token": "test-token"})
        assert denied["reason"] == "unauthorized"
        assert allowed["type"] == "hello.ack"
    finally:
        os.environ.pop("CODEX_DECK_MOCK_TOKEN", None)


def test_unsupported_message():
    response = mock_bridge.Handler.respond({"type": "future.message", "protocolVersion": 1})
    assert response["reason"] == "unsupported"


if __name__ == "__main__":
    for test in (test_hello_and_snapshot, test_action_receipt, test_authentication, test_unsupported_message):
        test()
    print("Validated 4 protocol smoke tests and Core capability coverage")
