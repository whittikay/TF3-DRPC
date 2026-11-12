# Repository Guidelines

TF3-DRPC shows a Transport Fever 3 game in the player's Discord status. It has two parts: a
game mod that writes the game state to the game log, and a portable companion app for
Windows and macOS that reads the log and talks to Discord. The design is in
[docs/README.md](docs/README.md).

## Source of Truth

This is the canonical guide for contributors and coding agents. [CLAUDE.md](CLAUDE.md) only
links here; do not duplicate guidance there. [README.md](README.md) is for players (what the
project does, installation, use) and [CONTRIBUTING.md](CONTRIBUTING.md) is the short
introduction for new contributors; both link here for the details instead of repeating them.
When the code and this file disagree, inspect the code and correct this file. Update it whenever a change makes it stale: new commands,
framework or dependency changes, structure, workflows, recurring pitfalls.

## Linux fork: multi-agent working agreement

The Linux plan is approved. Read [the plan](docs/linux-implementation-plan.md) and
[the execution stages](docs/linux-stages.md) before implementation. Linux support is
in development, not released. Keep the existing Windows/macOS behaviour and mod protocol.
This section takes precedence over older MAUI-only and stacked-PR assumptions below for
Linux work; all other safety, style, privacy and Native AOT rules still apply.

### Roles and ownership

- **Astra:** coordinator/integrator; owns architecture, shared interfaces, toolchain,
  package/project/solution files, CI and the stage ledger. Initially implements Stage 0,
  then the shared application seam and Discord transport work.
- **GPT-Sol:** independent implementation and review; initially reviews Stage 0, later
  owns Linux Steam/path/process integration. Reviews Astra's changes; Astra reviews Sol's.
- **Qwen:** bounded tests, synthetic fixtures, edge-case investigations, documentation
  and small utilities against approved contracts. Initially prepares the acceptance matrix.
- Roles are starting assignments, not assumptions about model capability. Reassign after
  observed results. One agent owns the Avalonia UI at a time; no parallel UI rewrites.
- **Identity check:** before acting on a task, each session checks which model is active in
  pi (`env | grep '^PI_'` shows `PI_MODEL` and `PI_PROVIDER`) and maps it to its role above;
  the user may reassign roles at any time. If the active model does not match the assumed
  role, stop and confirm with the user before making task-owned edits.
- A task grants write access only to its listed paths. Ask Astra before expanding scope.
  Shared configuration and dependencies have one owner even across separate worktrees.

### Worktree isolation and integration

- Each agent uses its own branch and Git worktree. Astra uses the primary checkout;
  Sol and Qwen use `.worktrees/sol` and `.worktrees/qwen`. Do not nest more worktrees in
  a worker checkout. The directories are ignored; Git metadata is shared, working files
  and build output are not. Never switch another agent's branch or edit its checkout.
- Read another checkout only for an assigned review. Build and test your own checkout;
  explicitly identify the revision or uncommitted snapshot actually tested.
- Uncommitted changes do not travel with branches. Until commits are authorised, Astra
  supplies a named bootstrap snapshot and per-worktree checksum manifest. Preserve those
  files and report only changes beyond that snapshot. Never assume `HEAD` contains them.
- No commits, merges, rebases, pushes, PR submissions or releases without explicit user
  authorisation for that action. The upstream `gh stack` commands below are reference,
  not permission to execute them. Do not install extensions or change repository rulesets.
- Astra integrates only independently reviewed, in-scope changes after checking the
  current diff. Never copy an entire worker checkout over the integration tree. Do not
  reset, stash, clean or discard another agent's changes. Resolve shared-file conflicts
  by agreement rather than last-writer-wins.
- SDKs/caches stay project-local and ignored. Never alter global SDKs, workloads, desktop
  settings, Discord configuration or installed packages without specific approval.
  Build outputs and NuGet caches are separate per worktree; a verified SDK may be shared
  read-only. No concurrent workload installations into a shared SDK.

### Task handoffs and review loop

Each task must name: stage/task ID, base snapshot, objective, non-goals, owner/reviewer,
allowed files, agreed interfaces, acceptance checks, dependencies and stop conditions.
Return changed paths, commands actually run with outcomes, unresolved risks and anything
not verified. Never equate a proposed command with a completed test.

Work in small increments: implement → automated checks → independent review → targeted
fixes → recheck → integration. An author does not approve their own work. Review findings
need severity, file/line, a concrete failure scenario and a test or reproduction where
possible. Review the actual diff and contracts, not just the author's summary. Recheck
changed findings; avoid repeating an unchanged full review. After two failed correction
cycles, stop and reconsider the design with the coordinator.

Stage 0 is a go/no-go gate: do not start the full UI or shared extraction before the
SDK, Native AOT, resources and desktop spike have been evaluated. Use source-generated
JSON/logging, compiled XAML/bindings, no reflection or LINQ in production code, one monitor
loop and explicit lifecycle management. No warning suppression to make AOT appear viable.
The Linux host uses Avalonia; Windows/macOS retain MAUI. No Generic Host or second daemon.

### Validation and permissions

- Core and application tests must not discover a live Discord socket. Use test-owned
  synthetic fixtures; never publish activity during normal test/CI runs.
- Live Discord tests, game launches/mod activation and changes outside the active project
  require specific user approval. Never screenshot Discord or collect its credentials.
- READY means IPC connected, not visible activity. End-to-end presence and store-button
  checks require separately reported manual evidence.
- Gate changes with Debug/Release builds, tests, analysers and a Native AOT publish.
  For the current Stage 1 work, the user has excluded native Windows/macOS validation
  from scope: record it as unverified, not a local integration blocker. Preserve their
  source behaviour; do not infer platform compatibility from Linux checks.
  Windows/macOS need their own runners; headless Linux tests do not certify tray hosts,
  Wayland, accessibility, game behaviour or sandbox/client interoperability.
- Prioritise wrong-client routing, timeouts/cancellation, stale logs, process identity,
  tray-less recovery, pending settings on exit and published translations in reviews.
- Report blockers rather than changing SDK/framework pins, broadening filesystem access,
  installing bridges or weakening portability/security requirements without review.
- Keep public reports sanitised as required by Security & Configuration below. Raw build
  logs and bootstrap manifests containing machine paths stay in ignored local storage.

## Quick Reference

- Stage 1 shared application seam: `companion/Tf3Drpc.Application/` contains the extracted
  presence/preview service, settings persistence, the single monitor loop, settings/status
  messages and public service contracts. MAUI resolves shared types through `ApplicationServices.cs`
  aliases; translation remains in the host. Its path service supplies the shared
  `ISettingsPaths` contract using the same instance registered as `IPathService`. The MAUI
  monitor factory supplies its existing services through Core's source/sink interfaces.
  Both solutions include the shared project and its xunit v3 test
  project. From the checkout root run
  `./tools/linux-dotnet.sh test --project Tf3Drpc.Application.Tests/Tf3Drpc.Application.Tests.csproj`.
  The Linux feasibility host does not yet compose these services; its AOT publish alone
  does not validate the extracted application code. See `docs/agent-handoffs/astra-stage1.md`.
- Linux Stage 0 (from the checkout root): `./tools/linux-dotnet.sh build TF3-DRPC.Linux.slnx`
  and `./tools/linux-dotnet.sh test --project Tf3Drpc.Core.Tests/Tf3Drpc.Core.Tests.csproj`.
  Publish: `./tools/linux-dotnet.sh publish Tf3Drpc.Linux/Tf3Drpc.Linux.csproj -c Release -r linux-x64`.
  This is a feasibility host, not a working companion. See [the stage ledger](docs/linux-stages.md)
  for the local SDK, workload-resolver scope, per-worktree caches and published smoke modes.
  The feasibility host's tray menu offers Show window, Hide to tray and Exit. Hide checks
  the StatusNotifier host through the existing Tmds.DBus.Protocol dependency; while hidden,
  a bounded UI-lifecycle check restores the window if the host becomes unavailable. This
  is not the game monitor loop. Normal window close and the window's Exit button still quit.
- Link the mod into the game's staging area (Windows and macOS): `./tools/install-mod.ps1` (`-Remove`
  unlinks it)
- Companion app, run from `companion/` (where `global.json` and the solution are):
  `dotnet build TF3-DRPC.slnx`, `dotnet test TF3-DRPC.slnx`,
  `dotnet publish Tf3Drpc.App/Tf3Drpc.App.csproj -c Release -r win-x64` (Native AOT; output in
  `artifacts/publish/Tf3Drpc.App/release_win-x64/`, about 83 MB); on macOS `-r maccatalyst-arm64`
  (the `.app`, about 21 MB, in `artifacts/bin/Tf3Drpc.App/release_maccatalyst-arm64/`, a `.pkg` in
  `artifacts/publish/…`). Every trim warning is shown on its own (`TrimmerSingleWarn=false` in
  `Directory.Build.props`).
- Release zip of the platform it runs on (tests, Native AOT publish, zip into `artifacts/`):
  `./tools/build-release.ps1` (`-SkipTests` after a full test run). CI runs it too: `ci.yml`
  builds and tests every pull request and push to `main` on Windows and macOS that changes what
  the build reads (`companion/`, the log samples, the app icon, the release scripts, `LICENSE`;
  the full list is in `ci.yml`, and `workflow_dispatch` runs it by hand). Pull requests always
  start its jobs, because the `CI on main` ruleset requires the `Windows` and `macOS` checks; a
  first step skips the rest when no listed path changed, and lists them a second time (keep it in
  step with `push.paths`). A push to `main` also
  keeps both zips, and `release.yml` publishes the release when a `v<VersionPrefix>` tag is pushed, after
  checking that the tagged commit is on `main` and that CHANGELOG.md has a section for that
  version whose heading ends with the date (YYYY-MM-DD), which isn't empty and has no relative
  links (the release page and mod.io can't resolve them). Releases are immutable (Settings →
  General → Releases): the workflow builds the release as a draft, compares its files with the
  built zips, publishes it and verifies GitHub's attestation (`gh release verify`). After that
  its tag and files can't change and the tag name can't be used again, so a fix needs a new
  version; a tag whose run failed before publishing can be moved or its run re-run
- Live Discord test (sets a test presence for 5 s): set `TF3DRPC_DISCORD_LIVE=1`, then
  `dotnet test TF3-DRPC.slnx -- --filter-method '*LiveDiscord*'`
- Manual test with the game and Discord: [docs/testing.md](docs/testing.md); scripts for a run
  driven by script: [tools/test-run/README.md](tools/test-run/README.md)

Use the SDK and workload set pinned in `global.json`. Use `dotnet build` to validate and
`dotnet publish` for shippable output; only `dotnet publish` shows every trim and AOT
warning. A change must not add a warning.

On Windows, install the pinned workload set once per machine and again after `global.json`
moves it, or every project fails restore with "workload version … not found". Visual Studio
keeps its own older manifests, so its updates do not fix this: run
`dotnet workload update --version <workloadVersion from global.json>`.

- `dotnet format` writes LF; convert touched files back to CRLF afterwards. So can `sed -i`
  in Git Bash.
- Escapes written through a shell heredoc or some edit tools can turn into the real character:
  `'\uFEFF'` became an invisible byte order mark in C# source, `\n` a line break. After such
  edits, check the touched files for control characters, U+FEFF after the first byte and
  zero-width characters; PowerShell single-quoted strings keep escapes literal.
- A build while the app or another build runs can fail with `MAUIR0001` (a locked icon
  file); stop the app and build again.
- Native AOT compiles satellite assemblies (translations) into the exe, but ILC only takes
  those of NuGet packages; the app project adds those of project references
  (`AddReferenceSatellitesToIlc`). Check a new translation in the published exe, not only in
  `dotnet test`.
- Unused usings (IDE0005) don't fail the build and `dotnet format --verify-no-changes` misses
  them. To check, add a temporary `companion/.globalconfig` with `is_global = true` and
  `dotnet_diagnostic.IDE0005.severity = warning`, build Debug and Release, then delete it.
- After a change of `DebugType`, clean `artifacts/bin/Tf3Drpc.App` and
  `artifacts/obj/Tf3Drpc.App` once; an old `.pdb` there is otherwise copied into publish.

### Developing on macOS

- Install the workload set pinned in `global.json` once; without it every project fails with
  "workload version … not found". The SDK in `/usr/local/share/dotnet` needs `sudo`:
  `sudo dotnet workload update --version <workloadVersion from global.json>`.
- The app project picks its target by the host: `net11.0-windows10.0.19041.0` on Windows,
  `net11.0-maccatalyst` on macOS (install the `maui-maccatalyst` workload with `sudo` from
  `companion/`). So a Mac never builds the Windows target: a change there can only be checked on
  Windows, and the pull request says so.
- Windows-only parts of `Tf3Drpc.App.csproj` carry the condition
  `'$([MSBuild]::GetTargetPlatformIdentifier($(TargetFramework)))' == 'windows'`. The platform service
  types are named once in `PlatformServices.cs` (global using aliases); shared code uses those names.
- `ValidateXcodeVersion=false`: the RC2 Mac Catalyst SDK (also in the RC3 workload set) requires
  Xcode 27.0 and refuses 27.1 and 27.2, which build and run it. Remove it at .NET 11 GA.
- Line endings on a Mac: `.editorconfig` asks for CRLF, but git checks files out with LF there
  (macOS's `/etc/gitconfig` sets `core.autocrlf=input`), and every C# file then gets `RCS0044`. Set
  `git config core.autocrlf false` and `git config core.eol crlf` in the repository once, then write
  the files out again (`rm .git/index && git checkout HEAD -- .` with a clean tree). Swift sources
  stay LF (`.gitattributes`, `tools/test-run/.editorconfig`): `swift-format` misreads CRLF.
- After changing `Platforms/MacCatalyst/Info.plist`, delete the cached manifest
  (`artifacts/obj/Tf3Drpc.App/<config>_maccatalyst-arm64/*AppManifest*`); an incremental build
  keeps the old values (seen with `UIDeviceFamily`). Check the built `Contents/Info.plist`.
- Platform-specific XAML values with resources go through typed `OnPlatform<T>` resources in
  `App.xaml`; the `{OnPlatform …}` markup extension with `StaticResource` values isn't trim safe
  (IL2026). Mac-only look: `MacPickerHandler`, `MacCheckBoxHandler` and the `Mac…` styles.
- `EnableWindowsTargeting` in `Directory.Build.props` lets the Windows projects restore on
  macOS. It reaches only projects under `companion/`; a project elsewhere that targets
  Windows needs it itself.
- A test that relies on Windows behaviour (`FileShare` locks, named pipes) starts with
  `Assert.SkipUnless(OperatingSystem.IsWindows(), "…")`.
- C# Dev Kit opened at the repository root builds its own solution from every `.csproj` and
  caches it in VS Code's `workspaceStorage/<id>/ms-dotnettools.csdevkit/`. After removing a
  project, delete that cached `.sln` and reload the window, or Dev Kit keeps restoring it.
- The game scripts in [tools/test-run/](tools/test-run/) run on macOS with `pwsh` and the Xcode
  command line tools; the app that runs them (Terminal, VS Code) needs Accessibility and Screen
  Recording in System Settings → Privacy & Security. Their native part, `tf3mac.swift`, is
  formatted with `swift-format` by `tools/test-run/.swift-format` (4 spaces, 140 characters);
  without the standalone tool, the toolchain's `swift format --configuration
  tools/test-run/.swift-format -i tools/test-run/tf3mac.swift` does the same.

## Repository Overview

| Path | Purpose |
|---|---|
| [mod/zagrthos_drpc/](mod/zagrthos_drpc/) | The game mod (Lua); `_metadata/0.png` … are its Mod Hub cover and gallery images, 1920 × 1080. Design: [docs/mod.md](docs/mod.md) |
| [tools/](tools/) | Development scripts; [tools/test-run/](tools/test-run/) drives the game and the app in a test run |
| [docs/](docs/) | Design documents and real game log samples |
| [reviews/](reviews/) | Dated code reviews, reproduction scripts and recorded evidence |
| [assets/discord/](assets/discord/) | Images uploaded to the Discord application; `cover.png` is also the source of the app icon |
| [companion/](companion/) | The companion app. Design: [docs/companion.md](docs/companion.md) |
| [.claude/skills/tf3-modding/](.claude/skills/tf3-modding/) | Offline TF3 modding reference for coding agents |
| [README.md](README.md), [CONTRIBUTING.md](CONTRIBUTING.md) | The overview for players and the short guide for new contributors |
| [CHANGELOG.md](CHANGELOG.md) | What each release brings; `release.yml` takes the GitHub release text from it, and the Mod Hub upload uses the same text |
| [.github/](.github/) | Issue forms (bug report, feature request, translation), the issue chooser, the pull request template, and the workflows: `ci.yml` (build and test on Windows and macOS), `release.yml` (release for a `v<VersionPrefix>` tag on `main`) and one `codeql-<language>.yml` per CodeQL language (Actions, C# on Windows and macOS, Swift; Lua and PowerShell have no CodeQL support), configured by `codeql/codeql-config.yml`, which leaves out `reviews/`; actions are pinned to commit hashes |

Key configuration: [.editorconfig](.editorconfig) (a shared file; don't edit it locally),
[.gitattributes](.gitattributes), [.luarc.json](.luarc.json) (Lua language server).

## Coding Style & Naming Conventions

### C#

Follow [.editorconfig](.editorconfig) and the analysers (Meziantou, Roslynator, Sonar), which
run in every build with code-style enforcement, strict compiler features and NuGet audit.
Fix a finding, or suppress it narrowly with a real justification.

- Four spaces, CRLF, UTF-8, final newline, no trailing whitespace.
- File-scoped namespaces; using directives outside the namespace, `System` first.
- Modern C#: primary constructors, pattern matching, collection expressions, `extension`
  blocks. `LangVersion` is `latest`, not `preview`.
- Patterns for every comparison with a constant (literal, `const`, enum member, `null`),
  also combined and as property patterns: `is 0`, `is not null`, `is >= 1 and <= 9`,
  `is A or B`, `status is { Version: Const }`. Comparisons between two variables and
  against `static readonly` values (such as a `TimeSpan`) can't be patterns.
- A single-line `if` without `else` and with one single-line statement of any kind (`return;`,
  a call, an assignment) has no braces; `if`/`else`, multi-line statements and loops keep them.
- In an expression-bodied member the `=>` starts the line after the member and is followed
  directly by the first part of the expression, never alone on its line. Continuation lines
  are indented one level deeper than the arrow.
- No `var`; explicit types.
- Parentheses of a multi-line parameter or argument list on their own lines, aligned with
  the member. A primary constructor can't be formatted like that (Roslynator RCS0039), so a
  class with a multi-line constructor parameter list gets an explicit constructor, with a
  narrow `#pragma warning disable IDE0290` around it.
- Types are `internal sealed` unless they must be public: in `Tf3Drpc.Core` only what the
  app uses, in the app only what generated code needs (the WinUI application class).
  Interfaces are internal too, except the six in `Tf3Drpc.Core/Monitor/`, which the app's
  services inherit (`GameMonitor` reaches the app through them). Put
  `#pragma warning disable CA1812 // Avoid uninstantiated internal classes` and the matching
  `restore` around the declaration of a class that only DI or a framework creates.
- Private fields form one block without blank lines. A service assigns its primary-constructor
  parameters to `private readonly` fields and uses the fields.
- Group members of ViewModels and of classes of more than about 150 lines in `#region` /
  `#endregion` blocks, with a blank line after and before.
- Static lambdas wherever they don't capture.
- Helper statics go into `Utilities/<Name>Utils.cs`.
- Bind a repeated expression or a constructed object to a named local instead of nesting it
  in a call.
- `_camelCase` for private and internal fields, `PascalCase` for types, members, static
  fields and constants. Shared limits and tuning values go into an `<Area>Constants` class
  in `Constants/`.
- Nullable annotations stay correct; nullable warnings are errors.
- Comments explain intent, constraints or workarounds, not what the code shows. XML doc
  comments are a one-line `<summary>`, `<remarks>` only for a real gotcha. No plan or task
  rationale in source; that goes into the commit message.
- `<see cref="..."/>` for symbol references in XML docs.

### Architecture patterns

- **DI and logging:** the existing MAUI host uses `MauiAppBuilder`; the Linux Avalonia
  host uses explicit composition. No separate Generic Host and no hosted services.
  Background work runs in the one loop of `IMonitorService`.
- **Services** behind an **interface**: interface in `Services/Interfaces/`, implementation
  in `Services/`, injected through primary constructors. The services do the work; the UI
  only presents.
- **Registration** is `Extensions/ServiceCollectionExtensions.cs`, an `internal static` class
  with a C# 14 `extension(IServiceCollection services)` block: `AddAppServices()` and
  `AddAppViewModels()`. The comments name the dependency tier ("These services do not depend
  on anything", "These services depend on the above services"), not the service.
- **Messages** are `internal sealed record class ...Message(...)` in `Messages/`, with a
  one-line `<summary>` and `<param>` docs, sent through `IMessenger`.
- A multi-value result on a service surface is an `internal readonly struct NameStruct :
  IEquatable<NameStruct>` in `Structs/` (not a tuple), with `[StructLayout(LayoutKind.Auto)]`,
  `init` properties, `Equals([NotNullWhen(true)] object? obj)` and `==`/`!=` operators with
  `in` parameters. Build it with an object initialiser.
- Services don't build display strings; they return identifiers and values, and the
  presentation layer turns them into text.
- **XAML first:** UI that XAML can express (window and title bar properties, visibility rules,
  text composition, styles) goes into XAML, not into C# or view-model properties. C# stays for
  DI, services and platform APIs XAML has no access to.

### Native AOT rules

Non-negotiable in the companion app and `Tf3Drpc.Core` (the tests are exempt):

- **No LINQ.** Use loops and pre-sized collections (`new List<T>(capacity)`).
- **No reflection.** App name and version come from `ThisAssembly.Info`; enum texts and keys
  from switch-expression `extension(EnumType value)` members in `Extensions/EnumExtensions.cs`,
  never attributes read by reflection.
- **Source generation wherever it exists:** JSON only through the one source-generated
  `JsonSourceGenContext` per project in `Serialization/` (reflection-based JSON is switched
  off; a differing option, such as indentation, comes from a second instance with its own
  `JsonSerializerOptions`); logging only through `[LoggerMessage]` partial methods in
  `Logging/LogMessages.cs` (extension methods on `ILogger<TheClass>`, grouped in regions by
  level, then by logger class), never `LogInformation` and the like; regex only as
  `[GeneratedRegex]` partial properties with a match timeout in `Utilities/RegexUtils.cs`,
  used through its helper methods; native calls through `[LibraryImport]`; ViewModels through
  CommunityToolkit.Mvvm; XAML through MAUI's source generator or Avalonia's compiled XAML
  with compiled bindings in the Linux host.
- Format invariant data with `string.Create(CultureInfo.InvariantCulture, $"...")`.
- Detect the build with `#if DEBUG`, not runtime environment checks.
- Every trim or AOT warning is fixed, never suppressed. Known exception, not ours and not
  suppressed: one `IL2070` in `Microsoft.Maui.Controls` (`MauiItemsView.DragDrop.cs`, the
  Windows CollectionView drag and drop, which the app doesn't use) in .NET 11 RC2; check
  again at GA.
- The publish also prints ILC messages "Method '…' will always throw because: Failed to load
  …". They are not warnings and need no fix: ILC compiled code that can never run into a
  throw. On Windows there are 20, for the web view code of MAUI (`WebViewHandler`,
  `HybridWebViewHandler`, `MauiWebView`, `WebViewExtensions`) and WinUI (`WebView2`), because
  `Microsoft.Web.WebView2` is left out of the publish (with the other unused Windows App SDK
  parts, about 40 MB; see [docs/companion.md](docs/companion.md#projects)); the app has no web view, and its links
  open the system browser through `Launcher`. On macOS there is one, for
  `UIKit.NSLayoutAnchor`1` in the Mac Catalyst SDK's type map. Don't add the WebView2 package
  back to silence them.

### Portable app

The companion app writes only into `DATA/` next to its own files: no registry, no user
profile folders, no MAUI `Preferences`, `SecureStorage` or `FileSystem.AppDataDirectory`,
no start with the system. Details in [docs/companion.md](docs/companion.md#portable).

### Lua (the mod)

- Four spaces (from [.editorconfig](.editorconfig)), not the tabs of the base game.
- Every variable `local`; `function data()` is the only global. The game logs every new
  global as an error.
- Call only functions and fields that exist in the TF3 definition files; look them up with
  the `tf3-modding` skill. Never assume a Transport Fever 2 API.
- Read every game value inside its own `pcall`, and a shared source once, so a changed API
  after a game update drops only the values that depend on it.
- The JSON encoder leaves out NaN and infinity: they have no JSON form and would make the
  companion reject the whole line.
- No `%d` for numbers (the game's Windows Lua takes only 32-bit values) and no `%c` in
  patterns (locale-dependent; it can match UTF-8 bytes). The encoder's output must not
  depend on the C locale.
- Log with `log.message` / `log.warning`, prefixed `[drpc]`; the status line uses
  `[drpc-status]` and its format is fixed in [docs/status-line.md](docs/status-line.md)
  (raise `v` when a key changes meaning).
- No `os.clock()` for timing (CPU time on macOS); use `api.util.getLocalTime()`.
- The Lua language server ([.luarc.json](.luarc.json)) must report no problems.

### Written content

British English in documentation, commit messages and prose (colour, behaviour, licence,
dialogue), not in code identifiers, package IDs, framework members or JSON keys. Do not
rename an identifier only for the spelling.

## Testing

- `Tf3Drpc.Core.Tests` uses xunit v3 on Microsoft.Testing.Platform. It sees the internal types
  of `Tf3Drpc.Core` through `InternalsVisibleTo`; the test classes stay public as xunit needs.
  Parser tests read the
  real log lines in [docs/log-samples/](docs/log-samples/); `.gitattributes` keeps those
  files byte for byte, change them only by copying real log output.
- The mod has no automated tests: test it in the game (see [docs/testing.md](docs/testing.md))
  and say in the commit or pull request what was not verified.
- A change to the companion app is validated with a build in Debug and Release, the tests,
  the analyser output, a publish and, for behaviour, a run with the game and the Discord
  client ([docs/testing.md](docs/testing.md)).
- Domain logic belongs in `Tf3Drpc.Core`; UI-neutral application orchestration is being
  extracted into `Tf3Drpc.Application` with its own tests. Neither test project references
  MAUI. The monitor's tick lives in Core's `GameMonitor` and is tested there; the timer
  loop and settings/status messages live in Application. MAUI supplies the shared
  messenger and platform adapters; the shared loop uses injected TimeProvider timers.

## Security & Configuration

- Never commit tokens or credentials. The Discord application ID is not secret.
- Treat paths, log content and file input as untrusted.
- Keep build output, `DATA/` folders, logs and machine-specific settings out of commits.
- Everything in the repository is public. Recorded evidence, logs, screenshots and docs use
  placeholders for local paths (`<repo>`, `<Steam>`, `<steam-account>`) and contain no user or
  computer names, account IDs, hardware, display size or scaling, time zones, installed
  software or screenshots of the Discord window. Record UI coordinates in device-independent
  pixels and strip metadata from images.

## Commit & Pull Request Guidelines

Short, imperative commit subjects, for example `Fix the log truncation check`. Keep each
commit focused. Dependency commits use bracketed scopes (`[NuGet]`, `[.NET SDK]`). A pull
request explains the behaviour change, lists the validation performed and names anything
not verified (for example macOS). No AI attribution: no `Co-Authored-By` trailer and no
"Generated with" line.

- A pull request body follows [.github/PULL_REQUEST_TEMPLATE.md](.github/PULL_REQUEST_TEMPLATE.md)
  (What changes, Validation, Not verified, Checklist).
- A change players notice gets a line in [CHANGELOG.md](CHANGELOG.md), in the section of the
  next release. Each paragraph and list item of a section stays on one line, because the GitHub
  release and the Mod Hub show every line break of the section. A section that isn't released yet says "not released yet" in place of the
  date; `release.yml` refuses a tag until the heading ends with the date.
- Issues and pull requests get one kind label (`bug` or `enhancement`) and the `area-` labels of
  the parts they touch (`area-mod`, `area-app`, `area-windows`, `area-macos`, `area-discord`,
  `area-translations`, `area-accessibility`, `area-docs`, `area-tools`); see
  [CONTRIBUTING.md](CONTRIBUTING.md#labels). Duplicates and issues that won't be worked on are
  closed with GitHub's close reasons, not labels.

Suggested labels by kind of change; combine the rows a change spans. Every issue and pull
request gets a kind label, `bug` for a fix and `enhancement` for something new or better:

| Change | Labels |
|---|---|
| The companion app on both platforms: settings, presets, templates, log reading, the monitor | kind, `area-app` |
| A refactor or new tests of the app without a behaviour change | `enhancement`, `area-app` |
| Only on Windows (tray, settings window, publish) or only on macOS (menu bar, Gatekeeper) | kind, `area-windows` or `area-macos`, plus `area-app` when shared code changes too |
| The Discord connection or what the status shows | kind, `area-discord` |
| The mod or the status line it writes | kind, `area-mod`, plus `area-app` when the app's parser changes with it |
| The app's own texts in the 13 languages | kind, `area-translations` |
| Screen readers, keyboard use, contrast | kind, `area-accessibility` |
| A dependency or SDK update (`[NuGet]`, `[.NET SDK]`) | `enhancement`, `area-app` |
| Documents only (README, CONTRIBUTING, AGENTS.md, `docs/`) | `bug` for a wrong or outdated statement, otherwise `enhancement`; `area-docs` |
| Test-run scripts in `tools/` and recorded test runs | kind, `area-tools`, plus `area-docs` when the docs record the run |

`good first issue` and `help wanted` go on issues only.

Work is stacked with [`gh stack`](https://github.com/github/gh-stack): one branch and one
pull request per topic, each based on the one below, trunk `main`. A pull request stacked
on a branch that GitHub doesn't know as part of the stack merges into that branch, not
`main`: add it to the stack with `gh stack add`, or retarget it to `main` before merging.

- Start a new stack from `main` with `gh stack init <branch>`.
- Commit a change on the layer that owns it (`gh stack checkout <branch>`), then
  `gh stack rebase --upstack` and `gh stack push --remote origin`.
- A new topic is a new layer on top: `gh stack add <branch>`, later
  `gh stack submit --auto --open --remote origin` for its pull request.
- Always run `gh stack view --json`, never the bare `gh stack view` (it opens a TUI).
- Merge with `gh stack merge <top PR> --yes`; it merges that PR and everything below it.
