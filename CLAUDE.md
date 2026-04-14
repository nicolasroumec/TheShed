# The Shed — Contexto para Claude

## Qué es
Gestor de contraseñas multi-usuario. Los usuarios guardan sus credenciales cifradas
en vaults organizables, con soporte para vaults compartidos, notas seguras, adjuntos
y generador de contraseñas. Ver `docs/PRODUCT.md` para el detalle completo.

## Stack
- **.NET 10** Blazor WebAssembly (hosted)
- **EF Core 10** + **SQL Server**
- **3 proyectos:**
  - `TheShed.Server` — API Web + host Blazor WASM
  - `TheShed.Client` — Blazor WASM frontend
  - `TheShed.Shared` — Modelos, DTOs, helpers (compartido)

## Comandos frecuentes
```bash
dotnet build                                                   # build todo
dotnet build TheShed.Shared/TheShed.Shared.csproj             # solo shared
dotnet ef migrations add <Nombre> --project TheShed.Server    # nueva migración
dotnet ef database update --project TheShed.Server            # aplicar migraciones
```

## Estructura de namespaces (Shared)
| Carpeta | Namespace |
|---------|-----------|
| `Models/Entities/` | `TheShed.Shared.Models.Entities` |
| `Models/Enums/` | `TheShed.Shared.Models.Enums` |
| `Models/Base/` | `TheShed.Shared.Models.Base` |
| `Models/DTOs/` | `TheShed.Shared.Models.DTOs` |

## Entidades principales
`User` · `Vault` · `VaultMember` · `PasswordEntry` · `Tag` · `EntryHistory`
`Attachment` · `SecureNote`

## Convenciones de código
- Idioma del código: **inglés**
- Idioma de docs y comentarios: **español**
- Todas las entidades heredan `AuditableEntity` (soft delete + timestamps)

## Seguridad — reglas críticas
- Hash de contraseña maestra: **Argon2** (nunca bcrypt, nunca MD5/SHA)
- Cifrado de entradas: **AES-256**
- Sesiones: **JWT**
- Nunca guardar contraseñas en texto plano, nunca loggear contraseñas

## Lo que NO hacer
- No commitear sin confirmación explícita del usuario
- No guardar contraseñas en texto plano
- No usar MD5 o SHA-1 para hashing de contraseñas

## Documentación
- `docs/PRODUCT.md` — funcionalidades de la app
- `docs/ARCHITECTURE.md` — stack, modelo de datos, decisiones técnicas
- `docs/TODO.md` — estado actual y próximos pasos
