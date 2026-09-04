# The Shed — Estado y Próximos Pasos

## Estado actual
- [x] Fase 1 — Renombramiento (SwimAnalytics → TheShed)
- [x] Fase 2 — Limpieza (modelos y docs de natación eliminados)
- [x] Fase 3 — Modelo de datos (entidades del gestor de contraseñas)
- [x] Fase 4 — Seguridad (Argon2, cifrado zero-knowledge client-side, JWT en cookie
      httpOnly, tests) — ver `SPRINTS.md`
- [~] Fase 5 — UI Blazor (auth, vaults+entradas, generador, compartir vaults, notas
      seguras, tags, historial, papelera, adjuntos, salud de contraseñas, identidad
      visual "Workshop", responsive mobile-first, session lock, PWA instalable) ←
      funcionalmente completa, quedan los pendientes de abajo

El detalle sprint por sprint (qué se hizo, cuándo, por qué) vive en `SPRINTS.md`
(lista "Shipped" al final) y en `git log` — no se duplica acá.

## Pendiente, por orden de daño (auditoría 2026-08-31, cerrada 2026-09-04)

1. 🟢 **Antiforgery (A4)** implementado en `feature/antiforgery` (Sprint 32, ver
   `AUDITORIA.md`/`DECISIONS.md` D10) — PR abierto, pendiente de merge.
2. 🔵 **No hay tests de integración.** Los ~230 son unitarios sobre EF InMemory; cero
   `WebApplicationFactory`. Plan armado en `SPRINTS.md` Sprint 33 (`WebApplicationFactory` +
   SQLite in-memory). Cubre el pipeline del servidor — los bugs client-side (`AesGcm` en
   WASM, campos en claro, F5/reauth) los sigue agarrando solo el navegador, no esto.
3. 🟡 **Deuda de idioma**: `docs/*.md` y comentarios viejos en español, contra el propio
   `CLAUDE.md`. Rama `feature/i18n-english` en `SPRINTS.md`, mergeable en cualquier momento.
4. 🟢 `Login.razor`, `Register.razor` y `Auth/RedirectToLogin.razor` siguen con `@code`
   inline (convención de code-behind adoptada a mitad del Sprint 20, no repasada ahí).
5. 🟢 Sprint 23 (adjuntos huérfanos, caracteres ambiguos en el generador) sin tocar.

## Próximo paso
Import/export (Sprint 17) mergeado a `main` (PR #28) — el plan había quedado escrito contra
`IEncryptionService` (Sprint 28 lo borró) pero se reescribió client-side antes de shippear;
la doc solo tenía el registro atrasado. Antiforgery (A4, Sprint 32) con PR abierto, pendiente
de merge. PWA (Sprint 31, PR #27) y session lock (Sprint 30, PR #26) también cerrados.
Siguiente en la cola: tests de integración (Sprint 33) → rama de traducción
(`feature/i18n-english`).
