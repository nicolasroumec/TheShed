# The Shed — Estado y Próximos Pasos

## Estado actual
- [x] Fase 1 — Renombramiento (SwimAnalytics → TheShed)
- [x] Fase 2 — Limpieza (modelos y docs de natación eliminados)
- [x] Fase 3 — Modelo de datos (entidades del gestor de contraseñas)
- [x] Fase 4 — Seguridad (Argon2 ✅, AES-256 ✅, JWT ✅, tests ✅, API de entradas ✅) — ver `SPRINTS.md`
- [~] Fase 5 — UI Blazor (auth ✅, vaults+entradas ✅, generador ✅, compartir vaults ✅, notas seguras ✅, tags ✅, UX de entradas ✅, historial de versiones ✅, papelera ✅) ← en curso

## Próximo paso
Sprint 13 (`feature/trash`) cerrado y en `main`: `DeletedAt` en `AuditableEntity` (soft-delete
centralizado en `SaveChangesAsync`), `TrashService`/`TrashController` (listar/restaurar/purgar manual),
purga automática cada 24h (`TrashPurgeService`, `Trash:RetentionDays`), UI en `/trash` verificada
e2e en navegador — tests (138/138). Sin PR (ver nota en `SPRINTS.md`: push accidental directo a
`main`). Siguiente: Sprint 14 — adjuntos (`feature/attachments`). Ver detalle en `SPRINTS.md`.

### Fase 4 — Seguridad (cerrada)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [x] Autenticación JWT (DTOs, infraestructura, endpoints register/login) — verificado e2e
- [x] Tests de auth (AuthService + endpoints register/login)
- [x] API de entradas (`PasswordEntryService` + `EntriesController`) que consume el cifrado, con tests

### Pendiente transversal
- [ ] Rama de traducción: pasar docs (`docs/*.md`) y comentarios viejos de auth/encryption a inglés
