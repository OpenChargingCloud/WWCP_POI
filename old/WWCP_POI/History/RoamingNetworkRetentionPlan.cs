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
/// An immutable local review plan binding the complete source archive and an explicit snapshot boundary.
/// Creating or exporting a plan does not delete history or write an archive.
/// </summary>
public sealed class RoamingNetworkRetentionPlan
{
    /// <summary>
    /// The canonical JSON identity of this review plan, excluding the derived identity itself.
    /// </summary>
    public ETag Id { get; }

    /// <summary>
    /// The proposed signed replay boundary in the original chain.
    /// </summary>
    public RoamingNetworkSnapshotBoundary Boundary { get; }

    /// <summary>
    /// The replay root before the proposed pruning operation.
    /// </summary>
    public RoamingNetworkCommitId BeforeAnchor { get; }

    /// <summary>
    /// The published head that must remain unchanged until execution.
    /// </summary>
    public RoamingNetworkCommitId ExpectedHead { get; }

    /// <summary>
    /// An optional first-parent cutoff; the snapshot must be at or before this commit.
    /// Calendar policy is the administrator's responsibility.
    /// </summary>
    public RoamingNetworkCommitId? CutoffCommit { get; }

    /// <summary>
    /// The administrator-supplied UTC planning timestamp, without implicit scheduling.
    /// </summary>
    public DateTimeOffset CreatedAt { get; }

    /// <summary>
    /// The exact deterministic CBOR source archive digest, including branches, peers and earlier receipts.
    /// </summary>
    public ETag SourceArchiveETag { get; }

    /// <summary>
    /// The commits retained after pruning, including every required merge dependency.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> RetainedCommits { get; }

    /// <summary>
    /// The commits removed from active replay and preserved in the cold archive.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> PrunedCommits { get; }

    /// <summary>
    /// Current frontier tips explicitly released from active retention by the administrator.
    /// Dependencies still required by another retained tip are kept.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> ArchiveOnlyTips { get; }

    /// <summary>
    /// Additional bases or unpublished work explicitly protected from pruning.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> ProtectedCommits { get; }

    internal RoamingNetworkRetentionPlan(RoamingNetworkSnapshotBoundary boundary, RoamingNetworkCommitId beforeAnchor,
        RoamingNetworkCommitId head, RoamingNetworkCommitId? cutoff, DateTimeOffset createdAt, ETag sourceArchiveETag,
        IEnumerable<RoamingNetworkCommitId> retained, IEnumerable<RoamingNetworkCommitId> pruned,
        IEnumerable<RoamingNetworkCommitId> archiveOnly, IEnumerable<RoamingNetworkCommitId> protectedCommits)
    {
        Boundary = boundary; BeforeAnchor = beforeAnchor; ExpectedHead = head; CutoffCommit = cutoff;
        CreatedAt = createdAt.ToUniversalTime(); SourceArchiveETag = sourceArchiveETag;
        RetainedCommits = Sort(retained); PrunedCommits = Sort(pruned);
        ArchiveOnlyTips = Sort(archiveOnly); ProtectedCommits = Sort(protectedCommits);
        using var document = JsonDocument.Parse(JSON(false));
        Id = ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(document));
    }

    internal static ImmutableArray<RoamingNetworkCommitId> Sort(IEnumerable<RoamingNetworkCommitId> ids)
        => ids.Distinct().OrderBy(id => id.ToString(), StringComparer.Ordinal).ToImmutableArray();

    /// <summary>
    /// Export a review document with structured commit IDs and digest tuples.
    /// </summary>
    public String ToJSON() => JSON(true);

    private String JSON(Boolean includeId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (includeId) { writer.WritePropertyName("Id"); Id.WriteTo(writer); }
            writer.WriteString("Profile", "wwcp-poi-retention-plan-v1");
            writer.WritePropertyName("CheckpointId"); Boundary.Checkpoint.Hash.WriteTo(writer);
            writer.WritePropertyName("BeforeAnchor"); BeforeAnchor.Hash.WriteTo(writer);
            writer.WritePropertyName("AfterAnchor"); Boundary.Anchor.Hash.WriteTo(writer);
            writer.WritePropertyName("Head"); ExpectedHead.Hash.WriteTo(writer);
            writer.WritePropertyName("CutoffCommit");
            if (CutoffCommit is { } cutoff) cutoff.Hash.WriteTo(writer); else writer.WriteNullValue();
            writer.WriteString("CreatedAt", CreatedAt.ToString("O"));
            writer.WritePropertyName("SourceArchiveETag"); SourceArchiveETag.WriteTo(writer);
            WriteIds(writer, "RetainedCommits", RetainedCommits);
            WriteIds(writer, "PrunedCommits", PrunedCommits);
            WriteIds(writer, "ArchiveOnlyTips", ArchiveOnlyTips);
            WriteIds(writer, "ProtectedCommits", ProtectedCommits);
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    internal static void WriteIds(Utf8JsonWriter writer, String name, IEnumerable<RoamingNetworkCommitId> ids)
    {
        writer.WritePropertyName(name); writer.WriteStartArray();
        foreach (var id in ids) id.Hash.WriteTo(writer);
        writer.WriteEndArray();
    }
}

/// <summary>
/// A protected tip whose complete parent closure cannot terminate at the proposed snapshot.
/// </summary>
public sealed record RoamingNetworkRetentionBlocker(RoamingNetworkCommitId Tip,
                                                     ImmutableArray<RoamingNetworkCommitId> RequiredHistory);

/// <summary>
/// The outcome of planning or explicitly executing a retention operation.
/// </summary>
public enum RoamingNetworkRetentionOutcome
{
    Planned,
    Pruned,
    Blocked,
    NothingToPrune,
    HeadConflict,
    InventoryChanged,
    InvalidInput,
    Unauthorized,
    PersistenceFailure,
    Unavailable
}

/// <summary>
/// A structured retention outcome; failures leave active history and its runtime head unchanged.
/// A completed cold archive can remain after an active archive write failure.
/// </summary>
public sealed record RoamingNetworkRetentionResult(RoamingNetworkRetentionOutcome Outcome,
    RoamingNetworkHead Head, String? Error = null,
    ImmutableArray<RoamingNetworkRetentionBlocker> Blockers = default,
    RoamingNetworkRetentionReceipt? Receipt = null, String? ColdArchivePath = null)
{
    /// <summary>
    /// Protected tips needing earlier history; initialized empty for outcomes without blockers.
    /// </summary>
    public ImmutableArray<RoamingNetworkRetentionBlocker> Blockers { get; init; }
        = Blockers.IsDefault ? ImmutableArray<RoamingNetworkRetentionBlocker>.Empty : Blockers;
}

/// <summary>
/// Whether an identity is retained, recorded as archived, or unknown to this replica.
/// </summary>
public enum RoamingNetworkCommitAvailability
{
    Retained,
    Archived,
    Unknown
}

/// <summary>
/// An atomic lookup of retained content or a historical pruning receipt and proposed replay boundary.
/// Receipts identify cold archive content; they do not independently authenticate omitted history.
/// </summary>
public sealed record RoamingNetworkCommitLookup(RoamingNetworkCommitAvailability Availability,
    RoamingNetworkCommit? Commit = null, RoamingNetworkRetentionReceipt? Receipt = null,
    RoamingNetworkSnapshotBoundary? ProposedBoundary = null);
