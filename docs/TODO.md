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
- 🔴 **Fix for Sprint 34** (`fix/owned-vault-keys-rewrap`). `feature/auth-hash` (N1 + N2,
  `DECISIONS.md` D11) merged in PR #32 with a bug: changing the master password didn't re-wrap
  owned vault keys, leaving those vaults undecryptable. Found in the browser check after the
  merge; fixed, tested and re-checked in Chrome. Pending: the PR to `main` — until then, don't
  use the password change on real data.

## Pending, by value (see `FEATURES-ROADMAP.md`)
1. 🔵 **Clipboard auto-clear (N4)** and **sign out everywhere (N3)** — both small. N3 also
   closes the gap Sprint 34 leaves: other sessions keep a stale `keySalt` claim after a
   password change until their JWT expires.
2. 🔵 **Sprint 23** — orphaned attachments on disk.
3. 🟡 **Language debt**: old `docs/*.md` and comments still in Spanish, against `CLAUDE.md`.
   `feature/i18n-english` branch in `SPRINTS.md`, mergeable at any time.
4. 🟣 Sprint 18 (2FA), recovery key, encrypted export, and the rest of the roadmap table.

## Next step
PR for `fix/owned-vault-keys-rewrap`. Then N4 + N3. Loose end: logout shows 503 in the browser (see `SPRINTS.md`
Sprint 34) — not from this branch, cause unknown.
