# The Shed — Architecture

## Stack
- **.NET 10** Blazor WebAssembly (hosted)
- **EF Core 10** + **SQL Server**
- **3 proyectos:** `TheShed.Server` · `TheShed.Client` · `TheShed.Shared`

## Seguridad
- Contraseñas de usuario: **Argon2** (hash de la contraseña maestra)
- Entradas del vault: **AES-256-GCM** (cifrado autenticado; clave de servidor en User Secrets)
- Sesiones: **JWT**

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
