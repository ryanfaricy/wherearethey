namespace WhereAreThey.Models;

public class AppOptions
{
    public const string SectionName = "App";
    public string BaseUrl { get; set; } = "https://www.aretheyhere.com";
    public string AdminPassword { get; set; } = "change-me-in-secrets";
    public SquareOptions Square { get; set; } = new();
}

public class SquareOptions
{
    public const string SectionName = "Square";
    public string ApplicationId { get; set; } = string.Empty;
    public string AccessToken { get; set; } = string.Empty;
    public string LocationId { get; set; } = string.Empty;
    public string Environment { get; set; } = "Sandbox";
}
