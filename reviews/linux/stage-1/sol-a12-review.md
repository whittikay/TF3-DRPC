# Sol independent review — A1.2 settings persistence

## Verdict

**Pass within the agreed scope. No blocking or non-blocking correctness findings.** This approves the bounded extraction, not completion of Stage 1. Native Windows/macOS checks are explicitly out of scope by user direction, remain unverified and are not a blocker for this verdict.

Identity checked: `PI_MODEL=gpt-6.1-sol`, provider `openai-codex`. Reviewed the primary checkout's uncommitted A1.1 + A1.2 snapshot above `899c64e`, with A1.2 isolated by comparison to the committed settings implementation and the A1.1 handoff/review. The snapshot and SHA-256 manifest are retained under Sol's ignored `.agent-state/a12-review/`. Historical bootstrap baseline checksums matched before review; all review-snapshot source checksums matched after mutation restoration. The primary checkout and existing worker sources were not edited.

## Diff and contracts

- SettingsService matches its committed predecessor exactly after namespace, visibility, path-interface name and internal-class suppression changes. All method bodies, normalisation, state updates, save ordering, writable recovery, failed-save retry and messenger-send behaviour are preserved.
- SettingsChangedMessage retains its payload and record semantics; its public shared identity is used by the service and all existing MAUI consumers through the global alias. No remaining fully qualified reference to the old message/service identity was found.
- ISettingsPaths extracts exactly the three settings-related path members. Existing IPathService inherits it, and DI registers the existing platform path instance under both interfaces. No extra path instance, alternate storage location or discovery was introduced.
- The six moved logging declarations retain their log levels, templates and parameter types. Logging remains source-generated. The new shared service's logger category necessarily follows its new namespace.
- Application references the centrally pinned CommunityToolkit.Mvvm package, with no package-version change. No monitor loop, native API, settings schema, protocol, reflection or LINQ change was introduced.
- The four new tests use isolated test-owned temporary directories, injected path state and a private messenger. They cover successful saves and delivery, unchanged-value suppression, memory retained after failure, notifications on failure, retry after both a rejected writable probe and a real write failure, template normalisation, invalid language and reload. No Discord discovery or game launch occurs.

## Commands actually run

All builds/tests ran against the disposable source snapshot in Sol's worktree, not the primary checkout or its build output.

| Check | Result |
|---|---|
| `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx -c Debug` | Exit 0; zero warnings/errors |
| Same, `-c Release` | Exit 0; zero warnings/errors |
| Core tests, Debug and Release, `--no-build` | Each exit 0; 650 passed, 3 skipped, zero failed |
| Application tests, Debug and Release, `--no-build` | Each exit 0; 10 passed, zero failed |
| Linux Release Native AOT publish | Exit 0; no reported build/trim/AOT warnings |
| Published `--check-resources` | Exit 0; all 13 languages |
| Mutation: remove LastSaveFailed exception to unchanged-value suppression | Exit 2; both retry tests failed |
| Mutation: send SettingsChangedMessage only on successful save | Exit 2; failed-save notification test failed |
| Mutation: skip in-memory template normalisation | Exit 2; template test failed |
| Restore original service bytes, rebuild/run Application Debug tests | Exit 0; all 10 passed |
| Reverify review-snapshot source manifest | All checksums matched |

The three Core skips are the real translation-file check, Windows-only locking and opt-in live Discord test. Mutation results were deliberately failing experiments, not baseline failures; source bytes were restored and the clean tests rerun. Raw logs remain ignored.

## Boundaries

- Linux host AOT publish remains a feasibility-host regression check: it does not include or execute Application. Extracted-service native execution remains unverified, not falsely certified by this review.
- No native Windows/macOS builds/publishes, GUI smoke, live Discord, real game session or visible activity checks were performed.
- Monitor orchestration, other messages, view models and platform dispatch/timer/launcher/lifecycle contracts are outside A1.2 and not approved by this review.
- Only this report and ignored review fixtures/logs were created in Sol's checkout. No integration, primary ledger edits, commits, pushes, dependency changes or branch changes were made.
