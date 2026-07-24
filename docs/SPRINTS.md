# The Shed — Plan de sprints

> Hoja de ruta de Fase 4 (Seguridad) → Fase 5 (UI). Cada sprint cierra en una rama
> con PR a `main`. Estado: 🟢 en curso · 🔵 pendiente · 🟣 futuro · ✅ hecho.
> Ver fases generales en `TODO.md` y decisiones en `DECISIONS.md`.

## 🟢 Sprint 1 — Cifrado AES-256 de entradas · `feature/encryption-aes`
- [x] Servicio `AesEncryptionService` (AES-256-GCM) + DI + config + `appsettings.Example`
      → commit `feat: add AES-256 encryption service for entries`
- [x] Tests del servicio (round-trip, nonce aleatorio, manipulación, validaciones)
      → commit `test: add encryption service tests`
- [x] Docs: `DECISIONS.md` (D3) + este plan → commit `docs: document entry encryption + sprint plan`
- [x] PR a `main`

> Nota: el cifrado queda **listo pero sin consumir** hasta el Sprint 4 (aún no existe
> la API de entradas). Por eso el sprint cierra con tests + docs, lo verificable ahora.

## ✅ Sprint 2 — Auth: DTOs + infraestructura JWT · `feature/jwt-auth`
*(Incrementos 2-3 de `AUTH_FLOW.md`)*
- [x] DTOs `Register/Login/AuthResponse` → commit `feat: add authentication DTOs`
- [x] `JwtSettings` + `JwtTokenService` + wiring en `Program.cs`
      → commit `feat: configure JWT authentication`

> Nota: se unificaron los nombres de config a `Jwt:Key` / `ExpiryMinutes` (antes el
> `appsettings.Example` usaba `SecretKey` / `ExpirationMinutes`). La clave va por User
> Secrets; Issuer/Audience/ExpiryMinutes en `appsettings.json`.

## ✅ Sprint 3 — Auth: endpoints register/login · `feature/jwt-auth`
*(Incremento 4 de `AUTH_FLOW.md`)*
- [x] `AuthService` + `AuthController` + DI → commit `feat: add register and login endpoints`
- [x] Fix de esquema: caminos de cascada múltiples en SQL Server (VaultMember,
      EntryHistory, PasswordEntryTag → `Restrict`), migración `InitialCreate` regenerada
      → commit `fix: avoid multiple cascade paths in SQL Server schema`
- [x] Verificación e2e ✅ (register 201/409, login 200/401, claims `sub/email/username/exp` OK)
- [x] PR a `main` (#3)

## ✅ Sprint 3.5 — Tests de auth · `feature/jwt-auth`
- [x] Tests de `AuthService` (register: alta + email duplicado; login: ok, password mala,
      email inexistente, usuario inactivo) — InMemory DB + fakes de hasher/JWT
- [x] Tests de `AuthController` (register 201/409, login 200/401) — fake de `IAuthService`
      → commit `test: add auth service and controller tests`
- [x] Suite completa en verde (19/19)

## ✅ Sprint 4 — API de entradas (CRUD `PasswordEntry`) · `feature/entries-api`
- [x] DTOs `EntryCreateRequest/EntryUpdateRequest/EntryResponse/EntryListItem`
      → commit `feat: add password entry DTOs`
- [x] `IVaultAccessService` (Owner/Editor → Write, Viewer → Read, resto → None)
      → commit `feat: add vault access service`
- [x] `PasswordEntryService` + `EntriesController` `[Authorize]` que **consume `IEncryptionService`**
      (cifra al crear/editar, descifra al leer) — cierra el loop del cifrado
      → commit `feat: add password entries CRUD API`
- [x] Autorización por vault/membresía; listado sin contraseñas, reveal una a una en `GET /{id}`;
      sin acceso → 404 (no revela existencia)
- [x] Tests de `PasswordEntryService` + `EntriesController` (suite completa 36/36)
- [x] PR a `main` (#4)

> Decisión de diseño: el listado devuelve solo metadata; la contraseña descifrada se entrega
> únicamente en `GET /api/entries/{id}` (estilo Bitwarden/1Password).

## ✅ Sprint 5 — UI: auth + tema · `feature/client-auth` + `feature/ui-theme`
- [x] `AuthService` + `JwtAuthenticationStateProvider` + `LocalStorageService`, guards de ruta
      (`AuthorizeRouteView` + `RedirectToLogin`), páginas Login/Register
      → commit `feat: add client auth (login/register, JWT state, route guards)`
- [x] Tema oscuro + app shell → commit `feat: add dark theme and app shell styling`
- [x] Paleta Workshop + design tokens → commit `feat: apply Workshop palette and design tokens`
- [x] PR a `main` (#5 client-auth + #6 ui-theme)

## ✅ Sprint 6 — Vaults API (CRUD `Vault` + membresías) · `feature/vaults-api`
> Prerequisito de la UI de vaults: hoy `EntriesController` recibe un `vaultId` pero
> no hay forma de listar/crear vaults. Reusa `IVaultAccessService` (Sprint 4).
- [x] DTOs `VaultCreateRequest/VaultUpdateRequest/VaultResponse/VaultListItem`
      (responses exponen `IsOwner`/`CanWrite`, no `Role`: el dueño no es un `VaultMember`)
      → commit `feat: add vault DTOs`
- [x] `VaultService` + `VaultsController` `[Authorize]` (list propios/compartidos, get, create,
      update, delete soft). El dueño se rastrea por `Vault.OwnerId`, **no** se crea un
      `VaultMember` al crear (alineado con `VaultAccessService`)
      → commit `feat: add vaults CRUD API`
- [x] Autorización: get/list por acceso (sin acceso → 404, oculta existencia);
      rename/delete **owner-only** (no-dueño con acceso → 403)
- [x] Tests de `VaultService` + `VaultsController` (suite completa 52/52)
      → commit `test: add vault service and controller tests`
- [x] PR a `main` (#7)

## ✅ Sprint 7 — UI: vaults + entradas (cliente WASM) · `feature/vaults-ui`
> Consume las APIs de Sprint 4 (entries) y 6 (vaults). El `HttpClient` ya manda el
> Bearer (lo setea `JwtAuthenticationStateProvider`).
- [x] Increment 1 — `VaultClient` + página `/vaults` (listar/crear), link en el nav
      → commit `feat: add vaults list and create UI`
- [x] Increment 2 — `EntryClient` + `VaultClient.GetAsync` + página `/vaults/{id}`:
      listar entradas, revelar contraseña una a una (GET `/{id}`)
      → commit `feat: add vault detail with entries list`
- [x] Increment 3 — alta/edición/borrado de entradas (acciones gated por `CanWrite`,
      confirmación al borrar) + `EntryClient` Create/Update/Delete
      → commit `feat: add entry create/edit/delete UI`
- [x] Increment 4 — `PasswordGenerator` (Shared, RNG seguro, +tests) integrado al form
      de entradas (longitud/símbolos, botón Generate, toggle Show)
      → commit `feat: add password generator`
- [x] PR a `main` (#8)

## 🟢 Sprint 8 — Compartir vaults (members) · `feature/vault-sharing`
> Cierra el feature de vaults compartidos: el dueño invita a otros usuarios por email
> y les asigna rol. Reusa `IVaultAccessService` (Sprint 4) para resolver el acceso de
> los miembros; el dueño se rastrea por `Vault.OwnerId`, **no** es un `VaultMember`.
- [x] DTOs `VaultMemberAddRequest` (email + rol), `VaultMemberItem` (userId/username/email/rol),
      `VaultMemberRoleUpdateRequest` → commit `feat: add vault member DTOs`
- [x] `VaultService` (Add/UpdateRole/Remove/ListMembers) + endpoints en `VaultsController`
      bajo `/api/vaults/{id}/members[/{memberUserId}]` → commit `feat: add vault member management to
      VaultService and API endpoints`
- [x] UI: panel de members en `/vaults/{id}` (listar/invitar/cambiar rol/quitar) + `VaultClient`
      → commit `feat: add vault members UI`
- [x] Tests de `VaultService` + `VaultsController` (members) — suite completa **67/67** en verde
- [ ] PR a `main`

### Decisiones de diseño (Sprint 8)
- **Gestión owner-only:** listar/invitar/cambiar rol/quitar son exclusivos del dueño. Sin acceso
  al vault → 404 (oculta existencia); con acceso pero no dueño (un member) → 403. Gate compartido
  `OwnerOnlyAsync` (mismo que rename/delete del Sprint 6).
- **Roles:** `VaultRole` = `Viewer` (lectura) | `Editor` (lectura+escritura). Mapea a
  `VaultAccess` vía `IVaultAccessService`; el listado de vaults expone `CanWrite = Role == Editor`.
- **Invitar por email:** el target se resuelve por email exacto. Email inexistente → 404
  (`UserNotFound`); ya es member o es el propio dueño → 409 (`AlreadyMember`).
- **`EntryError` reutilizado:** se sumaron `UserNotFound`/`AlreadyMember`. Sigue marcado con
  `ponytail:` para renombrar a `ServiceError` cuando crezca un consumidor más.

---

# Backlog — sprints planificados (Sprint 9 → fin)

> Orden por dependencia + valor: primero lo que reusa el patrón vault/entry ya engrasado,
> después lo que toca infra de soft-delete, luego storage y por último extras de seguridad.
> Cada sprint = una rama con PR a `main`. Todas las entidades (`SecureNote`, `Tag`,
> `EntryHistory`, `Attachment`) **ya existen** en el modelo; falta API + UI.

## ✅ Sprint 9 — Notas seguras (CRUD) · `feature/secure-notes`
> Reusa todo el patrón de entries: `IVaultAccessService` para permisos, `IEncryptionService`
> para cifrar `SecureNote.ContentEncrypted` (mismo formato AES-GCM que las entradas).
> **Sin migración:** la entidad/tabla `SecureNote` ya existe; solo falta API + UI.

### Increment 1 — DTOs · `TheShed.Shared/Models/DTOs/Notes/`
- [x] `NoteCreateRequest` (VaultId, Title [Req, Max 200], Content [Req], IsFavorite)
- [x] `NoteUpdateRequest` (Title, Content, IsFavorite — sin VaultId, como `EntryUpdateRequest`)
- [x] `NoteResponse` (Id, VaultId, Title, Content descifrado, IsFavorite, CreatedAt, UpdatedAt)
- [x] `NoteListItem` (Id, Title, IsFavorite — **sin contenido**)
      → commit `feat: add secure note DTOs`

### Increment 2 — Service + API
- [x] `ISecureNoteService` + `SecureNoteService` (espejo de `PasswordEntryService`, cifra
      `ContentEncrypted`; reusa `EntryResult<T>`/`EntryError`/`IVaultAccessService`)
- [x] `NotesController` `[Authorize]` ruta `api/notes` (List?vaultId, Get{id}, Post, Put{id},
      Delete{id}); DI `AddScoped<ISecureNoteService, SecureNoteService>` en `Server/Program.cs`
      → commit `feat: add secure notes CRUD API`

### Increment 3 — UI
- [x] `NoteClient` (espejo de `EntryClient`) + DI en `Client/Program.cs`
- [x] Sección "Secure notes" en `VaultDetail.razor` (debajo de entradas): listar, revelar
      contenido una a una, alta/edición/borrado gated por `CanWrite`, confirmar al borrar
      → commit `feat: add secure notes UI`

### Increment 4 — Tests
- [x] `SecureNoteServiceTests` + `NotesControllerTests` (espejo de los de entries) — suite
      completa **83/83** en verde → commit `test: add secure notes service and controller tests`
- [ ] PR a `main`

### Decisiones de diseño (Sprint 9)
- **Contenido = secreto:** `Content` se cifra con el mismo AES que las contraseñas y se devuelve
  descifrado solo en `GET /{id}`; el listado lleva solo metadata (Title/IsFavorite).
- **Duplicación aceptada:** `SecureNoteService` es ~90% copia de `PasswordEntryService`; con 2
  consumidores de formas distintas no se abstrae aún (comentario `ponytail:`). Igual `MapError`/
  `CurrentUserId`, ya en su 3ª copia en controllers: se deja, sube a un base controller si crece.
- **UI:** sección aparte en la misma página `/vaults/{id}`, no tab ni página nueva.

## ✅ Sprint 10 — Tags (categorizar entradas) · `feature/tags` + `feature/tags-ui`
> `Tag` es **por usuario** (`Tag.UserId`); se asignan a entradas vía join `PasswordEntryTag`.
- [x] DTOs + `TagService` + `TagsController` (CRUD de tags del usuario)
      → commit `feat: add tags CRUD API and entry assignment.`
- [x] Asignar/quitar tags a una entrada (`PUT/DELETE api/entries/{id}/tags/{tagId}`, idempotente);
      tags incluidos en `EntryResponse` y `EntryListItem`, filtro opcional `?tagId` en el listado
- [x] UI: `TagClient` + en `/vaults/{id}` filtro por tag, gestión (crear/borrar) y asignación en el
      form de edición, badges de tags en el listado → commit `feat: add tags UI (filter, manage, assign to entries)`
- [x] Tests de `TagService` + `TagsController` (suite completa **97/97** en verde)
      → commit `test: add tag service and controller tests`
- [x] PR a `main`

### Decisiones de diseño (Sprint 10)
- **Backend directo en `main`:** el CRUD + asignación se mergeó en `main` (commit `d63b7ef`); la UI y
  los tests van en `feature/tags-ui` sobre esa base.
- **Asignación solo al editar:** en la UI el toggle de tags aparece solo editando una entrada ya
  persistida (necesita `entryId`); una entrada nueva se crea primero y se etiqueta al editarla.
- **Borrar tag = soft-delete:** deja huérfanas las filas `PasswordEntryTag`, que dejan de resolver por
  el global query filter (comentario `ponytail:` en `TagService`). Purgar el join si se acumula.
- **UI sin rename:** `TagClient` solo crea/borra/lista aunque la API soporte `PUT` (comentario `ponytail:`).

## ✅ Sprint 11 — UX de entradas: favoritos + búsqueda + copiar · `feature/entry-ux`
> Solo `PasswordEntry` por ahora (`SecureNote` tiene `IsFavorite` también pero queda para un sprint
> posterior). `EntryListItem` no expone la contraseña, así que el toggle de favorito no puede
> reusar `UpdateAsync` (pide el payload completo); se agrega un endpoint idempotente propio,
> mismo patrón que la asignación de tags (`PUT/DELETE .../tags/{tagId}`).

### Increment 1 — Backend: favorito
- [x] Extraer `LoadEntryForAccessAsync(entryId, requireWrite)` en `PasswordEntryService` (el
      bloque `FirstOrDefault → GetAccessAsync → None/Forbidden` se repetía 5 veces; con
      `SetFavoriteAsync` sería la 6ª) y reusarlo en Get/Update/Delete/AddTag/RemoveTag/SetFavorite
- [x] `PasswordEntryService.SetFavoriteAsync` + rutas `PUT/DELETE /api/entries/{id}/favorite`
      (mismo molde idempotente que `AddTag`/`RemoveTag`)
      → commit `refactor: extract entry access-check helper and add favorite toggle endpoint`

### Increment 2 — Backend: búsqueda + orden
- [x] `ListAsync` suma `search` (contains sobre Name/Username/Url, case-insensitive) + orden
      default `IsFavorite desc, Name asc`; `EntriesController` con `[FromQuery] string? search`
      → commit `feat: add search filter and favorite-first ordering to entries list`

### Increment 3 — UI: favorito + búsqueda
- [x] `EntryClient.SetFavoriteAsync` + `ListAsync(..., search)`; input de búsqueda y botón
      estrella por fila en `VaultDetail.razor` (mismo `btn-group` que Reveal/Hide)
      → commit `feat: add favorite toggle and search UI to vault detail`

### Increment 4 — UI: copiar al clipboard
- [x] `wwwroot/js/interop.js` (`navigator.clipboard.writeText`) + botón "Copy" que llama
      `GetAsync` y copia sin activar el Reveal
      → commit `feat: add copy-to-clipboard for entry passwords`

### Increment 5 — Tests
- [x] Tests de `PasswordEntryService` (favoritos-primero, búsqueda por nombre/username/url,
      `SetFavoriteAsync` con sus casos de autorización) + `EntriesController` (favorite/unfavorite) —
      suite completa **106/106** en verde. El copy-to-clipboard es JS interop puro (sin lógica del
      lado servidor) y se verificó manualmente en navegador, no tiene test automatizado
      → commit `test: add tests for entry favorites and search filtering`
- [x] Verificado e2e en navegador: orden favoritos-primero, toggle ★, búsqueda por
      nombre/username/URL, copy-to-clipboard sin revelar la contraseña en pantalla — sin errores
      de consola
- [ ] PR a `main`

## 🔵 Sprint 12 — Historial de versiones · `feature/entry-history`
> `EntryHistory` versiona solo la **contraseña** (`PasswordEncrypted` + `ChangedByUserId`).
- [ ] Al actualizar una entrada, snapshot de la contraseña anterior en `EntryHistory`
      (en `PasswordEntryService.UpdateAsync`)
- [ ] `GET /api/entries/{id}/history` (descifra versiones para mostrar) — gated por acceso
- [ ] UI: ver historial de una entrada, revelar versión vieja una a una
- [ ] Tests (snapshot al editar, autorización)
- [ ] PR a `main`

## 🔵 Sprint 13 — Papelera / recuperar · `feature/trash`
> Reusa el soft-delete (`AuditableEntity.IsDeleted` + global query filter). Hoy borrar = ocultar.
- [ ] Listar borrados (bypass del filtro con `IgnoreQueryFilters`), restaurar y purgar definitivo
      — entradas y notas (y vaults para el dueño)
- [ ] Endpoints `GET /trash`, `POST /{id}/restore`, `DELETE /{id}/purge`; autorización por acceso
- [ ] UI: vista de papelera con restaurar / borrar definitivo (con confirmación)
- [ ] Tests
- [ ] PR a `main`

## 🔵 Sprint 14 — Adjuntos · `feature/attachments`
> `Attachment` guarda `StoragePath` + `FileSizeBytes` → el archivo va **fuera de la DB**
> (filesystem local en dev). Decidir límite de tamaño y si se cifra el blob.
- [ ] Upload/download/delete de adjuntos de una entrada; validar tamaño/tipo en el boundary
- [ ] `AttachmentService` + controller; storage local (carpeta configurable) detrás de una interfaz
      mínima por si después se va a blob storage
- [ ] UI: adjuntar/descargar/quitar en el detalle de entrada (gated por `CanWrite`)
- [ ] Tests (validación de límites, autorización)
- [ ] PR a `main`
> ⚠️ Decisión pendiente: ¿cifrar el contenido del adjunto con AES como las contraseñas? (recomendado)

## 🔵 Sprint 15 — Salud de contraseñas · `feature/password-health`
> Detectores de débiles y repetidas. Reusa `PasswordGenerator`/criterios del Shared.
- [ ] Detector de débiles (longitud/variedad/entropía simple) — Shared, con tests
- [ ] Detector de repetidas entre entradas del usuario (comparar descifradas en memoria, nunca log)
- [ ] UI: panel/badges de salud; nunca exponer la contraseña, solo el veredicto
- [ ] Tests de los detectores
- [ ] PR a `main`

## 🔵 Sprint 16 — Importar / Exportar (CSV) · `feature/import-export`
> CSV de LastPass / Bitwarden / 1Password (import) y export de entradas propias.
- [ ] Parser CSV por formato (mapear columnas → `EntryCreate`); cifra al importar
- [ ] Export de entradas propias a CSV (⚠️ contraseñas en claro en el archivo → warning explícito)
- [ ] UI: subir CSV con preview/selección de vault destino; botón export
- [ ] Tests de parsers (cada formato + filas inválidas)
- [ ] PR a `main`

## 🟣 Sprint 17 — 2FA (TOTP) · `feature/2fa`
> `User.TwoFactorSecret` (nullable, null = desactivado) ya existe en el modelo.
- [ ] Activar 2FA: generar secret TOTP + QR, verificar código antes de activar
- [ ] Exigir código TOTP en login si está activado (segundo paso del flujo de auth)
- [ ] Desactivar 2FA (reverificando)
- [ ] Tests del flujo TOTP (validación de código, ventana de tiempo)
- [ ] PR a `main`

## 🟣 Sprint 18 — Cierre por inactividad · `feature/session-timeout`
> Cliente: timeout configurable que cierra sesión y limpia el token de LocalStorage.
- [ ] Detectar inactividad (timers + eventos), auto-logout + redirect a login
- [ ] Timeout configurable (constante o setting de usuario)
- [ ] PR a `main`

---

## 🔵 Transversal — Traducir a inglés · `feature/i18n-english`
> **Hacerla pronto** (no bloquea features pero la deuda crece con cada sprint). CLAUDE.md
> exige inglés en código/comentarios/docs. Pasar `docs/*.md` y los comentarios viejos de
> auth/encryption a inglés. Rama independiente, mergeable en cualquier momento.

> **Fuera de scope (post-roadmap):** modelo zero-knowledge (clave derivada del master /
> clave por vault, ver D3/D4), refresh tokens (D2), app móvil, extensión de navegador, offline.
