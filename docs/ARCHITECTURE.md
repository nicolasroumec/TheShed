# The Shed — Architecture

## Stack
- **.NET 10** Blazor WebAssembly (hosted)
- **EF Core 10** + **SQL Server**
- **3 proyectos:** `TheShed.Server` · `TheShed.Client` · `TheShed.Shared`

## Security
- **Zero-knowledge** (D7): the master password is stretched client-side (PBKDF2, Web Crypto);
  vault keys, entries and notes are encrypted in the browser with **AES-256-GCM**. The server
  stores opaque blobs only
- Authentication: the client sends an auth hash derived from the stretched key, never the
  password (D11); the server hashes it with **Argon2**
- Sessions: **JWT** in an httpOnly cookie (D6) + antiforgery token (D10)

Detalle y contexto de cada decisión en `DECISIONS.md`.

## Modelo de datos (borrador)

```
User
├── Vaults (1:N)
│   ├── PasswordEntries (1:N)
│   │   ├── Tags (M:N)
│   │   ├── EntryHistory (1:N)   ← versiones anteriores
│   │   └── Attachments (1:N)
│   ├── SecureNotes (1:N)        ← notas de solo texto
│   └── VaultMembers (1:N)       ← usuarios con acceso compartido
└── Tags (1:N)                   ← etiquetas propias del usuario
```

## Decisiones técnicas
Ver DECISIONS.md (a completar a medida que avanzamos).

## Comandos
```bash
dotnet build                                                   # build todo
dotnet build TheShed.Shared/TheShed.Shared.csproj             # solo shared
dotnet ef migrations add <Nombre> --project TheShed.Server    # nueva migración
dotnet ef database update --project TheShed.Server            # aplicar migraciones
```
