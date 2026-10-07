/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// A receiver's checkpoint, published head and retained DAG frontier. Acknowledging a tip
/// acknowledges all of its parents, including unpublished branches and merge parents.
/// </summary>
public sealed class RoamingNetworkReplicationState
{
    /// <summary>
    /// The inventory wire profile; this announcement does not authenticate its sender.
    /// </summary>
    public const String Profile = "wwcp-poi-replication-state-v1";

    /// <summary>
    /// The checkpoint that both replicas must already share for incremental exchange.
    /// </summary>
    public RoamingNetworkCommitId Checkpoint { get; }

    /// <summary>
    /// The currently published head, independently of the retained frontier.
    /// </summary>
    public RoamingNetworkCommitId Head { get; }

    /// <summary>
    /// Distinct retained tips whose complete ancestry the receiver acknowledges.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> KnownTips { get; }

    /// <summary>
    /// Construct an immutable announcement; tip order is normalized by digest text.
    /// </summary>
    public RoamingNetworkReplicationState(RoamingNetworkCommitId checkpoint, RoamingNetworkCommitId head,
                                          ImmutableArray<RoamingNetworkCommitId> knownTips)
    {
        if (!checkpoint.IsValid || !head.IsValid || knownTips.IsDefaultOrEmpty ||
            knownTips.Any(id => !id.IsValid) || knownTips.Distinct().Count() != knownTips.Length)
            throw new ArgumentException("Checkpoint, head and distinct nonempty known tips must be initialized.");
        Checkpoint = checkpoint;
        Head = head;
        KnownTips = knownTips.OrderBy(id => id.ToString(), StringComparer.Ordinal).ToImmutableArray();
    }

    /// <summary>
    /// Serialize the announcement with structured JSON digest tuples.
    /// </summary>
    public String ToJSON()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", Profile);
            writer.WriteString("ContentProfile", POIContentProfile.Id);
            writer.WritePropertyName("Checkpoint"); Checkpoint.Hash.WriteTo(writer);
            writer.WritePropertyName("Head"); Head.Hash.WriteTo(writer);
            writer.WritePropertyName("KnownTips"); JsonSerializer.Serialize(writer, KnownTips);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Parse the exact JSON announcement contract without assigning trust to the sender.
    /// </summary>
    public static RoamingNetworkReplicationState Parse(String json, Int32 maxTips = 4096, Int32 maxBytes = 1024 * 1024)
    {
        RequireLimits(maxTips, maxBytes);
        if (Encoding.UTF8.GetByteCount(json) > maxBytes) throw new ArgumentException("The announcement exceeds maxBytes.");
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement;
        RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "Checkpoint", "Head", "KnownTips");
        if (value.GetProperty("Profile").GetString() != Profile) throw new ArgumentException("Unsupported replication-state profile.");
        POIContentProfile.Require(value.GetProperty("ContentProfile").GetString());
        if (value.GetProperty("KnownTips").GetArrayLength() > maxTips) throw new ArgumentException("The announcement exceeds maxTips.");
        return new(JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Checkpoint")),
                   JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Head")),
                   JsonSerializer.Deserialize<ImmutableArray<RoamingNetworkCommitId>>(value.GetProperty("KnownTips")));
    }

    /// <summary>
    /// Serialize deterministic native CBOR with binary digest tuples.
    /// </summary>
    public Byte[] ToCBOR()
        => RoamingNetworkCommit.Map(
            ("Profile", CBORValue.FromText(Profile)),
            ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
            ("Checkpoint", Checkpoint.ToCBOR()), ("Head", Head.ToCBOR()),
            ("KnownTips", CBORValue.FromArray(KnownTips.Select(id => id.ToCBOR())))).ToByteArray(CBORWriterOptions.Canonical);

    /// <summary>
    /// Parse a native CBOR announcement and reject unknown fields or profiles.
    /// </summary>
    public static RoamingNetworkReplicationState ParseCBOR(ReadOnlySpan<Byte> bytes, Int32 maxTips = 4096, Int32 maxBytes = 1024 * 1024)
    {
        RequireLimits(maxTips, maxBytes);
        if (bytes.Length > maxBytes) throw new ArgumentException("The announcement exceeds maxBytes.");
        var fields = RoamingNetworkCommit.Fields(CBORValue.Parse(bytes), "Profile", "ContentProfile", "Checkpoint", "Head", "KnownTips");
        if (RoamingNetworkCommit.Text(fields["Profile"]) != Profile) throw new ArgumentException("Unsupported replication-state profile.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        if (fields["KnownTips"].AsArray().Count > maxTips) throw new ArgumentException("The announcement exceeds maxTips.");
        return new(new(ETag.Parse(fields["Checkpoint"])), new(ETag.Parse(fields["Head"])),
                   fields["KnownTips"].AsArray().Select(value => new RoamingNetworkCommitId(ETag.Parse(value))).ToImmutableArray());
    }

    private static void RequireLimits(Int32 maxTips, Int32 maxBytes)
    {
        if (maxTips < 1 || maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxTips), "Announcement limits must be positive.");
    }
}
