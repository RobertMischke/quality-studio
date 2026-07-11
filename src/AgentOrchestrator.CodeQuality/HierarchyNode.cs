using System.Collections.ObjectModel;

namespace AgentOrchestrator.CodeQuality;

/// <summary>A derived source unit and its review metadata.</summary>
public sealed class HierarchyNode
{
    private readonly List<HierarchyNode> children = [];
    private readonly Dictionary<ReviewKind, AttachedReviewMetaDocument> documents = [];

    public HierarchyNode(string id, string name, ReviewLevel level, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Id = id;
        Name = name;
        Level = level;
        Path = path;
    }

    public string Id { get; }

    public string Name { get; }

    public ReviewLevel Level { get; }

    public string Path { get; }

    public IReadOnlyList<HierarchyNode> Children => children;

    /// <summary>At most one independently authored document per review kind.</summary>
    public IReadOnlyDictionary<ReviewKind, AttachedReviewMetaDocument> Documents =>
        new ReadOnlyDictionary<ReviewKind, AttachedReviewMetaDocument>(documents);

    /// <summary>Per-kind direct and rolled-up state for this node's current subtree.</summary>
    public IReadOnlyDictionary<ReviewKind, KindAggregation> AggregatedStates =>
        HierarchyAggregation.ForAllKinds(this);

    public HierarchyNode AddChild(HierarchyNode child)
    {
        ArgumentNullException.ThrowIfNull(child);
        if ((int)child.Level != (int)Level + 1)
        {
            throw new ArgumentException($"A {Level} node can only contain the next hierarchy level.", nameof(child));
        }

        children.Add(child);
        return this;
    }

    public HierarchyNode Attach(AttachedReviewMetaDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (!StringComparer.Ordinal.Equals(document.UnitId, Id))
        {
            throw new ArgumentException("The document belongs to a different hierarchy unit.", nameof(document));
        }

        if (!documents.TryAdd(document.Kind, document))
        {
            throw new InvalidOperationException($"Unit '{Id}' already has a {document.Kind} review document.");
        }

        return this;
    }
}
