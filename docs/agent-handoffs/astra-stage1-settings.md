# A1.2 — shared settings persistence

- Owner: Astra. Reviewer: Sol; Qwen may extend regression coverage in a separately assigned file.
- Base: `899c64e` plus the uncommitted A1.1 integration described in the stage ledger. No worker checkout changes or commits.
- Objective: move SettingsService and SettingsChangedMessage into Application, preserving schema, normalisation, save/retry behaviour and messenger notifications.
- Allowed files: Application settings service, ISettingsPaths contract, message and generated logging declarations; Application project reference to the already pinned CommunityToolkit.Mvvm; Application tests; corresponding MAUI service/message removals, path interface, aliases, DI registration and logging declarations; AGENTS, this handoff and stage ledger.
- Contract: ISettingsPaths exposes SettingsFilePath, IsDataWritable and RefreshDataWritable. The existing MAUI IPathService inherits this contract and its existing instance satisfies both registrations. No new filesystem discovery or storage location. Messenger message identity is shared through a global alias.
- Non-goals: monitor loop, view models, debounce/dispatcher/lifecycle contracts, Linux host composition, new package versions, platform behaviour changes.
- Acceptance: Debug/Release Linux builds and Core/Application tests, analysers, Linux host AOT/resource regression check; synthetic settings tests cover normalisation, notification, unchanged values and failed-save retry. Independent review required. Windows/macOS checks are out of scope by user direction and recorded as unverified, not a blocker. Linux host AOT does not exercise Application.
- Dependencies: existing Core SettingsStore and existing pinned messenger package.
- Stop conditions: schema/behaviour change required, new unresolved warnings, native API dependency or scope expansion.
