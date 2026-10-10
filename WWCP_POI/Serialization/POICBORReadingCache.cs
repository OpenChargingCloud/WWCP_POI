/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Reuse successful Styx metrological scalar trees during one archive POI payload call.
/// Exact text binds the scalar codec; schema ownership and depth are checked at every use.
/// </summary>
internal sealed class POICBORReadingCache
{
    internal const Int32 MaxEntries = 32;
    internal const Int32 MaxTextCharacters = 256;
    internal const Int32 MaxTotalTextCharacters = 4096;
    internal const Int32 MaxNodesPerEntry = 128;
    internal const Int32 MaxScalarPayloadPerNode = 256;

    private readonly Dictionary<String, CBORValue> values = new(StringComparer.Ordinal);
    internal Int32 Entries => values.Count;
    internal Int32 TextCharacters { get; private set; }

    internal CBORValue Read(String text, String path)
    {
        if (values.TryGetValue(text, out var value)) return value;
        if (!MetrologicalValue.TryParse(text, out var reading, out var error))
            throw new ArgumentException($"{path}: cannot encode a Styx metrological value: {error}");
        value = reading.ToCBOR();
        if (values.Count < MaxEntries && text.Length <= MaxTextCharacters &&
            text.Length <= MaxTotalTextCharacters - TextCharacters)
        {
            if (CanRetain(value))
            {
                values.Add(text, value);
                TextCharacters += text.Length;
            }
        }
        return value;
    }

    internal static Boolean CanRetain(CBORValue value)
    {
        var nodes = 0;
        return Admissible(value, ref nodes);
    }

    private static Boolean Admissible(CBORValue value, ref Int32 nodes)
    {
        if (++nodes > MaxNodesPerEntry) return false;
        switch (value.Kind)
        {
            case CBORValueKind.TextString: return value.AsText().Length <= MaxScalarPayloadPerNode;
            case CBORValueKind.ByteString: return value.AsBytes().Length <= MaxScalarPayloadPerNode;
            case CBORValueKind.Tagged: return Admissible(value.UntaggedValue, ref nodes);
            case CBORValueKind.Array:
                foreach (var item in value.AsArray()) if (!Admissible(item, ref nodes)) return false;
                break;
            case CBORValueKind.Map:
                foreach (var field in value.AsMap())
                    if (!Admissible(field.Key, ref nodes) || !Admissible(field.Value, ref nodes)) return false;
                break;
        }
        return true;
    }
}
