# The Shed — Status and Next Steps

## Current status
- [x] Phase 1 — Rename (SwimAnalytics → TheShed)
- [x] Phase 2 — Cleanup (swimming models and docs removed)
- [x] Phase 3 — Data model (password manager entities)
- [x] Phase 4 — Security (Argon2, client-side zero-knowledge encryption, auth hash instead of the
      master password, JWT in an httpOnly cookie with server-side revocation, antiforgery,
      integration tests) — see `SPRINTS.md` and `DECISIONS.md`
- [~] Phase 5 — Blazor UI (auth, vaults + entries, generator, vault sharing, secure notes,
      tags, history, trash, attachments, password health, "Workshop" visual identity,
      mobile-first responsive, session lock, installable PWA, import/export, master password
      change, sign out everywhere, clipboard auto-clear, 2FA) ← functionally complete, the items
      below remain

Sprint-by-sprint detail (what, when, why) lives in `SPRINTS.md` (the "Shipped" table at the end)
and in `git log` — not duplicated here.

## Awaiting PR
- **`feature/2fa-totp`** → `main`: Sprint 18, 2FA with an authenticator app (TOTP). See D13.

## Pending, by value (see `FEATURES-ROADMAP.md`)
1. 🟣 **Recovery key**: today a forgotten master password means the data is lost for good.
2. 🟣 **Encrypted export (N5)**: today the CSV export is plaintext.
3. 🟣 The rest of the roadmap table: old passwords in Health, rotate the vault key on member
   removal (M1), one-time share link, per-vault activity log.

## Next step
Merge `feature/2fa-totp`. Then the recovery key, then encrypted export.

## Loose ends
- `POST /api/auth/logout` shows **503** in the browser's network log (the user still ends up
  signed out). The server answers 200 to the same request via curl, and neither service worker
  produces a 503 — cause not found yet. Seen during the Sprint 34 check.
- 2FA wasn't checked end to end in a browser with a real authenticator app — worth one manual
  pass (set up, sign in with the code, turn off).
- `TwoFactorSecret` is stored unencrypted (`ponytail:` note on `User`). Wrap it with ASP.NET Data
  Protection once a deployment with a persistent key ring exists.
- The clipboard auto-clear was checked in Chrome with a stubbed clipboard (the real one needs a
  permission prompt the automation can't accept); worth one manual check.
