# The Shed — Context for Claude

## What it is
Multi-user password manager. Users store their encrypted credentials in organizable
vaults, with support for shared vaults, secure notes, attachments and a password
generator. See `docs/PRODUCT.md` for the full detail.

## Stack
- **.NET 10** Blazor WebAssembly (hosted)
- **EF Core 10** + **SQL Server**
- **3 projects:**
  - `TheShed.Server` — Web API + Blazor WASM host
  - `TheShed.Client` — Blazor WASM frontend
  - `TheShed.Shared` — Models, DTOs, helpers (shared)

## Frequent commands
```bash
dotnet build                                                   # build everything
dotnet build TheShed.Shared/TheShed.Shared.csproj             # shared only
dotnet ef migrations add <Name> --project TheShed.Server      # new migration
dotnet ef database update --project TheShed.Server            # apply migrations
```

## Namespace structure (Shared)
| Folder | Namespace |
|---------|-----------|
| `Models/Entities/` | `TheShed.Shared.Models.Entities` |
| `Models/Enums/` | `TheShed.Shared.Models.Enums` |
| `Models/Base/` | `TheShed.Shared.Models.Base` |
| `Models/DTOs/` | `TheShed.Shared.Models.DTOs` |

## Main entities
`User` · `Vault` · `VaultMember` · `PasswordEntry` · `Tag` · `EntryHistory`
`Attachment` · `SecureNote`

## Code conventions
- Language for the **whole project: English** — code, comments (including `<summary>` XML)
  and documentation (`docs/`, `CLAUDE.md`)
- All entities inherit `AuditableEntity` (soft delete + timestamps)
- Every `.razor` file **must** use code-behind: markup in `Foo.razor`, logic in `Foo.razor.cs`
  (partial class). Inline `@code { }` blocks are not allowed.

## Security — critical rules
- Master password hash: **Argon2** (never bcrypt, never MD5/SHA)
- Entry encryption: **AES-256**
- Sessions: **JWT**
- Never store passwords in plaintext, never log passwords

## What NOT to do
- Do not commit without explicit confirmation from the user
- Do not store passwords in plaintext
- Do not use MD5 or SHA-1 for password hashing

## Documentation
- `docs/PRODUCT.md` — app features
- `docs/ARCHITECTURE.md` — stack, data model, technical decisions
- `docs/TODO.md` — current status and next steps
