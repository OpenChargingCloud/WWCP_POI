/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Security.Cryptography;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI;


/// <summary>
/// An immutable content identifier with typed format, algorithm and digest bytes.
/// JSON carries an explicit digest encoding; CBOR uses a native digest byte string.
/// </summary>
public readonly struct ETag : IEquatable<ETag>
{

    private readonly ImmutableArray<Byte> HashDigest;

    /// <summary>
    /// The canonical content representation.
    /// </summary>
    public ETagFormat            Format           { get; }

    /// <summary>
    /// The digest algorithm.
    /// </summary>
    public ETagHashAlgorithm     HashAlgorithm    { get; }

    /// <summary>
    /// Immutable digest bytes, detached from constructor input.
    /// </summary>
    public ImmutableArray<Byte>  Digest
        => HashDigest.IsDefault
               ? []
               : HashDigest;

    /// <summary>
    /// Whether this value is initialized and has a supported format, algorithm and digest length.
    /// The default struct value is invalid and cannot be serialized or used as a precondition.
    /// </summary>
    public Boolean IsValid => !HashDigest.IsDefault &&
                              Format is ETagFormat.JSON or ETagFormat.CBOR or ETagFormat.EffectiveTree &&
                              HashDigest.Length == DigestLength(HashAlgorithm);

    /// <summary>
    /// The number of digest bytes of the given hash algorithm, or 0 for an unsupported one.
    /// </summary>
    /// <param name="HashAlgorithm">A hash algorithm.</param>
    public static Int32 DigestLength(ETagHashAlgorithm HashAlgorithm)

        => HashAlgorithm switch {
               ETagHashAlgorithm.SHA256  => 32,
               ETagHashAlgorithm.SHA384  => 48,
               ETagHashAlgorithm.SHA512  => 64,
               _                         => 0
           };

    /// <summary>
    /// The lowercase hexadecimal digest for textual transports and display.
    /// </summary>
    public String DigestHex
    {
        get { RequireValid(); return Convert.ToHexStringLower(HashDigest.AsSpan()); }
    }



    #region Constructor(s)

    /// <summary>
    /// Create a new ETag with a supported format, algorithm and digest bytes.
    /// </summary>
    /// <param name="Format">The format of the canonical content representation.</param>
    /// <param name="HashAlgorithm">The digest algorithm used to compute the identifier.</param>
    /// <param name="HashDigest">The digest bytes, which must be exactly 32 bytes for SHA-256.</param>
    public ETag(ETagFormat          Format,
                ETagHashAlgorithm   HashAlgorithm,
                ReadOnlySpan<Byte>  HashDigest)
    {

        if (Format is not (ETagFormat.JSON or ETagFormat.CBOR or ETagFormat.EffectiveTree))
            throw new ArgumentOutOfRangeException(nameof(Format),         "The only supported formats are JSON, CBOR and the effective tree!");

        if (HashAlgorithm is not (ETagHashAlgorithm.SHA256 or ETagHashAlgorithm.SHA384 or ETagHashAlgorithm.SHA512))
            throw new ArgumentOutOfRangeException(nameof(HashAlgorithm),  "The only supported hash algorithms are SHA-256, SHA-384 and SHA-512!");

        if (HashAlgorithm is ETagHashAlgorithm.SHA256 && HashDigest.Length != 32)
            throw new ArgumentException("A SHA-256 digest must contain exactly 32 bytes.", nameof(HashDigest));

        if (HashAlgorithm is ETagHashAlgorithm.SHA384 && HashDigest.Length != 48)
            throw new ArgumentException("A SHA-384 digest must contain exactly 48 bytes.", nameof(HashDigest));

        if (HashAlgorithm is ETagHashAlgorithm.SHA512 && HashDigest.Length != 64)
            throw new ArgumentException("A SHA-512 digest must contain exactly 64 bytes.", nameof(HashDigest));

        this.Format         = Format;
        this.HashAlgorithm  = HashAlgorithm;
        this.HashDigest     = [.. HashDigest.ToArray()];

    }

    #endregion





    /// <summary>
    /// Compute a SHA-256 identifier over already canonicalized content bytes.
    /// </summary>
    public static ETag Compute(ETagFormat          Format,
                               ReadOnlySpan<Byte>  CanonicalContent)

        => new (Format,
                ETagHashAlgorithm.SHA256,
                SHA256.HashData(CanonicalContent));

    /// <summary>
    /// Compute an identifier with the given hash algorithm over already canonicalized content bytes.
    /// </summary>
    /// <param name="Format">The format of the canonical content.</param>
    /// <param name="HashAlgorithm">The hash algorithm.</param>
    /// <param name="CanonicalContent">The canonical content bytes.</param>
    public static ETag Compute(ETagFormat          Format,
                               ETagHashAlgorithm   HashAlgorithm,
                               ReadOnlySpan<Byte>  CanonicalContent)

        => new (Format,
                HashAlgorithm,
                HashAlgorithm switch {
                    ETagHashAlgorithm.SHA256  => SHA256.HashData(CanonicalContent),
                    ETagHashAlgorithm.SHA384  => SHA384.HashData(CanonicalContent),
                    ETagHashAlgorithm.SHA512  => SHA512.HashData(CanonicalContent),
                    _                         => throw new ArgumentOutOfRangeException(nameof(HashAlgorithm), "Unsupported ETag hash algorithm!")
                });


    /// <summary>
    /// Parse the human-readable format:algorithm:encoding:digest representation.
    /// Wire parsers require structured arrays instead.
    /// </summary>
    public static ETag Parse(String text)
    {

        var parts = text.Split(':');

        if (parts.Length != 4)
            throw new ArgumentException("Expected format:algorithm:encoding:digest.", nameof(text));

        return FromTextParts(parts[0], parts[1], parts[2], parts[3]);

    }

    /// <summary>
    /// Try to parse a human-readable identifier, exactly as Parse(String) reads it: a hex digest of the length of its
    /// algorithm in lowercase digits, a Base64 digest canonical, in the standard alphabet with its padding and without
    /// whitespace (WWCPPOIConformanceTests O2).
    /// </summary>
    public static Boolean TryParse(String                            Text,
                                   out ETag                          Value,
                                   [NotNullWhen(false)] out String?  ErrorResponse)
    {

        Value = default;

        try
        {

            var parts = Text.Split(':');

            if (parts.Length != 4)
            {
                ErrorResponse = "Expected format:algorithm:encoding:digest!";
                return false;
            }

            Value          = FromTextParts(parts[0], parts[1], parts[2], parts[3]);
            ErrorResponse  = null;
            return true;

        }
        catch (Exception exception) {
            Value          = default;
            ErrorResponse  = exception.Message;
            return false;
        }

    }

    /// <summary>
    /// Parse a JSON array containing format, algorithm, explicit digest encoding and encoded digest.
    /// </summary>
    public static ETag Parse(JToken json)
    {

        if (json is not JArray array || array.Count != 4 || array.Any(item => item.Type != JTokenType.String))
            throw new ArgumentException("A JSON ETag must be [format, algorithm, encoding, encodedDigest].", nameof(json));

        return FromTextParts(
                   array[0].Value<String>()!,
                   array[1].Value<String>()!,
                   array[2].Value<String>()!,
                   array[3].Value<String>()!
               );

    }

    /// <summary>
    /// Parse a JSON array containing format, algorithm, the digest encoding "hex" and the hex digest: an ETag a change
    /// set, a precondition or a commit names, which their signatures and identifications are calculated over as they
    /// write it, in hex.
    /// </summary>
    public static ETag ParseHex(JToken json)
    {

        if (json is JArray array && array.Count == 4 && array[2].Type == JTokenType.String && array[2].Value<String>() != "hex")
            throw new ArgumentException($"An ETag of a change set or a commit must be written in hex, not in '{array[2].Value<String>()}': they are signed as they are written.", nameof(json));

        return Parse(json);

    }

    /// <summary>
    /// Parse a CBOR array containing format, algorithm and a digest byte string.
    /// </summary>
    public static ETag Parse(CBORValue cbor)
    {

        if (cbor.Kind != CBORValueKind.Array || cbor.Count != 3)
            throw new ArgumentException("An ETag must be a three-element CBOR array.", nameof(cbor));

        var items = cbor.AsArray();

        if (!items[0].TryGetText (out var format)    ||
            !items[1].TryGetText (out var algorithm) ||
            !items[2].TryGetBytes(out var bytes))
        {
            throw new ArgumentException("A CBOR ETag requires text format/algorithm and a digest byte string.", nameof(cbor));
        }

        return new (ParseFormat(format), ParseAlgorithm(algorithm), bytes);

    }

    /// <summary>
    /// Parse one complete CBOR-encoded identifier using the strict Styx reader.
    /// </summary>
    public static ETag ParseCBOR(ReadOnlySpan<Byte> bytes)
        => Parse(CBORValue.Parse(bytes));

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
                                   CBORValue.FromBytes(HashDigest.ToArray()));
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
        => Format == other.Format && HashAlgorithm == other.HashAlgorithm && HashDigest.AsSpan().SequenceEqual(other.HashDigest.AsSpan());

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
            var hash = ((Int32) Format * 397) ^ (Int32) HashAlgorithm;
            foreach (var value in HashDigest.AsSpan()) hash = hash * 31 + value;
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
        ETagFormat.JSON => "json", ETagFormat.CBOR => "cbor", ETagFormat.EffectiveTree => "effectiveTree", _ => throw new InvalidOperationException("Unsupported ETag format.")
    };
    internal String AlgorithmText => HashAlgorithm switch {
        ETagHashAlgorithm.SHA256 => "sha256", ETagHashAlgorithm.SHA384 => "sha384", ETagHashAlgorithm.SHA512 => "sha512",
        _ => throw new InvalidOperationException("Unsupported ETag algorithm.")
    };

    internal CBORValue ToTextCBOR()
        => CBORValue.FromArray(CBORValue.FromText(FormatText), CBORValue.FromText(AlgorithmText),
                               CBORValue.FromText("hex"), CBORValue.FromText(DigestHex));

    internal static ETag FromTextParts(String  format,
                                       String  algorithm,
                                       String  encoding,
                                       String  encodedDigest)
    {

        var parsedFormat    = ParseFormat(format);
        var parsedAlgorithm = ParseAlgorithm(algorithm);
        Byte[] bytes;

        switch (encoding)
        {

            case "hex":
                if (encodedDigest.Length != 2 * DigestLength(parsedAlgorithm) || encodedDigest.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0)
                    throw new ArgumentException($"A {algorithm} digest requires {2 * DigestLength(parsedAlgorithm)} lowercase hexadecimal digits.", nameof(encodedDigest));
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

        return new (parsedFormat, parsedAlgorithm, bytes);

    }

    private String EncodeDigest(ETagDigestEncoding encoding)
    {
        RequireValid();
        return encoding switch {
            ETagDigestEncoding.HEX => Convert.ToHexStringLower(HashDigest.AsSpan()),
            ETagDigestEncoding.Base64 => Convert.ToBase64String(HashDigest.AsSpan()),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding))
        };
    }

    private static String EncodingText(ETagDigestEncoding encoding) => encoding switch {
        ETagDigestEncoding.HEX => "hex", ETagDigestEncoding.Base64 => "base64",
        _ => throw new ArgumentOutOfRangeException(nameof(encoding))
    };

    private static ETagFormat ParseFormat(String format) => format switch {
        "json" => ETagFormat.JSON, "cbor" => ETagFormat.CBOR, "effectiveTree" => ETagFormat.EffectiveTree,
        _ => throw new ArgumentException("Unsupported ETag format.", nameof(format))
    };
    private static ETagHashAlgorithm ParseAlgorithm(String algorithm)
        => algorithm switch {
            "sha256" => ETagHashAlgorithm.SHA256, "sha384" => ETagHashAlgorithm.SHA384, "sha512" => ETagHashAlgorithm.SHA512,
            _ => throw new ArgumentException("Unsupported ETag hash algorithm.", nameof(algorithm))
        };

    private void RequireValid()
    {
        if (!IsValid)
            throw new InvalidOperationException("An uninitialized ETag cannot be serialized.");
    }

    internal static ImmutableArray<ETag> ValidatePair(ImmutableArray<ETag> tags, String parameterName)
    {
        if (tags.IsDefault || tags.Length != 2 || !tags[0].IsValid || !tags[1].IsValid ||
            tags[0].Format != ETagFormat.JSON || tags[1].Format != ETagFormat.CBOR)
            throw new ArgumentException("Expected initialized JSON and CBOR SHA-256 ETags, in that order.", parameterName);
        return tags;
    }

}
