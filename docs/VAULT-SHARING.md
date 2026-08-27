# Vault sharing — zero-knowledge key wrapping (Sprint 27)

`feature/e2e-vault-sharing` (closed). Increment plan for closing the gap D7 left open: a
shared member had no way to decrypt a vault before this.

## Context

Sprint 26 moved `PasswordEntry`/`SecureNote` encryption to the client using a per-vault AES
key (the "vault key"), wrapped for the owner with their stretched master key
(`VaultKeyWrap`, `IVaultKeyService`). But `VaultService.AddMemberAsync` still only inserts a
`VaultMember` row — it never creates a `VaultKeyWrap` for the new member. This sprint closes
that gap using the RSA keypair infrastructure Sprint 25 already put in place
(`User.PublicKey` / `User.EncryptedPrivateKey`, `IUserKeypairService`, `generateRsaKeypair`
in `interop.js`) but never wired into the sharing flow.

Two other things fall out of the same investigation, done in this branch because they're part
of finishing the zero-knowledge migration (D7):

- **Attachments never moved to client-side encryption.** `AttachmentService` still calls the
  old server-global `IEncryptionService`. History already *is* done — `PasswordEntryService`
  just copies the client-supplied ciphertext blob into `EntryHistory`, no server decrypt
  involved. So "history + attachments" from the original `SPRINTS.md` wording is really just
  attachments.
- **`PasswordHealthService` is currently broken**, not just architecturally wrong. It calls
  `_encryption.Decrypt(e.PasswordEncrypted)` with the old server key, but `PasswordEncrypted`
  has been client-ciphertext (vault key) since Sprint 26. Moving it client-side (over
  `PasswordHealthChecker`, already in `Shared`) both fixes the bug and finishes the migration
  for this data. The Health report eagerly unwraps every accessible vault's key (not just ones
  already opened this session) so it's always complete.

Out of scope (deferred to Sprint 28 per `SPRINTS.md`): migrating data created before this
sprint, and vault-key rotation on member removal (M1 in `AUDITORIA.md`, accepted risk per D7).

## Increments

Each increment stops for review/commit before the next starts.

- [x] **Increment 1 — Server: expose public keys.** New `GET /api/users/public-key?email=`
      (`UsersController`, `[Authorize]`) returning `{ UserId, PublicKey }` or 404. Extend
      `AuthResponse` with `PublicKey`/`EncryptedPrivateKey`, populated in `AuthController.Me()`
      (one extra `_db` lookup — too large for JWT claims) and `AuthService.Success(User)`.
- [x] **Increment 2 — Crypto primitives: RSA-OAEP wrap/unwrap.** `interop.js`:
      `wrapKeyRsaOaep`/`unwrapKeyRsaOaep` (same shape as `encryptAesGcm`/`decryptAesGcm`).
      `IUserKeypairService` gains `WrapKeyForMemberAsync(vaultKey, memberPublicKeyPem)` and
      `UnwrapKeyAsMemberAsync(stretchedMasterKey, encryptedPrivateKey, wrappedVaultKey)`,
      implemented in `WebCryptoUserKeypairService`.
- [x] **Increment 3 — Wire sharing end to end.** `VaultMemberAddRequest.VaultKeyWrap`;
      `VaultService.AddMemberAsync` persists a `VaultKeyWrap` row for the new member;
      `VaultsController.AddMember` guards a missing wrap (same pattern as `Create`).
      `MembersPanel.AddMemberAsync` fetches the target's public key, wraps the cached vault
      key, sends it along. `VaultDetail.OnInitializedAsync` branches the unwrap on
      `_vault.IsOwner`: owner keeps the AES path, member uses
      `UnwrapKeyAsMemberAsync`. Manual browser verification with two real accounts.
- [x] **Increment 4 — Attachments to client-side encryption.** `AttachmentClient`
      encrypts/decrypts bytes with the vault key (base64 through the existing
      `IAesGcmService`, no new interop needed). `EntryAttachmentsPanel` gains a `VaultKey`
      parameter. `AttachmentService` drops `IEncryptionService`, stores/returns ciphertext
      as-is (same shape entries/notes ended up in after Sprint 26).
- [x] **Increment 5 — Password Health to client-side.** Delete
      `PasswordHealthService`/`IPasswordHealthService` (currently broken). `Health.razor.cs`
      unwraps every accessible vault's key, decrypts each entry's password client-side, runs
      `PasswordHealthChecker` locally. No Blazor-component test harness in this repo — verified
      manually in-browser, same bar as Increment 3.
- [x] **Increment 6 — Docs + cleanup.** `docs/SPRINTS.md` checked off, "historial y adjuntos"
      line corrected (history was already done in Sprint 26). `docs/TODO.md` progress note.
      `RemoveMemberAsync` also deletes the removed member's now-stale `VaultKeyWrap` row (not
      rotation — M1 stays open — just stops the wrap being handed back out).

## Verification

`dotnet test` after every increment. Increments 3 and 5 additionally require manual browser
verification (two real accounts for sharing; a multi-vault account for Health) — green tests
alone didn't catch the WASM `AesGcm`/`RSA` gaps or the `SecureNote.Title`/`PasswordEntry.Notes`
field-encryption misses in Sprints 25–26, so the same bar applies here.
