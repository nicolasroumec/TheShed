window.copyToClipboard = (text) => navigator.clipboard.writeText(text);

window.downloadFile = (fileName, bytes) => {
    const url = URL.createObjectURL(new Blob([bytes]));
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

// Idle auto-lock (Sprint 30). The timer and the activity events live here rather than in .NET:
// the browser already owns them, and routing every keystroke through interop to reset a WASM
// timer would be a hop per event. .NET only hears about it once — when the session should lock.
let idleRef = null;
let idleTimer = null;
let idleTimeoutMs = 0;
let idleLastActivity = 0;

function idleLock() {
    if (idleRef) idleRef.invokeMethodAsync('OnIdle');
}

// Only real user activity counts. Notably absent: visibilitychange — coming back to the tab
// after two hours away is not a reason to grant another full timeout.
function idleReset() {
    idleLastActivity = Date.now();
    clearTimeout(idleTimer);
    idleTimer = setTimeout(idleLock, idleTimeoutMs);
}

// setTimeout is throttled in a background tab and frozen outright in a suspended PWA, so the
// timer alone cannot be trusted to have fired while the app was away. On the way back, compare
// wall clocks instead.
function idleOnVisibilityChange() {
    if (document.visibilityState === 'visible' && Date.now() - idleLastActivity >= idleTimeoutMs) {
        idleLock();
    }
}

window.startIdleWatch = (dotNetRef, timeoutMs) => {
    idleRef = dotNetRef;
    idleTimeoutMs = timeoutMs;
    // Capture phase: a handler that stops propagation further down must not blind the lock.
    document.addEventListener('pointerdown', idleReset, true);
    document.addEventListener('keydown', idleReset, true);
    document.addEventListener('visibilitychange', idleOnVisibilityChange);
    idleReset();
};

window.stopIdleWatch = () => {
    clearTimeout(idleTimer);
    document.removeEventListener('pointerdown', idleReset, true);
    document.removeEventListener('keydown', idleReset, true);
    document.removeEventListener('visibilitychange', idleOnVisibilityChange);
    idleRef = null;
};

function bytesToB64(bytes) {
    let bin = '';
    for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
    return btoa(bin);
}

function b64ToBytes(b64) {
    const bin = atob(b64);
    const bytes = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) bytes[i] = bin.charCodeAt(i);
    return bytes;
}

// RSA.Create() throws PlatformNotSupportedException on browser-wasm (no native crypto
// provider there), so keygen goes through the Web Crypto API instead.
window.generateRsaKeypair = async (modulusLength) => {
    const keyPair = await crypto.subtle.generateKey(
        { name: 'RSA-OAEP', modulusLength, publicExponent: new Uint8Array([1, 0, 1]), hash: 'SHA-256' },
        true,
        ['encrypt', 'decrypt']);
    const spki = await crypto.subtle.exportKey('spki', keyPair.publicKey);
    const pkcs8 = await crypto.subtle.exportKey('pkcs8', keyPair.privateKey);
    return {
        publicKeySpkiBase64: bytesToB64(new Uint8Array(spki)),
        privateKeyPkcs8Base64: bytesToB64(new Uint8Array(pkcs8))
    };
};

// Strips the PEM header/footer/newlines generateRsaKeypair's caller wraps the public key in
// (see WebCryptoUserKeypairService.FormatPublicKeyPem) back down to raw SPKI DER, base64.
function pemToSpkiBase64(pem) {
    return pem
        .replace('-----BEGIN PUBLIC KEY-----', '')
        .replace('-----END PUBLIC KEY-----', '')
        .replace(/\s/g, '');
}

// Wraps an AES vault key with a member's RSA public key so only they can unwrap it (Sprint 27
// vault sharing). publicKeyPem is what the server hands back from GET /api/users/public-key —
// PEM text, unlike the private key below which stays raw PKCS8 base64 throughout (it's never
// PEM-wrapped — see WebCryptoUserKeypairService.GenerateAsync).
window.wrapKeyRsaOaep = async (publicKeyPem, keyBase64) => {
    const key = await crypto.subtle.importKey(
        'spki', b64ToBytes(pemToSpkiBase64(publicKeyPem)),
        { name: 'RSA-OAEP', hash: 'SHA-256' }, false, ['encrypt']);
    const wrapped = await crypto.subtle.encrypt({ name: 'RSA-OAEP' }, key, b64ToBytes(keyBase64));
    return bytesToB64(new Uint8Array(wrapped));
};

// Inverse of wrapKeyRsaOaep, run by the member unwrapping their own copy of the vault key.
// privateKeyPkcs8Base64 comes from decryptAesGcm-ing the user's EncryptedPrivateKey.
window.unwrapKeyRsaOaep = async (privateKeyPkcs8Base64, wrappedKeyBase64) => {
    const key = await crypto.subtle.importKey(
        'pkcs8', b64ToBytes(privateKeyPkcs8Base64),
        { name: 'RSA-OAEP', hash: 'SHA-256' }, false, ['decrypt']);
    const unwrapped = await crypto.subtle.decrypt({ name: 'RSA-OAEP' }, key, b64ToBytes(wrappedKeyBase64));
    return bytesToB64(new Uint8Array(unwrapped));
};

// AesGcm also throws PlatformNotSupportedException on browser-wasm, same reason as RSA.
// Output layout matches TheShed.Shared.Security.AesEncryptionService (nonce(12) || ciphertext
// || tag(16)): Web Crypto appends the tag to the ciphertext by default, so this is just
// nonce + that, base64-encoded.
window.encryptAesGcm = async (keyBase64, plaintextBase64) => {
    const key = await crypto.subtle.importKey('raw', b64ToBytes(keyBase64), 'AES-GCM', false, ['encrypt']);
    const nonce = crypto.getRandomValues(new Uint8Array(12));
    const sealed = await crypto.subtle.encrypt({ name: 'AES-GCM', iv: nonce }, key, b64ToBytes(plaintextBase64));
    const combined = new Uint8Array(nonce.length + sealed.byteLength);
    combined.set(nonce, 0);
    combined.set(new Uint8Array(sealed), nonce.length);
    return bytesToB64(combined);
};

// Inverse of encryptAesGcm: splits the leading 12-byte nonce back off before handing the rest
// (ciphertext||tag) to Web Crypto, which expects them concatenated the same way it produced them.
window.decryptAesGcm = async (keyBase64, ciphertextBase64) => {
    const key = await crypto.subtle.importKey('raw', b64ToBytes(keyBase64), 'AES-GCM', false, ['decrypt']);
    const combined = b64ToBytes(ciphertextBase64);
    const nonce = combined.slice(0, 12);
    const sealed = combined.slice(12);
    const plaintext = await crypto.subtle.decrypt({ name: 'AES-GCM', iv: nonce }, key, sealed);
    return bytesToB64(new Uint8Array(plaintext));
};

// Rfc2898DeriveBytes.Pbkdf2 (the managed .NET implementation) also runs on browser-wasm, unlike
// RSA/AesGcm — but interpreted, not native, so 600k iterations there froze the tab for ~70-90s
// (single-threaded WASM blocks rendering and input while it's running). Web Crypto's PBKDF2 is
// native in the browser: same iteration count, no freeze.
window.deriveKeyPbkdf2 = async (password, saltBase64, iterations, keyLengthBits) => {
    const keyMaterial = await crypto.subtle.importKey(
        'raw', new TextEncoder().encode(password), 'PBKDF2', false, ['deriveBits']);
    const derived = await crypto.subtle.deriveBits(
        { name: 'PBKDF2', salt: b64ToBytes(saltBase64), iterations, hash: 'SHA-256' },
        keyMaterial, keyLengthBits);
    return bytesToB64(new Uint8Array(derived));
};
