# Q1.1 report — regression coverage for the A1.1 presence seam

## Verdict

**Ready for review.** Three new tests close Sol's A1-T1 gap and add the remembered-status
and distinct-texts coverage his review flagged as missing. Both key tests are proven
mutation-sensitive. No production code changed; no commits or pushes.

Identity checked: `PI_MODEL=Qwen3.8-27b-256k` (provider llama-swap) — Qwen per the working
agreement. Base: `899c64e` plus Astra's uncommitted A1.1 paths, copied read-only into this
worktree under `.agent-state/q1/manifest.json`; all 9 checksums matched before and after
every experiment. The historical bootstrap baseline also matched all recorded checksums.

## Task-owned changes (beyond the snapshot)

- `companion/Tf3Drpc.Application.Tests/PresenceServiceRegressionTests.cs` (new): three
  tests driving one real `GameMonitor` tick against a test-owned CRLF log fixture whose
  lines are timestamp-shifted copies of real `docs/log-samples/session.log` lines, with a
  fixed `TimeProvider`, a running process started 13 s before the log, and capturing
  presence sink.
  1. `InvalidPresetSetting_OnAPlayingGame_FallsBackToCareerAsync` — A1-T1: an invalid
     preset on a **playing** snapshot (rank 2) must render Career's "Mechanic", not
     Network's line/vehicle text.
  2. `PreviewAfterTryBuild_UsesTheRememberedStatusAsync` — after the tick, the service's
     preview uses the parsed status (rank 5 → "Expert"), not the example (rank 2).
  3. `DistinctChosenAndGameTexts_AreUsedForTheirOwnPartsAsync` — with a German game
     language, a custom `{saveName}` template and save name "Neues Spiel" (the game-
     language New Game label), the presence shows the chosen-language "New Game", proving
     `GameTexts` and `options.GameLanguageTexts` are wired separately.
- `docs/agent-handoffs/qwen-stage1.md` (new handoff).
- This report. Everything else in this worktree is the seeded bootstrap or the manifest-
  recorded A1.1 snapshot; raw logs stay ignored under `.agent-state/q1/`.

## Commands actually run (in this worktree)

| Check | Result |
|---|---|
| `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug` | Exit 0; 0 warnings/errors |
| Same, `-c Release` | Exit 0; 0 warnings/errors |
| Core tests, both configurations (`--no-build`) | Each 650 passed, 3 known skips, 0 failed |
| Application tests, both configurations (`--no-build`) | Each 6 passed (3 Astra + 3 Qwen) |
| Mutation: invalid-preset fallback Career → Network | New test 1 **fails** (exit 2), other 5 pass — A1-T1 closed |
| Mutation: `TryBuild` wired `GameLanguageTexts` to chosen texts | New test 3 **fails** (exit 2) |

After both mutations the service source was restored and the manifest re-verified. One
stale-binary artefact occurred during sequencing (a Debug run reused the mutation-built
binary); a clean rebuild reconfirmed all 6 pass — recorded here so it is not mistaken for
a finding.

## Findings for Astra

1. **A1-T1 (Sol) is closed by test 1.** His `InvalidPresetFallsBackToCareer` still uses
   MainMenu and remains mutation-blind; suggest he replaces or deletes it in favour of the
   new file, since both would assert the same fallback.
2. The new tests exercise Core's public monitor surface only (no `InternalsVisibleTo`, no
   production change), per the handoff stop conditions.
3. The fixture depends on `PauseAfter` > 10 s and `ProcessStartTolerance` ≥ 2 s; both are
   current constants, so the tests are stable against today's Core but will fail loudly if
   those tunings move — acceptable for a regression suite.

## Not verified / boundaries

- No Windows/macOS builds or publishes, no live Discord, no game launch, no desktop smoke.
- The Linux AOT publish was not re-run here; this task added tests only (Astra's A1.1
  publish evidence stands).
- Qwen does not approve its own work: Astra reviews, then the increment integrates.
