# Sol independent review — A1.3 monitor orchestration

## Verdict

**Pass within the agreed scope. No correctness findings.** This approves the bounded monitor extraction, not completion of Stage 1. Native Windows/macOS validation remains explicitly out of scope and unverified, not a blocker for this verdict.

Identity checked: `PI_MODEL=gpt-6.1-sol`, provider `openai-codex`. Reviewed the primary checkout's uncommitted A1.1 + A1.2 + A1.3 snapshot above `899c64e`, isolating A1.3 against the original committed monitor implementation and the preceding approved extractions. Historical bootstrap baseline and Astra's A1.3 author manifest matched before review. A disposable source snapshot and SHA-256 manifest are retained under Sol's ignored `.agent-state/a13-review/`. All review-source checksums matched after mutation restoration. Primary sources and existing worker sources were not edited.

## Diff and contracts examined

- MonitorService matches its committed predecessor exactly after the declared namespace/visibility changes, removal of the internal-class suppression, substitution of Core's six source/sink interfaces and relocation of the interval constant. Loop method bodies are unchanged.
- IMonitorService and AppStatusChangedMessage match their originals after namespace/visibility changes. Global aliases preserve existing MAUI consumers and the shared message identity.
- The MAUI explicit singleton factory supplies the same existing process, paths, settings, translation, presence, Discord, messenger, clock and logger-factory instances previously resolved by constructor injection. It does not register another loop or construct another Discord transport.
- The one-second interval is preserved. Generated status-send logging retains its severity, template and exception parameter, with its logger category following the moved type.
- Stop still cancels and awaits the current loop; timer disposal occurs through the existing using declaration. Sequential duplicate Start, Stop-before-Start, repeated Stop, restart and Dispose semantics are preserved. The injected sink remains host-owned: AppLifecycleService stops the monitor before disposing Discord and quitting.
- Tests use a manually fired timer, synthetic Core interfaces, private messenger and channel-based observation. No process lookup, profile/log reading, game launch or socket discovery occurs. Bounded waits and fixture cleanup release the deliberately held tick on assertion failure.
- No production dependencies, Core behaviour, extra daemon/Generic Host, reflection, LINQ or mod protocol changes were introduced.

## Commands actually run

All builds/tests ran in the disposable source snapshot inside Sol's worktree, not against the primary checkout's build output.

| Check | Result |
|---|---|
| `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug` | Exit 0; zero warnings/errors |
| Same, `-c Release` | Exit 0; zero warnings/errors |
| Core tests, Debug and Release, `--no-build` | Each exit 0; 650 passed, 3 skipped, zero failed |
| Application tests, Debug and Release, `--no-build` | Each exit 0; 13 passed, zero failed |
| Linux Release Native AOT publish | Exit 0; no reported build/trim/AOT warnings |
| Published `--check-resources` | Exit 0; all 13 languages |
| Mutation: rethrow receiver exception after logging | Exit 2; receiver-containment test failed |
| Mutation: omit awaiting the in-flight loop during Stop | Exit 2; cancellation/wait test failed |
| Mutation: omit resetting the cancellation field after Stop | Exit 2; all three lifecycle tests failed |
| Restore original service bytes, rebuild/run Application Debug tests | Exit 0; all 13 passed |
| Reverify review-snapshot source manifest | All checksums matched |

The three Core skips remain the real translation-file check, Windows-only locking and opt-in live Discord test. Deliberate mutation failures are not baseline failures. Source was restored and clean tests rerun; raw logs remain ignored.

## Boundaries and remaining work

- Concurrent Start/Stop callers and recovery from a faulted loop are not new guarantees. Their pre-existing semantics are preserved, not independently approved as a general concurrent lifecycle API. Linux composition must respect the sequential ownership stated in the handoff.
- The Linux feasibility host still does not reference Application: its AOT/resource checks do not certify extracted-service native execution.
- No native Windows/macOS checks, desktop smoke, real game session, live Discord or visible-activity verification ran.
- Remaining presentation logic and dispatch/debounce/launcher/exit contracts are outside A1.3; Stage 1 and Stages 2/3 remain gated accordingly.
- Only this report and ignored review fixtures/logs were created in Sol's worktree. No primary ledger edits, integration, commits, pushes or branch changes were made.
