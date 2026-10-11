using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The canonical representation whose bytes an ETag identifies.
/// </summary>
public enum ETagFormat
{
    /// <summary>
    /// Styx canonical JSON.
    /// </summary>
    JSON,

    /// <summary>
    /// Styx deterministic CBOR with the POI metrology profile.
    /// </summary>
    CBOR
}

/// <summary>
/// The hash algorithm of an ETag.
/// </summary>
public enum ETagHashAlgorithm
{
    /// <summary>
    /// SHA-256 with a 32-byte digest.
    /// </summary>
    SHA256
}

/// <summary>
/// The textual encoding used to transport digest bytes in JSON or display text.
/// This is independent of the ETag's content identity.
/// </summary>
public enum ETagDigestEncoding
{
    /// <summary>
    /// Lowercase hexadecimal.
    /// </summary>
    HEX,

    /// <summary>
    /// Standard RFC 4648 Base64 with canonical padding.
    /// </summary>
    Base64
}

/// <summary>
/// An immutable content identifier with typed format, algorithm and digest bytes.
/// JSON carries an explicit digest encoding; CBOR uses a native digest byte string.
/// </summary>
[System.Text.Json.Serialization.JsonConverter(typeof(ETagJSONConverter))]
[Newtonsoft.Json.JsonConverter(typeof(ETagNewtonsoftJSONConverter))]
public readonly struct ETag : IEquatable<ETag>
{
    private readonly ImmutableArray<Byte> digest;

    /// <summary>
    /// The canonical content representation.
    /// </summary>
    public ETagFormat Format { get; }

    /// <summary>
    /// The digest algorithm.
    /// </summary>
    public ETagHashAlgorithm Algorithm { get; }

    /// <summary>
    /// Immutable digest bytes, detached from constructor input.
    /// </summary>
    public ImmutableArray<Byte> Digest => digest.IsDefault ? ImmutableArray<Byte>.Empty : digest;

    /// <summary>
    /// Whether this value is initialized and has a supported format, algorithm and digest length.
    /// The default struct value is invalid and cannot be serialized or used as a precondition.
    /// </summary>
    public Boolean IsValid => !digest.IsDefault && digest.Length == 32 &&
                              Format is ETagFormat.JSON or ETagFormat.CBOR && Algorithm == ETagHashAlgorithm.SHA256;

    /// <summary>
    /// The lowercase hexadecimal digest for textual transports and display.
    /// </summary>
    public String DigestHex
    {
        get { RequireValid(); return Convert.ToHexStringLower(digest.AsSpan()); }
    }

    /// <summary>
    /// Construct an identifier from a supported format, hash algorithm and digest bytes.
    /// </summary>
    public ETag(ETagFormat format, ETagHashAlgorithm algorithm, ReadOnlySpan<Byte> digest)
    {
        if (format is not (ETagFormat.JSON or ETagFormat.CBOR))
            throw new ArgumentOutOfRangeException(nameof(format));
        if (algorithm != ETagHashAlgorithm.SHA256)
            throw new ArgumentOutOfRangeException(nameof(algorithm));
        if (digest.Length != 32)
            throw new ArgumentException("A SHA-256 digest must contain exactly 32 bytes.", nameof(digest));
        Format = format;
        Algorithm = algorithm;
        this.digest = ImmutableArray.CreateRange(digest.ToArray());
    }

    /// <summary>
    /// Compute a SHA-256 identifier over already canonicalized content bytes.
    /// </summary>
    public static ETag Compute(ETagFormat format, ReadOnlySpan<Byte> canonicalContent)
        => new(format, ETagHashAlgorithm.SHA256, SHA256.HashData(canonicalContent));

    /// <summary>
    /// Parse the human-readable format:algorithm:encoding:digest representation.
    /// Wire parsers require structured arrays instead.
    /// </summary>
    public static ETag Parse(String text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var parts = text.Split(':');
        if (parts.Length != 4) throw new ArgumentException("Expected format:algorithm:encoding:digest.", nameof(text));
        return FromTextParts(parts[0], parts[1], parts[2], parts[3]);
    }

    /// <summary>
    /// Try to parse a human-readable identifier.
    /// </summary>
    public static Boolean TryParse(String? text, out ETag value, [NotNullWhen(false)] out String? error)
    {
        try { value = Parse(text!); error = null; return true; }
        catch (ArgumentException exception) { value = default; error = exception.Message; return false; }
    }

    /// <summary>
    /// Parse a JSON array containing format, algorithm, explicit digest encoding and encoded digest.
    /// </summary>
    public static ETag Parse(JToken json)
    {
        if (json is not JArray array || array.Count != 4 || array.Any(item => item.Type != JTokenType.String))
            throw new ArgumentException("A JSON ETag must be [format, algorithm, encoding, encodedDigest].", nameof(json));
        return FromTextParts(array[0].Value<String>()!, array[1].Value<String>()!, array[2].Value<String>()!, array[3].Value<String>()!);
    }

    /// <summary>
    /// Parse a CBOR array containing format, algorithm and a digest byte string.
    /// </summary>
    public static ETag Parse(CBORValue cbor)
    {
        if (cbor.Kind != CBORValueKind.Array || cbor.Count != 3)
            throw new ArgumentException("An ETag must be a three-element CBOR array.", nameof(cbor));
        var items = cbor.AsArray();
        if (!items[0].TryGetText(out var format) || !items[1].TryGetText(out var algorithm) ||
            !items[2].TryGetBytes(out var bytes))
            throw new ArgumentException("A CBOR ETag requires text format/algorithm and a digest byte string.", nameof(cbor));
        return new(ParseFormat(format), ParseAlgorithm(algorithm), bytes);
    }

    /// <summary>
    /// Parse one complete CBOR-encoded identifier using the strict Styx reader.
    /// </summary>
    public static ETag ParseCBOR(ReadOnlySpan<Byte> bytes) => Parse(CBORValue.Parse(bytes));

    /// <summary>
    /// Serialize as [format, algorithm, encoding, encodedDigest], using HEX by default.
    /// </summary>
    public JArray ToJSON(ETagDigestEncoding encoding = ETagDigestEncoding.HEX)
        => new(FormatText, AlgorithmText, EncodingText(encoding), EncodeDigest(encoding));

    /// <summary>
    /// Write the structured JSON array directly to a UTF-8 writer.
    /// </summary>
    public void WriteTo(System.Text.Json.Utf8JsonWriter writer, ETagDigestEncoding encoding = ETagDigestEncoding.HEX)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var label = EncodingText(encoding);
        var encodedDigest = EncodeDigest(encoding);
        writer.WriteStartArray();
        writer.WriteStringValue(FormatText);
        writer.WriteStringValue(AlgorithmText);
        writer.WriteStringValue(label);
        writer.WriteStringValue(encodedDigest);
        writer.WriteEndArray();
    }

    /// <summary>
    /// Serialize as [format, algorithm, digestBytes] using a native CBOR byte string.
    /// </summary>
    public CBORValue ToCBOR()
    {
        RequireValid();
        return CBORValue.FromArray(CBORValue.FromText(FormatText), CBORValue.FromText(AlgorithmText),
                                   CBORValue.FromBytes(digest.ToArray()));
    }

    /// <summary>
    /// Return the readable format:algorithm:hex:digest form.
    /// </summary>
    public override String ToString() => ToString(ETagDigestEncoding.HEX);

    /// <summary>
    /// Return a readable identifier with an explicit digest encoding.
    /// </summary>
    public String ToString(ETagDigestEncoding encoding)
        => $"{FormatText}:{AlgorithmText}:{EncodingText(encoding)}:{EncodeDigest(encoding)}";

    /// <summary>
    /// Compare format, algorithm and digest content rather than backing-array identity.
    /// </summary>
    public Boolean Equals(ETag other)
        => Format == other.Format && Algorithm == other.Algorithm && digest.AsSpan().SequenceEqual(other.digest.AsSpan());

    /// <summary>
    /// Compare a boxed identifier by value.
    /// </summary>
    public override Boolean Equals(Object? other) => other is ETag tag && Equals(tag);

    /// <summary>
    /// Hash the format, algorithm and digest content.
    /// </summary>
    public override Int32 GetHashCode()
    {
        unchecked
        {
            var hash = ((Int32) Format * 397) ^ (Int32) Algorithm;
            foreach (var value in digest.AsSpan()) hash = hash * 31 + value;
            return hash;
        }
    }

    /// <summary>
    /// Whether two identifiers have equal content.
    /// </summary>
    public static Boolean operator ==(ETag left, ETag right) => left.Equals(right);

    /// <summary>
    /// Whether two identifiers differ in content.
    /// </summary>
    public static Boolean operator !=(ETag left, ETag right) => !left.Equals(right);

    internal String FormatText => Format switch {
        ETagFormat.JSON => "json", ETagFormat.CBOR => "cbor", _ => throw new InvalidOperationException("Unsupported ETag format.")
    };
    internal String AlgorithmText => Algorithm == ETagHashAlgorithm.SHA256 ? "sha256" :
                                     throw new InvalidOperationException("Unsupported ETag algorithm.");

    internal CBORValue ToTextCBOR()
        => CBORValue.FromArray(CBORValue.FromText(FormatText), CBORValue.FromText(AlgorithmText),
                               CBORValue.FromText("hex"), CBORValue.FromText(DigestHex));

    internal static ETag FromTextParts(String format, String algorithm, String encoding, String encodedDigest)
    {
        var parsedFormat = ParseFormat(format);
        var parsedAlgorithm = ParseAlgorithm(algorithm);
        Byte[] bytes;
        switch (encoding)
        {
            case "hex":
                if (encodedDigest.Length != 64 || encodedDigest.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0)
                    throw new ArgumentException("A SHA-256 digest requires 64 lowercase hexadecimal digits.", nameof(encodedDigest));
                bytes = Convert.FromHexString(encodedDigest);
                break;
            case "base64":
                try { bytes = Convert.FromBase64String(encodedDigest); }
                catch (FormatException exception)
                {
                    throw new ArgumentException("Invalid Base64 digest.", nameof(encodedDigest), exception);
                }
                if (!String.Equals(encodedDigest, Convert.ToBase64String(bytes), StringComparison.Ordinal))
                    throw new ArgumentException("A Base64 digest requires canonical standard alphabet and padding without whitespace.", nameof(encodedDigest));
                break;
            default:
                throw new ArgumentException("Unsupported ETag digest encoding.", nameof(encoding));
        }
        return new(parsedFormat, parsedAlgorithm, bytes);
    }

    private String EncodeDigest(ETagDigestEncoding encoding)
    {
        RequireValid();
        return encoding switch {
            ETagDigestEncoding.HEX => Convert.ToHexStringLower(digest.AsSpan()),
            ETagDigestEncoding.Base64 => Convert.ToBase64String(digest.AsSpan()),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
    }

    private static String EncodingText(ETagDigestEncoding encoding) => encoding switch {
        ETagDigestEncoding.HEX => "hex", ETagDigestEncoding.Base64 => "base64",
        _ => throw new ArgumentOutOfRangeException(nameof(encoding))
    };

    private static ETagFormat ParseFormat(String format) => format switch {
        "json" => ETagFormat.JSON, "cbor" => ETagFormat.CBOR,
        _ => throw new ArgumentException("Unsupported ETag format.", nameof(format))
    };
    private static ETagHashAlgorithm ParseAlgorithm(String algorithm)
        => algorithm == "sha256" ? ETagHashAlgorithm.SHA256 : throw new ArgumentException("Unsupported ETag hash algorithm.", nameof(algorithm));

    private void RequireValid()
    {
        if (!IsValid) throw new InvalidOperationException("An uninitialized ETag cannot be serialized.");
    }

    internal static ImmutableArray<ETag> ValidatePair(ImmutableArray<ETag> tags, String parameterName)
    {
        if (tags.IsDefault || tags.Length != 2 || !tags[0].IsValid || !tags[1].IsValid ||
            tags[0].Format != ETagFormat.JSON || tags[1].Format != ETagFormat.CBOR)
            throw new ArgumentException("Expected initialized JSON and CBOR SHA-256 ETags, in that order.", parameterName);
        return tags;
    }
}

/// <summary>
/// System.Text.Json converter for the structured ETag array.
/// </summary>
public sealed class ETagJSONConverter : System.Text.Json.Serialization.JsonConverter<ETag>
{
    private readonly ETagDigestEncoding encoding;

    /// <summary>
    /// Construct the default HEX JSON converter.
    /// </summary>
    public ETagJSONConverter() : this(ETagDigestEncoding.HEX)
    { }

    /// <summary>
    /// Select the output digest encoding; input always follows its explicit encoding label.
    /// </summary>
    public ETagJSONConverter(ETagDigestEncoding encoding)
    {
        if (!Enum.IsDefined(encoding)) throw new ArgumentOutOfRangeException(nameof(encoding));
        this.encoding = encoding;
    }

    /// <summary>
    /// Read exactly four string elements and validate the identifier and digest encoding.
    /// </summary>
    public override ETag Read(ref System.Text.Json.Utf8JsonReader reader, Type typeToConvert, System.Text.Json.JsonSerializerOptions options)
    {
        if (reader.TokenType != System.Text.Json.JsonTokenType.StartArray)
            throw new System.Text.Json.JsonException("An ETag must be a structured array.");
        var format = ReadText(ref reader);
        var algorithm = ReadText(ref reader);
        var inputEncoding = ReadText(ref reader);
        var encodedDigest = ReadText(ref reader);
        if (!reader.Read() || reader.TokenType != System.Text.Json.JsonTokenType.EndArray)
            throw new System.Text.Json.JsonException("A JSON ETag array must have exactly four elements.");
        try { return ETag.FromTextParts(format, algorithm, inputEncoding, encodedDigest); }
        catch (ArgumentException exception) { throw new System.Text.Json.JsonException(exception.Message, exception); }
    }

    /// <summary>
    /// Write the four-element JSON array with the selected explicit encoding.
    /// </summary>
    public override void Write(System.Text.Json.Utf8JsonWriter writer, ETag value, System.Text.Json.JsonSerializerOptions options)
        => value.WriteTo(writer, encoding);

    private static String ReadText(ref System.Text.Json.Utf8JsonReader reader)
        => reader.Read() && reader.TokenType == System.Text.Json.JsonTokenType.String ? reader.GetString()! :
           throw new System.Text.Json.JsonException("Each JSON ETag element must be a string.");
}

/// <summary>
/// Newtonsoft.Json converter for the same structured ETag array.
/// </summary>
public sealed class ETagNewtonsoftJSONConverter : Newtonsoft.Json.JsonConverter<ETag>
{
    /// <summary>
    /// Read and validate the four-element array and its explicit digest encoding.
    /// </summary>
    public override ETag ReadJson(Newtonsoft.Json.JsonReader reader, Type objectType, ETag existingValue,
                                  Boolean hasExistingValue, Newtonsoft.Json.JsonSerializer serializer)
    {
        if (reader.TokenType != Newtonsoft.Json.JsonToken.StartArray)
            throw new Newtonsoft.Json.JsonSerializationException("An ETag must be a structured array.");
        try { return ETag.Parse(JArray.Load(reader)); }
        catch (ArgumentException exception) { throw new Newtonsoft.Json.JsonSerializationException(exception.Message, exception); }
    }

    /// <summary>
    /// Write the four-element array with an explicit HEX encoding and lowercase digest.
    /// </summary>
    public override void WriteJson(Newtonsoft.Json.JsonWriter writer, ETag value, Newtonsoft.Json.JsonSerializer serializer)
        => value.ToJSON().WriteTo(writer);
}
