# The Shed — Product

## What it is
A multi-user password manager. Users keep their encrypted credentials in vaults, organized however
they like, reachable from any device.

## Who it is for
Anyone who wants to keep their passwords safely in the cloud, with the option of sharing vaults with
other users (partner, team, family).

---

## Features

### Authentication
- Sign up and sign in with email + master password
- Change the master password (vaults and entries are kept; only the keys that protect them are re-encrypted)
- Sign out everywhere: ends every session of the account on every device (a password change also signs out the other devices)
- Automatic sign-out on inactivity (configurable timeout)
- 2FA (two-factor authentication)

### Vaults
- Create, edit and delete vaults (grouping folders)
- Share a vault with another user
- Roles in a shared vault: read-only / read+write

### Entries
- Fields: name, username, password, URL, notes
- Show/hide the password
- Copy the password to the clipboard (without showing it on screen)
- The copied password is cleared from the clipboard after 30 s, unless something else was copied since
- Favorites for quick access
- Tags to categorize entries
- Version history (see an entry's previous passwords)
- Trash: deleted entries can be recovered before they are deleted for good

### Secure notes
- Text-only entries, with no username/password
- For storing: serial numbers, security answers, PINs, etc.

### Attachments
- Small files attached to an entry (e.g. a license PDF, a card image)
- Purging the entry (or its vault) from the trash also deletes its files from disk

### Password generator
- Configurable length
- Options: uppercase, lowercase, digits, symbols
- Usable while creating/editing an entry

### Security
- Every password encrypted with AES-256
- Weak password detector
- Detector for passwords repeated across entries

### Search
- Search entries by name, URL or username

### Import / Export
- Import from LastPass, Bitwarden, 1Password (CSV format)
- Export all your own entries

---

## Out of scope (for now)
- Native mobile app
- Browser extension
- Offline access
