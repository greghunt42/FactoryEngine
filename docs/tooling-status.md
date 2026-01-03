# fe-tools Status (Phase 2 Audit)

Phase 2 task #7 (`docs/implementation-roadmap.md`) called for the CLI skeleton to expose `validate-data` and `validate-assets`. The current toolchain delivers that plus additional commands introduced while wiring the asset pipeline into CI.

## Coverage Summary

| Requirement | Status | Notes |
|-------------|--------|-------|
| `validate-assets` + metadata rules/options | ✅ | Supports catalog discovery, metadata configs, hash fixing, sound-bank coverage, and shared options files documented in `docs/tooling-cli.md`. |
| `validate-data` | ✅ | Loads descriptors from manifests/assemblies, enforces asset references, emits canonical prefabs + JSON reports, and shares metadata rules with assets. |
| CLI extensibility (Phase 2 scope) | ✅ | Command registry powers `validate-assets`, `validate-data`, `validate-modules`, `hash`, and the new `validate-all` driver; modules/tests cover YAML manifests + report generation. |
| CI/shared entry point | ✅ | `build/validate-all.json` plus `build/validate-all.{sh,ps1}` keep desktops and GitHub Actions aligned. The workflow (`.github/workflows/ci.yml`) runs these alongside `dotnet test`. |

## Remaining Backlog

- No outstanding work is required to close Phase 2 task #7; future CLI enhancements are tracked under the Phase 5 “Tooling & Hardening” roadmap entries (capture/replay, performance harness, crash-reporting, etc.).
- When new commands/flags arise, append them to `docs/tooling-cli.md` and extend `docs/tooling-status.md` so this audit stays current.
