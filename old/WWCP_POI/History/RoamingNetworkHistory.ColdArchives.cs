/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    /// <summary>
    /// Locate a receipt's exact cold bytes through an explicit immutable local catalog and replay with fresh trust.
    /// Missing, unreadable, excessive and digest-mismatched candidates are diagnosed in registration order.
    /// The first digest match must pass verification and receipt membership; trust failure never falls back.
    /// Success returns a separate in-memory history with fresh runtime and no persistent writer lease.
    /// An optional requested commit must be retained; otherwise CommitLookup describes older archival evidence.
    /// Matching bytes stay mapped through verification and are released before returning. Cancellation returns no history.
    /// Receipt provenance remains the application's responsibility, with an optional current authorization callback.
    /// </summary>
    public static Boolean TryReadColdArchive(RoamingNetworkRetentionReceipt receipt, RoamingNetworkColdArchiveCatalog catalog,
        [NotNullWhen(true)] out RoamingNetworkHistory? history, out RoamingNetworkColdArchiveResult result,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null,
        RoamingNetworkHistoryLimits? limits = null, RoamingNetworkCommitId? requestedCommit = null,
        Func<RoamingNetworkRetentionReceipt, Boolean>? authorizeReceipt = null, CancellationToken cancellationToken = default)
    {
        history = null;
        RoamingNetworkHistory? restored = null;
        var attempts = ImmutableArray.CreateBuilder<RoamingNetworkColdArchiveCandidateResult>();
        String? selected = null;
        var verifying = false;
        try
        {
            ArgumentNullException.ThrowIfNull(receipt);
            ArgumentNullException.ThrowIfNull(catalog);
            if (requestedCommit is { IsValid: false }) throw new ArgumentException("Invalid requested commit identity.", nameof(requestedCommit));
            limits ??= new();
            cancellationToken.ThrowIfCancellationRequested();
            verifying = true;
            var authorized = authorizeReceipt?.Invoke(receipt) ?? true;
            cancellationToken.ThrowIfCancellationRequested();
            if (!authorized)
            {
                result = new(RoamingNetworkColdArchiveOutcome.Unauthorized, receipt, Error: "The application did not authorize this receipt.");
                return false;
            }
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var path in catalog.GetCandidates(receipt.SourceArchiveETag))
            {
                cancellationToken.ThrowIfCancellationRequested();
                POIArchiveMappedFile candidate;
                try { candidate = new(path, limits); }
                catch (RoamingNetworkHistoryLimitException exception)
                {
                    attempts.Add(new(path, RoamingNetworkColdArchiveCandidateOutcome.LimitExceeded,
                        Error: exception.Message, LimitViolation: exception.Violation));
                    continue;
                }
                catch (IOException exception)
                {
                    attempts.Add(new(path, exception is FileNotFoundException or DirectoryNotFoundException
                        ? RoamingNetworkColdArchiveCandidateOutcome.NotFound : RoamingNetworkColdArchiveCandidateOutcome.ReadFailure,
                        Error: exception.Message));
                    continue;
                }
                catch (UnauthorizedAccessException exception)
                {
                    attempts.Add(new(path, RoamingNetworkColdArchiveCandidateOutcome.ReadFailure, Error: exception.Message));
                    continue;
                }
                using (candidate)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var observed = ETag.Compute(ETagFormat.CBOR, candidate.Bytes);
                    cancellationToken.ThrowIfCancellationRequested();
                    if (observed != receipt.SourceArchiveETag)
                    {
                        attempts.Add(new(path, RoamingNetworkColdArchiveCandidateOutcome.DigestMismatch, observed));
                        continue;
                    }
                    attempts.Add(new(path, RoamingNetworkColdArchiveCandidateOutcome.DigestMatched, observed));
                    selected = path;
                    restored = RestoreCapturedInput(candidate.Bytes, limits, verifyBatchSignature, verifyCommitSignature,
                        authorizeCommit, authorizeSnapshotBoundary, cancellationToken);
                }
                break;
            }
            if (restored is null)
            {
                result = new(attempts.All(attempt => attempt.Outcome == RoamingNetworkColdArchiveCandidateOutcome.NotFound)
                    ? RoamingNetworkColdArchiveOutcome.ArchiveNotFound : RoamingNetworkColdArchiveOutcome.CandidatesRejected,
                    receipt, attempts.ToImmutable(), "No readable candidate matched the expected archive digest.");
                return false;
            }
            cancellationToken.ThrowIfCancellationRequested();
            var commits = restored.Commits.ToDictionary(commit => commit.Id);
            if (restored.CheckpointId != receipt.Checkpoint || restored.AnchorId != receipt.BeforeAnchor || restored.Head.Id != receipt.Head ||
                !commits.TryGetValue(receipt.AfterAnchor, out var snapshot) || snapshot.Kind != RoamingNetworkCommitKind.Snapshot || snapshot.Signatures.IsEmpty ||
                receipt.PrunedCommits.Any(id => !commits.ContainsKey(id)) || receipt.ArchiveOnlyTips.Any(id => !commits.ContainsKey(id)))
            {
                result = new(RoamingNetworkColdArchiveOutcome.ReceiptMismatch, receipt, attempts.ToImmutable(),
                    "The verified source archive does not contain the receipt's chain, roots, head and recorded commit inventory.", selected);
                return false;
            }
            var lookup = requestedCommit is { } requested ? restored.LookupCommit(requested) : null;
            if (lookup is not null && lookup.Availability != RoamingNetworkCommitAvailability.Retained)
            {
                result = new(RoamingNetworkColdArchiveOutcome.CommitNotRetained, receipt, attempts.ToImmutable(),
                    "The requested commit is not retained in this cold archive.", selected, lookup);
                return false;
            }
            cancellationToken.ThrowIfCancellationRequested();
            history = restored; restored = null;
            result = new(RoamingNetworkColdArchiveOutcome.Recovered, receipt, attempts.ToImmutable(), ResolvedPath: selected, CommitLookup: lookup);
            return true;
        }
        catch (OperationCanceledException exception) when (cancellationToken.IsCancellationRequested)
        {
            result = new(RoamingNetworkColdArchiveOutcome.Cancelled, receipt, attempts.ToImmutable(), exception.Message, selected);
            return false;
        }
        catch (RoamingNetworkHistoryLimitException exception)
        {
            result = new(RoamingNetworkColdArchiveOutcome.LimitExceeded, receipt, attempts.ToImmutable(),
                exception.Message, selected, LimitViolation: exception.Violation);
            return false;
        }
        catch (Exception exception)
        {
            result = new(verifying ? RoamingNetworkColdArchiveOutcome.VerificationFailed : RoamingNetworkColdArchiveOutcome.InvalidInput,
                receipt, attempts.ToImmutable(), exception.Message, selected);
            return false;
        }
        finally { restored?.Dispose(); }
    }
}
