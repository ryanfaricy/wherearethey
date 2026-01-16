using Fluid;
using WhereAreThey.Services.Interfaces;

namespace WhereAreThey.Services;

/// <inheritdoc />
public class EmailTemplateService : IEmailTemplateService
{
    private static readonly FluidParser Parser = new();
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmailTemplateService"/> class.
    /// </summary>
    /// <param name="environment">The web host environment.</param>
    public EmailTemplateService(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <inheritdoc />
    public async Task<string> RenderTemplateAsync<T>(string templateName, T model)
    {
        var templatePath = Path.Combine(_environment.ContentRootPath, "Resources", "EmailTemplates", $"{templateName}.liquid");
        
        if (!File.Exists(templatePath))
        {
            throw new FileNotFoundException($"Template file not found: {templatePath}");
        }

        var source = await File.ReadAllTextAsync(templatePath);

        if (Parser.TryParse(source, out var template, out var error))
        {
            var context = new TemplateContext();
            if (model != null)
            {
                context.Options.MemberAccessStrategy.Register(model.GetType());
                context.SetValue("Model", model);
                
                // Expose properties as top-level variables for convenience
                foreach (var prop in model.GetType().GetProperties())
                {
                    context.SetValue(prop.Name, prop.GetValue(model));
                }
            }
            
            return await template.RenderAsync(context);
        }

        throw new InvalidOperationException($"Failed to parse template '{templateName}': {error}");
    }
}
