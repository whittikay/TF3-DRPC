# Sol independent review — A1.1

## Verdict

**Pass for the bounded, behaviour-preserving extraction, with one low-severity test finding. Not Stage 1 completion or all-platform integration approval.** Native MAUI builds/publishes and native execution of the extracted service remain outstanding, as declared in the author's handoff.

Identity checked: `PI_MODEL=gpt-6.1-sol`, provider `openai-codex`. Reviewed base `899c64e` plus the primary checkout's 21 changed/new/deleted A1.1 paths. A checksum manifest and disposable source snapshot live in Sol's ignored `.agent-state/a1-review/`. All snapshot checksums matched before validation and after the mutation experiment. Sol's preserved historical bootstrap baseline also matched all recorded checksums. The primary checkout and pre-existing worker files were not changed.

## Finding

### A1-T1 — Low: fallback regression test does not distinguish presets

- Location: `companion/Tf3Drpc.Application.Tests/PresenceServiceTests.cs:52–58`.
- Scenario: `InvalidPresetFallsBackToCareer` uses `GameState.MainMenu`. Core's `PresenceBuilder` handles this state without consulting the preset; preset resolution occurs in the playing-state branch. A regression selecting Network rather than Career therefore escapes the test.
- Reproduction: in the disposable review snapshot only, changed the service's invalid-preset fallback from `PresencePreset.Career` to `PresencePreset.Network`, then ran `./tools/linux-dotnet.sh test --project Tf3Drpc.Application.Tests/Tf3Drpc.Application.Tests.csproj -c Debug`. All 3 tests still passed, exit 0. Restored the source bytes and reverified the snapshot manifest. This is a test gap, not an observed defect in the extracted implementation.
- Suggested correction: use a playing snapshot with distinguishable Career/Network output, and verify the test fails under this mutation. A test-owned parsed status fixture can exercise the public Core surface without exposing internal status types.

## Diff and contracts examined

- Compared the extracted presence implementation and all three interfaces against the original committed files. After accounting for namespace, visibility and removed internal-class suppression, all four files match exactly; no method-body changes.
- Inspected both new project files, both solution additions, MAUI project reference/global aliases, host consumers and explicit MAUI Application qualifications, documentation and handoff changes.
- The new shared project depends only on Core and adds no runtime package version. No duplicated monitor, Discord transport, reflection or LINQ was introduced. Existing settings schema and mod protocol are unchanged.
- MAUI aliases preserve source consumers and DI registrations. Explicit MAUI Application qualifications address the new sibling namespace in the existing handwritten C# usages. Generated MAUI/XAML and platform compilation still require native validation.

## Commands actually run

Against the hash-verified disposable snapshot in Sol's worktree:

| Check | Result |
|---|---|
| `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug` | Exit 0; 0 warnings/errors |
| Same, `-c Release` | Exit 0; 0 warnings/errors |
| Core test project, `-c Debug --no-build` | Exit 0; 650 passed, 3 skipped, 0 failed |
| Core test project, `-c Release --no-build` | Exit 0; 650 passed, 3 skipped, 0 failed |
| Application test project, `-c Debug --no-build` | Exit 0; 3 passed |
| Application test project, `-c Release --no-build` | Exit 0; 3 passed |
| `./tools/linux-dotnet.sh publish Tf3Drpc.Linux/Tf3Drpc.Linux.csproj -c Release -r linux-x64` | Exit 0; no reported build/trim/AOT warnings |
| Published binary `--check-resources` | Exit 0; all 13 languages |
| Wrong-fallback mutation, application Debug tests | Exit 0; all 3 passed, demonstrating A1-T1 |

The three Core skips are the real translation-file check, Windows-only locking and opt-in live Discord test. The mutation used separate output after the clean baseline checks; mutated build output is not baseline evidence. Raw logs and machine paths remain ignored.

## Boundaries and next checks

- Linux feasibility-host AOT publish does not include the new Application project; it is only a host regression check. No claim of extracted-service native execution.
- No Windows/macOS builds or publishes, desktop smoke, real game launch, live Discord access or activity-display verification were performed.
- Current tests do not exercise remembered gameplay status through the service or distinct chosen/game translations. Extend these in the planned regression work; exact source equivalence supports the narrow extraction verdict, not broad behaviour coverage.
- Remaining Stage 1 extraction and platform contracts are not reviewed or approved here. Stages 2/3 remain gated.
- Only this review report and ignored review fixtures/logs were created in Sol's checkout. No production edits, commits, branch changes or pushes.
