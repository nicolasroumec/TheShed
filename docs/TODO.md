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
más pasada de Health/Trash) es el próximo paso, pero se decidió partir `VaultDetail.razor`
(1077 líneas, 6 responsabilidades sin relación) antes de seguir, para no hacer la pasada mobile
sobre código a punto de reorganizarse. Plan aprobado y guardado en
`C:\Users\Nicolas\.claude\plans\swift-wiggling-hollerith.md`: 5 componentes nuevos bajo
`Components/Vault/` (`EntriesPanel`, `EntryRow`, `EntryHistoryPanel`, `EntryAttachmentsPanel`,
`NotesPanel`, `MembersPanel`), sin precedente de split de página en este codebase (patrón
"smart components" que inyectan sus propios servicios, `EventCallback` solo donde un hijo
muta estado del padre). De paso elimina el banner compartido `_actionError` (cada panel tiene
su error local) y corrige un bug: hoy fallar al sacar un miembro tira el error lejos del panel.
5 increments separados (A: MembersPanel, B: NotesPanel, C: EntriesPanel contenedor, D: EntryRow,
E: History/Attachments panels), parando para review/commit después de cada uno.

Nueva convención de código (decidida a mitad del Increment A, aplica a todo el proyecto en
adelante): **todo `.razor` va con code-behind** — markup en `Foo.razor`, lógica (`@code`,
`@inject` como `[Inject]`, `@implements` como interfaz de la partial class) en `Foo.razor.cs`.
Ya no se permite `@code { }` inline. Anotado en `CLAUDE.md` §Code conventions. Aplicado a
`Loading.razor` (único componente que ya existía) y a todo lo nuevo de este split. **Pendiente**:
8 archivos con `@code` inline que quedaron sin tocar por no ser parte de este increment —
`Vaults.razor`, `Trash.razor`, `Health.razor`, `Login.razor`, `Register.razor`,
`Layout/MainLayout.razor`, `Layout/NavMenu.razor`, `Auth/RedirectToLogin.razor` — convertirlos
en una pasada aparte cuando toque.

- [x] Increment A — `MembersPanel` extraído (`Components/Vault/`), fix de errores de
      add/remove miembro unificados en `_memberError`
- [x] Increment B — `NotesPanel` extraído, `_notesListError` separado del `_noteError` del form
- [x] Increment C — `EntriesPanel` contenedor (lista + filtro + tags + form, sin `EntryRow`
      todavía). `VaultDetail` quedó en ~35 líneas de code-behind. Ajuste sobre el plan original:
      `SubmitAsync` mantiene la limpieza de `_revealed`/`_history`/`_historyOpenIds` post-save
      (el plan decía sacarla ya) porque esos diccionarios siguen indexados por `entryId` a nivel
      de `EntriesPanel` hasta el Increment E — sacarla antes hubiera dejado una ventana de
      regresión (password/historial viejo visible tras editar).
- [x] Increment D — `EntryRow` extraído con contrato `EventCallback` hacia `EntriesPanel`
      (favorite/delete/edit). Ajuste sobre el plan: historial y adjuntos se mudaron con la fila
      (no podían quedar separados) como estado local por instancia, y ya quedó implementado el
      diffing por `PasswordChangedAt` en `OnParametersSet` que invalida reveal/historial cuando
      el padre recarga la lista tras un edit — así no queda ventana de regresión hasta el E.
- [x] Increment E — `EntryHistoryPanel`/`EntryAttachmentsPanel` separados de `EntryRow`. Mover
      mecánico como estaba previsto (la invalidación ya se había resuelto en el D); cada uno
      hace fetch lazy en `OnParametersSetAsync` (`if (IsOpen && _history is null)`) en vez del
      toggle síncrono que tenían en `EntryRow`.

**Split completo.** `VaultDetail` quedó en 70 líneas totales (razor+cs, era 1077 en un solo
archivo). Archivos finales bajo `Components/Vault/`: `EntriesPanel` (146+286 líneas — el más
grande, por el acoplamiento tags/filtro/form), `EntryRow` (52+93), `EntryHistoryPanel` (29+49),
`EntryAttachmentsPanel` (46+82), `NotesPanel` (85+134), `MembersPanel` (53+85). Build limpio en
cada increment. **Falta**: verificación manual en navegador (crear/editar/eliminar entrada,
reveal/copy, historial y adjuntos —cerrar/reabrir sin refetch de más—, notas, miembros) antes
de dar el split por probado end-to-end — no se hizo por pedido explícito del usuario de no
levantar el navegador en este tramo.

**Increment 14 completo** (3 commits, sin pausar entre ellos a pedido del usuario — "modifica
todo junto, luego probamos"). `EntryRow` ya estaba bien a 390px gracias a la regla mobile
genérica de `list-group-item`/`btn-group` (commit previo al split). Lo que faltaba:
- `MembersPanel` y `Health.razor` tenían las clases flex puestas directo en `.list-group-item`
  en vez de en un div hijo (a diferencia de `EntryRow`/`Trash`/`NotesPanel`) — restructurados
  al mismo patrón para reusar la regla existente en vez de CSS nueva.
- La regla de stacking usaba selector de hijo directo (`>`), que no llegaba a las filas de
  `EntryHistoryPanel`/`EntryAttachmentsPanel` (anidadas un nivel más adentro dentro del
  `.list-group-item` de `EntryRow`) — cambiado a descendiente.
- Barra de filtros de `EntriesPanel`: "Manage tags" usa `ms-auto` para pegarse a la derecha en
  desktop; al envolver a su propia línea en mobile quedaba flotando a la derecha de una línea
  vacía — reseteado en el breakpoint.

De paso aparecieron 3 bugs sin relación con mobile, corregidos en el mismo tramo:
- **"New entry" no abría el formulario** — `VaultDetail.razor` llama `_entriesPanel.StartCreate()`
  directo por `@ref`; el método mutaba estado sin `StateHasChanged()`, y como el evento pertenece
  al padre, Blazor nunca re-renderizaba `EntriesPanel`.
- **Botón `.btn-primary` disabled se veía azul** (Bootstrap default) en vez del acento del tema —
  el remap de tokens no restablecía `--bs-btn-disabled-*`.
- **Menú mobile se veía "gris" al desplegarse** — dos causas: (1) `.navbar-toggler` nunca se
  retematizó (Bootstrap hardcodea su borde/foco a un rgba blancuzco vía `.navbar-dark`, mismo
  patrón que el bug del botón disabled); (2) `.nav-backdrop` tiene `z-index` explícito pero
  `.top-row`/`.nav-scrollable` no, así que el backdrop pintaba *encima* de todo el menú en vez
  de solo detrás — el menú entero se veía lavado a través del dim. Se sumó `--bg-rgb` como token
  (mismo patrón que `--bs-primary-rgb`) para que el backdrop derive del tema en vez de un rgba
  hardcodeado. También se sacó la barra de `MainLayout` que solo mostraba el username (pedido
  del usuario, quedaba como una tira vacía redundante en mobile bajo la barra del nav).

Evaluada la idea de un bottom tab bar para mobile en vez del off-canvas actual — descartada por
ahora, se mantiene el menú desplegable.

**Próximo paso — reemplazar `window.confirm` por un modal propio.** 6 call-sites usan
`JS.InvokeAsync<bool>("confirm", ...)` (Trash, `EntriesPanel` borrar tag, `EntryRow` borrar
entrada, `EntryAttachmentsPanel` borrar archivo, `MembersPanel` sacar miembro, `NotesPanel`
borrar nota) — diálogo nativo del navegador, rompe el tema oscuro. Servicio genérico en
nombre y parámetros, no en contenido: se evaluó un modal con `RenderFragment` arbitrario
(formularios, etc.) pero eso solo se arma limpio desde markup `.razor`, y los 6 call-sites
actuales llaman desde `.razor.cs` puro (sin markup) — se agrega soporte a contenido arbitrario
el día que haya un caso concreto que lo pida, no antes. Planificado en 2 commits:
- [ ] Increment 1 — infraestructura + primer uso real: `Services/ModalService.cs` (scoped,
      `Task<bool> ConfirmAsync(string message, string title = "Confirm", string confirmText = "Confirm", string confirmVariant = "danger")`
      con `TaskCompletionSource`, async en vez del bloqueo de `window.confirm`; nombre e
      interfaz genéricos para poder sumar `AlertAsync(...)` u otros al lado con la misma
      cañería el día que haga falta). `Components/ModalHost.razor`+`.razor.cs` (modal
      Bootstrap, ya hereda el tema vía el remap de `--bs-body-bg` etc. que ya existe en
      `app.css`); registro en `Program.cs` + un solo `<ModalHost />` montado en
      `MainLayout.razor`; convertir `EntryRow` (delete entry) en el mismo incremento para
      probar el mecanismo con un uso real.
- [ ] Increment 2 — resto de call-sites (mecánico, mismo patrón que el 1): `NotesPanel`,
      `MembersPanel`, `EntryAttachmentsPanel`, `EntriesPanel` (delete tag), `Trash`. Sacar
      `IJSRuntime JS` donde ya no queda usándose para nada más (Trash, `EntriesPanel`,
      `MembersPanel`, `NotesPanel` — lo inyectaban solo para el `confirm`); se mantiene en
      `EntryAttachmentsPanel` (downloadFile) y `EntryRow` (copyToClipboard).

Después de esto, volver al orden del roadmap: Sprint 17 — Importar/Exportar CSV
(`feature/import-export`).

### Fase 4 — Seguridad (cerrada)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [x] Autenticación JWT (DTOs, infraestructura, endpoints register/login) — verificado e2e
- [x] Tests de auth (AuthService + endpoints register/login)
- [x] API de entradas (`PasswordEntryService` + `EntriesController`) que consume el cifrado, con tests

### Pendiente transversal
- [ ] Rama de traducción: pasar docs (`docs/*.md`) y comentarios viejos de auth/encryption a inglés
