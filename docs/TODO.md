# The Shed — Estado y Próximos Pasos

## Estado actual
- [x] Fase 1 — Renombramiento (SwimAnalytics → TheShed)
- [x] Fase 2 — Limpieza (modelos y docs de natación eliminados)
- [x] Fase 3 — Modelo de datos (entidades del gestor de contraseñas)
- [x] Fase 4 — Seguridad (Argon2 ✅, AES-256 ✅, JWT ✅, tests ✅, API de entradas ✅) — ver `SPRINTS.md`
- [~] Fase 5 — UI Blazor (auth ✅, vaults+entradas ✅, generador ✅, compartir vaults ✅, notas seguras ✅, tags ✅, UX de entradas ✅, historial de versiones ✅, papelera ✅, adjuntos ✅, salud de contraseñas ✅) ← en curso

## Próximo paso
Sprint 15 (`feature/password-health`) implementado y verificado e2e en navegador: detector de
contraseñas débiles (`PasswordHealthChecker` en Shared, longitud/variedad/entropía simple),
detector de repetidas (`PasswordHealthService`, compara descifradas en memoria entre entradas de
los vaults del usuario, nunca logueadas), endpoint `GET /api/health/passwords` y página `/health`
con badges de fuerza + "Reused" — tests (172/172). Falta el PR a `main` (ver detalle en
`SPRINTS.md`). Siguiente: Sprint 16 — cookie httpOnly para el JWT (`feature/jwt-cookie`,
diseño ya planificado en `SPRINTS.md`, hoy el JWT vive en LocalStorage y es robable vía XSS).

### Fase 4 — Seguridad (cerrada)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [x] Autenticación JWT (DTOs, infraestructura, endpoints register/login) — verificado e2e
- [x] Tests de auth (AuthService + endpoints register/login)
- [x] API de entradas (`PasswordEntryService` + `EntriesController`) que consume el cifrado, con tests

### Pendiente transversal
- [ ] Rama de traducción: pasar docs (`docs/*.md`) y comentarios viejos de auth/encryption a inglés
