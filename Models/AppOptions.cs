using System.ComponentModel.DataAnnotations;

namespace WhereAreThey.Models;

public class AppOptions
{
    public const string SectionName = "App";
    
    [Required]
    [Url]
    public string BaseUrl { get; set; } = "https://www.aretheyhere.com";
    
    [Required]
    [MinLength(8)]
    public string AdminPassword { get; set; } = "change-me-in-secrets";
    
    public SquareOptions Square { get; set; } = new();
}

public class SquareOptions
{
    public const string SectionName = "Square";
    
    [Required]
    public string ApplicationId { get; set; } = string.Empty;
    
    [Required]
    public string AccessToken { get; set; } = string.Empty;
    
    [Required]
    public string LocationId { get; set; } = string.Empty;
    
    [Required]
    public string Environment { get; set; } = "Sandbox";
}
