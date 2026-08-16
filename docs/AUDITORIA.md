# Auditoría de seguridad y features — The Shed
Fecha: 2026-08-13
Alcance revisado: `TheShed.Server/Security/*`, `TheShed.Server/Services/*`, `TheShed.Server/Controllers/*`,
`TheShed.Server/Data/TheShedContext.cs`, `TheShed.Server/Program.cs`, `TheShed.Client/Program.cs`,
`TheShed.Client/Auth/JwtAuthenticationStateProvider.cs`, `TheShed.Client/Services/AuthService.cs`,
`TheShed.Client/Pages/Login.razor`, `Register.razor`, `TheShed.Client/Components/Vault/EntryRow.razor.cs`,
`TheShed.Shared/Models/Entities/*`, `TheShed.Shared/Models/DTOs/Auth/*`, `TheShed.Shared/Helpers/PasswordGenerator.cs`,
`TheShed.Shared/Helpers/PasswordHealthChecker.cs`, `appsettings*.json`, `.gitignore`, todos los `*.csproj`,
`TheShed.Tests/Security/AesEncryptionServiceTests.cs`, `TheShed.Tests/Services/AuthServiceTests.cs`,
`docs/PRODUCT.md`, `docs/ARCHITECTURE.md`, `docs/TODO.md`.

No se leyeron línea por línea: `TagService.cs`, `TagsController.cs`, `NotesController.cs`, `TrashController.cs`,
`HealthController.cs` (sí se verificó que todos tienen `[Authorize]`), componentes Razor de UI restantes
(`VaultDetail`, `EntriesPanel`, `MembersPanel`, `NotesPanel`, paneles de historial/adjuntos), migraciones EF.

## Resumen ejecutivo
La base criptográfica está bien construida: Argon2 para la contraseña maestra, AES-256-GCM con nonce
aleatorio por operación para las entradas, JWT en cookie `HttpOnly`/`Secure`/`SameSite=Lax`, secretos
fuera del repo (User Secrets / variables de entorno, `.gitignore` correcto), control de acceso a vaults
centralizado y aplicado de forma consistente, tests que cubren casos de manipulación de ciphertext y
clave incorrecta. **El hallazgo que domina todo lo demás es arquitectónico, no un bug**: el cifrado
ocurre en el servidor con una clave que el servidor controla, por lo que **no es zero-knowledge** —
quien comprometa el servidor, la base de datos o la clave de cifrado puede leer todas las contraseñas de
todos los usuarios sin necesitar la contraseña maestra de nadie. Esto es una decisión de diseño explícita
(documentada en `ARCHITECTURE.md`), no un descuido, pero es la diferencia fundamental frente a
Bitwarden/1Password/Proton Pass y debería ser una decisión consciente y comunicada, no implícita.
Recomendación principal: evaluar mover la derivación de clave y el cifrado/descifrado al cliente
(WASM ya está ahí) antes de sumar más superficie sobre el modelo actual.

## Hallazgos críticos 🔴

### C1 — Arquitectura no zero-knowledge: el servidor puede descifrar todas las contraseñas
**Ubicación:** `TheShed.Server/Security/AesEncryptionService.cs:18-35`, `EncryptionSettings.cs`,
`TheShed.Server/Services/PasswordEntryService.cs:317-331` (`ToResponse` decripta en el servidor),
`docs/ARCHITECTURE.md:10` ("clave de servidor en User Secrets").

La clave AES-256 vive en el servidor (User Secrets en dev, variable de entorno en prod) y es la **misma
clave para todos los usuarios** — no se deriva de la contraseña maestra de cada uno. El servidor descifra
cada entrada en cada `GET` (`PasswordEntryService.ToResponse`, `SecureNoteService.ToResponse`,
`AttachmentService.DownloadAsync`). Esto significa:
- Un admin del servidor, un atacante con RCE, un dump de la base + la clave, o una orden judicial contra
  el operador del servicio permiten leer **todas** las contraseñas de **todos** los usuarios en texto plano.
- La contraseña maestra del usuario solo protege el *login*, no protege los datos: quien tiene la clave
  de cifrado no necesita la contraseña maestra de nadie para leer el vault.
- Esto contradice el modelo que un usuario esperaría de un "gestor de contraseñas" moderno (Bitwarden,
  1Password, Proton Pass son todos zero-knowledge: el cifrado ocurre en el cliente con una clave derivada
  de la contraseña maestra, que el servidor nunca ve).

**Impacto:** compromiso total del modelo de confianza, no de un endpoint puntual.

**Cómo corregirlo (dirección, no receta completa):** mover cifrado/descifrado al cliente Blazor WASM
usando `crypto.subtle` (Web Crypto API vía JS interop, o `System.Security.Cryptography` que también corre
en WASM) con una clave derivada de la contraseña maestra vía Argon2id/PBKDF2 en el cliente. El servidor
pasaría a almacenar y servir blobs opacos, sin poder nunca descifrarlos. Es un cambio de arquitectura
grande (afecta compartir vaults, búsqueda server-side, historial, adjuntos), por eso se marca como
hallazgo crítico y no como tarea suelta: amerita una decisión explícita del producto, no un parche.

## Hallazgos altos 🟠

### A1 — Sin reautenticación para acciones sensibles
**Ubicación:** `TheShed.Client/Components/Vault/EntryRow.razor.cs:52-65` (`ToggleRevealAsync`,
`CopyPasswordAsync`), `TheShed.Server/Controllers/EntriesController.cs:29-34,99-105`.

Revelar una contraseña, copiarla o ver una versión del historial solo requiere que la cookie JWT siga
vigente (60 minutos, `Jwt:ExpiryMinutes` en `appsettings.json:15`) — no se vuelve a pedir la contraseña
maestra. En un gestor de contraseñas profesional, ver/exportar una contraseña suele exigir reautenticación
o, como mínimo, un desbloqueo de sesión reciente. Con la sesión abierta (ej. una laptop desatendida,
un XSS futuro, o un token robado), un atacante tiene acceso irrestricto a todo el vault sin fricción
adicional.

### A2 — Sin auto-bloqueo por inactividad
**Ubicación:** no encontrado en `TheShed.Client/Layout/MainLayout.razor.cs` ni en ningún otro componente;
`docs/PRODUCT.md:17` lo lista como funcionalidad prevista ("Cierre automático de sesión por inactividad").

La única expiración de sesión es el JWT de 60 minutos. No hay temporizador de inactividad en el cliente,
ni un segundo "lock" de UI que pida de nuevo la contraseña maestra tras N minutos sin actividad, como sí
tienen Bitwarden/1Password. Feature planeada, no implementada — ver también sección "Features faltantes".

### A3 — Sin rate limiting ni bloqueo progresivo en login
**Ubicación:** `TheShed.Server/Services/AuthService.cs:45-59`, `TheShed.Server/Program.cs` (no hay
`AddRateLimiter` ni middleware equivalente registrado).

`LoginAsync` no lleva cuenta de intentos fallidos ni aplica demoras. Nada impide fuerza bruta o
credential-stuffing contra `/api/auth/login` a la velocidad que permita la red. Argon2 en el hash mitiga
el costo de crackear un hash robado, pero no protege el endpoint de login en sí mismo.

**✅ Resuelto (2026-08-16, Sprint 21).** `AddRateLimiter` en `Program.cs` con una policy de ventana fija
particionada por IP (10 intentos / 5 min, configurable vía `RateLimiting:AuthPermitLimit` y
`AuthWindowMinutes`), aplicada con `[EnableRateLimiting]` sobre `Register` y `Login`. Verificado contra
el servidor corriendo: intentos 1-10 → 401, del 11 en adelante → 429.
**Limitación conocida:** particiona solo por IP, no por email. El middleware de rate limiting corre antes
del model binding, así que el email todavía no está parseado en ese punto; fuerza bruta distribuida entre
muchas IPs sigue pasando. Hacerlo requiere un contador dentro de `AuthService` — marcado con `ponytail:`
en `Program.cs`. Detrás de un reverse proxy hace falta `UseForwardedHeaders` para ver la IP real.

### A4 — Sin protección CSRF explícita pese a sesión por cookie
**Ubicación:** `TheShed.Server/Controllers/AuthController.cs:65-75` (`SetAuthCookie`,
`SameSite = SameSiteMode.Lax`), sin `[ValidateAntiForgeryToken]` ni middleware antiforgery en
`Program.cs`.

`SameSite=Lax` mitiga la mayoría de los casos (bloquea POST/PUT/DELETE cross-site), pero es la única
defensa. No hay un token antiforgery de doble verificación como capa adicional, que es la práctica
recomendada cuando la autenticación vive en una cookie. Riesgo real hoy: bajo-medio (mitigado por Lax),
pero es una dependencia frágil de un solo mecanismo del navegador.

**🔵 Abierto — pospuesto deliberadamente (2026-08-16, Sprint 21).** Se evaluó junto con A3/M5 y se dejó
afuera por tamaño, no por olvido: el plan lo trataba como un ítem del mismo peso que los otros dos y no
lo es. Con un cliente WASM puro y auth por cookie, `AddAntiforgery()` no alcanza — hace falta un endpoint
que emita el token, que el cliente lo lea y lo reenvíe como header en cada mutación (patrón double-submit
completo). Sigue siendo defensa en profundidad: `SameSite=Lax` (D6) es la primera línea y ya está puesta.
Va en su propio PR.

## Hallazgos medios 🟡

### M1 — Clave de cifrado única y estática para todos los usuarios, sin rotación
**Ubicación:** `TheShed.Server/Security/EncryptionSettings.cs`, `AesEncryptionService.cs:18-35`.

Además del problema de fondo (C1), no hay versión de clave, ni mecanismo de rotación (`kid`/key-id
en el layout `nonce || ciphertext || tag`), ni forma de re-cifrar datos existentes si la clave se
compromete y hay que rotarla. Cambiar la clave hoy invalidaría todos los datos ya cifrados.

### M2 — Adjuntos huérfanos en disco al purgar en cascada
**Ubicación:** `TheShed.Server/Services/TrashService.cs:170-178` (comentario `ponytail` explícito
reconociendo el problema).

Cuando se purga una `PasswordEntry` desde la papelera, la fila de `Attachment` cae por cascada en la
base de datos, pero el archivo cifrado en `IAttachmentStorage` nunca recibe un `DeleteAsync`. No es una
fuga de datos (el archivo sigue cifrado), pero es acumulación de blobs huérfanos sin límite.

### M3 — Generador de contraseñas sin opción de excluir caracteres ambiguos
**Ubicación:** `TheShed.Shared/Helpers/PasswordGenerator.cs:10-13`.

Los pools de caracteres son fijos (no hay flag para excluir `l/1/I/O/0`). Funcionalidad menor pero
esperable en generadores de nivel profesional, y mencionada en el checklist de referencia de la
industria.

### M4 — `PasswordHealthChecker` es heurístico, no dictionary-aware
**Ubicación:** `TheShed.Shared/Helpers/PasswordHealthChecker.cs:11-13` (comentario propio del código:
"Heuristic, not a full zxcvbn-style dictionary/pattern check").

La fortaleza se calcula por longitud + variedad de clases + entropía estimada. No detecta patrones
comunes (`Passw0rd!`, `Qwerty123!`) que pasarían como "Strong" pese a ser triviales de crackear con
diccionario. Correctamente documentado como limitación conocida en el propio código — no es un
hallazgo oculto, pero vale la pena que quede en el radar de producto.

### M5 — Sin headers de seguridad HTTP explícitos
**Ubicación:** `TheShed.Server/Program.cs` — no hay `UseHsts()`, ni configuración de
`Content-Security-Policy`, `X-Frame-Options`, `X-Content-Type-Options`, etc.

No es grave por sí solo (la app hoy no maneja contenido de terceros ni iframes), pero para una app que
maneja secretos de alto valor, headers de defensa en profundidad (CSP en particular, para mitigar el
impacto de un XSS futuro) son una práctica estándar ausente.

**✅ Resuelto (2026-08-16, Sprint 22).** `UseHsts()` (solo fuera de Development) más un middleware con
`X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: no-referrer` y un CSP
completo. `script-src` quedó estricto (`'self' 'wasm-unsafe-eval'`, sin `unsafe-inline`/`unsafe-eval`),
que es la parte que realmente frena XSS. Verificado en navegador: la app arranca, fuentes e íconos de CDN
cargan, el login hace su POST y no hay violaciones en consola.
**Costo colateral:** el CSP estricto obligó a apagar el fingerprinting de assets WASM — ver **D8** en
`DECISIONS.md`.
**Limitación conocida:** `style-src` mantiene `'unsafe-inline'` porque 8 componentes usan atributos
`style=""`. Marcado con `ponytail:` en `Program.cs`; sacarlo es mover esos estilos a clases.

## Hallazgos bajos / mejoras menores 🟢

### B1 — Filtrado de existencia de email en registro
**Ubicación:** `TheShed.Server/Controllers/AuthController.cs:22-25` (`Conflict("El email ya está
registrado.")`).

El login ya evita enumeración (siempre "Credenciales inválidas", ver `AuthController.cs:36`,
`AuthService.cs:50-52` — bien hecho). El registro sí confirma si un email ya existe, lo cual es un
trade-off común y aceptable en la mayoría de productos (UX de registro > riesgo de enumeración), pero
vale mencionarlo como asimetría.

### B2 — Sin límite superior explícito de longitud en `Password` de `RegisterRequest`
**Ubicación:** `TheShed.Shared/Models/DTOs/Auth/RegisterRequest.cs:13` (`[Required, MinLength(8)]`,
sin `MaxLength`).

No hay tope de longitud en la contraseña maestra antes de pasar a Argon2. Riesgo bajo (Argon2 no es
vulnerable a DoS por string largo en la práctica típica de este volumen), pero es una validación de
entrada ausente en un límite de confianza.

**✅ Resuelto (2026-08-16, Sprint 21).** `[MaxLength(128)]` en `RegisterRequest.Password` **y también en
`LoginRequest.Password`**, que el hallazgo no mencionaba: login alimenta el mismo Argon2 y es el endpoint
que un anónimo puede golpear libremente, así que arreglarlo solo en registro dejaba la mitad abierta.
Cubierto por `TheShed.Tests/Security/AuthRequestValidationTests.cs`.

## Features faltantes

| Feature | Estado | Prioridad sugerida |
|---|---|---|
| Generador de contraseñas configurable | Completo | — |
| Indicador de fortaleza de contraseña | Completo (heurístico, ver M4) | Baja |
| Detección de contraseñas duplicadas/reusadas | Completo (`PasswordHealthService.cs:34-38`) | — |
| Detección de contraseñas débiles guardadas | Completo | — |
| Auto-bloqueo por inactividad | Ausente (ver A2) | Alta |
| Reautenticación para acciones sensibles | Ausente (ver A1) | Alta |
| Rate limiting / bloqueo progresivo en login | Completo — por IP, no por email (ver A3) | — |
| 2FA/MFA para desbloquear la app | Ausente — campos `TwoFactorEnabled`/`TwoFactorSecret` existen en `User` (`TheShed.Shared/Models/Entities/User.cs:13-14`) pero sin ninguna lógica que los use | Alta |
| TOTP integrado (generador/lector para las cuentas guardadas) | Ausente | Media |
| Alertas de brechas (Have I Been Pwned, k-anonimato) | Ausente | Media |
| Carpetas/tags/favoritos/búsqueda | Completo (vaults + tags + `IsFavorite` + búsqueda por nombre/usuario/URL) | — |
| Compartir vault con cifrado extremo a extremo | Parcial: se comparte el vault (roles lectura/escritura, `VaultAccessService.cs`), pero al no ser E2E (ver C1) "compartir" es solo un permiso de fila en la base, no un intercambio de claves | Depende de resolver C1 primero |
| Historial de versiones por entrada | Completo (`EntryHistory`, `PasswordEntryService.cs:224-271`) | — |
| Notas seguras / adjuntos | Completo | — |
| Importar desde otros gestores (CSV) | Ausente — planeado como Sprint 17 en `docs/TODO.md:130` | Media (ya priorizado por el equipo) |
| Exportar entradas propias | Ausente | Media |
| Autocompletado navegador / apps móviles | Fuera de alcance explícito (`docs/PRODUCT.md:61-63`) | — (decisión de producto) |

## Calidad de código

- **Cifrado bien aislado y testeado**: `AesEncryptionService` no tiene lógica de UI mezclada, y
  `TheShed.Tests/Security/AesEncryptionServiceTests.cs` cubre manipulación de ciphertext, clave
  incorrecta, clave de longitud errónea y datos truncados — el estándar de testing acá es sólido.
- **Control de acceso centralizado**: `IVaultAccessService.GetAccessAsync` (`VaultAccessService.cs:14-37`)
  es el único punto de verdad para permisos de vault, y todos los servicios que tocan datos de vault
  (`PasswordEntryService`, `SecureNoteService`, `AttachmentService`, `TrashService`) pasan por él antes
  de leer o escribir. Patrón consistente, reduce el riesgo de IDOR.
- **Ocultamiento de existencia consistente**: vaults/entradas sin acceso devuelven `NotFound` en vez de
  `Forbidden` cuando el usuario no debería saber que existen (`PasswordEntryService.cs:26-31`,
  `VaultService.cs:230-243`) — bien pensado.
- **Secretos fuera del repo**: `.gitignore` excluye correctamente `appsettings.*.json` salvo
  `appsettings.json`/`appsettings.Example.json`; `appsettings.Development.json` (con secretos reales)
  no está trackeado en git — verificado con `git ls-files` y `git show HEAD:...`. `appsettings.Example.json`
  documenta bien qué generar y cómo (`dotnet user-secrets set ...`).
- **Deuda técnica marcada explícitamente**: varios comentarios `// ponytail:` documentan simplificaciones
  deliberadas con su techo conocido (ej. `PasswordEntryService.cs:124` — historial sin límite de versiones;
  `TrashService.cs:172-174` — adjuntos huérfanos; `LocalFileAttachmentStorage.cs:31-33` — sin sanitización
  de path porque la key siempre es un GUID server-side). Es una práctica que ayuda mucho a distinguir
  "no se pensó" de "se pensó y se pospuso a propósito".
- **Dependencias**: todas las librerías de Microsoft están en `10.0.2`/`10.0.9` (línea de .NET 10
  actual), sin versiones desactualizadas visibles. `Isopoh.Cryptography.Argon2 2.0.0` es la librería
  Argon2 de referencia en el ecosistema .NET — elección correcta, no una implementación casera.
- **No se pudo verificar**: vulnerabilidades conocidas en el lockfile — no se ejecutó `dotnet list
  package --vulnerable` ni se consultó una base de CVEs en esta pasada (fuera del alcance de una lectura
  de código estática).
- **No se pudo verificar**: comportamiento de rendimiento de `PasswordHealthService.GetReportAsync`
  (`PasswordHealthService.cs:24-32`) a escala — descifra **todas** las entradas del usuario en memoria en
  cada request al reporte de salud. Correcto en cuanto a seguridad (nunca persiste ni loguea el
  resultado), pero no se midió el costo con vaults grandes.

## Oportunidades para destacar

1. **"Local-first, self-hosted, sin nube de terceros" como propuesta central.** El proyecto ya corre
   como un monolito Blazor WASM + SQL Server autohospedado, sin ninguna dependencia de un backend SaaS de
   terceros. En vez de competir con Bitwarden en "cifrado zero-knowledge en la nube de otro", The Shed
   podría posicionarse como el gestor que uno mismo hostea en su NAS/VPS, con el modelo de amenaza
   explícito de "confío en mi propio servidor, no en un tercero" — que de hecho es coherente con el
   diseño actual (C1 dejaría de ser un defecto y pasaría a ser una decisión de producto declarada).

2. **Historial de versiones con diffing ya resuelto en el cliente.** El trabajo reciente en `EntryRow`
   (`OnParametersSet` en `EntryRow.razor.cs:29-39`) ya invalida el historial/reveal cuando la contraseña
   cambia bajo el componente, usando `PasswordChangedAt` como clave de invalidación. Es una base sólida
   para una feature que muchos competidores tratan como secundaria: un historial de contraseñas realmente
   confiable y auditado (quién cambió qué y cuándo, ya con `ChangedByUserId` en el modelo), útil para
   vaults compartidos en equipos/familias donde "quién tocó esto" importa.

3. **Compartir vaults con roles explícitos (lectura/escritura) ya modelado a nivel de fila.** El sistema
   de `VaultMember` + `VaultRole` es más granular que el "compartir todo o nada" que ofrecen varios
   gestores personales. Con la base de datos ya lista, la app tiene una ventaja de partida para un nicho
   de "vaults familiares/de equipo con permisos reales" si se resuelve el cifrado end-to-end (C1) para
   que compartir implique intercambio de claves, no solo un permiso de fila.

4. **Papelera con retención configurable y purga automática ya construida.** `TrashPurgeService` +
   `TrashSettings.RetentionDays` es una feature de "red de seguridad contra el error humano" que muchos
   gestores tratan como ocurrencia tardía; acá ya es un `BackgroundService` de primera clase con tests.
   Vale la pena convertirlo en un diferencial visible ("nunca perdés una contraseña borrada por 30 días",
   configurable) en vez de dejarlo como feature interna.

## Próximos pasos sugeridos (orden de prioridad)
1. Decidir explícitamente si The Shed apunta a ser zero-knowledge (mover cifrado al cliente) o si el
   modelo "self-hosted, confío en mi servidor" (oportunidad #1) es la propuesta real — y documentarlo
   como decisión de arquitectura, no dejarlo implícito (C1).
2. ~~Rate limiting en `/api/auth/login` y `/api/auth/register` (A3)~~ — ✅ hecho (2026-08-16, Sprint 21).
3. Auto-bloqueo por inactividad + reautenticación para revelar/exportar contraseñas (A1, A2) — ya
   planeado en `docs/PRODUCT.md`, falta implementar.
4. Evaluar antiforgery token como capa adicional a `SameSite=Lax` (A4) — evaluado, pospuesto por tamaño
   (ver la nota en A4); es el candidato natural al próximo PR de hardening.
5. Resolver el problema de adjuntos huérfanos en purga (M2) antes de que `Attachments/` crezca sin límite.
6. ~~Sumar `Content-Security-Policy` y `UseHsts()` (M5)~~ — ✅ hecho (2026-08-16, Sprint 22), ver D8.
