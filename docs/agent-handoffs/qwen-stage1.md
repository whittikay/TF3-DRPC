# Q1.1 — regression coverage for the A1.1 presence seam

- Owner: Qwen (active model checked via `PI_MODEL`). Reviewer: Astra.
- Base: Sol's A1.1 review verdict (pass, one low finding A1-T1) against `899c64e` plus
  Astra's uncommitted A1.1 paths, copied read-only into this worktree under
  `.agent-state/q1/manifest.json` (checksums verified before editing).
- Objective: close the A1-T1 test gap and add bounded regression coverage of the
  extracted `PresenceService` through Core's public monitor surface, without touching
  shared production code.
- Allowed files: new `companion/Tf3Drpc.Application.Tests/PresenceServiceRegressionTests.cs`,
  this handoff, a report under `reviews/linux/stage-1/`, ignored `.agent-state/q1/`.
- Non-goals: editing Astra's existing test file (the corrected fallback assertion lives in
  the new file; Astra decides whether to keep or replace his weaker one), settings/message
  extraction, platform adapters, any production behaviour change.
- Contracts: `GameMonitor` and its six public source interfaces; fixture log lines are
  timestamp-shifted copies of real `docs/log-samples/session.log` lines. No live Discord,
  no game launch, no files outside the test-owned temp directory.
- Acceptance: Debug/Release builds, Core and Application tests, and a mutation check
  (wrong invalid-preset fallback must make the new test fail).
- Stop conditions: needing a production change, a Core `InternalsVisibleTo`, a new package
  version, or unexplained fixture flakiness.
