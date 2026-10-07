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
/// Archive replay currently requires a complete in-memory CBOR document.
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
    /// Maximum retained commit count, including the checkpoint.
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
    /// Construct positive local limits. Incoming manifests cannot enlarge them.
    /// </summary>
    public RoamingNetworkBootstrapLimits(Int32 maxArchiveBytes = 256 * 1024 * 1024,
        Int32 maxChunkBytes = 1024 * 1024, Int32 maxChunks = 4096, Int32 maxCommits = 100000,
        Int32 maxWireBytes = 2 * 1024 * 1024, Int32 maxManifestBytes = 512 * 1024)
    {
        if (maxArchiveBytes < 1 || maxChunkBytes < 1 || maxChunks < 1 || maxCommits < 1 ||
            maxWireBytes < 1 || maxManifestBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxArchiveBytes));
        MaxArchiveBytes = maxArchiveBytes; MaxChunkBytes = maxChunkBytes; MaxChunks = maxChunks;
        MaxCommits = maxCommits; MaxWireBytes = maxWireBytes; MaxManifestBytes = maxManifestBytes;
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
        ImmutableArray<ImmutableArray<Byte>> chunkDigests)
    {
        if (!checkpoint.IsValid || !head.IsValid || commitCount < 1 || archiveBytes < 1 || chunkBytes < 1 ||
            !archiveETag.IsValid || archiveETag.Format != ETagFormat.CBOR || chunkDigests.IsDefaultOrEmpty ||
            chunkDigests.Length != ((Int64) archiveBytes + chunkBytes - 1) / chunkBytes ||
            chunkDigests.Any(digest => digest.IsDefault || digest.Length != 32))
            throw new ArgumentException("Invalid bootstrap identities, lengths, count or SHA-256 fragment digests.");
        Checkpoint = checkpoint; Head = head; CommitCount = commitCount;
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
            writer.WriteString("Profile", Profile); writer.WriteString("ContentProfile", POIContentProfile.Id);
            writer.WriteString("ArchiveProfile", RoamingNetworkHistory.ArchiveProfile);
            writer.WritePropertyName("Checkpoint"); Checkpoint.Hash.WriteTo(writer);
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
        RoamingNetworkCommit.RequireFields(value, "Id", "Profile", "ContentProfile", "ArchiveProfile", "Checkpoint", "Head",
            "CommitCount", "ArchiveBytes", "ChunkBytes", "ArchiveETag", "DigestAlgorithm", "ChunkDigestEncoding", "ChunkDigests");
        RequireProfiles(value.GetProperty("Profile").GetString(), value.GetProperty("ContentProfile").GetString(),
            value.GetProperty("ArchiveProfile").GetString(), value.GetProperty("DigestAlgorithm").GetString());
        if (value.GetProperty("ChunkDigestEncoding").GetString() != "base64" || value.GetProperty("ChunkDigests").GetArrayLength() > limits.MaxChunks)
            throw new ArgumentException("Unsupported fragment encoding or excessive fragment count.");
        var manifest = new RoamingNetworkBootstrapManifest(JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Checkpoint")),
            JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Head")), value.GetProperty("CommitCount").GetInt32(),
            value.GetProperty("ArchiveBytes").GetInt32(), value.GetProperty("ChunkBytes").GetInt32(),
            JsonSerializer.Deserialize<ETag>(value.GetProperty("ArchiveETag")), value.GetProperty("ChunkDigests").EnumerateArray()
                .Select(item => ImmutableArray.CreateRange(DecodeBase64(item.GetString()!, 32))).ToImmutableArray());
        manifest.RequireLimits(limits);
        if (manifest.Id != JsonSerializer.Deserialize<ETag>(value.GetProperty("Id"))) throw new ArgumentException("Manifest identity mismatch.");
        return manifest;
    }

    /// <summary>
    /// Encode native CBOR maps and binary digest strings.
    /// </summary>
    public Byte[] ToCBOR() => RoamingNetworkCommit.Map(
        ("Id", Id.ToCBOR()), ("Profile", CBORValue.FromText(Profile)), ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
        ("ArchiveProfile", CBORValue.FromText(RoamingNetworkHistory.ArchiveProfile)), ("Checkpoint", Checkpoint.ToCBOR()), ("Head", Head.ToCBOR()),
        ("CommitCount", CBORValue.FromInt64(CommitCount)), ("ArchiveBytes", CBORValue.FromInt64(ArchiveBytes)),
        ("ChunkBytes", CBORValue.FromInt64(ChunkBytes)), ("ArchiveETag", ArchiveETag.ToCBOR()), ("DigestAlgorithm", CBORValue.FromText("sha256")),
        ("ChunkDigests", CBORValue.FromArray(ChunkDigests.Select(digest => CBORValue.FromBytes(digest.ToArray()))))).ToByteArray(CBORWriterOptions.Canonical);

    /// <summary>
    /// Decode a bounded exact CBOR manifest and recompute its shared identity.
    /// </summary>
    public static RoamingNetworkBootstrapManifest ParseCBOR(ReadOnlySpan<Byte> bytes, RoamingNetworkBootstrapLimits? limits = null)
    {
        limits ??= new();
        if (bytes.Length > limits.MaxManifestBytes) throw new ArgumentException("Manifest exceeds MaxManifestBytes.");
        var fields = RoamingNetworkCommit.Fields(CBORValue.Parse(bytes), "Id", "Profile", "ContentProfile", "ArchiveProfile", "Checkpoint", "Head",
            "CommitCount", "ArchiveBytes", "ChunkBytes", "ArchiveETag", "DigestAlgorithm", "ChunkDigests");
        RequireProfiles(RoamingNetworkCommit.Text(fields["Profile"]), RoamingNetworkCommit.Text(fields["ContentProfile"]),
            RoamingNetworkCommit.Text(fields["ArchiveProfile"]), RoamingNetworkCommit.Text(fields["DigestAlgorithm"]));
        if (fields["ChunkDigests"].AsArray().Count > limits.MaxChunks) throw new ArgumentException("Manifest exceeds MaxChunks.");
        var manifest = new RoamingNetworkBootstrapManifest(new(ETag.Parse(fields["Checkpoint"])), new(ETag.Parse(fields["Head"])),
            Integer(fields["CommitCount"]), Integer(fields["ArchiveBytes"]), Integer(fields["ChunkBytes"]), ETag.Parse(fields["ArchiveETag"]),
            fields["ChunkDigests"].AsArray().Select(item => {
                if (!item.TryGetBytes(out var digest) || digest.Length != 32) throw new ArgumentException("Expected a 32-byte fragment digest.");
                return ImmutableArray.CreateRange(digest);
            }).ToImmutableArray());
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
        if (profile != Profile || archive != RoamingNetworkHistory.ArchiveProfile || algorithm != "sha256")
            throw new ArgumentException("Unsupported bootstrap profile, archive profile or digest algorithm.");
        POIContentProfile.Require(content);
    }
}
