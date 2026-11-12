# A1.1 — shared presence seam

- Owner: Astra. Independent reviewer: Sol; Qwen may add regression coverage under a separate handoff.
- Base: `899c64e`, clean primary checkout, following the Stage 0 go decision and the user's Stage 1 implementation authorisation. Historical bootstrap manifests are preserved, not used as the current baseline.
- Objective: introduce `Tf3Drpc.Application` and extract the existing presence/preview service and its settings/translation contracts without changing behaviour. MAUI consumes the extracted implementation.
- Allowed files: new `companion/Tf3Drpc.Application/` and `companion/Tf3Drpc.Application.Tests/`; the four extracted MAUI source files, a MAUI global-alias file and project reference; explicit MAUI Application qualifications in App.xaml.cs, AppLifecycleService.cs and MacWindowService.cs to avoid the new sibling namespace collision; both solution files; `AGENTS.md`, this handoff and the stage ledger.
- Non-goals: full view-model/settings/monitor extraction, dispatcher/timer/launcher/lifecycle adapters, Linux UI integration, Discord transport, Steam discovery, package or SDK changes.
- Contracts: preserve existing method signatures and Core monitor interfaces; shared contracts become public for host adapters. Presence service remains the single implementation and retains its injected TimeProvider.
- Acceptance: Debug/Release Linux solution builds and Core/application tests; analyser output; existing Linux Native AOT publish and resource check. Native Windows/macOS builds/publishes and independent review remain required before integration approval. Linux host publish does not exercise the extracted service until host composition is added.
- Dependencies: existing Core public API and existing pinned SDK/xunit packages. No new package versions.
- Stop conditions: behavioural changes needed, new warnings not locally resolvable, changes beyond the listed scope, or incompatible public contracts.

Subsequent Stage 1 increments will extract settings/messages and monitor orchestration, then introduce platform adapters alongside view-model/lifecycle extraction. Stages 2/3 are not yet unblocked by this first increment. No worker checkout changes, commits or pushes are authorised by this handoff.
