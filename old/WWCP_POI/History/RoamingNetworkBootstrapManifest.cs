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
/// Receiver-controlled bounds for bootstrap transport, staging and archive replay.
/// Archive replay requires complete input bytes and retained models/states, with indexed CBOR validation.
/// </summary>
public sealed class RoamingNetworkBootstrapLimits
{
    /// <summary>
    /// Maximum complete archive length in bytes.
    /// </summary>
    public Int32 MaxArchiveBytes { get; }

    /// <summary>
    /// Maximum raw payload length of one chunk.
    /// </summary>
    public Int32 MaxChunkBytes { get; }

    /// <summary>
    /// Maximum chunk count advertised by a manifest.
    /// </summary>
    public Int32 MaxChunks { get; }

    /// <summary>
    /// Maximum retained commit count, including the checkpoint or snapshot root.
    /// </summary>
    public Int32 MaxCommits { get; }

    /// <summary>
    /// Maximum encoded chunk size in either wire representation.
    /// </summary>
    public Int32 MaxWireBytes { get; }

    /// <summary>
    /// Maximum encoded manifest size in either wire representation.
    /// </summary>
    public Int32 MaxManifestBytes { get; }

    /// <summary>
    /// Local archive bounds also checked against actual containers before final decoding and replay.
    /// </summary>
    public RoamingNetworkHistoryLimits HistoryLimits { get; }

    /// <summary>
    /// Maximum pruning receipts in the decoded archive.
    /// </summary>
    public Int32 MaxRetentionReceipts => HistoryLimits.MaxRetentionReceipts;

    /// <summary>
    /// Maximum total entries in all pruning receipts' two commit-ID arrays.
    /// </summary>
    public Int32 MaxCatalogCommitIds => HistoryLimits.MaxCatalogCommitIds;

    /// <summary>
    /// Construct positive local limits. Incoming manifests cannot enlarge them.
    /// </summary>
    public RoamingNetworkBootstrapLimits(Int32 maxArchiveBytes = 256 * 1024 * 1024,
        Int32 maxChunkBytes = 1024 * 1024, Int32 maxChunks = 4096, Int32 maxCommits = 100000,
        Int32 maxWireBytes = 2 * 1024 * 1024, Int32 maxManifestBytes = 512 * 1024,
        Int32 maxRetentionReceipts = 4096, Int32 maxCatalogCommitIds = 1000000)
    {
        if (maxArchiveBytes < 1 || maxChunkBytes < 1 || maxChunks < 1 || maxCommits < 1 ||
            maxWireBytes < 1 || maxManifestBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxArchiveBytes));
        MaxArchiveBytes = maxArchiveBytes; MaxChunkBytes = maxChunkBytes; MaxChunks = maxChunks;
        MaxCommits = maxCommits; MaxWireBytes = maxWireBytes; MaxManifestBytes = maxManifestBytes;
        HistoryLimits = new(maxArchiveBytes, maxCommits, maxRetentionReceipts, maxCatalogCommitIds);
    }
}

/// <summary>
/// A frozen static CBOR archive and its ordered SHA-256 fragment digests.
/// The manifest identity binds every field; hashes alone do not authenticate its sender.
/// </summary>
public sealed class RoamingNetworkBootstrapManifest
{
    /// <summary>
    /// The bootstrap manifest wire and identity profile.
    /// </summary>
    public const String Profile = "wwcp-poi-bootstrap-manifest-v1";

    /// <summary>
    /// The bootstrap manifest profile for complete archives containing snapshot links.
    /// </summary>
    public const String SnapshotProfile = "wwcp-poi-bootstrap-manifest-v2";

    /// <summary>
    /// The manifest profile binding an original chain and a trusted snapshot replay boundary.
    /// </summary>
    public const String BoundaryProfile = "wwcp-poi-bootstrap-manifest-v3";

    /// <summary>
    /// The manifest profile binding a snapshot archive with its explicit pruning receipt catalog.
    /// </summary>
    public const String RetentionProfile = "wwcp-poi-bootstrap-manifest-v4";

    /// <summary>
    /// The replay root captured by this archive, distinct from Checkpoint for partial histories.
    /// </summary>
    public RoamingNetworkCommitId Anchor { get; }

    /// <summary>
    /// The exact history archive profile bound into this manifest identity.
    /// </summary>
    public String ArchiveProfile { get; }

    /// <summary>
    /// The manifest profile corresponding to its bound archive contract.
    /// </summary>
    public String WireProfile => ArchiveProfile == RoamingNetworkHistory.RetentionArchiveProfile ? RetentionProfile :
        ArchiveProfile == RoamingNetworkHistory.BoundaryArchiveProfile ? BoundaryProfile :
        ArchiveProfile == RoamingNetworkHistory.SnapshotArchiveProfile ? SnapshotProfile : Profile;

    /// <summary>
    /// SHA-256 of canonical manifest JSON excluding this derived identity.
    /// </summary>
    public ETag Id { get; }

    /// <summary>
    /// The original checkpoint commit, including its version identity.
    /// </summary>
    public RoamingNetworkCommitId Checkpoint { get; }

    /// <summary>
    /// The original published head captured when the source was frozen.
    /// </summary>
    public RoamingNetworkCommitId Head { get; }

    /// <summary>
    /// Retained commits including the checkpoint and unpublished branches.
    /// </summary>
    public Int32 CommitCount { get; }

    /// <summary>
    /// Length of the complete deterministic CBOR history archive.
    /// </summary>
    public Int32 ArchiveBytes { get; }

    /// <summary>
    /// Fixed raw fragment length, except for the possibly shorter last fragment.
    /// </summary>
    public Int32 ChunkBytes { get; }

    /// <summary>
    /// SHA-256 of the entire CBOR archive, distinct from static POI state ETags.
    /// This also binds original peer signatures and the archive head reference.
    /// </summary>
    public ETag ArchiveETag { get; }

    /// <summary>
    /// Detached immutable SHA-256 digests of raw fragments, in transfer order.
    /// A fragment need not itself be a complete CBOR value.
    /// </summary>
    public ImmutableArray<ImmutableArray<Byte>> ChunkDigests { get; }

    /// <summary>
    /// The number of ordered fragments.
    /// </summary>
    public Int32 ChunkCount => ChunkDigests.Length;

    /// <summary>
    /// Construct an immutable manifest with a deterministic identity.
    /// </summary>
    public RoamingNetworkBootstrapManifest(RoamingNetworkCommitId checkpoint, RoamingNetworkCommitId head,
        Int32 commitCount, Int32 archiveBytes, Int32 chunkBytes, ETag archiveETag,
        ImmutableArray<ImmutableArray<Byte>> chunkDigests, String archiveProfile = RoamingNetworkHistory.ArchiveProfile,
        RoamingNetworkCommitId? anchor = null)
    {
        if (archiveProfile is not (RoamingNetworkHistory.ArchiveProfile or RoamingNetworkHistory.SnapshotArchiveProfile or RoamingNetworkHistory.BoundaryArchiveProfile or RoamingNetworkHistory.RetentionArchiveProfile))
            throw new ArgumentException("Unsupported history archive profile.", nameof(archiveProfile));
        Anchor = anchor ?? checkpoint;
        if (!Anchor.IsValid || (archiveProfile is RoamingNetworkHistory.BoundaryArchiveProfile or RoamingNetworkHistory.RetentionArchiveProfile) != (Anchor != checkpoint))
            throw new ArgumentException("The archive profile must match its explicit snapshot boundary.");
        if (!checkpoint.IsValid || !head.IsValid || commitCount < 1 || archiveBytes < 1 || chunkBytes < 1 ||
            !archiveETag.IsValid || archiveETag.Format != ETagFormat.CBOR || chunkDigests.IsDefaultOrEmpty ||
            chunkDigests.Length != ((Int64) archiveBytes + chunkBytes - 1) / chunkBytes ||
            chunkDigests.Any(digest => digest.IsDefault || digest.Length != 32))
            throw new ArgumentException("Invalid bootstrap identities, lengths, count or SHA-256 fragment digests.");
        Checkpoint = checkpoint; Head = head; CommitCount = commitCount;
        ArchiveProfile = archiveProfile;
        ArchiveBytes = archiveBytes; ChunkBytes = chunkBytes; ArchiveETag = archiveETag; ChunkDigests = chunkDigests;
        using var document = JsonDocument.Parse(JSON(includeId: false));
        Id = ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(document));
    }

    internal Int32 Length(Int32 index)
    {
        if (index < 0 || index >= ChunkCount) throw new ArgumentOutOfRangeException(nameof(index));
        return index == ChunkCount - 1 ? ArchiveBytes - index * ChunkBytes : ChunkBytes;
    }

    internal void RequireLimits(RoamingNetworkBootstrapLimits limits)
    {
        if (ArchiveBytes > limits.MaxArchiveBytes || ChunkBytes > limits.MaxChunkBytes ||
            ChunkCount > limits.MaxChunks || CommitCount > limits.MaxCommits ||
            Encoding.UTF8.GetByteCount(ToJSON()) > limits.MaxManifestBytes || ToCBOR().Length > limits.MaxManifestBytes)
            throw new ArgumentException("The manifest exceeds local bootstrap limits.");
    }

    /// <summary>
    /// Write structured identifiers and explicitly Base64-encoded fragment digests.
    /// </summary>
    public String ToJSON() => JSON(includeId: true);

    private String JSON(Boolean includeId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (includeId) { writer.WritePropertyName("Id"); Id.WriteTo(writer); }
            writer.WriteString("Profile", WireProfile); writer.WriteString("ContentProfile", POIContentProfile.Id);
            writer.WriteString("ArchiveProfile", ArchiveProfile);
            writer.WritePropertyName("Checkpoint"); Checkpoint.Hash.WriteTo(writer);
            if (WireProfile is BoundaryProfile or RetentionProfile) { writer.WritePropertyName("Anchor"); Anchor.Hash.WriteTo(writer); }
            writer.WritePropertyName("Head"); Head.Hash.WriteTo(writer);
            writer.WriteNumber("CommitCount", CommitCount); writer.WriteNumber("ArchiveBytes", ArchiveBytes);
            writer.WriteNumber("ChunkBytes", ChunkBytes);
            writer.WritePropertyName("ArchiveETag"); ArchiveETag.WriteTo(writer);
            writer.WriteString("DigestAlgorithm", "sha256"); writer.WriteString("ChunkDigestEncoding", "base64");
            writer.WritePropertyName("ChunkDigests"); writer.WriteStartArray();
            foreach (var digest in ChunkDigests) writer.WriteBase64StringValue(digest.AsSpan());
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Decode a bounded exact JSON manifest and recompute its identity.
    /// </summary>
    public static RoamingNetworkBootstrapManifest Parse(String json, RoamingNetworkBootstrapLimits? limits = null)
    {
        limits ??= new();
        if (Encoding.UTF8.GetByteCount(json) > limits.MaxManifestBytes) throw new ArgumentException("Manifest exceeds MaxManifestBytes.");
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement;
        var boundary = value.GetProperty("Profile").GetString() is BoundaryProfile or RetentionProfile;
        var names = new List<String> { "Id", "Profile", "ContentProfile", "ArchiveProfile", "Checkpoint", "Head",
            "CommitCount", "ArchiveBytes", "ChunkBytes", "ArchiveETag", "DigestAlgorithm", "ChunkDigestEncoding", "ChunkDigests" };
        if (boundary) names.Add("Anchor");
        RoamingNetworkCommit.RequireFields(value, names.ToArray());
        RequireProfiles(value.GetProperty("Profile").GetString(), value.GetProperty("ContentProfile").GetString(),
            value.GetProperty("ArchiveProfile").GetString(), value.GetProperty("DigestAlgorithm").GetString());
        if (value.GetProperty("ChunkDigestEncoding").GetString() != "base64" || value.GetProperty("ChunkDigests").GetArrayLength() > limits.MaxChunks)
            throw new ArgumentException("Unsupported fragment encoding or excessive fragment count.");
        var manifest = new RoamingNetworkBootstrapManifest(JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Checkpoint")),
            JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Head")), value.GetProperty("CommitCount").GetInt32(),
            value.GetProperty("ArchiveBytes").GetInt32(), value.GetProperty("ChunkBytes").GetInt32(),
            JsonSerializer.Deserialize<ETag>(value.GetProperty("ArchiveETag")), value.GetProperty("ChunkDigests").EnumerateArray()
                .Select(item => ImmutableArray.CreateRange(DecodeBase64(item.GetString()!, 32))).ToImmutableArray(),
            value.GetProperty("ArchiveProfile").GetString()!,
            boundary ? JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Anchor")) : null);
        manifest.RequireLimits(limits);
        if (manifest.Id != JsonSerializer.Deserialize<ETag>(value.GetProperty("Id"))) throw new ArgumentException("Manifest identity mismatch.");
        return manifest;
    }

    /// <summary>
    /// Encode native CBOR maps and binary digest strings.
    /// </summary>
    public Byte[] ToCBOR()
    {
        var fields = new List<(String Key, CBORValue Value)> {
        ("Id", Id.ToCBOR()), ("Profile", CBORValue.FromText(WireProfile)), ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
        ("ArchiveProfile", CBORValue.FromText(ArchiveProfile)), ("Checkpoint", Checkpoint.ToCBOR()), ("Head", Head.ToCBOR()),
        ("CommitCount", CBORValue.FromInt64(CommitCount)), ("ArchiveBytes", CBORValue.FromInt64(ArchiveBytes)),
        ("ChunkBytes", CBORValue.FromInt64(ChunkBytes)), ("ArchiveETag", ArchiveETag.ToCBOR()), ("DigestAlgorithm", CBORValue.FromText("sha256")),
        ("ChunkDigests", CBORValue.FromArray(ChunkDigests.Select(digest => CBORValue.FromBytes(digest.ToArray())))) };
        if (WireProfile is BoundaryProfile or RetentionProfile) fields.Add(("Anchor", Anchor.ToCBOR()));
        return RoamingNetworkCommit.Map(fields.ToArray()).ToByteArray(CBORWriterOptions.Canonical);
    }

    /// <summary>
    /// Decode a bounded exact CBOR manifest and recompute its shared identity.
    /// </summary>
    public static RoamingNetworkBootstrapManifest ParseCBOR(ReadOnlySpan<Byte> bytes, RoamingNetworkBootstrapLimits? limits = null)
    {
        limits ??= new();
        if (bytes.Length > limits.MaxManifestBytes) throw new ArgumentException("Manifest exceeds MaxManifestBytes.");
        var value = CBORValue.Parse(bytes);
        var boundary = RoamingNetworkCommit.Text(value.AsMap().Single(entry => RoamingNetworkCommit.Text(entry.Key) == "Profile").Value) is BoundaryProfile or RetentionProfile;
        var names = new List<String> { "Id", "Profile", "ContentProfile", "ArchiveProfile", "Checkpoint", "Head",
            "CommitCount", "ArchiveBytes", "ChunkBytes", "ArchiveETag", "DigestAlgorithm", "ChunkDigests" };
        if (boundary) names.Add("Anchor");
        var fields = RoamingNetworkCommit.Fields(value, names.ToArray());
        RequireProfiles(RoamingNetworkCommit.Text(fields["Profile"]), RoamingNetworkCommit.Text(fields["ContentProfile"]),
            RoamingNetworkCommit.Text(fields["ArchiveProfile"]), RoamingNetworkCommit.Text(fields["DigestAlgorithm"]));
        if (fields["ChunkDigests"].AsArray().Count > limits.MaxChunks) throw new ArgumentException("Manifest exceeds MaxChunks.");
        var manifest = new RoamingNetworkBootstrapManifest(new(ETag.Parse(fields["Checkpoint"])), new(ETag.Parse(fields["Head"])),
            Integer(fields["CommitCount"]), Integer(fields["ArchiveBytes"]), Integer(fields["ChunkBytes"]), ETag.Parse(fields["ArchiveETag"]),
            fields["ChunkDigests"].AsArray().Select(item => {
                if (!item.TryGetBytes(out var digest) || digest.Length != 32) throw new ArgumentException("Expected a 32-byte fragment digest.");
                return ImmutableArray.CreateRange(digest);
            }).ToImmutableArray(), RoamingNetworkCommit.Text(fields["ArchiveProfile"]),
            boundary ? new RoamingNetworkCommitId(ETag.Parse(fields["Anchor"])) : null);
        manifest.RequireLimits(limits);
        if (manifest.Id != ETag.Parse(fields["Id"])) throw new ArgumentException("Manifest identity mismatch.");
        return manifest;
    }

    internal static Int32 Integer(CBORValue value)
    {
        if (!value.TryGetInt64(out var number) || number < 0 || number > Int32.MaxValue) throw new ArgumentException("Expected a nonnegative Int32.");
        return (Int32) number;
    }

    internal static Byte[] DecodeBase64(String value, Int32 maxBytes)
    {
        if (value.Length > ((Int64) maxBytes + 2) / 3 * 4) throw new ArgumentException("Base64 data exceeds the decoded byte limit.");
        var bytes = Convert.FromBase64String(value);
        if (bytes.Length > maxBytes || Convert.ToBase64String(bytes) != value) throw new ArgumentException("Expected canonical Base64 within the byte limit.");
        return bytes;
    }

    private static void RequireProfiles(String? profile, String? content, String? archive, String? algorithm)
    {
        if (!((profile == Profile && archive == RoamingNetworkHistory.ArchiveProfile) ||
              (profile == SnapshotProfile && archive == RoamingNetworkHistory.SnapshotArchiveProfile) ||
              (profile == BoundaryProfile && archive == RoamingNetworkHistory.BoundaryArchiveProfile) ||
              (profile == RetentionProfile && archive == RoamingNetworkHistory.RetentionArchiveProfile)) || algorithm != "sha256")
            throw new ArgumentException("Unsupported bootstrap profile, archive profile or digest algorithm.");
        POIContentProfile.Require(content);
    }
}
