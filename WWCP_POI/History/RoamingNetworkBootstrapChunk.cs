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
/// A detached immutable fragment of a frozen archive, bound to its manifest and position.
/// Digests are verified against the manifest when staging, rather than trusting this envelope.
/// </summary>
public sealed class RoamingNetworkBootstrapChunk
{
    /// <summary>
    /// The fragment wire profile.
    /// </summary>
    public const String Profile = "wwcp-poi-bootstrap-chunk-v1";

    /// <summary>
    /// The canonical JSON identity of the complete manifest.
    /// </summary>
    public ETag Manifest { get; }

    /// <summary>
    /// Zero-based position in the ordered archive fragments.
    /// </summary>
    public Int32 Index { get; }

    /// <summary>
    /// Immutable raw archive bytes; these may split a CBOR value at any byte boundary.
    /// </summary>
    public ImmutableArray<Byte> Data { get; }

    /// <summary>
    /// Construct a detached fragment without assigning trust to its manifest identity.
    /// </summary>
    public RoamingNetworkBootstrapChunk(ETag manifest, Int32 index, ReadOnlySpan<Byte> data)
    {
        if (!manifest.IsValid || manifest.Format != ETagFormat.JSON || index < 0 || data.IsEmpty)
            throw new ArgumentException("A fragment requires a JSON manifest identity, nonnegative index and nonempty bytes.");
        Manifest = manifest; Index = index; Data = ImmutableArray.CreateRange(data.ToArray());
    }

    /// <summary>
    /// Encode raw bytes as explicitly labelled canonical Base64 in JSON.
    /// </summary>
    public String ToJSON()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject(); writer.WriteString("Profile", Profile); writer.WriteString("ContentProfile", POIContentProfile.Id);
            writer.WritePropertyName("Manifest"); Manifest.WriteTo(writer); writer.WriteNumber("Index", Index);
            writer.WriteString("DataEncoding", "base64"); writer.WriteBase64String("Data", Data.AsSpan()); writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Decode an exact bounded JSON fragment before allowing it to enter staging.
    /// </summary>
    public static RoamingNetworkBootstrapChunk Parse(String json, RoamingNetworkBootstrapLimits? limits = null)
    {
        limits ??= new();
        if (Encoding.UTF8.GetByteCount(json) > limits.MaxWireBytes) throw new ArgumentException("Fragment exceeds MaxWireBytes.");
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement;
        RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "Manifest", "Index", "DataEncoding", "Data");
        if (value.GetProperty("Profile").GetString() != Profile || value.GetProperty("DataEncoding").GetString() != "base64")
            throw new ArgumentException("Unsupported fragment profile or encoding.");
        POIContentProfile.Require(value.GetProperty("ContentProfile").GetString());
        return new(JsonSerializer.Deserialize<ETag>(value.GetProperty("Manifest")), value.GetProperty("Index").GetInt32(),
            RoamingNetworkBootstrapManifest.DecodeBase64(value.GetProperty("Data").GetString()!, limits.MaxChunkBytes));
    }

    /// <summary>
    /// Encode native CBOR with binary payload and digest strings.
    /// </summary>
    public Byte[] ToCBOR() => RoamingNetworkCommit.Map(
        ("Profile", CBORValue.FromText(Profile)), ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
        ("Manifest", Manifest.ToCBOR()), ("Index", CBORValue.FromInt64(Index)),
        ("Data", CBORValue.FromBytes(Data.ToArray()))).ToByteArray(CBORWriterOptions.Canonical);

    /// <summary>
    /// Decode an exact bounded native CBOR fragment.
    /// </summary>
    public static RoamingNetworkBootstrapChunk ParseCBOR(ReadOnlySpan<Byte> bytes, RoamingNetworkBootstrapLimits? limits = null)
    {
        limits ??= new();
        if (bytes.Length > limits.MaxWireBytes) throw new ArgumentException("Fragment exceeds MaxWireBytes.");
        var fields = RoamingNetworkCommit.Fields(CBORValue.Parse(bytes), "Profile", "ContentProfile", "Manifest", "Index", "Data");
        if (RoamingNetworkCommit.Text(fields["Profile"]) != Profile) throw new ArgumentException("Unsupported fragment profile.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        if (!fields["Data"].TryGetBytes(out var data) || data.Length > limits.MaxChunkBytes) throw new ArgumentException("Invalid or oversized fragment payload.");
        return new(ETag.Parse(fields["Manifest"]), RoamingNetworkBootstrapManifest.Integer(fields["Index"]), data);
    }

    internal void RequireLimits(RoamingNetworkBootstrapLimits limits)
    {
        if (Data.Length > limits.MaxChunkBytes || Encoding.UTF8.GetByteCount(ToJSON()) > limits.MaxWireBytes || ToCBOR().Length > limits.MaxWireBytes)
            throw new ArgumentException("Fragment exceeds local raw or encoded byte limits.");
    }
}

/// <summary>
/// An immutable source session retaining one frozen archive independently of later live changes.
/// Keep this session, or an identical archive, available to resume the same manifest.
/// </summary>
public sealed class RoamingNetworkBootstrapSource
{
    private readonly Byte[] archive;
    private readonly RoamingNetworkBootstrapLimits limits;

    /// <summary>
    /// The fixed manifest for every fragment produced by this source session.
    /// </summary>
    public RoamingNetworkBootstrapManifest Manifest { get; }

    internal RoamingNetworkBootstrapSource(Byte[] archive, RoamingNetworkCommitId checkpoint, RoamingNetworkCommitId head,
        Int32 commits, Int32 chunkBytes, RoamingNetworkBootstrapLimits limits, String archiveProfile = RoamingNetworkHistory.ArchiveProfile,
        RoamingNetworkCommitId? anchor = null)
    {
        if (chunkBytes < 1 || chunkBytes > limits.MaxChunkBytes || archive.Length > limits.MaxArchiveBytes ||
            ((Int64) archive.Length + chunkBytes - 1) / chunkBytes > limits.MaxChunks || commits > limits.MaxCommits)
            throw new ArgumentException("Frozen archive exceeds local bootstrap limits.");
        this.archive = archive; this.limits = limits;
        var digests = ImmutableArray.CreateBuilder<ImmutableArray<Byte>>();
        for (var offset = 0; offset < archive.Length; )
        {
            var length = Math.Min(chunkBytes, archive.Length - offset);
            digests.Add(ImmutableArray.CreateRange(System.Security.Cryptography.SHA256.HashData(archive.AsSpan(offset, length))));
            offset += length;
        }
        Manifest = new(checkpoint, head, commits, archive.Length, chunkBytes, ETag.Compute(ETagFormat.CBOR, archive), digests.ToImmutable(), archiveProfile, anchor);
        Manifest.RequireLimits(limits);
        // The last full fragment has the widest position encoding; also check the short tail.
        CreateChunk(Math.Max(0, Manifest.ChunkCount - 2));
        if (Manifest.ChunkCount > 1) CreateChunk(Manifest.ChunkCount - 1);
    }

    /// <summary>
    /// Produce a bounded original archive fragment, including on resumed requests.
    /// </summary>
    public RoamingNetworkBootstrapChunk CreateChunk(Int32 index)
    {
        var length = Manifest.Length(index);
        var chunk = new RoamingNetworkBootstrapChunk(Manifest.Id, index, archive.AsSpan(index * Manifest.ChunkBytes, length));
        chunk.RequireLimits(limits);
        return chunk;
    }
}
