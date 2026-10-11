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
/// An incremental static commit page rooted at a retained checkpoint or signed snapshot.
/// Boundary wire pages carry root IDs and peers, resolved against local static content.
/// Runtime data is absent.
/// </summary>
public sealed class RoamingNetworkCommitPack
{
    /// <summary>
    /// The transport profile of incremental commit pages.
    /// </summary>
    public const String Profile = "wwcp-poi-commit-pack-v1";

    /// <summary>
    /// The incremental transport profile for pages containing full snapshot links.
    /// </summary>
    public const String SnapshotProfile = "wwcp-poi-commit-pack-v2";

    /// <summary>
    /// The transport profile explicitly rooted at a signed snapshot and original chain checkpoint.
    /// </summary>
    public const String BoundaryProfile = "wwcp-poi-commit-pack-v3";

    /// <summary>
    /// The original shared chain checkpoint ID, independently of this page's replay root.
    /// </summary>
    public RoamingNetworkCommitId Checkpoint { get; }

    /// <summary>
    /// The page's original replay root, either a checkpoint or a signed snapshot envelope.
    /// </summary>
    public RoamingNetworkCommit AnchorCommit => CheckpointCommit;

    /// <summary>
    /// Whether this page explicitly excludes ancestry preceding its snapshot root.
    /// </summary>
    public Boolean HasSnapshotAnchor => AnchorCommit.Kind == RoamingNetworkCommitKind.Snapshot;

    /// <summary>
    /// The profile required by this page's original payload envelopes.
    /// </summary>
    public String WireProfile => HasSnapshotAnchor ? BoundaryProfile :
        Commits.Any(commit => commit.Kind == RoamingNetworkCommitKind.Snapshot) ? SnapshotProfile : Profile;

    /// <summary>
    /// The replay root envelope; retained as a compatibility accessor for checkpoint-rooted pages.
    /// </summary>
    public RoamingNetworkCommit CheckpointCommit { get; }

    /// <summary>
    /// The fixed requested tip, which can be unpublished at the sender.
    /// </summary>
    public RoamingNetworkCommitId Tip { get; }

    /// <summary>
    /// Whether this page completes the requested tip relative to the receiver's announcement.
    /// The receiver independently checks that a complete page leaves the tip retained.
    /// </summary>
    public Boolean Complete { get; }

    /// <summary>
    /// Original immutable transition envelopes in parent-before-child order.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommit> Commits { get; }

    /// <summary>
    /// Construct a page without treating identities or sender declarations as trusted.
    /// Parent resolution, replay and authorization occur at import time.
    /// </summary>
    public RoamingNetworkCommitPack(RoamingNetworkCommit checkpointCommit, RoamingNetworkCommitId tip,
                                    Boolean complete, ImmutableArray<RoamingNetworkCommit> commits,
                                    RoamingNetworkCommitId? checkpoint = null)
    {
        ArgumentNullException.ThrowIfNull(checkpointCommit);
        Checkpoint = checkpoint ?? checkpointCommit.Id;
        if (checkpointCommit.Kind == RoamingNetworkCommitKind.Snapshot)
        {
            if (!Checkpoint.IsValid || Checkpoint == checkpointCommit.Id)
                throw new ArgumentException("A snapshot-rooted page requires its distinct original checkpoint claim.");
        }
        else if (checkpointCommit.Kind != RoamingNetworkCommitKind.Checkpoint || Checkpoint != checkpointCommit.Id)
            throw new ArgumentException("A page requires the original checkpoint or a signed snapshot root and its checkpoint claim.");
        if (!tip.IsValid || commits.IsDefault ||
            commits.Any(commit => commit is null || commit.Kind == RoamingNetworkCommitKind.Checkpoint || commit.Id == checkpointCommit.Id ||
                                  commit.RoamingNetworkId != checkpointCommit.RoamingNetworkId) ||
            commits.Select(commit => commit.Id).Distinct().Count() != commits.Length || (!complete && commits.IsEmpty))
            throw new ArgumentException("A pack requires a checkpoint, valid tip and distinct transition envelopes; incomplete pages must make progress.");
        CheckpointCommit = checkpointCommit;
        Tip = tip;
        Complete = complete;
        Commits = commits;
    }

    /// <summary>
    /// Serialize original commits and signatures through the lossless JSON contract.
    /// </summary>
    public String ToJSON()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", WireProfile);
            writer.WriteString("ContentProfile", POIContentProfile.Id);
            if (HasSnapshotAnchor)
            {
                writer.WritePropertyName("CheckpointId"); Checkpoint.Hash.WriteTo(writer);
                writer.WritePropertyName("AnchorId"); AnchorCommit.Id.Hash.WriteTo(writer);
                writer.WritePropertyName("AnchorSignatures"); JsonSerializer.Serialize(writer, AnchorCommit.Signatures);
            }
            else { writer.WritePropertyName("CheckpointCommit"); AnchorCommit.WriteTo(writer); }
            writer.WritePropertyName("Tip"); Tip.Hash.WriteTo(writer);
            writer.WriteBoolean("Complete", Complete);
            writer.WritePropertyName("Commits");
            writer.WriteStartArray();
            foreach (var commit in Commits) commit.WriteTo(writer);
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Decode an exact JSON page and recompute transition identities.
    /// Boundary pages require the exact locally retained snapshot through resolveAnchor.
    /// </summary>
    public static RoamingNetworkCommitPack Parse(String json, Int32 maxCommits = 128, Int32 maxBytes = 1024 * 1024,
        Func<RoamingNetworkCommitId, RoamingNetworkCommit?>? resolveAnchor = null)
    {
        RequireLimits(maxCommits, maxBytes);
        if (Encoding.UTF8.GetByteCount(json) > maxBytes) throw new ArgumentException("The JSON page exceeds maxBytes.");
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement;
        var profile = value.GetProperty("Profile").GetString();
        var boundary = profile == BoundaryProfile;
        if (profile is not (Profile or SnapshotProfile or BoundaryProfile))
            throw new ArgumentException("Unsupported commit-pack profile.");
        if (boundary) RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "CheckpointId", "AnchorId", "AnchorSignatures", "Tip", "Complete", "Commits");
        else RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "CheckpointCommit", "Tip", "Complete", "Commits");
        POIContentProfile.Require(value.GetProperty("ContentProfile").GetString());
        if (value.GetProperty("Commits").GetArrayLength() > maxCommits) throw new ArgumentException("The page exceeds maxCommits.");
        var root = boundary ? ResolveAnchor(JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("AnchorId")),
            JsonSerializer.Deserialize<ImmutableArray<RoamingNetworkChangeSetSignature>>(value.GetProperty("AnchorSignatures")), resolveAnchor) :
            RoamingNetworkCommit.Parse(value.GetProperty("CheckpointCommit"));
        var pack = new RoamingNetworkCommitPack(root,
                   JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Tip")),
                   value.GetProperty("Complete").GetBoolean(),
                   value.GetProperty("Commits").EnumerateArray().Select(RoamingNetworkCommit.Parse).ToImmutableArray(),
                   boundary ? JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("CheckpointId")) : null);
        if (value.GetProperty("Profile").GetString() != pack.WireProfile) throw new ArgumentException("Unsupported or inconsistent commit-pack profile.");
        return pack;
    }

    /// <summary>
    /// Serialize native deterministic CBOR maps, binary digests and lossless ChangeSets.
    /// </summary>
    public Byte[] ToCBOR()
    {
        var fields = new List<(String Key, CBORValue Value)> {
            ("Profile", CBORValue.FromText(WireProfile)),
            ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
            ("Tip", Tip.ToCBOR()),
            ("Complete", CBORValue.FromBoolean(Complete)),
            ("Commits", CBORValue.FromArray(Commits.Select(commit => commit.ToCBORValue()))) };
        if (HasSnapshotAnchor)
        {
            fields.Add(("CheckpointId", Checkpoint.ToCBOR()));
            fields.Add(("AnchorId", AnchorCommit.Id.ToCBOR()));
            fields.Add(("AnchorSignatures", CBORValue.FromArray(AnchorCommit.Signatures.Select(signature => RoamingNetworkCommit.Map(
                ("Profile", CBORValue.FromText(signature.Profile)), ("Algorithm", CBORValue.FromText(signature.Algorithm)),
                ("KeyId", CBORValue.FromText(signature.KeyId)), ("Encoding", CBORValue.FromText(signature.Encoding)),
                ("Value", CBORValue.FromText(signature.Value)))))));
        }
        else fields.Add(("CheckpointCommit", AnchorCommit.ToCBORValue()));
        return RoamingNetworkCommit.Map(fields.ToArray()).ToByteArray(CBORWriterOptions.Canonical);
    }

    /// <summary>
    /// Decode a native CBOR page and check its contract and transition identities.
    /// Boundary pages require the exact locally retained snapshot through resolveAnchor.
    /// </summary>
    public static RoamingNetworkCommitPack ParseCBOR(ReadOnlySpan<Byte> bytes, Int32 maxCommits = 128, Int32 maxBytes = 1024 * 1024,
        Func<RoamingNetworkCommitId, RoamingNetworkCommit?>? resolveAnchor = null)
    {
        RequireLimits(maxCommits, maxBytes);
        if (bytes.Length > maxBytes) throw new ArgumentException("The CBOR page exceeds maxBytes.");
        var value = CBORValue.Parse(bytes);
        var profile = RoamingNetworkCommit.Text(value.AsMap().Single(entry => RoamingNetworkCommit.Text(entry.Key) == "Profile").Value);
        var boundary = profile == BoundaryProfile;
        if (profile is not (Profile or SnapshotProfile or BoundaryProfile))
            throw new ArgumentException("Unsupported commit-pack profile.");
        var fields = boundary ? RoamingNetworkCommit.Fields(value, "Profile", "ContentProfile", "CheckpointId", "AnchorId", "AnchorSignatures", "Tip", "Complete", "Commits") :
            RoamingNetworkCommit.Fields(value, "Profile", "ContentProfile", "CheckpointCommit", "Tip", "Complete", "Commits");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        if (fields["Commits"].AsArray().Count > maxCommits) throw new ArgumentException("The page exceeds maxCommits.");
        if (!fields["Complete"].TryGetBoolean(out var complete)) throw new ArgumentException("Complete must be a Boolean.");
        var root = boundary ? ResolveAnchor(new(ETag.Parse(fields["AnchorId"])), fields["AnchorSignatures"].AsArray().Select(value => {
            var signature = RoamingNetworkCommit.Fields(value, "Profile", "Algorithm", "KeyId", "Encoding", "Value");
            return new RoamingNetworkChangeSetSignature(RoamingNetworkCommit.Text(signature["Algorithm"]), RoamingNetworkCommit.Text(signature["KeyId"]),
                RoamingNetworkCommit.Text(signature["Value"]), RoamingNetworkCommit.Text(signature["Profile"]), RoamingNetworkCommit.Text(signature["Encoding"]));
        }).ToImmutableArray(), resolveAnchor) : RoamingNetworkCommit.ParseCBORValue(fields["CheckpointCommit"]);
        var pack = new RoamingNetworkCommitPack(root, new(ETag.Parse(fields["Tip"])), complete,
                   fields["Commits"].AsArray().Select(RoamingNetworkCommit.ParseCBORValue).ToImmutableArray(),
                   boundary ? new RoamingNetworkCommitId(ETag.Parse(fields["CheckpointId"])) : null);
        if (RoamingNetworkCommit.Text(fields["Profile"]) != pack.WireProfile) throw new ArgumentException("Unsupported or inconsistent commit-pack profile.");
        return pack;
    }

    private static RoamingNetworkCommit ResolveAnchor(RoamingNetworkCommitId id,
        ImmutableArray<RoamingNetworkChangeSetSignature> signatures,
        Func<RoamingNetworkCommitId, RoamingNetworkCommit?>? resolveAnchor)
    {
        if (!id.IsValid || signatures.IsDefault || resolveAnchor?.Invoke(id) is not { } root ||
            root.Id != id || root.Kind != RoamingNetworkCommitKind.Snapshot)
            throw new ArgumentException("A snapshot-rooted page requires its exact locally retained anchor and an initialized peer array.");
        return root.WithSignatures(signatures);
    }

    private static void RequireLimits(Int32 maxCommits, Int32 maxBytes)
    {
        if (maxCommits < 1 || maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxCommits), "Page limits must be positive.");
    }
}

/// <summary>
/// The outcome of creating or importing a bounded incremental page.
/// </summary>
public enum RoamingNetworkReplicationOutcome
{
    PackAvailable,
    Imported,
    AlreadyStored,
    CheckpointMismatch,
    UnknownTip,
    MissingParents,
    CommitTooLarge,
    ChangeSetIdConflict,
    InvalidInput,
    PersistenceFailure,
    Unavailable,
    SnapshotRequired,
    HistoryRequired
}

/// <summary>
/// A page outcome with the observed local head, counts and typed missing commit identities.
/// Import failures never install a partial page or advance the head.
/// </summary>
public sealed record RoamingNetworkReplicationResult(RoamingNetworkReplicationOutcome Outcome,
    RoamingNetworkHead Head, Int32 StoredCount = 0, Int32 DuplicateCount = 0,
    ImmutableArray<RoamingNetworkCommitId> MissingCommits = default, String? Error = null,
    RoamingNetworkSnapshotBoundary? ProposedBoundary = null,
    RoamingNetworkRetentionReceipt? RetentionReceipt = null)
{
    /// <summary>
    /// Missing external parents or a missing declared complete tip; empty for other outcomes.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> MissingCommits { get; init; }
        = MissingCommits.IsDefault ? ImmutableArray<RoamingNetworkCommitId>.Empty : MissingCommits;
}
