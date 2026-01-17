using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.EntityFrameworkCore;
using WhereAreThey.Data;
using WhereAreThey.Models;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

public class AdminPasskeyService(
    IFido2 fido2,
    IDbContextFactory<ApplicationDbContext> contextFactory,
    IEventService eventService,
    ILogger<AdminPasskeyService> logger) : IAdminPasskeyService
{
    public async Task<CredentialCreateOptions> GetRegistrationOptionsAsync(string adminEmail)
    {
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

    public async Task<Result<AdminPasskey>> CompleteRegistrationAsync(AuthenticatorAttestationRawResponse attestationRawResponse, CredentialCreateOptions options, string keyName)
    {
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

            return Result<AdminPasskey>.Success(newKey);
        }
        catch (Fido2VerificationException ex)
        {
            logger.LogError(ex, "Passkey registration verification failed");
            return Result<AdminPasskey>.Failure($"Registration failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during passkey registration");
            return Result<AdminPasskey>.Failure("An unexpected error occurred during registration.");
        }
    }

    public async Task<AssertionOptions> GetAssertionOptionsAsync()
    {
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

    public async Task<Result> CompleteAssertionAsync(AuthenticatorAssertionRawResponse assertionRawResponse, AssertionOptions options, string? ipAddress)
    {
        try
        {
            await using var context = await contextFactory.CreateDbContextAsync();
            var credId = assertionRawResponse.Id;
            // In-memory filter for byte[] comparison
            var keys = await context.AdminPasskeys.ToListAsync();
            var key = keys.FirstOrDefault(k => k.CredentialId.SequenceEqual(Convert.FromBase64String(credId.Replace('-', '+').Replace('_', '/').PadRight(4 * ((credId.Length + 3) / 4), '='))));

            if (key == null)
            {
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

    public async Task<List<AdminPasskey>> GetPasskeysAsync()
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        return await context.AdminPasskeys.ToListAsync();
    }

    public async Task<Result> DeletePasskeyAsync(int id)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var key = await context.AdminPasskeys.FindAsync(id);
        if (key == null)
        {
            return Result.Failure("Passkey not found.");
        }

        context.AdminPasskeys.Remove(key);
        await context.SaveChangesAsync();
        return Result.Success();
    }

    private async Task RecordAttempt(string? ipAddress, bool isSuccessful)
    {
        await using var context = await contextFactory.CreateDbContextAsync();
        var attempt = new AdminLoginAttempt
        {
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            IsSuccessful = isSuccessful,
        };
        context.AdminLoginAttempts.Add(attempt);
        await context.SaveChangesAsync();
        eventService.NotifyAdminLoginAttempt(attempt);
    }
}
