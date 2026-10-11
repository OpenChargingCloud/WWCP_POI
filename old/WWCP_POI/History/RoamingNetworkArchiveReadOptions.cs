/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Local capture policy for borrowed CBOR input streams, independent of signed archive content.
/// </summary>
public sealed class RoamingNetworkArchiveReadOptions
{
    /// <summary>
    /// Maximum private managed capture capacity before switching to an owned temporary file.
    /// Zero forces file capture; the archive byte budget also bounds memory and file bytes.
    /// </summary>
    public Int32 MemoryThresholdBytes { get; }

    /// <summary>
    /// Absolute existing directory for owned temporary files, resolved when options are created.
    /// It is inspected only when spilling; linked directory ancestors are rejected.
    /// </summary>
    public String TemporaryDirectory { get; }

    /// <summary>
    /// Local temporary archive namespace, accepted by TryInspectArchive for explicit orphan maintenance.
    /// Readers hold shared read leases; its coordination lock is retained after capture.
    /// </summary>
    public String TemporaryArchivePath { get; }

    /// <summary>
    /// Construct a capture policy. A null directory selects the operating system temporary directory.
    /// The caller administers this directory; recovery never creates or deletes directories.
    /// </summary>
    public RoamingNetworkArchiveReadOptions(Int32 memoryThresholdBytes = 1024 * 1024, String? temporaryDirectory = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(memoryThresholdBytes);
        if (temporaryDirectory is not null) ArgumentException.ThrowIfNullOrWhiteSpace(temporaryDirectory);
        MemoryThresholdBytes = memoryThresholdBytes;
        TemporaryDirectory = Path.GetFullPath(temporaryDirectory ?? Path.GetTempPath());
        TemporaryArchivePath = Path.Combine(TemporaryDirectory, "wwcp-poi-input.cbor");
    }
}
