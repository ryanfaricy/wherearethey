using Fluid;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class EmailTemplateService(IWebHostEnvironment environment, ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    private static readonly FluidParser Parser = new();

    /// <inheritdoc />
    public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
    {
        logger.LogDebug("Rendering email template {TemplateName}", templateName);
        var templatePath = Path.Combine(environment.ContentRootPath, "Resources", "EmailTemplates", $"{templateName}.liquid");
        
        if (!File.Exists(templatePath))
        {
            logger.LogError("Email template file not found: {TemplatePath}", templatePath);
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }

        var source = await File.ReadAllTextAsync(templatePath);

        if (!Parser.TryParse(source, out var template, out var error))
        {
            logger.LogError("Failed to parse email template {TemplateName}: {Error}", templateName, error);
            throw new InvalidOperationException($"Failed to parse template '{templateName}': {error}");
        }

        var context = new TemplateContext();
        if (model == null)
        {
            return await template.RenderAsync(context);
        }

        context.Options.MemberAccessStrategy.Register(model.GetType());
        context.SetValue("Model", model);
                
        // Expose properties as top-level variables for convenience
        foreach (var prop in model.GetType().GetProperties())
        {
            context.SetValue(prop.Name, prop.GetValue(model));
        }

        var result = await template.RenderAsync(context);
        logger.LogTrace("Successfully rendered template {TemplateName}", templateName);
        return result;
    }
}
