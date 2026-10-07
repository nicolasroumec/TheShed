# The Shed — Plan de sprints

> Hoja de ruta de Fase 4 (Seguridad) → Fase 5 (UI). Cada sprint cierra en una rama
> con PR a `main`. Estado: 🟢 en curso · 🔵 pendiente · 🟣 futuro · ✅ hecho.
> Ver fases generales en `TODO.md` y decisiones en `DECISIONS.md`. Los sprints ya
> shippeados quedan colapsados al final como referencia — el detalle día a día de
> cada uno vive en `git log`, no acá.

## 🟣 Sprint 18 — 2FA (TOTP) · `feature/2fa`
> `User.TwoFactorSecret` (nullable, null = desactivado) ya existe en el modelo.
- [ ] Activar 2FA: generar secret TOTP + QR, verificar código antes de activar
- [ ] Exigir código TOTP en login si está activado (segundo paso del flujo de auth)
- [ ] Desactivar 2FA (reverificando)
- [ ] Tests del flujo TOTP (validación de código, ventana de tiempo)
- [ ] PR a `main`

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
| 23 | Purging an entry or a vault (manual or by expiry) also deletes its attachment blobs from disk (`AUDITORIA.md` M2). M3, B1 and Sprint 24 (TOTP for saved entries + HIBP) were dropped on 2026-08-31, see "Descartado" in `AUDITORIA.md` | `feature/session-hardening` | pending |
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
| 34 | The client sends an auth hash, never the master password (N1, prelogin salt) + master password change (N2), re-wrapping the private key and owned vault keys. The owned-vault fix came after the merge. See D11 | `feature/auth-hash` + `fix/owned-vault-keys-rewrap` | #32, #33 |
| 35 | Clipboard auto-clear after 30 s (N4) + sign out everywhere (N3): `User.TokenVersion` checked on every request, also bumped by a password change; any 401 sends the app to `/login`. See D12 | `feature/session-hardening` | pending |

**Nota:** el Sprint 19 (`feature/session-timeout` — auto-logout + reautenticación) nunca se
empezó; su alcance completo terminó cubierto por el Sprint 30 con un diseño mejor (lock/unlock
en vez de logout duro). Se elimina de la lista de pendientes, no aporta nada por separado.
