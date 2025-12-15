# Architecture Decision Records

FactoryEngine tracks major technical choices in Architecture Decision Records (ADRs). Each ADR documents the context, options, decision, and consequences so future contributors understand why the system works the way it does.

## Conventions
- Filenames follow `NNNN-short-title.md` where `NNNN` is zero-padded in chronological order.
- ADRs live under `docs/adrs/` and never change order after being published.
- Amendments create a new ADR that supersedes the old one rather than mutating history.
- Refer to ADR IDs in code comments and docs whenever a design relies on a recorded decision.

## Template
Copy `_template.md` when drafting a new ADR.
