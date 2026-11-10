# Discord Rich Presence for Transport Fever 3

Status: the mod, the companion app and the polish of step 3 are done on Windows and macOS. Both
were reviewed three times (last on 2026-10-17) and tested end to end with the game and Discord:
the whole [testing.md](testing.md) on macOS on 2026-10-20 and on Windows on 2026-10-21, and
what was added since on both by 2026-10-24. Every item of [platform-parity.md](platform-parity.md)
is done or checked. The first release, `0.1.0` (step 3.3), is out: the app on GitHub,
unsigned and on .NET 11 RC2, and the mod on mod.io, both public.
Date: 2026-10-24

## How it works

- **The mod** reads the game state (date, mode, company rank, money, network statistics)
  and writes it as one JSON line to the game log every ~10 s.
- **The companion app**, a portable .NET 11 MAUI app for Windows and macOS published with
  Native AOT, sits in the tray or menu bar, follows the game log and shows the result in
  Discord. It runs from the extracted zip and writes only into a `DATA` folder next to it.

```
┌──────────────────────── Transport Fever 3 ────────────────────────┐
│  zagrthos_drpc mod (Lua, official API only)                       │
│  game script guiUpdate ── every ~10 s ──► "[drpc-status] {json}"   │
│  game itself ──► main menu, loading, mission lines                │
└───────────────────────────────────────────┬───────────────────────┘
                                            │  game log (stdout.txt in the user data)
                                            ▼
┌──────── Companion app (.NET MAUI, Native AOT; Windows + macOS) ───┐
│  follows the game log + watches the game process                  │
│  ──► Discord IPC (pipe / Unix socket) ──► Discord desktop client  │
└───────────────────────────────────────────────────────────────────┘
```

The mod goes to mod.io through the in-game mod hub; the companion app is released
separately (for example on GitHub). It can't be one part: the scripting API can't reach
another process, mods may contain no executables, and the game's Lua 5.2.2 exports no C
API for native extensions.

## Documents

| Document | Content |
|---|---|
| [mod.md](mod.md) | the mod |
| [status-line.md](status-line.md) | the `[drpc-status]` line, the interface between mod and companion |
| [game-log.md](game-log.md) | finding and reading the game log, the lines that matter, samples |
| [companion.md](companion.md) | the companion app: platform, portability, projects, architecture, Discord connection, states, settings |
| [presence.md](presence.md) | what Discord shows, texts and languages |
| [testing.md](testing.md) | how to test with the game and Discord, where to look, known traps |
| [platform-parity.md](platform-parity.md) | what differs between Windows and macOS, and what is decided for each difference |
| [linux-implementation-plan.md](linux-implementation-plan.md) | approved Linux companion implementation plan; support is in development |
| [linux-stages.md](linux-stages.md) | execution stages, agent worktrees/handoffs, Linux spike commands and observed validation |
| [log-samples/](log-samples/) | real game log lines for the parser tests |

What the project does for players is in the [README](../README.md); how to contribute is in
[CONTRIBUTING.md](../CONTRIBUTING.md). Coding conventions, commands and rules for contributors
are in [AGENTS.md](../AGENTS.md).

## Decisions

Each decision has its reason, so it isn't undone by accident.

| Decision | Why |
|---|---|
| Mod plus separate companion app | The scripting API can't reach another process, mods may contain no executables, and the game's Lua exports no C API |
| The mod writes to the game log | No API function writes a file of our own (`app.saveUserdata` accepts only three fixed folders); see [mod.md](mod.md) |
| App name `TF3-DRPC`; Discord application ID `1559680023458484244` with the `cover` image | |
| Every published image is our own: in-game shots of our savegame | No third-party artwork whose terms are unclear; the cover, the app and tray icons and the Mod Hub images come from the game's photo mode |
| The Mod Hub cover shows the mod's name and a status card | The wiki's [guidelines](https://wiki.transportfever3.com/doku.php?id=modding:general:guidelines) ask for a cover without text, but a script mod has nothing to show in the game, and the name says what it is |
| The mod is for PC only | The companion app runs only on Windows and macOS, so on PS5 and Xbox the mod does nothing. Neither the game nor the mod files can exclude consoles: an upload that passes the console checks is approved for PS5 and Xbox automatically. mod.io offers no way to take it off consoles either, so the description says "PC only" and explains that the mod does nothing there. The game leaves consoles out only after a critical console error in its validation (`mod_manager_react_util.tl`), which would mean breaking a console rule on purpose |
| Design A as default, the timer, 13 languages with a language choice | Design A needs no setup and uses no private placeholder; the other presets (step 3.1) and the custom templates are the player's choice |
| Portable app writing only to `DATA/`; no installer, no start with the system | Players unzip it anywhere; nothing is left behind |
| No full write trace (Process Monitor, `fs_usage`) | The bounded check is enough: it found nothing outside `DATA/` on Windows; on macOS the only files seen outside are the two the system writes (see [companion.md](companion.md#portable)) |
| macOS 26 minimum, never in the Mac App Store | The App Sandbox would block the Steam folder and Discord's socket |
| .NET 11 MAUI with Native AOT on both platforms | One app for Windows and macOS; AOT for start-up and size |
| Our own source-generated Discord IPC client | The known libraries depend on Newtonsoft.Json and aren't AOT-compatible |
| Tray icon through WinUIEx's `TrayIcon` (replaced `H.NotifyIcon.WinUI`) | Same behaviour, no AOT warnings, actively maintained, used by Microsoft apps |
| A theme setting for the settings window only; "Same as the system" by default | The window is the app's only own surface; the tray and menu bar menus are drawn by the system and follow its setting, and the default changes nothing for players who never open the picker |
| Preset tooltips show the preset rendered with the values the app already has | No new reads: the same status as the preview, built when the tooltip opens; on macOS the pop-up button's menu is built again when it opens, and each entry's discoverability title is its tooltip |
| The store button is off by default | A button in the player's status is advertising that the player has to choose; Discord doesn't show it to the player, so it can't be checked from their own client |
| The settings window opens centred on the primary display | It used to open on the display nearest to its off-screen parking spot, which wasn't always the primary one |
| Paused means `speedup` 0, or no status line for more than 20 s | The pause button leaves `guiUpdate` running and the status says `speedup` 0; the game menu (Escape) stops `guiUpdate`, so no line arrives |
| `Quit requested` doesn't end the session; only `Normal shutdown. Bye!`, `Ungraceful exit` or the process exit do | The game can stay open after `Quit requested` (seen 2026-10-17), which left the presence cleared while the game ran |
| On macOS the settings window comes to the Space the player is on, also over the full-screen game | The game runs in a full-screen Space of its own, and macOS doesn't switch Spaces for a menu bar app; over the game the window stays in front until it is closed |
| No mode next to the rank in a Tycoon game | The Tycoon mod fixes the rank at Tycoon; comparing the texts wouldn't help, since some languages word the rank and the mode differently |
| The map editor is never paused | Time doesn't run there and the mod always writes `speedup` 0 |
| No `autoActivate` in `mod.json` | Tested 2026-10-17: it doesn't turn the mod on, neither in a new game nor in an old save |
| Log lines from before the game process started are ignored | Until the game truncates its log, the file still holds the previous session |
| Translations compiled into the exe by an extra build target | Native AOT can't load satellite assemblies, and ILC only takes those of NuGet packages |
| Publish size measures (305 MB down to 83 MB) | See [companion.md](companion.md#projects); unused Windows App SDK parts and symbols are left out |
| One code style and build file layout | Rules in [AGENTS.md](../AGENTS.md) |
| One JSON source-generation context per project; no `Testing` configuration | Decided with the code style |
| Packages stay on the versions MAUI RC3 resolves | Raising the excluded Windows App SDK parts alone would mix runtime versions; all move together at .NET 11 GA |
| CommunityToolkit.Maui not used | No `net11.0` build yet, and the settings window needed nothing from it through step 3.2 |
| File logs through NReco.Logging.File | |
| Work in a `gh stack` of pull requests | See [AGENTS.md](../AGENTS.md#commit--pull-request-guidelines) |
| CI on GitHub Actions: `ci.yml` builds and tests every pull request and every push to `main` that changes what the build reads (`companion/`, the log samples, the app icon, the release scripts, `LICENSE`) on Windows and macOS, `release.yml` publishes an immutable release for a `v<VersionPrefix>` tag on `main` (no pre-release tags) | Both call the same `tools/build-release.ps1` a developer runs by hand. macOS runs on the `xcode-27` preview image, the only one with the Xcode 27 the RC2 Mac Catalyst SDK needs. Jobs that don't build use the `ubuntu-slim` runner (it has PowerShell and the GitHub CLI). Actions are pinned to commit hashes. Releases are immutable: a published release's tag and files can't change and the tag name can't be reused, so the workflow builds a draft, compares its files, publishes it and verifies GitHub's attestation, all with the `gh` CLI (GitHub's own release actions are archived, and third-party ones would get write access to the releases). A run takes about 6 minutes on Windows and 3 on macOS; a pull request that changes only the docs, the mod or the test-run scripts skips the build after the first step (the `CI on main` ruleset requires the `Windows` and `macOS` checks, so their jobs must report; while the repository was private, each full run cost about 42 Actions minutes), such a push to `main` doesn't start one, and `workflow_dispatch` runs it by hand. The mod stays a manual upload: only the game's Mod Hub validates it and prepares it for consoles |
| The first release is unsigned on Windows and macOS; the README walks players through the SmartScreen warning and Gatekeeper's "Open Anyway" | A code-signing certificate and the Apple Developer account cost money every year before anyone uses the app; signing follows later (see [To do later](#to-do-later)) |
| The first release ships on .NET 11 RC2, before GA | RC2 has a go-live licence and every test and game run used it; the GA update follows as its own release |
| The first release is app version `0.1.0` and mod `revision` 1 | Below 1.0 while it runs on a release candidate and the macOS app is experimental |
| The game build shows in the settings window only, untranslated, as "Transport Fever 3 · Build <number>" | It helps with bug reports and costs no new read: the app already follows the log, which names the build at every start. The game itself shows "Build <number>" untranslated in every language. Discord doesn't get it, because it means nothing to others |
| A README for players, a short CONTRIBUTING.md, AGENTS.md stays the detailed guide | Players and contributors need different things; CONTRIBUTING links to AGENTS.md instead of copying its rules, so the two can't drift apart. `CONTRIBUTING.md` is the name GitHub links from issues and pull requests |
| Issue forms with required fields, no blank issues | A bug report needs the system, the app and game versions and the logs; the chooser sends questions to the README's installation and troubleshooting sections |
| Labels: one kind (`bug`, `enhancement`) plus `area-` labels; no `duplicate`, `invalid`, `wontfix` or `question` | The areas match the project's parts; GitHub's close reasons ("duplicate", "not planned") replace the removed labels |
| Nothing in the repository identifies a person or a machine | It is public: evidence, logs and screenshots use placeholders for paths and leave out account IDs, hardware, display details, time zones and image metadata (see [AGENTS.md](../AGENTS.md#security--configuration)) |

## To do later

- **.NET 11 GA (November 2026):**
  - Update `global.json` (SDK and workload set), `Microsoft.Extensions.Logging.Debug`, MAUI and the five excluded packages in `Directory.Packages.props`, together.
  - Then check the MAUI trim warning again, and the `UseOptimizedPublish=false` (Windows only) and `ValidateXcodeVersion=false` workarounds.
- **macOS:**
  - A universal build (`maccatalyst-arm64;maccatalyst-x64`), since macOS 26 still runs on some Intel Macs; only `maccatalyst-arm64` is built now.
- **Signing**, after the first release:
  - A Windows code-signing certificate, so SmartScreen stops warning; then drop the README's SmartScreen section.
  - An Apple Developer account (99 USD per year) to sign and notarise macOS releases; then drop the README's "Open Anyway" steps.
- **Translations:** the app's own texts in 12 languages, also the translocation text (DeepL, 2026-10-20), checked by native speakers. Not tied to a release: each language ships once its check is done.
- CI: move macOS from the `xcode-27` preview image to a regular image once one has Xcode 27, and raise the pinned actions now and then.

## Open points

None blocks the release; all are accepted as they are:

- [x] The edge cases of the mod hardening (NaN or infinity, a clock set back, a balance above 2^31, a failing date field, values of an unexpected type, a malformed configuration pair) didn't occur in the game runs; they are covered only by the reviews' Lua harnesses ([reviews/mod/](../reviews/mod/)).
- [x] Not checked by hand: a real second Steam account with a log dated in the future (unit tests only).
- [x] While a new game's map is generated (up to a minute) the app still shows the main menu; the log has `Seed text:` there but no line the app uses for Loading ([game-log.md](game-log.md#lines-that-matter)).
- [x] The game folder is only looked for in the default Steam library (no `libraryfolders.vdf`); it's found anyway through the running process.
- [x] The test-run scripts' Windows set of `calibration.psd1`, measured on build 40423, checked on build 40425 on 2026-10-24: `start`, `load`, `classify` of the game menu and the Quit flyout, `photo` and `quit` worked with the points and boxes unchanged.

## Next steps

1. **The mod:** done. Players activate it per savegame and save once; `autoActivate` doesn't help.
2. **Companion MVP:** done on Windows and macOS, both tested end to end with the game and Discord.
3. **Polish**, in this order:
   1. Presets B–E, custom templates for both lines and the hover text, and the live preview: done ([presence.md](presence.md#presets), [custom templates](presence.md#custom-templates-and-preview)) and tested with the game and Discord on 2026-10-17.
   2. The optional store button, and the "New Game" save name in the chosen language: done ([store button](presence.md#store-button), [texts](presence.md#texts-and-languages)) and tested with the game and Discord on 2026-10-17; the button was also checked from a second Discord account.
   3. **The first release, `0.1.0`:** unsigned, on .NET 11 RC2 (see [Decisions](#decisions)).
      - [x] The release script `tools/build-release.ps1`, the README's SmartScreen section,
        the mod's `url` in `modinfo.json` and [CHANGELOG.md](../CHANGELOG.md).
      - [x] The Windows zip built and checked: 94 files in a `TF3-DRPC` folder, with
        `LICENSE.txt` and `THIRD-PARTY-NOTICES.txt`, no `DATA` folder, 33 MB zipped, and the exe's copyright "© 2026 Zagrthos. MIT Licence." instead
        of "All rights reserved", which contradicted the licence (2026-10-22).
      - [x] CI (`.github/workflows/`): every pull request and every push to `main` is built and tested on
        Windows and macOS; a push to `main` also leaves both zips as workflow artifacts.
      - [x] The macOS zip from CI unzipped on a Mac and started (2026-10-23): Gatekeeper blocks
        the quarantined copy with "Done" and "Move to Trash" and offers "Open Anyway" in
        Privacy & Security, as the README says; the started app shows the menu bar icon and
        its menu, the settings window in front, and GitHub, mod.io and Exit work. The check
        found the bundle at 99 MB, 80 MB of it CoreCLR files the Native AOT app never loads,
        and the version shown as `0.1.0.0`; both fixed, the zip is now 7 MB.
      - [x] The Mod Hub's logo and gallery images, from a game run on 2026-10-24 in English: an
        in-game photo mode shot with a drawn status card, the settings window with a preset
        tooltip and the plain settings window.
      - [x] The mod prepared for the upload (2026-10-24), against the wiki's
        [guidelines](https://wiki.transportfever3.com/doku.php?id=modding:general:guidelines):
        the images are in the mod's `_metadata/` as the game's own mods have them, all
        1920 × 1080: `0.png` the cover with the name and the status card (see
        [Decisions](#decisions)), then the settings window on the darkened photo. The
        description in `modinfo.json` says what the mod shows, that it needs the app, that it is for PC only, and how to
        turn it on. The game's validator found no problems for PC and console.
      - [x] **A session on a Mac** (2026-10-24, macOS 27, game build 40425):
        - The app icon was missing from every macOS build: `Info.plist` named the icon set
          `appicon`, while MAUI names it after `cover.png`, so actool left it out without a
          warning. Fixed; Finder now shows the cover, and the zip grows to 10 MB. The menu bar
          icon is sharp at Retina size; normal size wasn't checked (no such display at hand),
          the code sets it to 18 points. A menu bar app has no Dock icon.
        - `tools/build-release.ps1` now also deletes the platform's `obj` folder: after the
          `Info.plist` change an incremental build still skipped actool.
        - `LICENSE.txt` and `THIRD-PARTY-NOTICES.txt` are next to `TF3-DRPC.app` in the zip.
          A quarantined copy is stopped by Gatekeeper with "Done" and "Move to Trash", as the
          README says; "Open Anyway" was last checked on 2026-10-23.
        - The settings window shows "Transport Fever 3 · Build 40425" while the game runs.
        - The test-run scripts: `tf3mac` compiles, its `pixels` command read the Windows
          reference pictures correctly, and the macOS set (`macos-804`, the screenshot height
          here) is measured, all but the download bar; `start`, `load`, `quit`, `state` and
          `photo` ran on the live game ([testing.md](testing.md#on-macos)). The reference
          pictures and their step are deleted.
      - [x] The mod uploaded from the Mod Hub (2026-10-24), not public yet: mod.io id `6477175` in
        `_metadata/mod.io_fileid.txt`, page <https://mod.io/g/transportfever3/m/discord-rich-presence>,
        the same as `AppConstants.ModIoUrl` (the app's "mod.io" button). The upload made
        `1.png` a copy of the cover and moved the settings window to `2.png` and `3.png`, as
        the gallery on mod.io has them. Delete any `.sonar` folder in `mod/zagrthos_drpc/`
        (an editor extension creates them) before each upload.
      - [x] The changelog's heading dated (`## 0.1.0 (app) and revision 1 (mod) — 2026-10-24`;
        the Release workflow wants it to end in `YYYY-MM-DD` and the section's links to be
        absolute) and the repository reset (2026-10-24): its history starts with one commit,
        before it goes public and before the first tag, since the tag and the release page
        point to commits. The content was checked for personal and machine details first.
      - In this order, because the mod's description and the app's buttons link to this
        repository and its Releases page:
        - [x] Make the repository public (2026-10-24).
        - [x] Release the app (2026-10-24): `v0.1.0`, immutable, with both zips and
          `SHA256SUMS.txt`. The Release workflow checks that the tag matches the app version
          and sits on `main`, that the changelog's section is dated, not empty and free of
          relative links, and that the version isn't released yet; then it builds and tests
          both zips, builds the release as a draft, compares the draft's files with the built
          ones, publishes it and verifies its attestation. Its run for `v0.1.0` published the
          release but failed that last check, which ran one second after publishing, before
          GitHub had made the attestation; minutes later `gh release verify` and
          `verify-asset` passed for the release and all three files. The check now waits for it
          up to 2 minutes.
        - [x] Make the mod public on mod.io (2026-10-24).

## Repository layout

| Path | Content |
|---|---|
| `mod/zagrthos_drpc/` | the mod; its `_metadata/` also holds the Mod Hub's cover (`0.png`) and gallery images |
| `tools/install-mod.ps1` | links the mod into the game's staging area (Windows and macOS) |
| `tools/build-release.ps1` | tests, publishes and zips the companion app for the platform it runs on, into `artifacts/` |
| `tools/new-third-party-notices.ps1`, `tools/licences/` | writes the zips' `THIRD-PARTY-NOTICES.txt` from the restore output; the SPDX texts and the licence texts of packages that only link to theirs |
| `tools/test-run/` | scripts that drive the game and the app in a test run (the game scripts also on macOS); see its README |
| `docs/` | these documents and the log samples |
| `.claude/skills/tf3-modding/` | an offline copy of the TF3 modding reference for coding agents |
| `reviews/` | dated code reviews with their reproduction scripts and recorded evidence |
| `assets/discord/` | images uploaded to the Discord application (`cover.png`), also the app icon's source |
| `companion/` | the companion app: `Tf3Drpc.Core`, `Tf3Drpc.App`, `Tf3Drpc.Core.Tests` |
| `README.md`, `CONTRIBUTING.md`, `LICENSE` | the overview for players, the guide for new contributors, the MIT licence |
| `CHANGELOG.md` | what each release of the app and the mod brings; the text for the GitHub release and the Mod Hub upload |
| `AGENTS.md` | conventions, commands and rules for contributors |
| `.github/` | the issue forms, the issue chooser, the pull request template, and the workflows `ci.yml` (build and test), `release.yml` (release for a `v<VersionPrefix>` tag on `main`) and one `codeql-<language>.yml` per CodeQL language (Actions, C#, Swift) with their shared `codeql/codeql-config.yml` |
| `.editorconfig` | shared editor and analyser settings; don't edit locally |

Only what the project needs goes into git. `.gitignore` is the Visual Studio template plus
`DATA/`, `*.zip` and local editor and system files (`.vscode/`,
`.DS_Store`); the log samples are excepted from its `*.log` rule.

## Sources

The TF3 install (Steam on Windows, build 40420 from 2026-10-08, later 40423 and 40425) with its Teal definitions
and base-game scripts, the TF3 modding reference (version `2026.10.23.2`, online at
<https://tf3-docs.transportfever.net>), test runs in the game on Windows (2026-10-13 to 2026-10-24) and on macOS 27
(2026-10-20 to 2026-10-24), the .NET MAUI documentation and the .NET 11 release notes up to RC2. Each document lists its own
references.
