# Linux execution stages and agent assignments

The [implementation plan](linux-implementation-plan.md) is approved. This is the execution
ledger; [AGENTS.md](../AGENTS.md) is the authoritative working agreement. Linux support is
not yet released. Stage numbers match the plan, not completion percentages.

## Stage ledger

| Stage | Work and owner | Independent reviewer | Prerequisite | Exit gate | Status |
|---|---|---|---|---|---|
| **0 — Toolchain and desktop feasibility** | Astra: pinned SDK, Linux build entry point, Core baseline, Avalonia/AOT/resource/tray spike. Qwen: acceptance matrix | Sol (S0), Qwen for the A0-T hide-to-tray follow-up (user-reassigned) | Approved plan | Reproducible build/tests/publish; resources work in native binary; desktop/tray/backend and baseline-distro checks; review findings resolved | **Closed — go, scoped to Arch/niri/XWayland** on 2026-11-10: S0 review plus Qwen's independent hide-to-tray review passed; user selected Arch/niri as the baseline target. Screen-reader, tray-less-desktop, native-Wayland and other-distro checks remain unverified and are Stage 5/6 work, not claims |
| **1 — Shared application seam** | Astra: extract UI-neutral services, messages and presentation logic; define dispatch/timer/launcher/lifecycle contracts. Qwen: regression tests | Sol | Stage 0 go decision | Behaviour-preserving extraction; no new warnings; Core/application tests and Windows/macOS publish checks | Not started |
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
