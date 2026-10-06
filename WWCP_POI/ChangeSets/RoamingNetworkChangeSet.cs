using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>Describes the kind of operation contained in a roaming network change set.</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<RoamingNetworkChangeKind>))]
public enum RoamingNetworkChangeKind
{
    Add,
    Remove,
    UpdateProperty
}

/// <summary>A single immutable operation against an entity in a roaming network.</summary>
/// <param name="Kind">The operation to perform.</param>
/// <param name="EntityType">A stable, application-defined entity type (for example <c>ChargingStation</c>).</param>
/// <param name="EntityId">The entity's canonical identifier.</param>
/// <param name="PropertyName">The property being changed, for property updates.</param>
/// <param name="OldValue">The expected previous value, when supplied for conflict detection.</param>
/// <param name="NewValue">The new property value or full entity document.</param>
public sealed record RoamingNetworkChange(
    RoamingNetworkChangeKind Kind,
    string EntityType,
    string EntityId,
    string? PropertyName,
    JsonElement? OldValue,
    JsonElement? NewValue)
{
    /// <summary>Add an entity using its JSON representation.</summary>
    public static RoamingNetworkChange Add(string entityType, string entityId, JsonElement entity)
        => new(RoamingNetworkChangeKind.Add, Required(entityType, nameof(entityType)), Required(entityId, nameof(entityId)), null, null, entity.Clone());

    /// <summary>Remove an entity.</summary>
    public static RoamingNetworkChange Remove(string entityType, string entityId)
        => new(RoamingNetworkChangeKind.Remove, Required(entityType, nameof(entityType)), Required(entityId, nameof(entityId)), null, null, null);

    /// <summary>Update one entity property, optionally asserting its previous value.</summary>
    public static RoamingNetworkChange UpdateProperty(string entityType, string entityId, string propertyName, JsonElement? oldValue, JsonElement newValue)
        => new(RoamingNetworkChangeKind.UpdateProperty, Required(entityType, nameof(entityType)), Required(entityId, nameof(entityId)), Required(propertyName, nameof(propertyName)), oldValue?.Clone(), newValue.Clone());

    private static string Required(string value, string parameterName)
        => !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);
}

/// <summary>
/// An immutable, serializable batch of changes to one roaming network. The base revision
/// allows a future copy-on-write applier to reject changes built against stale data.
/// </summary>
public sealed record RoamingNetworkChangeSet
{
    /// <summary>Creates a change set.</summary>
    [System.Text.Json.Serialization.JsonConstructor]
    public RoamingNetworkChangeSet(string id,
                                   string roamingNetworkId,
                                   long baseRevision,
                                   DateTimeOffset createdAt,
                                   ImmutableArray<RoamingNetworkChange> changes,
                                   RoamingNetworkChangeSetSignature? signature = null)
    {
        Id = Required(id, nameof(id));
        RoamingNetworkId = Required(roamingNetworkId, nameof(roamingNetworkId));
        if (baseRevision < 0)
            throw new ArgumentOutOfRangeException(nameof(baseRevision));
        if (changes.IsDefault || changes.Any(change => change is null))
            throw new ArgumentException("Changes must be an initialized collection without null entries.", nameof(changes));

        BaseRevision = baseRevision;
        CreatedAt = createdAt;
        Changes = changes;
        Signature = signature;
    }

    public string Id { get; }
    public string RoamingNetworkId { get; }
    public long BaseRevision { get; }
    public DateTimeOffset CreatedAt { get; }
    public ImmutableArray<RoamingNetworkChange> Changes { get; }
    public RoamingNetworkChangeSetSignature? Signature { get; }

    private static string Required(string value, string parameterName)
        => !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);
}

/// <summary>Signature envelope reserved for later canonical signing and verification.</summary>
public sealed record RoamingNetworkChangeSetSignature(string Algorithm, string KeyId, string Value);
