# The Shed — Diagrama de Base de Datos

```mermaid
erDiagram

    User {
        int Id PK
        string Username
        string Email
        string PasswordHash
        bool IsActive
        bool TwoFactorEnabled
        string TwoFactorSecret "nullable"
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
        datetime LastLoginAt
    }

    Vault {
        int Id PK
        int OwnerId FK
        string Name
        string Description
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    VaultMember {
        int Id PK
        int VaultId FK
        int UserId FK
        VaultRole Role
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    PasswordEntry {
        int Id PK
        int VaultId FK
        string Name
        string Username
        string PasswordEncrypted
        string Url
        string Notes
        bool IsFavorite
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    SecureNote {
        int Id PK
        int VaultId FK
        string Title
        string ContentEncrypted
        bool IsFavorite
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    Tag {
        int Id PK
        int UserId FK
        string Name
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    PasswordEntryTag {
        int Id PK
        int PasswordEntryId FK
        int TagId FK
    }

    EntryHistory {
        int Id PK
        int PasswordEntryId FK
        string PasswordEncrypted
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    Attachment {
        int Id PK
        int PasswordEntryId FK
        string FileName
        string StoragePath
        int FileSizeBytes
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }

    User ||--o{ Vault : "owns"
    User ||--o{ VaultMember : "member of"
    User ||--o{ Tag : "owns"
    Vault ||--o{ VaultMember : "has members"
    Vault ||--o{ PasswordEntry : "contains"
    Vault ||--o{ SecureNote : "contains"
    PasswordEntry ||--o{ PasswordEntryTag : "has"
    Tag ||--o{ PasswordEntryTag : "has"
    PasswordEntry ||--o{ EntryHistory : "history"
    PasswordEntry ||--o{ Attachment : "attachments"
```

## Notas

- `VaultRole` → `Viewer` (solo lectura) | `Editor` (lectura+escritura)
- `PasswordEncrypted` y `ContentEncrypted` → cifrado AES-256, nunca en texto plano
- `TwoFactorSecret` → secret TOTP, nullable (null = 2FA desactivado)
- Todas las entidades heredan `AuditableEntity` (soft delete + timestamps)
- `Tag` es por usuario, no global — cada uno tiene sus propias etiquetas
- `Attachment.StoragePath` → ruta al archivo en disco/blob storage (no se guarda el binario en la DB)
