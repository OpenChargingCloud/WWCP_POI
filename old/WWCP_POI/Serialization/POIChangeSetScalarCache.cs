/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Reuse successful signed scalar codec results during one archive ChangeSet payload call.
/// Numeric tokens and schema SI text stay exact; occurrence ownership and depth are checked anew.
/// </summary>
internal sealed class POIChangeSetScalarCache
{
    internal const Int32 MaxEntries = 64;
    internal const Int32 MaxTextCharacters = 256;
    internal const Int32 MaxTotalTextCharacters = 8192;
    private readonly Dictionary<(JsonValueKind Kind, String Text), CBORValue> values = [];
    internal Int32 Entries => values.Count;
    internal Int32 TextCharacters { get; private set; }

    internal CBORValue Read(JsonElement value, String path, HashSet<String> readings)
    {
        if (value.ValueKind != JsonValueKind.Number &&
            (value.ValueKind != JsonValueKind.String || !readings.Contains(path)))
            throw new ArgumentException("The signed scalar cache accepts only numbers and schema reading strings.");
        var text = value.ValueKind == JsonValueKind.Number ? value.GetRawText() : value.GetString()!;
        var key = (value.ValueKind, text);
        if (values.TryGetValue(key, out var result)) return result;
        result = RoamingNetworkChangeSet.EncodeTransportScalar(value, path, readings);
        if (values.Count < MaxEntries && text.Length <= MaxTextCharacters &&
            text.Length <= MaxTotalTextCharacters - TextCharacters && POICBORReadingCache.CanRetain(result))
        {
            values.Add(key, result); TextCharacters += text.Length;
        }
        return result;
    }
}
