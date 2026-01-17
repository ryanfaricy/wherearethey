using Fido2NetLib;
using WhereAreThey.Models;

namespace WhereAreThey.Services.Interfaces;

public interface IAdminPasskeyService
{
    Task<CredentialCreateOptions> GetRegistrationOptionsAsync(string adminEmail);
    Task<Result<AdminPasskey>> CompleteRegistrationAsync(AuthenticatorAttestationRawResponse attestationRawResponse, CredentialCreateOptions options, string keyName);
    Task<AssertionOptions> GetAssertionOptionsAsync();
    Task<Result> CompleteAssertionAsync(AuthenticatorAssertionRawResponse assertionRawResponse, AssertionOptions options, string? ipAddress);
    Task<List<AdminPasskey>> GetPasskeysAsync();
    Task<Result> DeletePasskeyAsync(int id);
}
