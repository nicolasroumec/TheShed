# The Shed — Sprint plan

> Roadmap from Phase 4 (Security) → Phase 5 (UI). Each sprint closes on a branch with a PR to
> `main`. Status: 🟢 in progress · 🔵 pending · 🟣 future · ✅ done.
> See the overall phases in `TODO.md` and decisions in `DECISIONS.md`. Shipped sprints are
> collapsed at the end for reference — the day-to-day detail of each one lives in `git log`, not
> here.

## 🟣 Sprint 18 — 2FA (TOTP) · `feature/2fa`
> `User.TwoFactorSecret` (nullable, null = disabled) already exists in the model.
- [ ] Enable 2FA: generate a TOTP secret + QR, verify a code before enabling
- [ ] Require a TOTP code on login when enabled (second step of the auth flow)
- [ ] Disable 2FA (re-verifying)
- [ ] Tests for the TOTP flow (code validation, time window)
- [ ] PR to `main`

> **Out of scope (post-roadmap):** refresh tokens (D2), native mobile app, browser extension,
> offline vault access. (The zero-knowledge model left this list: it was done in Sprints 25-28.
> The installable app moved to Sprint 31 — it does not cover offline data.)

---

## ✅ Shipped (reference — detail in `git log` / PRs)

| # | Sprint | Branch | PR |
|---|--------|------|----|
| 1 | AES-256 entry encryption | `feature/encryption-aes` | #2 |
| 2+3+3.5 | Auth: DTOs, JWT infra, register/login endpoints, tests | `feature/jwt-auth` | #3 |
| 4 | Entries API (`PasswordEntry` CRUD) | `feature/entries-api` | #4 |
| 5 | UI: auth + theme | `feature/client-auth` + `feature/ui-theme` | #5, #6 |
| 6 | Vaults API (CRUD + memberships) | `feature/vaults-api` | #7 |
| 7 | UI: vaults + entries (WASM client) | `feature/vaults-ui` | #8 |
| 8 | Vault sharing (members) | `feature/vault-sharing` | #9 |
| 9 | Secure notes (CRUD) | `feature/secure-notes` | #10 |
| 10 | Tags | `feature/tags` + `feature/tags-ui` | #11 |
| 11 | Entry UX: favorites + search + copy | `feature/entry-ux` | #12 |
| 12 | Version history | `feature/entry-history` | #13 |
| 13 | Trash / restore (automatic purge after 30 days) | `feature/trash` | — |
| 14 | Attachments | `feature/attachments` | #14 |
| 15 | Password health (original server-side version — replaced client-side in 27) | `feature/password-health` | #15 |
| 16 | `httpOnly` cookie for the JWT (D6) | `feature/jwt-cookie` | #16 |
| 17 | CSV import/export (LastPass/Bitwarden/1Password), client-side on top of the zero-knowledge model | `feature/import-export` | #28 |
| 20 | Frontend refactor: "Workshop" identity, mobile-first responsive, typography, `VaultDetail` split. Design detail absorbed into `docs/UI.md` | `feature/frontend` | #17 |
| 21+22 | Hardening: rate limiting (A3) + password length cap (B2) + CSP/HSTS (M5, see D8). A4 (antiforgery) was deliberately left out, closed in Sprint 32 | `feature/security-hardening` | #20 |
| 23 | Purging an entry or a vault (manual or by expiry) also deletes its attachment blobs from disk (`AUDIT.md` M2). M3, B1 and Sprint 24 (TOTP for saved entries + HIBP) were dropped on 2026-08-31, see "Dropped" in `AUDIT.md` | `feature/session-hardening` | #34 |
| 25 | Zero-knowledge: key derivation (PBKDF2 through Web Crypto) + per-user RSA keypair (D7) | `feature/e2e-key-derivation` | #21 |
| 26 | Zero-knowledge: vault key + client-side encryption of entries/notes | `feature/e2e-vault-encryption` | #23 |
| 27 | Zero-knowledge: vault sharing (RSA-OAEP key wrapping). Removing a member does not rotate the vault key — accepted risk, see D7 | `feature/e2e-vault-sharing` | #24 |
| 28 | Zero-knowledge: removal of the server's legacy encryption (scope cut — data from before 26 was test data, not migrated) | `feature/e2e-migration` | #25 |
| 29 | Generator: memorable mode (passphrase) | `feature/password-generator-passphrase` | #22 |
| 30 | Session lock: unlock screen + auto-lock on inactivity (A2) + re-authentication for reveal/copy (A1). See D9 | `feature/session-lock` | #26 |
| 31 | Installable PWA: manifest + icons, service worker, offline banner. Does not cover offline data access | `feature/pwa` | #27 |
| 32 | Antiforgery (A4): token from an endpoint + `CsrfHandler`. See D10 | `feature/antiforgery` | #29 |
| 33 | Integration tests: `WebApplicationFactory` + SQLite in-memory (auth, antiforgery, cross-user vault access) | `feature/integration-tests` | #30 |
| — | `Login`, `Register`, `RedirectToLogin` moved to code-behind | `feature/razor-code-behind` | #31 |
| 34 | The client sends an auth hash, never the master password (N1, prelogin salt) + master password change (N2), re-wrapping the private key and owned vault keys. The owned-vault fix came after the merge. See D11 | `feature/auth-hash` + `fix/owned-vault-keys-rewrap` | #32, #33 |
| 35 | Clipboard auto-clear after 30 s (N4) + sign out everywhere (N3): `User.TokenVersion` checked on every request, also bumped by a password change; any 401 sends the app to `/login`. See D12 | `feature/session-hardening` | #34 |
| — | Language debt: remaining Spanish docs, comments, API messages and test names translated to English; `AUDITORIA.md` → `AUDIT.md` | `feature/i18n-english` | pending |

**Note:** Sprint 19 (`feature/session-timeout` — auto-logout + re-authentication) was never
started; its whole scope ended up covered by Sprint 30 with a better design (lock/unlock instead of
a hard logout). It is removed from the pending list, it adds nothing on its own.
