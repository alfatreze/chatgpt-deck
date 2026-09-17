#!/usr/bin/env python3
"""Small dependency-free contract check for Codex Deck v1 fixtures."""

import json
from pathlib import Path


ROOT = Path(__file__).parent
FIXTURES = ROOT / "fixtures"


def load(name: str) -> dict:
    with (FIXTURES / name).open(encoding="utf-8") as handle:
        return json.load(handle)


def validate(message: dict) -> None:
    assert message["protocolVersion"] == 1
    assert isinstance(message["type"], str)
    assert message["type"] == "action.receipt"
    assert message["status"] in {"accepted", "unavailable", "failed"}
    assert len(message["id"]) == 36
    if message["status"] == "unavailable":
        assert message.get("reason") in {
            "not_configured",
            "not_supported",
            "not_connected",
            "not_focused",
            "permission_denied",
            "execution_failed",
        }


def main() -> None:
    for fixture in ("action-receipt-accepted.json", "action-receipt-not-focused.json"):
        validate(load(fixture))
    for fixture in ("usage-supported.json", "usage-unsupported.json"):
        usage = load(fixture)
        assert usage["type"] == "usage.snapshot"
        assert usage["protocolVersion"] == 1
    print("Validated 2 protocol v1 fixtures")


if __name__ == "__main__":
    main()
