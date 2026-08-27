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
3 archivos con `@code` inline que quedaron sin tocar por no ser parte de este increment —
`Login.razor`, `Register.razor`, `Auth/RedirectToLogin.razor` — convertirlos en una pasada
aparte cuando toque. (`Vaults.razor`, `Trash.razor`, `Health.razor`, `Layout/MainLayout.razor`
y `Layout/NavMenu.razor` ya se convirtieron, en los increments 3, 6 y 8 del refactor visual —
ver `docs/UI-REFACTOR.md`.)

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

**Modal de confirmación:** `window.confirm` reemplazado por `ModalService`/`ModalHost` propio en
los 6 call-sites (Trash, `EntriesPanel` borrar tag, `EntryRow` borrar entrada,
`EntryAttachmentsPanel` borrar archivo, `MembersPanel` sacar miembro, `NotesPanel` borrar nota).
Cerrado, sin `window.confirm` restante en el cliente.

**Auditoría de seguridad (2026-08-13, `docs/AUDITORIA.md`):** base criptográfica sólida
(Argon2, AES-256-GCM, JWT en cookie `HttpOnly`, control de acceso centralizado), pero un
hallazgo crítico arquitectónico — el cifrado corría en el servidor con clave única para
todos los usuarios, **no era zero-knowledge** (C1). **Decisión tomada el 2026-08-14
(D7 en `DECISIONS.md`): migrar a zero-knowledge real.** El motivo que destrabó la
decisión de D3 ("evolución futura", sin fecha): The Shed puede terminar corriendo para
terceros no relacionados con quien lo hostea, y ahí el operador del servidor sí es parte
del modelo de amenaza — "self-hosted, confío en mi servidor" no alcanzaba. Scopeado como
4 sprints nuevos en `SPRINTS.md` (25-28: derivación de clave + keypair, vault key +
cifrado de entradas/notas, compartir vía key-wrapping asimétrico, migración de datos
existentes) — **prioridad real por delante de Sprint 17**, tocan la base de cifrado que
import/export y el resto van a usar. El resto de los hallazgos ya está incorporado al
roadmap: Sprint 19 se amplió (A1, reautenticación para revelar/copiar contraseñas) y se
sumaron los Sprints 21-24 (rate limiting + antiforgery, headers HTTP, adjuntos huérfanos
+ generador, TOTP en entradas guardadas + alertas HIBP a baja prioridad).

**Infraestructura del repo (2026-08-16).** Se sumó CI y versionado, que no existían:
`.github/workflows/ci.yml` corre `dotnet test` en cada push a `main` y en cada PR (un solo
comando alcanza: `TheShed.Tests` referencia a `Server`, que referencia a `Client` y `Shared`,
así que compila los cuatro proyectos). El versionado va con **MinVer** (`Directory.Build.props`,
`MinVerTagPrefix=v`): **el tag de git es la versión**, no hay número que mantener a mano —
`v0.2.0` estampa `0.2.0` en los ensamblados y los commits posteriores salen como
`0.2.1-alpha.0.N`. Release notes generadas por GitHub desde los commits convencionales, sin
changelog versionado en el repo (se descartó release-please: monta workflow + config + manifest
y no sabe actualizar un `.csproj` sin marcadores a mano). Tags publicados: `v0.1.0` (estado tras
el Sprint 20) y `v0.1.1` (fix de `Microsoft.AspNetCore.OpenApi` 10.0.2 → 10.0.11, que arrastraba
`Microsoft.OpenApi` 2.0.0 con el advisory NU1903/CVE-2026-49451; el riesgo real era nulo —
la vulnerabilidad es DoS al **parsear** documentos OpenAPI de fuentes no confiables, y The Shed
solo genera el suyo, con `MapOpenApi()` detrás de `IsDevelopment()`).

**Sprints 21+22 cerrados (2026-08-16, `feature/security-hardening`).** Rate limiting por IP en
login/register (429 tras 10 intentos / 5 min), `[MaxLength(128)]` en las contraseñas de ambos
DTOs de auth, `UseHsts()` y headers de seguridad con un CSP de `script-src` estricto. A3, B2 y M5
de la auditoría quedan cerrados; **A4 (antiforgery) sigue abierto a propósito** — es más grande de
lo que el plan asumía y va a su propio PR. Se eligieron por delante del Sprint 17 (import/export)
porque son ortogonales al cifrado: los Sprints 25-28 invalidarían el import/export construido
sobre `IEncryptionService`, pero no tocan el pipeline HTTP. Efecto colateral documentado en **D8**:
el CSP estricto obligó a apagar el fingerprinting de assets WASM.

**Sprint 25 — keypair RSA verificado end-to-end y arreglado (2026-08-19,
`feature/e2e-key-derivation`).** El feature de keypair (commits del 2026-08-18) tenía tests en
verde pero nunca se había probado en un navegador real. Al hacerlo, el registro rompía:
`RSA.Create()` y después `AesGcm` tiran `PlatformNotSupportedException` en browser-wasm — .NET
delega esas dos APIs al SO real (OpenSSL/CNG), y WASM no tiene salida ahí (PBKDF2/HMAC/SHA sí
son gestionados y andan bien, por eso el KDF nunca dio síntomas). Los tests no lo veían porque
corren sobre `net10.0` normal, no sobre WASM. Arreglado delegando ambas operaciones a la Web
Crypto API del navegador (`crypto.subtle`) vía JS interop —
`TheShed.Client/wwwroot/js/interop.js` (`generateRsaKeypair`, `encryptAesGcm`) +
`WebCryptoUserKeypairService` (`TheShed.Client/Services`) — manteniendo el mismo formato
SPKI/PKCS8/PEM y `nonce‖ciphertext‖tag` que ya usaba el servidor, así que quedan compatibles sin
tocar el wire format. Verificado registrando un usuario real en Chrome e inspeccionando el
payload de red (`window.fetch` hookeado): la privada nunca viaja en claro, solo como blob base64
cifrado. 185 tests. Detalle completo (glosario, causa raíz, diagrama del flujo) en un manual
publicado como Artifact ese día — pedir el link si hace falta releerlo, no está versionado en el
repo. **Importante para el Sprint 26:** el mismo problema de `AesGcm` en WASM le pega directo al
plan de reusar `AesEncryptionService` client-side para vault key/entradas/notas — quedó anotado
como bloqueante conocido (con la solución ya resuelta, mismo patrón Web Crypto) al principio de
la sección del Sprint 26 en `SPRINTS.md`.

**Sprint 26 cerrado (2026-08-25, `feature/e2e-vault-encryption`).** Vault key generada y envuelta
al crear un vault, cifrado de `PasswordEntry`/`SecureNote` movido al cliente con esa key,
listado/búsqueda de entradas movidos a client-side (el servidor ya no puede filtrar sobre
ciphertext), test end-to-end que prueba que lo guardado en el servidor no se puede reconstruir
sin la master password — bloqueante de `AesGcm`/WASM resuelto con el mismo patrón Web Crypto de
`interop.js` que ya se había armado en el Sprint 25. Verificado en navegador (mismo criterio que
Sprint 25: tests en verde no alcanza) y aparecieron 2 bugs que los tests no veían — `PasswordEntry
.Notes` y `SecureNote.Title` nunca se habían movido al cifrado client-side junto con sus campos
hermanos, viajaban y quedaban en texto plano. Arreglados en 2 commits separados (detalle completo,
incluido el ajuste de `SecureNoteService.ListAsync` que dejó de ordenar por `Title` server-side, en
`SPRINTS.md`). De paso se encontró y arregló un problema heredado del Sprint 25: el KDF
(`Rfc2898DeriveBytes`, 600k iteraciones de PBKDF2) corría interpretado en el hilo principal de
WASM y congelaba la pestaña ~70-90s en cada login/registro — pasó a `crypto.subtle.deriveBits`
(Web Crypto, nativo) vía `WebCryptoKeyDerivationService`, mismas iteraciones, sin freeze.

**Sprint 27 cerrado (`feature/e2e-vault-sharing`).** Compartir vaults vía key-wrapping
asimétrico: al agregar un miembro, el dueño pide su public key (`GET /api/users/public-key`),
envuelve la vault key con RSA-OAEP y sube un `VaultKeyWrap` para ese usuario; el miembro
desenvuelve su private key propia (AES, con su stretched master key) y con eso la vault key
(`IUserKeypairService.UnwrapKeyAsMemberAsync`) — verificado en navegador compartiendo un vault
entre dos cuentas reales. De paso, dos cosas que la investigación del sprint destapó:
- **Adjuntos** todavía cifraban server-side con la key global vieja (`IEncryptionService`) —
  quedó movido al mismo patrón client-side que entradas/notas desde el Sprint 26. Verificado
  subiendo y bajando un archivo entre las dos cuentas (byte a byte idéntico al original).
- **El reporte de salud de contraseñas estaba roto**, no solo desactualizado: desde el Sprint 26
  intentaba `_encryption.Decrypt` sobre `PasswordEntry.Password`, que ya es ciphertext
  client-side — nunca podía funcionar. Se eliminó `PasswordHealthService`/`HealthController`
  server-side y `Health.razor.cs` ahora corre `PasswordHealthChecker` (`Shared`) client-side,
  con unwrap eager de todos los vaults accesibles (`IVaultKeyResolver`, nuevo, compartido con
  `VaultDetail` para no duplicar la rama owner/member). Verificado con ambas cuentas.

Limitación conocida y aceptada (M1/D7): remover un miembro no rota la vault key — sí se borra
su `VaultKeyWrap` al removerlo (higiene, no rotación real). Migrar los vaults creados antes de
este sprint queda para el Sprint 28, que también es donde se apaga `Encryption:Key`/
`AesEncryptionService` del lado servidor (siguen vivos hasta entonces, para esa migración).

**Sprint 28 cerrado con alcance recortado (2026-08-27, `feature/e2e-migration`).** Decisión del
usuario: los vaults que existían antes del Sprint 26 eran todos datos de prueba, así que no
hacía falta migrarlos — se descartan. El sprint quedó reducido a apagar la superficie de
cifrado server-side que D3 había dejado viva (ya sin ningún consumidor real desde que los
Sprints 26/27 movieron el cifrado de entries/notes/attachments al cliente): sacado el wiring de
`IEncryptionService`/`EncryptionSettings`/`Encryption:Key` de `Program.cs` y
`appsettings.Example.json`, borrados ambos archivos (`EncryptionSettings.cs`,
`IEncryptionService.cs`). `AesEncryptionService` (Shared) se mantiene como clase concreta —
la siguen usando `ZeroKnowledgeE2ETests`/`AesEncryptionServiceTests` para simular el lado
cliente sin navegador. Build + suite completa (**211/211**) en verde.

**Próximo paso sugerido:** la pasada corta de mantenimiento de docs que venía pendiente:
tildar los ítems del Sprint 20 que ya están hechos en el código (ver la nota de desfasaje en
`SPRINTS.md`) y arrancar la rama de traducción a inglés, que crece con cada sprint.

### Fase 4 — Seguridad (cerrada)
- [x] Argon2 para hash de contraseña maestra
- [x] AES-256-GCM para cifrado de entradas (servicio + tests)
- [x] Autenticación JWT (DTOs, infraestructura, endpoints register/login) — verificado e2e
- [x] Tests de auth (AuthService + endpoints register/login)
- [x] API de entradas (`PasswordEntryService` + `EntriesController`) que consume el cifrado, con tests

### Pendiente transversal
- [ ] Rama de traducción: pasar docs (`docs/*.md`) y comentarios viejos de auth/encryption a inglés
