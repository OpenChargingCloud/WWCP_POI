/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// An atomically observed commit and network version; only the network's runtime data is mutable.
/// </summary>
public sealed class RoamingNetworkHead
{
    internal RoamingNetworkHead(RoamingNetworkCommit commit, RoamingNetwork network)
    {
        Commit = commit;
        Network = network;
    }

    /// <summary>
    /// The published immutable commit envelope.
    /// </summary>
    public RoamingNetworkCommit Commit { get; }

    /// <summary>
    /// The deterministic published commit identity.
    /// </summary>
    public RoamingNetworkCommitId Id => Commit.Id;

    /// <summary>
    /// The network version with independent mutable runtime schedules.
    /// </summary>
    public RoamingNetwork Network { get; }

    /// <summary>
    /// The immutable static snapshot belonging to this head.
    /// </summary>
    public RoamingNetworkDataSnapshot Snapshot => Network.DataSnapshot;
}

/// <summary>
/// The outcome of retaining or publishing a commit.
/// </summary>
public enum RoamingNetworkHistoryOutcome
{
    Published,
    AlreadyPublished,
    Stored,
    AlreadyStored,
    HeadConflict,
    ChangeSetIdConflict,
    InvalidCommit,
    PersistenceFailure,
    Unavailable
}

/// <summary>
/// A structured outcome carrying the observed head and an optional diagnostic.
/// </summary>
public sealed record RoamingNetworkHistoryResult(RoamingNetworkHistoryOutcome Outcome,
                                                 RoamingNetworkHead Head, String? Error = null);

/// <summary>
/// Retained immutable commit history with atomic expected-head publication and a runtime delivery gate.
/// File-backed instances persist the complete archive before installing a new in-memory head.
/// </summary>
public sealed partial class RoamingNetworkHistory : IDisposable
{
    private sealed record Entry(RoamingNetworkCommit Commit, RoamingNetworkDataSnapshot Snapshot);
    private readonly Object gate = new();
    private ImmutableDictionary<RoamingNetworkCommitId, Entry> entries;
    private ImmutableDictionary<String, RoamingNetworkCommitId> batchIds;
    private RoamingNetworkHead head;
    private readonly RoamingNetworkCommitId checkpointId;
    private readonly Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature;
    private readonly Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature;
    private readonly Func<RoamingNetworkCommit, Boolean>? authorizeCommit;
    private Boolean mutating;
    private Boolean disposed;
    private String? archivePath;
    private FileStream? archiveLease;

    /// <summary>
    /// Create an in-memory history anchored at the network's current version.
    /// Verifiers check every supplied peer; authorization can additionally require keys or quorum.
    /// </summary>
    public RoamingNetworkHistory(RoamingNetwork network,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null)
    {
        ArgumentNullException.ThrowIfNull(network);
        this.verifyBatchSignature = verifyBatchSignature;
        this.verifyCommitSignature = verifyCommitSignature;
        this.authorizeCommit = authorizeCommit;
        var checkpoint = RoamingNetworkCommit.CreateCheckpoint(network.DataSnapshot);
        checkpointId = checkpoint.Id;
        entries = ImmutableDictionary<RoamingNetworkCommitId, Entry>.Empty.Add(checkpoint.Id, new(checkpoint, network.DataSnapshot));
        batchIds = ImmutableDictionary.Create<String, RoamingNetworkCommitId>(StringComparer.Ordinal);
        if (checkpoint.AppliedChangeSetId is { } batchId) batchIds = batchIds.Add(batchId, checkpoint.Id);
        head = new(checkpoint, network);
    }

    /// <summary>
    /// Atomically observe the published commit and its corresponding network.
    /// </summary>
    public RoamingNetworkHead Head { get { lock (gate) return head; } }

    /// <summary>
    /// Return the retained immutable commit envelopes, including unpublished branches.
    /// </summary>
    public ImmutableArray<RoamingNetworkCommit> Commits
    {
        get { lock (gate) return OrderedEntries(entries).Select(entry => entry.Commit).ToImmutableArray(); }
    }

    /// <summary>
    /// Retrieve a retained static state without reconstructing a mutable runtime hierarchy.
    /// </summary>
    public RoamingNetworkDataSnapshot GetSnapshot(RoamingNetworkCommitId id)
    {
        lock (gate) return entries.TryGetValue(id, out var entry) ? entry.Snapshot : throw new KeyNotFoundException("Unknown commit.");
    }

    /// <summary>
    /// Prepare and validate a commit against any retained source, without retaining or publishing it.
    /// </summary>
    public RoamingNetworkCommit PrepareCommit(RoamingNetworkCommitId parentId, RoamingNetworkChangeSet changeSet,
                                               IEnumerable<RoamingNetworkCommitId>? additionalParents = null)
    {
        lock (gate)
        {
            BeginMutation();
            try
            {
                if (!entries.TryGetValue(parentId, out var parent)) throw new KeyNotFoundException("Unknown first parent.");
                var commit = RoamingNetworkCommit.Create(parent.Commit, changeSet, additionalParents);
                ValidateParents(commit);
                parent.Snapshot.ApplyChangeSet(changeSet, verifyBatchSignature);
                return commit;
            }
            finally { mutating = false; }
        }
    }

    /// <summary>
    /// Retain a validated branch without changing the published head.
    /// </summary>
    public Boolean TryStoreCommit(RoamingNetworkCommit commit, out RoamingNetworkHistoryResult result)
        => Mutate(commit, null, false, out result);

    /// <summary>
    /// Publish only against the expected current commit; exact first-parent duplicate delivery is idempotent.
    /// A conflict or validation/persistence failure leaves both retained history and head unchanged.
    /// </summary>
    public Boolean TryPublish(RoamingNetworkCommitId expectedHead, RoamingNetworkCommit commit,
                               out RoamingNetworkHistoryResult result)
        => Mutate(commit, expectedHead, true, out result);

    private Boolean Mutate(RoamingNetworkCommit commit, RoamingNetworkCommitId? expectedHead, Boolean publish,
                           out RoamingNetworkHistoryResult result)
    {
        lock (gate)
        {
            if (disposed || mutating)
            {
                result = new(RoamingNetworkHistoryOutcome.Unavailable, head, "History is disposed or a mutation callback is reentrant.");
                return false;
            }
            mutating = true;
            try
            {
                ArgumentNullException.ThrowIfNull(commit);
                VerifyCommit(commit);
                if (commit.ChangeSet is { } batch && batchIds.TryGetValue(batch.Id, out var priorId) && priorId != commit.Id)
                {
                    result = new(RoamingNetworkHistoryOutcome.ChangeSetIdConflict, head, "The batch ID already identifies different commit content or ancestry.");
                    return false;
                }
                var known = entries.TryGetValue(commit.Id, out var oldEntry);
                var alreadyPublished = publish && known && IsOnFirstParentChain(commit.Id);
                if (publish && !alreadyPublished &&
                    (!expectedHead!.Value.IsValid || expectedHead.Value != head.Id || commit.Parents.IsEmpty || commit.Parents[0] != head.Id))
                {
                    result = new(RoamingNetworkHistoryOutcome.HeadConflict, head, "Expected head or first parent differs from the published head.");
                    return false;
                }
                if (known)
                {
                    commit = MergeEnvelopes(oldEntry!.Commit, commit);
                    VerifyCommit(commit);
                    VerifyBatch(commit);
                }
                else ValidateParents(commit);

                RoamingNetwork? network = null;
                RoamingNetworkDataSnapshot snapshot;
                if (publish && !alreadyPublished)
                {
                    // Capture the latest delivered runtime state while holding the publication gate.
                    network = head.Network.ApplyChangeSet(commit.ChangeSet!, verifyBatchSignature);
                    snapshot = network.DataSnapshot;
                }
                else if (known) snapshot = oldEntry!.Snapshot;
                else snapshot = entries[commit.Parents[0]].Snapshot.ApplyChangeSet(commit.ChangeSet!, verifyBatchSignature);
                MatchState(commit, snapshot);
                var updatedEntries = entries.SetItem(commit.Id, new(commit, snapshot));
                var updatedBatches = commit.ChangeSet is { } storedBatch ? batchIds.SetItem(storedBatch.Id, commit.Id) : batchIds;
                var updatedHead = network is not null ? new RoamingNetworkHead(commit, network) :
                                  head.Id == commit.Id ? new RoamingNetworkHead(commit, head.Network) : head;
                try { Persist(updatedEntries, updatedHead.Id); }
                catch (Exception exception)
                {
                    result = new(RoamingNetworkHistoryOutcome.PersistenceFailure, head, exception.Message);
                    return false;
                }
                entries = updatedEntries;
                batchIds = updatedBatches;
                head = updatedHead;
                result = new(publish ? alreadyPublished ? RoamingNetworkHistoryOutcome.AlreadyPublished : RoamingNetworkHistoryOutcome.Published :
                                      known ? RoamingNetworkHistoryOutcome.AlreadyStored : RoamingNetworkHistoryOutcome.Stored, head);
                return true;
            }
            catch (Exception exception)
            {
                result = new(RoamingNetworkHistoryOutcome.InvalidCommit, head, exception.Message);
                return false;
            }
            finally { mutating = false; }
        }
    }

    /// <summary>
    /// Serialize scoped status delivery with head publication, preserving static commit identity.
    /// </summary>
    public void ApplyRuntimeUpdate(RoamingNetworkRuntimeUpdate update)
    {
        lock (gate)
        {
            BeginMutation();
            try { head.Network.ApplyRuntimeUpdate(update); }
            finally { mutating = false; }
        }
    }

    private void BeginMutation()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (mutating) throw new InvalidOperationException("Mutation callbacks must not mutate history reentrantly.");
        mutating = true;
    }

    private void ValidateParents(RoamingNetworkCommit commit)
        => ValidateParents(commit, entries);

    private static void ValidateParents(RoamingNetworkCommit commit, ImmutableDictionary<RoamingNetworkCommitId, Entry> sourceEntries)
    {
        if (commit.ChangeSet is null || commit.Parents.IsEmpty)
            throw new ArgumentException("A history has exactly one checkpoint; new commits require a batch and known parents.");
        foreach (var id in commit.Parents)
        {
            if (!sourceEntries.TryGetValue(id, out var parent)) throw new ArgumentException($"Missing parent: {id}.");
            if (parent.Commit.RoamingNetworkId != commit.RoamingNetworkId)
                throw new ArgumentException("All parents must belong to the same checkpoint network.");
        }
        var source = sourceEntries[commit.Parents[0]];
        if (source.Commit.Revision != commit.ChangeSet.BaseRevision ||
            !source.Commit.StateETags.SequenceEqual(commit.ChangeSet.BeforeETags))
            throw new ArgumentException("The batch must start at the first parent's revision and state.");
    }

    private void VerifyCommit(RoamingNetworkCommit commit)
    {
        foreach (var signature in commit.Signatures)
            if (verifyCommitSignature is null || !verifyCommitSignature(commit, signature))
                throw new ArgumentException($"Commit signature '{signature.KeyId}' was not accepted.");
        if (authorizeCommit is not null && !authorizeCommit(commit))
            throw new ArgumentException("The application did not authorize this commit.");
    }

    private void VerifyBatch(RoamingNetworkCommit commit)
    {
        if (commit.ChangeSet is not { } batch) return;
        foreach (var signature in batch.Signatures)
            if (verifyBatchSignature is null || !verifyBatchSignature(batch, signature))
                throw new ArgumentException($"Batch signature '{signature.KeyId}' was not accepted.");
    }

    private static RoamingNetworkCommit MergeEnvelopes(RoamingNetworkCommit stored, RoamingNetworkCommit incoming)
    {
        var merged = stored.WithSignatures(stored.Signatures.AddRange(incoming.Signatures.Where(signature => !stored.Signatures.Contains(signature))));
        if (stored.ChangeSet is { } oldBatch && incoming.ChangeSet is { } newBatch)
            merged = merged.WithChangeSet(oldBatch.WithSignatures(oldBatch.Signatures.AddRange(
                newBatch.Signatures.Where(signature => !oldBatch.Signatures.Contains(signature)))));
        return merged;
    }

    private static void MatchState(RoamingNetworkCommit commit, RoamingNetworkDataSnapshot snapshot)
    {
        if (commit.RoamingNetworkId != snapshot.Root.Id || commit.Revision != snapshot.Revision ||
            commit.AppliedChangeSetId != snapshot.AppliedChangeSetId || !commit.StateETags.SequenceEqual(snapshot.ETags))
            throw new ArgumentException("The resulting snapshot differs from the commit header.");
    }

    private Boolean IsOnFirstParentChain(RoamingNetworkCommitId id)
    {
        var current = head.Id;
        while (true)
        {
            if (current == id) return true;
            var parents = entries[current].Commit.Parents;
            if (parents.IsEmpty) return false;
            current = parents[0];
        }
    }

    private static IEnumerable<Entry> OrderedEntries(ImmutableDictionary<RoamingNetworkCommitId, Entry> source)
    {
        var remaining = source.ToDictionary(entry => entry.Key, entry => entry.Value.Commit.Parents.Length);
        var children = source.Keys.ToDictionary(id => id, _ => new List<RoamingNetworkCommitId>());
        foreach (var entry in source.Values)
            foreach (var parent in entry.Commit.Parents) children[parent].Add(entry.Commit.Id);
        var ready = new SortedSet<(String Text, RoamingNetworkCommitId Id)>(
            Comparer<(String Text, RoamingNetworkCommitId Id)>.Create((left, right) => StringComparer.Ordinal.Compare(left.Text, right.Text)));
        foreach (var entry in remaining.Where(entry => entry.Value == 0)) ready.Add((entry.Key.ToString(), entry.Key));
        var count = 0;
        while (ready.Count > 0)
        {
            var current = ready.Min;
            ready.Remove(current);
            yield return source[current.Id];
            count++;
            foreach (var child in children[current.Id])
                if (--remaining[child] == 0) ready.Add((child.ToString(), child));
        }
        if (count != source.Count) throw new InvalidOperationException("History contains a cycle.");
    }

    /// <summary>
    /// Release a persistent archive's writer lease and reject further mutations.
    /// </summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (mutating) throw new InvalidOperationException("A mutation callback must not dispose history.");
            disposed = true;
            archiveLease?.Dispose();
            archiveLease = null;
        }
    }
}
