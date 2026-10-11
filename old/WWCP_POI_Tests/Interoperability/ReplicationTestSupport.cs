/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

internal static class ReplicationTestSupport
{
    internal static RoamingNetworkChange Rename(String name)
        => RoamingNetworkChange.UpdateProperty("ChargingStation", "DE*ABC*S1", "name", null,
            Value(System.Text.Json.JsonSerializer.Serialize(new { en = name })));

    internal static RoamingNetworkCommit Prepare(RoamingNetworkHistory history, RoamingNetworkCommitId parent,
                                                 String id, params RoamingNetworkChange[] changes)
        => Sign(history.PrepareCommit(parent, Sign(Batch(history.GetSnapshot(parent), id, changes))));

    internal static void Store(RoamingNetworkHistory history, RoamingNetworkCommit commit)
        => Assert.That(history.TryStoreCommit(commit, out var result), Is.True, result.Error);

    internal static void Publish(RoamingNetworkHistory history, RoamingNetworkCommit commit)
        => Assert.That(history.TryPublish(commit.Parents[0], commit, out var result), Is.True, result.Error);

    internal static RoamingNetworkCommit Merge(RoamingNetworkHistory history, RoamingNetworkCommit left, RoamingNetworkCommit right,
                                               String id = "merge")
    {
        Assert.That(history.TryMerge(left.Id, right.Id, out var prepared, out var result, merge: true,
            mergedChangeSetId: id, createdAt: Time.AddDays(1)), Is.True, result.Message);
        return Sign(prepared!.WithChangeSet(Sign(prepared.ChangeSet!)));
    }

    internal static void Status(RoamingNetworkHistory history, POIRuntimeTarget target, String status, Int32 day = 2)
        => history.ApplyRuntimeUpdate(new("interop-network", target, POIRuntimeStatusKind.Status,
            new(status, Time.AddDays(day)), mode: POIRuntimeUpdateMode.ReplaceHistory));

    internal static POIRuntimeTarget EvseTarget => POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1");
    internal static POIRuntimeTarget PoolMeter => POIRuntimeTarget.Meter(InfrastructureEntityType.ChargingPool, "DE*ABC*P1", "pool-meter");
    internal static POIRuntimeTarget StationMeter => POIRuntimeTarget.Meter(InfrastructureEntityType.ChargingStation, "DE*ABC*S1", "station-meter");

    internal static RoamingNetworkCommitPack Transport(RoamingNetworkCommitPack pack, Boolean cbor)
        => cbor ? RoamingNetworkCommitPack.ParseCBOR(pack.ToCBOR()) : RoamingNetworkCommitPack.Parse(pack.ToJSON());

    internal static Int32 Size(RoamingNetworkCommitPack pack)
        => Math.Max(Encoding.UTF8.GetByteCount(pack.ToJSON()), pack.ToCBOR().Length);

    internal sealed class Observation
    {
        private readonly RoamingNetworkHead head;
        private readonly String json;
        private readonly Byte[] cbor;
        private readonly String runtime;

        internal Observation(RoamingNetworkHistory history)
        {
            head = history.Head;
            json = history.ToJSON();
            cbor = history.ToCBOR();
            runtime = head.Network.ToJSONSnapshot().ToString();
        }

        internal void AssertUnchanged(RoamingNetworkHistory history)
        {
            Assert.That(history.Head, Is.SameAs(head));
            Assert.That(history.Head.Network, Is.SameAs(head.Network));
            Assert.That(history.ToJSON(), Is.EqualTo(json));
            Assert.That(history.ToCBOR(), Is.EqualTo(cbor));
            Assert.That(history.Head.Network.ToJSONSnapshot().ToString(), Is.EqualTo(runtime));
        }
    }

    internal sealed class ArchiveDirectory : IDisposable
    {
        internal String DirectoryPath { get; } = Path.Combine(TestContext.CurrentContext.TestDirectory, "replication-test-" + Guid.NewGuid().ToString("N"));
        internal String ArchivePath => Path.Combine(DirectoryPath, "history.cbor");
        internal ArchiveDirectory() => Directory.CreateDirectory(DirectoryPath);

        public void Dispose()
        {
            // Delete only exact leaf files in this test's freshly allocated directory.
            foreach (var file in Directory.EnumerateFiles(DirectoryPath)) File.Delete(file);
            Directory.Delete(DirectoryPath);
        }
    }
}
