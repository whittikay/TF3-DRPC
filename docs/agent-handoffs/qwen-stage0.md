# Q0 — Qwen: bounded Linux acceptance and fixture design

- Owner: Qwen. Reviewer: Astra, with Sol consulting on Stage 0 findings.
- Base: upstream `00592fd2b5cc6a36fa7a4c158ce5fbd830db7930` plus `stage0-bootstrap-01`.
  Verify `.agent-state/bootstrap.json`; do not revert the seeded uncommitted changes.
- Checkout: `.worktrees/qwen`; branch `linux/qwen-stage0-tests`.
- Read: `AGENTS.md`, `docs/linux-implementation-plan.md`, `docs/linux-stages.md`, A0.

## Objective

Produce a concrete, bounded acceptance matrix for Stage 0 and fixture specifications for
the later Discord/path work. This is test design, not implementation of unapproved APIs.
Make it easy to distinguish an automated check from a real-desktop/client observation.

## Allowed writes

- `docs/linux-test-matrix.md` (create it).
- `reviews/linux/stage-0/qwen-report.md` (create it).
- Ignored `.agent-state/` for local notes/results in this worktree.

Do not edit source, project/package/solution files, SDK pins, AGENTS.md, the stage ledger
or another agent's files. Do not install packages, run live Discord tests, access actual
Steam/game data, change client settings or start additional servers.

## Required output

Give every case an ID, prerequisite, synthetic input or manual steps, expected observable
result, automation level, stage/owner and initial status. Mark unexecuted cases Not run.

Cover:

- Correct/missing pinned SDK; shared SDK with independent worktree caches; no MAUI workload
  installation; Linux wrapper used only for workload-free targets.
- Debug/Release/Core tests/AOT publish; published resource check; missing resource and
  invalid argument failures; compiled XAML startup versus a managed-only run.
- Optional tray enabled/disabled, no D-Bus/tray host, window close/Exit and timer cleanup.
  Do not confuse `--no-tray` with a real GNOME tray-less test.
- Native Wayland versus XWayland and actual distro dependencies, clearly pending where
  the spike has no implementation or available environment.
- Future synthetic socket cases: missing/stale/silent endpoints, valid later endpoint,
  split frames, timeouts/cancellation, permission errors, multiple clients and explicit
  selection. Fixtures must never bind or connect to the real `discord-ipc-*` paths.
- Future VDF/path/process cases: secondary libraries, duplicate roots, escaped paths,
  Unicode/spaces, corrupt/oversized input, multiple accounts, PID reuse and stale logs.
  Use invented paths and content, not copies of personal game/Steam files.

Identify any gap where current tests appear to pass but do not prove the stated behaviour.
Reference the existing source/tests by path rather than inventing interfaces.

## Optional verification and stopping point

You may run the existing non-live build/tests and published resource check using the
commands in the ledger if resources permit. Do not report these as run unless executed
in your own checkout. Do not open GUI windows for Q0; leave manual cases pending.

Return the two owned documents with a concise report and any questions about contracts.
Stop before writing production code or new test projects. Executable fixtures/tests will
be assigned in a follow-up once the relevant interfaces are approved.
