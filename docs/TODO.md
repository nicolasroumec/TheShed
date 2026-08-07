# The Shed — Estado y Próximos Pasos

## Estado actual
- [x] Fase 1 — Renombramiento (SwimAnalytics → TheShed)
- [x] Fase 2 — Limpieza (modelos y docs de natación eliminados)
- [x] Fase 3 — Modelo de datos (entidades del gestor de contraseñas)
- [x] Fase 4 — Seguridad (Argon2 ✅, AES-256 ✅, JWT ✅, tests ✅, API de entradas ✅) — ver `SPRINTS.md`
- [~] Fase 5 — UI Blazor (auth ✅, vaults+entradas ✅, generador ✅, compartir vaults ✅, notas seguras ✅, tags ✅, UX de entradas ✅, historial de versiones ✅, papelera ✅, adjuntos ✅, salud de contraseñas ✅) ← en curso

## Próximo paso
Sprint 15 (`feature/password-health`) mergeado a `main` (PR #15). Sprint 16 (`feature/jwt-cookie`)
implementado y verificado e2e (navegador + curl): el JWT pasó de `localStorage` a una cookie
`HttpOnly`/`Secure`/`SameSite=Lax` (`AuthController` setea/borra la cookie, `GET /api/auth/me`
reemplaza la lectura del token en el cliente, `JwtBearerEvents.OnMessageReceived` la lee en el
server, se sacó el CORS `AllowAll`) — tests (176/176), decisión D6 documentada. Falta el PR a
`main` (ver detalle en `SPRINTS.md`). Siguiente: Sprint 17 — Importar/Exportar CSV
(`feature/import-export`).

### Fase 4 — Seguridad (cerrada)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [x] Autenticación JWT (DTOs, infraestructura, endpoints register/login) — verificado e2e
- [x] Tests de auth (AuthService + endpoints register/login)
- [x] API de entradas (`PasswordEntryService` + `EntriesController`) que consume el cifrado, con tests

### Pendiente transversal
- [ ] Rama de traducción: pasar docs (`docs/*.md`) y comentarios viejos de auth/encryption a inglés
