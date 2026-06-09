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

## 🔵 Sprint 2 — Auth: DTOs + infraestructura JWT · `feature/jwt-auth`
*(Incrementos 2-3 de `AUTH_FLOW.md`, ya especificados)*
- [ ] DTOs `Register/Login/AuthResponse` → commit `feat: add authentication DTOs`
- [ ] `JwtSettings` + `JwtTokenService` + wiring en `Program.cs`
      → commit `feat: configure JWT authentication`

## 🔵 Sprint 3 — Auth: endpoints register/login
*(Incremento 4 de `AUTH_FLOW.md`)*
- [ ] `AuthService` + `AuthController` + DI → commit `feat: add register and login endpoints`
- [ ] Verificación e2e (register 201/409, login 200/401, token válido en jwt.io)
- [ ] Docs de avance → commit `docs: document auth flow progress` · PR a `main`

## 🟣 Sprint 4 — API de entradas (CRUD `PasswordEntry`)
- [ ] `PasswordEntryService` + controller `[Authorize]` que **consume `IEncryptionService`**
      (cifra al crear/editar, descifra al leer) — acá se cierra el loop del cifrado
- [ ] Filtrado por vault + membresía

## 🟣 Sprint 5 — UI Blazor (Fase 5)
- [ ] Login/registro, listado de vaults, CRUD de entradas en el cliente WASM
