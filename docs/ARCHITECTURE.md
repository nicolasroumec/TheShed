# The Shed — Architecture

## Stack
- **.NET 10** Blazor WebAssembly (hosted)
- **EF Core 10** + **SQL Server**
- **3 projects:** `TheShed.Server` · `TheShed.Client` · `TheShed.Shared`

## Security
- **Zero-knowledge** (D7): the master password is stretched client-side (PBKDF2, Web Crypto);
  vault keys, entries and notes are encrypted in the browser with **AES-256-GCM**. The server
  stores opaque blobs only
- Authentication: the client sends an auth hash derived from the stretched key, never the
  password (D11); the server hashes it with **Argon2**
- Sessions: **JWT** in an httpOnly cookie (D6) + antiforgery token (D10); revocable through
  `User.TokenVersion`, checked on every request (D12)

Detail and context for each decision in `DECISIONS.md`.

## Data model (draft)

```
User
├── Vaults (1:N)
│   ├── PasswordEntries (1:N)
│   │   ├── Tags (M:N)
│   │   ├── EntryHistory (1:N)   ← previous versions
│   │   └── Attachments (1:N)
│   ├── SecureNotes (1:N)        ← text-only notes
│   └── VaultMembers (1:N)       ← users with shared access
└── Tags (1:N)                   ← the user's own tags
```

## Technical decisions
See DECISIONS.md.

## Commands
```bash
dotnet build                                                   # build everything
dotnet build TheShed.Shared/TheShed.Shared.csproj             # shared only
dotnet ef migrations add <Name> --project TheShed.Server      # new migration
dotnet ef database update --project TheShed.Server            # apply migrations
```
