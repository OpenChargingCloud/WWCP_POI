using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

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
public sealed record RoamingNetworkChange
{
    /// <summary>Create a validated operation owning independent copies of its JSON values.</summary>
    /// <param name="kind">The operation to perform.</param>
    /// <param name="entityType">A stable entity type, for example <c>ChargingStation</c>.</param>
    /// <param name="entityId">The entity's canonical identifier.</param>
    /// <param name="propertyName">The property being changed, for property updates.</param>
    /// <param name="oldValue">An optional expected previous value for conflict detection.</param>
    /// <param name="newValue">The new property value or full entity document. A defined JSON null clears a property.</param>
    [JsonConstructor]
    public RoamingNetworkChange(RoamingNetworkChangeKind kind, string entityType, string entityId,
                                string? propertyName, JsonElement? oldValue, JsonElement? newValue)
    {
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        EntityType = Required(entityType, nameof(entityType));
        EntityId = Required(entityId, nameof(entityId));
        if (oldValue?.ValueKind == JsonValueKind.Undefined || newValue?.ValueKind == JsonValueKind.Undefined)
            throw new ArgumentException("JSON values must be defined.");

        switch (kind)
        {
            case RoamingNetworkChangeKind.Add when newValue?.ValueKind != JsonValueKind.Object ||
                                                   oldValue.HasValue || propertyName is not null:
                throw new ArgumentException("Add requires an entity object and no old value or property name.");
            case RoamingNetworkChangeKind.Remove when newValue.HasValue || propertyName is not null:
                throw new ArgumentException("Remove must not contain a new value or property name.");
            case RoamingNetworkChangeKind.UpdateProperty when !newValue.HasValue || string.IsNullOrWhiteSpace(propertyName):
                throw new ArgumentException("UpdateProperty requires a property name and a new JSON value.");
        }

        Kind = kind;
        PropertyName = propertyName;
        OldValue = oldValue?.Clone();
        NewValue = newValue?.Clone();
    }

    [JsonInclude, JsonRequired]
    public RoamingNetworkChangeKind Kind { get; private init; }
    [JsonInclude, JsonRequired]
    public string EntityType { get; private init; }
    [JsonInclude, JsonRequired]
    public string EntityId { get; private init; }
    public string? PropertyName { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalJsonElementConverter))]
    public JsonElement? OldValue { get; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonConverter(typeof(OptionalJsonElementConverter))]
    public JsonElement? NewValue { get; }

    /// <summary>Add an entity using its JSON representation.</summary>
    public static RoamingNetworkChange Add(string entityType, string entityId, JsonElement entity)
        => new(RoamingNetworkChangeKind.Add, entityType, entityId, null, null, entity);

    /// <summary>Remove an entity.</summary>
    public static RoamingNetworkChange Remove(string entityType, string entityId)
        => new(RoamingNetworkChangeKind.Remove, entityType, entityId, null, null, null);

    /// <summary>Update one entity property, optionally asserting its previous value.</summary>
    public static RoamingNetworkChange UpdateProperty(string entityType, string entityId, string propertyName, JsonElement? oldValue, JsonElement newValue)
        => new(RoamingNetworkChangeKind.UpdateProperty, entityType, entityId, propertyName, oldValue, newValue);

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

    [JsonInclude, JsonRequired] public string Id { get; private init; }
    [JsonInclude, JsonRequired] public string RoamingNetworkId { get; private init; }
    [JsonInclude, JsonRequired] public long BaseRevision { get; private init; }
    [JsonInclude, JsonRequired] public DateTimeOffset CreatedAt { get; private init; }
    [JsonInclude, JsonRequired] public ImmutableArray<RoamingNetworkChange> Changes { get; private init; }
    public RoamingNetworkChangeSetSignature? Signature { get; }

    private static string Required(string value, string parameterName)
        => !string.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);
}

/// <summary>Signature envelope reserved for later canonical signing and verification.</summary>
public sealed record RoamingNetworkChangeSetSignature(string Algorithm, string KeyId, string Value);
