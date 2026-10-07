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
/// An incremental static commit page. The checkpoint envelope carries peer signatures,
/// while the checkpoint snapshot must already exist locally. Runtime data is absent.
/// </summary>
public sealed class RoamingNetworkCommitPack
{
    /// <summary>
    /// The transport profile of incremental commit pages.
    /// </summary>
    public const String Profile = "wwcp-poi-commit-pack-v1";

    /// <summary>
    /// The shared checkpoint envelope, including its current peer signatures.
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
                                    Boolean complete, ImmutableArray<RoamingNetworkCommit> commits)
    {
        ArgumentNullException.ThrowIfNull(checkpointCommit);
        if (checkpointCommit.ChangeSet is not null || !checkpointCommit.Parents.IsEmpty || !tip.IsValid || commits.IsDefault ||
            commits.Any(commit => commit is null || commit.ChangeSet is null || commit.Id == checkpointCommit.Id ||
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
            writer.WriteString("Profile", Profile);
            writer.WriteString("ContentProfile", POIContentProfile.Id);
            writer.WritePropertyName("CheckpointCommit"); CheckpointCommit.WriteTo(writer);
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
    /// Decode an exact JSON page contract and recompute each commit's identity.
    /// </summary>
    public static RoamingNetworkCommitPack Parse(String json, Int32 maxCommits = 128, Int32 maxBytes = 1024 * 1024)
    {
        RequireLimits(maxCommits, maxBytes);
        if (Encoding.UTF8.GetByteCount(json) > maxBytes) throw new ArgumentException("The JSON page exceeds maxBytes.");
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement;
        RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "CheckpointCommit", "Tip", "Complete", "Commits");
        if (value.GetProperty("Profile").GetString() != Profile) throw new ArgumentException("Unsupported commit-pack profile.");
        POIContentProfile.Require(value.GetProperty("ContentProfile").GetString());
        if (value.GetProperty("Commits").GetArrayLength() > maxCommits) throw new ArgumentException("The page exceeds maxCommits.");
        return new(RoamingNetworkCommit.Parse(value.GetProperty("CheckpointCommit")),
                   JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Tip")),
                   value.GetProperty("Complete").GetBoolean(),
                   value.GetProperty("Commits").EnumerateArray().Select(RoamingNetworkCommit.Parse).ToImmutableArray());
    }

    /// <summary>
    /// Serialize native deterministic CBOR maps, binary digests and lossless ChangeSets.
    /// </summary>
    public Byte[] ToCBOR()
        => RoamingNetworkCommit.Map(
            ("Profile", CBORValue.FromText(Profile)),
            ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
            ("CheckpointCommit", CheckpointCommit.ToCBORValue()), ("Tip", Tip.ToCBOR()),
            ("Complete", CBORValue.FromBoolean(Complete)),
            ("Commits", CBORValue.FromArray(Commits.Select(commit => commit.ToCBORValue())))).ToByteArray(CBORWriterOptions.Canonical);

    /// <summary>
    /// Decode a native CBOR page and check its exact contract and commit identities.
    /// </summary>
    public static RoamingNetworkCommitPack ParseCBOR(ReadOnlySpan<Byte> bytes, Int32 maxCommits = 128, Int32 maxBytes = 1024 * 1024)
    {
        RequireLimits(maxCommits, maxBytes);
        if (bytes.Length > maxBytes) throw new ArgumentException("The CBOR page exceeds maxBytes.");
        var fields = RoamingNetworkCommit.Fields(CBORValue.Parse(bytes), "Profile", "ContentProfile", "CheckpointCommit", "Tip", "Complete", "Commits");
        if (RoamingNetworkCommit.Text(fields["Profile"]) != Profile) throw new ArgumentException("Unsupported commit-pack profile.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        if (fields["Commits"].AsArray().Count > maxCommits) throw new ArgumentException("The page exceeds maxCommits.");
        if (!fields["Complete"].TryGetBoolean(out var complete)) throw new ArgumentException("Complete must be a Boolean.");
        return new(RoamingNetworkCommit.ParseCBORValue(fields["CheckpointCommit"]), new(ETag.Parse(fields["Tip"])), complete,
                   fields["Commits"].AsArray().Select(RoamingNetworkCommit.ParseCBORValue).ToImmutableArray());
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
    Unavailable
}

/// <summary>
/// A page outcome with the observed local head, counts and typed missing commit identities.
/// Import failures never install a partial page or advance the head.
/// </summary>
public sealed record RoamingNetworkReplicationResult(RoamingNetworkReplicationOutcome Outcome,
    RoamingNetworkHead Head, Int32 StoredCount = 0, Int32 DuplicateCount = 0,
    ImmutableArray<RoamingNetworkCommitId> MissingCommits = default, String? Error = null)
{
    /// <summary>
    /// Missing external parents or a missing declared complete tip; empty for other outcomes.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommitId> MissingCommits { get; init; }
        = MissingCommits.IsDefault ? ImmutableArray<RoamingNetworkCommitId>.Empty : MissingCommits;
}
