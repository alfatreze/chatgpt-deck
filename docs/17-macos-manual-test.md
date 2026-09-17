# macOS manual test — permissions and New Task

1. Reload the plugin and assign `Permissions & Connection` and `New Task` to spare buttons.
2. With permission not yet granted, press `Permissions & Connection`.
3. Confirm macOS shows the native Logi Plugin Service → System Events prompt.
4. Select **Allow**, then return to Loupedeck. If Accessibility does not visibly prompt during the probe, close and reopen Loupedeck; macOS may present consent when the host/service starts.
5. Press `Permissions & Connection` again and confirm it reports the ready/connected state.
6. Press `Test Permissions` and confirm it performs a harmless probe without sending a Codex shortcut.
7. Press `Companion Status` with the companion running and confirm a fresh connection result.
8. Open Codex, select a disposable conversation, and press `New Task`.
9. Confirm a new task/conversation appears in Codex.
10. If no task appears, inspect `CodexDeck.log` for the automation exit code and stderr.
11. Revoke Accessibility/Automation permission, press `New Task`, and confirm readable permission-needed feedback.
12. Restore permission and repeat step 8. New Task is expected to activate Codex when it is not focused; Interrupt actions intentionally require Codex to be focused first.
13. Start a long-running task, focus Codex, and press `Interrupt Codex`. Verify whether Codex requires a second press while its Escape/stop control is visible; record both presses and final task termination.
14. After the first accepted Interrupt press, confirm the button shows the red attention bitmap and `Press again to interrupt`; after the timeout, confirm it returns to the normal icon.

Profile/page note: if the Loupedeck device exposes page or profile buttons (for example `1`, `2`, or workspace selectors), switch away from the Codex layout and back, then confirm assignments remain. If no such controls are visible, record `not exposed by current host layout`; this is not a plugin failure.

Test 9 accessibility review: skipped by user decision for this validation pass; retain for a later accessibility-focused review. Test Permissions transient `Permissions checked` feedback is confirmed working.

Record macOS version, Loupedeck version, permission prompt text, displayed feedback, and the relevant log line. Current evidence: Test 1 (`Permissions & Connection`) reached `Connected`; Test 8 retained connection state through sleep and screen lock.
