#!/usr/bin/env python3
"""Create a deterministic direct-install .lplug4 POSIX ustar archive."""
from pathlib import Path
import io
import re
import sys
import tarfile

ROOT = Path(__file__).resolve().parents[1]
configuration = sys.argv[1] if len(sys.argv) > 1 else "Debug"
source = ROOT / "CodexDeckPlugin" / "bin" / configuration
yaml_path = source / "metadata" / "LoupedeckPackage.yaml"
version = re.search(r"^version:\s*(\S+)", yaml_path.read_text(), re.MULTILINE).group(1).replace(".", "_")
output = ROOT / f"CodexDeck_{version}.lplug4"
mtime = 1767974400

def entry(name: str, mode: int, size: int, kind: bytes) -> tarfile.TarInfo:
    info = tarfile.TarInfo(name)
    info.mode = mode
    info.uid = info.gid = 0
    info.uname = info.gname = ""
    info.mtime = mtime
    info.size = size
    info.type = kind
    return info

folders = sorted(path.name for path in source.iterdir() if path.is_dir() and path.name != "localization.generated")
if "bin" not in folders or "metadata" not in folders:
    raise SystemExit(f"Expected bin/ and metadata/ under {source}")

with tarfile.open(output, "w", format=tarfile.USTAR_FORMAT) as archive:
    for folder in folders:
        archive.addfile(entry(f"{folder}/", 0o777, 0, tarfile.DIRTYPE))
    for folder in folders:
        for path in sorted((source / folder).iterdir()):
            if path.is_file():
                data = path.read_bytes()
                archive.addfile(entry(f"{folder}/{path.name}", 0o666, len(data), tarfile.REGTYPE), io.BytesIO(data))

names = tarfile.open(output).getnames()
for required in ("metadata/LoupedeckPackage.yaml", "metadata/Icon256x256.png", "bin/CodexDeckPlugin.dll"):
    if required not in names:
        raise SystemExit(f"Package is missing {required}")
print(f"wrote {output} ({output.stat().st_size} bytes, {len(names)} entries)")
