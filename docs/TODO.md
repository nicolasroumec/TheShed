# The Shed — Estado y Próximos Pasos

## Estado actual
- [x] Fase 1 — Renombramiento (SwimAnalytics → TheShed)
- [x] Fase 2 — Limpieza (modelos y docs de natación eliminados)
- [x] Fase 3 — Modelo de datos (entidades del gestor de contraseñas)
- [x] Fase 4 — Seguridad (Argon2 ✅, AES-256 ✅, JWT ✅, tests ✅, API de entradas ✅) — ver `SPRINTS.md`
- [~] Fase 5 — UI Blazor (auth ✅, vaults+entradas ✅, generador ✅, compartir vaults ✅) ← en curso

## Próximo paso
**Cerrar Sprint 8 — Compartir vaults** (`feature/vault-sharing`): código + tests hechos
(suite 67/67), falta el PR a `main`. Ver `SPRINTS.md`.

### Fase 4 — Seguridad (cerrada)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [x] Autenticación JWT (DTOs, infraestructura, endpoints register/login) — verificado e2e
- [x] Tests de auth (AuthService + endpoints register/login)
- [x] API de entradas (`PasswordEntryService` + `EntriesController`) que consume el cifrado, con tests

### Pendiente transversal
- [ ] Rama de traducción: pasar docs (`docs/*.md`) y comentarios viejos de auth/encryption a inglés
