/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The outcome of previewing or explicitly selecting a retained descendant as head.
/// </summary>
public enum RoamingNetworkHeadAdoptionOutcome
{
    AdoptionAvailable,
    Adopted,
    AlreadyCurrent,
    AlreadyIntegrated,
    HeadConflict,
    UnknownTip,
    Diverged,
    InvalidInput,
    PersistenceFailure,
    Unavailable
}

/// <summary>
/// How a successful head change transfers only the current replica's runtime state.
/// </summary>
public enum RoamingNetworkRuntimeTransfer
{
    None,
    FirstParentReplay,
    ConservativeBranchContinuity
}

/// <summary>
/// The observed head, selected tip, runtime transfer policy and optional diagnostic.
/// Preview and failed adoption leave the head and retained commits unchanged.
/// </summary>
public sealed record RoamingNetworkHeadAdoptionResult(RoamingNetworkHeadAdoptionOutcome Outcome,
    RoamingNetworkHead Head, RoamingNetworkCommitId Target,
    RoamingNetworkRuntimeTransfer RuntimeTransfer = RoamingNetworkRuntimeTransfer.None, String? Error = null);

public sealed partial class RoamingNetworkHistory
{
    /// <summary>
    /// Preview a retained descendant, considering every parent edge; adopt=true explicitly
    /// selects it against expectedHead. Divergence requires a separate explicit merge.
    /// Original commit identity, signatures and revision remain intact; ancestors never rewind head.
    /// </summary>
    public Boolean TryAdoptHead(RoamingNetworkCommitId expectedHead, RoamingNetworkCommitId targetTip,
        out RoamingNetworkHeadAdoptionResult result, Boolean adopt = false)
    {
        lock (gate)
        {
            if (disposed || mutating)
            {
                result = new(RoamingNetworkHeadAdoptionOutcome.Unavailable, head, targetTip,
                    Error: "History is disposed or a mutation callback is reentrant.");
                return false;
            }
            mutating = true;
            try
            {
                if (!targetTip.IsValid || !entries.TryGetValue(targetTip, out var target))
                {
                    result = new(RoamingNetworkHeadAdoptionOutcome.UnknownTip, head, targetTip, Error: "Import the target's complete ancestry first.");
                    return false;
                }
                var targetAncestry = Ancestors(targetTip);
                var localAncestry = Ancestors(head.Id);
                foreach (var id in targetAncestry.Union(localAncestry))
                {
                    VerifyCommit(entries[id].Commit);
                    VerifyBatch(entries[id].Commit);
                }
                if (targetTip == head.Id)
                {
                    result = new(RoamingNetworkHeadAdoptionOutcome.AlreadyCurrent, head, targetTip);
                    return true;
                }
                if (!expectedHead.IsValid || expectedHead != head.Id)
                {
                    result = new(RoamingNetworkHeadAdoptionOutcome.HeadConflict, head, targetTip, Error: "The expected head differs from the published head.");
                    return false;
                }
                if (localAncestry.Contains(targetTip))
                {
                    result = new(RoamingNetworkHeadAdoptionOutcome.AlreadyIntegrated, head, targetTip);
                    return true;
                }
                if (!targetAncestry.Contains(head.Id))
                {
                    result = new(RoamingNetworkHeadAdoptionOutcome.Diverged, head, targetTip,
                        Error: "The tips diverge; preview and explicitly prepare a merge before adoption.");
                    return false;
                }
                var targetLine = FirstParentLine(targetTip);
                var index = targetLine.FindIndex(entry => entry.Commit.Id == head.Id);
                var transfer = index >= 0 ? RoamingNetworkRuntimeTransfer.FirstParentReplay :
                                            RoamingNetworkRuntimeTransfer.ConservativeBranchContinuity;
                if (!adopt)
                {
                    result = new(RoamingNetworkHeadAdoptionOutcome.AdoptionAvailable, head, targetTip, transfer);
                    return true;
                }

                RoamingNetwork network;
                if (index >= 0)
                {
                    network = head.Network;
                    for (var i = index - 1; i >= 0; i--)
                        network = network.ApplyChangeSet(targetLine[i].Commit.ChangeSet!, verifyBatchSignature);
                }
                else
                {
                    var localLine = FirstParentLine(head.Id);
                    var localIds = localLine.Select(entry => entry.Commit.Id).ToHashSet();
                    var common = targetLine.First(entry => localIds.Contains(entry.Commit.Id)).Commit.Id;
                    var lifetimeHistory = localLine.TakeWhile(entry => entry.Commit.Id != common)
                        .Concat(targetLine.TakeWhile(entry => entry.Commit.Id != common))
                        .DistinctBy(entry => entry.Commit.Id)
                        .Select(entry => (entry.Commit.ChangeSet!, entries[entry.Commit.Parents[0]].Snapshot)).ToImmutableArray();
                    // A secondary parent records ancestry, not an applicable batch against the local head.
                    // Transfer independently captured local runtime only across proved first-parent lifetimes.
                    network = head.Network.DeriveRetainedSnapshot(target.Snapshot, lifetimeHistory);
                }
                MatchState(target.Commit, network.DataSnapshot);
                var updatedHead = new RoamingNetworkHead(target.Commit, network);
                try { Persist(entries, updatedHead.Id); }
                catch (Exception exception)
                {
                    result = new(RoamingNetworkHeadAdoptionOutcome.PersistenceFailure, head, targetTip, transfer, exception.Message);
                    return false;
                }
                head = updatedHead;
                result = new(RoamingNetworkHeadAdoptionOutcome.Adopted, head, targetTip, transfer);
                return true;
            }
            catch (Exception exception)
            {
                result = new(RoamingNetworkHeadAdoptionOutcome.InvalidInput, head, targetTip, Error: exception.Message);
                return false;
            }
            finally { mutating = false; }
        }
    }

    private List<Entry> FirstParentLine(RoamingNetworkCommitId tip)
    {
        var result = new List<Entry>();
        while (true)
        {
            var entry = entries[tip];
            result.Add(entry);
            if (entry.Commit.Parents.IsEmpty) return result;
            tip = entry.Commit.Parents[0];
        }
    }
}
