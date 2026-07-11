using System.Text.Json;
using AgentOrchestrator.CodeQuality;
using Microsoft.Extensions.Options;

namespace QualityStudio.Api;

public sealed class RepositoryAccess
{
    private readonly string root;

    public RepositoryAccess(IOptions<RepositoryOptions> options, IHostEnvironment environment)
    {
        var configured = options.Value.RepositoryRoot;
        root = Path.GetFullPath(Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(environment.ContentRootPath, configured));
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Configured repository root does not exist: {root}");
        }
    }

    public string Root => root;

    public string NormalizeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path == ".")
        {
            return ".";
        }

        if (Path.IsPathRooted(path))
        {
            throw new ArgumentException("Path must be repository-relative.", nameof(path));
        }

        var absolute = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));
        var prefix = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!absolute.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Path escapes the configured repository root.", nameof(path));
        }

        return Path.GetRelativePath(root, absolute).Replace('\\', '/');
    }

    public string ResolveFile(string? path)
    {
        var relative = NormalizeRelativePath(path);
        if (relative == ".")
        {
            throw new ArgumentException("A file path is required.", nameof(path));
        }

        var absolute = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(absolute))
        {
            throw new FileNotFoundException($"Repository file was not found: {relative}", relative);
        }

        return absolute;
    }

    public IReadOnlyList<JsonElement> ReadMetaDocuments(string relativePath)
    {
        var result = new List<JsonElement>();
        foreach (var candidate in Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories)
                     .Where(candidate => candidate.Contains(".review-meta.", StringComparison.Ordinal)))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(candidate));
            if (document.RootElement.TryGetProperty("unit", out var unit) &&
                unit.TryGetProperty("path", out var unitPath) &&
                string.Equals(NormalizeStoredPath(unitPath.GetString()), relativePath, StringComparison.Ordinal))
            {
                result.Add(document.RootElement.Clone());
            }
        }

        return result;
    }

    private static string? NormalizeStoredPath(string? path) => path?.Replace('\\', '/').TrimStart('/');
}
