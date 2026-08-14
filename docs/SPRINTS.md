# The Shed — Plan de sprints

> Hoja de ruta de Fase 4 (Seguridad) → Fase 5 (UI). Cada sprint cierra en una rama
> con PR a `main`. Estado: 🟢 en curso · 🔵 pendiente · 🟣 futuro · ✅ hecho.
> Ver fases generales en `TODO.md` y decisiones en `DECISIONS.md`.

## ✅ Sprint 1 — Cifrado AES-256 de entradas · `feature/encryption-aes`
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

## ✅ Sprint 8 — Compartir vaults (members) · `feature/vault-sharing`
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
- [x] PR a `main` (#9)

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
- [x] PR a `main` (#10)

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
- [x] PR a `main` (#12)

## ✅ Sprint 12 — Historial de versiones · `feature/entry-history`
> `EntryHistory` versiona solo la **contraseña** (`PasswordEncrypted` + `ChangedByUserId`).
> AES-GCM cifra con nonce aleatorio, así que dos cifrados del mismo texto dan ciphertext
> distinto: para saber si la contraseña realmente cambió hay que comparar contra el
> plaintext descifrado, no contra el ciphertext viejo.

### Increment 1 — Backend: snapshot al actualizar
- [x] En `PasswordEntryService.UpdateAsync`, si `request.Password` difiere del plaintext
      actual, guarda un `EntryHistory` (contraseña vieja cifrada + `ChangedByUserId`) antes
      de aplicar el update. Si no cambió, no genera entrada (evita ruido en el historial)
      → commit `feat: snapshot previous password into EntryHistory on update`

### Increment 2 — Backend: endpoints de historial
- [x] DTO `EntryHistoryItem` (Id, CreatedAt, ChangedByUsername — sin contraseña, mismo
      patrón metadata-en-listado que `EntryListItem`) + `EntryHistoryDetail` (revela una
      contraseña vieja puntual, descifrada)
- [x] `GetHistoryAsync` (lista ordenada por fecha desc) + `GetHistoryEntryAsync`;
      autorización vía `LoadForAccessAsync` (solo lectura, del Sprint 11) → rutas
      `GET /api/entries/{id}/history` y `GET /api/entries/{id}/history/{historyId}`
- [x] `EntryListItem`/`EntryResponse` suman `PasswordChangedAt` (el `CreatedAt` del último
      `EntryHistory` de la entrada, o el `CreatedAt` de la propia entrada si nunca cambió) —
      "antigüedad de la contraseña actual" visible sin abrir el historial
      → commit `feat: add entry history endpoints`

### Increment 3 — UI
- [x] `EntryClient.GetHistoryAsync`/`GetHistoryEntryAsync`; botón "History" por entrada en
      `VaultDetail.razor` que despliega la lista (fecha + quién cambió) con Reveal
      individual por versión (mismo patrón que el Reveal de la contraseña actual)
- [x] Texto "Password changed X days ago" en el listado de entradas, con `PasswordChangedAt`
      → commit `feat: add entry history UI`

### Increment 4 — Tests
- [x] Snapshot solo cuando cambia la contraseña (no en ediciones que solo tocan
      nombre/URL/notas), orden desc, autorización (viewer lee, no-miembro → 404, historial
      de otra entrada → 404), reveal del valor descifrado correcto, cálculo de
      `PasswordChangedAt` (con y sin historial) — suite completa **118/118** en verde
      → commit `test: add tests for entry history`
- [x] PR a `main` (#13)

### Decisiones de diseño (Sprint 12)
- **Sin purga ni límite de versiones:** el historial crece sin tope por ahora (comentario
  `ponytail:` en el código). No hay evidencia todavía de que la tabla crezca lo suficiente
  como para justificar un límite o una purga automática; si se vuelve un problema real,
  la mejora natural es capar a las N versiones más recientes por entrada o purgar por
  antigüedad, no antes.

## ✅ Sprint 13 — Papelera / recuperar · `feature/trash`
> Reusa el soft-delete (`AuditableEntity.IsDeleted` + global query filter). Hoy borrar = ocultar.
> Alcance: **entradas + notas + vaults** (dueño) juntos, mismo sprint. Suma **purga automática
> a los 30 días** además de la purga manual.

### Increment 1 — Modelo: `DeletedAt`
- [x] `AuditableEntity` suma `DateTime? DeletedAt` — migración (toca las 7 entidades)
      → commit `feat: add DeletedAt to AuditableEntity for trash retention tracking`
- [x] Auditoría centralizada: en vez de tocar cada soft-delete existente, `TheShedContext.SaveChangesAsync`
      intercepta `EntityState.Deleted` y, si la fila no estaba ya `IsDeleted`, la convierte en un
      update (`IsDeleted = true`, `DeletedAt = now`) — cualquier `_db.Remove(...)` en cualquier
      servicio queda soft-delete automáticamente, sin auditar sitio por sitio. Si ya estaba
      `IsDeleted` (purga), se deja pasar como hard delete real

### Increment 2 — Backend: listar/restaurar/purgar (manual)
- [x] `ITrashService`/`TrashService`: lista unificada de borrados del usuario (entries + notes +
      vaults propios) con `IgnoreQueryFilters`; restaurar (`IsDeleted = false`, `DeletedAt = null`);
      purgar definitivo (hard delete)
- [x] Endpoints `GET /api/trash`, `POST /api/trash/{tipo}/{id}/restore`, `DELETE /api/trash/{tipo}/{id}/purge`
      — autorización: entries/notes por `IVaultAccessService` (write), vaults **owner-only**
      → commit `feat: add trash list/restore/purge endpoints for entries, notes and vaults`
- [x] Vault borrado: sus entradas/notas quedan ocultas junto con el vault (no aparecen sueltas en
      la papelera, solo el vault) y restaurar el vault las restaura a ellas también — siguen el
      estado del vault, no tienen entrada propia en la papelera

### Increment 3 — Backend: purga automática (30 días)
- [x] `BackgroundService` (`TrashPurgeService`) que corre cada 24h y llama a
      `TrashService.PurgeExpiredAsync(now)` (hard-delete de Vault/PasswordEntry/SecureNote/Tag
      con `IsDeleted = true` y `DeletedAt < now - RetentionDays`) en un scope propio
      (`IServiceScopeFactory`, porque `ITrashService` es Scoped)
- [x] Configurable (`Trash:RetentionDays`, default 30, `appsettings.json`)
- [x] Tests del cálculo de expiración pasando `now` directo (sin reloj real ni fake de tiempo)
      → commit `feat: add automatic trash purge background service`
- [x] El ciclo de purga atrapa excepciones y loguea en vez de dejarlas propagar: el default de
      .NET (`BackgroundServiceExceptionBehavior.StopHost`) tumba **todo el host** ante una
      excepción no manejada en un `BackgroundService` — un hiccup transitorio de DB no debe
      bajar la API entera

### Increment 4 — UI
- [x] Página `/trash` (link en el nav): lista unificada (tipo + nombre + vault + "Deleted X days
      ago" + "expires in N days"), Restore / Delete forever con `confirm()` (mismo patrón que
      el resto de la app) → `TrashClient` + `Trash.razor`
- [x] Verificado e2e en navegador: crear vault → entrada → borrar → aparece en `/trash` con
      countdown de expiración → Restore → vuelve a aparecer en el vault

### Increment 5 — Tests + PR
- [x] Tests de `TrashService` + `TrashController` (listar, restaurar, purgar manual, autorización
      NotFound/Forbidden, purga automática por expiración) — suite completa **138/138** en verde
      → commit `test: add trash service and controller tests`
- [x] ~~PR a `main`~~ — no hubo PR: `feature/trash` quedó con upstream apuntando a `origin/main`
      (nunca se creó `origin/feature/trash`) y un push mandó los 5 commits directo a `main` sin
      pasar por revisión, rompiendo el patrón del resto de los sprints. Se decidió dejarlo así
      (el código ya estaba testeado y verificado en navegador) en vez de reescribir el historial
      remoto de `main` con force-push. Para el próximo sprint: confirmar `git push -u origin
      feature/<nombre>` explícito antes de empezar a commitear

## ✅ Sprint 14 — Adjuntos · `feature/attachments`
> `Attachment` guarda `StoragePath` + `FileSizeBytes` → el archivo va **fuera de la DB**
> (filesystem local en dev, carpeta configurable).
- [x] `IEncryptionService` suma overloads `EncryptBytes`/`DecryptBytes` (AES-256-GCM, igual que las
      contraseñas) para poder cifrar el contenido binario de los adjuntos
      → commit `feat: add byte[] overloads to IEncryptionService for binary content`
- [x] `AttachmentService` + `AttachmentsController` — sube/baja/borra adjuntos de una entrada;
      valida extensión (`.pdf/.jpg/.jpeg/.png/.txt`) y tamaño (`MaxFileSizeBytes`, default 5 MB) en
      el boundary; `IAttachmentStorage`/`LocalFileAttachmentStorage` (una interfaz mínima detrás,
      por si más adelante se pasa a blob storage) guarda el blob cifrado bajo un nombre
      `Guid.NewGuid()` (nunca el nombre de archivo del usuario, sin superficie de path-traversal)
      → commit `feat: add attachments API`
- [x] UI: adjuntar/descargar/quitar en el detalle de entrada (gated por `CanWrite`) — `AttachmentClient`
      + sección en `VaultDetail.razor` → commit `feat: add attachments UI (upload/download/delete in vault detail)`
- [x] Tests de `AttachmentService` + `AttachmentsController` (límites de tamaño/tipo, autorización)
      → commit `test: add attachment service and controller tests`
- [x] PR a `main` (#14)

### Decisiones de diseño (Sprint 14)
- **Blob cifrado:** el contenido se cifra con el mismo AES-256-GCM que las contraseñas antes de
  tocar disco (decisión pendiente del plan original, resuelta a favor de cifrar).
- **Borrado real, sin papelera:** a diferencia de entries/notes/vaults, los adjuntos no pasan por
  soft-delete — el blob en disco no tiene "deshacer", así que la fila tampoco lo tiene
  (`AttachmentService.DeleteAsync` fuerza `IsDeleted = true` antes del `Remove` para saltar el
  soft-delete automático de `SaveChangesAsync` e ir directo al hard delete).

## ✅ Sprint 15 — Salud de contraseñas · `feature/password-health`
> Reusa `PasswordGenerator`/criterios del Shared para el detector de débiles; el de repetidas vive
> en un servicio nuevo del Server porque necesita comparar contraseñas descifradas entre entradas.

### Increment 1 — Detector de débiles
- [x] `PasswordHealthChecker.EvaluateStrength` (Shared): `Weak`/`Medium`/`Strong` a partir de
      longitud + variedad de clases de carácter + una estimación simple de entropía
      (`length * log2(poolSize)`) — heurística, no un dictionary/pattern check tipo zxcvbn
      → commit `feat: add weak password strength detector`

### Increment 2 — Detector de repetidas + endpoint
- [x] `PasswordHealthService`/`IPasswordHealthService` (Server): descifra en memoria las
      contraseñas de todas las entradas en los vaults accesibles al usuario (propios + compartidos),
      nunca las loguea, y arma el veredicto (`Strength` + `IsReused`, comparando por igualdad de
      texto plano); DTO `PasswordHealthItem` (Shared) sin campo de contraseña
- [x] `HealthController` `[Authorize]` — `GET /api/health/passwords`, nunca devuelve texto plano
      → commit `feat: add password reuse detector and health report endpoint`
- [x] Tests de `PasswordHealthService` + `HealthControllerTests` (fuerza por entrada, reutilización
      cruzada, entradas de vaults compartidos, usuario sin vaults) — suite completa **172/172** en verde

### Increment 3 — UI
- [x] `HealthClient` + página `/health` (link "Password health" en el nav): lista de entradas con
      badge de fuerza (rojo/ámbar/verde) y badge "Reused" aparte, cada una linkeada a su vault —
      nunca se muestra la contraseña, solo el veredicto → commit `feat: add password health UI
      (strength and reuse badges)`
- [x] Verificado e2e en navegador: vault de prueba con entradas débil/fuerte/dos repetidas,
      badges correctos, sin errores de consola
- [x] PR a `main` (#15)

## ✅ Sprint 16 — Cookie httpOnly para el JWT · `feature/jwt-cookie`
> Hoy el JWT viaja en el body de login/register, el cliente lo guarda en `localStorage`
> (`LocalStorageService`) y lo reenvía a mano como header `Authorization`. Cualquier XSS
> puede leer `localStorage` y robar la sesión — en un password manager es el punto más
> sensible del cliente. Mover el JWT a una cookie `HttpOnly` (JS no puede leerla ni con
> XSS) cierra ese hueco sin tocar la decisión D2 (`DECISIONS.md`): sigue siendo un JWT
> HMAC-SHA256 sin refresh token, solo cambia el canal de transporte.
>
> **SameSite = `Lax`** (decidido): manda la cookie en navegaciones normales (click en un
> link, escribir la URL) pero la bloquea en POST/PUT/DELETE cross-site, que es lo que
> importa para CSRF.
>
> **Consecuencia a tener presente:** `TheShed.Client` tiene su propio `launchSettings.json`
> standalone (:5064/:7163, leftover del template hosted de Blazor WASM) que hoy no se usa
> en el flujo real del proyecto (`CLAUDE.md` solo documenta correr `TheShed.Server`, que
> sirve API + cliente en el mismo origen). Con `SameSite=Lax` y sin CORS, correr el Client
> standalone contra un `TheShed.Server` en otro puerto dejaría de poder loguearse (la
> cookie no viaja cross-origin). No se toca ese `launchSettings.json` en este sprint.

### Increment 1 — Separar el token secreto de `AuthResponse`
> Hoy `AuthResponse.Token` viaja del servicio al controller *y* se serializa al cliente en
> el mismo paso — hay que separarlos antes de tocar el controller.
- [x] `TheShed.Shared/Models/DTOs/Auth/AuthResponse.cs` pierde `Token` (queda
      `ExpiresAt`/`Username`/`Email`)
- [x] `IAuthService.cs`: `AuthResult` (record interno, nunca cruza a HTTP tal cual) suma
      `string? Token = null` como 4to parámetro posicional (con default, no rompe los
      `new AuthResult(...)` de 3 args que ya existen en los tests)
- [x] `AuthService.cs`: `Success(User)` pasa el token generado por `_jwt.GenerateToken(user)`
      al nuevo campo `AuthResult.Token` en vez de a `AuthResponse.Token`
- [x] `AuthServiceTests.cs` línea 41 (`result.Response!.Token`) pasa a `result.Token`
      → commit `refactor: separate JWT token from AuthResponse into AuthResult`

> Nota: este increment deja `TheShed.Client` sin compilar a propósito (`AuthService.cs`
> del cliente todavía lee `auth.Token`) — se arregla recién en el Increment 4. `Shared`/
> `Server`/tests compilan y pasan igual.

### Increment 2 — `AuthController`: cookie + `/me` + `/logout`
- [x] `Register`/`Login`: en éxito, `Response.Cookies.Append("authToken", result.Token!, new
      CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.Lax, Path = "/",
      Expires = result.Response!.ExpiresAt })` antes de devolver `result.Response` (sin token)
- [x] Nuevo `[Authorize] [HttpGet("me")]` — arma un `AuthResponse` desde `User` (claim
      `"username"`, `ClaimTypes.Email`/`JwtRegisteredClaimNames.Email`, `exp` decodificado a
      `ExpiresAt`). Es lo que el cliente usa para saber "¿estoy logueado y quién soy?" ahora
      que no puede leer el JWT
- [x] Nuevo `[HttpPost("logout")]` (sin `[Authorize]`, borrar una cookie ausente es
      inofensivo) — `Response.Cookies.Delete("authToken", new CookieOptions { Path = "/" })`
- [x] `AuthControllerTests.cs` — `SampleResponse()` sin `Token`; sumar casos: login/register
      exitoso setea la cookie `authToken` en `HttpContext.Response` (con
      `ControllerContext.HttpContext = new DefaultHttpContext()`, mismo patrón que
      `EntriesControllerTests`), `Me` devuelve 200 con los claims esperados, `Logout` limpia
      la cookie
      → commit `feat: set httpOnly JWT cookie and add /me, /logout endpoints`

### Increment 3 — `Program.cs`: el JwtBearer lee la cookie + sacar CORS `AllowAll`
- [x] `options.Events = new JwtBearerEvents { OnMessageReceived = ctx => { if
      (string.IsNullOrEmpty(ctx.Token) && ctx.Request.Cookies.TryGetValue("authToken", out var t))
      ctx.Token = t; return Task.CompletedTask; } }`. Solo cae a la cookie si **no** vino un
      header `Authorization` — así `Authorization: Bearer <token>` manual (Postman/REST
      client, ya documentado en `AUTH_FLOW.md`) sigue funcionando igual que hoy
- [x] Sacar la policy CORS `AllowAll` (`AllowAnyOrigin+AllowAnyMethod+AllowAnyHeader`) y su
      `app.UseCors(...)`: cliente y API viven en el mismo origen (modelo hosted), no hace
      falta, y con `SameSite=Lax` sin `AllowCredentials` un request cross-origin no iba a
      poder autenticarse igual — dejarla wireada es superficie sin beneficio
      → commit `feat: read JWT from cookie in JwtBearer and remove permissive CORS policy`

### Increment 4 — Cliente: dejar de tocar el token a mano
- [x] `JwtAuthenticationStateProvider.cs` — reescribir sin `ILocalStorageService`:
      `GetAuthenticationStateAsync()` llama `GET api/auth/me`; 200 → arma el `ClaimsIdentity`
      con claims `"username"` y `"email"` (**preservar el tipo `"username"` tal cual** —
      `Home.razor:8` hace `context.User.FindFirst("username")`); si falla (401 →
      `HttpRequestException`) devuelve `Anonymous`. `NotifyAuthenticated` pasa a
      `RefreshAsync()` (re-llama `/me` y notifica) — se usa después de un login/register
      exitoso, porque el `Set-Cookie` ya lo puso el browser solo. `NotifyLoggedOut` se
      simplifica (ya no hay header `Authorization` que limpiar a mano)
- [x] `JwtParser.cs` — **borrado** (nadie decodifica un JWT visible del lado cliente nunca más)
- [x] `AuthService.cs` (client) — sin `ILocalStorageService`; `LoginAsync`/`RegisterAsync` ya
      no leen/guardan `auth.Token` (el campo no existe más), llaman
      `_stateProvider.RefreshAsync()` tras un POST exitoso; `LogoutAsync` pasa a `POST
      api/auth/logout` + `NotifyLoggedOut()`
- [x] `ILocalStorageService.cs`/`LocalStorageService.cs` — **borrados** (sin otros consumidores
      tras este cambio, confirmado por grep). Sacado su `AddScoped` de `Program.cs`
      → commit `refactor: switch client auth to cookie-based /me flow, drop localStorage`

### Increment 5 — Docs + PR
- [x] Sumar entrada **D6** en `DECISIONS.md` (cookie httpOnly, SameSite=Lax, motivo)
      siguiendo el patrón de D1-D5
- [x] Verificación e2e en navegador (ver abajo)
- [ ] PR a `main`

### Verificación
- `dotnet build` (solución completa) + `dotnet test` — suite completa en verde.
- E2E en navegador: login → DevTools ▸ Application ▸ Cookies muestra `authToken` con
  `HttpOnly`/`Secure`/`SameSite=Lax`; Local Storage ya no tiene `authToken`;
  `document.cookie` en consola no incluye `authToken` (prueba que JS no puede leerla). F5
  con sesión activa → sigue logueado (roundtrip a `/me`). `/vaults`, `/health`, `/trash`
  siguen andando (la cookie autentica los requests). "Welcome back, {username}" en Home
  sigue mostrando el nombre (el claim `"username"` sigue llegando). Logout → cookie
  borrada, páginas protegidas redirigen a `/login`.

## ✅ Decisión tomada (2026-08-14) — Zero-knowledge real, ver D7
> `docs/AUDITORIA.md` C1/M1 + `DECISIONS.md` D7. El modelo de amenaza incluye al operador
> del servidor (The Shed puede correr para terceros no relacionados con quien lo hostea),
> así que "self-hosted, confío en mi servidor" no alcanza. Se migra a cifrado
> client-side (WASM) con clave derivada de la master password; D3 queda superseded.
>
> **Prioridad real, no solo de lectura:** estos 4 sprints (25-28) tocan la base de
> cifrado que usan Sprint 17 (import/export), 21 (login hardening no se pisa, pero
> comparte el modelo de auth) y 23 (adjuntos huérfanos — la limpieza en disco sigue
> aplicando igual, no cambia con esto). Conviene resolverlos **antes** de sumar más
> superficie sobre entradas/adjuntos/sharing — mismo motivo por el que Sprint 20 se
> adelantó al resto en su momento. Orden de ejecución real: decisión del usuario al
> crear las ramas, esto es la nota, no un bloqueo automático.

## 🟣 Sprint 25 — Zero-knowledge: derivación de clave y keypair por usuario · `feature/e2e-key-derivation`
> Base de todo lo que sigue: nada de esto cambia D1 (Argon2 sigue siendo el hash de
> auth) — es una derivación **independiente** del mismo master password, que el
> servidor nunca ve.

- [ ] KDF client-side: `Rfc2898DeriveBytes` (PBKDF2-SHA256, managed, corre en WASM sin
      interop) deriva una "stretched master key" (256 bits) del master password + un
      salt del usuario. **Nota ponytail:** no Argon2 client-side por ahora —
      `Isopoh.Cryptography.Argon2` es nativo (P/Invoke), no corre en WASM sin research
      aparte; PBKDF2 con ≥600k iteraciones (guía OWASP 2023) es aceptable como punto de
      partida, upgrade a Argon2id-WASM si aparece una lib que lo soporte
- [ ] Keypair por usuario al registrar: `RSA` (`System.Security.Cryptography.RSA`, ya
      disponible en WASM) generado en el cliente. La clave privada se cifra con la
      stretched master key (AES-GCM, mismo formato que D3) antes de subir. El servidor
      guarda `PublicKey` (texto) y `EncryptedPrivateKey` (blob opaco) — nunca ve la
      privada en claro
- [ ] Migración de esquema: `User.PublicKey`/`User.EncryptedPrivateKey` nullable —
      usuarios existentes quedan sin keypair hasta que logueen post-Sprint 28 y se les
      genere retroactivamente
- [ ] Tests: derivación determinística (mismo password + salt → misma key), roundtrip
      de keypair, inspección del payload de red del registro para confirmar que la
      privada nunca viaja en claro

## 🟣 Sprint 26 — Zero-knowledge: vault key y cifrado de entradas/notas · `feature/e2e-vault-encryption`
- [ ] Vault key: al crear un vault, el cliente genera una clave AES-256 aleatoria
      ("vault key"), la cifra con la stretched master key del dueño y la sube como blob
      opaco (`VaultKeyWrap` por `vaultId`+`userId`, arranca con una fila: el dueño)
- [ ] `PasswordEntry`/`SecureNote`: el cifrado se mueve al cliente con la vault key
      (reusa el mismo formato `nonce||ciphertext||tag` de `AesEncryptionService`, corriendo
      del lado `Client` en vez de `Server` — mismo código de `Shared`, distinto lugar de
      ejecución). El servidor deja de descifrar: `PasswordEntryService.ToResponse` y
      equivalentes pasan a devolver el blob tal cual
- [ ] Listado y búsqueda: hoy `PasswordEntryService` filtra server-side por
      nombre/usuario/URL. Pasa a traer todos los items del vault de una vez (blobs),
      descifrar en memoria del cliente y filtrar ahí — mismo patrón que Bitwarden/
      1Password. Válido a la escala de un vault personal (cientos de items, no miles)
- [ ] Tests: roundtrip end-to-end simulado desde el cliente; confirmar que con lo que
      el servidor tiene guardado **no** puede reconstruir el plaintext

## 🟣 Sprint 27 — Zero-knowledge: compartir vaults (key wrapping) · `feature/e2e-vault-sharing`
- [ ] Al agregar un `VaultMember`: el dueño pide la public key del nuevo miembro
      (`GET /api/users/{id}/public-key`), envuelve la vault key con ella (RSA-OAEP) y
      sube un nuevo `VaultKeyWrap` para ese `userId`. El miembro, al loguear, desenvuelve
      su privada con su stretched master key y con eso desenvuelve la vault key
- [ ] Remover un miembro: **no rota la vault key** en este increment — limitación
      conocida y documentada (D7, riesgo aceptado), no fingir que remover es
      retroactivamente seguro sin rotación (M1)
- [ ] Historial y adjuntos: mismo patrón que entradas/notas (Sprint 26) — cifrados con
      la vault key, cifrado/descifrado movido al cliente
- [ ] `PasswordHealthService` pasa a `PasswordHealthChecker` (ya vive en `Shared`)
      corriendo client-side sobre el vault ya descifrado en memoria — el endpoint deja
      de necesitar descifrar todo server-side para armar el reporte
- [ ] Tests + PR a `main`

## 🟣 Sprint 28 — Zero-knowledge: migración de datos existentes · `feature/e2e-migration`
> El sprint que hace real todo lo anterior para los vaults que ya existen — sin esto,
> Sprint 25-27 solo aplican a cuentas nuevas.
- [ ] Endpoint de migración: con la clave global vieja de D3 (que se mantiene viva
      **solo** para esto), el servidor descifra una última vez el contenido de un vault
      y lo expone por HTTPS a una llamada autenticada del propio dueño; el cliente lo
      vuelve a cifrar con la vault key nueva y sube los blobs nuevos; el servidor no
      persiste el valor descifrado en ningún punto intermedio
- [ ] Bandera de estado por vault (`EncryptionVersion` o similar) para distinguir
      "todavía con clave de servidor" de "ya migrado" mientras conviven ambos modelos
- [ ] Una vez confirmado el 100% de los vaults migrados (medido, no asumido): apagar
      `Encryption:Key`/`EncryptionSettings` y borrar `AesEncryptionService` del lado
      `Server`
- [ ] Increment opcional, a decidir si el producto lo pide: recovery key — generarla y
      mostrarla una única vez al usuario (para guardar offline) como mitigación del
      costo de UX aceptado en D7 (olvidar la master password = pérdida total sin esto)
- [ ] PR a `main`

## 🔵 Sprint 17 — Importar / Exportar (CSV) · `feature/import-export`
> CSV de LastPass / Bitwarden / 1Password (import) y export de entradas propias. Ningún
> proyecto tiene hoy una lib de CSV — un parser a mano que solo hace `Split(',')` rompe con
> comillas/comas/saltos de línea embebidos en `notes` o `password`, justo el campo más
> sensible. Se suma `CsvHelper` (NuGet, la lib estándar de facto en .NET) en vez de
> reinventar el parseo: es la excepción lazy correcta acá — un parser CSV RFC4180 a mano
> es más código y más riesgo que una dependencia madura de un solo propósito.

### Increment 1 — DTOs + dependencia
- [ ] `PackageReference CsvHelper` en `TheShed.Server.csproj`
- [ ] DTOs `ImportResult` (Imported/Skipped/Errors por fila) y `ExportEntryRow` (Name,
      Username, Password, Url, Notes — mismas columnas que expone el export)

### Increment 2 — Backend: import
- [ ] `ICsvImportService`/`CsvImportService`: un mapper de columnas por formato (LastPass:
      `url,username,password,extra,name,grouping,fav`; Bitwarden: `folder,favorite,type,name,
      notes,fields,reprompt,login_uri,login_username,login_password,login_totp`; 1Password:
      `Title,Website,Username,Password,Notes`) → `EntryCreateRequest`, reusa
      `IVaultAccessService` (write al vault destino) + `IEncryptionService` (cifra cada fila
      al crear, mismo camino que `PasswordEntryService.CreateAsync`)
- [ ] Fila inválida (columnas faltantes, vacía) no aborta el import completo: se cuenta en
      `ImportResult.Errors` con el número de fila, se sigue con las demás
- [ ] `POST /api/import/csv?vaultId={id}&format={lastpass|bitwarden|1password}` (multipart
      file upload), `[Authorize]`

### Increment 3 — Backend: export
- [ ] `CsvExportService`: entradas propias (vaults donde el usuario tiene acceso), descifra
      y vuelca a CSV vía `CsvHelper`
- [ ] `GET /api/export/csv?vaultId={id}` devuelve el archivo (`Content-Disposition:
      attachment`) — contraseñas en claro en el archivo, advertencia va en la UI, no en la API

### Increment 4 — UI
- [ ] `ImportExportClient` + sección en `/vaults/{id}` (o página propia `/import-export`):
      input de archivo + selector de formato, preview de `ImportResult` tras subir
      (importadas/saltadas/errores por fila); botón "Export" con `confirm()` de aviso
      ("el archivo tendrá las contraseñas en texto plano") antes de disparar la descarga

### Increment 5 — Tests
- [ ] Tests de `CsvImportService` (los 3 formatos, fila con columnas faltantes, fila vacía,
      autorización de escritura al vault) + `CsvExportService` (roundtrip descifrado) +
      controllers (upload/download)
- [ ] PR a `main`

## 🟣 Sprint 18 — 2FA (TOTP) · `feature/2fa`
> `User.TwoFactorSecret` (nullable, null = desactivado) ya existe en el modelo.
- [ ] Activar 2FA: generar secret TOTP + QR, verificar código antes de activar
- [ ] Exigir código TOTP en login si está activado (segundo paso del flujo de auth)
- [ ] Desactivar 2FA (reverificando)
- [ ] Tests del flujo TOTP (validación de código, ventana de tiempo)
- [ ] PR a `main`

## 🟣 Sprint 19 — Cierre por inactividad y reautenticación · `feature/session-timeout`
> Cliente: timeout configurable que cierra sesión y limpia el token de LocalStorage. Si el
> Sprint 16 (cookie httpOnly) ya se implementó para entonces, "limpiar el token" pasa a ser
> `POST api/auth/logout` en vez de tocar LocalStorage.
> **Alcance ampliado por `docs/AUDITORIA.md` (A1, A2):** al auto-logout ya planeado (A2)
> se suma reautenticación para acciones sensibles (A1) — hoy alcanza con que la cookie JWT
> siga vigente (60 min, `Jwt:ExpiryMinutes`) para revelar/copiar una contraseña o abrir una
> versión del historial, sin volver a pedir la master password.
- [ ] Detectar inactividad (timers + eventos), auto-logout + redirect a login
- [ ] Timeout configurable (constante o setting de usuario)
- [ ] Reautenticación (A1): pedir de nuevo la master password (o un PIN corto derivado de
      ella) antes de Reveal/Copy de una contraseña o de abrir una versión del historial;
      ventana corta de "desbloqueado" tras verificar (ej. 5 min) para no pedirla en cada click
- [ ] PR a `main`

## 🟣 Sprint 20 — Refactor de frontend: cerrar la identidad "Workshop" · `feature/frontend`
> Al cierre de todos los sprints funcionales (post Sprint 19), no antes — evita rehacer UI
> a mitad de camino. **No arranca de cero:** el Sprint 5 ya definió una identidad propia
> (`docs/UI.md`, concepto "Workshop" — acento ámbar/cobre, sin gradientes/glass, mono para
> secretos, vault card = cajón etiquetado) que es exactamente el punto medio buscado
> (profesional + distintivo, sin literal wood-grain/pegboard). Este sprint es **cerrar los
> ítems sin tildar de `UI.md` + auditar el drift** de los 11 sprints de features construidos
> encima desde entonces (Sprints 6-16), no reinventar paleta ni tono.
>
> **Creció sobre la marcha:** los increments 1-5 cerraron la identidad visual; cerrado eso se
> auditó el cliente entero (Increment 6) y los increments 7-11 son la deuda técnica de frontend
> que salió de ahí. Sigue todo en `feature/frontend`, un solo PR al final.

### Increment 1 — Auditoría (sin código)
- [x] Pasada página por página (Home, Login/Register, Vaults, VaultDetail —entries/notes/
      tags/history/attachments—, Health, Trash) contra el checklist de `docs/UI.md`: ¿usa
      los tokens (`--surface`/`--accent`/`--radius`/`--font-mono`) o se filtró un default de
      Bootstrap sin pasar por ellos?
- [x] ~~Confirmado ya: `NavMenu.razor` usa clases `bi-*-nav-menu` que **no existen en ningún
      CSS del proyecto** (residuo del template default de Blazor WASM, de antes del rename
      SwimAnalytics→TheShed) — hoy el nav no muestra ningún ícono, no es un tema de
      "elegir mejor ícono"~~
- [x] ⚠️ **Corrección: la auditoría se equivocó en dos puntos, ambos por no haber abierto los
      `.razor.css` scoped.** Solo miró `app.css` y el markup de las páginas.
      1. Las clases `bi-*-nav-menu` **sí existían**, en `NavMenu.razor.css:24-42`, con los SVG
         embebidos en `fill='white'`. El nav no mostraba íconos por otra razón: el `.bi` de esas
         reglas es un `background-image` de 1.25rem, no la fuente Bootstrap Icons — que nunca
         estuvo instalada (ver Increment 2)
      2. **Se le pasó el problema visual más grande del proyecto**: `MainLayout.razor.css:12`
         tenía el degradado violeta del template de Blazor
         (`linear-gradient(180deg, rgb(5,39,103) 0%, #3a0647 70%)`) en `.sidebar`, más un
         `.top-row` gris claro `#f7f7f7` y un sidebar de 250px. Apareció recién al mirar la app
         en el navegador, después de dar los increments por cerrados
      **Lección para la próxima auditoría de UI: el CSS scoped no es opcional de revisar.** Compila
      a `.sidebar[b-xxx]`, le gana en especificidad a `app.css`, y por eso todo el shell definido
      en el Sprint 5 (sidebar sobre `--surface`, top-row con borde `--border`, ancho 15rem) nunca
      se aplicó — estaba escrito y era invisible. Y auditar leyendo archivos no reemplaza abrir la
      app: los dos errores se veían a simple vista en pantalla
- [x] **Auditoría completa — confirmado el mismo patrón en todas las vistas de listado.**
      Los overrides globales de `app.css` (`--bs-primary`→ámbar, headings uppercase, `.card`
      recoloreado) sí llegan a todos lados porque son automáticos. Pero el contenido de cada
      página nunca se conectó con las clases que Sprint 5 diseñó a propósito para él — de ahí
      la sensación "IA + Bootstrap genérico": la pintura de fondo está, el resto no.
      - `Vaults.razor` — `list-group-item` en vez de `.vault-card` (`app.css:208-209`, ya
        definida: hover con borde ámbar + `surface-2`, el efecto "cajón etiquetado"); badges
        de rol (Owner/Editor/Viewer) en `bg-secondary`/`bg-info`/`bg-light` stock
      - `VaultDetail.razor` — mismo `list-group-item` para entries, notes **y** members;
        badges de tags en `bg-secondary` stock; **`.secret` (mono para contraseñas, el otro
        pilar de identidad de `UI.md`) no se usa en ningún lado del archivo** — la contraseña
        revelada se muestra en la tipografía normal, no monoespaciada
      - `Trash.razor` — `list-group-item` + badge de tipo en `bg-secondary` stock
      - `Health.razor` — `list-group-item`; los badges de fuerza usan `bg-danger`/
        `bg-warning`/`bg-success` de Bootstrap **sin remapear** (`app.css` solo redefine
        `--bs-primary` y `--bs-secondary-color`, no `--bs-danger`/`--bs-warning`/
        `--bs-success`) — el rojo/verde de Bootstrap, no el rust/success que `UI.md` define
        como identidad (`--danger: #c4503a`, `--success: #6f9e4a`)
      - `Login.razor`/`Register.razor` — **estas sí están bien conectadas**, usan `.auth-card`
        correctamente. Sirven de referencia de "cómo se ve cuando el contenido sí usa el
        sistema", contraste útil para el resto
- [x] Conclusión: el problema no es visual (elegir mejores colores), es de **integración** —
      re-conectar clases que ya existen y ya están bien diseñadas. El Increment 5
      (consistencia) es en la práctica el core de este sprint, no un cleanup menor al final
- [x] Salida: punch list concreta, no un rediseño — este increment no toca código

### Increment 2 — Nav: íconos reales
- [x] Reemplazar las clases muertas de `NavMenu.razor` por **Bootstrap Icons** (`bi bi-*`,
      ya usado/documentado en `UI.md` §6, cero dependencia nueva) — 6 íconos (Home, Vaults,
      Password health, Trash, Sign out, Sign in), elegidos por afinidad al vocabulario
      workshop donde el nombre lo permita (ej. `bi-box-seam`/`bi-archive` para Vaults en vez
      de un genérico "list")
- [x] Sin ícono custom dibujado a mano para esto: 6 glyphs de nav no justifican mantener un
      icon font propio; Bootstrap Icons cubre el caso
- [x] **Corrección al plan:** Bootstrap Icons **no estaba** en el proyecto (`UI.md` §6 lo daba
      por tildado, pero `wwwroot/lib/` solo tenía el CSS/JS de Bootstrap y `index.html` no lo
      linkeaba) — por eso el nav no mostraba nada. Se sumó por CDN de jsDelivr, una línea en
      `index.html`; vendorizarlo queda como opción si el uso offline importa

### Increment 3 — Empty states con voz "workshop"
- [x] Copy propio (no "No items found") en las listas vacías: Vaults, entradas/notas de un
      vault recién creado, Trash sin nada borrado, Health sin vaults. `UI.md` ya da el tono
      de ejemplo ("The shed's empty — hang your first vault.")
- [x] Sin ilustración custom todavía — texto + el ícono de la sección alcanza; una
      ilustración dibujada es la primera candidata a cortar si el sprint se alarga

### Increment 4 — Feedback y loading, una vez
- [x] Definir **un** patrón de alerta de éxito/error (hoy cada página arma su propio
      `alert alert-danger` suelto) y **un** indicador de carga (spinner Bootstrap ya
      alcanza) — implementarlo en un lugar y aplicarlo, no un componente nuevo por página
- [x] Carga: componente `Components/Loading.razor` aplicado en los 8 puntos de espera
      (vaults, entradas, notas, historial, adjuntos). Feedback: el `alert alert-danger`
      inline **ya era** el patrón repetido en 5 páginas y quedó themeado solo por el remapeo
      de tokens — se documentó como decisión en vez de agregar un componente de toast sin
      consumidor; el éxito usa la misma forma con `alert-success` cuando alguna pantalla
      lo necesite
- [x] Cierra los dos ítems de `UI.md` §5 que quedaron sin decidir en Sprint 5

### Increment 5 — Pasada de consistencia
- [x] Corregir el drift real que haya salido del Increment 1 (colores/radios/espaciados
      que no pasan por los tokens) — alcance = lo que apareció en la auditoría, no una
      reescritura general
- [x] **La causa raíz no eran las páginas, era el tema oscuro de Bootstrap**: pinta sus
      propias superficies/bordes/semánticos en grises fríos (`#212529`, `#343a40`, `#6c757d`),
      así que `list-group`, inputs y badges ignoraban la paleta por más que el contenido usara
      las clases correctas. Se remapearon los `--bs-*` a los tokens en `:root` una sola vez
      (diff mucho menor que reescribir cada listado en cada razor). Excepción: los `.btn-*`
      llevan hex literal compilado, así que `outline-secondary/primary/danger` y `warning`
      (la estrella de favorito) se restatearon a mano
- [x] `.badge-role` pasó a `.badge-chip` — roles, tags y tipos de la papelera comparten el
      mismo chip neutro, y `.secret` (mono) se aplicó al campo de contraseña del formulario
- [x] **Shell: se borraron `MainLayout.razor.css` y `NavMenu.razor.css`** (ver la corrección del
      Increment 1). Eran la fuente de verdad duplicada que le ganaba a `app.css` por
      especificidad; el shell quedó consolidado en `app.css` con tokens (sidebar y top-row sobre
      `--surface`, sticky, nav-links en `--text-muted` con el activo en ámbar). Neto −173/+39
      líneas. También salió el link "About" a learn.microsoft.com del top-row, reemplazado por el
      usuario logueado
- [x] Tipografía: solo si la auditoría encuentra algo roto — el scale de Bootstrap ya
      está aceptado en `UI.md` ("keep Bootstrap's; only revisit if something looks off").
      No apareció nada roto, no se tocó

### Increment 6 — Auditoría de frontend (sin código)
> Cerrada la identidad visual, se auditó `TheShed.Client` entero (21 archivos `.razor`/`.cs`,
> 1.985 líneas + `wwwroot`) buscando duplicación, código muerto y problemas propios de Blazor.
> Los increments 7-10 salen de ahí. **Lo que está bien y no se toca:** cero `async void`, cero
> `.Result`/`.Wait()`, cero `StateHasChanged` manual, servicios tipados por recurso inyectados
> por DI (ninguna página arma `HttpClient` a mano) y `NotFoundPage` de .NET 10 ya usado.
- [x] Inventario: **solo 2 componentes propios** (`Loading`, `RedirectToLogin`), todo lo demás
      son páginas — de ahí que haya 8 `alert alert-danger` y 6 `confirm()` copiados
- [x] Hallazgo estructural: `VaultDetail.razor` son **986 líneas, el 50% del frontend**
      (la segunda página más grande tiene 121), con 5 responsabilidades y 4 pares
      `_busy`/`_error` paralelos
- [x] Salida: 19 hallazgos priorizados. Ninguno es breaking hacia afuera — no cambian rutas,
      contratos de API ni DTOs de `Shared`

### Increment 7 — Correcciones visibles
> Primero lo que el usuario puede ver hoy. Nada toca firmas públicas.
- [x] Búsqueda de entradas con debounce + cancelación (`VaultDetail.razor:140,734`): hoy
      dispara **un request por tecla** y sin cancelar, así que una respuesta vieja puede pisar
      a una nueva y mostrar resultados que no corresponden al input. `CancellationTokenSource`
      + `Task.Delay(300)`, sin librerías. Ojo: esto sí obliga a implementar `IDisposable` en el
      componente, que hoy no lo necesita
- [x] La cancelación se llevó un cambio de firma en `EntryClient.ListAsync` (parámetro
      `CancellationToken` opcional, default `default`): sin eso el debounce corta la espera pero
      **no el request ya en vuelo**, que es la mitad de la carrera. Los otros 4 llamadores no
      se tocaron
- [x] `_copiedId` no se resetea nunca (`VaultDetail.razor:207,584`): el botón queda en
      "Copied!" hasta recargar. Y el comentario de la línea 584 dice "never shown on screen"
      justo de lo que la 207 muestra en pantalla — corregir ambos. Es de los pocos lugares
      donde `StateHasChanged` está justificado (el cambio ocurre después de un `await`)
- [x] `ErrorBoundary` en `MainLayout` + try/catch en las 7 operaciones destructivas sin red
      (`Trash.razor:76,87` · `VaultDetail.razor:557,742,790,882`): hoy un 500 al borrar sube al
      renderer y el usuario ve la barra amarilla genérica. Es incoherente dentro del mismo
      archivo — crear y editar **sí** están envueltos. El patrón de alerta ya está decidido en
      `UI.md` §5: reusar, no diseñar
- [x] El `ErrorBoundary` lleva `Recover()` en `OnParametersSet`: sin eso, una vez que una
      página rompe el boundary queda mostrando el error **para siempre**, incluso navegando a
      otra sección. Se le puso `ErrorContent` propio con el `alert alert-danger` del sistema en
      vez del default de Blazor (que trae su propio rojo `#b32121` fuera de paleta) — o sea que
      `.blazor-error-boundary` en `app.css:260-268` **queda definitivamente muerto** y el
      Increment 8 lo borra en vez de dudar
- [x] Superficie de error para las acciones de lista: `_error` solo se renderiza dentro del
      formulario abierto, así que fallar al borrar o marcar favorito no tenía dónde mostrarse.
      Se sumó **un** `_actionError` a nivel página (favorito, borrar entrada, borrar nota, quitar
      miembro) en vez de un campo por sección — el componente ya tiene 4 pares busy/error y el
      Increment 10 los va a migrar a todos. Borrar tag y borrar adjunto usan los suyos, que sí
      están cerca del botón
- [x] `@key` en los `@foreach` de entradas, notas y miembros (`:178`, `:362`, `:397`).
      Importa sobre todo en entradas porque **la lista se reordena** al marcar favorito.
      Matiz: el markup se deriva del modelo y el estado va en diccionarios por id, así que los
      **datos que se muestran ya son correctos** — lo que se pierde sin `@key` es la identidad
      de los nodos DOM (foco, scroll, el `<input type=file>` de adjuntos)
- [ ] **Falta verificación en navegador** — los 4 arreglos son de comportamiento (timing,
      reordenamiento, fallos de red) y ninguno se prueba compilando. Ver la nota de límites al
      pie del sprint

### Increment 8 — Limpieza: borrar, no agregar
- [x] Podar `wwwroot/lib/bootstrap/` a `bootstrap.min.css`: **el JS de Bootstrap nunca se
      carga** (`index.html` no lo linkea) y aun así se publican los 12 archivos de `dist/js/`,
      los `.map`, los RTL y los builds grid/reboot/utilities. Verificado que nada lo necesita:
      no hay modales/dropdowns/tooltips, el colapso del nav es C# + CSS y "Manage tags" usa
      `<details>` nativo. **Requiere verificación** antes de borrar: que ningún sprint futuro
      dependa del JS (los modales serían el candidato)
- [x] Resultado medido: **8,5 MB → 236 KB, 44 archivos trackeados → 1**. El grueso eran los
      `.map`, no el CSS. Riesgo asumido: el Sprint 19 (aviso de inactividad) es el candidato
      más probable a querer un modal; si pasa, se re-agrega solo el bundle JS o se hace con
      `<dialog>` nativo. Todo esto es un `git checkout` de distancia
- [x] `.form-floating` (`app.css:309-317`) sin ningún `.razor` que lo use — residuo del
      template. Y `.blazor-error-boundary` (`:260-268`) solo aplica si existe un
      `<ErrorBoundary>`: si entra el Increment 7 pasa a estar viva, si no se borra. Decidir,
      no dejarla en el limbo → **borrada**: el Increment 7 le puso `ErrorContent` propio al
      boundary, así que el default de Blazor no se renderiza nunca
- [x] `AuthResult.Response` (`IAuthService.cs:6`) no lo lee nadie — `AuthService:58`
      deserializa el `AuthResponse` solo para descartarlo. Pasa a `record AuthResult(bool
      Success, string? Error)`; el estado del usuario ya viene por `AuthenticationStateProvider`,
      que es la fuente correcta. Único cambio de firma pública del sprint, sin consumidores
      fuera del proyecto
- [x] Con `Response` afuera, la deserialización quedó sin motivo y se borró entera: el único
      chequeo que aportaba era "body no nulo", y quien confirma la sesión de verdad es
      `RefreshAsync()` contra `/api/auth/me`. Se fue también el `AuthResult.Fail("Unexpected
      response from the server.")`, que solo podía dispararse con un 2xx de body vacío
- [x] `wwwroot/icon-192.png` sin referencias (**requiere verificación**: es el tamaño típico
      de manifest PWA y el proyecto no tiene manifest ni service worker) → borrado; si más
      adelante se quiere PWA, el icono se regenera
- [x] Comentario XML mentiroso en `VaultClient.cs:6-7` — afirma que el `HttpClient` lleva el
      Bearer token, falso desde el Sprint 16 (cookie `HttpOnly`). Un comentario que miente
      sobre el modelo de auth es peor que ninguno. Más el BOM y el `@layout MainLayout`
      redundante de `NotFound.razor` (ya es el `DefaultLayout` de `App.razor:4`)
- [x] **Fuera del plan original:** `type="email"` en los campos de email de `Login.razor:22` y
      `Register.razor:28`, que eran `type="text"`. Salió de ver el gestor de contraseñas del
      navegador autocompletando `user2user3` en el login y fallando la validación de
      `[EmailAddress]`. No elimina el autofill —eso es una credencial guardada en el navegador,
      no algo que la app controle— pero acota lo que el navegador ofrece y da el teclado
      correcto en móvil

### Increment 9 — Extracciones (con la duplicación ya medida)
- [ ] `Components/ErrorAlert.razor` (`Message` + `Class` para las variantes inline) y una
      extensión `IJSRuntime.ConfirmAsync()` — 14 usos entre las dos. Extensión y no servicio
      con interfaz: hay una sola implementación posible
- [ ] `DateTimeExtensions.ToRelativeText()`: `DaysAgoText` está duplicado idéntico en
      `Trash.razor:57` y `VaultDetail.razor:587`. Si se quiere en `Shared` para que Server
      también lo use, decidirlo **antes** del Sprint 17
- [ ] Constantes repartidas: claims `"username"`/`"email"` en 3 archivos, ruta `vaults/{id}`
      armada a mano en 4 lugares, y el límite de 5 MB en `AttachmentClient.cs:10` (con el
      comentario que admite que espeja al server) repetido como string de UI en
      `VaultDetail.razor:652`. El de adjuntos es el de más riesgo: si el server cambia el
      límite, el cliente miente en dos lugares y nada falla al compilar
- [ ] **No unificar `Login`/`Register`.** Son ~90% idénticas, pero son dos páginas estables
      que nadie toca hace 15 sprints y el componente con `RenderFragment` se lee peor que
      cualquiera de las dos. Extraer solo si aparece una tercera pantalla de auth — el 2FA del
      Sprint 18 es el candidato natural

### Increment 10 — Partir `VaultDetail`
> Al final a propósito: los increments 7-9 le sacan ~150 líneas antes de empezar y le dejan
> las piezas (`ErrorAlert`, `ConfirmAsync`, `ToRelativeText`) que los componentes nuevos van a
> consumir. **Conflicto de agenda a decidir:** el Sprint 17 (import/export) suma UI a este
> mismo archivo, así que o se parte antes o se parte a ~1.100 líneas.
- [ ] Partir por sección y no por tipo de dato — `EntryList`, `NoteList`, `MemberPanel`, cada
      uno dueño de su propio busy/error. `VaultDetail` queda como cabecera + composición (~80
      líneas). Refactor interno: no cambia rutas ni API
- [ ] `OnInitializedAsync` → `OnParametersSetAsync` (`:490`): hoy el cuerpo depende de
      `[Parameter] Id` y no re-ejecuta si el parámetro cambia sin recarga. **No es alcanzable
      hoy** (no hay ningún link de un vault a otro), pero el primer breadcrumb o "vaults
      relacionados" lo activa. Limpiar además el estado por vault (`_revealed`, `_history`,
      `_attachmentsOpenIds`) al cambiar el id
- [ ] Oportunistas, si el refactor los toca de paso: `DotNetStreamReference` para la descarga
      de adjuntos (hoy el `byte[]` va a JS en base64 dentro del JSON del interop, +33% y una
      copia entera de hasta 5 MB de cada lado) y cachear el `AuthenticationState` en
      `JwtAuthenticationStateProvider:16-28`, que además traga cualquier `HttpRequestException`
      como "no autenticado" — un 500 o un corte de red se le muestran al usuario como sesión
      cerrada

### Increment 11 — Accesibilidad y PR
- [ ] `aria-label` en el botón de favorito (`VaultDetail.razor:199-201`): su contenido es `★`,
      así que un lector de pantalla anuncia el carácter. `role="status"` en `Loading.razor`.
      El resto está bien: `<label for>` correctos en los 4 formularios y los `<span class="bi">`
      con `aria-hidden="true"`
- [ ] Verificación e2e en navegador de las páginas tocadas (mismo patrón que sprints
      anteriores) + PR a `main`

### Increment 12 — Responsive: shell mobile-first
> Cierra el límite que dejó explícito el Increment 6: "el responsive abajo de 641px no
> se evaluó — `app.css` ya declara desde el Sprint 5 que el shell es desktop-first".
> `app.css` forzaba el sidebar siempre abierto con un comentario
> `ponytail: desktop-first, revisit if mobile matters` — llegó el momento.
- [x] Sidebar pasa a top bar sticky en mobile (marca + hamburguesa); `.nav-scrollable`
      se despega del flujo y se muestra como dropdown superpuesto, con `.nav-backdrop`
      oscureciendo el resto y cerrando el menú al tocar afuera. Desktop (≥641px) sigue
      igual: columna fija de 15rem, nav siempre abierto
      → commit `fix: make the app shell mobile-first with an off-canvas nav menu`
- [x] Verificado en navegador a 390px (login/register, vaults, vault detail con
      formulario abierto y con un entry creado) y sin regresión a 1536px (desktop)
- [ ] **Encontrado al verificar, queda para el Increment 13:** la fila de un entry
      (★ Reveal/Copy/History/Files/Edit/Delete) se desborda del card en mobile — el
      `btn-group` no wrappea ni scrollea, se corta contra el borde

### Increment 13 — Identidad tipográfica
> Pedido del usuario a mitad del Increment 12: la paleta/tokens Workshop ya estaban
> cerrados desde el Increment 5, pero body/headings seguían en
> `'Helvetica Neue', Helvetica, Arial, sans-serif` — la pila más genérica posible, sin
> relación con la identidad "Workshop". Vía Bunny Fonts (mismo mecanismo de CDN que
> Bootstrap Icons desde el Increment 2; a diferencia de Google Fonts directo, no deja
> cookies ni manda el IP del visitante a Google — más apropiado para un gestor de
> contraseñas).
- [x] `--font-display` (Big Shoulders Display, condensada industrial) aplicada a
      `h1`/`h2`/`.navbar-brand` — refuerza el look "stamped label" que ya tenían por el
      `uppercase`/`letter-spacing` del Increment 5. `--font-sans` (Inter) para el resto
      del body. `--font-mono` (secretos) sin cambios
      → commit `feat: give headings an industrial condensed identity (Big Shoulders Display + Inter)`
- [x] Verificado en navegador (login/register): la mayúscula "stamped" pasó de
      Helvetica genérica a algo con carácter real

### Increment 14 — Responsive: VaultDetail y pasada de Health/Trash
> El shell (Increment 12) quedó sólido, pero `VaultDetail.razor` repite el patrón
> `d-flex justify-content-between` sin wrap en varias filas — solo se confirmó roto
> una (fila de entry) mirando el navegador a 390px. Se divide en un commit chico por
> sección en vez de uno solo grande, para poder revisar/mergear cada uno por separado.
- [ ] Fila de entry: el `btn-group` de acciones (★ Reveal Copy History Files Edit
      Delete) se corta contra el borde del card en mobile — **confirmado** con
      screenshot a 390px. Wrap o scroll horizontal contenido, sin recortar
- [ ] Panel de Members: fila de member (nombre/email + select de rol + Remove) y fila
      de "agregar member" (email + select + Add) — mismo patrón `d-flex` sin wrap.
      **Sin confirmar overflow real todavía** (el input de email se achica solo en la
      captura que se vio, pero queda muy apretado)
- [ ] Filas de historial de contraseña y de adjuntos (fecha/usuario o nombre de
      archivo + botones) — mismo patrón, esos paneles no se abrieron en mobile todavía
- [ ] Barra de filtros (tags + buscador + "Manage tags"): revisar que el buscador de
      ancho fijo (`14rem`) no rompa el wrap en pantallas angostas con varios tags
- [ ] Pasada de Health/Trash a 390px — no se miraron todavía, sumar un commit solo si
      aparece algo roto
- [ ] Pasada rápida de Vaults/Login/Register a 390px para confirmar que la tipografía
      nueva (Increment 13) no rompió el wrap en ningún lado (ya son grid de Bootstrap,
      debería ser gratis)

> **Evitar en todo el sprint:** texturas de madera, pegboard de fondo, nombres "cute" para
> secciones, ilustraciones custom fuera de empty states — eso es lo que lo hace ver
> amateur en vez de premium.

> **Límites de la auditoría del Increment 6, explícitos:** se hizo leyendo código, sin
> herramientas de navegador, así que los hallazgos de rendimiento están razonados y **no
> medidos**. El contraste (`--text-muted` ≈6.4:1, `.badge-chip` ≈5.9:1) es cálculo de
> luminancia, no medición. El responsive abajo de 641px no se evaluó — `app.css` ya declara
> desde el Sprint 5 que el shell es desktop-first. Tampoco se corrió un `publish`, así que el
> impacto de podar `lib/` está en archivos y no en KB. Dado que la auditoría del Increment 1
> falló justamente por leer archivos sin abrir la app, este límite pesa.

---

## 🟣 Sprint 21 — Login hardening: rate limiting + antiforgery · `feature/login-hardening`
> `docs/AUDITORIA.md` A3, A4, B2. Prioridad alta en la auditoría: "la pieza más barata de
> las de severidad alta, cierra una superficie de ataque real hoy mismo".

- [ ] Rate limiting en `POST /api/auth/login` y `/api/auth/register` —
      `Microsoft.AspNetCore.RateLimiting` (built-in desde .NET 7, sin dependencia nueva),
      ventana fija/deslizante por IP (+ por email en login, para no dejar fuerza bruta
      distribuida entre IPs). 429 tras N intentos en la ventana
- [ ] Antiforgery token (`AddAntiforgery()`) como capa adicional a `SameSite=Lax` (D6) en
      los endpoints que mutan estado vía cookie — defensa en profundidad, no reemplaza
      SameSite, que sigue siendo la primera línea
- [ ] `[MaxLength(128)]` en `RegisterRequest.Password` (B2) — oportunista, un atributo
- [ ] Tests (rate limit dispara 429, antiforgery rechaza request sin token) + PR a `main`

## 🟣 Sprint 22 — Headers de seguridad HTTP · `feature/security-headers`
> `docs/AUDITORIA.md` M5. Bajo costo, buena defensa en profundidad — hoy no hay
> `UseHsts()` ni `Content-Security-Policy` configurados en `Program.cs`.

- [ ] `UseHsts()` + middleware de headers: `Content-Security-Policy`,
      `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`,
      `Referrer-Policy: no-referrer`
- [ ] Verificación manual (DevTools ▸ Network ▸ Response headers) + PR a `main`

## 🟣 Sprint 23 — Hardening menor: adjuntos huérfanos + generador · `feature/minor-hardening`
> `docs/AUDITORIA.md` M2, M3, B1.

- [ ] `TrashService` (purga en cascada) llama `IAttachmentStorage.DeleteAsync` por cada
      `Attachment` de la entry purgada — cierra el `// ponytail:` ya marcado en
      `TrashService.cs:172-174` (adjuntos huérfanos en disco, no fuga de datos pero
      acumulación sin límite)
- [ ] `PasswordGenerator`: opción de excluir caracteres ambiguos (`l/1/I/O/0`) + toggle en
      la UI del generador
- [ ] B1 (asimetría: login oculta existencia de email, registro la confirma con
      `Conflict`) — trade-off de UX aceptado, no un fix; si se quiere dejar constancia,
      documentarlo en `DECISIONS.md`, si no, cerrar el hallazgo sin tocar código
- [ ] Tests + PR a `main`

## 🟣 Sprint 24 — TOTP en entradas guardadas + alertas de brechas (baja prioridad) · `feature/breach-alerts`
> `docs/AUDITORIA.md`, tabla "Features faltantes" — ambas Media prioridad, ninguna
> bloqueante. **No confundir con el Sprint 18** (2FA de la propia app): esto es TOTP
> *para las cuentas guardadas* (generador/lector estilo Bitwarden/1Password Authenticator)
> y alertas Have I Been Pwned (k-anonimato — solo se manda un prefijo del hash, nunca la
> contraseña ni el hash completo). M4 (`PasswordHealthChecker` heurístico, no
> dictionary-aware — limitación ya documentada en el propio código) es candidato a
> resolver en el mismo sprint si se vuelve a tocar el generador/health.
- [ ] Sin increments todavía — placeholder de roadmap, evaluar recién si la lista de
      arriba se vacía.

---

## 🔵 Transversal — Traducir a inglés · `feature/i18n-english`
> **Hacerla pronto** (no bloquea features pero la deuda crece con cada sprint). CLAUDE.md
> exige inglés en código/comentarios/docs. Pasar `docs/*.md` y los comentarios viejos de
> auth/encryption a inglés. Rama independiente, mergeable en cualquier momento.

> **Fuera de scope (post-roadmap):** modelo zero-knowledge (clave derivada del master /
> clave por vault, ver D3/D4), refresh tokens (D2), app móvil, extensión de navegador, offline.
