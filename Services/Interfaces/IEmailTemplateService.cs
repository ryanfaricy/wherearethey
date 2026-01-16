namespace WhereAreThey.Services.Interfaces;

/// <summary>
/// Service for rendering email templates.
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders a template with the specified model.
    /// </summary>
    /// <typeparam name="T">The type of the model.</typeparam>
    /// <param name="templateName">The name of the template file (without extension).</param>
    /// <param name="model">The model containing data for the template.</param>
    /// <returns>The rendered HTML content.</returns>
    Task<string> RenderTemplateAsync<T>(string templateName, T model);
}
