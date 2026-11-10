# S0-T — Independent hide-to-tray review and scoped gate recommendation

> **Reassigned by the user on 2026-11-10:** the active session model was Qwen,
> not Sol, so the user assigned this review to Qwen. It ran in Qwen's worktree
> against the same snapshot and scope; report:
> `.worktrees/qwen/reviews/linux/stage-0/qwen-hide-to-tray-review.md`
> (verdict: pass, scoped to Arch/niri/XWayland).

- Owner/reviewer: Sol. Implementer: Astra.
- Base: primary checkout commit `df91364` plus the uncommitted `hide-to-tray-review-01`
  snapshot recorded in the primary checkout's ignored
  `.agent-state/hide-to-tray/review-snapshot.json`.
- Dependency: A0-T implementation and the completed original S0 review. This is a
  targeted follow-up, not a repeat of unchanged Stage 0 code.
- Objective: independently review hide/show/exit and recovery, then recommend whether
  Stage 0 can close for the user-selected **Arch/niri/XWayland** feasibility target.
- Non-goals: no broader distro qualification, native Wayland support claim, production
  companion UI, game/Discord connections or Stage 1 implementation.

## Scope and isolation

Read the primary checkout only for this assigned review. Do not modify it or another
worker's files. Sol's original branch is still based on upstream plus its bootstrap;
do not reset, switch, rebase or discard that checkout to obtain the new snapshot.

Allowed writes: `reviews/linux/stage-0/sol-hide-to-tray-review.md` in Sol's checkout,
and its ignored `.agent-state/` for evidence and a disposable build snapshot. Construct
that snapshot from `git archive df91364` plus the exact manifest-listed current files;
verify their hashes before using them. Preserve the original worker bootstrap and report.
Build/test only the disposable snapshot in Sol's checkout, with local caches and the
verified SDK read-only. A wrapper override may select the existing primary SDK; no
SDK installation or workload management. No source fixes in this review task.

No commits, pushes or desktop/client configuration changes by the reviewer. The user
has authorised the coordinator to commit only after the review/closure conditions hold.

## Contracts and review focus

- Normal close and Exit still terminate. Startup remains visible; there is no
  start-hidden option. Only explicit Hide to tray hides the window.
- A one-second bounded D-Bus query checks `IsStatusNotifierHostRegistered`. A true
  response is not proof that this particular icon is rendered; assess that limitation
  against the real-host observation and recovery behaviour.
- While hidden, a one-second UI timer repeats bounded probes; a failed query restores
  the window. Showing or exiting stops that timer. No game monitor is introduced.
- Show supersedes a pending Hide through the visibility version. Exit cancels pending
  work and disposes the timer/token source/tray. Inspect async event-handler exceptions,
  query disposal after timeouts, repeated requests and shutdown races.
- `Tmds.DBus.Protocol` 0.94.1 is made explicit only for the Linux host, matching the
  existing Avalonia transitive version. No SDK/Core/MAUI behaviour changes.

## Evidence to reproduce, not assume

Astra's author recheck found no blocking defect; this is not independent approval.
Debug/Release builds: no warnings/errors. Core tests: 650 passed, 3 known skips per
configuration. Native AOT publish and all 13 resource checks passed.

The private D-Bus synthetic watcher exercised nine original recovery assertions, then
additional pending-Hide/Show, Exit-during-Hide and entire-session-bus-loss scenarios.
All passed against the published executable, checking its X11 state and matching PID.
Raw fixtures/results are in the primary checkout's ignored
`.agent-state/hide-to-tray/{recovery,review-check,review-pending-hide,review-bus-loss}/`.
Read/copy only the named fixture scripts into review-owned local evidence if needed.
Use fresh directories per run: the scripts contain ready/registration marker files
that must not be reused as evidence of a newly launched fixture. No live Discord paths.
Only the private synthetic watcher may be paused/terminated; never the user's tray host.

The user separately confirmed actual Arch/niri tray visibility, Show focus, Hide/Show,
Exit, window close, visible keyboard focus/Enter activation and the rendered German
resource sample. Process checks confirmed intentional Exit stopped the app. X11 window
properties confirmed XWayland, not native Wayland. Screenshots are unnecessary; if
needed, only the feasibility window is permitted, never the desktop or Discord.

## Gate decision and deliverable

Return severity, file/line, scenario and reproduction for findings; commands actually
run, exact tested snapshot, skips, risks and an explicit recommendation. Stop after two
failed correction cycles or if snapshot/configuration differs unexpectedly.

The original broad S0 gate is not silently satisfied: Ubuntu/Fedora, actual tray-less
GNOME, screen-reader access and native Wayland remain unverified. The user selected
Arch/niri rather than Ubuntu for immediate qualification; assess a scoped go with those
limitations explicit, not a universal Linux support statement. Coordinator records any
approved scope decision in the plan and ledger before closure. A reviewer recommendation
is required; author self-checks cannot substitute for it.
