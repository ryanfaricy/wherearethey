using Fido2NetLib;
using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for managing administrative passkeys (WebAuthn).
/// </summary>
public interface IAdminPasskeyService
{
    /// <summary>
    /// Gets the options for registering a new passkey.
    /// </summary>
    /// <param name="adminEmail">The email address of the administrator.</param>
    /// <returns>Credential creation options.</returns>
    Task<CredentialCreateOptions> GetRegistrationOptionsAsync(string adminEmail);

    /// <summary>
    /// Completes the registration of a new passkey.
    /// </summary>
    /// <param name="attestationRawResponse">The raw response from the authenticator.</param>
    /// <param name="options">The creation options that were used.</param>
    /// <param name="keyName">A friendly name for the key.</param>
    /// <returns>A Result containing the new passkey or an error message.</returns>
    Task<Result<AdminPasskey>> CompleteRegistrationAsync(AuthenticatorAttestationRawResponse attestationRawResponse, CredentialCreateOptions options, string keyName);

    /// <summary>
    /// Gets the options for passkey assertion (login).
    /// </summary>
    /// <returns>Assertion options.</returns>
    Task<AssertionOptions> GetAssertionOptionsAsync();

    /// <summary>
    /// Completes the assertion process (login).
    /// </summary>
    /// <param name="assertionRawResponse">The raw response from the authenticator.</param>
    /// <param name="options">The assertion options that were used.</param>
    /// <param name="ipAddress">The IP address of the requester.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> CompleteAssertionAsync(AuthenticatorAssertionRawResponse assertionRawResponse, AssertionOptions options, string? ipAddress);

    /// <summary>
    /// Gets all registered passkeys.
    /// </summary>
    /// <returns>A list of passkeys.</returns>
    Task<List<AdminPasskey>> GetPasskeysAsync();

    /// <summary>
    /// Deletes a passkey.
    /// </summary>
    /// <param name="id">The identifier of the passkey to delete.</param>
    /// <returns>A Result indicating success or failure.</returns>
    Task<Result> DeletePasskeyAsync(int id);
}
