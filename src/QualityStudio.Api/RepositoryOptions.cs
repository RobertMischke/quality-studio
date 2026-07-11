namespace QualityStudio.Api;

public sealed class RepositoryOptions
{
    public const string SectionName = "QualityStudio";

    public string RepositoryRoot { get; set; } = ".";

    public string[] AllowedOrigins { get; set; } = ["http://localhost:4200"];
}
