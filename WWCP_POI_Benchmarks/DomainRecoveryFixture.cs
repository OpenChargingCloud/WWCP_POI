/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

internal sealed record DomainInventory(Int32 Pools, Int32 Stations, Int32 EVSEs, Int32 GridOperators,
    Int32 SoftwareReleases, Int32 Certificates, Int32 MeterSlots, Int32 SoftwareAssignments,
    Int32 Tariffs, Int32 TariffElements, Int32 PriceComponents, Int32 Restrictions,
    Int32 ParkingOperators, Int32 ParkingGarages, Int32 ParkingSpaces, Int32 ParkingGroups, Int32 ParkingProducts,
    Int32 ReferenceEdges, Int32 MergeCommits, Int32 ElementOperations, String BranchStateIdentity);

/// <summary>
/// Deterministic shared-reference, tariff and parking data with signed edits and an explicit merge.
/// This source is compiled into the benchmark and the contract tests; it uses only production APIs.
/// </summary>
internal sealed class DomainRecoveryFixture : IDisposable
{
    internal static readonly DateTimeOffset Epoch = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture);
    // Published, reproducible seeds. Neither key establishes production authority.
    private static readonly Ed25519PrivateKeyParameters[] Keys = [
        new(Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0),
        new(Convert.FromHexString("1f1e1d1c1b1a191817161514131211100f0e0d0c0b0a09080706050403020100"), 0)];
    private static readonly Ed25519PublicKeyParameters[] PublicKeys = Keys.Select(key => key.GeneratePublicKey()).ToArray();
    private readonly String directory = Path.Combine(Path.GetTempPath(), "wwcp-poi-domain-" + Guid.NewGuid().ToString("N"));
    private Int32 ordinal;
    internal RoamingNetworkHistory History { get; private set; } = null!;
    internal RoamingNetworkCommitId Checkpoint { get; }
    internal DomainInventory Inventory { get; }
    internal Int32 Profile { get; }

    internal static Boolean VerifyBatch(RoamingNetworkChangeSet batch, RoamingNetworkChangeSetSignature peer)
        => KeyIndex(peer.KeyId) is var index && index >= 0 && batch.VerifySignature(peer, PublicKeys[index], peer.KeyId, out _);
    internal static Boolean VerifyCommit(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer)
        => KeyIndex(peer.KeyId) is var index && index >= 0 && commit.VerifySignature(peer, PublicKeys[index], peer.KeyId, out _);
    private static Int32 KeyIndex(String id) => id == "domain-a" ? 0 : id == "domain-b" ? 1 : -1;
    internal static RoamingNetworkChangeSet Sign(RoamingNetworkChangeSet value)
        => value.Sign(Keys[0], "domain-a", COSEAlgorithm.Ed25519).Sign(Keys[1], "domain-b", COSEAlgorithm.Ed25519);
    internal static RoamingNetworkCommit Sign(RoamingNetworkCommit value)
        => value.Sign(Keys[0], "domain-a", COSEAlgorithm.Ed25519).Sign(Keys[1], "domain-b", COSEAlgorithm.Ed25519);
    internal Boolean Boundary(RoamingNetworkSnapshotBoundary value) => value.Checkpoint == Checkpoint;
    private DateTimeOffset At() => Epoch.AddSeconds(++ordinal);

    internal DomainRecoveryFixture(Int32 evses, Int32 profile)
    {
        if (evses < 8 || evses % 8 != 0) throw new ArgumentOutOfRangeException(nameof(evses));
        if (profile is < 1 or > 4) throw new ArgumentOutOfRangeException(nameof(profile));
        Profile = profile; Directory.CreateDirectory(directory);
        try
        {
            History = new(CreateNetwork(evses), VerifyBatch, VerifyCommit); Checkpoint = History.CheckpointId;
            Require(History.TryStoreCommit(Sign(History.Head.Commit), out var stored), stored.Error);
            ApplyEdits();
            if (profile is 2 or 3) PublishSnapshot();
            if (profile == 3)
            {
                var complete = History;
                History = RoamingNetworkHistory.FromSnapshot(Checkpoint, complete.Head.Commit, Boundary, VerifyCommit, VerifyBatch);
                complete.Dispose();
            }
            if (profile == 4)
            {
                for (var round = 0; round < 2; round++)
                {
                    var anchor = PublishSnapshot();
                    Publish(Edit("TransparencySoftwareCertificate", "approval-1", "documentNumber", Text("prefix-" + round)));
                    Require(History.TryPlanRetention(anchor.Id, At(), out var plan, out var review, cutoffCommit: anchor.Id), review.Error);
                    Require(History.TryExecuteRetention(plan!, Path.Combine(directory, $"cold-{round}.cbor"), Boundary,
                        out var retained, prune: true), retained.Error);
                }
            }
            if (profile != 1) ApplyEdits();
            var parent = History.Head.Id;
            var left = Prepare(parent, Edit("TransparencySoftware", "verifier-1", "vendor", Text("Merged vendor")));
            var right = Prepare(parent, Edit("ParkingProduct", "short-stay", "minDuration", Text("900 s")));
            Require(History.TryPublish(parent, left, out var published), published.Error);
            Require(History.TryStoreCommit(right, out stored), stored.Error);
            Require(History.TryMerge(left.Id, right.Id, out _, out var preview), preview.Message);
            Require(preview.Status == RoamingNetworkMergeStatus.MergeAvailable, "Expected a disjoint merge preview.");
            Require(History.TryMerge(left.Id, right.Id, out var merge, out var merged, merge: true,
                mergedChangeSetId: "domain-merge", createdAt: At()), merged.Message);
            var signed = Sign(merge!.WithChangeSet(Sign(merge.ChangeSet!)));
            Require(History.TryPublish(left.Id, signed, out published), published.Error);
            CheckGraph(History.Head.Network);
            Inventory = Describe(History);
            // Operational changes are local and must disappear from static recovery.
            Runtime(POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"), "charging");
            Runtime(POIRuntimeTarget.GridOperator("DE*GRD"), "Offline");
            Runtime(POIRuntimeTarget.Entity(InfrastructureEntityType.ParkingOperator, "parking-op"), "Offline");
            Runtime(POIRuntimeTarget.Meter(InfrastructureEntityType.ChargingPool, "DE*ABC*P1", "pool-grid-1"), "error");
        }
        catch { Dispose(); throw; }
    }

    internal RoamingNetworkCommit Prepare(RoamingNetworkCommitId parent, params RoamingNetworkChange[] changes)
    {
        var timestamp = At(); var batch = Sign(History.GetSnapshot(parent).CreateChangeSet("domain-" + ordinal, timestamp, [.. changes]));
        return Sign(History.PrepareCommit(parent, batch));
    }
    private void Publish(params RoamingNetworkChange[] changes)
    {
        var parent = History.Head.Id; var commit = Prepare(parent, changes);
        Require(History.TryPublish(parent, commit, out var result), result.Error);
    }
    private RoamingNetworkCommit PublishSnapshot()
    {
        var parent = History.Head.Id; var snapshot = Sign(History.PrepareSnapshot(parent, At()));
        Require(History.TryPublish(parent, snapshot, out var result), result.Error); return snapshot;
    }
    private void Runtime(POIRuntimeTarget target, String value)
        => History.ApplyRuntimeUpdate(new("benchmark-network", target, POIRuntimeStatusKind.Status,
            new(value, Epoch.AddMonths(5)), mode: POIRuntimeUpdateMode.ReplaceHistory));

    private void ApplyEdits()
    {
        Publish(Edit("GridOperator", "DE*GRD", "name", Json(new JObject { ["en"] = "Grid " + ordinal, ["de"] = "Netz" })));
        Publish(Edit("TransparencySoftware", "verifier-1", "vendor", Text("Vendor " + ordinal)));
        Publish(Edit("TransparencySoftwareCertificate", "approval-1", "documentNumber", Text("Document " + ordinal)));
        var point = JToken.Parse(History.Head.Snapshot.GetEntity(InfrastructureEntityType.ChargingPool, "DE*ABC*P1")
            .Properties["gridConnectionPoint"].GetRawText());
        point["gridOperatorId"] = point["gridOperatorId"]!.Value<String>() == "DE*GRD" ? "DE*ALT" : "DE*GRD";
        Publish(Edit("ChargingPool", "DE*ABC*P1", "gridConnectionPoint", Json(point)));
        var tariff = History.Head.Snapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["tariffIds"]
            .EnumerateArray().First(value => value.GetString() is "DE*ABC*T1" or "DE*ABC*T3").GetString()!;
        var next = tariff == "DE*ABC*T1" ? "DE*ABC*T3" : "DE*ABC*T1";
        Publish(RoamingNetworkChange.RemoveElement("EVSE", "DE*ABC*E1", [new("tariffIds", tariff)]),
            RoamingNetworkChange.AddElement("EVSE", "DE*ABC*E1", [new("tariffIds", next)], Text(next)));
        Publish(Edit("ChargingTariff", "DE*ABC*T1", "elements", Json(TariffElements(ordinal))));
        var hasThirdSpace = History.Head.Snapshot.GetEntity(InfrastructureEntityType.ParkingSpaceGroup, "group-1")
            .Properties["parkingSpaceIds"].EnumerateArray().Any(value => value.GetString() == "space-3");
        var add = hasThirdSpace ? "space-1" : "space-3"; var remove = hasThirdSpace ? "space-3" : "space-1";
        Publish(RoamingNetworkChange.AddElement("ParkingSpaceGroup", "group-1", [new("parkingSpaceIds", add)], Text(add)),
            RoamingNetworkChange.RemoveElement("ParkingSpaceGroup", "group-1", [new("parkingSpaceIds", remove)]));
    }

    internal static JsonElement Text(String text) => JsonSerializer.SerializeToElement(text);
    internal static JsonElement Json(JToken value) => JsonSerializer.Deserialize<JsonElement>(value.ToString(Newtonsoft.Json.Formatting.None));
    internal static RoamingNetworkChange Edit(String type, String id, String property, JsonElement value)
        => RoamingNetworkChange.UpdateProperty(type, id, property, null, value);
    private static JObject Node(String id) => new() { ["@id"] = id, ["name"] = new JObject { ["en"] = "Domain benchmark", ["de"] = "Messnetz" },
        ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" };
    private static JObject Meter(String id, String role, Int32 certificate)
        => new() { ["id"] = id, ["role"] = role, ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z",
            ["transparencySoftware"] = new JArray(Enumerable.Range(certificate * 2 - 1, 2).Select(release => new JObject {
                ["transparencySoftwareId"] = "verifier-" + release, ["legalStatus"] = release % 2 == 1 ? "verified" : "compatible",
                ["certificateId"] = "approval-" + certificate })) };

    internal static JArray TariffElements(Int32 ordinal)
        => new(Enumerable.Range(0, 2).Select(index => new JObject {
            ["priceComponents"] = new JArray(
                new JObject { ["type"] = "ENERGY", ["price"] = 0.456789123456789m + ordinal / 10000m, ["stepSize"] = "1 Wh" },
                new JObject { ["type"] = "TIME", ["price"] = 1.234500m, ["stepSize"] = "300 s" },
                new JObject { ["type"] = "FLAT", ["price"] = 2.50m }),
            ["restrictions"] = new JArray(new JObject { ["startTime"] = index == 0 ? "08:00" : "20:00",
                ["endTime"] = index == 0 ? "18:00" : "23:00", ["minEnergy"] = "0.1234567 kWh", ["maxEnergy"] = "42.9876543 kWh",
                ["minPower"] = "0 W", ["maxPower"] = "350.123 kW", ["minDuration"] = "0.0000001 s", ["maxDuration"] = "3600.25 s",
                ["daysOfWeek"] = new JArray("MONDAY", "FRIDAY") },
                new JObject { ["startDate"] = "2026-01-01T00:00:00Z", ["endDate"] = "2027-01-01T00:00:00Z", ["maxEnergy"] = "100 kWh" }) }));

    internal static RoamingNetwork CreateNetwork(Int32 evses) => RoamingNetwork.Parse(CreateDocument(evses));
    internal static JObject CreateDocument(Int32 evses)
    {
        var root = Node("benchmark-network");
        root["gridOperators"] = new JArray(new[] { "DE*GRD", "DE*ALT" }.Select(id => { var item = Node(id); item["id"] = id; item.Remove("@id"); return item; }));
        root["transparencySoftware"] = new JArray(Enumerable.Range(1, 4).Select(index => new JObject {
            ["@id"] = "verifier-" + index, ["name"] = new JObject { ["en"] = "Meter verifier" }, ["version"] = index + ".0", ["vendor"] = "Domain vendor",
            ["openSourceLicenses"] = new JArray(new JObject { ["@id"] = "MIT", ["description"] = new JObject { ["en"] = "MIT License" } }) }));
        root["transparencySoftwareCertificates"] = new JArray(Enumerable.Range(1, 2).Select(index => new JObject {
            ["@id"] = "approval-" + index, ["issuer"] = "Example inspection organization", ["chargingStationModel"] = "Station X",
            ["chargingStationModelVersion"] = "3.1", ["verifiedTransparencySoftwareIds"] = new JArray("verifier-" + (index * 2 - 1)),
            ["compatibleTransparencySoftwareIds"] = new JArray("verifier-" + index * 2), ["documentNumber"] = "42/2026",
            ["notBefore"] = "2026-01-01T00:00:00.1234567Z", ["notAfter"] = "2030-01-01T00:00:00Z" }));
        var pools = new JArray();
        for (var offset = 0; offset < evses; offset += 8)
        {
            var number = offset / 8 + 1; var pool = Node("DE*ABC*P" + number);
            pool["energyMeters"] = new JArray(Meter("pool-grid-" + number, "grid", 1), Meter("pool-pv-" + number, "pv", 2));
            pool["gridConnectionPoint"] = new JObject { ["gridOperatorId"] = "DE*GRD", ["nominalVoltage"] = "400 V",
                ["nominalFrequency"] = "50 Hz", ["contractedImportPower"] = "250 kW", ["contractedImportApparentPower"] = "300 kVA",
                ["energyMeter"] = Meter("point-" + number, "grid", 1) };
            var stations = new JArray();
            for (var start = offset; start < offset + 8; start += 4)
            {
                var station = Node("DE*ABC*S" + (start / 4 + 1));
                station["energyMeters"] = new JArray(Meter("station-grid-" + start, "grid", 1), Meter("station-pv-" + start, "pv", 2));
                station["EVSEs"] = new JArray(Enumerable.Range(start + 1, 4).Select(index => {
                    var evse = Node("DE*ABC*E" + index); evse["currentType"] = new JArray("DC"); evse["maxPower"] = "100 kW";
                    evse["tariffIds"] = new JArray("DE*ABC*T1", "DE*ABC*T2"); evse["energyMeter"] = Meter("evse-" + index, "grid", 1);
                    evse["socketOutlets"] = new JArray(new JObject { ["@id"] = "1", ["type"] = "CCS", ["lockable"] = false,
                        ["tariffIds"] = new JArray("DE*ABC*T2", "DE*ABC*T4") }); return evse;
                })); stations.Add(station);
            }
            pool["chargingStations"] = stations; pools.Add(pool);
        }
        var owner = Node("DE*ABC"); owner["chargingPools"] = pools;
        owner["chargingTariffs"] = new JArray(Enumerable.Range(1, 4).Select(index => { var tariff = Node("DE*ABC*T" + index);
            tariff["currency"] = "EUR"; tariff["elements"] = TariffElements(index); return tariff; }));
        root["chargingStationOperators"] = new JArray(owner);
        var parking = Node("parking-op"); parking["id"] = "parking-op"; parking.Remove("@id");
        parking["parkingProducts"] = new JArray(new JObject { ["@id"] = "short-stay", ["minDuration"] = "300 s", ["stopParkingAfterTime"] = "7200 s" },
            new JObject { ["@id"] = "long-stay", ["minDuration"] = "600 s", ["stopParkingAfterTime"] = "28800 s" });
        parking["parkingGarages"] = new JArray(Enumerable.Range(1, evses / 8).Select(index => {
            var garage = Node("garage-" + index); garage["parkingProductIds"] = new JArray("short-stay");
            garage["chargingStationIds"] = new JArray("DE*ABC*S" + (index * 2 - 1)); return garage; }));
        parking["parkingSpaces"] = new JArray(Enumerable.Range(1, evses).Select(index => {
            var space = Node("space-" + index); space["parkingGarageId"] = "garage-" + ((index - 1) / 8 + 1);
            space["parkingProductIds"] = new JArray("long-stay"); space["chargingStationIds"] = new JArray("DE*ABC*S" + ((index - 1) / 4 + 1)); return space; }));
        parking["parkingSpaceGroups"] = new JArray(Enumerable.Range(1, 2).Select(index => {
            var group = Node("group-" + index); group["parkingSpaceIds"] = new JArray("space-1", "space-2");
            group["parkingProductIds"] = new JArray(index == 1 ? "short-stay" : "long-stay"); return group; }));
        root["parkingOperators"] = new JArray(parking); return root;
    }

    internal static IEnumerable<EnergyMeter> Meters(RoamingNetwork value)
        => value.ChargingPools.SelectMany(pool => pool.EnergyMeters).Concat(value.ChargingStations.SelectMany(station => station.EnergyMeters))
            .Concat(value.EVSEs.Select(evse => evse.EnergyMeter!)).Concat(value.ChargingPools.Select(pool => pool.GridConnectionPoint!.EnergyMeter!));
    internal static void CheckGraph(RoamingNetwork value)
    {
        foreach (var pool in value.ChargingPools)
            Require(ReferenceEquals(pool.GridConnectionPoint!.GridOperator, value.GridOperators.Single(grid => grid.Id == pool.GridConnectionPoint.GridOperator.Id)), "Grid registry instance duplicated.");
        foreach (var assignment in Meters(value).SelectMany(meter => meter.TransparencySoftware))
        {
            Require(ReferenceEquals(assignment.TransparencySoftware, value.GetTransparencySoftwareById(assignment.TransparencySoftwareId)), "Software registry instance duplicated.");
            Require(ReferenceEquals(assignment.Certificate, value.GetTransparencySoftwareCertificateById(assignment.CertificateId!.Value)), "Certificate registry instance duplicated.");
        }
        foreach (var parking in value.ParkingOperators)
        foreach (var space in parking.ParkingSpaces)
            Require(space.ChargingStations.All(station => ReferenceEquals(station, value.ChargingStations.Single(other => other.Id == station.Id))), "Parking station reference duplicated.");
        Require(value.ParkingOperators.Single().GetAvailableParkingProducts(ParkingSpace_Id.Parse("space-2")).Select(product => product.Id.ToString())
            .SequenceEqual(new[] { "long-stay", "short-stay" }), "Parking product union changed.");
    }
    internal void CheckRecovered(RoamingNetworkHistory restored)
    {
        CheckGraph(restored.Head.Network);
        Require(Describe(restored) == Inventory, "Domain or branch inventory changed.");
        Require(restored.Head.Network.GridOperators.Single(value => value.Id.ToString() == "DE*GRD").Status.Value != GridOperatorStatusTypes.Offline,
            "Local grid status leaked into recovery.");
        Require(restored.Head.Network.ParkingOperators.Single().Status.Value != ParkingOperatorStatusTypes.Offline, "Local parking status leaked into recovery.");
        Require(restored.Head.Network.ChargingPools.First(pool => pool.Id.ToString() == "DE*ABC*P1").EnergyMeters[0].Status.Value.ToString() != "error", "Local meter status leaked into recovery.");
        Require(!ReferenceEquals(restored.Head.Network.GridOperators.First(), History.Head.Network.GridOperators.First()), "Independent runtime shared a grid object.");
    }
    internal static DomainInventory Describe(RoamingNetworkHistory history)
    {
        var network = history.Head.Network; var tariffs = network.ChargingStationOperators.SelectMany(value => value.ChargingTariffs).ToArray();
        var elements = tariffs.SelectMany(tariff => tariff.TariffElements).ToArray(); var meters = Meters(network).ToArray();
        return new(network.ChargingPools.Count(), network.ChargingStations.Count(), network.EVSEs.Count(), network.GridOperators.Count(),
            network.TransparencySoftware.Length, network.TransparencySoftwareCertificates.Length, meters.Length, meters.Sum(meter => meter.TransparencySoftware.Count()),
            tariffs.Length, elements.Length, elements.Sum(element => element.ChargingPriceComponents.Count()), elements.Sum(element => element.ChargingTariffRestrictions.Count()),
            network.ParkingOperators.Count(), network.ParkingGarages.Length, network.ParkingSpaces.Length, network.ParkingSpaceGroups.Length, network.ParkingProducts.Length,
            history.Head.Snapshot.References.Sum(pair => pair.Value.Count), history.Commits.Count(commit => commit.Parents.Length > 1),
            history.Commits.Sum(commit => commit.ChangeSet?.Changes.Count(change => change.Kind is RoamingNetworkChangeKind.AddElement or RoamingNetworkChangeKind.RemoveElement) ?? 0),
            BranchIdentity(history));
    }
    internal static String BranchIdentity(RoamingNetworkHistory history)
    {
        using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        foreach (var commit in history.Commits.OrderBy(commit => commit.Id.ToString(), StringComparer.Ordinal))
        {
            writer.Write(commit.Id.ToString());
            foreach (var etag in history.GetSnapshot(commit.Id).ETags) writer.Write(etag.ToString());
        }
        writer.Flush(); return "branches:sha256:hex:" + Convert.ToHexStringLower(SHA256.HashData(stream.GetBuffer().AsSpan(0, (Int32)stream.Length)));
    }
    private static void Require(Boolean condition, String? message)
    { if (!condition) throw new InvalidOperationException(message ?? "Domain fixture contract failed."); }
    public void Dispose()
    {
        History?.Dispose();
        foreach (var file in Directory.EnumerateFiles(directory)) File.Delete(file);
        Directory.Delete(directory);
    }
}
