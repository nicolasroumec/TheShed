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
