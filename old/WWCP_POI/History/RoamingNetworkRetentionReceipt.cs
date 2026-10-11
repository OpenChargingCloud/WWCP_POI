/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Content-addressed bookkeeping for an explicit pruning event and its complete preceding cold archive.
/// This unsigned receipt is not an authorization or an independent proof of omitted commit membership.
/// </summary>
public sealed class RoamingNetworkRetentionReceipt
{
    /// <summary>
    /// The versioned receipt serialization and identity contract.
    /// </summary>
    public const String Profile = "wwcp-poi-retention-receipt-v1";

    /// <summary>
    /// SHA-256 of canonical receipt JSON, excluding this derived identity.
    /// </summary>
    public ETag Id { get; }

    /// <summary>
    /// The immutable review plan identity approved for this operation.
    /// </summary>
    public ETag PlanId { get; }

    /// <summary>
    /// The original chain identity, unchanged by pruning.
    /// </summary>
    public RoamingNetworkCommitId Checkpoint { get; }

    /// <summary>
    /// The replay root before pruning.
    /// </summary>
    public RoamingNetworkCommitId BeforeAnchor { get; }

    /// <summary>
    /// The signed snapshot root after pruning.
    /// </summary>
    public RoamingNetworkCommitId AfterAnchor { get; }

    /// <summary>
    /// The published head preserved by this event.
    /// </summary>
    public RoamingNetworkCommitId Head { get; }

    /// <summary>
    /// The explicit UTC planning timestamp, not an independently trusted clock.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// The SHA-256 CBOR identity of the complete archive before pruning.
    /// Storage locations are local and are excluded from this receipt.
    /// </summary>
    public ETag SourceArchiveETag { get; }

    /// <summary>
    /// Original commit identities removed from active history by this event.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> PrunedCommits { get; }

    /// <summary>
    /// Frontier tips explicitly released from active retention.
    /// Some may remain if required as a dependency of a protected tip.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> ArchiveOnlyTips { get; }

    internal RoamingNetworkRetentionReceipt(RoamingNetworkRetentionPlan plan)
        : this(plan.Id, plan.Boundary.Checkpoint, plan.BeforeAnchor, plan.Boundary.Anchor, plan.ExpectedHead,
               plan.CreatedAt, plan.SourceArchiveETag, plan.PrunedCommits, plan.ArchiveOnlyTips)
    { }

    private RoamingNetworkRetentionReceipt(ETag planId, RoamingNetworkCommitId checkpoint,
        RoamingNetworkCommitId beforeAnchor, RoamingNetworkCommitId afterAnchor, RoamingNetworkCommitId head,
        DateTimeOffset createdAt, ETag sourceArchiveETag, ImmutableArray<RoamingNetworkCommitId> pruned,
        ImmutableArray<RoamingNetworkCommitId> archiveOnly)
    {
        if (!planId.IsValid || planId.Format != ETagFormat.JSON || !sourceArchiveETag.IsValid || sourceArchiveETag.Format != ETagFormat.CBOR ||
            !checkpoint.IsValid || !beforeAnchor.IsValid || !afterAnchor.IsValid || !head.IsValid || checkpoint == afterAnchor ||
            pruned.IsDefaultOrEmpty || archiveOnly.IsDefault || pruned.Any(id => !id.IsValid || id == afterAnchor || id == head) ||
            archiveOnly.Any(id => !id.IsValid || id == head) || pruned.Distinct().Count() != pruned.Length ||
            archiveOnly.Distinct().Count() != archiveOnly.Length)
            throw new ArgumentException("Invalid retention identities, archive digest or commit inventory.");
        PlanId = planId; Checkpoint = checkpoint; BeforeAnchor = beforeAnchor; AfterAnchor = afterAnchor; Head = head;
        CreatedAt = createdAt.ToUniversalTime(); SourceArchiveETag = sourceArchiveETag;
        PrunedCommits = RoamingNetworkRetentionPlan.Sort(pruned); ArchiveOnlyTips = RoamingNetworkRetentionPlan.Sort(archiveOnly);
        using var document = JsonDocument.Parse(JSON(false));
        Id = ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(document));
    }

    /// <summary>
    /// Export structured JSON identifiers and digest tuples.
    /// </summary>
    public String ToJSON() => JSON(true);

    private String JSON(Boolean includeId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteTo(writer, includeId);
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal void WriteTo(Utf8JsonWriter writer, Boolean includeId = true)
    {
        writer.WriteStartObject();
        if (includeId) { writer.WritePropertyName("Id"); Id.WriteTo(writer); }
        writer.WriteString("Profile", Profile);
        writer.WritePropertyName("PlanId"); PlanId.WriteTo(writer);
        writer.WritePropertyName("CheckpointId"); Checkpoint.Hash.WriteTo(writer);
        writer.WritePropertyName("BeforeAnchor"); BeforeAnchor.Hash.WriteTo(writer);
        writer.WritePropertyName("AfterAnchor"); AfterAnchor.Hash.WriteTo(writer);
        writer.WritePropertyName("Head"); Head.Hash.WriteTo(writer);
        writer.WriteString("CreatedAt", CreatedAt.ToString("O", CultureInfo.InvariantCulture));
        writer.WritePropertyName("SourceArchiveETag"); SourceArchiveETag.WriteTo(writer);
        RoamingNetworkRetentionPlan.WriteIds(writer, "PrunedCommits", PrunedCommits);
        RoamingNetworkRetentionPlan.WriteIds(writer, "ArchiveOnlyTips", ArchiveOnlyTips);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Export native CBOR maps and binary digest tuples.
    /// </summary>
    public Byte[] ToCBOR() => ToCBORValue().ToByteArray(CBORWriterOptions.Canonical);

    internal CBORValue ToCBORValue() => RoamingNetworkCommit.Map(
        ("Id", Id.ToCBOR()), ("Profile", CBORValue.FromText(Profile)), ("PlanId", PlanId.ToCBOR()),
        ("CheckpointId", Checkpoint.ToCBOR()), ("BeforeAnchor", BeforeAnchor.ToCBOR()),
        ("AfterAnchor", AfterAnchor.ToCBOR()), ("Head", Head.ToCBOR()),
        ("CreatedAt", CBORValue.FromText(CreatedAt.ToString("O", CultureInfo.InvariantCulture))),
        ("SourceArchiveETag", SourceArchiveETag.ToCBOR()),
        ("PrunedCommits", CBORValue.FromArray(PrunedCommits.Select(id => id.ToCBOR()))),
        ("ArchiveOnlyTips", CBORValue.FromArray(ArchiveOnlyTips.Select(id => id.ToCBOR()))));

    private static readonly String[] FieldNames = ["Id", "Profile", "PlanId", "CheckpointId", "BeforeAnchor",
        "AfterAnchor", "Head", "CreatedAt", "SourceArchiveETag", "PrunedCommits", "ArchiveOnlyTips"];

    /// <summary>
    /// Parse an exact JSON receipt and verify its content identity, without establishing trust.
    /// </summary>
    public static RoamingNetworkRetentionReceipt Parse(String json)
    {
        using var document = JsonDocument.Parse(json);
        return Parse(document.RootElement);
    }

    internal static RoamingNetworkRetentionReceipt Parse(JsonElement value)
    {
        RoamingNetworkCommit.RequireFields(value, FieldNames);
        if (value.GetProperty("Profile").GetString() != Profile) throw new ArgumentException("Unsupported retention receipt profile.");
        var receipt = new RoamingNetworkRetentionReceipt(JsonSerializer.Deserialize<ETag>(value.GetProperty("PlanId")),
            JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("CheckpointId")),
            JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("BeforeAnchor")),
            JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("AfterAnchor")),
            JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Head")),
            DateTimeOffset.ParseExact(value.GetProperty("CreatedAt").GetString()!, "O", CultureInfo.InvariantCulture),
            JsonSerializer.Deserialize<ETag>(value.GetProperty("SourceArchiveETag")),
            value.GetProperty("PrunedCommits").EnumerateArray().Select(id => JsonSerializer.Deserialize<RoamingNetworkCommitId>(id)).ToImmutableArray(),
            value.GetProperty("ArchiveOnlyTips").EnumerateArray().Select(id => JsonSerializer.Deserialize<RoamingNetworkCommitId>(id)).ToImmutableArray());
        if (receipt.Id != JsonSerializer.Deserialize<ETag>(value.GetProperty("Id"))) throw new ArgumentException("Retention receipt identity mismatch.");
        return receipt;
    }

    /// <summary>
    /// Parse native CBOR and verify the canonical JSON receipt identity, without establishing trust.
    /// </summary>
    public static RoamingNetworkRetentionReceipt ParseCBOR(ReadOnlySpan<Byte> bytes) => ParseCBORValue(CBORValue.Parse(bytes));

    internal static RoamingNetworkRetentionReceipt ParseCBORValue(CBORValue value)
    {
        var fields = RoamingNetworkCommit.Fields(value, FieldNames);
        if (RoamingNetworkCommit.Text(fields["Profile"]) != Profile) throw new ArgumentException("Unsupported retention receipt profile.");
        var receipt = new RoamingNetworkRetentionReceipt(ETag.Parse(fields["PlanId"]), new(ETag.Parse(fields["CheckpointId"])),
            new(ETag.Parse(fields["BeforeAnchor"])), new(ETag.Parse(fields["AfterAnchor"])), new(ETag.Parse(fields["Head"])),
            DateTimeOffset.ParseExact(RoamingNetworkCommit.Text(fields["CreatedAt"]), "O", CultureInfo.InvariantCulture),
            ETag.Parse(fields["SourceArchiveETag"]),
            fields["PrunedCommits"].AsArray().Select(id => new RoamingNetworkCommitId(ETag.Parse(id))).ToImmutableArray(),
            fields["ArchiveOnlyTips"].AsArray().Select(id => new RoamingNetworkCommitId(ETag.Parse(id))).ToImmutableArray());
        if (receipt.Id != ETag.Parse(fields["Id"])) throw new ArgumentException("Retention receipt identity mismatch.");
        return receipt;
    }
}
