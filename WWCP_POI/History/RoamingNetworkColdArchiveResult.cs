/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The outcome of locating, verifying and replaying a receipt's cold archive.
/// </summary>
public enum RoamingNetworkColdArchiveOutcome
{
    Recovered,
    ArchiveNotFound,
    CandidatesRejected,
    Unauthorized,
    VerificationFailed,
    ReceiptMismatch,
    CommitNotRetained,
    LimitExceeded,
    InvalidInput,
    Cancelled
}

/// <summary>
/// The result of reading and hashing one local candidate before archive replay.
/// </summary>
public enum RoamingNetworkColdArchiveCandidateOutcome
{
    DigestMatched,
    NotFound,
    DigestMismatch,
    ReadFailure,
    LimitExceeded
}

/// <summary>
/// Immutable diagnostics for one attempted path; a digest match alone does not establish authority.
/// </summary>
public sealed record RoamingNetworkColdArchiveCandidateResult(String Path,
    RoamingNetworkColdArchiveCandidateOutcome Outcome, ETag? ObservedArchiveETag = null,
    String? Error = null, RoamingNetworkHistoryLimitViolation? LimitViolation = null);

/// <summary>
/// Structured recovery diagnostics; failure returns no history and preserves all candidate files.
/// CommitLookup can identify a still earlier receipt when a requested commit is archived rather than retained.
/// </summary>
public sealed record RoamingNetworkColdArchiveResult(RoamingNetworkColdArchiveOutcome Outcome,
    RoamingNetworkRetentionReceipt? Receipt, ImmutableArray<RoamingNetworkColdArchiveCandidateResult> Candidates = default,
    String? Error = null, String? ResolvedPath = null, RoamingNetworkCommitLookup? CommitLookup = null,
    RoamingNetworkHistoryLimitViolation? LimitViolation = null)
{
    /// <summary>
    /// Attempted paths in registration order, initialized empty even when no file was read.
    /// </summary>
    public ImmutableArray<RoamingNetworkColdArchiveCandidateResult> Candidates { get; init; }
        = Candidates.IsDefault ? [] : Candidates;
}
