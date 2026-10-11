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
/// Complete immutable static state and administrator metadata bound into a snapshot commit.
/// Runtime data and peer signature envelopes are absent from this payload.
/// </summary>
public sealed class RoamingNetworkSnapshotContent
{
    /// <summary>
    /// The complete resulting static state, including revision and last applied batch ID.
    /// </summary>
    public RoamingNetworkDataSnapshot State { get; }

    /// <summary>
    /// The explicitly supplied creation timestamp, normalized to UTC.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// Immutable descriptions indexed by language label.
    /// </summary>
    public ImmutableDictionary<String, String> Description { get; }

    /// <summary>
    /// Detached application metadata; JSON number spelling is retained in CBOR transport.
    /// </summary>
    public ImmutableDictionary<String, JsonElement> Metadata { get; }

    internal RoamingNetworkSnapshotContent(RoamingNetworkDataSnapshot state, DateTimeOffset createdAt,
        ImmutableDictionary<String, String>? description = null,
        ImmutableDictionary<String, JsonElement>? metadata = null)
    {
        ArgumentNullException.ThrowIfNull(state);
        State = state;
        CreatedAt = createdAt.ToUniversalTime();
        var descriptions = ImmutableDictionary.CreateBuilder<String, String>(StringComparer.Ordinal);
        foreach (var entry in description ?? ImmutableDictionary<String, String>.Empty)
        {
            if (String.IsNullOrWhiteSpace(entry.Key) || entry.Value is null)
                throw new ArgumentException("Description requires nonempty language labels and nonnull text.");
            descriptions.Add(entry.Key, entry.Value);
        }
        Description = descriptions.ToImmutable();
        var values = ImmutableDictionary.CreateBuilder<String, JsonElement>(StringComparer.Ordinal);
        foreach (var entry in metadata ?? ImmutableDictionary<String, JsonElement>.Empty)
        {
            if (String.IsNullOrWhiteSpace(entry.Key)) throw new ArgumentException("Metadata keys must not be empty.");
            RoamingNetworkChangeSet.ValidateSigningJSON(entry.Value);
            values.Add(entry.Key, entry.Value.Clone());
        }
        Metadata = values.ToImmutable();
    }

    internal void WriteTo(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("CreatedAt", CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.WritePropertyName("Description"); JsonSerializer.Serialize(writer, Description);
        writer.WritePropertyName("Metadata"); JsonSerializer.Serialize(writer, Metadata);
        writer.WritePropertyName("State"); State.WriteTo(writer);
        writer.WriteEndObject();
    }

    internal static RoamingNetworkSnapshotContent Parse(JsonElement value)
    {
        RoamingNetworkCommit.RequireFields(value, "CreatedAt", "Description", "Metadata", "State");
        return new(RoamingNetworkDataSnapshot.Parse(value.GetProperty("State").GetRawText()),
                   DateTimeOffset.ParseExact(value.GetProperty("CreatedAt").GetString()!, "O", CultureInfo.InvariantCulture),
                   JsonSerializer.Deserialize<ImmutableDictionary<String, String>>(value.GetProperty("Description"))
                       ?? throw new ArgumentException("Description must be an object."),
                   JsonSerializer.Deserialize<ImmutableDictionary<String, JsonElement>>(value.GetProperty("Metadata"))
                       ?? throw new ArgumentException("Metadata must be an object."));
    }

    internal CBORValue ToCBOR()
        => RoamingNetworkCommit.Map(
            ("CreatedAt", CBORValue.FromText(CreatedAt.ToString("O", CultureInfo.InvariantCulture))),
            ("Description", RoamingNetworkChangeSet.EncodeApplicationJSON(JsonSerializer.SerializeToElement(Description))),
            ("Metadata", RoamingNetworkChangeSet.EncodeApplicationJSON(JsonSerializer.SerializeToElement(Metadata))),
            ("State", CBORValue.Parse(State.ToCBOR(IncludeVersionMetadata: true))));

    internal static RoamingNetworkSnapshotContent ParseCBOR(CBORValue value)
    {
        var fields = RoamingNetworkCommit.Fields(value, "CreatedAt", "Description", "Metadata", "State");
        return new(RoamingNetworkDataSnapshot.ParseCBOR(fields["State"].ToByteArray(CBORWriterOptions.Canonical)),
                   DateTimeOffset.ParseExact(RoamingNetworkCommit.Text(fields["CreatedAt"]), "O", CultureInfo.InvariantCulture),
                   JsonSerializer.Deserialize<ImmutableDictionary<String, String>>(RoamingNetworkChangeSet.DecodeApplicationJSON(fields["Description"]))
                       ?? throw new ArgumentException("Description must be a map."),
                   JsonSerializer.Deserialize<ImmutableDictionary<String, JsonElement>>(RoamingNetworkChangeSet.DecodeApplicationJSON(fields["Metadata"]))
                       ?? throw new ArgumentException("Metadata must be a map."));
    }
}
