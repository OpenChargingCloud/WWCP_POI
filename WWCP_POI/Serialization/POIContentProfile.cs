/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The fixed static projection and canonicalization contract shared by independent replicas.
/// </summary>
public static class POIContentProfile
{
    /// <summary>
    /// Identifies stored static properties, Styx canonical JSON and deterministic metrological CBOR.
    /// </summary>
    public const String Id = "wwcp-poi-static-v1";

    /// <summary>
    /// The schema-owned transport declaration, excluded from the static digest inputs.
    /// </summary>
    public const String PropertyName = "contentProfile";

    /// <summary>
    /// Reject an unsupported profile rather than interpreting it using the current rules.
    /// </summary>
    public static void Require(String? profile)
    {
        if (profile != Id)
            throw new ArgumentException("Unsupported POI content profile: " + profile);
    }
}
