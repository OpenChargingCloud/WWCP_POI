/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text.Json.Serialization;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// One immutable ownership step from a graph entity to a nested POI value or collection element.
/// Property names are schema fields; collection elements use their existing domain identifiers.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record POIElementPathSegment
{
    /// <summary>
    /// Create a property step with an optional element identity. Arrays require an identity.
    /// Singleton values can use their ownership slot, optionally asserting their existing identity.
    /// </summary>
    [JsonConstructor]
    public POIElementPathSegment(String propertyName, String? elementId = null)
    {
        if (String.IsNullOrWhiteSpace(propertyName))
            throw new ArgumentException("A schema property name is required.", nameof(propertyName));
        if (elementId is not null && String.IsNullOrWhiteSpace(elementId))
            throw new ArgumentException("An element identifier must not be empty.", nameof(elementId));
        PropertyName = propertyName;
        ElementId = elementId;
    }

    /// <summary>
    /// The exact schema property name on the current owner.
    /// </summary>
    [JsonInclude, JsonRequired]
    public String PropertyName { get; private init; }

    /// <summary>
    /// The existing element identity, mandatory for arrays and optional for identified singletons.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public String? ElementId { get; }
}
