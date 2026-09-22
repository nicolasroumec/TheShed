# The Shed — New Features Roadmap
Date: 2026-09-22 · Based on a code audit of `main` at `07bf7ed` (after PR #31).

Complements `AUDITORIA.md` (C1, A1–A4 and M5 are resolved there; M1, M2 and IP-only rate limiting
are already tracked). Items already discarded on 2026-08-31 (HIBP, ambiguous characters, TOTP for
saved entries) and out-of-scope items (browser extension, offline access) are not re-proposed.

## New findings

### N1 🔴 — The master password reaches the server, which can then decrypt everything
**Where:** `TheShed.Client/Services/AuthService.cs` — `LoginAsync` and `RegisterAsync` post
`request.Password` as-is; `TheShed.Server/Services/AuthService.cs` hashes it with Argon2.

The server receives the raw master password and already stores `KeySalt` and
`EncryptedPrivateKey`. PBKDF2 is deterministic, so a compromised or malicious server can do:

```
password (from /login) + KeySalt → stretched key → decrypt EncryptedPrivateKey
→ unwrap every vault key → read everything
```

A database dump alone is still useless. What breaks is protection against an *active* server
compromise (RCE, a middleware logging request bodies, a curious operator), which is exactly the
threat zero-knowledge (D7) exists for.

**Fix (Bitwarden's scheme):**
1. Client derives `masterKey = PBKDF2(password, salt, 600k)` — unchanged.
2. Client sends `authHash = PBKDF2(masterKey, password, 1)` instead of the password.
3. Server Argon2-hashes `authHash` (the CLAUDE.md Argon2 rule still holds).

Login needs the salt *before* deriving, so add `GET /api/auth/prelogin?email=`. For unknown emails
it must return a fake but deterministic salt (e.g. `HMAC(serverSecret, email)`), otherwise it
becomes an account-enumeration endpoint. Rate-limit it with the existing auth policy.

**Migration:** existing accounts can't be converted server-side (the server shouldn't hold the
password). If data is still test data (as in Sprint 28), re-registering is enough.

**Caveat:** no web app is zero-knowledge against a server that ships malicious JS. N1 matters
because today *reading* traffic is enough; after the fix, an attacker has to *modify* the client.

### N2 🟡 — No way to change the master password
`AuthController` exposes only register, login, me, logout and public-key. A leaked master password
has no remedy. The key hierarchy makes this cheap: re-encrypt `EncryptedPrivateKey` with the new
stretched key and store the new salt + hash. Vault keys and entries are untouched.

### N3 🟡 — A stolen JWT can't be revoked
Logout only deletes the cookie; a stolen cookie is valid for up to 60 minutes
(`Jwt:ExpiryMinutes`). Minimal fix: `User.TokenVersion` column, emitted as a claim, checked in
`OnTokenValidated`. Incrementing it gives "sign out everywhere" and invalidates sessions on a
password change (N2).

### N4 🟡 — Clipboard is never cleared
`EntryRow.razor.cs` copies the plaintext password and leaves it there. Clear it after 30 s in
`interop.js`, only if the clipboard still holds what we copied (fall back to clearing
unconditionally if `readText` permission is denied).

### N5 🟡 — Export writes plaintext CSV
`ImportExportClient` exports a plaintext CSV. A backup left in Downloads exposes every password.

## Proposed features (value / cost order)

| # | Feature | Why | Cost |
|---|---|---|---|
| 1 | Auth hash + prelogin (N1) | Closes the gap in the zero-knowledge model | M |
| 2 | Change master password (N2) | No remedy today if it leaks; cheap thanks to the key hierarchy | S |
| 3 | Clipboard auto-clear (N4) | Industry standard, a few lines of JS | XS |
| 4 | Sign out everywhere (N3) | Real revocation; one column + one claim | S |
| 5 | 2FA TOTP (Sprint 18, already planned) | Fields exist on `User`. With zero-knowledge it protects login, not data | M |
| 6 | Recovery key | Forgotten master password = data lost forever. A random printable key that also wraps the private key (like 1Password's Emergency Kit) | M |
| 7 | Encrypted export (N5) | JSON encrypted with AES-GCM under an export password; keep CSV as an explicit "unencrypted" option | S |
| 8 | Health: old passwords | `PasswordChangedAt` already exists; flag entries older than 12 months in `Health` | XS |
| 9 | Rotate vault key on member removal (M1) | Needed for sharing with teams/families to be trustworthy | L |
| 10 | One-time share link for a single entry (Bitwarden Send-like) | Differentiator: key lives in the URL `#fragment`, the server never sees it; link expires | M |
| 11 | Per-vault activity log | Builds on `ChangedByUserId` (AUDITORIA opportunity #2): who changed what, when | M |

## Suggested order
1. **N1 + N2 together** (`feature/auth-hash`): same flow, same migration. Cover with the new
   `WebApplicationFactory` suite (`TheShed.Tests/Integration/AuthFlowTests.cs`): the server must
   never receive the raw password.
2. N4 and N3 (small), plus the pending Sprint 23 (orphaned attachments).
3. Then Sprint 18 (2FA), recovery key, encrypted export, and the rest by value.
