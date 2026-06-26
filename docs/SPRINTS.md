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
- [ ] PR a `main`

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
- [ ] PR a `main`

> Decisión de diseño: el listado devuelve solo metadata; la contraseña descifrada se entrega
> únicamente en `GET /api/entries/{id}` (estilo Bitwarden/1Password).

## ✅ Sprint 5 — UI: auth + tema · `feature/client-auth` + `feature/ui-theme`
- [x] `AuthService` + `JwtAuthenticationStateProvider` + `LocalStorageService`, guards de ruta
      (`AuthorizeRouteView` + `RedirectToLogin`), páginas Login/Register
      → commit `feat: add client auth (login/register, JWT state, route guards)`
- [x] Tema oscuro + app shell → commit `feat: add dark theme and app shell styling`
- [x] Paleta Workshop + design tokens → commit `feat: apply Workshop palette and design tokens`
- [ ] PR a `main`

## 🟢 Sprint 6 — Vaults API (CRUD `Vault` + membresías) · `feature/vaults-api`
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

## 🟢 Sprint 7 — UI: vaults + entradas (cliente WASM) · `feature/vaults-ui`
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
- [ ] PR a `main`
