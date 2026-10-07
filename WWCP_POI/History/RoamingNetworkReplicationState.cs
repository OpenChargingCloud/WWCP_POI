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
/// acknowledges its retained ancestry through every parent down to the declared replay root.
/// </summary>
public sealed class RoamingNetworkReplicationState
{
    /// <summary>
    /// The inventory wire profile; this announcement does not authenticate its sender.
    /// </summary>
    public const String Profile = "wwcp-poi-replication-state-v1";

    /// <summary>
    /// The announcement profile with an explicit trusted snapshot ancestry boundary.
    /// </summary>
    public const String BoundaryProfile = "wwcp-poi-replication-state-v2";

    /// <summary>
    /// The local replay root; predecessors outside a snapshot boundary are not acknowledged.
    /// </summary>
    public RoamingNetworkCommitId Anchor { get; }

    /// <summary>
    /// Whether acknowledged ancestry reaches the original checkpoint.
    /// </summary>
    public Boolean HasCompleteAncestry => Anchor == Checkpoint;

    /// <summary>
    /// The wire profile reflecting complete ancestry or an explicit snapshot boundary.
    /// </summary>
    public String WireProfile => HasCompleteAncestry ? Profile : BoundaryProfile;

    /// <summary>
    /// The original chain checkpoint shared as complete retained data or an authorized boundary claim.
    /// </summary>
    public RoamingNetworkCommitId Checkpoint { get; }

    /// <summary>
    /// The currently published head, independently of the retained frontier.
    /// </summary>
    public RoamingNetworkCommitId Head { get; }

    /// <summary>
    /// Distinct retained tips whose ancestry down to Anchor the receiver acknowledges.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> KnownTips { get; }

    /// <summary>
    /// Construct an immutable announcement; tip order is normalized by digest text.
    /// </summary>
    public RoamingNetworkReplicationState(RoamingNetworkCommitId checkpoint, RoamingNetworkCommitId head,
                                          ImmutableArray<RoamingNetworkCommitId> knownTips,
                                          RoamingNetworkCommitId? anchor = null)
    {
        if (!checkpoint.IsValid || !head.IsValid || knownTips.IsDefaultOrEmpty ||
            knownTips.Any(id => !id.IsValid) || knownTips.Distinct().Count() != knownTips.Length)
            throw new ArgumentException("Checkpoint, head and distinct nonempty known tips must be initialized.");
        Checkpoint = checkpoint;
        Anchor = anchor ?? checkpoint;
        if (!Anchor.IsValid) throw new ArgumentException("Anchor must be valid.", nameof(anchor));
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
            writer.WriteString("Profile", WireProfile);
            writer.WriteString("ContentProfile", POIContentProfile.Id);
            writer.WritePropertyName("Checkpoint"); Checkpoint.Hash.WriteTo(writer);
            if (!HasCompleteAncestry) { writer.WritePropertyName("Anchor"); Anchor.Hash.WriteTo(writer); }
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
        var profile = value.GetProperty("Profile").GetString();
        if (profile is not (Profile or BoundaryProfile)) throw new ArgumentException("Unsupported replication-state profile.");
        if (profile == BoundaryProfile) RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "Checkpoint", "Anchor", "Head", "KnownTips");
        else RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "Checkpoint", "Head", "KnownTips");
        POIContentProfile.Require(value.GetProperty("ContentProfile").GetString());
        if (value.GetProperty("KnownTips").GetArrayLength() > maxTips) throw new ArgumentException("The announcement exceeds maxTips.");
        var state = new RoamingNetworkReplicationState(JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Checkpoint")),
                   JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Head")),
                   JsonSerializer.Deserialize<ImmutableArray<RoamingNetworkCommitId>>(value.GetProperty("KnownTips")),
                   profile == BoundaryProfile ? JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Anchor")) : null);
        if (state.WireProfile != profile) throw new ArgumentException("Inconsistent ancestry boundary profile.");
        return state;
    }

    /// <summary>
    /// Serialize deterministic native CBOR with binary digest tuples.
    /// </summary>
    public Byte[] ToCBOR()
    {
        var fields = new List<(String Key, CBORValue Value)> {
            ("Profile", CBORValue.FromText(WireProfile)),
            ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
            ("Checkpoint", Checkpoint.ToCBOR()), ("Head", Head.ToCBOR()),
            ("KnownTips", CBORValue.FromArray(KnownTips.Select(id => id.ToCBOR()))) };
        if (!HasCompleteAncestry) fields.Add(("Anchor", Anchor.ToCBOR()));
        return RoamingNetworkCommit.Map(fields.ToArray()).ToByteArray(CBORWriterOptions.Canonical);
    }

    /// <summary>
    /// Parse a native CBOR announcement and reject unknown fields or profiles.
    /// </summary>
    public static RoamingNetworkReplicationState ParseCBOR(ReadOnlySpan<Byte> bytes, Int32 maxTips = 4096, Int32 maxBytes = 1024 * 1024)
    {
        RequireLimits(maxTips, maxBytes);
        if (bytes.Length > maxBytes) throw new ArgumentException("The announcement exceeds maxBytes.");
        var value = CBORValue.Parse(bytes);
        var profile = RoamingNetworkCommit.Text(value.AsMap().Single(entry => RoamingNetworkCommit.Text(entry.Key) == "Profile").Value);
        if (profile is not (Profile or BoundaryProfile)) throw new ArgumentException("Unsupported replication-state profile.");
        var fields = profile == BoundaryProfile ? RoamingNetworkCommit.Fields(value, "Profile", "ContentProfile", "Checkpoint", "Anchor", "Head", "KnownTips") :
            RoamingNetworkCommit.Fields(value, "Profile", "ContentProfile", "Checkpoint", "Head", "KnownTips");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        if (fields["KnownTips"].AsArray().Count > maxTips) throw new ArgumentException("The announcement exceeds maxTips.");
        var state = new RoamingNetworkReplicationState(new(ETag.Parse(fields["Checkpoint"])), new(ETag.Parse(fields["Head"])),
                   fields["KnownTips"].AsArray().Select(item => new RoamingNetworkCommitId(ETag.Parse(item))).ToImmutableArray(),
                   profile == BoundaryProfile ? new RoamingNetworkCommitId(ETag.Parse(fields["Anchor"])) : null);
        if (state.WireProfile != profile) throw new ArgumentException("Inconsistent ancestry boundary profile.");
        return state;
    }

    private static void RequireLimits(Int32 maxTips, Int32 maxBytes)
    {
        if (maxTips < 1 || maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxTips), "Announcement limits must be positive.");
    }
}
