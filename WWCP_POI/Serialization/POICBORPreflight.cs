/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Validate prepared POI JSON before emitting a borrowed archive payload.
/// Preserve standalone writer limits and the archive reader's remaining-depth rules.
/// </summary>
internal sealed class POICBORPreflight(Int32 remainingDepth, POICBORReadingCache? readings = null)
{
    private CBORException? readerFailure;
    private HashSet<String>? changeSetReadings;
    private POIChangeSetScalarCache? changeSetScalars;
    // Active parent maps use distinct depths. Clear each completed map before sibling reuse.
    // Large maps are checked normally, but their oversized set is not retained in the pool.
    private readonly HashSet<String>?[] namesAtDepth = new HashSet<String>?[64];
    internal const Int32 MaxPooledMapNames = 128;
    internal Int32 PooledMapSets => namesAtDepth.Count(names => names is not null);

    internal void Validate(JsonElement value, String kind)
    {
        changeSetReadings = null;
        changeSetScalars = null;
        ValidateCore(value, kind);
    }

    internal void ValidateChangeSet(JsonElement value, HashSet<String> paths, POIChangeSetScalarCache? scalars = null)
    {
        changeSetReadings = paths;
        changeSetScalars = scalars;
        ValidateCore(value, null);
    }

    private void ValidateCore(JsonElement value, String? kind)
    {
        readerFailure = null;
        Visit(value, kind, 0, "");
        // Standalone codec failures precede the old Encoded/SkipValue validation.
        if (readerFailure is not null) throw readerFailure;
    }

    private void Visit(JsonElement value, String? kind, Int32 depth, String path)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.Object:
                Enter(depth, value.EnumerateObject().Any());
                var names = namesAtDepth[depth] ??= new HashSet<String>(StringComparer.Ordinal);
                try
                {
                    foreach (var field in value.EnumerateObject())
                    {
                        var name = field.Name;
                        if (!names.Add(name)) throw new CBORException("Duplicate CBOR POI map key.");
                        if ((kind is not null && name == "ETags") ||
                            (changeSetReadings is not null && path.Length == 0 && name is "BeforeETags" or "AfterETags"))
                        {
                            if (field.Value.ValueKind != JsonValueKind.Array || field.Value.GetArrayLength() != 2)
                                throw new ArgumentException("ETags: expected the JSON and CBOR identifier arrays.");
                            var tags = ETag.ValidatePair(ImmutableArray.Create(POICBORWriter.ReadETag(field.Value[0]),
                                                                              POICBORWriter.ReadETag(field.Value[1])), "ETags");
                            // A validated pair emits two arrays of text/text/32 digest bytes.
                            // Its exact structural bound is two, with no tags or nested scalars.
                            if (!Fits(2, depth + 1))
                                Leaf(CBORValue.FromArray(tags.Select(tag => tag.ToCBOR())), depth + 1);
                        }
                        else if (kind is not null && field.Value.ValueKind == JsonValueKind.String &&
                                 POIRepresentation.IsMeasurement(kind, name))
                        {
                            var text = field.Value.GetString()!;
                            var fieldPath = path + "/" + Escape(name);
                            if (readings is not null) Leaf(readings.Read(text, fieldPath), depth + 1);
                            else
                            {
                                if (!MetrologicalValue.TryParse(text, out var reading, out var error))
                                    throw new ArgumentException($"{fieldPath}: cannot encode a Styx metrological value: {error}");
                                Leaf(reading.ToCBOR(), depth + 1);
                            }
                        }
                        else
                        {
                            var child = kind is null ? null : POIRepresentation.ChildKind(kind, name);
                            Visit(field.Value, child, depth + 1, changeSetReadings is not null || child is not null ? path + "/" + Escape(name) : "");
                        }
                    }
                }
                finally
                {
                    if (names.Count > MaxPooledMapNames) namesAtDepth[depth] = null;
                    names.Clear();
                }
                break;
            case JsonValueKind.Array:
                Enter(depth, value.GetArrayLength() > 0);
                var index = 0;
                foreach (var item in value.EnumerateArray())
                {
                    var child = item.ValueKind == JsonValueKind.Object ? kind : null;
                    Visit(item, child, depth + 1, changeSetReadings is not null || child is not null ? path + "/" + index.ToString(CultureInfo.InvariantCulture) : "");
                    index++;
                }
                break;
            case JsonValueKind.Number:
                if (changeSetReadings is not null)
                    Leaf(changeSetScalars?.Read(value, path, changeSetReadings) ?? RoamingNetworkChangeSet.EncodeTransportScalar(value, path, changeSetReadings), depth);
                else if (!value.TryGetInt64(out _) && !value.TryGetUInt64(out _))
                    Leaf(CBORJSON.ToCBOR(value.GetRawText()), depth);
                break;
            case JsonValueKind.String:
                if (changeSetReadings is not null && changeSetReadings.Contains(path))
                    Leaf(changeSetScalars?.Read(value, path, changeSetReadings) ?? RoamingNetworkChangeSet.EncodeTransportScalar(value, path, changeSetReadings), depth);
                else _ = value.GetString();
                break;
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                break;
            default:
                throw new CBORException($"Unsupported POI JSON kind '{value.ValueKind}'.");
        }
    }

    private void Enter(Int32 depth, Boolean hasItems)
    {
        if (depth >= 64) throw new CBORException("The maximum CBOR nesting depth of 64 was exceeded!");
        // SkipValue does not push definite empty containers. Tags live only inside scalar
        // leaves in POI JSON, where the actual Styx reader supplies tag-run accounting.
        if (hasItems && depth >= remainingDepth)
            readerFailure ??= new CBORException($"The maximum CBOR nesting depth of {remainingDepth} was exceeded!");
    }

    private void Leaf(CBORValue value, Int32 depth)
    {
        // Counting all ancestor tags and even empty containers bounds both Styx algorithms.
        // Generated scalar trees have valid keys/types; only depth can reject their encoding.
        // Away from the boundary no temporary leaf encoding is needed. Near it, use Styx.
        var bound = DepthBound(value);
        if (Fits(bound, depth)) return;
        // Only this scalar's number/unit/digest structure is buffered. A complete POI value
        // is never encoded here. Writer and SkipValue intentionally have different budgets.
        var bytes = value.ToByteArray(new CBORWriterOptions { Deterministic = true, PreferredFloatEncoding = true, MaxDepth = 64 - depth });
        if (readerFailure is not null) return;
        try
        {
            var reader = new CBORReader(bytes, new CBORReaderOptions { MaxDepth = remainingDepth - depth });
            reader.SkipValue();
        }
        catch (CBORException error) { readerFailure = error; }
    }

    private Boolean Fits(Int32 bound, Int32 depth)
        => bound <= 64 - depth && (readerFailure is not null || bound <= remainingDepth - depth);

    private static Int32 DepthBound(CBORValue value)
    {
        if (value.Kind == CBORValueKind.Tagged) return 1 + DepthBound(value.UntaggedValue);
        var children = 0;
        if (value.Kind == CBORValueKind.Array)
        {
            foreach (var child in value.AsArray()) children = Math.Max(children, DepthBound(child));
            return 1 + children;
        }
        if (value.Kind == CBORValueKind.Map)
        {
            foreach (var field in value.AsMap())
                children = Math.Max(children, Math.Max(DepthBound(field.Key), DepthBound(field.Value)));
            return 1 + children;
        }
        return 0;
    }

    private static String Escape(String value) => value.Replace("~", "~0").Replace("/", "~1");
}
