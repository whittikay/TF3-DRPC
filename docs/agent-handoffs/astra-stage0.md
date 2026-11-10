# A0 — Astra: Linux toolchain and desktop feasibility

- Owner: Astra. Reviewer: GPT-Sol (S0).
- Base: upstream `00592fd2b5cc6a36fa7a4c158ce5fbd830db7930`; subsequent review uses the
  `stage0-bootstrap-01` snapshot recorded in `.agent-state/bootstrap.json`.
- Checkout: primary repository; branch `linux/astra-stage0`.
- Read: `AGENTS.md`, `docs/linux-implementation-plan.md`, `docs/linux-stages.md`.

## Objective

Establish a reproducible Linux build without MAUI workloads, preserve the SDK pin, run
Core's baseline tests and publish a minimal Avalonia desktop host with compiled bindings,
Core resources and an optional tray. Keep the window reachable without a tray. Prepare
isolated worktrees and bounded handoffs for the other agents.

## Owned paths

- `AGENTS.md`, `.gitignore`, `.gitattributes`, `docs/README.md`.
- `docs/linux-implementation-plan.md`, `docs/linux-stages.md`, `docs/agent-handoffs/`.
- `companion/Directory.Packages.props`, `companion/TF3-DRPC.Linux.slnx`.
- `companion/Tf3Drpc.Linux/`, `tools/linux-dotnet.sh`, `tools/.editorconfig`.
- Ignored local SDK, evidence and bootstrap files. Do not change shared Core behaviour.

Any new dependency, cross-platform configuration change or extension of this list must
be called out for review. Do not modify the root `.editorconfig` or downgrade the SDK.

## Non-goals and contracts

No shared-service extraction, Discord connection, real Steam/game reads, settings store,
production single-instance service, final UI, installer or release pipeline in this task.
Keep the spike independent of the MAUI solution. Resource verification must execute in
the published binary; GUI smoke checks must not require Discord or a running game.
No commits, pushes, system-package changes or live-presence tests are authorised.

## Acceptance and stop conditions

Use the Stage 0 commands in `docs/linux-stages.md`; report Debug/Release results, skipped
tests, AOT warnings and native-binary startup separately. Inspect package/native dependency
requirements. Do not suppress AOT warnings or call an untested backend supported.

The first local implementation is ready for S0 review, but Stage 0 remains open until
its broader desktop and portability checks are complete. Respond to findings in small
patches. Stage 1 must wait for the go/no-go decision.
