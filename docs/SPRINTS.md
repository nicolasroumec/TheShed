# The Shed — Plan de sprints

> Hoja de ruta de Fase 4 (Seguridad) → Fase 5 (UI). Cada sprint cierra en una rama
> con PR a `main`. Estado: 🟢 en curso · 🔵 pendiente · 🟣 futuro · ✅ hecho.
> Ver fases generales en `TODO.md` y decisiones en `DECISIONS.md`. Los sprints ya
> shippeados quedan colapsados al final como referencia — el detalle día a día de
> cada uno vive en `git log`, no acá.

## 🟢 Sprint 34 — Auth hash + change master password · `feature/auth-hash`
> N1 + N2 in `FEATURES-ROADMAP.md`; design in `DECISIONS.md` D11. Until now the server received the
> raw master password on login/register — with `KeySalt` and `EncryptedPrivateKey` already in the
> database, an *active* server compromise (RCE, a body-logging middleware) was enough to decrypt
> everything. Same branch as N2 because both touch the same flow.

### Increment 1 — Prelogin ✅
- [x] `GET /api/auth/prelogin?email=` returns the account's `KeySalt`; for unknown emails a fake
      salt, `HMAC(Jwt:Key, "prelogin-salt:" + email)[..16]` — stable and the same length as a
      real one, so it doesn't enumerate accounts. Rate-limited with the `Auth` policy

### Increment 2 — Auth hash on login/register ✅
- [x] `AuthHash.Compute(stretchedKey) = HMAC-SHA256(stretchedKey, "theshed-auth-hash")` is what
      goes on the wire as `Password`; the server Argon2-hashes it unchanged (D1 still holds)
- [x] Client tests: the master password never appears in the request body

### Increment 3 — Change master password, server ✅
- [x] `POST /api/auth/change-password` (`[Authorize]`, `Auth` rate limit): verifies the current
      auth hash, stores the new hash + `KeySalt` + `EncryptedPrivateKey`, re-issues the cookie
      (the old JWT carries the old `keySalt` claim). Wrong current password → 400, not 401
- [x] Unit tests (`AuthServiceTests`) + integration round trip (`AuthFlowTests`): `/me` returns
      the new salt, the old password gets 401, the new one logs in

### Increment 4 — Change master password, client ✅
- [x] `IUserKeypairService.RewrapPrivateKeyAsync`: decrypt with the old stretched key, re-encrypt
      with the new one, base64 straight through JS — `IAesGcmService` round-trips through UTF-8
      and would corrupt the binary PKCS#8 key
- [x] `AuthService.ChangePasswordAsync`: a wrong current password fails the AES-GCM tag
      client-side, before any request. Vault keys are untouched; the session stays unlocked
- [x] `/account` page (code-behind) + nav link
- [x] 253/253 tests green

### Increment 5 — PR
- [ ] Browser check: register → entry → change password → F5 → unlock with the new one →
      logout → login
- [ ] PR to `main`

**Migration:** none. Accounts created before Increment 2 stored `Argon2(raw password)` and can't
log in anymore — test data only, re-register (same call as Sprint 28).
**Known gap:** other open sessions keep working until their JWT expires, with a stale `keySalt`
(unlock fails there until re-login). Closed by N3 (sign out everywhere).

## 🔵 Sprint 35 — Clipboard auto-clear + sign out everywhere · `feature/session-hardening`
> N4 + N3 in `FEATURES-ROADMAP.md`.
- [ ] N4: clear the clipboard after 30 s in `interop.js`, only if it still holds what we copied
- [ ] N3: `User.TokenVersion` column, emitted as a claim, checked in `OnTokenValidated`;
      incremented by "sign out everywhere" and by a master password change
- [ ] Tests + PR to `main`

## 🟣 Sprint 18 — 2FA (TOTP) · `feature/2fa`
> `User.TwoFactorSecret` (nullable, null = desactivado) ya existe en el modelo.
- [ ] Activar 2FA: generar secret TOTP + QR, verificar código antes de activar
- [ ] Exigir código TOTP en login si está activado (segundo paso del flujo de auth)
- [ ] Desactivar 2FA (reverificando)
- [ ] Tests del flujo TOTP (validación de código, ventana de tiempo)
- [ ] PR a `main`

## 🟣 Sprint 23 — Adjuntos huérfanos · `feature/minor-hardening`
> `docs/AUDITORIA.md` M2. (M3, B1 y el Sprint 24 — TOTP en entradas guardadas + alertas
> HIBP — se sacaron del roadmap el 2026-08-31: valor bajo/dudoso para el tamaño de este
> proyecto, ver el "Descartado" al final de `AUDITORIA.md`.)

- [ ] `TrashService` (purga en cascada) llama `IAttachmentStorage.DeleteAsync` por cada
      `Attachment` de la entry purgada — cierra el `// ponytail:` ya marcado en
      `TrashService.cs:172-174` (adjuntos huérfanos en disco, no fuga de datos pero
      acumulación sin límite)
- [ ] Tests + PR a `main`

## 🔵 Transversal — Traducir a inglés · `feature/i18n-english`
> **Hacerla pronto** (no bloquea features pero la deuda crece con cada sprint). CLAUDE.md
> exige inglés en código/comentarios/docs. Pasar `docs/*.md` y los comentarios viejos de
> auth/encryption a inglés. Rama independiente, mergeable en cualquier momento.

> **Fuera de scope (post-roadmap):** refresh tokens (D2), app móvil nativa, extensión de
> navegador, lectura de vaults offline. (El modelo zero-knowledge salió de esta lista: se hizo
> en los Sprints 25-28. La app instalable pasó al Sprint 31 — no cubre offline de datos.)

---

## ✅ Shipped (referencia — detalle en `git log` / PRs)

| # | Sprint | Rama | PR |
|---|--------|------|----|
| 1 | Cifrado AES-256 de entradas | `feature/encryption-aes` | #2 |
| 2+3+3.5 | Auth: DTOs, infra JWT, endpoints register/login, tests | `feature/jwt-auth` | #3 |
| 4 | API de entradas (CRUD `PasswordEntry`) | `feature/entries-api` | #4 |
| 5 | UI: auth + tema | `feature/client-auth` + `feature/ui-theme` | #5, #6 |
| 6 | Vaults API (CRUD + membresías) | `feature/vaults-api` | #7 |
| 7 | UI: vaults + entradas (cliente WASM) | `feature/vaults-ui` | #8 |
| 8 | Compartir vaults (members) | `feature/vault-sharing` | #9 |
| 9 | Notas seguras (CRUD) | `feature/secure-notes` | #10 |
| 10 | Tags | `feature/tags` + `feature/tags-ui` | #11 |
| 11 | UX de entradas: favoritos + búsqueda + copiar | `feature/entry-ux` | #12 |
| 12 | Historial de versiones | `feature/entry-history` | #13 |
| 13 | Papelera / recuperar (purga automática a 30 días) | `feature/trash` | — |
| 14 | Adjuntos | `feature/attachments` | #14 |
| 15 | Salud de contraseñas (versión server-side original — reemplazada client-side en el 27) | `feature/password-health` | #15 |
| 16 | Cookie `httpOnly` para el JWT (D6) | `feature/jwt-cookie` | #16 |
| 17 | Importar/exportar CSV (LastPass/Bitwarden/1Password), client-side sobre el modelo zero-knowledge | `feature/import-export` | #28 |
| 20 | Refactor de frontend: identidad "Workshop", responsive mobile-first, tipografía, split de `VaultDetail`. Detalle de diseño absorbido en `docs/UI.md` | `feature/frontend` | #17 |
| 21+22 | Hardening: rate limiting (A3) + tope de longitud de password (B2) + CSP/HSTS (M5, ver D8). A4 (antiforgery) quedó deliberadamente afuera, cerrado en el Sprint 32 | `feature/security-hardening` | #20 |
| 25 | Zero-knowledge: derivación de clave (PBKDF2 vía Web Crypto) + keypair RSA por usuario (D7) | `feature/e2e-key-derivation` | #21 |
| 26 | Zero-knowledge: vault key + cifrado de entradas/notas client-side | `feature/e2e-vault-encryption` | #23 |
| 27 | Zero-knowledge: compartir vaults (key wrapping RSA-OAEP). Remover miembro no rota la vault key — riesgo aceptado, ver D7 | `feature/e2e-vault-sharing` | #24 |
| 28 | Zero-knowledge: baja del cifrado legacy del servidor (alcance recortado — datos previos al 26 eran de prueba, no se migraron) | `feature/e2e-migration` | #25 |
| 29 | Generador: modo memorable (passphrase) | `feature/password-generator-passphrase` | #22 |
| 30 | Session lock: unlock screen + auto-lock por inactividad (A2) + reautenticación para reveal/copy (A1). Ver D9 | `feature/session-lock` | #26 |
| 31 | PWA instalable: manifest + íconos, service worker, banner offline. No cubre lectura offline de datos | `feature/pwa` | #27 |
| 32 | Antiforgery (A4): token from an endpoint + `CsrfHandler`. See D10 | `feature/antiforgery` | #29 |
| 33 | Integration tests: `WebApplicationFactory` + SQLite in-memory (auth, antiforgery, cross-user vault access) | `feature/integration-tests` | #30 |
| — | `Login`, `Register`, `RedirectToLogin` moved to code-behind | `feature/razor-code-behind` | #31 |

**Nota:** el Sprint 19 (`feature/session-timeout` — auto-logout + reautenticación) nunca se
empezó; su alcance completo terminó cubierto por el Sprint 30 con un diseño mejor (lock/unlock
en vez de logout duro). Se elimina de la lista de pendientes, no aporta nada por separado.
