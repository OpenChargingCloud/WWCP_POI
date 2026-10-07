/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    private ImmutableArray<RoamingNetworkRetentionReceipt> retentionReceipts = [];
    private ImmutableDictionary<RoamingNetworkCommitId, RoamingNetworkRetentionReceipt> archivedCommits =
        ImmutableDictionary<RoamingNetworkCommitId, RoamingNetworkRetentionReceipt>.Empty;

    /// <summary>
    /// Immutable pruning receipts in execution order, without local storage paths.
    /// </summary>
    public ImmutableArray<RoamingNetworkRetentionReceipt> RetentionReceipts
    {
        get { lock (gate) return retentionReceipts; }
    }

    /// <summary>
    /// Read an independently stored cold archive into a separate in-memory history after checking its exact digest.
    /// Recovery rechecks caller-supplied trust and initializes fresh runtime; the cold file is never opened for mutation.
    /// A cold archive from an earlier pruning event may itself require snapshot-boundary authorization.
    /// </summary>
    public static RoamingNetworkHistory ReadColdArchive(String path, ETag expectedArchiveETag,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null)
    {
        if (!expectedArchiveETag.IsValid || expectedArchiveETag.Format != ETagFormat.CBOR)
            throw new ArgumentException("A valid CBOR archive digest is required.", nameof(expectedArchiveETag));
        using var file = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        using var stream = new MemoryStream();
        file.CopyTo(stream);
        var bytes = stream.ToArray();
        if (ETag.Compute(ETagFormat.CBOR, bytes) != expectedArchiveETag)
            throw new ArgumentException("Cold archive digest mismatch.");
        return ParseCBOR(bytes, verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeSnapshotBoundary);
    }

    /// <summary>
    /// Distinguish retained identities, recorded cold history and identities unknown to this replica.
    /// A reimported retained identity takes precedence over its historical pruning receipt.
    /// </summary>
    public RoamingNetworkCommitLookup LookupCommit(RoamingNetworkCommitId id)
    {
        if (!id.IsValid) throw new ArgumentException("Invalid commit identity.", nameof(id));
        lock (gate)
        {
            if (entries.TryGetValue(id, out var entry)) return new(RoamingNetworkCommitAvailability.Retained, entry.Commit);
            return archivedCommits.TryGetValue(id, out var receipt)
                ? new(RoamingNetworkCommitAvailability.Archived, Receipt: receipt, ProposedBoundary: AvailableBoundary())
                : new(RoamingNetworkCommitAvailability.Unknown);
        }
    }

    /// <summary>
    /// List signed snapshot candidates at or before a retained first-parent cutoff, newest first.
    /// A calendar threshold must first be mapped to a commit by the administrator's own time policy.
    /// Candidates still require branch and merge dependency checks by TryPlanRetention.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommit> GetRetentionSnapshots(RoamingNetworkCommitId cutoffCommit)
    {
        lock (gate)
        {
            if (!cutoffCommit.IsValid || !FirstParentLine(head.Id).Any(entry => entry.Commit.Id == cutoffCommit))
                throw new ArgumentException("The cutoff must be on the retained first-parent head line.");
            return FirstParentLine(cutoffCommit).Select(entry => entry.Commit)
                .Where(commit => commit.Kind == RoamingNetworkCommitKind.Snapshot && !commit.Signatures.IsEmpty).ToImmutableArray();
        }
    }

    /// <summary>
    /// Preview pruning at a signed snapshot, protecting the head, all frontier tips and explicit bases.
    /// Only explicitly named current frontier tips can be released into the complete cold archive.
    /// Every protected parent path, including merge parents, must terminate at the proposed snapshot.
    /// </summary>
    public Boolean TryPlanRetention(RoamingNetworkCommitId snapshotId, DateTimeOffset createdAt,
        [NotNullWhen(true)] out RoamingNetworkRetentionPlan? plan, out RoamingNetworkRetentionResult result,
        RoamingNetworkCommitId? cutoffCommit = null, IEnumerable<RoamingNetworkCommitId>? archiveOnlyTips = null,
        IEnumerable<RoamingNetworkCommitId>? protectedCommits = null)
    {
        lock (gate)
        {
            plan = null;
            if (disposed || mutating)
            {
                result = new(RoamingNetworkRetentionOutcome.Unavailable, head, "History is disposed or a mutation callback is reentrant.");
                return false;
            }
            mutating = true;
            try
            {
                VerifyBoundary();
                return PlanRetention(snapshotId, createdAt, cutoffCommit, archiveOnlyTips ?? [], protectedCommits ?? [], out plan, out result);
            }
            catch (Exception exception)
            {
                result = new(RoamingNetworkRetentionOutcome.InvalidInput, head, exception.Message);
                return false;
            }
            finally { mutating = false; }
        }
    }

    private Boolean PlanRetention(RoamingNetworkCommitId snapshotId, DateTimeOffset createdAt,
        RoamingNetworkCommitId? cutoff, IEnumerable<RoamingNetworkCommitId> archiveOnly,
        IEnumerable<RoamingNetworkCommitId> protectedIds,
        [NotNullWhen(true)] out RoamingNetworkRetentionPlan? plan, out RoamingNetworkRetentionResult result)
    {
        plan = null;
        if (!snapshotId.IsValid || !entries.TryGetValue(snapshotId, out var snapshot) ||
            !FirstParentLine(head.Id).Any(entry => entry.Commit.Id == snapshotId))
            throw new ArgumentException("The snapshot must be retained on the first-parent head line.");
        var boundary = new RoamingNetworkSnapshotBoundary(checkpointId, snapshot.Commit);
        VerifyCommit(snapshot.Commit);
        if (cutoff is { } cutoffId && (!cutoffId.IsValid ||
            !FirstParentLine(head.Id).Any(entry => entry.Commit.Id == cutoffId) ||
            !FirstParentLine(cutoffId).Any(entry => entry.Commit.Id == snapshotId)))
            throw new ArgumentException("The cutoff must be on the head line at or after the snapshot.");
        var parents = entries.Values.SelectMany(entry => entry.Commit.Parents).ToHashSet();
        var frontier = entries.Keys.Where(id => !parents.Contains(id)).ToHashSet();
        var released = archiveOnly.ToHashSet();
        var protectedSet = protectedIds.ToHashSet();
        if (released.Any(id => !id.IsValid || !frontier.Contains(id) || id == head.Id || protectedSet.Contains(id)) ||
            protectedSet.Any(id => !id.IsValid || !entries.ContainsKey(id)))
            throw new ArgumentException("Archive-only IDs must be current frontier tips other than head or protected bases; protected IDs must be retained.");
        var required = frontier.Except(released).Concat(protectedSet).Append(head.Id).Distinct();
        var keep = new HashSet<RoamingNetworkCommitId> { snapshotId };
        var blockers = ImmutableArray.CreateBuilder<RoamingNetworkRetentionBlocker>();
        foreach (var tip in RoamingNetworkRetentionPlan.Sort(required))
        {
            if (TryAncestorsWithin(tip, snapshotId, out var ancestry, out var missing)) keep.UnionWith(ancestry);
            else blockers.Add(new(tip, missing));
        }
        if (blockers.Count != 0)
        {
            result = new(RoamingNetworkRetentionOutcome.Blocked, head,
                "Protected branches or merge dependencies need history before the selected snapshot.", blockers.ToImmutable());
            return false;
        }
        var pruned = entries.Keys.Where(id => !keep.Contains(id)).ToArray();
        if (pruned.Length == 0)
        {
            result = new(RoamingNetworkRetentionOutcome.NothingToPrune, head, "The selected boundary and protected tips retain all current entries.");
            return false;
        }
        plan = new(boundary, anchorId, head.Id, cutoff, createdAt, ETag.Compute(ETagFormat.CBOR, Archive(entries, head.Id)),
            keep, pruned, released, protectedSet);
        result = new(RoamingNetworkRetentionOutcome.Planned, head);
        return true;
    }

    /// <summary>
    /// Execute an unchanged reviewed plan only when prune is explicitly true and the boundary policy accepts it.
    /// First durably publish the complete preceding cold archive, then atomically replace active persistence.
    /// The exact current head and its local runtime network are preserved; snapshots and signatures are not rewritten.
    /// </summary>
    public Boolean TryExecuteRetention(RoamingNetworkRetentionPlan plan, String coldArchivePath,
        Func<RoamingNetworkSnapshotBoundary, Boolean> authorizeBoundary, out RoamingNetworkRetentionResult result,
        Boolean prune = false, Func<RoamingNetworkRetentionPlan, Boolean>? authorizeRetention = null)
    {
        lock (gate)
        {
            if (disposed || mutating)
            {
                result = new(RoamingNetworkRetentionOutcome.Unavailable, head, "History is disposed or a mutation callback is reentrant.");
                return false;
            }
            mutating = true;
            try
            {
                ArgumentNullException.ThrowIfNull(plan);
                ArgumentNullException.ThrowIfNull(authorizeBoundary);
                if (head.Id != plan.ExpectedHead)
                {
                    result = new(RoamingNetworkRetentionOutcome.HeadConflict, head, "The published head changed after planning.");
                    return false;
                }
                var oldArchive = Archive(entries, head.Id);
                if (checkpointId != plan.Boundary.Checkpoint || anchorId != plan.BeforeAnchor ||
                    ETag.Compute(ETagFormat.CBOR, oldArchive) != plan.SourceArchiveETag)
                {
                    result = new(RoamingNetworkRetentionOutcome.InventoryChanged, head, "Branches, envelopes, receipts or the replay boundary changed after planning.");
                    return false;
                }
                VerifyBoundary();
                if (!PlanRetention(plan.Boundary.Anchor, plan.CreatedAt, plan.CutoffCommit, plan.ArchiveOnlyTips,
                    plan.ProtectedCommits, out var current, out result)) return false;
                if (current.Id != plan.Id) throw new ArgumentException("The review plan differs from recomputed retention requirements.");
                if (!prune)
                {
                    result = new(RoamingNetworkRetentionOutcome.Planned, head, "Preview only; explicit prune: true is required for execution.");
                    return true;
                }
                if (!authorizeBoundary(current.Boundary) || (authorizeRetention is not null && !authorizeRetention(current)))
                {
                    result = new(RoamingNetworkRetentionOutcome.Unauthorized, head, "The boundary or retention policy rejected the reviewed operation.");
                    return false;
                }
                var keep = current.RetainedCommits.ToHashSet();
                var updated = entries.Where(entry => keep.Contains(entry.Key)).ToImmutableDictionary();
                foreach (var entry in OrderedEntries(updated, current.Boundary.Anchor))
                {
                    VerifyCommit(entry.Commit);
                    VerifyBatch(entry.Commit);
                }
                var receipt = new RoamingNetworkRetentionReceipt(current);
                var receipts = retentionReceipts.Add(receipt);
                var catalog = IndexReceipts(receipts, checkpointId);
                var batches = ImmutableDictionary.Create<String, RoamingNetworkCommitId>(StringComparer.Ordinal);
                if (updated[current.Boundary.Anchor].Commit.AppliedChangeSetId is { } lastBatch)
                    batches = batches.Add(lastBatch, current.Boundary.Anchor);
                foreach (var entry in updated.Values)
                    if (entry.Commit.ChangeSet is { } batch) batches = batches.Add(batch.Id, entry.Commit.Id);
                var activeBytes = BoundaryArchive(updated, current.Boundary.Anchor, head.Id, receipts);
                String resolved;
                try
                {
                    resolved = ResolveColdArchivePath(coldArchivePath);
                    using var coldLease = AcquireLease(resolved);
                    using var coldFile = PublishColdArchive(resolved, oldArchive, plan.SourceArchiveETag);
                    Persist(activeBytes);
                }
                catch (Exception exception)
                {
                    result = new(RoamingNetworkRetentionOutcome.PersistenceFailure, head, exception.Message,
                        ColdArchivePath: Path.GetFullPath(coldArchivePath));
                    return false;
                }
                // All fallible preparation and durable writes precede this installation under the gate.
                entries = updated; batchIds = batches; anchorId = current.Boundary.Anchor;
                authorizeSnapshotBoundary = authorizeBoundary; retentionReceipts = receipts; archivedCommits = catalog;
                result = new(RoamingNetworkRetentionOutcome.Pruned, head, Receipt: receipt, ColdArchivePath: resolved);
                return true;
            }
            catch (Exception exception)
            {
                result = new(RoamingNetworkRetentionOutcome.InvalidInput, head, exception.Message);
                return false;
            }
            finally { mutating = false; }
        }
    }

    private static ImmutableDictionary<RoamingNetworkCommitId, RoamingNetworkRetentionReceipt> IndexReceipts(
        ImmutableArray<RoamingNetworkRetentionReceipt> receipts, RoamingNetworkCommitId checkpoint)
    {
        var ids = new HashSet<ETag>();
        var index = ImmutableDictionary.CreateBuilder<RoamingNetworkCommitId, RoamingNetworkRetentionReceipt>();
        foreach (var receipt in receipts)
        {
            if (receipt.Checkpoint != checkpoint || !ids.Add(receipt.Id))
                throw new ArgumentException("Retention receipts must be unique and belong to this chain.");
            foreach (var id in receipt.PrunedCommits) index[id] = receipt;
        }
        return index.ToImmutable();
    }

    private String ResolveColdArchivePath(String path)
    {
        var resolved = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(resolved)!);
        if (File.Exists(resolved) && (File.GetAttributes(resolved) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("A cold archive destination cannot be a symbolic link.");
        var physical = PhysicalArchivePath(resolved);
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var physicalLease = PhysicalArchivePath(resolved + ".lock");
        if (File.Exists(resolved + ".lock") && (File.GetAttributes(resolved + ".lock") & FileAttributes.ReparsePoint) != 0)
            throw new IOException("A cold archive lease cannot be a symbolic link.");
        if (archivePath is not null && (comparer.Equals(physical, PhysicalArchivePath(archivePath)) ||
            comparer.Equals(physical, PhysicalArchivePath(archivePath + ".lock")) ||
            comparer.Equals(physicalLease, PhysicalArchivePath(archivePath)) ||
            comparer.Equals(physicalLease, PhysicalArchivePath(archivePath + ".lock"))))
            throw new IOException("The cold archive must have a separate destination from active persistence and its writer lease.");
        return resolved;
    }

    private static String PhysicalArchivePath(String path)
    {
        static String DirectoryPath(DirectoryInfo directory)
        {
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                return directory.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? throw new IOException("Unresolved archive directory link.");
            return directory.Parent is null ? directory.FullName : Path.Combine(DirectoryPath(directory.Parent), directory.Name);
        }
        return Path.Combine(DirectoryPath(new DirectoryInfo(Path.GetDirectoryName(path)!)), Path.GetFileName(path));
    }

    private static FileStream PublishColdArchive(String path, Byte[] bytes, ETag expected)
    {
        if (!File.Exists(path))
        {
            var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
            try
            {
                using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    file.Write(bytes); file.Flush(flushToDisk: true);
                }
                File.Move(temporary, path, overwrite: false);
            }
            finally
            {
                try { File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
        // Write access permits Flush(true) for a matching preexisting destination as well.
        // The handle excludes other writers and deletion through active archive replacement.
        var retained = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        try
        {
            if (retained.Length != bytes.Length || !SHA256.HashData(retained).AsSpan().SequenceEqual(expected.Digest.AsSpan()))
                throw new IOException("An existing cold archive does not match the complete reviewed source archive.");
            retained.Flush(flushToDisk: true);
            return retained;
        }
        catch { retained.Dispose(); throw; }
    }
}
