# S0 — GPT-Sol: independent Stage 0 review

- Owner: GPT-Sol. Implementer under review: Astra.
- Base: upstream `00592fd2b5cc6a36fa7a4c158ce5fbd830db7930` plus `stage0-bootstrap-01`.
  Verify `.agent-state/bootstrap.json`; the spike is uncommitted, not present in `HEAD`.
- Checkout: `.worktrees/sol`; branch `linux/sol-stage0-review`.
- Read: `AGENTS.md`, `docs/linux-implementation-plan.md`, `docs/linux-stages.md`, A0.

## Objective

Independently evaluate the actual Linux toolchain/spike changes against the approved plan.
Search for failure cases; do not treat Astra's reported green checks as your own evidence.
Recommend go, conditional go, or blocked, with outstanding gate conditions explicit.

## Allowed writes

- `reviews/linux/stage-0/sol-review.md` (create it).
- Ignored `.agent-state/` for raw logs and experiments inside this worktree.

All source/configuration is read-only for this task. Propose precise fixes in the review
instead of silently implementing them. Ask for an explicit follow-up task if code changes
are needed. Never modify Astra's or Qwen's checkout, the shared SDK, or system packages.

## Review checklist

1. Examine the diff against `HEAD`, including untracked seeded files in the manifest.
   Check that the Windows/macOS solution, SDK/workload pins and Core behaviour are intact.
2. Check the wrapper's SDK selection, workload-resolver scope, isolated caches, argument
   forwarding, handling of paths with spaces and live-test opt-out. It is not a sandbox.
3. Reproduce Debug/Release builds, non-live Core tests and Native AOT publish using the
   documented commands in your own worktree. Your cache is separate and may need downloads.
4. Run the published resource check. Inspect the `UseOptimizedPublish=false` and satellite
   targets: are they necessary and sufficient? A managed run is not an AOT startup test.
5. Inspect compiled bindings, asset loading, shutdown/timer disposal and no-tray behaviour.
   Assess the limits of the three-second smoke test. It must not claim tray visibility,
   menu usability, single-instance behaviour or native Wayland coverage it does not test.
6. GUI smoke runs may open only the feasibility host. Do not launch a game, connect to
   Discord, change a desktop/client setting or capture the Discord window.
7. Check that package additions are scoped to the Linux project and that documentation
   accurately distinguishes the spike from a working companion/release.
8. Identify native dependencies, baseline-distro/AOT risks, and the remaining Stage 0
   checks. Do not install missing dependencies or change pins without approval.

## Deliverable and stopping point

Use the report template in `docs/linux-stages.md`. Findings need severity, path/line,
concrete scenario and reproduction/test where possible. List commands actually run,
skips, unavailable environments and remaining risks even if no defect is found. Keep
machine paths/account details out of the public review; raw evidence stays ignored.

Do not start Stage 1 or Linux game discovery yet. Later, after contracts settle, Sol is
expected to own Stage 3 and the Stage 4 UI, each under a new handoff.
