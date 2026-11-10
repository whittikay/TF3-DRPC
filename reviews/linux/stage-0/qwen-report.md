# Q0 report — bounded Linux acceptance and fixture design

## Integration review note (Astra)

Accepted as test design, with corrections in the integrated `docs/linux-test-matrix.md`.
The original observations below are retained as Qwen's handoff, not additional verified
results. In particular, TC-02 is partial (shared-SDK immutability was not checked);
BL-07 remains blocked because Sol's restricted-language publish still contained all
13 languages. Ordinary tests lack synthetic socket-discovery coverage, but an opt-in
live test does instantiate the real connector. `FakeServer.SendRawAsync` can send
arbitrary bytes; the gap is a dedicated fragmented-frame scenario, not an inability
of the existing helper to express it. Missing translation completeness coverage and
existing Discord discovery limitations are not regressions introduced by Stage 0.

The matrix also corrects connect versus READY deadlines, Unix pathname limits in bytes,
file timestamp versus clock injection, and pre-connect fixture isolation. Sol's S0-G1/G2
remain open; completion of Q0 does not close the Stage 0 gate.

## Original Q0 handoff

Task / owner / reviewer: Q0 (Stage 0 acceptance matrix and fixture specifications) / Qwen / Astra, with Sol consulting.

Base commit + bootstrap snapshot: `00592fd2b5cc6a36fa7a4c158ce5fbd830db7930` plus uncommitted
`stage0-bootstrap-01`; all 20 seeded paths matched their recorded SHA-256 checksums and
executable flags in this checkout before any edit.

Changed files beyond bootstrap:

- `docs/linux-test-matrix.md` (created) — the acceptance matrix and fixture specifications.
- `reviews/linux/stage-0/qwen-report.md` (created) — this report.
- Ignored `.agent-state/q0-verify/` in this worktree — raw command logs only; not public.

Behaviour implemented (and intentionally not implemented): test design only. Every Stage 0
case carries an ID, prerequisite, synthetic input or manual steps, expected observable
result, automation level, stage/owner and status; unexecuted cases are marked Not run.
Future Discord socket fixtures (DX-01…DX-09) and Steam path/VDF/process fixtures
(PF-01…PF-10) are design-only against the existing sources (`DiscordConnector`,
`DiscordIpcConstants`, `FakeDiscord`, `PathServiceBase`, `UserDataUtils`), referenced by
path; no production code, new test project, source, package, SDK pin, ledger or other
agent's file was touched. No GUI window was opened, no live Discord test run, no
Steam/game data accessed.

Commands actually run + exit codes/results (all in `.worktrees/qwen`, via the wrapper):

- `./tools/linux-dotnet.sh --version` → exit 0, `11.0.100-rc.3.26522.148`.
- `build TF3-DRPC.Linux.slnx -c Debug` → exit 0, 0 warnings / 0 errors.
- `test --project Tf3Drpc.Core.Tests/Tf3Drpc.Core.Tests.csproj -c Debug --no-build` → exit 0, 650 passed / 3 skipped / 0 failed.
- `build TF3-DRPC.Linux.slnx -c Release` → exit 0, 0 warnings / 0 errors.
- `test … -c Release --no-build` → exit 0, 650 passed / 3 skipped / 0 failed.
- `publish Tf3Drpc.Linux/Tf3Drpc.Linux.csproj -c Release -r linux-x64` → exit 0, no trim/AOT warnings in the log.
- Published `Tf3Drpc.Linux --check-resources` → exit 0, "Resource check passed for 13 languages."
- Published `Tf3Drpc.Linux --bogus` → exit 2 (usage on stderr).
- Non-AOT `dotnet Tf3Drpc.Linux.dll --check-resources` → exit 0, same result (managed-only run).

The worker checkout has no local SDK; the wrapper resolved the primary checkout's shared
`.tools/dotnet` and created its own `.local-build/` caches here (matrix TC-02).

Findings: severity, file:line, failure scenario, reproduction/test:

- Low / `companion/Tf3Drpc.Core/Discord/DiscordConnector.cs` (`GetSocketDirectory`, single-root
  first-success `ConnectAsync`): a silent early endpoint can keep winning discovery because the
  connector returns the first accepted stream and there is no per-candidate handshake failover;
  no seam exists to inject candidate roots for testing. Scenario: `discord-ipc-0` accepts then
  stalls, `discord-ipc-1` is healthy; the client never reaches -1. Reproduction is DX-03 once a
  candidate-provider contract is approved; no test can express it today.
- Low / `companion/Tf3Drpc.Core.Tests/Discord/FakeDiscord.cs` (`FakeServer.SendRawAsync`): whole-frame
  writes only, so split-frame delivery (header and payload in separate chunks) is untested.
  Scenario: a real client sends a READY frame across multiple reads; reassembly is assumed, not
  proven. Reproduction DX-04.
- Low / `companion/Tf3Drpc.Linux/ResourceCheck.cs`: one string per language; a passing check does
  not prove translation completeness (matrix gap 2). Reproduction BL-07 (scratch publish with one
  satellite excluded, expected exit 1).
- Info / published binary: ELF symbol-version inspection by the owner found a maximum GLIBC_2.38
  requirement (matrix BE-03); a conservative glibc baseline is not yet established.

Remaining risks / not verified: all H-level desktop cases (DT-01…DT-06, BE-01/BE-02/BE-04) remain
Not run in Q0 by design — no window was opened and no second distro, Wayland session or tray-less
desktop is available here. TC-03/TC-04 need a controlled PATH. The shared SDK read-only check of
TC-02 was not performed. DX/PF cases are blocked on the Stage 1/2 contracts named in the matrix.

Ready for review, blocked, or needs a scope decision: ready for Astra's review of the two owned
documents; three contract questions (candidate-provider seam, process-identity seam, BL-07 fixture
ownership) need Astra's answers before Qwen writes executable fixtures. Stopped before any
production code or new test project, as instructed. No commits, pushes, installs or live tests.
