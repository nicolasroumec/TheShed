# The Shed — Status and Next Steps

## Current status
- [x] Phase 1 — Rename (SwimAnalytics → TheShed)
- [x] Phase 2 — Cleanup (swimming models and docs removed)
- [x] Phase 3 — Data model (password manager entities)
- [x] Phase 4 — Security (Argon2, client-side zero-knowledge encryption, JWT in an httpOnly
      cookie, antiforgery, integration tests) — see `SPRINTS.md`
- [~] Phase 5 — Blazor UI (auth, vaults + entries, generator, vault sharing, secure notes,
      tags, history, trash, attachments, password health, "Workshop" visual identity,
      mobile-first responsive, session lock, installable PWA, import/export, master password
      change) ← functionally complete, the items below remain

Sprint-by-sprint detail (what, when, why) lives in `SPRINTS.md` (the "Shipped" table at the end)
and in `git log` — not duplicated here.

## In progress
- 🟢 **Sprint 35** (`feature/session-hardening`): clipboard auto-clear (N4) ✅, sign out
  everywhere (N3) pending. N3 also closes the gap Sprint 34 leaves: other sessions keep a stale
  `keySalt` claim after a password change until their JWT expires.

## Pending, by value (see `FEATURES-ROADMAP.md`)
1. 🔵 **Sprint 23** — orphaned attachments on disk.
2. 🟡 **Language debt**: old `docs/*.md` and comments still in Spanish, against `CLAUDE.md`.
   `feature/i18n-english` branch in `SPRINTS.md`, mergeable at any time.
3. 🟣 Sprint 18 (2FA), recovery key, encrypted export, and the rest of the roadmap table.

## Next step
N3 (sign out everywhere). Loose end: logout shows 503 in the browser (see `SPRINTS.md`
Sprint 34) — cause unknown.
