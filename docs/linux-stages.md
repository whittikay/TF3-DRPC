# Linux execution stages and agent assignments

The [implementation plan](linux-implementation-plan.md) is approved. This is the execution
ledger; [AGENTS.md](../AGENTS.md) is the authoritative working agreement. Linux support is
not yet released. Stage numbers match the plan, not completion percentages.

## Stage ledger

| Stage | Work and owner | Independent reviewer | Prerequisite | Exit gate | Status |
|---|---|---|---|---|---|
| **0 — Toolchain and desktop feasibility** | Astra: pinned SDK, Linux build entry point, Core baseline, Avalonia/AOT/resource/tray spike. Qwen: acceptance matrix | Sol (S0), Qwen for the A0-T hide-to-tray follow-up (user-reassigned) | Approved plan | Reproducible build/tests/publish; resources work in native binary; desktop/tray/backend and baseline-distro checks; review findings resolved | **Closed — go, scoped to Arch/niri/XWayland** on 2026-11-10: S0 review plus Qwen's independent hide-to-tray review passed; user selected Arch/niri as the baseline target. Screen-reader, tray-less-desktop, native-Wayland and other-distro checks remain unverified and are Stage 5/6 work, not claims |
| **1 — Shared application seam** | Astra: extract UI-neutral services, messages and presentation logic; define dispatch/timer/launcher/lifecycle contracts. Qwen: regression tests | Sol | Stage 0 go decision | Behaviour-preserving extraction; no new warnings; Core/application tests and Linux publish checks; native Windows/macOS validation excluded by user, remains unverified | In progress — A1.1–A1.3 independently reviewed and integrated; presentation and platform contracts remain |
| **2 — Discord IPC** | Astra: candidate discovery, explicit selection, handshake failover, deadlines and diagnostics. Qwen: synthetic socket fixtures/tests in assigned files | Sol | Stage 1 contracts | Bad early endpoints cannot mask healthy ones; cancellation and routing tests; no automatic live socket access | Not started |
| **3 — Linux game integration** | Sol: Steam roots/VDF/manifests, secondary libraries, native process identity, overrides and Proton handling. Qwen: assigned path/process fixtures | Astra | Stage 1 contracts; can run alongside Stage 2 | Native game and secondary-library session verified; stale/racy process/log tests; actual Proton check before claiming Proton support | Not started |
| **4 — Desktop feature parity** | Sol initially owns the whole Avalonia UI; Astra owns shared integration contracts. Qwen: bounded UI/view-model regression coverage | Astra reviews Sol; Sol reviews Astra's integration | Stages 2 and 3 integrated | Settings/preview/languages/theme/templates/tray/single instance/exit work; no inaccessible hidden window; portability retained | Not started |
| **5 — Release qualification** | Astra: CI, native dependency baseline, portable archive, notices/checksums. Sol: published-build review. Qwen: documentation and regression matrix | Cross-review by code owner | Stage 4 | Published-binary matrix on target distros/desktops; Windows/macOS regressions checked; no unsupported compatibility claims | Not started |
| **6 — Broader setups** | Assign one setup per task: browser/arRPC, other wrappers, Snap variants and explicitly configured relays | Agent other than implementer | Stable shared contracts and Stage 5 baseline | Each advertised setup has reproducible prerequisites and a real end-to-end result | Not started |

Only Astra updates this ledger. Finishing one machine's smoke run does not close Stage 0.
The primary delivery sequence is 0 → 1 → (2 and 3 in parallel) → 4 → 5 → 6.
Research for a later stage is allowed; implementing it requires an assigned handoff.

## Worktrees and starting snapshots

| Agent | Checkout relative to the primary repository | Branch | Initial task |
|---|---|---|---|
| Astra | `.` | `linux/astra-stage0` | [A0: toolchain/spike](agent-handoffs/astra-stage0.md) |
| Sol | `.worktrees/sol` | `linux/sol-stage0-review` | [S0: independent review](agent-handoffs/sol-stage0.md) |
| Qwen | `.worktrees/qwen` | `linux/qwen-stage0-tests` | [Q0: acceptance design](agent-handoffs/qwen-stage0.md) |

The branches initially share upstream commit
`00592fd2b5cc6a36fa7a4c158ce5fbd830db7930`. The user subsequently authorised review
and a local Stage 0 integration commit. Worker branches and uncommitted snapshots are
preserved; that authorisation does not permit pushes, rebases or Stage 1 advancement.
The coordinator seeds all three with the uncommitted **`stage0-bootstrap-01`** snapshot
of the coordination documents, build configuration and feasibility host. In each checkout,
`.agent-state/bootstrap.json` records the seeded paths and SHA-256 checksums. Its
`baseline/` directory keeps the original seeded bytes for comparison. These files are
local and ignored. They are not a replacement for eventual reviewed commits.

Before starting:

1. Open only your assigned checkout and read `.agent-role.md`, `AGENTS.md` and your handoff.
2. Run `git status --short --branch`; expect seeded changes relative to `HEAD`. Do not
   revert them. Verify the snapshot's hashes before editing and report any mismatch.
3. Read the stage report and inspect the actual implementation, not just its summary.
4. Return only your task-owned changes and a report of tests actually run. Do not stage,
   commit, merge, rebase, push or change another worktree without user authorisation.

Astra integrates reviewed changes by named file/hunk, preserving all unrelated work.
Agents must not copy the whole worktree back. When commits are authorised, establish a
common committed baseline before advancing the worker branches; do not silently discard
these uncommitted snapshots. A shared SDK is read-only; build output and caches are local
to each checkout. No worker has been launched automatically by creating these worktrees.

## Stage 0 build and smoke commands

Run from the root of **your** checkout. The Linux-only wrapper changes into `companion/`,
so project paths below are relative to that directory.

```sh
./tools/linux-dotnet.sh --version
./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug
./tools/linux-dotnet.sh test --project Tf3Drpc.Core.Tests/Tf3Drpc.Core.Tests.csproj -c Debug --no-build
./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Release
./tools/linux-dotnet.sh test --project Tf3Drpc.Core.Tests/Tf3Drpc.Core.Tests.csproj -c Release --no-build
./tools/linux-dotnet.sh publish Tf3Drpc.Linux/Tf3Drpc.Linux.csproj -c Release -r linux-x64
```

The wrapper looks for a project-local `.tools/dotnet` SDK, then the primary worktree's
SDK, then `dotnet` on PATH. `TF3DRPC_DOTNET_ROOT` can select an existing SDK directory.
It does not download or install anything. `companion/global.json` remains authoritative;
do not change the pin to match another installed SDK.

The initial local SDK was downloaded from Microsoft's release metadata and checked
against its SHA-512 before extraction. For another machine, obtain the pinned SDK from
Microsoft through a reviewed installation step; do not run a script supplied by search
results. Never modify a shared/global SDK from a worker.

`MSBuildEnableWorkloadResolver=false` is scoped to this wrapper: Core and the Avalonia
host need no MAUI workloads, while the common `global.json` keeps the workload set for
Windows/macOS. Do **not** use this wrapper for MAUI builds or workload management.
It also removes `TF3DRPC_DISCORD_LIVE` from the child environment. Logs/caches/temp files
from builds stay under the checkout's ignored `.local-build/` and `.agent-state/`.

The initial published host is intentionally a **feasibility application**, not a working
Discord companion. It never starts a game monitor, reads Steam/game data, connects to
Discord or saves settings. Its English diagnostic UI is not the final translated UI.

```sh
app=companion/artifacts/publish/Tf3Drpc.Linux/release_linux-x64/Tf3Drpc.Linux
"$app" --check-resources
"$app" --smoke-test --no-tray
"$app" --smoke-test
```

`--check-resources` needs no display and checks a translated Core string in all 13
languages; it does not prove every translated string is correct. `--smoke-test` opens a
window and exits three seconds after its Opened event. Both GUI commands require a desktop
session. `--no-tray` deliberately skips tray attachment, but does not simulate every
tray-less desktop. Closing the window always exits this spike so it cannot become hidden
and unreachable. Without `--smoke-test` the host stays open for manual examination.
The tray's **Hide to tray** action hides rather than minimises the window, which works
with niri. It only hides after a bounded D-Bus check confirms a registered StatusNotifier
host. While hidden, a one-second UI timer rechecks availability (each probe has a
one-second deadline); loss or failure restores the window. This timer stops when shown
or exiting and does not start a game monitor. `--no-tray` retains a visible window.
Invalid arguments return exit code 2. These modes never publish a Discord activity.

Build notes:

- `.gitattributes` now enforces CRLF for C# to match upstream's analyser policy. A fresh
  Linux checkout otherwise generates thousands of formatting-only warnings. Shell entry
  points use LF through a scoped `tools/.editorconfig` override.
- Avalonia is pinned to **12.1.6** for this spike. `UsePlatformDetect()` selects the
  X11/XWayland route on Linux; native Wayland is not yet implemented or validated here.
- The .NET 11 RC3 optimised AOT publish skipped Avalonia's post-compilation XAML step:
  the binary built without warnings but failed at startup. The Linux project explicitly
  sets `UseOptimizedPublish=false`; the subsequently published binary passed both startup
  smoke modes. Keep this regression in review and recheck when the SDK changes.
- `AddReferenceSatellitesToIlc` preserves the referenced Core translations, as in the
  existing MAUI host. The published `--check-resources` mode is the validation, not only
  the managed test suite.
- The spike remains separate from the existing Windows/macOS solution and release scripts.
  No Linux release artifact is wired into CI yet.

## Observed validation and remaining gate

Astra's first Stage 0 pass (2026-11-10):

| Check | Result |
|---|---|
| Pinned SDK resolution through the wrapper | Passed |
| Linux solution Debug build | Passed, 0 warnings / 0 errors |
| Linux solution Release build | Passed, 0 warnings / 0 errors |
| Core tests, Debug | 650 passed, 3 skipped, 0 failed |
| Core tests, Release | 650 passed, 3 skipped, 0 failed |
| `linux-x64` Native AOT publish | Passed, no reported build/trim/AOT warnings |
| Published resource check | Passed for all 13 languages |
| Published GUI smoke with tray attachment disabled | Exit 0 after about 3 seconds |
| Published GUI smoke with tray attachment enabled | Exit 0 after about 3 seconds |
| Invalid CLI argument | Expected exit 2 |

Skipped tests: real game translation file at the default Steam path, Windows-only file
locking, and the opt-in live Discord test. These were not silently treated as passes.
Raw logs are ignored under `.agent-state/`; do not copy them into public reports.

**Still required before Stage 0 closes:** verification of tray visibility/menu
interaction and D-Bus behaviour; native
Wayland evaluation; tray-less desktop and baseline-distro runs; accessibility/resource
inspection of the actual window; native dependency and portability checks. Windows/macOS
builds were not run. No live Discord activity or real game session has been tested.

### A0 bootstrap revalidation

Astra re-ran the acceptance commands against the unchanged `stage0-bootstrap-01`
implementation at base `00592fd2b5cc6a36fa7a4c158ce5fbd830db7930`. All 20
bootstrap paths matched their recorded SHA-256 checksums and executable flags before
validation. The assigned branch and both worker worktree registrations were confirmed;
no worker checkout was read or changed.

- Pinned SDK version matched; Debug and Release builds each exited 0 with no warnings
  or errors. Core tests in each configuration exited 0: 650 passed, 3 skipped, 0 failed.
  Skips were the real German game translation file, Windows-only file locking and the
  opt-in live Discord test.
- Native AOT publish exited 0 with no reported build/trim/AOT warnings. The published
  resource check passed all 13 languages (exit 0); an invalid argument returned 2.
  Published GUI smoke runs with and without tray attachment each exited 0 within the
  20-second timeout. These are startup checks, not tray visibility or interaction evidence.
- `bash -n tools/linux-dotnet.sh` passed. Direct native dependency resolution reported
  no missing libraries on this host (`libm.so.6`, `libc.so.6` and the ELF loader).
  ELF symbol-version inspection found a maximum requirement of **GLIBC_2.38** in this
  binary. It therefore cannot establish compatibility with an older glibc baseline;
  a reviewed baseline build and separate distro runs remain required. This direct
  dependency check does not cover dynamically loaded GUI, font, ICU or tray libraries.
- Raw command logs and native dependency evidence remain ignored under
  `.agent-state/astra-a0-recheck/`. No application code, package pins or SDK files were
  changed. Only this ledger was changed beyond the bootstrap snapshot.

**Disposition:** ready for S0 review, not a Stage 0 go decision. All outstanding desktop,
accessibility, baseline-distro and Windows/macOS checks listed above remain unverified.
No game or Discord activity was started, and no commits or pushes were made.

## Reviewed Stage 0 integration

S0 and Q0 are complete as bounded review/design tasks, not as a Stage 0 go decision.
Astra reviewed and integrated the named documents, preserving both worker checkouts:

- [Sol's independent review](../reviews/linux/stage-0/sol-review.md): accepted. S0-G1
  (baseline-distro compatibility) and S0-G2 (desktop evidence) remain open. Sol reproduced
  the optimised-publish XAML failure (native exit 134) and successful default startup.
- [Acceptance matrix](linux-test-matrix.md) and [Qwen's task report](../reviews/linux/stage-0/qwen-report.md):
  accepted with documented integration corrections. Partial checks no longer claim full
  success; timeout, path-byte-limit, timestamp and fixture-isolation descriptions were
  corrected. Sol's unsuccessful satellite-removal experiment is not a negative-control pass.
- The binary requires GLIBC_2.38; bundled Skia/HarfBuzz require 2.27/2.14 respectively.
  A minimum baseline has not been chosen. No older-distro compatibility is certified.
- Candidate discovery/handshake and process-identity injection contracts remain future
  assigned design work. No executable DX/PF fixtures are authorised yet. Astra owns
  the outstanding missing-satellite negative control; it needs a separate bounded
  scratch-build design, not a new production API or test project in this commit.

Integration validation in the primary checkout re-ran Debug/Release builds (exit 0,
0 warnings/errors), Core tests in both configurations (650 passed, 3 skipped, 0 failed),
Native AOT publish (exit 0, no reported warnings), and the published resource check
(all 13 languages). Raw logs remain ignored under `.agent-state/stage0-integration/`.
No GUI checks were re-run during integration; S0's separate startup evidence is retained.

The local commit records the reviewed feasibility implementation and evidence only.
Stage 1 remains blocked. Worker branches still retain their original bootstrap base;
reconciliation with a committed baseline needs an explicit follow-up and must preserve
worker files rather than reset or overwrite their checkouts.

## A0-T — explicit hide-to-tray follow-up

The user requested hide-to-tray after validating the committed spike on Arch/niri.
Owner: Astra; reviewer: Qwen (user-reassigned from Sol on 2026-11-10 after the
active model was checked via `PI_MODEL`; see the identity-check note in AGENTS.md).
Base: local commit `df91364`.
Scope: Linux host, its existing D-Bus dependency reference, AGENTS and these validation
notes. No game/Discord transport, settings, start-hidden option or close-to-tray change.
This follow-up is uncommitted and needs independent review before integration.

The host now explicitly references Tmds.DBus.Protocol 0.94.1, already resolved by
Avalonia, without changing its version. Its tray menu can hide the window; Show window
restores it; Exit and normal window close still quit. A bounded read-only watcher
property query rejects hiding without a registered host. While hidden, availability
checks restore the window after a failed probe. A positive watcher property is not
proof of visibility on every desktop, so manual menu and recovery checks remain required.

Checks run in the primary checkout: Debug/Release builds (0 warnings/errors), Core tests
in both configurations (650 passed, 3 skipped), Native AOT publish (no reported warnings),
and native resource check (13 languages), all successful. Both published startup smoke
modes exited 0. An ignored source-linked probe verified present-host, cancelled-call,
unreachable-session-bus and malformed-address cases within its time bound. No tray host
was stopped and no desktop settings were changed. The user confirmed Hide to tray kept
the icon available, Show window restored the hidden window, and tray Exit quit afterward.
The recorded host was no longer running after Exit; its log contained no exception.

Synthetic recovery was subsequently validated against the published native binary in a
private test-owned D-Bus session, using an existing Python D-Bus/GLib installation and
an isolated synthetic StatusNotifier watcher. All nine assertions passed: initially
visible; hide with an available host; restore within five seconds when the host reports
unavailable; refuse hide while unavailable; hide after availability returns; restore
within five seconds when the watcher stops responding; hide after responsiveness returns;
restore within five seconds when the watcher exits; clean tray Exit after recovery.
X11 window state and matching process identity were checked, not just startup exit codes.
Only the synthetic watcher was paused/terminated; the real tray and desktop settings were
untouched. All test-owned processes were cleaned up. Fixture scripts and raw results
remain ignored under `.agent-state/hide-to-tray/recovery/`.
An author recheck repeated all nine assertions in fresh fixture directories and also
verified Show superseding a pending Hide, clean Exit during pending Hide, and visible
window recovery after loss of the entire private session bus. Debug/Release builds,
Core tests, AOT publish and the resource check were re-run successfully. This remains
an author recheck, not independent approval. The targeted
[S0-T handoff](agent-handoffs/sol-hide-to-tray.md) records the snapshot and review scope.

Qwen completed the independent review in its worktree against a hash-verified
disposable snapshot of `df91364` plus the 11 follow-up files: Debug/Release builds,
Core tests (650 passed, 3 known skips per configuration), AOT publish, the 13-language
resource check and 14 fresh synthetic recovery/race assertions all passed against its
review-published binary; findings are informational only. Verdict:
**pass, scoped to Arch/niri/XWayland** (report in Qwen's worktree,
`reviews/linux/stage-0/qwen-hide-to-tray-review.md`).

**Stage 0 closure.** On 2026-11-10 the user directed this session (Qwen, active model
verified via `PI_MODEL`) to execute the closure and local commit from it, overriding the
"only Astra updates this ledger" note for this one entry; the user had earlier
authorised the conditional commit if all checks passed. Stage 0 is closed with a **go
decision scoped to Arch/niri/XWayland**: the pinned SDK builds workload-free, Native AOT
publishes without warnings, resources work in the native binary, and the desktop/tray/
backend evidence above holds on the user's selected baseline. Explicitly not claimed:
screen-reader access, a genuine tray-less desktop, native Wayland and any other
distribution; BL-07 (missing-satellite negative control) remains an open Astra
follow-up with its separate bounded design. Stage 1 may start from this go decision;
no push was made and none is authorised.

Before this follow-up, the user confirmed the committed host's rendered German sample,
tray visibility, Show window focus, tray Exit, window Exit, normal close, and visible
Tab focus plus Enter activation. Each exit was followed by a process-stopped check.
The app's X11 properties matched the launched PID/class, confirming the XWayland route
on niri. Screen-reader access was not tested (none configured); a genuine tray-less
desktop and native Wayland were not tested. The user selected Arch/niri as the immediate
validation target rather than Ubuntu; no other distro support is inferred and the
Stage 0 gate is not closed by this narrower local evidence.

## A1.1 — shared presence seam

The user authorised beginning Stage 1 implementation after Stage 0 closure. Astra
(active model checked) started from clean `899c64e` in the primary checkout; worker
checkouts and historical bootstrap manifests were not changed. Scope and contracts:
[handoff](agent-handoffs/astra-stage1.md).

The first increment adds `Tf3Drpc.Application` and `Tf3Drpc.Application.Tests` to both
solutions. The existing PresenceService and its three settings/translation/presence
interfaces move to the shared project; method bodies and signatures are unchanged,
with visibility widened for host access. MAUI references the project and uses global
aliases, with explicit MAUI Application qualifications to avoid the sibling namespace
collision. No Linux host composition or game/Discord access was added.

Validation against the uncommitted A1.1 snapshot above `899c64e`:

- `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug` and `-c Release`:
  passed, 0 warnings/errors after correcting test expression-body formatting.
- `./tools/linux-dotnet.sh test --project Tf3Drpc.Core.Tests/Tf3Drpc.Core.Tests.csproj`
  with `-c Debug --no-build` and `-c Release --no-build`: each 650 passed, 3 known
  skips, 0 failed (real translation file, Windows locking, opt-in live Discord).
- Equivalent commands for `Tf3Drpc.Application.Tests/Tf3Drpc.Application.Tests.csproj`:
  each 3 passed, 0 failed. Synthetic dependencies cover preview/current settings/time,
  invalid-preset fallback and null inputs; no sockets or filesystem settings are used.
- `./tools/linux-dotnet.sh publish Tf3Drpc.Linux/Tf3Drpc.Linux.csproj -c Release -r linux-x64`:
  passed without reported warnings; published `--check-resources` passed all 13 languages.
  This is a feasibility-host regression check, not native execution of the extracted service.
- `git diff --check`: passed. Raw logs remain ignored under `.agent-state/stage1/`.

Outstanding: Sol's independent review, further application regression coverage, native
Windows/macOS builds/publishes and extracted-service AOT execution. This is only the first
Stage 1 increment. Settings/messages, monitor orchestration, view-model logic and
platform dispatch/timer/launcher/lifecycle contracts still need extraction. Stages 2/3
are not yet unblocked. No commits, pushes, live Discord tests or game launches were made.

## A1.1 integration and A1.2 settings extraction

The user authorised integration and continued Stage 1 work after reviewing
[Astra's review](../reviews/linux/stage-1/astra-a1-review.md). Native Windows/macOS
validation is explicitly out of scope for this work: it remains unverified, not a local
integration blocker. This supersedes the native-check gate in the original A1.1 handoff;
it does not certify those platforms or remove future release validation.

Astra integrated only the reviewed Qwen regression test, its handoff and report, and
Sol's review report. The old MainMenu test was renamed to describe its actual coverage;
task-specific comments in the new test were removed. Worker files were not changed.
The independent three-mutation results are recorded in Astra's review. A1-T1 is now
closed in the primary source tree. A1.1 is accepted within the revised validation scope.

[A1.2 handoff](agent-handoffs/astra-stage1-settings.md) bounds the next increment:
SettingsService and SettingsChangedMessage move into Application with source-generated
logging and the already pinned CommunityToolkit.Mvvm dependency. A narrow ISettingsPaths
contract is implemented by the existing MAUI path service; DI registers the same instance
under both interfaces. Settings schema, save/retry behaviour and message delivery are
unchanged. The monitor loop and other messages remain in MAUI for the next increment.

Validation ran in the primary checkout against the uncommitted A1.1 + A1.2 snapshot above
`899c64e`, including the integrated Qwen tests:

- Debug/Release Linux solution builds: exit 0, zero warnings/errors.
- Core tests in each configuration: 650 passed, 3 known skips, zero failures.
- Application tests in each configuration: 10 passed, zero failures. Four new settings
  tests cover writes/notifications, unchanged-value suppression, normalisation, invalid
  language rejection, memory retained on failure, failed-write retry and writable recovery.
- Linux Native AOT publish: exit 0, no reported warnings; published resource check passed
  all 13 languages. This still does not execute Application in native code.
- The first Debug build exposed an inaccessible private test recipient in the messenger
  generator; making that fixture type internal resolved it without suppressions.
- Logs stay ignored under `.agent-state/stage1-settings/`. No game, live Discord, GUI
  smoke or native Windows/macOS checks ran. No commits or pushes were made.

A1.2 is ready for independent Sol review, not self-approved. Qwen may add bounded
settings edge-case coverage in a new test file under a separate handoff. Full Stage 1
and Stages 2/3 remain gated on the remaining shared orchestration/presentation contracts.

## A1.2 integration and A1.3 monitor extraction

The user authorised integrating [Sol's A1.2 review](../reviews/linux/stage-1/sol-a12-review.md)
and proceeding. Astra verified that all A1.2 author-snapshot checksums still matched,
then copied only the review report into the primary tree. Sol found no correctness issues
and independently reproduced Debug/Release builds, Core/Application tests, Linux AOT and
resources, plus three mutation checks. A1.2 is accepted within the agreed scope;
Windows/macOS remains explicitly unverified and out of scope.

[A1.3 handoff](agent-handoffs/astra-stage1-monitor.md) moves the existing single monitor
loop, its public interface, status message, generated error log and one-second interval
into Application. The constructor now accepts Core's six existing source/sink interfaces;
MAUI's explicit singleton factory supplies the same host services as before. Loop method
bodies and sequential lifecycle behaviour are otherwise unchanged. No new loop, socket
access or filesystem discovery was introduced. The host still owns Discord disposal
and activity clearing after stopping the loop; concurrent Start/Stop is not a new promise.

Validation in the primary checkout against uncommitted A1.1 + A1.2 + A1.3 above `899c64e`:

- Debug/Release Linux solution builds: exit 0, zero warnings/errors.
- Core tests per configuration: 650 passed, 3 known skips, zero failures.
- Application tests per configuration: 13 passed, zero failures. Three new tests use a
  manually fired TimeProvider timer and synthetic dependencies only: immediate/periodic
  ticks, duplicate Start, one-second cadence, Stop/restart, receiver exception containment,
  cancellation while a tick is held, awaiting its completion and host-owned sink disposal.
- Linux Native AOT publish: exit 0, no reported warnings; native resource check passed
  all 13 languages. Application native execution is still not exercised by this host.
- Initial test-only analyser findings were fixed (test cancellation, asynchronous
  registration disposal and empty-body formatting); no suppressions added.
- Raw logs remain ignored in `.agent-state/stage1-monitor/`. No native Windows/macOS,
  desktop, game or live Discord validation ran. No commits or pushes were made.

A1.3 is ready for independent Sol review, not self-approved. Qwen was not dispatched:
the bounded lifecycle tests ran locally without needing additional test ownership.
Remaining Stage 1 work includes presentation extraction and dispatcher/debounce/launcher/
exit contracts. Stages 2/3 remain gated on those agreed contracts.

## A1.1–A1.3 reviewed integration

The user authorised Astra to integrate the A1.3 review, commit and push the reviewed
increments. [Sol's A1.3 review](../reviews/linux/stage-1/sol-a13-review.md) passed with no
correctness findings. Astra verified both the author manifest and Sol's full review-source
manifest against the current primary sources before copying the named report; worker
checkouts remain unchanged. A1.1–A1.3 are accepted within the agreed validation scope.

Final integration checks in the primary checkout above `899c64e`: Debug/Release Linux
solution builds each passed with zero warnings/errors; Core tests each passed 650 with
3 known skips; Application tests each passed all 13; Linux Native AOT publish passed
without reported warnings and the native resource check passed all 13 languages. Raw
logs remain ignored under `.agent-state/stage1-integration/`.

This integration does not close Stage 1 or unblock Stages 2/3. Presentation logic and
platform dispatch/debounce/launcher/exit contracts remain. Windows/macOS validation is
out of scope and unverified; extracted-service native execution, GUI/game behaviour and
live Discord remain unverified. The existing Linux host is still a feasibility host.
The authorised commit/push targets `linux/astra-stage0` on the fork's `origin`; it does
not authorise merging, releasing or changing worker branches.

## Task report template

```text
Task / owner / reviewer:
Base commit + bootstrap snapshot:
Changed files beyond bootstrap:
Behaviour implemented (and intentionally not implemented):
Commands actually run + exit codes/results:
Findings: severity, file:line, failure scenario, reproduction/test:
Remaining risks / not verified:
Ready for review, blocked, or needs a scope decision:
```

After two failed correction cycles on the same issue, stop and ask for a design decision.
A successful self-check is not independent approval. Do not advance a stage just because
its owner declares it complete.
