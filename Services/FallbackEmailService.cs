namespace WhereAreThey.Services;

public class FallbackEmailService(IEnumerable<IEmailService> emailServices, ILogger<FallbackEmailService> logger) : IEmailService
{
    private readonly List<IEmailService> _services = emailServices.Where(s => s.GetType() != typeof(FallbackEmailService)).ToList();

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        if (_services.Count == 0)
        {
            logger.LogError("No email services configured for fallback.");
            return;
        }

        List<Exception> exceptions = new();

        foreach (var service in _services)
        {
            try
            {
                logger.LogDebug("Attempting to send email via {ServiceType}", service.GetType().Name);
                await service.SendEmailAsync(to, subject, body);
                return; // Success!
            }
            catch (InvalidOperationException ex)
            {
                // Likely a configuration missing for this provider, skip it without much noise
                logger.LogDebug("Skipping {ServiceType} as it is not fully configured: {Message}", service.GetType().Name, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send email via {ServiceType}. Trying next provider...", service.GetType().Name);
                exceptions.Add(ex);
            }
        }

        if (exceptions.Count > 0)
        {
            logger.LogCritical("All configured email providers failed. Total attempts: {Count}", exceptions.Count);
            throw new AggregateException("All configured email providers failed to send the email.", exceptions);
        }

        logger.LogWarning("None of the email providers were configured. Email to {To} was NOT sent.", to);
    }
}
