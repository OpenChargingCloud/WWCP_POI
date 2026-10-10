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
/// The explicitly selected local storage namespace to inspect and maintain.
/// </summary>
public enum RoamingNetworkArchiveCleanupScope
{
    Archive,
    Bootstrap
}

/// <summary>
/// Whether a recognized file is installed data or an unacknowledged temporary sibling.
/// </summary>
public enum RoamingNetworkArchiveFileKind
{
    Installed,
    Temporary
}

/// <summary>
/// An immutable snapshot of a regular file's exact raw bytes and filesystem metadata.
/// </summary>
public sealed class RoamingNetworkArchiveFileState
{
    /// <summary>
    /// The leaf filename in the selected namespace.
    /// </summary>
    public String Name { get; }

    /// <summary>
    /// Installed files are protected; only temporary siblings can be removed.
    /// </summary>
    public RoamingNetworkArchiveFileKind Kind { get; }

    /// <summary>
    /// The exact raw byte length observed before hashing.
    /// </summary>
    public Int64 Length { get; }

    /// <summary>
    /// The observed UTC filesystem creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// The observed UTC filesystem last-write timestamp.
    /// </summary>
    public DateTime LastWrite { get; }

    /// <summary>
    /// The observed regular-file attributes, including read-only state.
    /// </summary>
    public FileAttributes Attributes { get; }

    /// <summary>
    /// Immutable SHA-256 digest bytes of the complete raw file, independent of document encoding.
    /// </summary>
    public ImmutableArray<Byte> SHA256Digest { get; }

    internal RoamingNetworkArchiveFileState(String name, RoamingNetworkArchiveFileKind kind, Int64 length,
        DateTime createdAt, DateTime lastWrite, FileAttributes attributes, Byte[] digest)
    {
        Name = name; Kind = kind; Length = length; CreatedAt = createdAt; LastWrite = lastWrite; Attributes = attributes;
        SHA256Digest = digest.ToImmutableArray();
    }

    internal Boolean SameAs(RoamingNetworkArchiveFileState other)
        => Name == other.Name && Kind == other.Kind && Length == other.Length && CreatedAt == other.CreatedAt &&
           LastWrite == other.LastWrite && Attributes == other.Attributes && SHA256Digest.AsSpan().SequenceEqual(other.SHA256Digest.AsSpan());
}

/// <summary>
/// Positive local bounds for directory inventory and streaming raw-file hashing.
/// </summary>
public sealed class RoamingNetworkArchiveCleanupLimits
{
    /// <summary>
    /// Maximum direct directory entries, including ignored files and directories.
    /// </summary>
    public Int32 MaxDirectoryEntries { get; }

    /// <summary>
    /// Maximum recognized installed and temporary files together.
    /// </summary>
    public Int32 MaxFiles { get; }

    /// <summary>
    /// Maximum byte length of one recognized file.
    /// </summary>
    public Int64 MaxFileBytes { get; }

    /// <summary>
    /// Maximum aggregate byte length of all recognized files.
    /// </summary>
    public Int64 MaxTotalBytes { get; }

    /// <summary>
    /// Construct positive limits. Exact budgets and zero-length files are accepted.
    /// </summary>
    public RoamingNetworkArchiveCleanupLimits(Int32 maxDirectoryEntries = 100000, Int32 maxFiles = 8192,
        Int64 maxFileBytes = 1024L * 1024 * 1024, Int64 maxTotalBytes = 4L * 1024 * 1024 * 1024)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDirectoryEntries, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFiles, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxFileBytes, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTotalBytes, 1);
        MaxDirectoryEntries = maxDirectoryEntries; MaxFiles = maxFiles; MaxFileBytes = maxFileBytes; MaxTotalBytes = maxTotalBytes;
    }
}

/// <summary>
/// Immutable local review of installed files and recognizable temporary siblings under one writer lease.
/// Its deterministic identity binds target, scope, local budgets, raw digests and filesystem timestamps.
/// </summary>
public sealed class RoamingNetworkArchiveCleanupPlan
{
    /// <summary>
    /// SHA-256 of canonical local review JSON, excluding the derived identity.
    /// </summary>
    public ETag Id { get; }

    /// <summary>
    /// The namespace whose writer lease must be acquired again for execution.
    /// </summary>
    public RoamingNetworkArchiveCleanupScope Scope { get; }

    /// <summary>
    /// The absolute selected archive path or bootstrap staging directory.
    /// </summary>
    public String Target { get; }

    /// <summary>
    /// The fixed local inventory and hashing budgets used again during execution.
    /// </summary>
    public RoamingNetworkArchiveCleanupLimits Limits { get; }

    /// <summary>
    /// Recognized files in ordinal filename order, including protected installed data.
    /// </summary>
    public ImmutableArray<RoamingNetworkArchiveFileState> Files { get; }

    /// <summary>
    /// Only these reviewed temporary files are eligible for explicit removal.
    /// </summary>
    public ImmutableArray<RoamingNetworkArchiveFileState> TemporaryFiles { get; }

    internal RoamingNetworkArchiveCleanupPlan(RoamingNetworkArchiveCleanupScope scope, String target,
        RoamingNetworkArchiveCleanupLimits limits, ImmutableArray<RoamingNetworkArchiveFileState> files)
    {
        Scope = scope; Target = target; Limits = limits; Files = files;
        TemporaryFiles = files.Where(file => file.Kind == RoamingNetworkArchiveFileKind.Temporary).ToImmutableArray();
        using var json = JsonDocument.Parse(JSON(false));
        Id = ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(json));
    }

    /// <summary>
    /// Export a local administrative review document, containing paths and raw digest hex strings.
    /// This document is independent of all POI transport, identity and signature profiles.
    /// </summary>
    public String ToJSON() => JSON(true);

    private String JSON(Boolean includeId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            if (includeId) { writer.WritePropertyName("Id"); Id.WriteTo(writer); }
            writer.WriteString("Scope", Scope.ToString()); writer.WriteString("Target", Target);
            writer.WriteStartObject("Limits");
            writer.WriteNumber("MaxDirectoryEntries", Limits.MaxDirectoryEntries); writer.WriteNumber("MaxFiles", Limits.MaxFiles);
            writer.WriteNumber("MaxFileBytes", Limits.MaxFileBytes); writer.WriteNumber("MaxTotalBytes", Limits.MaxTotalBytes); writer.WriteEndObject();
            writer.WriteStartArray("Files");
            foreach (var file in Files)
            {
                writer.WriteStartObject(); writer.WriteString("Name", file.Name); writer.WriteString("Kind", file.Kind.ToString());
                writer.WriteNumber("Length", file.Length); writer.WriteString("CreatedAt", file.CreatedAt.ToString("O", CultureInfo.InvariantCulture));
                writer.WriteString("LastWrite", file.LastWrite.ToString("O", CultureInfo.InvariantCulture));
                writer.WriteNumber("Attributes", (Int32) file.Attributes);
                writer.WriteString("SHA256Hex", Convert.ToHexString(file.SHA256Digest.AsSpan()).ToLowerInvariant()); writer.WriteEndObject();
            }
            writer.WriteEndArray(); writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }
}

/// <summary>
/// The local inventory or cleanup outcome; deletion failures may leave explicit partial progress.
/// </summary>
public enum RoamingNetworkArchiveCleanupOutcome
{
    Inspected,
    CleanupAvailable,
    Cleaned,
    NothingToClean,
    InventoryChanged,
    PersistenceFailure,
    UnsupportedFile,
    LimitExceeded,
    InvalidInput
}

/// <summary>
/// The local inventory budget that was exceeded.
/// </summary>
public enum RoamingNetworkArchiveCleanupLimitKind { DirectoryEntries, Files, FileBytes, TotalBytes }

/// <summary>
/// A structured local inventory budget rejection.
/// </summary>
public sealed record RoamingNetworkArchiveCleanupLimitViolation(RoamingNetworkArchiveCleanupLimitKind Kind, Int64 Maximum, Int64 Observed);

/// <summary>
/// Local maintenance diagnostics with the reviewed plan, removed filenames and a failing path or budget.
/// Installed data and unrelated files are never scheduled for deletion.
/// </summary>
public sealed record RoamingNetworkArchiveCleanupResult(RoamingNetworkArchiveCleanupOutcome Outcome,
    RoamingNetworkArchiveCleanupPlan? Plan = null, String? Error = null,
    ImmutableArray<String> RemovedFiles = default, String? AffectedPath = null,
    RoamingNetworkArchiveCleanupLimitViolation? LimitViolation = null)
{
    /// <summary>
    /// Exact reviewed filenames already removed, initialized empty even on failure before deletion.
    /// </summary>
    public ImmutableArray<String> RemovedFiles { get; init; } = RemovedFiles.IsDefault ? [] : RemovedFiles;
}
