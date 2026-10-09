# Security and features audit — The Shed
Date: 2026-08-13 (critical and high findings resolved since then — see the "✅ Resolved" notes on
each one, and the updated summary in "Suggested next steps" at the end)
Scope reviewed: `TheShed.Server/Security/*`, `TheShed.Server/Services/*`, `TheShed.Server/Controllers/*`,
`TheShed.Server/Data/TheShedContext.cs`, `TheShed.Server/Program.cs`, `TheShed.Client/Program.cs`,
`TheShed.Client/Auth/JwtAuthenticationStateProvider.cs`, `TheShed.Client/Services/AuthService.cs`,
`TheShed.Client/Pages/Login.razor`, `Register.razor`, `TheShed.Client/Components/Vault/EntryRow.razor.cs`,
`TheShed.Shared/Models/Entities/*`, `TheShed.Shared/Models/DTOs/Auth/*`, `TheShed.Shared/Helpers/PasswordGenerator.cs`,
`TheShed.Shared/Helpers/PasswordHealthChecker.cs`, `appsettings*.json`, `.gitignore`, every `*.csproj`,
`TheShed.Tests/Security/AesEncryptionServiceTests.cs`, `TheShed.Tests/Services/AuthServiceTests.cs`,
`docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/TODO.md`.

Not read line by line: `TagService.cs`, `TagsController.cs`, `NotesController.cs`, `TrashController.cs`,
`HealthController.cs` (they were checked to all carry `[Authorize]`), the remaining Razor UI components
(`VaultDetail`, `EntriesPanel`, `MembersPanel`, `NotesPanel`, history/attachment panels), EF migrations.

## Executive summary
The cryptographic foundation is well built: Argon2 for the master password, AES-256-GCM with a random
nonce per operation for entries, JWT in an `HttpOnly`/`Secure`/`SameSite=Lax` cookie, secrets kept out
of the repo (User Secrets / environment variables, correct `.gitignore`), vault access control
centralized and applied consistently, tests covering ciphertext tampering and wrong-key cases. **The
finding that dominates everything else is architectural, not a bug**: encryption happens on the server
with a key the server controls, so it is **not zero-knowledge** — anyone who compromises the server, the
database or the encryption key can read every password of every user without needing anyone's master
password. This is an explicit design decision (documented in `ARCHITECTURE.md`), not an oversight, but
it is the fundamental difference from Bitwarden/1Password/Proton Pass and should be a conscious,
communicated decision rather than an implicit one.
Main recommendation: consider moving key derivation and encryption/decryption to the client (WASM is
already there) before adding more surface on top of the current model.

## Critical findings 🔴

### C1 — Not a zero-knowledge architecture: the server can decrypt every password
**Location:** `TheShed.Server/Security/AesEncryptionService.cs:18-35`, `EncryptionSettings.cs`,
`TheShed.Server/Services/PasswordEntryService.cs:317-331` (`ToResponse` decrypts on the server),
`docs/ARCHITECTURE.md:10` ("server key in User Secrets").

The AES-256 key lives on the server (User Secrets in dev, an environment variable in prod) and is the
**same key for every user** — it is not derived from each user's master password. The server decrypts
every entry on every `GET` (`PasswordEntryService.ToResponse`, `SecureNoteService.ToResponse`,
`AttachmentService.DownloadAsync`). This means:
- A server admin, an attacker with RCE, a database dump plus the key, or a court order against the
  service operator can read **every** password of **every** user in plaintext.
- The user's master password only protects the *login*, not the data: whoever holds the encryption key
  needs nobody's master password to read the vault.
- This contradicts the model a user would expect from a modern password manager (Bitwarden, 1Password
  and Proton Pass are all zero-knowledge: encryption happens on the client with a key derived from the
  master password, which the server never sees).

**Impact:** total compromise of the trust model, not of a single endpoint.

**How to fix it (direction, not a full recipe):** move encryption/decryption to the Blazor WASM client
using `crypto.subtle` (Web Crypto API through JS interop, or `System.Security.Cryptography`, which also
runs on WASM) with a key derived from the master password through Argon2id/PBKDF2 on the client. The
server would then store and serve opaque blobs it can never decrypt. It is a large architectural change
(it affects vault sharing, server-side search, history, attachments), which is why it is flagged as a
critical finding and not as a loose task: it deserves an explicit product decision, not a patch.

**✅ Resolved (2026-08-14 to 2026-08-27, Sprints 25-28, decision D7 in `DECISIONS.md`).** Migrated to
real zero-knowledge: key derivation (PBKDF2 through Web Crypto) and a per-user RSA keypair
(Sprint 25), vault key + client-side encryption of entries/notes/attachments (Sprints 26/27), sharing
through asymmetric key wrapping (Sprint 27), removal of the server's legacy encryption (Sprint 28).
Checked in the browser on every sprint by inspecting the network payload: the server never receives
or stores plaintext. **Accepted residual risk (not C1, see M1):** removing a member from a shared
vault does not rotate the vault key yet.

## High findings 🟠

### A1 — No re-authentication for sensitive actions
**Location:** `TheShed.Client/Components/Vault/EntryRow.razor.cs:52-65` (`ToggleRevealAsync`,
`CopyPasswordAsync`), `TheShed.Server/Controllers/EntriesController.cs:29-34,99-105`.

Revealing a password, copying it or viewing a history version only requires the JWT cookie to still be
valid (60 minutes, `Jwt:ExpiryMinutes` in `appsettings.json:15`) — the master password is never asked
for again. In a professional password manager, viewing/exporting a password usually requires
re-authentication or, at the very least, a recent session unlock. With the session open (e.g. an
unattended laptop, a future XSS, or a stolen token), an attacker has unrestricted access to the whole
vault with no extra friction.

**✅ Resolved (2026-08-30, Sprint 30, see D9 in `DECISIONS.md`).** `IReauthGate`/`ReauthGate` with a
5-minute window, wired into reveal/copy (`EntryRow`) and into revealing a history version
(`EntryHistoryPanel`). A cheap check: the session already holds the correct key, so the typed password
is derived and compared with `CryptographicOperations.FixedTimeEquals` — no server round-trip.

### A2 — No auto-lock on inactivity
**Location:** not found in `TheShed.Client/Layout/MainLayout.razor.cs` nor in any other component;
`docs/PRODUCT.md:17` lists it as a planned feature ("automatic sign-out on inactivity").

The only session expiry is the 60-minute JWT. There is no inactivity timer on the client, nor a second
UI "lock" that asks for the master password again after N idle minutes, as Bitwarden/1Password have.
Planned feature, not implemented — see also the "Missing features" section.

**✅ Resolved (2026-08-30, Sprint 30, see D9 in `DECISIONS.md`).** A 15-minute inactivity timer
(activity listeners in `interop.js`) that calls `IAuthService.Lock()` — it clears the stretched key,
the keypair and the cached vault keys, without touching the cookie. Along the way it fixed the
project's biggest problem at the time: an F5 left the app "signed in" with every decryption path dead
(the stretched key only lives in memory); now it lands on a `Locked` screen that only asks for the
master password and re-derives.

### A3 — No rate limiting or progressive lockout on login
**Location:** `TheShed.Server/Services/AuthService.cs:45-59`, `TheShed.Server/Program.cs` (no
`AddRateLimiter` or equivalent middleware registered).

`LoginAsync` does not count failed attempts or apply delays. Nothing stops brute force or credential
stuffing against `/api/auth/login` at whatever speed the network allows. Argon2 in the hash mitigates
the cost of cracking a stolen hash, but does not protect the login endpoint itself.

**✅ Resolved (2026-08-16, Sprint 21).** `AddRateLimiter` in `Program.cs` with a fixed-window policy
partitioned by IP (10 attempts / 5 min, configurable through `RateLimiting:AuthPermitLimit` and
`AuthWindowMinutes`), applied with `[EnableRateLimiting]` on `Register` and `Login`. Checked against
the running server: attempts 1-10 → 401, from the 11th on → 429.
**Known limitation:** partitions by IP only, not by email. The rate-limiting middleware runs before
model binding, so the email is not parsed yet at that point; brute force spread across many IPs still
gets through. Doing it needs a counter inside `AuthService` — marked with `ponytail:` in `Program.cs`.
Behind a reverse proxy, `UseForwardedHeaders` is needed to see the real IP.

### A4 — No explicit CSRF protection despite a cookie session
**Location:** `TheShed.Server/Controllers/AuthController.cs:65-75` (`SetAuthCookie`,
`SameSite = SameSiteMode.Lax`), no `[ValidateAntiForgeryToken]` or antiforgery middleware in
`Program.cs`.

`SameSite=Lax` mitigates most cases (it blocks cross-site POST/PUT/DELETE), but it is the only
defense. There is no double-submit antiforgery token as an extra layer, which is the recommended
practice when authentication lives in a cookie. Real risk today: low-medium (mitigated by Lax), but it
is a fragile dependency on a single browser mechanism.

**✅ Resolved (2026-09-04, Sprint 32, see D10 in `DECISIONS.md`).** `AntiforgeryController` issues the
token (`GET /api/antiforgery/token`, anonymous) and a middleware in `Program.cs` validates it on every
unsafe method (POST/PUT/DELETE/PATCH), returning 400 if it is missing or does not match the cookie. The
client fetches it once at startup and resends it on every mutation through `CsrfHandler` (a
`DelegatingHandler` on the shared `HttpClient` — the typed clients don't change). It is still defense
in depth, an extra layer on top of `SameSite=Lax` (D6).

## Medium findings 🟡

### M1 — A single static encryption key for every user, with no rotation
**Location:** `TheShed.Server/Security/EncryptionSettings.cs`, `AesEncryptionService.cs:18-35`.

Beyond the underlying problem (C1), there is no key version, no rotation mechanism (`kid`/key-id in the
`nonce || ciphertext || tag` layout), and no way to re-encrypt existing data if the key is compromised
and has to be rotated. Changing the key today would invalidate all the data already encrypted.

### M2 ✅ — Orphaned attachments on disk after a cascading purge
> **Resolved** in Sprint 23 (`feature/session-hardening`): every purge path deletes the blobs.
**Location:** `TheShed.Server/Services/TrashService.cs:170-178` (an explicit `ponytail` comment
acknowledging the problem).

When a `PasswordEntry` is purged from the trash, the `Attachment` row goes away by cascade in the
database, but the encrypted file in `IAttachmentStorage` never gets a `DeleteAsync`. It is not a data
leak (the file stays encrypted), but it is an unbounded build-up of orphaned blobs.

### M3 — Password generator with no option to exclude ambiguous characters
**Location:** `TheShed.Shared/Helpers/PasswordGenerator.cs:10-13`.

The character pools are fixed (there is no flag to exclude `l/1/I/O/0`). A minor feature, but expected
from professional-grade generators, and listed in the industry reference checklist.

**🚫 Dropped (2026-08-31).** It makes sense when the password is typed by hand (offline, paper); in
this flow it is copied/pasted from the vault, so the real value is low. It stays off the roadmap unless
specifically requested.

### M4 — `PasswordHealthChecker` is heuristic, not dictionary-aware
**Location:** `TheShed.Shared/Helpers/PasswordHealthChecker.cs:11-13` (the code's own comment:
"Heuristic, not a full zxcvbn-style dictionary/pattern check").

Strength is computed from length + class variety + estimated entropy. It does not detect common
patterns (`Passw0rd!`, `Qwerty123!`) that would pass as "Strong" despite being trivial to crack with a
dictionary. Correctly documented as a known limitation in the code itself — not a hidden finding, but
worth keeping on the product radar.

### M5 — No explicit HTTP security headers
**Location:** `TheShed.Server/Program.cs` — no `UseHsts()`, and no `Content-Security-Policy`,
`X-Frame-Options`, `X-Content-Type-Options`, etc. configuration.

Not serious on its own (the app handles no third-party content or iframes today), but for an app that
handles high-value secrets, defense-in-depth headers (CSP in particular, to limit the impact of a
future XSS) are a standard practice that is missing.

**✅ Resolved (2026-08-16, Sprint 22).** `UseHsts()` (outside Development only) plus a middleware with
`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` and a full
CSP. `script-src` ended up strict (`'self' 'wasm-unsafe-eval'`, no `unsafe-inline`/`unsafe-eval`),
which is the part that actually stops XSS. Checked in the browser: the app boots, CDN fonts and icons
load, login makes its POST and there are no violations in the console.
**Side cost:** the strict CSP forced WASM asset fingerprinting off — see **D8** in `DECISIONS.md`.
**Known limitation:** `style-src` keeps `'unsafe-inline'` because 8 components use `style=""`
attributes. Marked with `ponytail:` in `Program.cs`; removing it means moving those styles to classes.

## Low findings / minor improvements 🟢

### B1 — Email existence leak on registration
**Location:** `TheShed.Server/Controllers/AuthController.cs:22-25` (`Conflict("That email is already
registered.")`).

Login already avoids enumeration (always "Invalid credentials", see `AuthController.cs:36`,
`AuthService.cs:50-52` — well done). Registration does confirm whether an email already exists, which is
a common and acceptable trade-off in most products (signup UX > enumeration risk), but it is worth
mentioning as an asymmetry.

### B2 — No explicit upper length limit on `RegisterRequest.Password`
**Location:** `TheShed.Shared/Models/DTOs/Auth/RegisterRequest.cs:13` (`[Required, MinLength(8)]`,
no `MaxLength`).

There is no length cap on the master password before it reaches Argon2. Low risk (Argon2 is not
vulnerable to long-string DoS in practice at this volume), but it is missing input validation at a trust
boundary.

**✅ Resolved (2026-08-16, Sprint 21).** `[MaxLength(128)]` on `RegisterRequest.Password` **and also on
`LoginRequest.Password`**, which the finding did not mention: login feeds the same Argon2 and is the
endpoint an anonymous caller can hit freely, so fixing it only on registration left half of it open.
Covered by `TheShed.Tests/Security/AuthRequestValidationTests.cs`.

## Missing features

| Feature | Status | Suggested priority |
|---|---|---|
| Configurable password generator | Done | — |
| Password strength indicator | Done (heuristic, see M4) | Low |
| Duplicate/reused password detection | Done (`PasswordHealthService.cs:34-38`) | — |
| Weak stored password detection | Done | — |
| Auto-lock on inactivity | Done (Sprint 30, see A2 above) | — |
| Re-authentication for sensitive actions | Done (Sprint 30, see A1 above) | — |
| Rate limiting / progressive lockout on login | Done — by IP, not by email (see A3) | — |
| 2FA/MFA to unlock the app | Missing — the `TwoFactorEnabled`/`TwoFactorSecret` fields exist on `User` (`TheShed.Shared/Models/Entities/User.cs:13-14`) but no logic uses them | High — planned in Sprint 18 |
| Built-in TOTP (generator/reader for stored accounts) | Missing | Medium — Sprint 24 |
| Breach alerts (Have I Been Pwned, k-anonymity) | Missing | Medium — Sprint 24 |
| Folders/tags/favorites/search | Done (vaults + tags + `IsFavorite` + search by name/username/URL) | — |
| End-to-end encrypted vault sharing | Done (Sprints 25-27, zero-knowledge — see C1 above). Rotating the vault key on member removal is still pending (M1) | — |
| Per-entry version history | Done (`EntryHistory`, `PasswordEntryService.cs:224-271`) | — |
| Secure notes / attachments | Done | — |
| Installable as a PWA | Done (Sprint 31) — does not cover offline data access | — |
| Import from other managers (CSV) | Missing — planned as Sprint 17 in `docs/SPRINTS.md`, pending a client-side rewrite | Medium (already prioritized by the team) |
| Export your own entries | Missing — same Sprint 17 | Medium |
| Browser autofill / mobile apps | Explicitly out of scope (`docs/PRODUCT.md:61-63`) | — (product decision) |

## Code quality

- **Encryption well isolated and tested**: `AesEncryptionService` has no UI logic mixed in, and
  `TheShed.Tests/Security/AesEncryptionServiceTests.cs` covers ciphertext tampering, wrong key,
  wrong-length key and truncated data — the testing bar here is solid.
- **Centralized access control**: `IVaultAccessService.GetAccessAsync` (`VaultAccessService.cs:14-37`)
  is the single source of truth for vault permissions, and every service that touches vault data
  (`PasswordEntryService`, `SecureNoteService`, `AttachmentService`, `TrashService`) goes through it
  before reading or writing. A consistent pattern that lowers the IDOR risk.
- **Consistent existence hiding**: vaults/entries without access return `NotFound` instead of
  `Forbidden` when the user should not know they exist (`PasswordEntryService.cs:26-31`,
  `VaultService.cs:230-243`) — well thought out.
- **Secrets out of the repo**: `.gitignore` correctly excludes `appsettings.*.json` except
  `appsettings.json`/`appsettings.Example.json`; `appsettings.Development.json` (with real secrets) is
  not tracked in git — checked with `git ls-files` and `git show HEAD:...`. `appsettings.Example.json`
  documents well what to generate and how (`dotnet user-secrets set ...`).
- **Technical debt marked explicitly**: several `// ponytail:` comments document deliberate
  simplifications with their known ceiling (e.g. `PasswordEntryService.cs:124` — history with no
  version cap; `TrashService.cs:172-174` — orphaned attachments; `LocalFileAttachmentStorage.cs:31-33` —
  no path sanitization because the key is always a server-side GUID). It is a practice that helps a lot
  to tell "nobody thought about it" from "it was thought about and deliberately postponed".
- **Dependencies**: every Microsoft library is on `10.0.2`/`10.0.9` (the current .NET 10 line), with no
  visibly outdated versions. `Isopoh.Cryptography.Argon2 2.0.0` is the reference Argon2 library in the
  .NET ecosystem — the right choice, not a home-grown implementation.
- **Could not verify**: known vulnerabilities in the lockfile — `dotnet list package --vulnerable` was
  not run and no CVE database was checked in this pass (out of scope for a static code read).
- **Could not verify**: performance of `PasswordHealthService.GetReportAsync`
  (`PasswordHealthService.cs:24-32`) at scale — it decrypts **all** of the user's entries in memory on
  every health report request. Correct security-wise (it never persists or logs the result), but the
  cost with large vaults was not measured.

## Opportunities to stand out

1. **"Local-first, self-hosted, no third-party cloud" as the core pitch.** The project already runs as
   a self-hosted Blazor WASM + SQL Server monolith, with no dependency on a third-party SaaS backend.
   Instead of competing with Bitwarden on "zero-knowledge encryption in someone else's cloud", The Shed
   could position itself as the manager you host yourself on your NAS/VPS, with the explicit threat
   model of "I trust my own server, not a third party" — which is in fact consistent with the current
   design (C1 would stop being a defect and become a stated product decision).

2. **Version history with diffing already solved on the client.** The recent work in `EntryRow`
   (`OnParametersSet` in `EntryRow.razor.cs:29-39`) already invalidates the history/reveal when the
   password changes under the component, using `PasswordChangedAt` as the invalidation key. It is a solid
   base for a feature many competitors treat as secondary: a truly reliable, audited password history
   (who changed what and when, already with `ChangedByUserId` in the model), useful for vaults shared by
   teams/families where "who touched this" matters.

3. **Vault sharing with explicit roles (read/write) already modeled at row level.** The
   `VaultMember` + `VaultRole` system is more granular than the all-or-nothing sharing several personal
   managers offer. With the database already in place, the app has a head start for a niche of
   "family/team vaults with real permissions" if end-to-end encryption (C1) is solved so that sharing
   means exchanging keys, not just a row permission.

4. **Trash with configurable retention and automatic purge already built.** `TrashPurgeService` +
   `TrashSettings.RetentionDays` is a "safety net against human error" feature that many managers treat
   as an afterthought; here it is already a first-class `BackgroundService` with tests. It is worth
   turning it into a visible differentiator ("you never lose a deleted password for 30 days",
   configurable) instead of leaving it as an internal feature.

## Suggested next steps (by priority) — updated 2026-09-04

Every critical and high finding in this audit is resolved (C1, A1, A2, A3, A4). What remains:

1. Orphaned attachments on purge (M2) + excluding ambiguous characters from the generator (M3) —
   Sprint 23, low impact, blocks nothing.
2. The app's own 2FA (Sprint 18) and TOTP/breach alerts for stored accounts (Sprint 24) —
   High/Medium priority, no date.

~~Rate limiting (A3)~~ ✅ Sprint 21 · ~~CSP/HSTS (M5)~~ ✅ Sprint 22, see D8 · ~~Zero-knowledge
(C1)~~ ✅ Sprints 25-28, see D7 · ~~Auto-lock + re-authentication (A1, A2)~~ ✅ Sprint 30, see D9 ·
~~Antiforgery (A4)~~ ✅ Sprint 32, see D10 · ~~Import/export (Sprint 17)~~ ✅ Sprint 17, PR #28.
