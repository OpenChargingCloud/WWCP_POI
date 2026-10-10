using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial record RoamingNetworkChangeSet
{
    private static readonly CBORTag EmbeddedJSON = new(262);

    /// <summary>
    /// Serialize the complete batch as deterministic CBOR without changing its signed content.
    /// ETags contain native digest bytes; losslessly representable SI readings use Styx tag 44252.
    /// </summary>
    public Byte[] ToCBOR()
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(this);
        using var document = JsonDocument.Parse(json);
        ValidateSigningJSON(document.RootElement);
        var paths = MeasurementPaths();
        var output = new System.Buffers.ArrayBufferWriter<Byte>();
        new POICBORWriter(output).WriteChangeSet(document.RootElement, paths);
        return output.WrittenSpan.ToArray();
    }

    /// <summary>
    /// Emit this complete signed batch into an archive after standalone and remaining-depth preflight.
    /// Exact scalar spelling and every peer envelope retain the standalone transport representation.
    /// </summary>
    internal void WriteArchiveCBOR(System.Buffers.IBufferWriter<Byte> output, Int32 remainingDepth)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(this);
        using var document = JsonDocument.Parse(json);
        ValidateSigningJSON(document.RootElement);
        var paths = MeasurementPaths();
        var scalars = new POIChangeSetScalarCache();
        new POICBORPreflight(remainingDepth).ValidateChangeSet(document.RootElement, paths, scalars);
        new POICBORWriter(output).WriteChangeSet(document.RootElement, paths, scalars);
    }

    /// <summary>
    /// Decode a complete ChangeSet, preserving optional payload presence, metadata and peer signatures.
    /// Decoding validates the transport and model; trusted signature verification remains explicit.
    /// </summary>
    public static RoamingNetworkChangeSet ParseCBOR(ReadOnlySpan<Byte> data)
    {
        var cbor = ConvertTransportETags(CBORValue.Parse(data), false);
        var readings = new HashSet<String>(StringComparer.Ordinal);
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
            WriteTransportJSON(writer, cbor, "", readings);
        using var document = JsonDocument.Parse(stream.ToArray());
        ValidateSigningJSON(document.RootElement);
        var result = JsonSerializer.Deserialize<RoamingNetworkChangeSet>(document.RootElement) ??
                     throw new ArgumentException("A ChangeSet CBOR document must be a map.", nameof(data));
        if (!readings.IsSubsetOf(result.MeasurementPaths()))
            throw new ArgumentException("Metrological CBOR tags are only allowed at schema-defined SI properties.", nameof(data));
        return result;
    }

    /// <summary>
    /// Try to decode a complete ChangeSet without returning a partially reconstructed batch.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out RoamingNetworkChangeSet? changeSet,
                                       [NotNullWhen(false)] out String? error)
    {
        try
        {
            changeSet = ParseCBOR(data);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            changeSet = null;
            error = exception.Message;
            return false;
        }
    }

    private HashSet<String> MeasurementPaths()
    {
        var paths = new HashSet<String>(StringComparer.Ordinal);
        for (var index = 0; index < Changes.Length; index++)
        {
            var change = Changes[index];
            var kind = change.EntityType;
            foreach (var segment in change.ElementPath)
                kind = POIElementSchema.Child(kind, segment.PropertyName).Kind;
            Add(change.OldValue, "OldValue");
            Add(change.NewValue, "NewValue");

            void Add(JsonElement? value, String field)
            {
                if (value is not { } element) return;
                var prefix = $"/Changes/{index}/{field}";
                var propertyOperation = change.Kind is RoamingNetworkChangeKind.UpdateProperty or
                                                         RoamingNetworkChangeKind.UpdateElementProperty or
                                                         RoamingNetworkChangeKind.RemoveProperty or RoamingNetworkChangeKind.RemoveElementProperty;
                if (!propertyOperation) Visit(element, kind, prefix);
                else if (element.ValueKind == JsonValueKind.String && POIRepresentation.IsMeasurement(kind, change.PropertyName!))
                    paths.Add(prefix);
                else if (POIRepresentation.ChildKind(kind, change.PropertyName!) is { } childKind)
                    Visit(element, childKind, prefix);
            }
        }
        return paths;

        void Visit(JsonElement element, String kind, String path)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                var index = 0;
                foreach (var item in element.EnumerateArray()) Visit(item, kind, path + "/" + index++);
            }
            else if (element.ValueKind == JsonValueKind.Object)
                foreach (var property in element.EnumerateObject())
                {
                    var childPath = path + "/" + Pointer(property.Name);
                    if (property.Value.ValueKind == JsonValueKind.String && POIRepresentation.IsMeasurement(kind, property.Name))
                        paths.Add(childPath);
                    else if (POIRepresentation.ChildKind(kind, property.Name) is { } childKind)
                        Visit(property.Value, childKind, childPath);
                }
        }
    }

    // Signed JSON binds the original text spelling. Native readings may only replace it losslessly.
    private static Boolean LosslessReading(String text)
        => MetrologicalValue.TryParse(text, out var reading, out _) &&
           String.Equals(text, reading.ToString(), StringComparison.Ordinal);

    private static CBORValue EncodeTransportJSON(JsonElement value, String path, HashSet<String> readings)
    {
        if (value.ValueKind == JsonValueKind.Object)
            return CBORValue.FromMap(value.EnumerateObject().Select(property => new KeyValuePair<CBORValue, CBORValue>(
                CBORValue.FromText(property.Name), EncodeTransportJSON(property.Value, path + "/" + Pointer(property.Name), readings))));
        if (value.ValueKind == JsonValueKind.Array)
            return CBORValue.FromArray(value.EnumerateArray().Select((item, index) => EncodeTransportJSON(item, path + "/" + index, readings)));
        if (value.ValueKind == JsonValueKind.String && readings.Contains(path) && LosslessReading(value.GetString()!))
            return MetrologicalValue.Parse(value.GetString()!).ToCBOR();
        if (value.ValueKind == JsonValueKind.Number)
        {
            var text = value.GetRawText();
            if (BigInteger.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number) &&
                text == number.ToString(CultureInfo.InvariantCulture))
                return CBORValue.FromBigInteger(number);
            if (value.TryGetDecimal(out var fraction) && text == fraction.ToString(CultureInfo.InvariantCulture))
                return CBORValue.FromDecimal(fraction);
            // The v2 signature profile retains JSON number spelling (1.0, 1e0 and -0 differ).
            // Tag 262 carries a JSON object; this profile defines its single Number member as a scalar wrapper.
            return CBORValue.Tagged(EmbeddedJSON, CBORValue.FromBytes(Encoding.UTF8.GetBytes("{\"Number\":" + text + "}")));
        }
        return CBORJSON.ToCBOR(value.GetRawText());
    }

    /// <summary>
    /// Encode one signed scalar with the existing lossless transport rules.
    /// Containers are forbidden here so direct output cannot construct a complete CBOR tree.
    /// </summary>
    internal static CBORValue EncodeTransportScalar(JsonElement value, String path, HashSet<String> readings)
    {
        if (value.ValueKind is JsonValueKind.Object or JsonValueKind.Array)
            throw new ArgumentException("The ChangeSet scalar codec does not accept containers.");
        return EncodeTransportJSON(value, path, readings);
    }

    private static CBORValue ConvertTransportETags(CBORValue value, Boolean toBinary)
    {
        if (value.Kind != CBORValueKind.Map)
            throw new ArgumentException("A ChangeSet CBOR document must be a map with text keys.");
        return CBORValue.FromMap(value.AsMap().Select(entry => {
            if (!entry.Key.TryGetText(out var field))
                throw new ArgumentException("ChangeSet property names must be text.");
            if (field is not (nameof(BeforeETags) or nameof(AfterETags))) return entry;
            if (entry.Value.Kind != CBORValueKind.Array)
                throw new ArgumentException($"{field}: expected an ETag array.");
            var tags = ETag.ValidatePair(entry.Value.AsArray().Select(item =>
                toBinary ? ETag.Parse(CBORJSON.ToJSON(item)) : ETag.Parse(item)).ToImmutableArray(), field);
            return new KeyValuePair<CBORValue, CBORValue>(entry.Key, CBORValue.FromArray(tags.Select(tag =>
                toBinary ? tag.ToCBOR() : tag.ToTextCBOR())));
        }));
    }

    // Use exact integers/decimal exponents throughout arbitrary application JSON, never Double coercion.
    private static void WriteTransportJSON(Utf8JsonWriter writer, CBORValue value, String path, HashSet<String> readings)
    {
        switch (value.Kind)
        {
            case CBORValueKind.Null: writer.WriteNullValue(); return;
            case CBORValueKind.Boolean: writer.WriteBooleanValue(value.AsBoolean()); return;
            case CBORValueKind.TextString: writer.WriteStringValue(value.AsText()); return;
            case CBORValueKind.UnsignedInteger:
            case CBORValueKind.NegativeInteger:
                writer.WriteRawValue(value.AsBigInteger().ToString(CultureInfo.InvariantCulture)); return;
            case CBORValueKind.Array:
                writer.WriteStartArray();
                var index = 0;
                foreach (var item in value.AsArray()) WriteTransportJSON(writer, item, path + "/" + index++, readings);
                writer.WriteEndArray(); return;
            case CBORValueKind.Map:
                writer.WriteStartObject();
                foreach (var entry in value.AsMap())
                {
                    if (!entry.Key.TryGetText(out var key)) throw new ArgumentException($"{path}: JSON maps require text keys.");
                    writer.WritePropertyName(key);
                    WriteTransportJSON(writer, entry.Value, path + "/" + Pointer(key), readings);
                }
                writer.WriteEndObject(); return;
            case CBORValueKind.Tagged:
                if (value.Tag == EmbeddedJSON)
                {
                    if (value.UntaggedValue.Kind != CBORValueKind.ByteString)
                        throw new ArgumentException($"{path}: embedded JSON must be a byte string.");
                    using var embedded = JsonDocument.Parse(value.UntaggedValue.AsBytes());
                    if (embedded.RootElement.ValueKind != JsonValueKind.Object ||
                        embedded.RootElement.EnumerateObject().Count() != 1 ||
                        !embedded.RootElement.TryGetProperty("Number", out var number) || number.ValueKind != JsonValueKind.Number)
                        throw new ArgumentException($"{path}: expected an embedded JSON Number wrapper.");
                    writer.WriteRawValue(number.GetRawText()); return;
                }
                if (value.Tag == CBORTag.UnsignedBignum || value.Tag == CBORTag.NegativeBignum)
                {
                    writer.WriteRawValue(value.AsBigInteger().ToString(CultureInfo.InvariantCulture)); return;
                }
                if (value.Tag == CBORTag.DecimalFraction)
                {
                    if (value.TryGetDecimal(out var fraction))
                    {
                        writer.WriteRawValue(fraction.ToString(CultureInfo.InvariantCulture)); return;
                    }
                    var parts = value.UntaggedValue;
                    if (parts.Kind != CBORValueKind.Array || parts.Count != 2)
                        throw new ArgumentException($"{path}: invalid decimal fraction.");
                    var exponent = parts.ItemAt(0).AsInt128();
                    if (exponent < -65536 || exponent > 65536)
                        throw new ArgumentException($"{path}: decimal exponent is outside the transport range.");
                    writer.WriteRawValue(parts.ItemAt(1).AsBigInteger().ToString(CultureInfo.InvariantCulture) +
                                         "e" + exponent.ToString(CultureInfo.InvariantCulture)); return;
                }
                if (value.Tag == CBORTag.MetrologicalValue)
                {
                    if (!MetrologicalValue.TryParse(value, out var reading, out var error))
                        throw new ArgumentException($"{path}: {error}");
                    readings.Add(path);
                    writer.WriteStringValue(reading.ToString()); return;
                }
                break;
        }
        throw new ArgumentException($"{path}: CBOR {value.Kind} is not part of the ChangeSet JSON transport profile.");
    }

    private static String Pointer(String value) => value.Replace("~", "~0").Replace("/", "~1");

    internal static CBORValue EncodeApplicationJSON(JsonElement value)
    {
        ValidateSigningJSON(value);
        return EncodeTransportJSON(value, "", []);
    }

    internal static JsonElement DecodeApplicationJSON(CBORValue value)
    {
        using var stream = new MemoryStream();
        var readings = new HashSet<String>(StringComparer.Ordinal);
        using (var writer = new Utf8JsonWriter(stream)) WriteTransportJSON(writer, value, "", readings);
        if (readings.Count != 0) throw new ArgumentException("Application metadata cannot contain schema-defined measurement tags.");
        using var document = JsonDocument.Parse(stream.ToArray());
        ValidateSigningJSON(document.RootElement);
        return document.RootElement.Clone();
    }
}
