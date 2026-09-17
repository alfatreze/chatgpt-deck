# Development environment baseline

**Captured:** 2026-09-16  
**Status:** Phase 0 baseline complete — `net10.0` builds, the generated plugin loads into Logi Plugin Service, and the reference device produced three verified presses.

## Observed host

| Item | Observed value | Evidence/status |
| --- | --- | --- |
| OS | macOS 26.6.2 | `sw_vers -productVersion` |
| CPU architecture | `arm64` | `uname -m` |
| Loupedeck host | `/Applications/Loupedeck.app`, version `6.4.1.364` | bundle metadata |
| Logi Plugin Service | `/Applications/Utilities/LogiPluginService.app`, version `6.4.1.3246` | bundle metadata |
| Shared support path | `/Library/Application Support/Logi/LogiPluginService` | present |
| `dotnet` SDK on shell `PATH` | not on `PATH`; invoke `/usr/local/share/dotnet/dotnet` explicitly or update shell profile | environment cleanup |
| `.NET 8` install attempt | Homebrew resolved `dotnet-sdk@8` version `8.0.425`, but the installer required an interactive administrator password | historical/template baseline |
| `.NET 8` SDK | installed at `/usr/local/share/dotnet`, version `8.0.425`, RID `osx-arm64` | verified with `/usr/local/share/dotnet/dotnet --info` |
| LogiPluginTool | installed global tool version `6.1.4.22672` | verified; generated scaffold successfully |
| `PluginApi.dll` | `/Applications/Utilities/LogiPluginService.app/Contents/MonoBundle/PluginApi.dll`, assembly version `6.4.1.3246` | its references require `System.Runtime 10.0` |
| `.NET 10` SDK | installed at `/usr/local/share/dotnet`, version `10.0.401`, RID `osx-arm64` | verified with `/usr/local/share/dotnet/dotnet --info` |
| Generated project | `CodexDeckPlugin/src/CodexDeckPlugin.csproj`, target `net10.0` | clean build succeeds |
| Development link | `/Users/abel.santos/Library/Application Support/Logi/LogiPluginService/Plugins/CodexDeckPlugin.link` | created by build |
| Plugin log | `/Users/abel.santos/Library/Application Support/Logi/LogiPluginService/Logs/plugin_logs/CodexDeck.log` | confirms plugin loaded and 8 dynamic actions registered |

The host and service versions above are inventory data, not compatibility confirmation. Do not infer a plugin target framework, package version, or package format from them.

## Phase 0 validation record

1. Pressed the generated sample action three times and verified sequential log entries.
2. The exact device model and any visual rendering issue remain useful follow-up metadata.
3. Keep the clean build, development link, host load, and log evidence below as the Phase 0 baseline.

## Commands to re-run after prerequisites are available

```zsh
dotnet --info
dotnet tool install --global LogiPluginTool
logiplugintool generate CodexDeck
```

## Deliberate deferrals

- The official generator created the solution and project; the project is explicitly retargeted to `net10.0` because of the installed host API. The development link, host load, action discovery, hardware assignment, and three physical presses have been validated.
- No application or host files have been changed.
- The project license is not selected. A license decision is required before the repository’s first shared commit or publication.

## First build result

The historical .NET 8 build failed with `CS1705`. After installing .NET SDK `10.0.401` and retargeting to `net10.0`, `/usr/local/share/dotnet/dotnet build CodexDeckPlugin/CodexDeckPlugin.sln` completed with 0 warnings and 0 errors. The host loaded `CodexDeck` from the Debug output, `CodexDeck.log` recorded eight dynamic actions, and physical presses plus interrupt feedback were verified.

## Authoritative implementation references

- [Logi Actions C# SDK introduction](https://logitech.github.io/actions-sdk-docs/csharp/plugin-development/introduction/) — Plugin Tool, generator, macOS `.link`, and hot reload. Its current template instructions say .NET 8; this project deliberately uses .NET 10 to match the installed host API.
- [Logi Actions SDK getting started](https://logitech.github.io/actions-sdk-docs/getting-started/) — C# is the established full-feature SDK; Node.js remains beta/limited.
- [Logi Actions plugin logging](https://logitech.github.io/actions-sdk-docs/csharp/plugin-features/logging/) — plugin logs are always enabled and written under the user’s Logi Plugin Service data directory.

## macOS Accessibility troubleshooting

`Interrupt Codex` uses System Events to verify the frontmost bundle (`com.openai.codex`) and send Escape. If permission is missing, macOS may return error `-10827`; the action fails closed as `not_focused`.

Grant permission under **System Settings → Privacy & Security → Accessibility** for the process launching the plugin automation, then retest. Never bypass the focus guard or substitute screen-coordinate automation.
