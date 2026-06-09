# The Shed — Estado y Próximos Pasos

## Estado actual
- [x] Fase 1 — Renombramiento (SwimAnalytics → TheShed)
- [x] Fase 2 — Limpieza (modelos y docs de natación eliminados)
- [x] Fase 3 — Modelo de datos (entidades del gestor de contraseñas)
- [~] Fase 4 — Seguridad (Argon2 ✅, AES-256 ✅, JWT ⬜) — ver `SPRINTS.md`
- [ ] Fase 5 — UI Blazor

## Próximo paso
**Fase 4 — Seguridad** (desglose y estado detallado en `SPRINTS.md`)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [ ] Autenticación JWT (DTOs, infraestructura, endpoints register/login) ← siguiente
- [ ] API de entradas que consuma el cifrado (Sprint 4)
