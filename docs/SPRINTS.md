# The Shed — Plan de sprints

> Hoja de ruta de Fase 4 (Seguridad) → Fase 5 (UI). Cada sprint cierra en una rama
> con PR a `main`. Estado: 🟢 en curso · 🔵 pendiente · 🟣 futuro · ✅ hecho.
> Ver fases generales en `TODO.md` y decisiones en `DECISIONS.md`. Los sprints ya
> shippeados quedan colapsados al final como referencia — el detalle día a día de
> cada uno vive en `git log`, no acá.

## 🔵 Sprint 17 — Importar / Exportar (CSV) · `feature/import-export`
> Reescrito 2026-09-01 sobre el modelo zero-knowledge (D7, Sprints 25-28): todo el
> cifrado corre client-side con la vault key vía Web Crypto, mismo patrón que
> `EntriesPanel.SubmitAsync`/`LoadEntriesAsync` (`IVaultKeyCache.Get(VaultId)` +
> `IAesGcmService.EncryptAsync/DecryptAsync`). No hay endpoint de servidor nuevo: import
> reusa `POST /api/entries` fila por fila, export no pega al servidor más que el `GET`
> que ya hace `EntriesPanel`.
>
> CSV de LastPass / Bitwarden / 1Password (import) y export de entradas propias. Ningún
> proyecto tiene hoy una lib de CSV — un parser a mano que solo hace `Split(',')` rompe con
> comillas/comas/saltos de línea embebidos en `notes` o `password`, justo el campo más
> sensible. Se suma `CsvHelper` (NuGet, la lib estándar de facto en .NET) en vez de
> reinventar el parseo: es la excepción lazy correcta acá — un parser CSV RFC4180 a mano
> es más código y más riesgo que una dependencia madura de un solo propósito.

### Increment 1 — DTOs + dependencia
- [ ] `PackageReference CsvHelper` en `TheShed.Client.csproj` (solo Client — el parseo
      corre en el browser, junto al cifrado; el servidor no toca CSV)
- [ ] DTOs en `TheShed.Shared/Models/DTOs/Entries/`: `ImportResult` (Imported/Skipped +
      `List<ImportRowError>` con número de fila y motivo) y `ExportEntryRow` (Name,
      Username, Password, Url, Notes — mismas columnas que expone el export)

### Increment 2 — Import (client-side)
- [ ] Mapper de columnas por formato (LastPass: `url,username,password,extra,name,
      grouping,fav`; Bitwarden: `folder,favorite,type,name,notes,fields,reprompt,
      login_uri,login_username,login_password,login_totp`; 1Password:
      `Title,Website,Username,Password,Notes`) → por fila, cifra con
      `IAesGcmService.EncryptAsync(vaultKey, ...)` (vaultKey de
      `IVaultKeyCache.Get(VaultId)`, mismo guard de `MissingVaultKeyError` que
      `EntriesPanel`) y sube ya cifrada vía `EntryClient.CreateAsync` — el servidor
      sigue sin ver texto plano
- [ ] Fila inválida (columnas faltantes, vacía) no aborta el import completo: se cuenta en
      `ImportResult` con el número de fila, se sigue con las demás

### Increment 3 — Export (client-side)
- [ ] Trae las entradas del vault (`EntryClient.ListAsync`, igual que
      `EntriesPanel.LoadEntriesAsync`), descifra en memoria con
      `IAesGcmService.DecryptAsync(vaultKey, ...)`, arma el CSV con `CsvHelper` y dispara
      la descarga desde el navegador (`Blob`/`URL.createObjectURL` vía JS interop) — sin
      endpoint de servidor nuevo

### Increment 4 — UI
- [ ] `ImportExportClient` (wrapper delgado sobre `CsvHelper` + los pasos de arriba) +
      sección en `/vaults/{id}` (o página propia `/import-export`): input de archivo +
      selector de formato, preview de `ImportResult` tras subir (importadas/saltadas/
      errores por fila); botón "Export" con confirmación propia (`ModalService`, no
      `window.confirm` — ver Sprint 20) avisando que el archivo tendrá las contraseñas en
      texto plano, antes de disparar la descarga

### Increment 5 — Tests
- [ ] Tests de import/export (los 3 formatos, fila con columnas faltantes, fila vacía,
      roundtrip cifrado/descifrado)
- [ ] PR a `main`

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
| 20 | Refactor de frontend: identidad "Workshop", responsive mobile-first, tipografía, split de `VaultDetail`. Detalle de diseño absorbido en `docs/UI.md` | `feature/frontend` | #17 |
| 21+22 | Hardening: rate limiting (A3) + tope de longitud de password (B2) + CSP/HSTS (M5, ver D8). A4 (antiforgery) quedó deliberadamente afuera — sigue abierto | `feature/security-hardening` | #20 |
| 25 | Zero-knowledge: derivación de clave (PBKDF2 vía Web Crypto) + keypair RSA por usuario (D7) | `feature/e2e-key-derivation` | #21 |
| 26 | Zero-knowledge: vault key + cifrado de entradas/notas client-side | `feature/e2e-vault-encryption` | #23 |
| 27 | Zero-knowledge: compartir vaults (key wrapping RSA-OAEP). Remover miembro no rota la vault key — riesgo aceptado, ver D7 | `feature/e2e-vault-sharing` | #24 |
| 28 | Zero-knowledge: baja del cifrado legacy del servidor (alcance recortado — datos previos al 26 eran de prueba, no se migraron) | `feature/e2e-migration` | #25 |
| 29 | Generador: modo memorable (passphrase) | `feature/password-generator-passphrase` | #22 |
| 30 | Session lock: unlock screen + auto-lock por inactividad (A2) + reautenticación para reveal/copy (A1). Ver D9 | `feature/session-lock` | #26 |
| 31 | PWA instalable: manifest + íconos, service worker, banner offline. No cubre lectura offline de datos | `feature/pwa` | #27 |

**Nota:** el Sprint 19 (`feature/session-timeout` — auto-logout + reautenticación) nunca se
empezó; su alcance completo terminó cubierto por el Sprint 30 con un diseño mejor (lock/unlock
en vez de logout duro). Se elimina de la lista de pendientes, no aporta nada por separado.
