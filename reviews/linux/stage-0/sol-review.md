# S0 — Independent Stage 0 review

## Verdict

**Blocked for Stage 0 closure / Stage 1 advancement.** The workload-free build and
Avalonia Native AOT startup are independently demonstrated on this host. No confirmed
new production-code defect was found in the bootstrap. The remaining blockers are
acceptance evidence, not a recommendation to replace Avalonia or change SDK pins.
Do not interpret this review as release qualification or approval to begin Stage 1.

Task / owner / implementer: S0 / GPT-Sol / Astra (A0).
Base: `00592fd2b5cc6a36fa7a4c158ce5fbd830db7930` plus uncommitted
`stage0-bootstrap-01`, tested in Sol's assigned checkout on
`linux/sol-stage0-review`. All 20 manifest hashes and executable flags matched before
experiments; all seeded file hashes still matched at the end.

Changed beyond bootstrap: only `reviews/linux/stage-0/sol-review.md`, plus ignored
local evidence and experiments under `.agent-state/s0-review/`.

## Gate findings

### S0-G1 — Medium: baseline-distro compatibility is not demonstrated

References: `docs/linux-implementation-plan.md:215`,
`docs/linux-stages.md:141`, `companion/Tf3Drpc.Linux/Tf3Drpc.Linux.csproj:10`.

The Stage 0 exit criterion explicitly requires the published spike on a baseline distro.
Independent ELF inspection of this publish found a maximum required symbol version of
`GLIBC_2.38` in `Tf3Drpc.Linux`. An older-glibc installation cannot launch this executable,
even though it is self-contained and has no .NET runtime prerequisite. No minimum distro
version has been selected or tested here. This is not proof that every Ubuntu or Fedora
version fails; suitability depends on the baseline actually chosen.

Reproduction: publish with the documented command, then run `readelf --version-info`
on the executable and inspect its `GLIBC_*` requirements. No other distro was available
for an actual execution test.

Required resolution: Astra should select and document a reviewed baseline, build against
it if needed, and execute the native resource/startup checks on that distro. Preserve the
SDK pin; do not weaken AOT or advertise untested ABI compatibility.

The archive also needs both bundled native libraries, not merely the executable:
`libSkiaSharp.so` and `libHarfBuzzSharp.so`. Their maximum required GLIBC symbol versions
were 2.27 and 2.14 respectively. `ldd` resolved all three files on this host; Skia's
resolved dependency tree includes fontconfig, FreeType and their dependencies. The
executable's direct libc/libm/loader inventory alone is insufficient for GUI deployment.
Dynamically loaded X11, graphics, ICU and D-Bus facilities still need a baseline inventory.

### S0-G2 — Medium: desktop integration evidence remains incomplete

References: `docs/linux-stages.md:141`, `companion/Tf3Drpc.Linux/App.axaml.cs:24`,
`companion/Tf3Drpc.Linux/App.axaml.cs:43`.

Both published smoke modes exited 0, as did a tray-enabled run with the child process's
session-bus address pointing at a nonexistent test-owned socket. These are useful startup
checks, but the three-second Opened timer proves neither tray visibility nor menu actions,
window accessibility, actual tray-less GNOME behaviour, or backend equivalence. A blank,
clipped or inaccessible window could also satisfy this smoke exit condition.

Required resolution: record real-desktop evidence for tray Show window/Exit, window close
and Exit button, keyboard/screen-reader access, and a genuine no-tray-host environment.
Evaluate native Wayland separately; record the backend used for each run. A subprocess-only
unreachable bus address is not a complete no-D-Bus desktop simulation. No desktop settings
should be changed to manufacture these results.

These are already acknowledged open gates, not newly discovered regressions. Native
Wayland support, single-instance activation and tray-loss recovery are not implemented by
this spike; the latter two remain later-stage work rather than S0 code defects.

## Independent commands and observations

All builds/tests/publishes below ran in Sol's checkout, not another agent's output tree.
Raw logs are ignored under `.agent-state/s0-review/`; public evidence is sanitised here.

| Command/check | Observed result |
|---|---|
| Bootstrap SHA-256 and executable flags | 20/20 matched |
| `./tools/linux-dotnet.sh --version` | Exit 0; `11.0.100-rc.3.26522.148` |
| `build TF3-DRPC.Linux.slnx -c Debug` through wrapper | Exit 0; 0 warnings / 0 errors |
| Core `test --project … -c Debug --no-build` through wrapper | Exit 0; 650 passed, 3 skipped, 0 failed |
| `build TF3-DRPC.Linux.slnx -c Release` through wrapper | Exit 0; 0 warnings / 0 errors |
| Core `test --project … -c Release --no-build` through wrapper | Exit 0; 650 passed, 3 skipped, 0 failed |
| `publish Tf3Drpc.Linux/Tf3Drpc.Linux.csproj -c Release -r linux-x64` through wrapper | Exit 0; native code generated; no reported build/trim/AOT warnings |
| Published `--check-resources` | Exit 0; all 13 languages |
| Published `--invalid` | Exit 2 |
| Published `--smoke-test --no-tray` with 20-second timeout | Exit 0 |
| Published `--smoke-test` with 20-second timeout | Exit 0 |
| Published `--smoke-test` with unreachable session-bus address, 20-second timeout | Exit 0 |
| `ldd` and `readelf --version-info` on executable and both bundled native libraries | No unresolved direct/transitive `ldd` dependencies on this host; symbol versions above |
| `bash -n tools/linux-dotnet.sh` | Exit 0 |
| Mock SDK probe in ignored path containing spaces | Passed quoted SDK/repository paths, exact arguments with spaces, child working directory, local cache paths, resolver flag and live-test flag removal |
| Protected-path comparison against HEAD | No changes to Core, tests, MAUI app/solution, global.json, Directory.Build.props/targets or root .editorconfig |
| `git diff --check` | Exit 0 (existing Git line-ending notices only) |

Skipped in both test configurations: real German game translation file
(`MoReaderTests.RealGermanFile_ContainsKnownPairs`), Windows file locking
(`SettingsStoreTests.Save_OverALockedFileFailsWithoutThrowing`), and opt-in live Discord
(`DiscordPresenceClientTests.LiveDiscord_ShowsAndClearsAPresenceAsync`). Skips are not passes.
Normal test execution made no live Discord connection; the wrapper removes the opt-in flag.
The existing real translation test is not evidence of a game session.

### Controlled publish experiments (source unchanged)

1. Published with `-p:UseOptimizedPublish=true` into an ignored output directory:
   publish exited 0; native `--smoke-test --no-tray` exited 134 with
   `Avalonia.Markup.Xaml.XamlLoadException: No precompiled XAML found for Tf3Drpc.Linux.App`.
   Core dumps were disabled for this subprocess. This independently reproduces the
   documented RC3 regression and supports keeping `UseOptimizedPublish=false`
   (`Tf3Drpc.Linux.csproj:14`), not treating a clean publish as sufficient evidence.
2. Published with `-p:SatelliteResourceLanguages=en` and optimised publish disabled
   into another ignored directory: publish and resource check both exited 0, still
   reporting all 13 languages. **This is not a successful missing-resource negative
   control.** The property alone did not remove the referenced Core satellites; do not
   use it as a fixture that is assumed to exercise ResourceCheck's failure path.
3. Re-published the unchanged default configuration after both experiments: publish,
   13-language resource check and native no-tray startup all exited 0 again.

## Implementation assessment

- The Linux solution excludes MAUI. Avalonia package references occur only in the new
  Linux project; their central version entries do not add those references to existing
  projects. The C# CRLF attribute is a deliberate cross-platform configuration change
  consistent with the existing analyser policy; Windows/macOS runners remain untested.
- The wrapper quotes SDK and repository paths and forwards arguments with `"$@"`.
  SDK selection is local, primary-worktree fallback, then PATH; an explicit override
  takes precedence. Workload-resolver disabling and live-test opt-out are child-scoped.
  The mock SDK probe is not proof of a full real build from a spaced repository path.
  Missing/wrong-SDK environments were not executed. The wrapper forwards arbitrary
  dotnet verbs: documentation, not enforcement, prevents MAUI/workload misuse; it is
  not a sandbox or a guarantee of a physically read-only SDK.
- XAML bindings are compiled by default with an explicit `x:DataType`; the native startup
  checks exercise compiled application/window loading and resource/asset use. They do
  not assert that the bound sample was visually rendered or that the tray icon appeared.
- Main-window close shuts down the spike. Exit disposes the smoke timer and attached
  tray icons; there is no hide-on-close path or background monitor. The three-second
  timer is scheduled after Opened, so callers must retain an external timeout when
  testing startup failure or a window that never opens.
- The Core satellite target mirrors the MAUI host and is sufficient for this native
  13-language check. I did not remove that target to independently prove its necessity.
  The resource check exercises one key per language, not translation completeness.
- No reflection/LINQ, real Discord transport, Steam discovery, settings writes, shared
  extraction, Generic Host or second monitor loop was added. Documentation correctly
  labels the host as feasibility-only and unreleased.

## Remaining work / stop point

Astra should resolve S0-G1/G2 with independently reviewed evidence and review/integrate
Qwen's acceptance matrix before seeking a Stage 0 go decision. This report reviews the
named bootstrap, not an assertion that later worker documents have been integrated.
Windows/macOS builds/publishes, baseline distro execution, native Wayland, actual tray/menu
interaction, screen-reader behaviour and end-to-end game/Discord presence remain unverified.
No source fixes were made or authorised; no Stage 1 implementation started. No commits,
pushes, package installation, shared SDK modification or desktop/client setting changes.
