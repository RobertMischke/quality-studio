namespace QualityStudio.Api;

/// <summary>Confines repository operations to one immutable registry entry.</summary>
public sealed class RepositoryAccess
{
    private readonly string root;
    private readonly ReviewMetaIndex? metaIndex;

    public RepositoryAccess(string root, ReviewMetaIndex? metaIndex = null)
    {
        this.root = Path.GetFullPath(root);
        this.metaIndex = metaIndex;
        if (!Directory.Exists(this.root))
        {
            throw new DirectoryNotFoundException($"Repository root does not exist: {this.root}");
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
        if (!PathConfinement.IsWithin(root, absolute))
        {
            throw new ArgumentException("Path escapes the selected repository root.", nameof(path));
        }

        PathConfinement.RejectReparseTraversal(root, absolute);

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

    public IReadOnlyList<System.Text.Json.JsonElement> ReadMetaDocuments(string relativePath)
    {
        var normalized = NormalizeRelativePath(relativePath);
        return (metaIndex ?? throw new InvalidOperationException("Review metadata indexing is unavailable."))
            .Read(root, normalized);
    }

    public string FindMetaDocument(string relativePath, string kind)
    {
        var normalized = NormalizeRelativePath(relativePath);
        return (metaIndex ?? throw new InvalidOperationException("Review metadata indexing is unavailable."))
            .Find(root, normalized, kind);
    }
}
