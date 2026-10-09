# The Shed — Decision log

> Technical decisions with their context. Chronological order (most recent at the bottom).

## D1 — Master password hash: Argon2 (server)
**Decision:** hash the master password with **Argon2** on the server, using
`Isopoh.Cryptography.Argon2` (PHC format with embedded salt and parameters).
**Context:** the *zero-knowledge* model (deriving the key on the client) is left as a future
evolution. For now the server receives the password on login/register.
**Update (D11, Sprint 34):** the server no longer receives the password — it Argon2-hashes the
client's auth hash instead. Argon2 stays as the server-side hash.
**Alternatives discarded:** bcrypt, PBKDF2, MD5/SHA (forbidden by CLAUDE.md).

## D2 — Sessions: JWT (HMAC-SHA256, access token only)
**Decision:** a JWT access token signed with HMAC-SHA256; the key lives in User Secrets
(dev) / environment variables (prod), never in `appsettings.json`.
**Context:** no refresh token yet; it gets added when needed. Implementation detail in
`AUTH_FLOW.md`.
**Update (Sprint 35):** tokens can now be revoked before they expire — see D12.

## D3 — Entry encryption: AES-256-GCM with a server key
**Decision:** encrypt the sensitive entry values (`PasswordEntry.PasswordEncrypted`) with
**AES-256-GCM**. Stored format: `base64(nonce(12) || ciphertext || tag(16))`.
The AES-256 key comes from **User Secrets** (`Encryption:Key`, 32 bytes in base64), the same
pattern as the JWT key.
**Why GCM:** authenticated encryption — it detects tampering through the integrity tag, avoiding
the manual, error-prone assembly of AES-CBC + HMAC.
**Why a server key:** consistent with D1/D2 (not zero-knowledge for now). It means the server can
decrypt; acceptable at this stage.
**Future evolution:** a key derived from the master password (zero-knowledge) or a per-vault key
for shared vaults.
**Implementation:** `TheShed.Server/Security/{IEncryptionService,AesEncryptionService,EncryptionSettings}.cs`;
random nonce per operation; tests in `TheShed.Tests/Security/AesEncryptionServiceTests.cs`.
**Superseded by D7** (2026-08-14): this paragraph's "future evolution" became the decision taken.
This entry stays as a historical record of why the model started this way, not as the current
design.

## D4 — Shared vaults: owner via `OwnerId`, members via `VaultMember` + role
**Decision:** a vault's owner is tracked with `Vault.OwnerId` and **no** `VaultMember` is created
for them. Sharing = adding `VaultMember (UserId, Role)` rows with `VaultRole` ∈ `{Viewer, Editor}`.
Member management (list/invite/change role/remove) is **owner-only**.
**Context:** avoids the ambiguous "owner who is also a member" case (with no dedup in the listing)
and keeps a single source of truth for ownership. Effective access is resolved by
`IVaultAccessService` (Sprint 4): `Owner/Editor → Write`, `Viewer → Read`, anything else → `None`.
**Authorization:** no access to the vault → 404 (hides existence); access but not the owner → 403.
Invite by exact email: unknown user → 404, already a member or the owner themselves → 409.
**Future evolution:** a per-vault AES key for real zero-knowledge sharing (see D3).
**Implementation:** `VaultService` (Add/UpdateRole/Remove/ListMembers) + `VaultsController`
under `/api/vaults/{id}/members`; DTOs in `TheShed.Shared/Models/DTOs/Vaults/`; tests in
`TheShed.Tests/{Services/VaultServiceTests,Controllers/VaultsControllerTests}.cs`.

## D5 — Trash: automatic purge after 30 days
**Decision:** everything soft-deleted (`IsDeleted = true`) is purged (hard delete) automatically
**30 days** after deletion, by a `BackgroundService` that runs periodically. The user can purge
earlier by hand from the trash, but cannot prevent the automatic purge.
**Context:** today `AuditableEntity` does not record *when* something was deleted, only the
`IsDeleted` flag; without that date there is no way to compute expiry. `DateTime? DeletedAt` is
added.
**Configurable retention:** `Trash:RetentionDays` (`appsettings.json`, default `30`) — not
hard-coded, so it can be tuned without recompiling.
**Scope:** applies to `PasswordEntry`, `SecureNote` and `Vault` (owner). A deleted vault drags its
entries/notes along: restoring the vault restores them; purging the vault purges them.
**Future evolution:** if the periodic purge does not scale (very large table), move it to a batch
job or to the database level (e.g. a SQL Agent job) instead of an in-process `BackgroundService`.
**Update (Sprint 23):** purging (manual or automatic) also deletes the attachment blobs from
`IAttachmentStorage`, after the DB delete commits. Before that the rows went away by cascade but
the encrypted files stayed on disk forever (`AUDIT.md` M2).

## D6 — JWT session: `HttpOnly` cookie instead of `localStorage`
**Decision:** the JWT travels in an `HttpOnly` cookie (`authToken`, `Secure`, `SameSite=Lax`,
`Path=/`) set by the server on `login`/`register`, instead of being returned in the body and stored
in `localStorage`. The client no longer reads or decodes the token: it uses `GET /api/auth/me` to
know whether it is signed in and who it is.
**Why:** `localStorage` is readable by any script — an XSS in a password manager compromises the
whole session. An `HttpOnly` cookie is not reachable from JS, not even with XSS.
**Why `SameSite=Lax` and not `Strict`:** it sends the cookie on normal navigations (clicking a
link, typing the URL) but blocks it on cross-site POST/PUT/DELETE, which is what matters for CSRF;
it does not break the flow of opening a link and staying signed in.
**Consequence — CORS:** the `AllowAll` policy (`AllowAnyOrigin+AllowAnyMethod+AllowAnyHeader`) was
removed: client and API live on the same origin (hosted model) and, with `SameSite=Lax` and no
`AllowCredentials`, a cross-origin request could not authenticate anyway.
**Consequence — standalone `TheShed.Client`:** the Client's own `launchSettings.json`
(`:5064`/`:7163`, a leftover from the hosted template) can no longer sign in when run against a
`TheShed.Server` on another port (the cookie does not travel cross-origin without CORS+credentials).
It is not used in the project's real flow (`TheShed.Server` is what runs), so it was left alone.
**Unchanged:** still an HMAC-SHA256 JWT with no refresh token (D2); only the transport channel
changes.
**Implementation:** `AuthController` (`Cookies.Append`/`Delete`, `GET /me`), `JwtBearerEvents.OnMessageReceived`
in `Program.cs` (falls back to the cookie when no `Authorization` header came in),
`JwtAuthenticationStateProvider` + `AuthService` (Client, no `ILocalStorageService`/`JwtParser`).

## D7 — Zero-knowledge encryption: adopt it, don't settle for "self-hosted, I trust my server"
**Decision:** migrate from the D3 model (a server key, the same for every user) to **real
zero-knowledge**: the master password key is derived on the client (Blazor WASM), and the server
just stores and serves opaque blobs it can never decrypt.
**Context:** triggered by finding C1 in `docs/AUDIT.md` (2026-08-13), which pointed out that D3
left this as a "future evolution" with no date and no criterion for when to address it. The
missing criterion was the real threat model: **who runs the server?** If it is always the same
user or someone trusted, the current model is an acceptable trade-off (audit, "Opportunities to
stand out #1"). It was confirmed (2026-08-14) that The Shed may end up running for third parties
unrelated to whoever hosts it — there the server operator **is** part of the threat model, the
same problem Bitwarden/1Password/Proton Pass solve. With that on the table, the current model is
not enough.
**Unchanged:** D1 (Argon2 is still the authentication hash — it is a different derivative of the
master password, not the encryption key) and D2 (JWT in an httpOnly cookie).
**Replaces:** D3 is now **superseded** — see the note on that entry. The original plan kept the
server key alive during the migration (Sprint 28), to decrypt existing data one last time.
**Adjustment (2026-08-27):** the vaults from before Sprint 26 were all test data, so it was decided
not to migrate them — they are discarded. Sprint 28 was reduced to switching off that encryption
surface (`IEncryptionService`/`EncryptionSettings`/`Encryption:Key`, with no real consumers since
Sprints 26/27) with no migration step.
**Explicitly accepted risk (not solved in this scope):** removing a member from a shared vault does
not rotate the vault key (Sprint 27, increment 2) — a former member who kept a copy of the key wrap
could in theory keep decrypting data written after their removal until key rotation exists (see M1
in `AUDIT.md`).
**Accepted UX cost:** without a recovery key (an optional increment in Sprint 28), forgetting the
master password means total, unrecoverable data loss — the same as Bitwarden/1Password's real
model, not an oversight of this project.
**Scope:** not a loose task, it is 4 sprints — see `SPRINTS.md` 25-28 (key derivation + per-user
keypair, a per-vault key, sharing through asymmetric key wrapping, migration of existing data).

## D8 — Strict CSP over WASM asset fingerprinting
**Decision:** the `Content-Security-Policy` keeps `script-src 'self' 'wasm-unsafe-eval'` without
`'unsafe-inline'`, and to make that possible WASM asset fingerprinting is switched off
(`<WasmFingerprintAssets>false</WasmFingerprintAssets>` in `TheShed.Client.csproj`).
**Context:** with fingerprinting on, Blazor resolves `_framework/dotnet.js` to its real (hashed)
name through an **inline** `<script type="importmap">`. A `script-src` without `'unsafe-inline'`
blocks that importmap, the translation never happens, Blazor requests the literal name and gets a
404: the app hangs on "Loading". It is not an edge case — it is the `script-src` Microsoft's own
documentation recommends for Blazor WebAssembly.
**Alternatives discarded:**
- `'unsafe-inline'` in `script-src` — it cancels exactly the protection the header exists for, and
  in a password manager XSS is the main client-side threat (the same reason as D6).
- An SRI hash or nonce on the importmap (what Microsoft recommends) — both assume a Razor
  `ImportMap` component to inject the attribute into. Here the importmap is generated by the static
  assets pipeline while serving a static `index.html`: there is no injection point without writing
  code that intercepts and rewrites the response.
**Accepted cost:** file-name cache busting on deploy is lost. `blazor.boot.json` still carries
content hashes, so the runtime revalidates anyway; the practical risk is a browser serving a stale
cached asset after a deploy.
**Way back (marked with `ponytail:` in the csproj):** compute the SHA-256 of the importmap rendered
per response and add it to `script-src` as an SRI hash. Only worth it if fingerprinting ever
matters.
**Known limitation:** `style-src` keeps `'unsafe-inline'` — 8 components use `style=""`
attributes. A much smaller surface than scripts; removing it means moving them to classes.

**Addendum (2026-08-31, Sprint 31 Increment 4):** `WasmFingerprintAssets=false` was not enough —
it covers the WASM payload but not `blazor.webassembly.js`, which has its own switch
(`BlazorFingerprintBlazorJs`, gated by `OverrideHtmlAssetPlaceholders` in the SDK targets). With
that switch at its default, `index.html` was left with the literal placeholder
`_framework/blazor.webassembly#[.{fingerprint}].js` and nothing to resolve it (this app does not
use `MapStaticAssets`), and the browser reads everything from the `#` on as a URL fragment — the
app hung on "Loading" in any real `dotnet publish -c Release`. Nobody had seen it because nobody
had published in Release since this decision was made: `dotnet run` resolves the placeholder some
other way (the Development static web assets pipeline) and hid it. Fix: an explicit
`BlazorFingerprintBlazorJs=false` + `index.html` pointing straight at the now-deterministic file
name, with no placeholder.

## D9 — Session lock: re-derive the key, don't persist it
**Decision:** on a page reload (F5, or the OS killing and relaunching an installed PWA), the
stretched master key and the decrypted keypair are **not stored in any browser storage**. The app
moves to a `Locked` screen that only asks for the master password again and re-derives the key (the
same path as login, with no server round-trip beyond the `GET /api/auth/me` that already existed).
**Context:** with zero-knowledge encryption (D7) the stretched master key only lives in memory
(`StretchedKeyStore`), but the JWT cookie (D6) survives the reload — the app showed as signed in
with every decryption path dead until a full logout/login. Sprint 31 (PWA) made it a blocker: an
installed PWA gets killed and relaunched by the OS all the time.
**Alternative discarded — persisting the key in `localStorage`/`sessionStorage`:** readable by any
XSS (the same reason as D6), and it does not solve the PWA case either — `sessionStorage` dies
with the process when the OS kills the installed app.
**Out of scope, not discarded — a non-extractable `CryptoKey` in IndexedDB:** Web Crypto can store
a key with `extractable: false`; an XSS could *use* it while the page is open but never read its
bytes. Materially better than `localStorage`, materially more work (the stretched key is a `byte[]`
passed to `interop.js` today, and would become an opaque handle everywhere). Only worth it if
typing the master password after every reload turns out to be annoying in real use — see "Out of
scope (P4)" in `docs/SPRINTS.md`.
**Implementation:** `Unlock.razor` + `AuthService.UnlockAsync` (verified by trial-decrypting
`EncryptedPrivateKey`, with no separate verifier — resending the login would have sent the master
password over the network again and spent the auth rate limit on an action far more frequent than
signing in). Alongside it: auto-lock after 15 min of inactivity and re-authentication (the same
password screen, without re-deriving) before revealing/copying a password — they close A2/A1 in
`AUDIT.md`. Full detail in `docs/SPRINTS.md` Sprint 30 (shipped).

## D10 — Antiforgery (A4): token issued by an endpoint, not a readable XSRF cookie

**Decision:** `AddAntiforgery()` with an `X-CSRF-TOKEN` header. An anonymous
`AntiforgeryController.GetToken` calls `IAntiforgery.GetAndStoreTokens` and returns the
`RequestToken` in the body (not in a JS-readable cookie). A middleware in `Program.cs`, after
`UseAuthorization` and before `MapControllers`, calls `IAntiforgery.ValidateRequestAsync` on every
method other than GET/HEAD/OPTIONS/TRACE and returns 400 if it fails. The WASM client fetches the
token once at startup (`CsrfHandler`, cached in memory) and resends it on every mutation — this
covers login/register too, not just the authenticated endpoints.

**Context:** `SameSite=Lax` on the JWT cookie (D6) already blocks most cross-site POST/PUT/DELETE,
but it is the only defense (A4 in `AUDIT.md`). With a pure WASM client there are no Razor Pages or
`<form>`s that generate the token automatically — an explicit endpoint is needed.

**Alternative discarded — a readable XSRF-TOKEN cookie + JS that reads and resends it:** it is the
"classic" SPA pattern (Angular does it out of the box), but it needs the client to read a
non-HttpOnly cookie through JS interop — one more surface (any XSS that reads cookies already reads
that one) for no extra value: here the session token is already HttpOnly (D6) and there is no Razor
in the middle that needs the readable cookie. Fetching it from an endpoint and keeping it in memory
avoids exposing anything new to the DOM.

**Implementation:** `AntiforgeryController` (server) + `CsrfHandler : DelegatingHandler` (client)
hooked once onto the shared `HttpClient` via `AddHttpClient` + `AddHttpMessageHandler` — the 9 typed
clients (`VaultClient`, `EntryClient`, etc.) don't change, they all go through the same handler
without knowing the token exists. The token endpoint is a GET, so its own middleware never blocks
it. Checked against the running server: POST without the header → 400, header without the matching
cookie → 400, a valid pair → passes antiforgery (reaches `AuthService`).

**Two real bugs that only showed up testing in the browser (not with curl, not in the unit
tests):**
1. The ASP.NET Core token is bound to the authenticated identity at the moment it was generated
   (`GetAndStoreTokens`) — a token fetched while anonymous stops validating as soon as the user logs
   in/registers, with the exact message `"The provided antiforgery token was meant for a
   different claims-based user than the current user"`. `CsrfHandler.Invalidate()` forces a refetch
   on every auth transition (login, register, logout) — see `AuthService`.
2. `CsrfHandler` was registered `AddScoped`, but `IHttpClientFactory` resolves
   `AddHttpMessageHandler<T>()` from its **own internal DI scope** (the one it uses for handler
   pooling, 2-minute lifetime) — a different scope from the one that hands the instance to
   `AuthService`. `Invalidate()` was clearing a ghost copy; the real HTTP pipeline kept serving the
   old token. It moved to `AddSingleton`: singletons are shared across scopes, so both consumers end
   up seeing the same instance. Neither bug was caught by the unit tests (they mock
   `IAuthService`/controllers directly, without going through the real `HttpClient` or
   `IHttpClientFactory`) — only the real flow in Chrome (register → create vault → logout → login)
   showed them.

Detail in `docs/SPRINTS.md` Sprint 32.

## D11 — Auth hash instead of the master password (N1) + master password change (N2)

**Decision:** the client derives the stretched key as before (`PBKDF2(password, KeySalt, 600k)`)
and sends `AuthHash.Compute(stretchedKey) = HMAC-SHA256(stretchedKey, "theshed-auth-hash")` as the
password on register/login. The server Argon2-hashes that value (D1 unchanged). Since login needs
the salt before deriving, `GET /api/auth/prelogin?email=` returns it.

**Context:** with D7 the server already stored `KeySalt` and `EncryptedPrivateKey`. Receiving the
raw password too meant `password + KeySalt → stretched key → private key → every vault key`: a DB
dump was useless, but an active compromise (RCE, a request-logging middleware, a curious operator)
read everything — exactly the threat D7 exists for. Same scheme as Bitwarden.

**Prelogin enumeration:** unknown emails get `HMAC(Jwt:Key, "prelogin-salt:" + email)[..16]` —
stable across calls and the same length as a real salt, so the endpoint answers identically for
registered and unregistered emails. Reuses the JWT key (domain-separated) instead of a new secret;
split it if the two ever need to rotate independently.

**Master password change:** two things are wrapped with the stretched key — the user's private
key, and the owner's wrap of every vault they own (`IVaultKeyService`; members' wraps use RSA and
don't move). The client re-wraps all of them (`IUserKeypairService.RewrapAsync`) — the vault keys
themselves and the entries never change — and sends the current and new auth hashes + new salt +
new private key + one new wrap per owned vault. The server verifies the current hash, requires
the wraps to match exactly the owned set (`GET /api/auth/owned-vault-keys`, **trashed vaults
included**, since they can be restored) — otherwise 409 and nothing is saved — and swaps
everything in one `SaveChanges`, re-issuing the cookie because the JWT carries `keySalt` as a
claim. The first version only re-wrapped the private key, on the wrong assumption that owned
vaults were RSA-wrapped too; the browser check caught it (`OperationError` unwrapping the vault
key after F5) — none of the unit or integration tests did, they never decrypt anything.

**Accepted limits:**
- No web app is zero-knowledge against a server that ships malicious JS. After D11 an attacker
  has to *modify* the client, not just *read* traffic.
- Other sessions used to survive a password change until their JWT expired, with a stale
  `keySalt`. Closed by D12 (Sprint 35): a password change now revokes every other session.
- No migration: pre-D11 accounts can't log in (test data, re-register).

Detail in PRs #32 and #33 (`git log`).

## D12 — Session revocation: `TokenVersion` checked on every request (N3)

**Decision:** `User.TokenVersion` (int) is copied into every JWT as the `tv` claim. The JWT
bearer's `OnTokenValidated` loads the user by id and rejects the token unless the user exists,
is active and has the same `TokenVersion`. Bumping the number revokes every token issued before:
`POST /api/auth/logout-all` ("Sign out everywhere" on `/account`) does it, and so does a master
password change, which then re-issues the caller's own cookie so only the *other* sessions fall.

**Context:** logout only deleted the cookie in the browser that called it (D6), so a stolen cookie
stayed valid for up to `Jwt:ExpiryMinutes` (60). After D11 it also left other sessions with a stale
`keySalt` claim after a password change.

**Alternatives discarded:**
- *Short access token + refresh token* (D2's "later"): still needs server state to revoke the
  refresh token, and it is a much bigger change to the auth flow.
- *Denylist of revoked `jti`s*: more state to store and clean up, and it can't express "every
  session of this user" without listing them all.

**Cost:** one primary-key lookup per authenticated request. Fine at this scale; if it ever shows
up in profiling, cache per user with a short TTL (the comment in `Program.cs` marks the spot).

**Client side:** `SessionExpiredHandler` turns any 401 (except `login`, where it means wrong
credentials, and `me`, the "am I signed in?" probe) into a full reload of `/login`, which also
wipes the in-memory keys. A revoked tab finds out on its next API call, not instantly.

**Migration:** `AddUserTokenVersion`. Tokens issued before it carry no `tv` claim and are
rejected, so every open session signs in once.
