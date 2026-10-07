/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// A SHA-256 identity of the canonical commit profile, distinct from a static POI state identifier.
/// </summary>
[JsonConverter(typeof(RoamingNetworkCommitIdJSONConverter))]
public readonly struct RoamingNetworkCommitId : IEquatable<RoamingNetworkCommitId>
{
    private readonly ETag hash;

    /// <summary>
    /// Whether this value contains an initialized commit digest.
    /// </summary>
    public Boolean IsValid => hash.IsValid && hash.Format == ETagFormat.JSON;

    /// <summary>
    /// The canonical JSON SHA-256 digest used by the commit identity profile.
    /// </summary>
    public ETag Hash => IsValid ? hash : throw new InvalidOperationException("Uninitialized commit identity.");

    /// <summary>
    /// Construct a typed commit identity from a canonical JSON SHA-256 digest.
    /// </summary>
    public RoamingNetworkCommitId(ETag hash)
    {
        if (!hash.IsValid || hash.Format != ETagFormat.JSON)
            throw new ArgumentException("A commit identity requires a JSON SHA-256 digest.", nameof(hash));
        this.hash = hash;
    }

    /// <summary>
    /// Parse the human-readable format:algorithm:encoding:digest representation.
    /// </summary>
    public static RoamingNetworkCommitId Parse(String text) => new(ETag.Parse(text));

    /// <summary>
    /// Return a native CBOR identifier containing digest bytes.
    /// </summary>
    public CBORValue ToCBOR() => Hash.ToCBOR();

    /// <summary>
    /// Return the readable JSON SHA-256 HEX identifier.
    /// </summary>
    public override String ToString() => Hash.ToString();

    /// <summary>
    /// Compare commit identities by digest content.
    /// </summary>
    public Boolean Equals(RoamingNetworkCommitId other) => hash.Equals(other.hash);

    /// <summary>
    /// Compare a boxed commit identity by value.
    /// </summary>
    public override Boolean Equals(Object? other) => other is RoamingNetworkCommitId id && Equals(id);

    /// <summary>
    /// Hash the typed digest.
    /// </summary>
    public override Int32 GetHashCode() => hash.GetHashCode();

    /// <summary>
    /// Whether two commit identities are equal.
    /// </summary>
    public static Boolean operator ==(RoamingNetworkCommitId left, RoamingNetworkCommitId right) => left.Equals(right);

    /// <summary>
    /// Whether two commit identities differ.
    /// </summary>
    public static Boolean operator !=(RoamingNetworkCommitId left, RoamingNetworkCommitId right) => !left.Equals(right);
}

/// <summary>
/// Serialize typed commit identities using the structured JSON ETag tuple.
/// </summary>
public sealed class RoamingNetworkCommitIdJSONConverter : JsonConverter<RoamingNetworkCommitId>
{
    /// <summary>
    /// Read a structured identity and reject unsupported digest formats.
    /// </summary>
    public override RoamingNetworkCommitId Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
        => new(JsonSerializer.Deserialize<ETag>(ref reader));

    /// <summary>
    /// Write the canonical HEX tuple independent of caller serializer options.
    /// </summary>
    public override void Write(Utf8JsonWriter writer, RoamingNetworkCommitId value, JsonSerializerOptions options)
        => value.Hash.WriteTo(writer);
}
