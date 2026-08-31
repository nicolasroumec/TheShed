# The Shed — Log de decisiones

> Registro de decisiones técnicas con su contexto. Orden cronológico (más reciente abajo).

## D1 — Hash de la contraseña maestra: Argon2 (servidor)
**Decisión:** hashear la contraseña maestra con **Argon2** en el servidor, usando
`Isopoh.Cryptography.Argon2` (formato PHC con salt y parámetros embebidos).
**Contexto:** el modelo *zero-knowledge* (derivar la clave en el cliente) queda como
evolución futura. Por ahora el servidor recibe la contraseña en el login/registro.
**Alternativas descartadas:** bcrypt, PBKDF2, MD5/SHA (prohibidos por CLAUDE.md).

## D2 — Sesiones: JWT (HMAC-SHA256, solo access token)
**Decisión:** access token JWT firmado con HMAC-SHA256; la clave vive en User Secrets
(dev) / variables de entorno (prod), nunca en `appsettings.json`.
**Contexto:** sin refresh token todavía; se agrega cuando haga falta. Detalle de
implementación en `AUTH_FLOW.md`.

## D3 — Cifrado de entradas: AES-256-GCM con clave de servidor
**Decisión:** cifrar los valores sensibles de las entradas (`PasswordEntry.PasswordEncrypted`)
con **AES-256-GCM**. Formato almacenado: `base64(nonce(12) || ciphertext || tag(16))`.
La clave AES-256 se provee por **User Secrets** (`Encryption:Key`, 32 bytes en base64),
mismo patrón que la clave JWT.
**Por qué GCM:** cifrado autenticado — detecta manipulación vía tag de integridad,
evitando el armado manual y propenso a errores de AES-CBC + HMAC.
**Por qué clave de servidor:** consistente con D1/D2 (modelo no zero-knowledge por ahora).
Implica que el servidor puede descifrar; aceptable en esta etapa.
**Evolución futura:** clave derivada del master password (zero-knowledge) o clave por
vault para vaults compartidos.
**Implementación:** `TheShed.Server/Security/{IEncryptionService,AesEncryptionService,EncryptionSettings}.cs`;
nonce aleatorio por operación; tests en `TheShed.Tests/Security/AesEncryptionServiceTests.cs`.
**Superseded por D7** (2026-08-14): la "evolución futura" de este párrafo pasó a ser la
decisión tomada. Esta entrada queda como registro histórico de por qué el modelo empezó
así, no como el diseño vigente.

## D4 — Vaults compartidos: dueño por `OwnerId`, miembros por `VaultMember` + rol
**Decisión:** el dueño de un vault se rastrea con `Vault.OwnerId` y **no** se crea un
`VaultMember` para él. Compartir = agregar filas `VaultMember (UserId, Role)` con
`VaultRole` ∈ `{Viewer, Editor}`. La gestión de miembros (listar/invitar/cambiar rol/quitar)
es **exclusiva del dueño**.
**Contexto:** evita el caso ambiguo "dueño que también es member" (sin dedup en el listado)
y mantiene una única fuente de verdad para la propiedad. El acceso efectivo se resuelve por
`IVaultAccessService` (D del Sprint 4): `Owner/Editor → Write`, `Viewer → Read`, resto → `None`.
**Autorización:** sin acceso al vault → 404 (oculta existencia); con acceso pero no dueño → 403.
Invitar por email exacto: usuario inexistente → 404, ya member o el propio dueño → 409.
**Evolución futura:** clave AES por vault para sharing real zero-knowledge (ver D3).
**Implementación:** `VaultService` (Add/UpdateRole/Remove/ListMembers) + `VaultsController`
bajo `/api/vaults/{id}/members`; DTOs en `TheShed.Shared/Models/DTOs/Vaults/`; tests en
`TheShed.Tests/{Services/VaultServiceTests,Controllers/VaultsControllerTests}.cs`.

## D5 — Papelera: purga automática a los 30 días
**Decisión:** todo lo soft-deleted (`IsDeleted = true`) se purga (hard delete) automáticamente
a los **30 días** de borrado, vía un `BackgroundService` que corre periódicamente. El usuario
puede purgar antes manualmente desde la papelera, pero no puede evitar la purga automática.
**Contexto:** hoy `AuditableEntity` no registra *cuándo* se borró, solo el flag `IsDeleted`;
sin esa fecha no hay forma de calcular expiración. Se suma `DateTime? DeletedAt`.
**Retención configurable:** `Trash:RetentionDays` (`appsettings.json`, default `30`) — no
hardcodeado, para poder ajustarlo sin recompilar.
**Alcance:** aplica a `PasswordEntry`, `SecureNote` y `Vault` (dueño). Un vault borrado arrastra
sus entradas/notas: restaurar el vault las restaura a ellas; purgar el vault las purga a ellas.
**Evolución futura:** si la purga periódica no escala (tabla muy grande), mover a un job por
lotes o a nivel de base de datos (ej. SQL Agent job) en vez de `BackgroundService` in-process.

## D6 — Sesión JWT: cookie `HttpOnly` en vez de `localStorage`
**Decisión:** el JWT se transporta en una cookie `HttpOnly` (`authToken`, `Secure`, `SameSite=Lax`,
`Path=/`) seteada por el servidor en `login`/`register`, en vez de devolverse en el body y
guardarse en `localStorage`. El cliente ya no lee ni decodifica el token: usa `GET /api/auth/me`
para saber si está logueado y quién es.
**Por qué:** `localStorage` es legible por cualquier script — un XSS en un password manager
compromete la sesión entera. Una cookie `HttpOnly` no es accesible desde JS ni con XSS.
**Por qué `SameSite=Lax` y no `Strict`:** manda la cookie en navegaciones normales (click en un
link, escribir la URL) pero la bloquea en POST/PUT/DELETE cross-site, que es lo que importa para
CSRF; no rompe el flujo de abrir un link y seguir logueado.
**Consecuencia — CORS:** se sacó la policy `AllowAll` (`AllowAnyOrigin+AllowAnyMethod+AllowAnyHeader`):
cliente y API viven en el mismo origen (modelo hosted) y, con `SameSite=Lax` sin `AllowCredentials`,
un request cross-origin no iba a poder autenticarse de todos modos.
**Consecuencia — `TheShed.Client` standalone:** el `launchSettings.json` propio del Client
(`:5064`/`:7163`, leftover del template hosted) deja de poder loguearse si se corre contra un
`TheShed.Server` en otro puerto (la cookie no viaja cross-origin sin CORS+credentials). No se usa
en el flujo real del proyecto (se corre `TheShed.Server`), así que no se tocó.
**No cambia:** sigue siendo un JWT HMAC-SHA256 sin refresh token (D2); solo cambia el canal de
transporte.
**Implementación:** `AuthController` (`Cookies.Append`/`Delete`, `GET /me`), `JwtBearerEvents.OnMessageReceived`
en `Program.cs` (cae a la cookie si no vino header `Authorization`), `JwtAuthenticationStateProvider`
+ `AuthService` (Client, sin `ILocalStorageService`/`JwtParser`).

## D7 — Cifrado zero-knowledge: adoptar, no quedarse en "self-hosted, confío en mi servidor"
**Decisión:** migrar del modelo de D3 (clave de servidor, única para todos los usuarios)
a **zero-knowledge real**: derivación de clave del master password en el cliente
(Blazor WASM), el servidor pasa a almacenar y servir blobs opacos que nunca puede
descifrar.
**Contexto:** disparado por el hallazgo C1 de `docs/AUDITORIA.md` (2026-08-13), que
señaló que D3 dejaba esto como "evolución futura" sin fecha ni criterio de cuándo
resolverlo. El criterio que faltaba era el modelo de amenaza real: **¿quién opera el
servidor?** Si es siempre el mismo usuario/alguien de confianza, el modelo actual es un
trade-off aceptable (auditoría, "Oportunidades para destacar #1"). Se confirmó
(2026-08-14) que The Shed puede terminar corriendo para terceros no relacionados con
quien lo hostea — ahí el operador del servidor **sí** es parte del modelo de amenaza,
el mismo problema que resuelven Bitwarden/1Password/Proton Pass. Con eso en la mesa, el
modelo actual no alcanza.
**Se mantiene sin cambios:** D1 (Argon2 sigue siendo el hash de autenticación — es un
derivado distinto del master password, no la clave de cifrado) y D2 (JWT en cookie
httpOnly).
**Reemplaza:** D3 queda **superseded** — ver nota en esa entrada. El plan original preveía
mantener la clave de servidor viva durante la migración (Sprint 28), para descifrar por
última vez los datos existentes. **Ajuste (2026-08-27):** los vaults previos al Sprint 26
eran todos datos de prueba, así que se decidió no migrarlos — se descartan. Sprint 28 quedó
reducido a apagar esa superficie de cifrado (`IEncryptionService`/`EncryptionSettings`/
`Encryption:Key`, sin consumidores reales desde el Sprint 26/27) sin ningún paso de
migración.
**Riesgo aceptado explícitamente (no resuelto en este alcance):** remover un miembro de
un vault compartido no rota la vault key (Sprint 27, increment 2) — un ex-miembro que
guardó una copia del key-wrap podría en teoría seguir descifrando datos posteriores a su
remoción hasta que exista rotación de clave (ver M1 en `AUDITORIA.md`).
**Costo de UX aceptado:** sin una recovery key (increment opcional en Sprint 28), olvidar
la master password implica pérdida total e irrecuperable de los datos — igual que el
modelo real de Bitwarden/1Password, no un descuido de este proyecto.
**Alcance:** no es una tarea suelta, son 4 sprints — ver `SPRINTS.md` 25-28 (derivación
de clave + keypair por usuario, vault key por vault, compartir vía key-wrapping
asimétrico, migración de datos existentes).

## D8 — CSP estricto por encima del fingerprinting de assets WASM
**Decisión:** el `Content-Security-Policy` mantiene `script-src 'self' 'wasm-unsafe-eval'` sin
`'unsafe-inline'`, y para que eso sea posible se apaga el fingerprinting de assets WASM
(`<WasmFingerprintAssets>false</WasmFingerprintAssets>` en `TheShed.Client.csproj`).
**Contexto:** con fingerprinting activo, Blazor resuelve `_framework/dotnet.js` al nombre real
(hasheado) a través de un `<script type="importmap">` **inline**. Un `script-src` sin
`'unsafe-inline'` bloquea ese importmap, la traducción nunca ocurre, Blazor pide el nombre literal
y recibe un 404: la app queda colgada en "Loading". No es un caso de borde — es el
`script-src` que la propia documentación de Microsoft recomienda para Blazor WebAssembly.
**Alternativas descartadas:**
- `'unsafe-inline'` en `script-src` — anula justamente la protección por la que existe el header, y
  en un gestor de contraseñas XSS es la amenaza principal del cliente (el mismo motivo de D6).
- Hash SRI o nonce sobre el importmap (lo que Microsoft recomienda) — ambos asumen un componente
  `ImportMap` de Razor donde inyectar el atributo. Acá el importmap lo genera el pipeline de static
  assets al servir un `index.html` estático: no hay punto de inyección sin escribir código que
  intercepte y reescriba la respuesta.
**Costo aceptado:** se pierde el cache-busting por nombre de archivo al deployar. `blazor.boot.json`
sigue llevando hashes de contenido, así que el runtime revalida igual; el riesgo práctico es un
navegador sirviendo un asset viejo de caché tras un deploy.
**Camino de vuelta (marcado con `ponytail:` en el csproj):** calcular el SHA-256 del importmap
renderizado por respuesta y sumarlo a `script-src` como hash SRI. Vale la pena recién si el
fingerprinting llega a importar.
**Limitación conocida:** `style-src` conserva `'unsafe-inline'` — 8 componentes usan atributos
`style=""`. Es una superficie mucho menor que la de scripts; sacarlo es moverlos a clases.

**Addendum (2026-08-31, Sprint 31 Increment 4):** `WasmFingerprintAssets=false` no alcanzaba —
cubre el payload WASM pero no `blazor.webassembly.js`, que tiene su propio switch
(`BlazorFingerprintBlazorJs`, gateado por `OverrideHtmlAssetPlaceholders` en los targets del SDK).
Con ese switch en su default, `index.html` quedaba con el placeholder literal
`_framework/blazor.webassembly#[.{fingerprint}].js` sin nada que lo resolviera (esta app no usa
`MapStaticAssets`), y el navegador interpreta todo desde el `#` como fragmento de URL — la app
quedaba colgada en "Loading" en cualquier `dotnet publish -c Release` real. Nadie lo había visto
porque nadie había publicado en Release desde que este decision se tomó: `dotnet run` resuelve el
placeholder por otro camino (el pipeline de static web assets de Development) y lo tapaba. Fix:
`BlazorFingerprintBlazorJs=false` explícito + `index.html` apunta directo al nombre de archivo
ya determinístico, sin placeholder.

## D9 — Session lock: re-derivar la clave, no persistirla
**Decisión:** al recargar la página (F5, o el SO matando y relanzando una PWA instalada), la
stretched master key y el keypair descifrado **no se guardan en ningún storage del navegador**.
La app pasa a una pantalla `Locked` que solo pide la master password de nuevo y re-deriva la
clave (mismo camino que el login, sin round-trip al servidor salvo `GET /api/auth/me` que ya
existía).
**Contexto:** con el cifrado zero-knowledge (D7) la stretched master key vive solo en memoria
(`StretchedKeyStore`), pero la cookie JWT (D6) sobrevive al reload — la app se mostraba logueada
con toda ruta de descifrado muerta hasta un logout/login completo. Sprint 31 (PWA) lo convertía
en bloqueante: una PWA instalada es matada y relanzada por el SO constantemente.
**Alternativa descartada — persistir la clave en `localStorage`/`sessionStorage`:** legible por
cualquier XSS (mismo motivo que D6), y tampoco resuelve el caso PWA — `sessionStorage` muere
con el proceso cuando el SO mata la app instalada.
**Fuera de alcance, no descartada — `CryptoKey` no extraíble en IndexedDB:** Web Crypto permite
guardar una clave con `extractable: false`; un XSS podría *usarla* mientras la página está abierta
pero nunca leer sus bytes. Materialmente mejor que `localStorage`, materialmente más trabajo (la
stretched key es hoy un `byte[]` pasado a `interop.js`, pasaría a ser un handle opaco en todos
lados). Vale la pena solo si escribir la master password tras cada reload resulta molesto en uso
real — ver "Out of scope (P4)" en `docs/SPRINTS.md`.
**Implementación:** `Unlock.razor` + `AuthService.UnlockAsync` (verificación por descifrado de
prueba de `EncryptedPrivateKey`, sin verifier separado — reenviar el login habría vuelto a mandar
la master password por la red y gastado el rate limit de auth en una acción mucho más frecuente
que iniciar sesión). Junto con esto: auto-lock por 15 min de inactividad y reautenticación (misma
pantalla de password, sin re-derivar) antes de revelar/copiar una contraseña — cierran A2/A1 de
`AUDITORIA.md`. Detalle completo en `docs/SPRINTS.md` Sprint 30 (shipped).
