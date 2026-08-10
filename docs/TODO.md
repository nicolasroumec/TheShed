# The Shed — Estado y Próximos Pasos

## Estado actual
- [x] Fase 1 — Renombramiento (SwimAnalytics → TheShed)
- [x] Fase 2 — Limpieza (modelos y docs de natación eliminados)
- [x] Fase 3 — Modelo de datos (entidades del gestor de contraseñas)
- [x] Fase 4 — Seguridad (Argon2 ✅, AES-256 ✅, JWT ✅, tests ✅, API de entradas ✅) — ver `SPRINTS.md`
- [~] Fase 5 — UI Blazor (auth ✅, vaults+entradas ✅, generador ✅, compartir vaults ✅, notas seguras ✅, tags ✅, UX de entradas ✅, historial de versiones ✅, papelera ✅, adjuntos ✅, salud de contraseñas ✅) ← en curso

## Próximo paso
Sprint 16 (`feature/jwt-cookie`) mergeado a `main` (PR #16). Sprint 20 (`feature/frontend`) se
adelantó al resto: increments 1-5 hechos — auditoría, íconos reales en el nav (Bootstrap Icons
no estaba instalado pese a lo que decía `UI.md` §6), empty states con voz workshop, componente
`Loading` único, y el remapeo de los `--bs-*` del tema oscuro de Bootstrap a los tokens Workshop
(la causa raíz del look genérico: sus grises fríos ganaban aunque las páginas usaran las clases
correctas). Cerrada la identidad visual, se auditó el cliente entero (Increment 6): 19 hallazgos,
ninguno breaking hacia afuera, que quedaron como increments 7-11 en la misma rama — correcciones
visibles (debounce de búsqueda, `ErrorBoundary`, `@key`), limpieza de código muerto, extracciones
y partir `VaultDetail` (986 líneas, la mitad del frontend). Se coló un pedido nuevo del usuario
(responsive mobile-first + navbar + identidad tipográfica) que en parte el propio Increment 6
ya había marcado como límite no evaluado: Increment 12 (shell mobile-first, top bar + menú
off-canvas) e Increment 13 (tipografía — Big Shoulders Display + Inter vía Bunny Fonts, en vez
de la pila Helvetica genérica) cerrados y verificados en navegador. Increment 14 (varios commits
chicos: filas de entry/members/historial/adjuntos y barra de filtros de `VaultDetail` a 390px,
más pasada de Health/Trash) es el próximo paso. **Decidir antes de arrancar el resto:** el Sprint 17 suma UI a `VaultDetail`, así
que o se parte antes o se parte a ~1.100 líneas. Después, volver al orden del roadmap: Sprint 17
— Importar/Exportar CSV (`feature/import-export`).

### Fase 4 — Seguridad (cerrada)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [x] Autenticación JWT (DTOs, infraestructura, endpoints register/login) — verificado e2e
- [x] Tests de auth (AuthService + endpoints register/login)
- [x] API de entradas (`PasswordEntryService` + `EntriesController`) que consume el cifrado, con tests

### Pendiente transversal
- [ ] Rama de traducción: pasar docs (`docs/*.md`) y comentarios viejos de auth/encryption a inglés
