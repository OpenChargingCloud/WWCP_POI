/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SharedDomainModelTests
{
    private const String Pool1 = "DE*ABC*P1";
    private const String Pool2 = "DE*ABC*P2";
    private const String Software2 = "verifier-2";
    private const String Certificate42 = "approval-42";

    private static JObject Input()
    {
        using var text = new StringReader(File.ReadAllText(FilePath("snapshot.input.json")));
        using var reader = new JsonTextReader(text) {
            DateParseHandling = DateParseHandling.None, FloatParseHandling = FloatParseHandling.Decimal
        };
        var json = JObject.Load(reader);
        var pool = PoolJSON(json);
        pool["gridConnectionPoint"]!["energyMeter"] = JObject.Parse("""
            {"id":"point-meter", "role":"grid", "created":"2026-01-01T00:00:00Z", "lastChange":"2026-01-01T00:00:00Z",
             "transparencySoftware":[{"transparencySoftwareId":"verifier-2","legalStatus":"verified","certificateId":"approval-42"}]}
            """);
        var second = (JObject)pool.DeepClone();
        second["@id"] = Pool2;
        second["chargingStations"] = new JArray();
        ((JArray)json["chargingStationOperators"]![0]!["chargingPools"]!).Add(second);
        return json;
    }

    private static JObject PoolJSON(JObject json) => (JObject)json["chargingStationOperators"]![0]!["chargingPools"]![0]!;
    private static JObject ParkingJSON(JObject json) => (JObject)json["parkingOperators"]![0]!;
    private static RoamingNetwork Source() => RoamingNetwork.Parse(Input().ToString(Formatting.None));
    private static JsonElement Document(JToken json) => Value(json.ToString(Formatting.None));
    private static JsonElement Text(String text) => System.Text.Json.JsonSerializer.SerializeToElement(text);
    private static RoamingNetworkChange Edit(String type, String id, String property, JsonElement value)
        => RoamingNetworkChange.UpdateProperty(type, id, property, null, value);
    private static RoamingNetwork Derive(RoamingNetwork source, params RoamingNetworkChange[] changes)
        => source.ApplyChangeSet(Batch(source.DataSnapshot, "domain-change", changes));

    private static IEnumerable<EnergyMeter> Meters(RoamingNetwork network)
        => network.ChargingPools.SelectMany(pool => pool.EnergyMeters)
            .Concat(network.ChargingStations.SelectMany(station => station.EnergyMeters))
            .Concat(network.EVSEs.Where(evse => evse.EnergyMeter is not null).Select(evse => evse.EnergyMeter!))
            .Concat(network.ChargingPools.Where(pool => pool.GridConnectionPoint?.EnergyMeter is not null)
                .Select(pool => pool.GridConnectionPoint!.EnergyMeter!));

    private static void AssertShared(RoamingNetwork network)
    {
        var grid = network.GridOperators.Single();
        foreach (var point in network.ChargingPools.Select(pool => pool.GridConnectionPoint!))
            Assert.That(point.GridOperator, Is.SameAs(grid));
        foreach (var assignment in Meters(network).SelectMany(meter => meter.TransparencySoftware))
        {
            Assert.That(assignment.TransparencySoftware, Is.SameAs(network.GetTransparencySoftwareById(assignment.TransparencySoftwareId)));
            Assert.That(assignment.Certificate, Is.SameAs(network.GetTransparencySoftwareCertificateById(assignment.CertificateId!.Value)));
        }
    }

    [Test]
    public void All_meter_slots_and_points_resolve_one_shared_catalog_instance()
    {
        var network = Source();
        Assert.That(network.ChargingPools.Count(), Is.EqualTo(2));
        Assert.That(Meters(network).Count(), Is.EqualTo(6));
        Assert.That(network.TransparencySoftware, Has.Length.EqualTo(2));
        Assert.That(network.TransparencySoftwareCertificates, Has.Length.EqualTo(1));
        AssertShared(network);
        var key = new InfrastructureEntityKey(InfrastructureEntityType.TransparencySoftwareCertificate, Certificate42);
        Assert.That(network.DataSnapshot.References[key], Is.EquivalentTo(new[] {
            new InfrastructureEntityKey(InfrastructureEntityType.ChargingPool, Pool1),
            new InfrastructureEntityKey(InfrastructureEntityType.ChargingPool, Pool2),
            new InfrastructureEntityKey(InfrastructureEntityType.ChargingStation, "DE*ABC*S1"),
            new InfrastructureEntityKey(InfrastructureEntityType.EVSE, "DE*ABC*E1")
        }));
    }

    [TestCase("en-US", false, false)]
    [TestCase("en-US", false, true)]
    [TestCase("en-US", true, false)]
    [TestCase("en-US", true, true)]
    [TestCase("de-DE", false, false)]
    [TestCase("de-DE", false, true)]
    [TestCase("de-DE", true, false)]
    [TestCase("de-DE", true, true)]
    [TestCase("ar-EG", false, false)]
    [TestCase("ar-EG", false, true)]
    [TestCase("ar-EG", true, false)]
    [TestCase("ar-EG", true, true)]
    public void Complete_transports_preserve_shared_references_hashes_and_parking(String culture, Boolean cbor, Boolean runtime)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var source = Source();
            source.ApplyRuntimeUpdate(new(source.Id.ToString(), POIRuntimeTarget.GridOperator("DE*GRD"),
                POIRuntimeStatusKind.Status, new("Offline", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
            var restored = cbor
                ? RoamingNetwork.ParseCBOR(source.ToCBOR(IncludeRuntime: runtime, IncludeVersionMetadata: true))
                : POIRepresentation.ParseJSON(source.ToJSONWithETags(IncludeRuntime: runtime, IncludeVersionMetadata: true),
                    json => RoamingNetwork.Parse(json));
            Assert.That(restored.ETags, Is.EqualTo(source.ETags));
            Assert.That(restored.ToCanonicalJSON(), Is.EqualTo(source.ToCanonicalJSON()));
            Assert.That(restored.ToCanonicalCBOR(), Is.EqualTo(source.ToCanonicalCBOR()));
            Assert.That(restored.Revision, Is.EqualTo(source.Revision));
            AssertShared(restored);
            Assert.That(restored.TransparencySoftwareCertificates.Single().NotBefore!.Value.Ticks % TimeSpan.TicksPerSecond, Is.EqualTo(1234567));
            Assert.That(restored.ParkingOperators.Single().GetAvailableParkingProducts(ParkingSpace_Id.Parse("space-1"))
                .Select(product => product.Id.ToString()), Is.EqualTo(new[] { "long-stay", "short-stay" }));
            Assert.That(restored.GridOperators.Single().Status.Value == GridOperatorStatusTypes.Offline, Is.EqualTo(runtime));
            Assert.That(restored.GridOperators.Single(), Is.Not.SameAs(source.GridOperators.Single()));
            var json = restored.DataSnapshot.ToJSON();
            Assert.That(json["gridOperators"], Has.Count.EqualTo(1));
            Assert.That(json["transparencySoftware"], Has.Count.EqualTo(2));
            Assert.That(PoolJSON(json)["gridConnectionPoint"]!["gridOperator"], Is.Null);
            Assert.That(PoolJSON(json)["energyMeters"]![0]!["transparencySoftware"]![0]!["transparencySoftware"], Is.Null);
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [TestCase("Software", false)]
    [TestCase("Software", true)]
    [TestCase("Certificate", false)]
    [TestCase("Certificate", true)]
    [TestCase("Meter", false)]
    [TestCase("Meter", true)]
    public void Standalone_transports_validate_declared_ETags_and_resolve_context(String kind, Boolean cbor)
    {
        var network = Source();
        IImmutablePOI original = kind switch {
            "Software" => network.TransparencySoftware[0],
            "Certificate" => network.TransparencySoftwareCertificates[0],
            _ => network.ChargingPools.First().EnergyMeters[0]
        };
        IImmutablePOI Read(JObject json) => kind switch {
            "Software" => POIRepresentation.ParseJSON(json, value => TransparencySoftware.Parse(value)),
            "Certificate" => POIRepresentation.ParseJSON(json, value => TransparencySoftwareCertificate.Parse(value, network)),
            _ => POIRepresentation.ParseJSON(json, value => EnergyMeter.Parse(value, Network: network))
        };
        IImmutablePOI restored = cbor ? kind switch {
            "Software" => TransparencySoftware.ParseCBOR(original.ToCBOR()),
            "Certificate" => TransparencySoftwareCertificate.ParseCBOR(original.ToCBOR(), network),
            _ => EnergyMeter.ParseCBOR(original.ToCBOR(), network)
        } : Read(original.ToJSONWithETags());
        Assert.That(restored.ETags, Is.EqualTo(original.ETags));
        var tampered = original.ToJSONWithETags();
        tampered[kind switch { "Software" => "vendor", "Certificate" => "issuer", _ => "role" }] = "changed";
        Assert.Throws<ArgumentException>(() => Read(tampered));
        if (kind == "Meter") Assert.Throws<ArgumentException>(() => EnergyMeter.ParseCBOR(original.ToCBOR()));
    }

    [TestCase("duplicate-grid")]
    [TestCase("duplicate-software")]
    [TestCase("duplicate-certificate")]
    [TestCase("missing-grid")]
    [TestCase("missing-software")]
    [TestCase("missing-certificate")]
    [TestCase("uncovered-release")]
    [TestCase("duplicate-assignment")]
    [TestCase("overlapping-coverage")]
    [TestCase("empty-coverage")]
    [TestCase("duplicate-product")]
    [TestCase("duplicate-space-member")]
    [TestCase("missing-garage")]
    [TestCase("missing-product")]
    public void Invalid_catalogs_and_references_are_rejected_without_changing_input(String kind)
    {
        var json = Input();
        var meter = PoolJSON(json)["energyMeters"]![0]!;
        var certificate = json["transparencySoftwareCertificates"]![0]!;
        var parking = ParkingJSON(json);
        switch (kind)
        {
            case "duplicate-grid": ((JArray)json["gridOperators"]!).Add(json["gridOperators"]![0]!.DeepClone()); break;
            case "duplicate-software": ((JArray)json["transparencySoftware"]!).Add(json["transparencySoftware"]![0]!.DeepClone()); break;
            case "duplicate-certificate": ((JArray)json["transparencySoftwareCertificates"]!).Add(certificate.DeepClone()); break;
            case "missing-grid": PoolJSON(json)["gridConnectionPoint"]!["gridOperatorId"] = "DE*BAD"; break;
            case "missing-software": meter["transparencySoftware"]![0]!["transparencySoftwareId"] = "missing"; break;
            case "missing-certificate": meter["transparencySoftware"]![0]!["certificateId"] = "missing"; break;
            case "uncovered-release": certificate["compatibleTransparencySoftwareIds"] = new JArray(); break;
            case "duplicate-assignment": ((JArray)meter["transparencySoftware"]!).Add(meter["transparencySoftware"]![0]!.DeepClone()); break;
            case "overlapping-coverage": certificate["compatibleTransparencySoftwareIds"] = new JArray(Software2); break;
            case "empty-coverage": certificate["verifiedTransparencySoftwareIds"] = new JArray(); certificate["compatibleTransparencySoftwareIds"] = new JArray(); break;
            case "duplicate-product": ((JArray)parking["parkingProducts"]!).Add(parking["parkingProducts"]![0]!.DeepClone()); break;
            case "duplicate-space-member": parking["parkingSpaceGroups"]![0]!["parkingSpaceIds"] = new JArray("space-1", "space-1"); break;
            case "missing-garage": parking["parkingSpaces"]![0]!["parkingGarageId"] = "missing"; break;
            case "missing-product": parking["parkingGarages"]![0]!["parkingProductIds"] = new JArray("missing"); break;
        }
        var before = json.ToString(Formatting.None);
        Assert.Throws<ArgumentException>(() => RoamingNetwork.Parse(json));
        Assert.That(json.ToString(Formatting.None), Is.EqualTo(before));
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(" release")]
    [TestCase("release ")]
    public void Opaque_identifiers_reject_empty_or_surrounding_whitespace(String text)
    {
        Assert.That(TransparencySoftware_Id.TryParse(text, out _), Is.False);
        Assert.That(TransparencySoftwareCertificate_Id.TryParse(text, out _), Is.False);
        Assert.Throws<ArgumentException>(() => TransparencySoftware_Id.Parse(text));
        Assert.Throws<ArgumentException>(() => TransparencySoftwareCertificate_Id.Parse(text));
    }

    [Test]
    public void Identifiers_are_case_sensitive_and_certificate_inputs_are_detached()
    {
        var software = TransparencySoftware_Id.Parse(Software2);
        Assert.That(software, Is.Not.EqualTo(TransparencySoftware_Id.Parse(Software2.ToUpperInvariant())));
        Assert.That(TransparencySoftwareCertificate_Id.Parse(Certificate42), Is.Not.EqualTo(TransparencySoftwareCertificate_Id.Parse(Certificate42.ToUpperInvariant())));
        var ids = new List<TransparencySoftware_Id> { software };
        var certificate = new TransparencySoftwareCertificate(TransparencySoftwareCertificate_Id.Parse("new-document"), "Issuer", "Model", "Version", ids);
        var tags = certificate.ETags;
        ids.Clear();
        Assert.That(certificate.VerifiedTransparencySoftwareIds, Is.EqualTo(new[] { software }));
        Assert.That(certificate.ETags, Is.EqualTo(tags));
    }

    [Test]
    public void Registry_edits_rebind_constructors_and_preserve_independent_operator_runtime()
    {
        var source = Source();
        var points = source.ChargingPools.Select(pool => pool.GridConnectionPoint!).ToArray();
        source.ApplyRuntimeUpdate(new(source.Id.ToString(), POIRuntimeTarget.GridOperator("DE*GRD"),
            POIRuntimeStatusKind.Status, new("Offline", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        var next = Derive(source,
            Edit("GridOperator", "DE*GRD", "name", Value("{\"en\":\"New grid name\"}")),
            Edit("TransparencySoftware", Software2, "vendor", Text("New vendor")),
            Edit("TransparencySoftwareCertificate", Certificate42, "issuer", Text("New issuer")));
        AssertShared(next);
        Assert.That(next.ETags, Is.Not.EqualTo(source.ETags));
        Assert.That(next.GridOperators.Single().Name.ToJSON()["en"]!.Value<String>(), Is.EqualTo("New grid name"));
        Assert.That(next.GridOperators.Single().Status.Value, Is.EqualTo(GridOperatorStatusTypes.Offline));
        Assert.That(next.GridOperators.Single(), Is.Not.SameAs(source.GridOperators.Single()));
        Assert.That(next.ChargingPools.Select(pool => pool.GridConnectionPoint!.ETags), Is.EqualTo(points.Select(point => point.ETags)));
        var oldMeter = source.ChargingPools.First().EnergyMeters[0];
        var pool = new ChargingPool(ChargingPool_Id.Parse("DE*ABC*P3"), next.ChargingStationOperators.Single(),
            EnergyMeters: [oldMeter], GridConnectionPoint: points[0]);
        var assignment = pool.EnergyMeters[0].TransparencySoftware.Single();
        Assert.That(assignment.TransparencySoftware, Is.SameAs(next.GetTransparencySoftwareById(assignment.TransparencySoftwareId)));
        Assert.That(assignment.Certificate!.Issuer, Is.EqualTo("New issuer"));
        Assert.That(pool.GridConnectionPoint!.GridOperator, Is.SameAs(next.GridOperators.Single()));
        Assert.That(oldMeter.TransparencySoftware.Single().TransparencySoftware.Vendor, Is.EqualTo("Example vendor"));
        next.ApplyRuntimeUpdate(new(next.Id.ToString(), POIRuntimeTarget.GridOperator("DE*GRD"),
            POIRuntimeStatusKind.Status, new("Available", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        Assert.That(source.GridOperators.Single().Status.Value, Is.EqualTo(GridOperatorStatusTypes.Offline));
        Assert.That(next.GridOperators.Single().Status.Value, Is.EqualTo(GridOperatorStatusTypes.Available));
    }

    [TestCase("grid")]
    [TestCase("software")]
    [TestCase("certificate")]
    [TestCase("product")]
    [TestCase("coverage")]
    [TestCase("runtime")]
    public void Failed_second_operations_leave_static_references_and_runtime_unchanged(String kind)
    {
        var network = Source();
        var snapshot = network.DataSnapshot;
        network.ApplyRuntimeUpdate(new(network.Id.ToString(), POIRuntimeTarget.GridOperator("DE*GRD"),
            POIRuntimeStatusKind.Status, new("Offline", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        var before = network.ToJSONSnapshot().ToString(Formatting.None);
        var good = Edit("GridOperator", "DE*GRD", "name", Value("{\"en\":\"First valid edit\"}"));
        var prepared = Batch(snapshot, "valid", good);
        var bad = kind switch {
            "grid" => RoamingNetworkChange.Remove("GridOperator", "DE*GRD"),
            "software" => RoamingNetworkChange.Remove("TransparencySoftware", Software2),
            "certificate" => RoamingNetworkChange.Remove("TransparencySoftwareCertificate", Certificate42),
            "product" => RoamingNetworkChange.Remove("ParkingProduct", "long-stay"),
            "coverage" => Edit("TransparencySoftwareCertificate", Certificate42, "verifiedTransparencySoftwareIds", Value("[]")),
            _ => Edit("TransparencySoftware", Software2, "status", Text("changed"))
        };
        var invalid = new RoamingNetworkChangeSet("invalid", network.Id.ToString(), network.Revision, InteropFixture.Time,
            [good, bad], network.ETags, prepared.AfterETags);
        var error = Assert.Throws<RoamingNetworkChangeSetException>(() => network.ApplyChangeSet(invalid));
        Assert.That(error!.OperationIndex, Is.EqualTo(1));
        Assert.That(network.DataSnapshot, Is.SameAs(snapshot));
        Assert.That(network.DataSnapshot.References, Is.SameAs(snapshot.References));
        Assert.That(network.ToJSONSnapshot().ToString(Formatting.None), Is.EqualTo(before));
        AssertShared(network);
    }

    [Test]
    public void Catalog_nodes_support_ordered_add_edit_and_remove()
    {
        var source = Source();
        var software = Value("""{"@id":"unused-release","name":"Other verifier","version":"1","vendor":"Other vendor","openSourceLicense":{"@id":"MIT"}}""");
        var document = Value("""{"@id":"unused-document","issuer":"Other issuer","chargingStationModel":"Model Y","chargingStationModelVersion":"1","compatibleTransparencySoftwareIds":["unused-release"]}""");
        var next = Derive(source,
            RoamingNetworkChange.Add("TransparencySoftware", "unused-release", software, "RoamingNetwork", source.Id.ToString()),
            RoamingNetworkChange.Add("TransparencySoftwareCertificate", "unused-document", document, "RoamingNetwork", source.Id.ToString()),
            Edit("TransparencySoftwareCertificate", "unused-document", "documentNumber", Text("42")));
        Assert.That(next.TransparencySoftware, Has.Length.EqualTo(3));
        Assert.That(next.TransparencySoftwareCertificates, Has.Length.EqualTo(2));
        Assert.That(next.DataSnapshot.GetEntity(InfrastructureEntityType.TransparencySoftwareCertificate, "unused-document").Properties.ContainsKey("created"), Is.False);
        var removed = Derive(next, RoamingNetworkChange.Remove("TransparencySoftwareCertificate", "unused-document"),
            RoamingNetworkChange.Remove("TransparencySoftware", "unused-release"));
        Assert.That(removed.DataSnapshot.Entities.Count, Is.EqualTo(source.DataSnapshot.Entities.Count));
        Assert.That(removed.DataSnapshot.References.Keys, Does.Not.Contain(new InfrastructureEntityKey(InfrastructureEntityType.TransparencySoftware, "unused-release")));
        Assert.That(source.TransparencySoftware, Has.Length.EqualTo(2));
    }

    [TestCase(InfrastructureEntityType.TransparencySoftware, Software2)]
    [TestCase(InfrastructureEntityType.TransparencySoftwareCertificate, Certificate42)]
    [TestCase(InfrastructureEntityType.ParkingProduct, "long-stay")]
    public void Stateless_catalog_nodes_reject_operational_targets(InfrastructureEntityType type, String id)
        => Assert.Throws<ArgumentException>(() => POIRuntimeTarget.Entity(type, id));

    [Test]
    public void Overlapping_parking_groups_offer_a_deduplicated_union_and_targeted_membership_edits()
    {
        var source = Source();
        var parking = source.ParkingOperators.Single();
        Assert.That(parking.ParkingSpaceGroups.Count(group => group.ParkingSpaceIds.Contains(ParkingSpace_Id.Parse("space-1"))), Is.EqualTo(2));
        Assert.That(parking.GetAvailableParkingProducts(ParkingSpace_Id.Parse("space-1")).Select(product => product.Id.ToString()),
            Is.EqualTo(new[] { "long-stay", "short-stay" }));
        Assert.Throws<ArgumentException>(() => parking.GetAvailableParkingProducts(ParkingSpace_Id.Parse("missing")));
        var next = Derive(source,
            RoamingNetworkChange.Add("ParkingSpace", "space-2", Value("""{"@id":"space-2","parkingProductIds":["long-stay"]}"""), "ParkingOperator", "parking-op"),
            RoamingNetworkChange.AddElement("ParkingSpaceGroup", "group-1", [new("parkingSpaceIds", "space-2")], Text("space-2")),
            Edit("ParkingProduct", "long-stay", "minDuration", Text("1200 s")),
            RoamingNetworkChange.RemoveProperty("ParkingSpace", "space-1", "parkingGarageId"));
        var current = next.ParkingOperators.Single();
        Assert.That(current.GetAvailableParkingProducts(ParkingSpace_Id.Parse("space-2")).Select(product => product.Id.ToString()),
            Is.EqualTo(new[] { "long-stay", "short-stay" }));
        Assert.That(current.ParkingSpaces.Single(space => space.Id.ToString() == "space-1").ParkingGarageId, Is.Null);
        Assert.That(current.ParkingProducts.Single(product => product.Id.ToString() == "long-stay").MinDuration, Is.EqualTo(TimeSpan.FromMinutes(20)));
        Assert.That(parking.ParkingProducts.Single(product => product.Id.ToString() == "long-stay").MinDuration, Is.EqualTo(TimeSpan.FromMinutes(10)));
        var final = Derive(next, RoamingNetworkChange.RemoveElement("ParkingSpaceGroup", "group-1", [new("parkingSpaceIds", "space-2")]),
            RoamingNetworkChange.Remove("ParkingSpace", "space-2"));
        Assert.That(final.ParkingOperators.Single().ParkingSpaces, Has.Length.EqualTo(1));
    }

    [TestCase("garage")]
    [TestCase("product")]
    [TestCase("space")]
    public void Parking_references_cannot_cross_operator_scope(String kind)
    {
        var json = Input();
        ((JArray)json["parkingOperators"]!).Add(JObject.Parse("""
            {"id":"other-parking", "created":"2026-01-01T00:00:00Z", "lastChange":"2026-01-01T00:00:00Z",
             "parkingProducts":[{"@id":"foreign-product"}],
             "parkingGarages":[{"@id":"foreign-garage"}], "parkingSpaces":[{"@id":"foreign-space"}]}
            """));
        var parking = ParkingJSON(json);
        if (kind == "garage") parking["parkingSpaces"]![0]!["parkingGarageId"] = "foreign-garage";
        if (kind == "product") parking["parkingSpaces"]![0]!["parkingProductIds"] = new JArray("foreign-product");
        if (kind == "space") parking["parkingSpaceGroups"]![0]!["parkingSpaceIds"] = new JArray("foreign-space");
        Assert.Throws<ArgumentException>(() => RoamingNetwork.Parse(json));
    }

    [Test]
    public void Parking_station_links_protect_target_deletion_until_explicitly_detached()
    {
        var source = Derive(Source(), RoamingNetworkChange.AddElement("ParkingSpace", "space-1",
            [new("chargingStationIds", "DE*ABC*S1")], Text("DE*ABC*S1")));
        Assert.Throws<RoamingNetworkChangeSetException>(() => Batch(source.DataSnapshot, "blocked", RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1")));
        var next = Derive(source, RoamingNetworkChange.RemoveElement("ParkingSpace", "space-1", [new("chargingStationIds", "DE*ABC*S1")]),
            RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S1"));
        Assert.That(next.ChargingStations, Is.Empty);
        Assert.That(next.ParkingOperators.Single().ParkingSpaces.Single().ChargingStations, Is.Empty);
    }

    [TestCase(-1, 60)]
    [TestCase(0, -1)]
    [TestCase(61, 60)]
    public void Invalid_parking_duration_ranges_are_rejected(Int32 minimum, Int32 cutoff)
        => Assert.Throws<ArgumentException>(() => new ParkingProduct(ParkingProduct_Id.Parse("invalid"), TimeSpan.FromSeconds(minimum), TimeSpan.FromSeconds(cutoff)));

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void Optional_parking_durations_preserve_absence_in_complete_transports(Boolean minimum, Boolean cutoff)
    {
        var product = new ParkingProduct(ParkingProduct_Id.Parse("optional"),
            minimum ? TimeSpan.FromMinutes(5) : null, cutoff ? TimeSpan.FromHours(2) : null);
        var json = product.ToJSON();
        Assert.That(json.ContainsKey("minDuration"), Is.EqualTo(minimum));
        Assert.That(json.ContainsKey("stopParkingAfterTime"), Is.EqualTo(cutoff));
        var restored = POIRepresentation.ParseJSON(json, ParkingProduct.Parse);
        var fromCBOR = ParkingProduct.ParseCBOR(product.ToCBOR());
        foreach (var copy in new[] { restored, fromCBOR })
        {
            Assert.That(copy.MinDuration, Is.EqualTo(product.MinDuration));
            Assert.That(copy.StopParkingAfterTime, Is.EqualTo(product.StopParkingAfterTime));
            Assert.That(copy.ETags, Is.EqualTo(product.ETags));
        }
    }

    [Test]
    public void Disjoint_catalog_and_parking_edits_merge_only_after_explicit_request()
    {
        using var history = History(Source());
        var root = history.Head.Id;
        var left = Prepare(history, root, "vendor", Edit("TransparencySoftware", Software2, "vendor", Text("Changed vendor")));
        var right = Prepare(history, root, "document-and-parking",
            Edit("TransparencySoftwareCertificate", Certificate42, "documentNumber", Text("New number")),
            Edit("ParkingProduct", "long-stay", "minDuration", Text("900 s")));
        Publish(history, left); Store(history, right);
        var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var preview, out var report), Is.True, report.Message);
        Assert.That(preview, Is.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.MergeAvailable));
        before.AssertUnchanged(history);
        var merged = Merge(history, left, right); before.AssertUnchanged(history); Publish(history, merged);
        AssertShared(history.Head.Network);
        Assert.That(history.Head.Network.GetTransparencySoftwareById(TransparencySoftware_Id.Parse(Software2))!.Vendor, Is.EqualTo("Changed vendor"));
        Assert.That(history.Head.Network.TransparencySoftwareCertificates.Single().DocumentNumber, Is.EqualTo("New number"));
        Assert.That(history.Head.Network.ParkingOperators.Single().ParkingProducts.Single(product => product.Id.ToString() == "long-stay").MinDuration,
            Is.EqualTo(TimeSpan.FromMinutes(15)));
        using var recovered = RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit);
        Assert.That(recovered.Head.Id, Is.EqualTo(merged.Id)); AssertShared(recovered.Head.Network);
    }

    private static RoamingNetworkChange[] Recreate(RoamingNetworkDataSnapshot snapshot, InfrastructureEntityKey target)
    {
        var detach = new List<RoamingNetworkChange>();
        var restore = new List<RoamingNetworkChange>();
        foreach (var entity in snapshot.Entities.Values.OrderBy(entity => entity.Key.Type).ThenBy(entity => entity.Key.Id, StringComparer.Ordinal))
        {
            foreach (var field in new[] { "energyMeters", "energyMeter", "gridConnectionPoint" })
            {
                if (!entity.Properties.TryGetValue(field, out var before)) continue;
                var value = JToken.Parse(before.GetRawText());
                if (field == "gridConnectionPoint" && target.Type == InfrastructureEntityType.GridOperator)
                {
                    detach.Add(RoamingNetworkChange.RemoveProperty(entity.Key.Type.ToString(), entity.Key.Id, field, before));
                    restore.Add(Edit(entity.Key.Type.ToString(), entity.Key.Id, field, before));
                    continue;
                }
                var meters = field == "energyMeters" ? (IEnumerable<JToken>)value.Children() :
                    field == "gridConnectionPoint" ? value["energyMeter"] is { } pointMeter ? [pointMeter] : [] : [value];
                foreach (var meter in meters)
                    if (meter["transparencySoftware"] is JArray assignments)
                        foreach (var assignment in assignments.OfType<JObject>().ToArray())
                        {
                            if (target.Type == InfrastructureEntityType.TransparencySoftware && assignment["transparencySoftwareId"]?.Value<String>() == target.Id)
                                assignment.Remove();
                            if (target.Type == InfrastructureEntityType.TransparencySoftwareCertificate && assignment["certificateId"]?.Value<String>() == target.Id)
                                assignment.Remove("certificateId");
                        }
                var after = Document(value);
                if (after.GetRawText() == before.GetRawText()) continue;
                detach.Add(Edit(entity.Key.Type.ToString(), entity.Key.Id, field, after));
                restore.Add(Edit(entity.Key.Type.ToString(), entity.Key.Id, field, before));
            }
        }
        if (target.Type == InfrastructureEntityType.TransparencySoftware)
            foreach (var certificate in snapshot.Entities.Values.Where(entity => entity.Key.Type == InfrastructureEntityType.TransparencySoftwareCertificate))
                foreach (var field in new[] { "verifiedTransparencySoftwareIds", "compatibleTransparencySoftwareIds" })
                    if (certificate.Properties.TryGetValue(field, out var ids) && ids.EnumerateArray().Any(id => id.GetString() == target.Id))
                    {
                        detach.Add(RoamingNetworkChange.RemoveElement(certificate.Key.Type.ToString(), certificate.Key.Id, [new(field, target.Id)]));
                        restore.Insert(0, RoamingNetworkChange.AddElement(certificate.Key.Type.ToString(), certificate.Key.Id, [new(field, target.Id)], Text(target.Id)));
                    }
        var replacement = snapshot.GetEntityJSON(target.Type, target.Id);
        replacement[target.Type switch {
            InfrastructureEntityType.GridOperator => "name", InfrastructureEntityType.TransparencySoftware => "vendor", _ => "issuer"
        }] = target.Type == InfrastructureEntityType.GridOperator ? JObject.Parse("{\"en\":\"Replacement grid\"}") : new JValue("Replacement");
        var parent = snapshot.Entities[target].Parent!.Value;
        return [.. detach, RoamingNetworkChange.Remove(target.Type.ToString(), target.Id),
            RoamingNetworkChange.Add(target.Type.ToString(), target.Id, Document(replacement), parent.Type.ToString(), parent.Id), .. restore];
    }

    [TestCase(InfrastructureEntityType.GridOperator, "DE*GRD", false)]
    [TestCase(InfrastructureEntityType.GridOperator, "DE*GRD", true)]
    [TestCase(InfrastructureEntityType.TransparencySoftware, Software2, false)]
    [TestCase(InfrastructureEntityType.TransparencySoftware, Software2, true)]
    [TestCase(InfrastructureEntityType.TransparencySoftwareCertificate, Certificate42, false)]
    [TestCase(InfrastructureEntityType.TransparencySoftwareCertificate, Certificate42, true)]
    public void Referenced_catalog_recreation_merges_with_explicit_temporary_transitions(InfrastructureEntityType type, String id, Boolean swapped)
    {
        using var history = History(Source());
        var root = history.Head.Id;
        var original = history.Head.Snapshot;
        var recreated = Prepare(history, root, "recreate", Recreate(original, new(type, id)));
        var unrelated = Prepare(history, root, "unrelated", Rename("Unrelated station edit"));
        var left = swapped ? recreated : unrelated;
        var right = swapped ? unrelated : recreated;
        Publish(history, left); Store(history, right);
        Status(history, POIRuntimeTarget.GridOperator("DE*GRD"), "Offline");
        Status(history, PoolMeter, "error"); Status(history, StationMeter, "reserved");
        Status(history, POIRuntimeTarget.ConnectionMeter(Pool1, "point-meter"), "error");
        var old = history.Head.Network;
        var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var prepared, out var report, merge: true,
            mergedChangeSetId: "recreation-merge"), Is.True, report.Message + " " + String.Join("; ", report.Conflicts.Select(conflict => conflict.Path + ": " + conflict.Message)));
        before.AssertUnchanged(history);
        Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.Prepared));
        if (!swapped)
        {
            Assert.That(report.ReferenceTransitions, Is.Not.Empty);
            foreach (var transition in report.ReferenceTransitions)
            {
                Assert.That(transition.Targets, Does.Contain(new InfrastructureEntityKey(type, id)));
                Assert.That(transition.DetachOperationIndices, Is.Not.Empty);
                Assert.That(transition.RestoreOperationIndices, Is.Not.Empty);
                Assert.That(transition.DetachOperationIndices.Max(), Is.LessThan(transition.RestoreOperationIndices.Min()));
            }
        }
        var signed = Sign(prepared!.WithChangeSet(Sign(prepared.ChangeSet!)));
        Publish(history, signed); AssertShared(history.Head.Network);
        var current = history.Head.Network;
        var continuity = type != InfrastructureEntityType.GridOperator || swapped;
        Assert.That(current.GridOperators.Single().Status.Value == GridOperatorStatusTypes.Offline, Is.EqualTo(continuity));
        Assert.That(current.ChargingPools.Single(pool => pool.Id.ToString() == Pool1).GridConnectionPoint!.EnergyMeter!.Status.Value.ToString() == "error", Is.EqualTo(continuity));
        Assert.That(current.ChargingPools.Single(pool => pool.Id.ToString() == Pool1).EnergyMeters[0].Status.Value.ToString(), Is.EqualTo("error"));
        Assert.That(current.ChargingStations.Single().EnergyMeters[0].Status.Value.ToString(), Is.EqualTo("reserved"));
        Assert.That(old.GridOperators.Single().Status.Value, Is.EqualTo(GridOperatorStatusTypes.Offline));
        Assert.That(history.Head.Network.ChargingStations.Single().Name.ToJSON()["en"]!.Value<String>(), Is.EqualTo("Unrelated station edit"));
        using var restored = RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit);
        Assert.That(restored.Head.Id, Is.EqualTo(signed.Id)); AssertShared(restored.Head.Network);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Second_parent_adoption_keeps_shared_registries_and_independent_local_runtime(Boolean cbor)
    {
        using var sender = History(Source()); using var receiver = History(Source());
        var root = sender.Head.Id;
        var left = Prepare(sender, root, "software-vendor", Edit("TransparencySoftware", Software2, "vendor", Text("Adopted vendor")));
        var right = Prepare(receiver, root, "parking-minimum", Edit("ParkingProduct", "long-stay", "minDuration", Text("900 s")));
        Publish(sender, left); Publish(receiver, right); Store(sender, right);
        var merge = Merge(sender, left, right); Publish(sender, merge);
        Status(receiver, POIRuntimeTarget.GridOperator("DE*GRD"), "Offline");
        Status(receiver, PoolMeter, "error"); Status(receiver, StationMeter, "reserved");
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), merge.Id, out var pack, out var exported), Is.True, exported.Error);
        Assert.That(receiver.TryImportCommitPack(Transport(pack!, cbor), out var imported), Is.True, imported.Error);
        var before = new Observation(receiver);
        Assert.That(receiver.TryAdoptHead(right.Id, merge.Id, out var preview), Is.True, preview.Error);
        before.AssertUnchanged(receiver);
        var old = receiver.Head.Network;
        Assert.That(receiver.TryAdoptHead(right.Id, merge.Id, out var adopted, adopt: true), Is.True, adopted.Error);
        var current = receiver.Head.Network;
        AssertShared(current);
        Assert.That(current.GridOperators.Single().Status.Value, Is.EqualTo(GridOperatorStatusTypes.Offline));
        Assert.That(current.ChargingPools.Single(pool => pool.Id.ToString() == Pool1).EnergyMeters[0].Status.Value.ToString(), Is.EqualTo("error"));
        Assert.That(current.ChargingStations.Single().EnergyMeters[0].Status.Value.ToString(), Is.EqualTo("reserved"));
        Assert.That(current.GetTransparencySoftwareById(TransparencySoftware_Id.Parse(Software2))!.Vendor, Is.EqualTo("Adopted vendor"));
        Assert.That(current.ETags, Is.EqualTo(sender.Head.Network.ETags));
        Status(receiver, POIRuntimeTarget.GridOperator("DE*GRD"), "Available", 3);
        Assert.That(old.GridOperators.Single().Status.Value, Is.EqualTo(GridOperatorStatusTypes.Offline));
        using var restored = RoamingNetworkHistory.ParseCBOR(receiver.ToCBOR(), VerifyBatch, VerifyCommit);
        AssertShared(restored.Head.Network);
        Assert.That(restored.Head.Network.GridOperators.Single().Status.Value, Is.Not.EqualTo(GridOperatorStatusTypes.Offline));
        Assert.That(restored.Head.Id, Is.EqualTo(merge.Id));
    }

    [TestCase("software")]
    [TestCase("grid")]
    [TestCase("product")]
    public void Divergent_deletion_and_new_reference_report_typed_conflicts_and_allow_explicit_resolution(String kind)
    {
        var source = Source();
        if (kind == "software") source = Derive(source, RoamingNetworkChange.Add("TransparencySoftware", "unused", Value("""
            {"@id":"unused","name":"Unused","version":"1","vendor":"Vendor","openSourceLicense":{"@id":"MIT"}}
            """), "RoamingNetwork", source.Id.ToString()));
        if (kind == "product") source = Derive(source, RoamingNetworkChange.Add("ParkingProduct", "unused", Value("""{"@id":"unused"}"""), "ParkingOperator", "parking-op"));
        if (kind == "grid") source = Derive(source, RoamingNetworkChange.Add("ChargingPool", "DE*ABC*P3", Value("""{"@id":"DE*ABC*P3"}"""), "ChargingStationOperator", "DE*ABC"));
        using var history = History(source); var root = history.Head.Id;
        RoamingNetworkChange[] removals = kind switch {
            "software" => [RoamingNetworkChange.Remove("TransparencySoftware", "unused")],
            "product" => [RoamingNetworkChange.Remove("ParkingProduct", "unused")],
            _ => [RoamingNetworkChange.RemoveProperty("ChargingPool", Pool1, "gridConnectionPoint"),
                  RoamingNetworkChange.RemoveProperty("ChargingPool", Pool2, "gridConnectionPoint"),
                  RoamingNetworkChange.Remove("GridOperator", "DE*GRD")]
        };
        var addition = kind switch {
            "software" => RoamingNetworkChange.AddElement("TransparencySoftwareCertificate", Certificate42,
                [new("compatibleTransparencySoftwareIds", "unused")], Text("unused")),
            "product" => RoamingNetworkChange.AddElement("ParkingSpace", "space-1", [new("parkingProductIds", "unused")], Text("unused")),
            _ => Edit("ChargingPool", "DE*ABC*P3", "gridConnectionPoint", Value("""{"gridOperatorId":"DE*GRD"}"""))
        };
        var left = Prepare(history, root, "delete", removals); var right = Prepare(history, root, "reference", addition);
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var absent, out var report), Is.False);
        Assert.That(absent, Is.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.Conflicts));
        var conflict = report.Conflicts.Single();
        Assert.That(conflict.Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.Reference));
        Assert.That(conflict.RelatedEntity!.Value.Type, Is.EqualTo(kind switch {
            "software" => InfrastructureEntityType.TransparencySoftware,
            "product" => InfrastructureEntityType.ParkingProduct, _ => InfrastructureEntityType.GridOperator
        }));
        Assert.That(conflict.PropertyName, Is.EqualTo(kind switch {
            "software" => "compatibleTransparencySoftwareIds", "product" => "parkingProductIds", _ => "gridConnectionPoint"
        }));
        before.AssertUnchanged(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var resolved, out report, merge: true,
            mergedChangeSetId: "resolved-domain", createdAt: InteropFixture.Time.AddDays(1), resolveConflict: _ => RoamingNetworkMergeResolution.UseLeft), Is.True, report.Message);
        before.AssertUnchanged(history);
        var signed = Sign(resolved!.WithChangeSet(Sign(resolved.ChangeSet!))); Publish(history, signed);
        using var recovered = RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit);
        Assert.That(recovered.Head.Id, Is.EqualTo(signed.Id)); Assert.That(recovered.Head.Snapshot.ETags, Is.EqualTo(report.AfterETags));
    }
}
