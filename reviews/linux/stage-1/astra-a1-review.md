# Astra review — A1.1 worker deliverables

## Verdict

**Accept Sol's bounded extraction review and Qwen's regression tests. No blocking code findings. Stage 1 is not complete.** This review does not grant all-platform integration approval or unblock Stages 2/3.

Active identity: `gpt-6-astra`, provider `openai-codex`. Reviewed the primary checkout's uncommitted A1.1 changes above `899c64e`, Sol's `reviews/linux/stage-1/sol-a1-review.md` and Qwen's `PresenceServiceRegressionTests.cs`, handoff and report in their respective worktrees. Worker checkouts were read only.

## Findings and disposition

- Sol's A1-T1 is valid: the existing MainMenu test does not distinguish Career from Network. Qwen's playing-state test closes the coverage gap in the candidate combined suite. It is not yet integrated into the primary test project. Rename the older test to describe its MainMenu coverage, or remove it during integration; it must not be cited as fallback-specific evidence.
- All four extracted production files match their committed predecessors after namespace/visibility changes and removal of the internal-class suppression. No behavioural changes were found. MAUI references, aliases and explicit Application qualifications are consistent with the extraction; generated/platform code still needs native validation.
- Qwen's fixtures use test-owned files, injected process/path/translation dependencies and a capturing presence sink. They do not discover Discord or launch the game. Each new test detects its corresponding production mutation.
- The fixture is appropriately bounded for these three cases, not a general JSON fixture generator or broad monitor acceptance suite. The original test and new test comments contain task-specific wording; removing that wording is minor integration tidy-up, not a correctness blocker.

## Independent validation actually run

A disposable source snapshot under ignored `.agent-state/astra-a1-review/` contains the current primary sources plus exactly Qwen's new regression test file. A SHA-256 manifest records the tested source bytes. Builds and tests ran in this snapshot, not in either worker checkout. All source checksums matched after the mutation experiments.

| Command/check | Observed result |
|---|---|
| `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug` and `-c Release` | Both exit 0, zero warnings/errors |
| Core test project, both configurations, `--no-build` | Each 650 passed, 3 skipped, zero failures |
| Application test project, both configurations, `--no-build` | Each 6 passed, zero failures |
| Wrong fallback: Career → Network | Exit 2; only Qwen's fallback test failed |
| Wrong game-language texts: use chosen texts in TryBuild | Exit 2; only Qwen's distinct-texts test failed |
| Remove TryBuild's preview Remember call | Exit 2; only Qwen's remembered-status test failed |
| Restore original production bytes, rebuild/run Application Debug tests | Exit 0; all 6 passed |
| Linux Release Native AOT publish | Exit 0; no reported build/trim/AOT warnings |
| Published `--check-resources` | Exit 0; all 13 languages |
| Primary `git diff --check` | Exit 0 |

The three Core skips remain real translation-file access, Windows locking and opt-in live Discord. Raw logs and the checksum manifest stay ignored. The Linux feasibility host does not reference Application, so its AOT success is not extracted-service native execution evidence.

## Remaining gates

- Integrate the individually reviewed worker files; neither tests nor worker reports were copied into the primary source tree by this review.
- Native Windows/macOS build/test/publish regression checks and extracted-service native execution remain unverified.
- Settings/messages, monitor orchestration, presentation logic and dispatch/timer/launcher/lifecycle contracts remain future Stage 1 increments.
- No desktop smoke, real game session or live Discord test was run. No commits, pushes, dependency changes or worker edits were made.

Changed tracked-scope path in this review: this report only. Ignored review sources and build output are retained separately.
