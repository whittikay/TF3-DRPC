# Linux companion app: implementation plan

**Status: approved; Stage 0 implementation is in progress, not released.**

Current assignments, validation results and review gates are in
[the execution ledger](linux-stages.md).

Prepared 2026-11-10 against upstream commit
`00592fd2b5cc6a36fa7a4c158ce5fbd830db7930`.
The fork is created and checked out locally, with `origin` pointing to the fork and
`upstream` to `Zagrthos/TF3-DRPC`. This plan is not a Linux support announcement.

## Approved direction

1. **Add an Avalonia Linux desktop app, retaining MAUI on Windows and macOS.** Reuse
   the existing Core and extract UI-independent application logic rather than rewriting
   the parser, presence builder or Discord client. Confirm Avalonia's suitability with
   a small Native AOT and desktop-integration spike before committing to it.
2. **Use local Discord IPC, not client-specific plugins or Discord account tokens.**
   Expand endpoint discovery and diagnostics so the same companion works with official
   Discord and compatible alternative clients, including Vesktop.
3. **Ship an unsandboxed, portable `linux-x64` archive first.** Keep persistent settings
   and logs in `DATA/` beside the executable, preserving upstream's portability contract.
   Supporting a Flatpak Discord client does not require packaging the companion as Flatpak.
4. **Treat “all Discord setups” as all reachable, protocol-compatible local endpoints,
   plus documented user-managed bridges where necessary.** An unmodified browser,
   a remote Discord session, or a sandbox that denies access cannot be made compatible
   by socket discovery alone. Never claim these work without their prerequisites.
5. **Keep the mod and status-line protocol unchanged unless Linux testing proves a
   defect.** Native Linux TF3 is the primary game target; Proton is an additional test
   case, not a prerequisite for this design.

These choices and the staged scope below have been approved. A replacement of all desktop
UIs, an installer/XDG storage mode, or a managed bridge remains a separate scope decision.

## Findings and current limitations

### Existing project

| Area | What the code already provides | Linux work required |
|---|---|---|
| `Tf3Drpc.Core` | UI-independent `net11.0`, Native AOT compatibility, log parsing/following, state machine, monitor, settings, translations and presence formatting | Reuse; add Linux discovery/protocol regression tests |
| `Tf3Drpc.App/Tf3Drpc.App.csproj` | MAUI targets selected only for Windows and macOS | No Linux target; installing a MAUI workload does not solve this [1] |
| `Tf3Drpc.Core/Discord/DiscordConnector.cs` | Windows pipes and Unix-domain sockets, endpoint indices 0–9 | Selects only the first non-empty runtime/temp environment variable; does not try other roots or sandbox subdirectories |
| `Tf3Drpc.Core/Discord/DiscordPresenceClient.cs` | READY handshake, reconnect, rate limiting, rejection backoff and shutdown | A connected socket that fails the handshake can repeatedly win discovery and prevent trying a healthy later endpoint |
| `Tf3Drpc.App/Services/PathServiceBase.cs` | Steam userdata selection and a default-library fallback; running process supplies the install path | Needs Linux Steam roots, secondary libraries, manifests, overrides and Proton-aware path handling |
| `Tf3Drpc.App/Services/GameProcessService.cs` | Process lookup by one name, executable path and start time | Verify Linux process naming, truncated names, `/proc` races and Wine/Proton identity |
| `Tf3Drpc.App/ViewModels/SettingsViewModel.cs` and lifecycle service | Existing UI behaviour and settings orchestration | Dispatcher, timer, URL launcher and quit calls depend on MAUI; cannot reuse unchanged |
| Build/release/CodeQL workflows | Windows/macOS jobs and MAUI-specific setup | Independent Linux build/test/publish path; preserve existing checks |

Paths above are relative to `companion/` unless stated otherwise. The existing tests
exercise Core, but do not establish Linux packaging or real-client compatibility.

The SDK is pinned to `11.0.100-rc.3.26522.148`, with workload set
`11.0.100-rc.3.26524.4`. At planning time the local SDK-resolution preflight failed and no
build, tests or publish ran. Stage 0 has since established a project-local pinned SDK and
Linux build path; see the execution ledger for observed results. Adopting .NET 11 GA is a
separate implementation step, not an unreviewed downgrade to match a development machine.

### Discord and desktop model

- **Vencord is a client modification; Vesktop is the Electron desktop wrapper.**
  Vesktop v1.6.8 starts its bundled arRPC worker when its `arRPC` setting is enabled,
  then forwards activities into its renderer. No second arRPC server is needed for
  that setup [2]. Newer Vesktop code may change its internals: rely on the public
  IPC protocol, not the worker implementation or setting-file layout.
- **RPC and automatic game detection are different.** TF3-DRPC supplies the activity
  itself; the game's presence in a client's process-detection catalogue is irrelevant.
- Discord IPC uses Unix sockets and a framed JSON handshake followed by `SET_ACTIVITY`
  [3]. The existing Core already implements this transport. Wayland versus X11 affects
  the companion window/tray, not the Discord socket protocol.
- Flatpak Discord exposes its socket under a per-application runtime directory. A host
  companion should connect there directly instead of creating or replacing global
  symlinks [4]. Vesktop's Flatpak wrapper also has a per-application temp path [5].
- arRPC implements Discord-compatible local RPC for alternative clients and browser
  bridges [6]. Browser support still needs a compatible extension/plugin and a running
  local bridge. Vencord documents its WebRichPresence plugin for that case [7].
- Avalonia supports Linux and Native AOT. Its current documentation describes X11 as
  the default, XWayland on Wayland desktops, and an opt-in native Wayland backend from
  12.1.0 that is still early-stage [8, 9]. Pin and test a version; do not assume every
  backend or tray feature has equal support.
- Linux trays require a StatusNotifierItem/AppIndicator host. GNOME may have none by
  default, and tiling compositors need a suitable shell/bar [10]. Niri's X11 applications
  run through xwayland-satellite [11]. The window must remain usable without either a
  tray host or compositor support for forced focus/placement.

Machine-specific inventory, account identifiers, paths and raw logs stay out of this
public plan. Local inspection established the initial test route, not end-to-end success:
no activity was published and no Discord settings, packages or game files were changed.

## Compatibility contract

These are **planned coverage levels**, not claims of tested support.

| Discord setup on Linux | Intended connection | Required coverage / boundary |
|---|---|---|
| Official Stable, PTB or Canary; distribution packages or upstream archive | Native `discord-ipc-0` … `-9` | Direct-support target; do not depend on package or process names |
| Official client modified with Vencord/other mods | Same native IPC | Direct-support target if the client still exposes RPC |
| Vesktop, native package or AppImage | Built-in arRPC/compatible IPC | Initial acceptance target; RPC enabled and client logged in |
| Discord Flatpak | `$XDG_RUNTIME_DIR/app/com.discordapp.Discord/discord-ipc-N` | Direct host-to-sandbox-path target; no symlink workaround required |
| Vesktop Flatpak | Per-app runtime path, including `app/dev.vencord.Vesktop/discord-ipc-N` | Validate the actual package's exported socket; test independently of official Discord |
| Discord Snap and parallel instances | Runtime `snap.discord*` locations | Target after validating actual paths and confinement; permission failures require actionable guidance, not sandbox bypasses [12] |
| Other wrappers, such as ArmCord/its successors or WebCord | Their compatible IPC server or a user-managed arRPC bridge | Capability-based support; a wrapper being Electron-based is not sufficient |
| Browser Discord with a supported plugin and local arRPC | Companion → Unix IPC → arRPC → browser plugin | Conditional-support target; test the complete bridge, not merely its socket |
| Unmodified browser/PWA without a bridge | No reachable Discord RPC endpoint | Explain prerequisite; cannot work directly |
| Several clients/accounts running together | User-selected endpoint; deterministic Auto mode | Send to one endpoint only; never silently broadcast to every account |
| Windows Discord running in Wine, or remote/container-isolated Discord | A deliberately configured compatible relay/export | Conditional compatibility only; not part of the first release's automatic discovery |

The companion requires access to a same-user local endpoint and activity-sharing enabled
in the receiving client. A successful READY handshake means **RPC connected**, not proof
that friends can see the activity. Privacy settings, offline sessions and disconnected
browser bridges can prevent display even when IPC responds.

First release target: mainstream glibc Linux on x86-64, tested on Arch/CachyOS plus
Ubuntu and Fedora. Linux ARM64, musl distributions and Steam Deck Gaming Mode need
separate validation before support claims; a portable build is not a promise of every
Linux ABI or desktop environment.

## Proposed architecture

```text
Tf3Drpc.Core                         existing protocol/domain logic + testable discovery
       ↑
Tf3Drpc.Application                  proposed shared orchestration/view-model logic
       ↑                         ↑
Tf3Drpc.App (MAUI)                  Tf3Drpc.Linux (Avalonia)
Windows / macOS adapters            Linux paths/processes, window, tray, lifecycle
```

- Extract only genuinely shared services, messages and presentation logic from the MAUI
  app. Keep domain logic in Core and platform APIs in adapters. No copied parallel
  state machines or second Discord library.
- Introduce small abstractions for UI dispatch, debounced saves, opening URLs and
  application exit. Preserve pending-template saves and clear presence on exit.
- Keep the single monitor loop and explicit DI; do not add a background daemon or a
  second Generic Host just for Linux. Preserve source-generated JSON/logging, compiled
  bindings, localisation and Native AOT rules.
- Keep Windows/macOS entry points and platform services intact. Make new public API
  surface minimal; add tests for extracted application logic.
- Provide a Linux solution/filter that excludes MAUI projects. Linux restore/test must
  not require Windows or Apple workloads. Deal explicitly with the shared SDK/workload
  pin during the build spike, without relaxing the pins for other platforms.

### Discord endpoint discovery and selection

1. Accept an explicit Unix-socket override, otherwise enumerate ordered candidates:
   `XDG_RUNTIME_DIR`, a validated `/run/user/<uid>` fallback, `TMPDIR`, `TMP`, `TEMP`,
   and `/tmp`. Do not stop merely because an environment variable is set.
2. Include known Flatpak/Snap runtime subdirectories and a bounded search of appropriate
   same-user app directories for alternative clients. Try indices 0–9, normalise and
   deduplicate aliases, and handle absent, invalid and overlong socket paths.
3. Keep both connection and handshake attempts bounded. Advance after a refused,
   inaccessible, malformed or silent candidate; a bad `-0` must not permanently mask
   a good `-1`. Rework the connector/handshake boundary to make this testable.
4. Do discovery asynchronously with an overall attempt budget, so many bad candidates
   do not stall the one-second monitor loop. Preserve cancellation and the existing
   approximate 15-second reconnect/update cadence and rejection backoff.
5. Show the selected endpoint category and a redacted path. Provide Auto and explicit
   selection; if an explicitly chosen client disappears, report that rather than silently
   switching to another account. Reconnect and resend current state after a restart.
6. Check socket type, ownership and, where available, peer credentials. Respect existing
   same-user symlinks without creating, unlinking or chmod-ing Discord-owned paths.
   No root, blanket filesystem permissions, client injection or token handling.
7. Expose useful outcomes: no endpoint, permission denied, handshake timeout, protocol
   error, activity rejected, RPC connected. Do not label socket presence as verified
   activity display. Diagnostic collection must redact user/account data.

### Steam and game discovery

- Look for native Steam roots (`~/.local/share/Steam`, `~/.steam/steam`, relevant XDG
  locations), resolving duplicate symlinks, and Flatpak Steam's application-data root.
- Parse `steamapps/libraryfolders.vdf` and `appmanifest_3493540.acf` with a bounded VDF
  reader that understands quoted/escaped values. Use metadata rather than assuming the
  game is in the default library or that its directory name never changes.
- Keep userdata discovery separate from library discovery: the existing Steam layout is
  `<Steam>/userdata/<account>/3493540/local/crash_dump/stdout.txt`; a secondary install
  library does not imply userdata moved there. Preserve existing freshness/account rules.
- Verify native Linux executable identity and process start time. Handle `/proc` entries
  disappearing, Linux name truncation, denied reads and PID reuse. For Proton, correlate
  executable/command line and Steam app metadata; do not match every `wine` process or
  treat a Wine loader's directory as the game install directory.
- Add user overrides for game directory and log/userdata location, including nonstandard
  Steam roots and Proton prefixes. Validate actual Proton log locations before encoding
  fallback paths. Unknown process state must not silently publish stale logs forever.
- Read only the necessary game/Steam files; never scan the whole home directory, execute
  `profile.lua`, or alter Steam/game settings. Keep the existing parser, `.mo` loading,
  truncation handling, stale-session rejection and no-mod fallback.

### Desktop and distribution behaviour

- Port the existing settings, preview, presets, templates, languages, theme, store-button
  opt-in and error messages. Add advanced path/endpoint selection and a diagnostic view.
- Provide tray Settings/Language/Exit via StatusNotifierItem where available. If there is
  no tray, show a normal window and do not hide the only usable control surface on close.
  Recover if the tray host disappears while the app runs.
- Use a per-session single-instance mechanism, preferably a D-Bus name and activation
  request, so a second launch opens the existing window even from another extracted copy.
  Respect Wayland focus restrictions; don't require global hotkeys or input injection.
  Any private runtime-file fallback needs a documented, narrow exception to upstream's
  DATA-only rule; no persistent files outside `DATA/` by default.
- Test niri/Wayland, KDE and GNOME, including tray-less GNOME; retain X11/XWayland support.
  Validate native Wayland separately and keep a documented backend fallback where needed.
- Ship a Native AOT tar archive with executable permissions, licence, third-party notices
  and checksums. Select the oldest supported build baseline for glibc compatibility; list
  native library/ICU/font/display dependencies. “Self-contained” removes the .NET runtime
  requirement, not all system-library dependencies [9, 13].
- AppImage, AUR/deb/rpm and a sandboxed companion are later packaging decisions. Read-only
  AppImage mounts and system installation paths need an approved storage design; don't
  silently redirect `DATA/` into user-profile directories. No automatic startup.

## Delivery stages and review gates

| Stage | Deliverables | Exit criterion |
|---|---|---|
| **0. Toolchain and UI feasibility** | Resolve the pinned SDK/workload requirements; baseline Core build/tests on Linux; minimal Avalonia Native AOT window, tray/no-tray handling, D-Bus, translations and backend checks | A published spike runs on the primary desktop and a baseline distro without new trim/AOT warnings; record package versions, dependencies and any blockers before approving the UI choice |
| **1. Shared application seam** | Extract UI-neutral services/view-model logic; add adapters and Linux-only solution/filter; keep current MAUI shells | Shared tests pass; Windows/macOS build/test/publish regression checks remain green on appropriate hosts |
| **2. Robust Discord IPC** | Candidate discovery, explicit selection, handshake failover, diagnostics and real Unix-socket test server | Native/arRPC/Flatpak/Snap path fixtures pass; a silent early endpoint cannot block a healthy one; no client filesystem changes |
| **3. Linux game integration** | Native/Flatpak Steam discovery, VDF/manifests, native/Proton process adapters and manual overrides | Real native-game session from a secondary library follows all supported states; replay/fixture tests cover Proton and permission/race failures, followed by a real Proton run before claiming it |
| **4. Linux desktop feature parity** | Full settings/preview/tray/lifecycle, single instance, diagnostics, portable storage and translations | Usable on X11 and Wayland, with and without a tray; exit saves edits and clears activity; published translations work |
| **5. Release qualification** | Linux CI, release archive/notices/checksums, compatibility guide, client matrix and regression evidence | Published build passes the release checklist below; untested combinations are labelled, not advertised as working |
| **6. Broader conditional setups** | Browser/arRPC guide and live check, additional wrappers, Snap variants and optional relay scenarios; later packaging proposals | Each advertised setup has reproducible prerequisites, an observed end-to-end result and documented limits |

Stages 2 and 3 can proceed independently after the shared contracts settle. Start with
Vesktop on Arch/CachyOS, but do not call the first successful run universal compatibility.
Broad client coverage is an ongoing release criterion, not a one-time client-name list.
Implementation is authorised. Commits, pushes, pull requests and releases still require
separate user authorisation.

## Validation and definition of done

### Automated coverage

- Preserve all existing Core tests; run Debug/Release builds, analysers and Native AOT
  publish, not just `dotnet test`. Re-run Windows/macOS on native CI runners after shared
  changes. Add Linux CodeQL coverage for new projects.
- Use synthetic sockets under test-owned directories for discovery and protocol tests:
  env precedence, alternate roots, sandbox paths, `-0` through `-9`, duplicates/symlinks,
  permissions, stale files, malformed/oversized/fragmented frames, READY timeout,
  PING/PONG, rejection, reconnect, cancellation and cleanup.
- Never let ordinary CI tests discover the developer's real Discord socket. Live tests
  stay explicitly opt-in because they publish an activity.
- Test VDF parsing, multiple libraries/accounts, spaces and Unicode, native/Flatpak Steam,
  process identity/exit races, Proton fixtures, log rotation/truncation and clock changes.
- Test shared view-model behaviour and settings schema compatibility, including corrupt
  overrides, save failures, pending edits on exit and all existing language resources.
- Headless GUI tests are useful, but cannot certify a real compositor, tray host, Discord
  login, game run or browser bridge. Record those separately.

### Manual release matrix

| Dimension | Required evidence |
|---|---|
| Initial route | Native Linux TF3, secondary Steam library, portable companion and native Vesktop on Arch/CachyOS |
| Official clients | Stable plus PTB/Canary coexistence; vanilla and a Vencord-modified official client |
| Packaging boundaries | Host companion → Discord Flatpak, Vesktop Flatpak and Discord Snap; Steam Flatpak tested separately |
| Bridge/alternative clients | At least one other compatible wrapper, and browser + local arRPC + supported plugin before advertising those routes |
| Desktop differences | niri/Wayland, KDE, GNOME without tray support, and an X11 session; backend used explicitly recorded |
| Game modes | Native plus a real Proton run; start/menu/load/play/pause/campaign/no-mod/quit/crash; companion launched before and after game |
| Recovery | Discord absent at launch, restart, competing clients, bridge unavailable, permission failure, suspend/resume, game restart and changed log |
| Distribution | Extracted published archive on Arch/CachyOS, Ubuntu and Fedora; dependencies, executable bit, writable/read-only DATA, translated UI |

**Release acceptance:** current state appears at the existing update cadence; disconnects
recover without restarting the companion; game/app exit clears presence; selected-client
routing is respected; no inaccessible hidden app on tray-less desktops; no new build,
trim or AOT warnings; no mod protocol regression; no automatic system changes or secrets.
Verify activity display manually, and the optional store button from another account if
available. Never capture the Discord window or publish raw machine/game logs as evidence.
A handshake or a non-error write alone is insufficient proof of visible presence.

## Risks and mitigations

| Risk | Mitigation / decision point |
|---|---|
| Avalonia backend, tray, D-Bus or AOT incompatibility | Stage 0 is a go/no-go gate. Keep a tested XWayland route; revisit framework choice rather than suppress warnings or silently drop AOT |
| Shared extraction breaks existing apps | Small behaviour-preserving changes, new application tests, Windows/macOS publish gates; no all-platform UI migration |
| SDK RC/GA churn and workload coupling | Establish a reproducible pin; handle any coordinated SDK/MAUI upgrade separately, not as a hidden prerequisite |
| Flatpak/Snap changes paths or blocks sockets | Capability-based bounded discovery, explicit override, versioned evidence and minimal permission guidance; never auto-rewrite client packaging |
| arRPC/client acknowledges but does not display activity | Distinguish connection from display; real-client visual checks and documented privacy/bridge prerequisites |
| Wrong client/account chosen | Deterministic Auto, explicit override, selected endpoint shown, no fan-out |
| Proton/process identity or stale logs produce false presence | Native-first validation, explicit identity checks, preserved freshness rules and real Proton acceptance test |
| Public diagnostics leak local data | Redact paths/account identifiers; opt-in bounded export; no Discord tokens, screenshots or raw payload logging |
| Portable archive fails on another distro | Conservative ABI baseline, native-dependency inventory and published-binary smoke tests across distros |
| Packaging conflicts with DATA-only policy | Defer system packages/AppImage/Flatpak companion until storage and permissions receive explicit review |

## Research references

Sources consulted 2026-11-10. Documentation and package manifests are moving targets;
recheck the selected release versions during implementation. Third-party code was read
for compatibility research, not executed or copied into this MIT-licensed project.

1. [Microsoft: supported MAUI platforms](https://learn.microsoft.com/en-us/dotnet/maui/supported-platforms?view=net-maui-11.0).
2. Vesktop v1.6.8: [arRPC startup](https://github.com/Vencord/Vesktop/blob/v1.6.8/src/main/arrpc/index.ts), [worker](https://github.com/Vencord/Vesktop/blob/v1.6.8/src/main/arrpc/worker.ts), [renderer integration](https://github.com/Vencord/Vesktop/blob/v1.6.8/src/renderer/arrpc.ts).
3. [Discord: RPC protocol and IPC paths](https://docs.discord.com/developers/topics/rpc).
4. [Discord Flathub: Rich Presence socket placement](https://github.com/flathub/com.discordapp.Discord/wiki/Rich-Precense-%28discord-rpc%29), [Flatpak sandbox permissions](https://docs.flatpak.org/en/latest/sandbox-permissions.html).
5. Vesktop Flathub: [manifest](https://github.com/flathub/dev.vencord.Vesktop/blob/master/dev.vencord.Vesktop.yml), [launcher](https://github.com/flathub/dev.vencord.Vesktop/blob/master/startvesktop).
6. [arRPC overview and supported clients/transports](https://github.com/OpenAsar/arrpc/tree/3c4e1ce3c49926bfeb9febbaa04327b06b3a4c2c).
7. [Vencord: WebRichPresence plugin](https://vencord.dev/plugins/WebRichPresence%20%28arRPC%29/).
8. [Avalonia: Linux backends, Wayland and accessibility](https://docs.avaloniaui.net/docs/platform-specific-guides/linux).
9. [Avalonia: Native AOT](https://docs.avaloniaui.net/docs/deployment/native-aot).
10. [Avalonia: TrayIcon Linux requirements](https://docs.avaloniaui.net/controls/navigation/trayicon/).
11. [Niri: XWayland integration](https://niri-wm.github.io/niri/Xwayland.html).
12. [Snap Discord manifest and confinement](https://github.com/snapcrafters/discord/blob/master/snap/snapcraft.yaml); [existing bridge's Snap/runtime path handling](https://github.com/0e4ef622/wine-discord-ipc-bridge/blob/master/winediscordipcbridge-steam.sh) is a discovery reference, not a requirement to run the native companion under Wine.
13. [Avalonia: Linux deployment and native dependencies](https://docs.avaloniaui.net/docs/deployment/linux).

Local design references: [companion](companion.md), [game log](game-log.md),
[testing](testing.md), [existing platform parity](platform-parity.md), and
[repository constraints](../AGENTS.md).
