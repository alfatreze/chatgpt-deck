#!/usr/bin/env python3
"""Minimal newline-delimited JSON loopback bridge for contract testing."""

import json
import os
import socketserver


SNAPSHOT = {
    "type": "state.snapshot",
    "protocolVersion": 1,
    "mode": "shortcut",
    "capabilities": ["open_codex", "interrupt", "new_task"],
    "updatedAt": "2026-09-16T00:00:00Z",
    "actions": {
        "open_codex": {"enabled": True, "status": "idle"},
        "interrupt": {"enabled": True, "status": "idle"},
        "new_task": {"enabled": True, "status": "idle"},
    },
}


class Handler(socketserver.StreamRequestHandler):
    def handle(self) -> None:
        for raw in self.rfile:
            try:
                message = json.loads(raw)
                response = self.respond(message)
            except (json.JSONDecodeError, TypeError):
                response = {
                    "type": "error",
                    "protocolVersion": 1,
                    "reason": "invalid_message",
                }
            self.wfile.write((json.dumps(response) + "\n").encode())
            self.wfile.flush()

    @staticmethod
    def respond(message: dict) -> dict:
        expected_token = os.environ.get("CODEX_DECK_MOCK_TOKEN")
        if expected_token and message.get("token") != expected_token:
            return {"type": "error", "protocolVersion": 1, "reason": "unauthorized"}
        message_type = message.get("type")
        if message_type == "hello":
            return {"type": "hello.ack", "protocolVersion": 1, "snapshot": SNAPSHOT}
        if message_type == "action.intent":
            return {
                "type": "action.receipt",
                "protocolVersion": 1,
                "id": message.get("id"),
                "status": "accepted",
            }
        return {
            "type": "error",
            "protocolVersion": 1,
            "reason": "unsupported",
        }


class Bridge(socketserver.ThreadingTCPServer):
    allow_reuse_address = True


if __name__ == "__main__":
    with Bridge(("127.0.0.1", 0), Handler) as server:
        print(f"mock bridge listening on {server.server_address[1]}")
        server.serve_forever()
