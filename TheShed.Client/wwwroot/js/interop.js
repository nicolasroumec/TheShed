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
