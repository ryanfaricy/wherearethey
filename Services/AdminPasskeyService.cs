using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class AdminPasskeyService(
    IFido2 fido2,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IEventService eventService,
    ILogger<AdminPasskeyService> logger) : IAdminPasskeyService
{
    /// <inheritdoc />
    public async Task<CredentialCreateOptions> GetRegistrationOptionsAsync(string adminEmail)
    {
        logger.LogInformation("Requesting passkey registration options for {AdminEmail}", adminEmail);
        await Task.CompletedTask;
        
        var user = new Fido2User
        {
            DisplayName = "Admin",
            Name = adminEmail,
            Id = "admin-user-id"u8.ToArray(),
        };

        return fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = user,
            ExcludeCredentials = new List<PublicKeyCredentialDescriptor>(),
            AuthenticatorSelection = new AuthenticatorSelection
            {
                UserVerification = UserVerificationRequirement.Preferred,
                ResidentKey = ResidentKeyRequirement.Preferred,
            },
        });
    }

    /// <inheritdoc />
    public async Task<Result<AdminPasskey>> CompleteRegistrationAsync(AuthenticatorAttestationRawResponse attestationRawResponse, CredentialCreateOptions options, string keyName)
    {
        logger.LogInformation("Completing passkey registration for key: {KeyName}", keyName);
        try
        {
            var result = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
            {
                AttestationResponse = attestationRawResponse,
                OriginalOptions = options,
                IsCredentialIdUniqueToUserCallback = (_, _) => Task.FromResult(true),
            });

            var newKey = new AdminPasskey
            {
                Name = keyName,
                CredentialId = result.Id,
                PublicKey = result.PublicKey,
                Counter = result.SignCount,
                CredType = result.Type.ToString(),
                Aaguid = result.AaGuid.ToString(),
                CreatedAt = DateTime.UtcNow,
            };

            await using var context = await contextFactory.CreateDbContextAsync();
            context.AdminPasskeys.Add(newKey);
            await context.SaveChangesAsync();

            logger.LogInformation("Passkey {KeyName} registered successfully", keyName);
            return Result<AdminPasskey>.Success(newKey);
        }
        catch (Fido2VerificationException ex)
        {
            logger.LogError(ex, "Passkey registration verification failed for key: {KeyName}", keyName);
            return Result<AdminPasskey>.Failure($"Registration failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during passkey registration for key: {KeyName}", keyName);
            return Result<AdminPasskey>.Failure("An unexpected error occurred during registration.");
        }
    }

    /// <inheritdoc />
    public async Task<AssertionOptions> GetAssertionOptionsAsync()
    {
        logger.LogInformation("Requesting passkey assertion options");
        var existingKeys = await GetPasskeysAsync();
        var allowedCredentials = existingKeys
            .Select(k => new PublicKeyCredentialDescriptor(k.CredentialId))
            .ToList();

        return fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowedCredentials,
            UserVerification = UserVerificationRequirement.Preferred,
        });
    }

    /// <inheritdoc />
    public async Task<Result> CompleteAssertionAsync(AuthenticatorAssertionRawResponse assertionRawResponse, AssertionOptions options, string? ipAddress)
    {
        logger.LogInformation("Completing passkey assertion for IP {IpAddress}", ipAddress);
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var credId = assertionRawResponse.Id;
            // In-memory filter for byte[] comparison
            var keys = await context.AdminPasskeys.ToListAsync();
            var key = keys.FirstOrDefault(k => k.CredentialId.SequenceEqual(Convert.FromBase64String(credId.Replace('-', '+').Replace('_', '/').PadRight(4 * ((credId.Length + 3) / 4), '='))));

            if (key == null)
            {
                logger.LogWarning("Passkey assertion failed: unknown credential ID {CredentialId} from IP {IpAddress}", credId, ipAddress);
                return Result.Failure("Unknown credential");
            }

            var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
            {
                AssertionResponse = assertionRawResponse,
                OriginalOptions = options,
                StoredPublicKey = key.PublicKey,
                StoredSignatureCounter = key.Counter,
                IsUserHandleOwnerOfCredentialIdCallback = (_, _) => Task.FromResult(true),
            });

            // Update counter to prevent replay attacks
            key.Counter = result.SignCount;
            await context.SaveChangesAsync();

            logger.LogInformation("Passkey assertion successful for key {KeyName} from IP {IpAddress}", key.Name, ipAddress);
            await RecordAttempt(ipAddress, true);
            return Result.Success();
        }
        catch (Fido2VerificationException ex)
        {
            logger.LogWarning(ex, "Passkey assertion verification failed for IP {IpAddress}", ipAddress);
            await RecordAttempt(ipAddress, false);
            return Result.Failure($"Login failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during passkey assertion for IP {IpAddress}", ipAddress);
            return Result.Failure("An unexpected error occurred during login.");
        }
    }

    /// <inheritdoc />
    public async Task<List<AdminPasskey>> GetPasskeysAsync()
    {
        logger.LogDebug("Retrieving all registered passkeys");
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.AdminPasskeys.ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result> DeletePasskeyAsync(int id)
    {
        logger.LogInformation("Deleting passkey with ID {Id}", id);
        await using var context = await contextFactory.CreateDbContextAsync();
        var key = await context.AdminPasskeys.FindAsync(id);
        if (key == null)
        {
            logger.LogWarning("Passkey with ID {Id} not found for deletion", id);
            return Result.Failure("Passkey not found.");
        }

        context.AdminPasskeys.Remove(key);
        await context.SaveChangesAsync();
        logger.LogInformation("Passkey {KeyName} (ID {Id}) deleted successfully", key.Name, id);
        return Result.Success();
    }

    private async Task RecordAttempt(string? ipAddress, bool isSuccessful)
    {
        logger.LogInformation("Recording admin login attempt from IP {IpAddress}, Successful: {IsSuccessful}", ipAddress, isSuccessful);
        await using var context = await contextFactory.CreateDbContextAsync();
        var attempt = new AdminLoginAttempt
        {
            CreatedAt = DateTime.UtcNow,
            IpAddress = ipAddress,
            IsSuccessful = isSuccessful,
        };
        context.AdminLoginAttempts.Add(attempt);
        await context.SaveChangesAsync();
        eventService.NotifyAdminLoginAttempt(attempt);
    }
}
