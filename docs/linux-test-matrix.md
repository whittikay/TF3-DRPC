# Linux Stage 0 acceptance matrix and fixture specifications

Owner: Qwen (Q0). Reviewer: Astra, with Sol consulting on Stage 0 findings.
Base: upstream `00592fd2b5cc6a36fa7a4c158ce5fbd830db7930` plus the uncommitted
`stage0-bootstrap-01` snapshot (verified against `.agent-state/bootstrap.json`).

This is a bounded acceptance matrix for Stage 0 and design-only fixture
specifications for the later Discord (Stage 2) and game-path (Stage 3) work. It does
not implement unapproved APIs. Every case names an ID, prerequisite, synthetic input
or manual steps, expected observable result, automation level, stage/owner and status.

**Automation levels:** `A` automated through the wrapper/tests/published binary;
`H` human observation on a real desktop/client, recorded separately; `D` design only,
to be executed once the referenced contract is approved.

**Statuses:** `Passed` (executed in the named checkout), `Partial` (only some assertions verified), `Not run`,
`Not run — blocked` (missing implementation or environment). A `Passed` here is an
owner self-check, not independent approval.

Commands run from the root of the owning checkout; the wrapper changes into
`companion/`. Raw logs stay in the ignored `.agent-state/` of the running checkout.
Build cases require the pinned SDK and restored dependencies; test cases require the
matching build; published-binary cases require BL-04. `$app` denotes the published
`companion/artifacts/publish/Tf3Drpc.Linux/release_linux-x64/Tf3Drpc.Linux` executable.
All DX/PF cases are **Not run — blocked**, pending approved injectable contracts and
follow-up fixture assignments. They describe expected behaviour, not approved APIs.
This integrated copy includes Astra's review corrections and Sol's S0 evidence.

## 1 Toolchain and build (TC)

- **TC-01 Pinned SDK resolves through the wrapper.**
  Prerequisite: worker checkout without its own `.tools/dotnet`.
  Steps: `./tools/linux-dotnet.sh --version`.
  Expected: prints exactly `11.0.100-rc.3.26522.148` (the pin in
  `companion/global.json`) and exits 0.
  Level A · Stage 0 / Astra · **Passed** (Qwen checkout, Q0 run).

- **TC-02 Shared SDK with independent worktree caches.**
  Prerequisite: primary checkout provides `.tools/dotnet`; worker has none.
  Steps: run the wrapper in the worker checkout; inspect cache locations.
  Expected: build succeeds using the shared SDK read-only, while
  `DOTNET_CLI_HOME`, `NUGET_PACKAGES`, `NUGET_HTTP_CACHE_PATH` and `TMPDIR` resolve to
  the worker's own ignored `.local-build/` (see `tools/linux-dotnet.sh`). No new files
  appear under the shared SDK.
  Level A · Stage 0 / Astra · **Partial** (cache directories created in the Qwen
  checkout; a read-only check of the shared SDK was not performed).

- **TC-03 Pinned SDK missing everywhere.**
  Prerequisite: isolated fixture with neither checkout-local nor primary-worktree SDK,
  and a controlled `PATH` retaining shell utilities and Git but no `dotnet`.
  Steps: run the wrapper in that fixture; do not rename or remove an installed SDK.
  Expected: exit 2 and the message "Install the pinned SDK locally or set
  TF3DRPC_DOTNET_ROOT." No download is attempted.
  Level A · Stage 0 / Astra · **Not run** (this host has a `dotnet` on `PATH`; needs
  a controlled environment, e.g. CI).

- **TC-04 Wrong SDK version on PATH.**
  Prerequisite: isolated fixture with neither local nor primary-worktree SDK; only an
  incompatible SDK on `PATH` (outside global.json's allowed latestPatch roll-forward).
  Steps: run the wrapper build.
  Expected: non-zero exit with the SDK-resolution error naming the required version.
  The pin in `companion/global.json` must not be changed to match the machine.
  Level A · Stage 0 / Astra · **Not run** (needs a second SDK; see TC-03 environment).

- **TC-05 No MAUI workload installation needed.**
  Prerequisite: none of the pinned workload set installed for the local SDK.
  Steps: `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug`.
  Expected: restore and build succeed with 0 warnings without any workload install;
  the wrapper's `MSBuildEnableWorkloadResolver=false` keeps the common `global.json`
  pin untouched. `TF3-DRPC.Linux.slnx` lists only `Tf3Drpc.Core`,
  `Tf3Drpc.Core.Tests` and `Tf3Drpc.Linux`.
  Level A · Stage 0 / Astra · **Passed** (Qwen checkout, Q0 run; solution contents
  verified).

- **TC-06 Wrapper is workload-free only (guardrail).**
  Steps: inspect command forwarding and documented scope without invoking MAUI or
  workload management. Expected: usage is restricted by documented policy, not a
  command allowlist; the wrapper is not a sandbox. Such commands must not be run here.
  Level A · Stage 0 / Astra · **Passed** (source inspection, confirmed by S0).

## 2 Core baseline and publish (BL)

- **BL-01 Debug build clean.** `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug`
  → exit 0, "0 Warning(s)", "0 Error(s)". Level A · Stage 0 / Astra · **Passed** (Qwen checkout).

- **BL-02 Release build clean.** Same command with `-c Release`.
  Level A · Stage 0 / Astra · **Passed** (Qwen checkout).

- **BL-03 Core tests, both configurations.**
  `./tools/linux-dotnet.sh test --project Tf3Drpc.Core.Tests/Tf3Drpc.Core.Tests.csproj -c <Debug|Release> --no-build`
  → exit 0, 650 passed / 3 skipped / 0 failed. The three skips are the known
  environment-bound tests: `MoReaderTests.RealGermanFile_ContainsKnownPairs` (real game
  translation file at the default Steam path),
  `SettingsStoreTests.Save_OverALockedFileFailsWithoutThrowing` (Windows-only locking)
  and `DiscordPresenceClientTests.LiveDiscord_ShowsAndClearsAPresenceAsync` (opt-in live
  Discord). Skips are reported, never counted as passes.
  Level A · Stage 0 / Astra · **Passed** (Qwen checkout, both configurations).

- **BL-04 Native AOT publish clean.**
  `./tools/linux-dotnet.sh publish Tf3Drpc.Linux/Tf3Drpc.Linux.csproj -c Release -r linux-x64`
  → exit 0 with no trim/AOT warnings in the log (the only expected message is
  `NETSDK1057`, a preview-SDK notice, not a warning). Level A · Stage 0 / Astra ·
  **Passed** (Qwen checkout; zero "warning" lines in the publish log).

- **BL-05 Published resource check.**
  `companion/artifacts/publish/Tf3Drpc.Linux/release_linux-x64/Tf3Drpc.Linux --check-resources`
  → exit 0, "Resource check passed for 13 languages." Proves the Core satellite
  assemblies survived ILC and that one translated string per language resolves
  (`companion/Tf3Drpc.Linux/ResourceCheck.cs`, `OwnText.StartingTheGame`); it does not
  prove every translated string is correct.
  Level A · Stage 0 / Astra · **Passed** (Qwen checkout).

- **BL-06 Invalid CLI argument.** Any unknown flag → exit 2 and the usage line on
  stderr (`companion/Tf3Drpc.Linux/Program.cs`). Level A · Stage 0 / Astra ·
  **Passed** (Qwen checkout, `--bogus` → exit 2).

- **BL-07 Missing-resource failure path.** Synthetic input: a scratch publish of
  `Tf3Drpc.Linux` with one culture's Core satellite demonstrably excluded from ILC,
  under an approved ignored scratch-build assignment. Sol's `SatelliteResourceLanguages=en`
  experiment still retained all 13 languages and does not exercise this failure path.
  Expected: `--check-resources` exits 1 and names the missing language.
  Level A · Stage 0 / Astra · **Not run — blocked** (negative control not yet designed).

- **BL-08 Compiled XAML startup regression.** The RC3 optimised AOT publish skips
  Avalonia's post-compilation XAML step, so `companion/Tf3Drpc.Linux/Tf3Drpc.Linux.csproj`
  sets `UseOptimizedPublish=false`. Expected: the published binary starts and opens its
  window (smoke on a desktop session); a negative control with the default optimised
  publish reproduces the startup failure. Re-run only when the SDK changes.
  Level H · Stage 0 / Sol observes · **Partial** (S0 reproduced exit 134 with missing
  precompiled XAML under optimised publish, then exit 0 after restoring the default;
  visual inspection remains outstanding).

- **BL-09 Managed-only run.** Run the non-AOT build with a runtime:
  `dotnet companion/artifacts/bin/Tf3Drpc.Linux/release/Tf3Drpc.Linux.dll --check-resources`
  → exit 0, same result as BL-05. Catches resource issues only visible in managed mode.
  Level A · Stage 0 / Astra · **Passed** (Qwen checkout).

## 3 Desktop, tray, D-Bus and lifecycle (DT)

- **DT-01 Smoke with tray enabled.** Prerequisite: a desktop session with a tray host.
  Steps: `"$app" --smoke-test`. Expected: exit 0 about three seconds after the window's
  Opened event; the window is visible; a tray icon appears when a host exists.
  Level H · Stage 0 / Sol · **Partial** (S0 observed exit 0; visibility and menu
  interaction are unverified).

- **DT-02 Smoke with tray disabled.** Steps: `"$app" --smoke-test --no-tray`.
  Expected: exit 0 after about three seconds, no tray attachment. This deliberately
  skips tray attachment; it is **not** a test of a tray-less desktop (see DT-04).
  Level H · Stage 0 / Sol · **Passed** (S0 process-startup smoke, exit 0; not visual
  verification of tray absence).

- **DT-03 Window close and Exit.** Prerequisite: `"$app"` running without
  `--smoke-test`. Steps: click the Exit button, then in a second run close the window.
  Expected: the process exits both times (`ShutdownMode.OnMainWindowClose`), no hidden
  unreachable process remains. In a separate smoke-mode run, close before the timer
  fires and verify prompt exit; inspect that the timer is disposed on exit
  (`companion/Tf3Drpc.Linux/App.axaml.cs`, `OnExit`).
  Level H · Stage 0 / Sol · **Not run**.

- **DT-04 No tray host / no D-Bus session.** Prerequisite: a desktop session with no
  StatusNotifierItem/AppIndicator host (for example GNOME without an appindicator
  extension) or a bare X server without a tray daemon. Steps: `"$app"` and
  `"$app" --smoke-test`. Expected: startup succeeds, the window stays reachable and
  usable, closing it exits; no crash or unhandled D-Bus failure. This is the case
  `--no-tray` does not cover.
  Level H · Stage 0 / Sol · **Not run — blocked** (needs a tray-less environment).

- **DT-05 Tray menu interaction.** Prerequisite: DT-01 environment. Steps: use the
  tray menu's "Show window" and "Exit". In a separately approved disposable desktop,
  test removal of a test-owned tray host; never terminate the user's desktop services.
  Expected: Show window re-activates the existing window; Exit shuts down; a vanished
  tray host does not lose the only usable control surface (recovery behaviour is a
  Stage 4 item, visibility/menu are Stage 0 evidence).
  Level H · Stage 0 / Sol · **Not run**.

- **DT-06 Accessibility inspection of the actual window.** Prerequisite: DT-01
  environment plus a screen reader. Steps: keyboard-traverse and read out the window
  (labels, focus order, contrast). Expected: all controls announced; Exit reachable by
  keyboard. Level H · Stage 0 / Sol · **Not run**.

## 4 Backends and portability (BE)

- **BE-01 X11/XWayland route.** Prerequisite: one X11 session and one Wayland session
  using XWayland. Steps: `"$app" --smoke-test` on each; record which backend
  `UsePlatformDetect()` selected. Expected: exit 0 on both routes, window usable.
  Level H · Stage 0 / Sol · **Not run** (only one environment so far).

- **BE-02 Native Wayland.** Avalonia's opt-in native Wayland backend is early-stage in
  the pinned 12.1.x line and is neither implemented nor validated in this spike.
  Steps: evaluate with the documented opt-in for the pinned version on a native
  Wayland session; record the result, do not claim support.
  Level H · Stage 0 / Sol · **Not run — blocked** (no implementation or environment).

- **BE-03 Native dependency inventory.** Steps: run `ldd` and ELF symbol-version
  inspection on the executable and both bundled native libraries. Expected: record
  resolved/missing dependencies and ABI requirements, including dynamically loaded
  dependencies before qualification. S0 observed maximum GLIBC requirements of 2.38
  (executable), 2.27 (Skia) and 2.14 (HarfBuzz); all resolved on that host, including
  Skia's fontconfig/FreeType dependency tree. This does not certify an older baseline
  or dynamically loaded GUI, font, ICU or tray facilities.
  Level A · Stage 0 / Astra · **Partial** (S0 inventory; baseline selection and a real
  baseline-distro run are required before Stage 0 closes).

- **BE-04 Distro matrix.** Prerequisite: the published archive plus Arch/CachyOS,
  Ubuntu and Fedora x86-64 machines or VMs. Steps: extract, check dependencies and the
  executable bit, run `--check-resources` and a smoke test on each. Expected: success on
  all three with no new warnings; untested combinations are labelled, not advertised.
  Level H · Stage 5 gate (evidence collected in Stage 0) / Astra · **Not run — blocked**
  (no other distros available here).

## 5 Fixture specification: Discord synthetic sockets (Stage 2, design only)

Guardrail for every case below: fixtures bind and connect **only** under a test-owned
temporary directory. Inject a closed candidate list before discovery; environment
redirection alone cannot isolate future `/run/user` or `/tmp` fallbacks. Assert every
candidate is test-owned before any connect or bind, not after discovery has connected.
If environment overrides are needed, use an isolated subprocess, not parallel tests'
shared environment. It never touches `/tmp/discord-ipc-*` (`DiscordIpcConstants.FallbackDirectory`) or
any real client's socket, and no fixture ever publishes an activity.

Contract question for Astra (blocks execution): `DiscordConnector`
(`companion/Tf3Drpc.Core/Discord/DiscordConnector.cs`) reads the environment and returns
a single root directory, then connects to the first accepted endpoint of 0–9. There is no
seam to inject candidate roots/indices or per-candidate handshake outcomes, so the cases
below are design-only until a candidate-provider contract is approved in Stage 2.

- **DX-01 Missing endpoints.** Input: test directory with no `discord-ipc-*` files.
  Expected: `ConnectAsync` returns null; the presence client retries only after
  `DiscordIpcConstants.ReconnectInterval` (15 s). The in-memory analogue
  (`ConnectorWithoutDiscord_IsRetriedOnlyAfterTheIntervalAsync` in
  `companion/Tf3Drpc.Core.Tests/Discord/DiscordPresenceClientTests.cs`) passes, but no
  ordinary synthetic test exercises Unix-socket discovery. Level D · Stage 2 / Qwen.

- **DX-02 Stale socket path.** Input: a regular file named `discord-ipc-0` in the test
  directory. Expected: treated as absent; discovery continues. Level D · Stage 2 / Qwen.

- **DX-03 Silent early endpoint, healthy later one.** Input: `discord-ipc-0` accepts
  connections but never answers the handshake; `discord-ipc-1` completes READY within
  `ReadyTimeout` (5 s). Expected: discovery advances past -0 within a bounded attempt
  budget and connects to -1. A bad early endpoint must not permanently mask a healthy
  later one. Not implemented today: the connector returns the first accepted stream, so
  -0 can win again on every reconnect. Level D · Stage 2 / Qwen (fixture), Astra
  (connector contract).

- **DX-04 Split frames.** Input: a READY frame whose 8-byte header and payload are
  written in separate `WriteAsync` chunks with a delay between them. Expected: the
  client reassembles and completes the handshake. Gap: no existing test delivers a
  READY frame deliberately split across writes. `FakeServer.SendRawAsync` in
  `companion/Tf3Drpc.Core.Tests/Discord/FakeDiscord.cs` accepts arbitrary bytes and can
  support such a test; the existing utility tests do not establish stream fragmentation. Level D · Stage 2 / Qwen.

- **DX-05 Timeouts and cancellation.** Input: an injected connect attempt that stalls
  past the connect deadline (currently 300 ms); separately, an accepted connection
  silent past the READY deadline (currently 5 s), and caller cancellation during a
  stalled write. Expected: each candidate is bounded; the overall discovery budget keeps the
  ~1 s monitor tick responsive; only `OperationCanceledException` propagates on
  cancellation (in-memory analogue: `Cancellation_IsTheOnlyThingThrownAsync`).
  Level D · Stage 2 / Qwen.

- **DX-06 Permission errors.** Input: injected permission-denied errors, supplemented
  by a test-owned directory with access removed and restored in fixture cleanup.
  Do not require another user's directory or privilege escalation. Expected: the candidate is skipped with a permission-denied
  diagnostic; no crash. The test skips itself (with a stated reason) when running as
  root, where the check cannot fail. Level D · Stage 2 / Qwen.

- **DX-07 Multiple clients and explicit selection.** Input: healthy `-0` and `-1`.
  Expected: Auto follows the approved root/index ordering (lowest index within the
  same root); the client never
  fans out to both; an explicitly selected endpoint that disappears is reported, not
  silently replaced by another account. Level D · Stage 2 / Qwen (fixture), Astra
  (selection contract).

- **DX-08 Overlong and invalid paths.** Input: a candidate whose resolved path exceeds
  Linux pathname-socket capacity (108 bytes including the terminating NUL), including
  multibyte names that exceed the byte limit despite fewer characters; invalid input
  representation awaits the approved .NET path contract.
  Expected: both are skipped without crashing or throwing an unhandled exception.
  Level D · Stage 2 / Qwen.

- **DX-09 Sandbox client subdirectories.** Input: fabricated
  `<test-dir>/app/com.discordapp.Discord/discord-ipc-0` (Flatpak-style) and a Snap-style
  runtime location. Expected: discovered when the host client is sandboxed; no symlinks
  are created or replaced. Level D · Stage 2 / Qwen (fixture), Astra (discovery scope).

## 6 Fixture specification: Steam paths, VDF and processes (Stage 3, design only)

All inputs are invented under a test-owned temporary root, with invented account IDs,
paths and content. No personal Steam or game files are copied. The current resolution
logic lives in `companion/Tf3Drpc.App/Services/PathServiceBase.cs` and the MAUI app's
process service; Core tests cannot reference them, so these cases also depend on the
Stage 1 seam that makes path/process logic testable.

- **PF-01 Secondary library.** Input: `<root>/steamapps/libraryfolders.vdf` listing a
  secondary library where `Transport Fever 3` is installed (invented
  `appmanifest_3493540.acf`, app ID from `AppConstants.GameSteamAppId`). Expected: the
  game directory resolves through the manifest, not the default
  `steamapps/common/Transport Fever 3` fallback; userdata stays under `<root>/userdata`.
  Level D · Stage 3 / Qwen (fixture), Sol (implementation).

- **PF-02 Duplicate roots.** Input: the same library reachable directly and through a
  symlink. Expected: deduplicated to one candidate. Level D · Stage 3 / Qwen.

- **PF-03 Quoted and escaped VDF values.** Input: `libraryfolders.vdf` entries with
  quoted paths, backslash escapes and embedded quotes. Expected: a bounded VDF reader
  decodes them correctly. Level D · Stage 3 / Qwen.

- **PF-04 Unicode and spaces.** Input: library paths containing spaces, CJK characters
  and `&`. Expected: preserved end to end in the resolved log and game paths.
  Level D · Stage 3 / Qwen.

- **PF-05 Corrupt and oversized input.** Input: truncated VDF, NUL bytes, a single
  absurdly long line. Expected: the reader reports the error or absence, never crashes
  and uses bounded memory. Level D · Stage 3 / Qwen.

- **PF-06 Multiple accounts.** Input: two `userdata/<invented-account>/3493540/local`
  trees with different log mtimes (set with file timestamp APIs). Control the comparison
  clock separately with `companion/Tf3Drpc.Core.Tests/SettableTimeProvider.cs` once the
  path contract permits clock injection. Expected: the newest plausible
  account wins, matching `UserDataUtils.ChooseFolder`. Level D · Stage 3 / Qwen.

- **PF-07 Future-dated logs.** Input: one log dated beyond now plus
  `GameStateConstants.FutureLogTolerance`, others plausible. Expected: the future log
  loses; it wins only when every log is future-dated (documented `ChooseFolder` remark).
  Level D · Stage 3 / Qwen.

- **PF-08 PID reuse and /proc races.** Input: a fake `/proc` tree where a PID vanishes
  between enumeration and identity read, or reappears with a different executable.
  Expected: the identity check fails closed (no presence from a stale process).
  Requires a process-identity seam; Level D · Stage 3 / Qwen (fixture), Sol
  (implementation), contract decision in Stage 1.

- **PF-09 Truncated process names.** Input: a process whose `comm` is truncated to
  15 characters. Expected: matching uses the executable path, not `comm`.
  Level D · Stage 3 / Qwen.

- **PF-10 Stale logs.** Input: a log whose mtime predates the freshness threshold while
  no game process runs. Expected: no presence is published; the existing stale-session
  rejection in the parser/state machine applies. Level D · Stage 3 / Qwen.

## 7 Known gaps: passing tests that do not prove the stated behaviour

1. **No synthetic Unix-socket discovery coverage in ordinary Core tests.** The opt-in
   live test uses the real connector but is skipped in normal runs. Ordinary
   `DiscordPresenceClientTests` run against the in-memory `FakeConnector`; `DiscordConnector`'s environment root
   selection and first-success semantics have no test (no reference to
   `XDG_RUNTIME_DIR` or `discord-ipc` in `companion/Tf3Drpc.Core.Tests`).
2. **`--check-resources` is narrow.** One string per language proves satellite
   presence, not that all 13 translations are complete or correct.
3. **`--smoke-test --no-tray` exit 0 is not a tray-less-desktop result** (DT-04 is the
   case that covers it).
4. **One environment's GUI smoke is not a desktop matrix.** KDE, GNOME, niri and X11
   are unverified; the backend used was not recorded (BE-01).
5. **Zero publish warnings is not portability.** The observed GLIBC_2.38 requirement
   prevents execution on older glibc versions; baseline selection and distro runs
   (BE-04) are pending.
6. **Deliberately fragmented READY delivery lacks a dedicated test** (DX-04);
   whole-frame writes can still be consumed through separate header/payload reads.
7. **Path and process logic is outside Core tests.** `PathServiceBase` and the process
   service live in the MAUI app project, so the PF cases are design-only until the
   Stage 1 seam lands.
8. **The three skipped Core tests are environment-bound** and are reported as skips,
   never as passes (BL-03).

## Open contract questions for Astra

- Candidate-provider seam for `DiscordConnector` (injected roots/indices, per-candidate
  handshake outcome, attempt budget) — prerequisite for all DX cases.
- Process-identity seam (fake `/proc`) — prerequisite for PF-08/PF-09.
- Fixture ownership and location for the BL-07 scratch publish (ignored local build vs.
  a Stage 2/4 test project).
