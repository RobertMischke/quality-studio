using System.Text;

namespace AgentOrchestrator.CodeQuality;

public sealed record ReviewInput(
    string Id,
    string Source,
    string Scope,
    int Priority,
    IReadOnlyList<string> Kinds,
    IReadOnlyList<string> Levels,
    string Content,
    string IncludedContent,
    bool Truncated);

public sealed record InputOmission(string Id, string Source, string Reason, int OmittedCharacters);

public sealed record ResolvedInputs(
    string Kind,
    string Level,
    int BudgetCharacters,
    int IncludedCharacters,
    IReadOnlyList<ReviewInput> Inputs,
    IReadOnlyList<InputOmission> Omissions)
{
    public bool Complete => Omissions.All(omission => omission.Reason == "overridden-by-project");

    public string Guidelines(string scope)
    {
        var selected = Inputs.Where(input => input.Scope == scope && input.IncludedContent.Length > 0).ToArray();
        return selected.Length == 0
            ? "(none supplied)"
            : string.Join("\n\n", selected.Select(input => $"## {input.Id}\n{input.IncludedContent}"));
    }
}

public sealed class InputResolver
{
    public const int DefaultBudgetCharacters = 12_000;

    public ResolvedInputs Resolve(
        string repositoryRoot,
        string kind,
        ReviewLevel level,
        string? globalInputsDirectory = null,
        int budgetCharacters = DefaultBudgetCharacters)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot)) throw new ArgumentException("A repository root is required.", nameof(repositoryRoot));
        if (!Enum.TryParse<ReviewKind>(kind, true, out _)) throw new ArgumentException($"Unsupported review kind: {kind}", nameof(kind));
        if (budgetCharacters < 0) throw new ArgumentOutOfRangeException(nameof(budgetCharacters));

        var normalizedKind = kind.ToLowerInvariant();
        var normalizedLevel = level.ToString().ToLowerInvariant();
        var global = ReadDirectory(globalInputsDirectory, "global", normalizedKind, normalizedLevel);
        var projectDirectory = Path.Combine(Path.GetFullPath(repositoryRoot), ".quality", "inputs");
        var project = ReadDirectory(projectDirectory, "project", normalizedKind, normalizedLevel);
        var projectIds = project.Select(input => input.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var omissions = global
            .Where(input => projectIds.Contains(input.Id))
            .Select(input => new InputOmission(input.Id, input.Source, "overridden-by-project", 0))
            .ToList();
        var effective = global.Where(input => !projectIds.Contains(input.Id)).Concat(project).ToArray();

        var remaining = budgetCharacters;
        var included = new List<ReviewInput>();
        foreach (var input in effective)
        {
            var take = Math.Min(remaining, input.Content.Length);
            var text = input.Content[..take];
            var truncated = take < input.Content.Length;
            included.Add(input with { IncludedContent = text, Truncated = truncated });
            remaining -= take;
            if (truncated)
            {
                omissions.Add(new InputOmission(input.Id, input.Source,
                    take == 0 ? "budget-exhausted" : "truncated-to-budget", input.Content.Length - take));
            }
        }

        return new ResolvedInputs(normalizedKind, normalizedLevel, budgetCharacters,
            budgetCharacters - remaining, included, omissions);
    }

    private static IReadOnlyList<ReviewInput> ReadDirectory(
        string? directory, string scope, string kind, string level)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory)) return [];
        return Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            .Select(path => Parse(path, scope))
            .Where(input => Applies(input.Kinds, kind) && Applies(input.Levels, level))
            .OrderByDescending(input => input.Priority)
            .ThenBy(input => input.Id, StringComparer.Ordinal)
            .ThenBy(input => input.Source, StringComparer.Ordinal)
            .ToArray();
    }

    private static ReviewInput Parse(string path, string scope)
    {
        var text = File.ReadAllText(path).Replace("\r\n", "\n", StringComparison.Ordinal);
        if (!text.StartsWith("---\n", StringComparison.Ordinal))
            throw new InputFormatException($"Input '{path}' must start with YAML frontmatter.");
        var end = text.IndexOf("\n---\n", 4, StringComparison.Ordinal);
        if (end < 0) throw new InputFormatException($"Input '{path}' has unterminated frontmatter.");

        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in text[4..end].Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0) throw new InputFormatException($"Input '{path}' has invalid frontmatter line '{line}'.");
            fields[line[..separator].Trim()] = line[(separator + 1)..].Trim();
        }

        if (!fields.TryGetValue("id", out var id) || string.IsNullOrWhiteSpace(id))
            throw new InputFormatException($"Input '{path}' requires an id.");
        var priority = fields.TryGetValue("priority", out var rawPriority) && int.TryParse(rawPriority, out var parsed)
            ? parsed
            : throw new InputFormatException($"Input '{path}' requires an integer priority.");
        var kinds = Values(fields, "kinds", "kind", "kind-applicability");
        var levels = Values(fields, "levels", "level", "level-applicability");
        var content = text[(end + 5)..].Trim();
        return new ReviewInput(id.Trim(), Path.GetFullPath(path), scope, priority, kinds, levels, content, string.Empty, false);
    }

    private static IReadOnlyList<string> Values(Dictionary<string, string> fields, params string[] names)
    {
        var raw = names.Select(name => fields.GetValueOrDefault(name)).FirstOrDefault(value => value is not null) ?? "all";
        return raw.Trim().TrimStart('[').TrimEnd(']').Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.Trim('"', '\'').ToLowerInvariant()).ToArray();
    }

    private static bool Applies(IReadOnlyList<string> values, string value) =>
        values.Contains("all", StringComparer.OrdinalIgnoreCase) || values.Contains("*", StringComparer.Ordinal) ||
        values.Contains(value, StringComparer.OrdinalIgnoreCase);
}

public sealed class InputFormatException(string message) : Exception(message);
