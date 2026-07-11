namespace AgentOrchestrator.CodeQuality;

/// <summary>Describes whether review metadata still applies to its subject.</summary>
public enum ReviewState
{
    /// <summary>No metadata document exists at the relevant scope.</summary>
    NotReviewed,

    /// <summary>The reviewed subject still matches the metadata document.</summary>
    Current,

    /// <summary>The subject has changed since the metadata document was produced.</summary>
    Stale,
}
