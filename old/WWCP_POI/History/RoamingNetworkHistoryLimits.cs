/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Locally selected bounds for archive decoding and replay, independent of wire metadata.
/// </summary>
public sealed class RoamingNetworkHistoryLimits
{
    /// <summary>
    /// Maximum encoded archive length; JSON is measured in UTF-8 bytes.
    /// </summary>
    public Int32 MaxArchiveBytes { get; }

    /// <summary>
    /// Maximum retained commit count, including the checkpoint or snapshot root.
    /// </summary>
    public Int32 MaxCommits { get; }

    /// <summary>
    /// Maximum number of pruning receipts in one archive.
    /// </summary>
    public Int32 MaxRetentionReceipts { get; }

    /// <summary>
    /// Maximum total entries in all receipts' PrunedCommits and ArchiveOnlyTips arrays.
    /// Repeated identities across arrays or receipts consume the budget again.
    /// </summary>
    public Int32 MaxCatalogCommitIds { get; }

    /// <summary>
    /// Construct positive local limits. Exact limits are accepted.
    /// </summary>
    public RoamingNetworkHistoryLimits(Int32 maxArchiveBytes = 256 * 1024 * 1024,
        Int32 maxCommits = 100000, Int32 maxRetentionReceipts = 4096, Int32 maxCatalogCommitIds = 1000000)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxArchiveBytes, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCommits, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxRetentionReceipts, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxCatalogCommitIds, 1);
        MaxArchiveBytes = maxArchiveBytes; MaxCommits = maxCommits;
        MaxRetentionReceipts = maxRetentionReceipts; MaxCatalogCommitIds = maxCatalogCommitIds;
    }

    internal void Require(RoamingNetworkHistoryLimitKind kind, Int64 observed)
    {
        var maximum = kind switch {
            RoamingNetworkHistoryLimitKind.ArchiveBytes      => MaxArchiveBytes,
            RoamingNetworkHistoryLimitKind.RetainedCommits   => MaxCommits,
            RoamingNetworkHistoryLimitKind.RetentionReceipts => MaxRetentionReceipts,
            RoamingNetworkHistoryLimitKind.CatalogCommitIds  => MaxCatalogCommitIds,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        if (observed > maximum) throw new RoamingNetworkHistoryLimitException(new(kind, maximum, observed));
    }
}

/// <summary>
/// The local archive budget that was exceeded.
/// </summary>
public enum RoamingNetworkHistoryLimitKind
{
    ArchiveBytes,
    RetainedCommits,
    RetentionReceipts,
    CatalogCommitIds
}

/// <summary>
/// A structured rejection with the local maximum and observed byte or item count.
/// Streaming scans may stop at the first item beyond the budget.
/// </summary>
public sealed record RoamingNetworkHistoryLimitViolation(RoamingNetworkHistoryLimitKind Kind, Int64 Maximum, Int64 Observed);

/// <summary>
/// Archive recovery exceeded a locally selected budget before document materialization and replay.
/// </summary>
public sealed class RoamingNetworkHistoryLimitException : ArgumentException
{
    /// <summary>
    /// The exceeded budget and counts, suitable for application diagnostics.
    /// </summary>
    public RoamingNetworkHistoryLimitViolation Violation { get; }

    /// <summary>
    /// Construct a rejection describing the exceeded local budget.
    /// </summary>
    public RoamingNetworkHistoryLimitException(RoamingNetworkHistoryLimitViolation violation)
        : base($"History archive exceeds {violation.Kind}: observed {violation.Observed}, maximum {violation.Maximum}.")
    {
        Violation = violation;
    }
}
