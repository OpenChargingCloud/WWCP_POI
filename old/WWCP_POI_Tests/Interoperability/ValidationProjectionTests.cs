/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WWCP_POI_Benchmarks;
using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ValidationProjectionTests
{
    [TestCase("DE*ABC*TG1")] [TestCase("DEABC*TGtariff_1")] [TestCase("DE*ABC*TGoffer.€")]
    public void Tariff_group_identifiers_emit_parseable_text_and_report_the_emitted_length(String text)
    {
        var id = ChargingTariffGroup_Id.Parse(text);
        Assert.That(ChargingTariffGroup_Id.Parse(id.ToString()), Is.EqualTo(id));
        Assert.That(id.Length, Is.EqualTo((UInt64)id.ToString().Length));
        Assert.That(id.IsNullOrEmpty, Is.False); Assert.That(id.IsNotNullOrEmpty, Is.True);
        Assert.That(default(ChargingTariffGroup_Id).IsNullOrEmpty, Is.True);
        Assert.That(default(ChargingTariffGroup_Id).IsNotNullOrEmpty, Is.False);
    }

    [TestCase("DEABC", "DE*ABC")] [TestCase("FRXYZ", "FR*XYZ")]
    public void Equal_tariff_group_identifiers_keep_equal_ordering_across_operator_text_formats(String compact, String starred)
    {
        var left = ChargingTariffGroup_Id.Parse(compact + "*TGoffer");
        var right = ChargingTariffGroup_Id.Parse(starred + "*TGoffer");
        Assert.That(left, Is.EqualTo(right)); Assert.That(left.GetHashCode(), Is.EqualTo(right.GetHashCode()));
        Assert.That(left.CompareTo(right), Is.Zero); Assert.That(right.CompareTo(left), Is.Zero);
        Assert.That(new SortedSet<ChargingTariffGroup_Id> { left, right }, Has.Count.EqualTo(1));
        Assert.That(left.Length, Is.EqualTo((UInt64)left.ToString().Length));
        Assert.That(right.Length, Is.EqualTo((UInt64)right.ToString().Length));
    }

    [Test]
    public void All_group_kinds_roundtrip_as_owned_graph_nodes_in_JSON_and_CBOR()
    {
        var source = Network(groups: true);
        foreach (var restored in new[] { RoamingNetwork.Parse(source.DataSnapshot.ToJSON()), RoamingNetwork.ParseCBOR(source.ToCBOR(IncludeVersionMetadata: true)) })
        {
            Assert.That(restored.ToCanonicalJSON(), Is.EqualTo(source.ToCanonicalJSON()));
            Assert.That(restored.ToCanonicalCBOR(), Is.EqualTo(source.ToCanonicalCBOR()));
            Assert.That(restored.ETags, Is.EqualTo(source.ETags));
            Assert.That(restored.ChargingStationOperators.Single().ChargingTariffGroups.Single().Id.ToString(), Is.EqualTo("DE*ABC*TG1"));
        }
    }

    [TestCase(16, 1, "cbor:sha256:hex:808decb0be828c2ba159b09dd18c59fba7d6b5f91d6a54e03e490677767a81bb", "json:sha256:hex:6dfba000eddce193d6134f61a4bae9c0dbb07dd7c4f68cde7d5b8e7ead0de0be|cbor:sha256:hex:96047a401f47c2d6265e3556d5adb10dbb422a016073dfbac70eea919c929344", "branches:sha256:hex:592884793e2f00f54f5610c0f3b8a605d8b7d1e70cf99cbf169e1f512fab13eb")]
    [TestCase(16, 2, "cbor:sha256:hex:e76c7f46bd6b742e479ef30d567604adfe77aa95601ea1415bc6a9c4713c3a11", "json:sha256:hex:b5d9ff32a8b315e8aa06e2b74e32104a5d4b7869207b3bd17b8f5e2b191a7308|cbor:sha256:hex:f9b58f640e2ac3d2dbedb567c3a9b267617582c81afe6a38ec91c0693da733b6", "branches:sha256:hex:b3722dd3c00f5aa627d90b5046c1bd034d2d771897b9fff4fa6e28732ef89480")]
    [TestCase(16, 3, "cbor:sha256:hex:f68b3b7cba448d3924b381939cf286018fc4d50cd97ac711cba183a4856fefcc", "json:sha256:hex:b5d9ff32a8b315e8aa06e2b74e32104a5d4b7869207b3bd17b8f5e2b191a7308|cbor:sha256:hex:f9b58f640e2ac3d2dbedb567c3a9b267617582c81afe6a38ec91c0693da733b6", "branches:sha256:hex:75439a293e1d519c495fa6898dd6059d48cf5e134290a09179f94203d706f581")]
    [TestCase(16, 4, "cbor:sha256:hex:d5f662280621630f8c225e700cfb10855fbe55aca56e4c8c33b0f0536e690536", "json:sha256:hex:0f2abe58f345ed5eb52c2df184fdd40e5080a35caae9d6e512254b09ec63e75a|cbor:sha256:hex:d3e06059fba204b9882ac566c8295cd90fa584440987fc3ba19c8add843317f2", "branches:sha256:hex:c2fc6af3e1a421d5b4ab8612debf8a82975f84a90f8943a67bb97e37c9d72cbe")]
    [TestCase(64, 1, "cbor:sha256:hex:809adaf1b42be8f83cf91a6954396477e45567cbadebe31cfa0a54ad19d31f1e", "json:sha256:hex:e09bb1d00a1a68bf30787a06028455b92a2c97191194b98e23afb08f4dd27768|cbor:sha256:hex:0e12d20331a968bd4c4707fb7b59e459daeaf23a720e6b1f34ed388b681d1c31", "branches:sha256:hex:6f0ce0fd06cb2aeaf3f41216195391ff4abadb464455de89ba14cdd28bc851cc")]
    [TestCase(64, 2, "cbor:sha256:hex:179e04bac64e63fd5b33c9ebcca5f8a983d59021dccbbb67c0a1a18f83c07641", "json:sha256:hex:63aada293b899ab6928ae4dbffdab080f7cd2ddd7e3f8e266c2ca43f4e015799|cbor:sha256:hex:a7798c8c30d490a23d8d2df528559a188fca2d6aefce32564a276a88adcd588a", "branches:sha256:hex:97f558f5eda59584fa996966306c291e5c3c10b28cd1506300137d3c02841792")]
    [TestCase(64, 3, "cbor:sha256:hex:2947aed9bc8bb3c1586435707f9c9cd6d8f1affb2c51093ff27c0656466e3a8f", "json:sha256:hex:63aada293b899ab6928ae4dbffdab080f7cd2ddd7e3f8e266c2ca43f4e015799|cbor:sha256:hex:a7798c8c30d490a23d8d2df528559a188fca2d6aefce32564a276a88adcd588a", "branches:sha256:hex:2333176864a0657df9d95972ac5c8ea13767873de3b3e9559aaf876a0a1a9648")]
    [TestCase(64, 4, "cbor:sha256:hex:7701eab6b0f1642d4feddfa1edb36f0f07e83af4530eb7eff674369cc2535888", "json:sha256:hex:13e745a4be905fbbae2c34a24e17305dda9212c4b4f53f560c19c45ac436f61f|cbor:sha256:hex:225ae2ade76af7a1a10a4522d307845e5807e0d0023c2a1b667e0b8f309be14f", "branches:sha256:hex:1317ccd848fb3e2ebf38599328db9cbf104aa226b4166114241677392d53a6e1")]
    public void Rich_archives_keep_the_exact_preceding_bytes_states_and_original_peers(
        Int32 evses, Int32 profile, String archive, String state, String branches)
    {
        using var fixture = new DomainRecoveryFixture(evses, profile);
        Assert.That(ETag.Compute(ETagFormat.CBOR, fixture.History.ToCBOR()).ToString(), Is.EqualTo(archive));
        Assert.That(String.Join("|", fixture.History.Head.Snapshot.ETags), Is.EqualTo(state));
        Assert.That(fixture.Inventory.BranchStateIdentity, Is.EqualTo(branches));
        using var restored = RoamingNetworkHistory.ParseCBOR(fixture.History.ToCBOR(), DomainRecoveryFixture.VerifyBatch,
            DomainRecoveryFixture.VerifyCommit, authorizeSnapshotBoundary: fixture.Boundary);
        fixture.CheckRecovered(restored);
    }

    private sealed class Pass(EntityMap map)
    {
        private static readonly Type Type = typeof(RoamingNetworkDataSnapshot).GetNestedType("ValidationProjection", BindingFlags.NonPublic)!;
        private readonly Object instance = Activator.CreateInstance(Type, BindingFlags.Instance | BindingFlags.NonPublic,
            null, [map], null)!;
        internal Object Project(InfrastructureEntityKey key)
        {
            try { return Type.GetMethod("Project", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(instance, [key])!; }
            catch (TargetInvocationException error) { ExceptionDispatchInfo.Capture(error.InnerException!).Throw(); throw; }
        }
        internal IDictionary Cached => (IDictionary) Type.GetField("projected", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
        internal ChargingStation[]? Stations => (ChargingStation[]?) Type.GetField("stations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance);
    }
    private static readonly Action<IEnumerable<InfrastructureEntityKey>, EntityMap> ValidateMany =
        typeof(RoamingNetworkDataSnapshot).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
            .Single(method => method.Name == "Validate" && method.GetParameters()[0].ParameterType == typeof(IEnumerable<InfrastructureEntityKey>))
            .CreateDelegate<Action<IEnumerable<InfrastructureEntityKey>, EntityMap>>();
    private static InfrastructureEntityKey Key(EntityMap map, InfrastructureEntityType type)
        => map.Keys.Where(key => key.Type == type).OrderBy(key => key.Id, StringComparer.Ordinal).First();
    private static String Document(Object value)
    {
        // Transparency software and its documents are values of the network, no POI documents.
        var document = value switch {
            TransparencySoftware software => software.ToJSON(),
            TransparencySoftwareCertificate certificate => certificate.ToJSON(),
            _ => POIRepresentation.WithoutETags(() => POIJSON.Document((IImmutablePOI)value))
        };
        POIRepresentation.RemoveRuntime(document, value.GetType().Name);
        return document.ToString(Newtonsoft.Json.Formatting.None);
    }
    private static EntityMap Edit(EntityMap map, InfrastructureEntityKey key, String property, String raw)
        => map.SetItem(key, map[key].With(properties: map[key].Properties.SetItem(property, JsonSerializer.Deserialize<JsonElement>(raw))));
    private static RoamingNetwork Network(Int32 evses = 16, Boolean groups = false)
    {
        var document = DomainRecoveryFixture.CreateDocument(evses);
        if (groups)
        {
            var owner = document["chargingStationOperators"]![0]!;
            foreach (var (field, memberField, member, id) in new[] {
                ("EVSEGroups", "EVSEIds", "DE*ABC*E1", "DE*ABC*EG1"), ("chargingStationGroups", "chargingStationIds", "DE*ABC*S1", "DE*ABC*SG1"),
                ("chargingPoolGroups", "chargingPoolIds", "DE*ABC*P1", "DE*ABC*PG1"), ("chargingTariffGroups", "chargingTariffIds", "DE*ABC*T1", "DE*ABC*TG1") })
            {
                var group = new JObject { ["@id"] = id, [memberField] = new JArray(member),
                    ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" };
                if (field != "chargingTariffGroups") group["name"] = new JObject { ["en"] = "Validation group" };
                owner[field] = new JArray(group);
            }
            document["eMobilityProviders"] = new JArray(new JObject { ["@id"] = "DE*EMP", ["name"] = new JObject { ["en"] = "Validation provider" },
                ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" });
            document["chargingStationManufacturers"] = new JArray(new JObject { ["@id"] = "maker", ["name"] = new JObject { ["en"] = "Validation manufacturer" } });
            document["parkingOperators"]![0]!["parkingSensors"] = new JArray(new JObject { ["@id"] = "sensor", ["chargingStationIds"] = new JArray("DE*ABC*S1"),
                ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" });
        }
        return RoamingNetwork.Parse(document);
    }

    public static IEnumerable<TestCaseData> Kinds()
    {
        foreach (var type in Network(groups: true).DataSnapshot.Entities.Keys.Select(key => key.Type).Distinct().Order())
            yield return new TestCaseData(type);
    }

    [TestCaseSource(nameof(Kinds))]
    public void Each_domain_projection_preserves_the_frozen_model_and_successful_identity(InfrastructureEntityType type)
    {
        var source = Network(groups: true); var map = source.DataSnapshot.Entities; var key = Key(map, type); var pass = new Pass(map);
        var expected = ValidationProjectionOracle.Project(key, map); var actual = pass.Project(key);
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(Document(actual), Is.EqualTo(Document(expected)));
        Assert.That(pass.Project(key), Is.SameAs(actual)); Assert.That(pass.Cached.Count, Is.LessThanOrEqualTo(map.Count));
        Assert.That(source.DataSnapshot.Entities, Is.SameAs(map));
    }

    [TestCase(16)] [TestCase(64)]
    public void All_nodes_share_ancestors_and_the_station_view_only_within_one_fixed_map(Int32 evses)
    {
        var map = Network(evses).DataSnapshot.Entities; var pass = new Pass(map);
        foreach (var key in map.Keys.OrderBy(key => key.Type).ThenBy(key => key.Id, StringComparer.Ordinal))
            Assert.That(Document(pass.Project(key)), Is.EqualTo(Document(ValidationProjectionOracle.Project(key, map))), key.ToString());
        Assert.That(pass.Cached.Count, Is.EqualTo(map.Count)); Assert.That(pass.Stations, Has.Length.EqualTo(evses / 4));
        var first = (EVSE)pass.Project(Key(map, InfrastructureEntityType.EVSE));
        var secondKey = map.Keys.Single(key => key.Type == InfrastructureEntityType.EVSE && key.Id == "DE*ABC*E2");
        Assert.That(((EVSE)pass.Project(secondKey)).ChargingStation, Is.SameAs(first.ChargingStation));
        Assert.That(pass.Stations!.All(station => ReferenceEquals(station, pass.Project(map.Keys.Single(key =>
            key.Type == InfrastructureEntityType.ChargingStation && key.Id == station.Id.ToString())))), Is.True);
        var independent = new Pass(map); var other = (EVSE)independent.Project(secondKey);
        Assert.That(other.ChargingStation, Is.Not.SameAs(first.ChargingStation));
        Assert.That(other.ChargingStation!.RoamingNetwork, Is.Not.Null);
        Assert.That(other.ChargingStation.RoamingNetwork, Is.Not.SameAs(first.ChargingStation!.RoamingNetwork));
    }

    [Test, Combinatorial]
    public void Invalid_nodes_and_ancestors_keep_the_preceding_error_and_remain_uncached(
        [Values(InfrastructureEntityType.RoamingNetwork, InfrastructureEntityType.ChargingStationOperator,
                InfrastructureEntityType.ChargingPool, InfrastructureEntityType.ChargingStation, InfrastructureEntityType.EVSE,
                InfrastructureEntityType.TransparencySoftware, InfrastructureEntityType.TransparencySoftwareCertificate,
                InfrastructureEntityType.ChargingTariff, InfrastructureEntityType.ParkingOperator, InfrastructureEntityType.ParkingSpace)]
        InfrastructureEntityType type, [Values(false, true)] Boolean descendant)
    {
        var map = Network().DataSnapshot.Entities; var changed = Key(map, type);
        var property = type == InfrastructureEntityType.TransparencySoftwareCertificate ? "issuer" :
                       type == InfrastructureEntityType.ChargingTariff ? "currency" : "name";
        map = Edit(map, changed, property, "false");
        var key = descendant && type is InfrastructureEntityType.RoamingNetwork or InfrastructureEntityType.ChargingStationOperator or
            InfrastructureEntityType.ChargingPool or InfrastructureEntityType.ChargingStation ? Key(map, InfrastructureEntityType.EVSE) : changed;
        var expected = Assert.Catch(() => ValidationProjectionOracle.Project(key, map))!; var pass = new Pass(map);
        for (var repeat = 0; repeat < 2; repeat++)
        {
            var actual = Assert.Catch(() => pass.Project(key))!;
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
            Assert.That(pass.Cached.Contains(key), Is.False); Assert.That(pass.Cached.Contains(changed), Is.False);
        }
        var repaired = Network().DataSnapshot.Entities; Assert.DoesNotThrow(() => new Pass(repaired).Project(Key(repaired, type)));
    }

    [TestCase(false)] [TestCase(true)]
    public void Multi_node_failure_order_matches_repeated_preceding_validation(Boolean reverse)
    {
        var map = Network().DataSnapshot.Entities; var evse = Key(map, InfrastructureEntityType.EVSE); var tariff = Key(map, InfrastructureEntityType.ChargingTariff);
        map = Edit(Edit(map, evse, "name", "false"), tariff, "currency", "false");
        var keys = reverse ? new[] { tariff, evse } : new[] { evse, tariff };
        var expected = Assert.Catch(() => { foreach (var key in keys) ValidationProjectionOracle.Project(key, map); })!;
        var actual = Assert.Catch(() => ValidateMany(keys, map))!;
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
    }

    [Test]
    public void Failed_station_views_are_not_published_and_valid_retry_uses_a_new_map()
    {
        var source = Network(); var map = source.DataSnapshot.Entities; var station = Key(map, InfrastructureEntityType.ChargingStation);
        var broken = Edit(map, station, "name", "false"); var pass = new Pass(broken); var parking = Key(map, InfrastructureEntityType.ParkingOperator);
        var expected = Assert.Catch(() => ValidationProjectionOracle.Project(parking, broken))!;
        for (var index = 0; index < 2; index++)
        {
            var actual = Assert.Catch(() => pass.Project(parking))!;
            Assert.That(actual.Message, Is.EqualTo(expected.Message)); Assert.That(pass.Stations, Is.Null);
            Assert.That(pass.Cached.Contains(station), Is.False); Assert.That(pass.Cached.Contains(parking), Is.False);
        }
        var retry = new Pass(map); Assert.DoesNotThrow(() => retry.Project(parking)); Assert.That(retry.Stations, Has.Length.EqualTo(4));
    }

    [Test]
    public void Validation_runtime_and_changed_maps_are_independent_from_the_source_and_other_passes()
    {
        var source = Network(); var snapshot = source.DataSnapshot; var map = snapshot.Entities;
        var root = snapshot.Root; var first = (RoamingNetwork)new Pass(map).Project(root);
        first.ApplyRuntimeUpdate(new(first.Id.ToString(), POIRuntimeTarget.GridOperator("DE*GRD"), POIRuntimeStatusKind.Status,
            new("Offline", DomainRecoveryFixture.Epoch), mode: POIRuntimeUpdateMode.ReplaceHistory));
        var second = (RoamingNetwork)new Pass(map).Project(root);
        Assert.That(second.GridOperators.Single(grid => grid.Id.ToString() == "DE*GRD").Status.Value, Is.Not.EqualTo(GridOperatorStatusType.Offline));
        Assert.That(source.GridOperators.Single(grid => grid.Id.ToString() == "DE*GRD").Status.Value, Is.Not.EqualTo(GridOperatorStatusType.Offline));
        var software = Key(map, InfrastructureEntityType.TransparencySoftware); var before = new Pass(map);
        var original = (TransparencySoftware)before.Project(software); var updatedMap = Edit(map, software, "vendor", "\"new vendor\"");
        var updated = (TransparencySoftware)new Pass(updatedMap).Project(software);
        Assert.That(updated.Vendor, Is.EqualTo("new vendor")); Assert.That(before.Project(software), Is.SameAs(original));
        Assert.That(original.Vendor, Is.Not.EqualTo(updated.Vendor)); Assert.That(source.DataSnapshot, Is.SameAs(snapshot));
    }

    [TestCase("en-US")] [TestCase("de-DE")] [TestCase("ar-EG")]
    public void Successful_reused_models_keep_culture_and_exact_values(String culture)
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(culture);
            var map = Network().DataSnapshot.Entities; var pass = new Pass(map);
            foreach (var type in new[] { InfrastructureEntityType.ChargingTariff, InfrastructureEntityType.ChargingPool,
                                        InfrastructureEntityType.ParkingOperator, InfrastructureEntityType.TransparencySoftwareCertificate })
            {
                var key = Key(map, type); Assert.That(Document(pass.Project(key)), Is.EqualTo(Document(ValidationProjectionOracle.Project(key, map))));
            }
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }

    [TestCase(false)] [TestCase(true)]
    public void Added_subtrees_validate_every_child_and_reject_bad_descendants_atomically(Boolean invalid)
    {
        var source = Network(); var before = source.ToCanonicalCBOR(); var snapshot = source.DataSnapshot;
        var payload = (JObject)DomainRecoveryFixture.CreateDocument(16)["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!.DeepClone();
        payload["@id"] = "DE*ABC*S100";
        for (var index = 0; index < 4; index++) payload["EVSEs"]![index]!["@id"] = "DE*ABC*E100" + index;
        if (invalid) payload["EVSEs"]![3]!["name"] = false;
        var change = RoamingNetworkChange.Add("ChargingStation", "DE*ABC*S100", DomainRecoveryFixture.Json(payload), "ChargingPool", "DE*ABC*P1");
        if (invalid)
        {
            var error = Assert.Throws<RoamingNetworkChangeSetException>(() => source.CreateChangeSet("subtree", DomainRecoveryFixture.Epoch.AddSeconds(1), [change]))!;
            Assert.That(error.OperationIndex, Is.EqualTo(0));
        }
        else
        {
            var batch = source.CreateChangeSet("subtree", DomainRecoveryFixture.Epoch.AddSeconds(1), [change]);
            payload.RemoveAll(); var next = source.ApplyChangeSet(batch);
            Assert.That(next.EVSEs.Count(), Is.EqualTo(20)); Assert.That(next.DataSnapshot.Entities.Count, Is.EqualTo(snapshot.Entities.Count + 9));
            DomainRecoveryFixture.CheckGraph(next);
            Assert.That(next.ChargingStations.Single(station => station.Id.ToString() == "DE*ABC*S100").EVSEs.Count(), Is.EqualTo(4));
        }
        Assert.That(source.DataSnapshot, Is.SameAs(snapshot)); Assert.That(source.ToCanonicalCBOR(), Is.EqualTo(before));
    }

    [TestCase("root")] [TestCase("station")] [TestCase("disjoint")]
    public void Merge_validation_collects_every_preceding_domain_error_in_the_same_order(String variant)
    {
        var snapshot = Network(groups: true).DataSnapshot; var map = snapshot.Entities; var before = snapshot.ToCanonicalCBOR();
        if (variant == "root") map = Edit(map, snapshot.Root, "name", "false");
        else if (variant == "station") map = Edit(map, Key(map, InfrastructureEntityType.ChargingStation), "name", "false");
        else map = Edit(Edit(map, Key(map, InfrastructureEntityType.EVSE), "name", "false"),
            Key(map, InfrastructureEntityType.ChargingTariff), "currency", "false");
        var expected = new List<(InfrastructureEntityKey Key, String Error)>();
        foreach (var node in map.Values.OrderBy(node => node.Key.Type).ThenBy(node => node.Key.Id, StringComparer.Ordinal).ThenBy(node => node.Key.Scope, StringComparer.Ordinal))
            try { ValidationProjectionOracle.Project(node.Key, map); }
            catch (Exception error) { expected.Add((node.Key, error.Message)); }
        var actual = Assert.Throws<POIMergePlanningException>(() => snapshot.MergeTarget(map.Values))!;
        Assert.That(actual.Issues.Select(issue => (issue.Entity, issue.Message)), Is.EqualTo(expected));
        Assert.That(snapshot.ToCanonicalCBOR(), Is.EqualTo(before));
    }

    [Test]
    public void Concurrent_validation_passes_keep_runtime_and_projections_independent()
    {
        var source = Network(); var snapshot = source.DataSnapshot; using var barrier = new Barrier(2);
        RoamingNetwork Run(String status)
        {
            var root = (RoamingNetwork)new Pass(snapshot.Entities).Project(snapshot.Root);
            root.ApplyRuntimeUpdate(new(root.Id.ToString(), POIRuntimeTarget.GridOperator("DE*GRD"), POIRuntimeStatusKind.Status,
                new(status, DomainRecoveryFixture.Epoch), mode: POIRuntimeUpdateMode.ReplaceHistory));
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Concurrent validation barrier.");
            return root;
        }
        var left = Task.Run(() => Run("Offline")); var right = Task.Run(() => Run("Available")); Task.WaitAll(left, right);
        Assert.That(left.Result, Is.Not.SameAs(right.Result));
        Assert.That(left.Result.GridOperators.Single(grid => grid.Id.ToString() == "DE*GRD").Status.Value, Is.EqualTo(GridOperatorStatusType.Offline));
        Assert.That(right.Result.GridOperators.Single(grid => grid.Id.ToString() == "DE*GRD").Status.Value, Is.EqualTo(GridOperatorStatusType.Available));
        Assert.That(source.GridOperators.Single(grid => grid.Id.ToString() == "DE*GRD").Status.Value, Is.Not.EqualTo(GridOperatorStatusType.Offline));
        Assert.That(source.DataSnapshot, Is.SameAs(snapshot));
    }
}
