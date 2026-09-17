# Initial GitHub handoff checklist

Run only after the Phase 1 exit gate in `docs/05-roadmap.md` passes.

## Preflight

Run `scripts/github_preflight.sh` before staging the initial commit.

- Run plugin and companion builds with zero warnings/errors.
- Run C# protocol integration tests and Python fixture/smoke tests.
- Confirm no tokens, prompt content, personal paths, or generated binaries are staged.
- Review `git diff --check` and repository status.
- Confirm `plugin-development-findings.md` contains only durable findings and reusable evidence.
- Choose repository name, visibility, and license.

## Publish

1. Create the GitHub repository using the approved name/visibility.
2. Add the remote and push the reviewed initial commit.
3. Record repository URL, commit, visibility, license, and publication date.
4. Verify a clean clone can follow the documented build/test steps.

Do not publish while pairing secrets, local endpoint files, host logs, or unreviewed experimental artifacts are present.

## Publication record

- Repository: [alfatreze/chatgpt-deck](https://github.com/alfatreze/chatgpt-deck)
- Visibility: Public
- License: MIT
- Initial commit: `c08b5a3`
- Publication date: 2026-09-17
- Clean-clone verification: passed `scripts/github_preflight.sh` and `scripts/regression_gate.sh` after dependency restore.
