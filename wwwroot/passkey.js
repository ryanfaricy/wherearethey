export async function registerPasskey(optionsJson) {
    const options = JSON.parse(optionsJson);
    console.debug('Passkey registration options:', options);
    
    // Convert base64url strings to ArrayBuffers
    options.challenge = base64urlToBuffer(options.challenge);
    options.user.id = base64urlToBuffer(options.user.id);
    if (options.excludeCredentials) {
        options.excludeCredentials.forEach(c => c.id = base64urlToBuffer(c.id));
    }

    try {
        const credential = await navigator.credentials.create({
            publicKey: options
        });
        console.debug('Passkey registration credential:', credential);

        // Convert ArrayBuffers back to base64url strings
        return JSON.stringify({
            id: credential.id,
            rawId: bufferToBase64url(credential.rawId),
            type: credential.type,
            extensions: credential.getClientExtensionResults(),
            response: {
                attestationObject: bufferToBase64url(credential.response.attestationObject),
                clientDataJSON: bufferToBase64url(credential.response.clientDataJSON),
                transports: credential.response.getTransports ? credential.response.getTransports() : []
            }
        });
    } catch (e) {
        console.error('Passkey registration error:', e);
        throw e;
    }
}

export async function loginWithPasskey(optionsJson) {
    const options = JSON.parse(optionsJson);
    console.debug('Passkey login options:', options);
    
    // Convert base64url strings to ArrayBuffers
    options.challenge = base64urlToBuffer(options.challenge);
    if (options.allowCredentials) {
        options.allowCredentials.forEach(c => c.id = base64urlToBuffer(c.id));
    }

    try {
        const credential = await navigator.credentials.get({
            publicKey: options
        });
        console.debug('Passkey login credential:', credential);

        // Convert ArrayBuffers back to base64url strings
        return JSON.stringify({
            id: credential.id,
            rawId: bufferToBase64url(credential.rawId),
            type: credential.type,
            extensions: credential.getClientExtensionResults(),
            response: {
                authenticatorData: bufferToBase64url(credential.response.authenticatorData),
                clientDataJSON: bufferToBase64url(credential.response.clientDataJSON),
                signature: bufferToBase64url(credential.response.signature),
                userHandle: credential.response.userHandle ? bufferToBase64url(credential.response.userHandle) : null
            }
        });
    } catch (e) {
        console.error('Passkey login error:', e);
        throw e;
    }
}

function base64urlToBuffer(base64url) {
    const base64 = base64url.replace(/-/g, '+').replace(/_/g, '/');
    const padLen = (4 - (base64.length % 4)) % 4;
    const padded = base64 + '='.repeat(padLen);
    const binary = atob(padded);
    const buffer = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) {
        buffer[i] = binary.charCodeAt(i);
    }
    return buffer.buffer;
}

function bufferToBase64url(buffer) {
    const bytes = new Uint8Array(buffer);
    let binary = '';
    for (let i = 0; i < bytes.byteLength; i++) {
        binary += String.fromCharCode(bytes[i]);
    }
    const base64 = btoa(binary);
    return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}
