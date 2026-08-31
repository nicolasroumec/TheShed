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

## Pendiente, por orden de daño (auditoría 2026-08-31)

1. 🟡 **El plan del Sprint 17 (import/export) quedó obsoleto**: está escrito sobre
   `IEncryptionService`, que el Sprint 28 borró. Hay que reescribirlo client-side. Es la
   feature que más falta — sin export, un gestor zero-knowledge no tiene salida de emergencia.
2. 🟡 **Antiforgery (A4)** sigue abierto, como se decidió en el Sprint 21+22 — va en su
   propio PR.
3. 🟡 **No hay tests de integración.** Los ~220 son unitarios sobre EF InMemory; cero
   `WebApplicationFactory`, cero bUnit. Los bugs más caros del proyecto (`AesGcm` en WASM,
   campos viajando en claro, bugs de F5/reauth) los encontró el navegador, no la suite —
   verificar cada feature en navegador sigue siendo obligatorio, no opcional.
4. 🟡 **Deuda de idioma**: `docs/*.md` y comentarios viejos en español, contra el propio
   `CLAUDE.md`. Rama `feature/i18n-english` en `SPRINTS.md`, mergeable en cualquier momento.
5. 🟢 `Login.razor`, `Register.razor` y `Auth/RedirectToLogin.razor` siguen con `@code`
   inline (convención de code-behind adoptada a mitad del Sprint 20, no repasada ahí).
6. 🟢 Sprint 23 (adjuntos huérfanos, caracteres ambiguos en el generador) sin tocar.

## Próximo paso
PWA (Sprint 31) mergeado a `main` (PR #27) — falta instalar de verdad en desktop/Android
para confirmar el `beforeinstallprompt` real (no automatizable desde acá). Sprint 30
(session lock) también cerrado y mergeado (PR #26). Siguiente en la cola: reescribir el
Sprint 17 (import/export) client-side → antiforgery (A4) → rama de traducción.
