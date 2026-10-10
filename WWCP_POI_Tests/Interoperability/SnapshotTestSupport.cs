/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;

namespace WWCP_POI_Tests.Interoperability;

internal static class SnapshotTestSupport
{
    internal static RoamingNetworkCommit Snapshot(RoamingNetworkHistory history, Int32 day = 10)
        => Sign(history.PrepareSnapshot(history.Head.Id, InteropFixture.Time.AddDays(day),
            ImmutableDictionary<String, String>.Empty.Add("de", "Vollständiger Stand").Add("en", "Complete state"),
            ImmutableDictionary<String, JsonElement>.Empty.Add("case", Value("{\"decimal\":1.0,\"exponent\":1e0,\"negativeZero\":-0,\"reading\":\"250 kW\"}"))));

    internal static RoamingNetworkCommit LinearHistory(RoamingNetworkHistory history)
    {
        Store(history, Sign(history.Head.Commit));
        Publish(history, Prepare(history, history.Head.Id, "before-snapshot", Power("150 kW")));
        var snapshot = Snapshot(history); Publish(history, snapshot);
        Publish(history, Prepare(history, history.Head.Id, "after-snapshot", Rename("After snapshot")));
        return snapshot;
    }

    internal static Func<RoamingNetworkSnapshotBoundary, Boolean> AcceptChain(RoamingNetworkHistory history)
    {
        var checkpoint = history.CheckpointId;
        return boundary => boundary.Checkpoint == checkpoint;
    }

    internal static RoamingNetworkHistory Restore(RoamingNetworkHistory source, Boolean cbor)
        => cbor ? RoamingNetworkHistory.ParseCBOR(source.ToCBOR(), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AcceptChain(source)) :
                  RoamingNetworkHistory.Parse(source.ToJSON(), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AcceptChain(source));

    internal static RoamingNetworkCommit CommitTransport(RoamingNetworkCommit commit, Boolean cbor)
        => cbor ? RoamingNetworkCommit.ParseCBOR(commit.ToCBOR()) : RoamingNetworkCommit.Parse(commit.ToJSON());

    internal static RoamingNetworkCommitPack PageTransport(RoamingNetworkCommitPack pack, RoamingNetworkHistory receiver, Boolean cbor)
        => cbor ? RoamingNetworkCommitPack.ParseCBOR(pack.ToCBOR(), resolveAnchor: id => receiver.LookupCommit(id).Commit) :
                  RoamingNetworkCommitPack.Parse(pack.ToJSON(), resolveAnchor: id => receiver.LookupCommit(id).Commit);

    internal static RoamingNetworkRetentionPlan Plan(RoamingNetworkHistory history, RoamingNetworkCommit snapshot,
        IEnumerable<RoamingNetworkCommitId>? released = null, IEnumerable<RoamingNetworkCommitId>? protectedIds = null)
    {
        Assert.That(history.TryPlanRetention(snapshot.Id, InteropFixture.Time.AddDays(20), out var plan, out var result,
            cutoffCommit: snapshot.Id, archiveOnlyTips: released, protectedCommits: protectedIds), Is.True, result.Error);
        return plan!;
    }

    internal static RoamingNetworkRetentionResult Prune(RoamingNetworkHistory history, RoamingNetworkRetentionPlan plan, String path)
    {
        Assert.That(history.TryExecuteRetention(plan, path, AcceptChain(history), out var result, prune: true), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.Pruned));
        return result;
    }

    internal static void Finish(RoamingNetworkBootstrapReceiver receiver, RoamingNetworkBootstrapSource source, Boolean cbor)
    {
        while (receiver.NextChunk < source.Manifest.ChunkCount)
        {
            var chunk = source.CreateChunk(receiver.NextChunk);
            chunk = cbor ? RoamingNetworkBootstrapChunk.ParseCBOR(chunk.ToCBOR()) : RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON());
            Assert.That(receiver.TryAcceptChunk(chunk, out var result), Is.True, result.Error);
        }
    }
}
