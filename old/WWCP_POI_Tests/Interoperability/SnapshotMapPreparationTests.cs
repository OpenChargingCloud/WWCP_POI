/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Reflection;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WWCP_POI_Benchmarks;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SnapshotMapPreparationTests
{
    private static RoamingNetwork Network()
    {
        var document = DomainRecoveryFixture.CreateDocument(16);
        var owner = document["chargingStationOperators"]![0]!;
        foreach (var (field, memberField, member, id) in new[] {
            ("EVSEGroups", "EVSEIds", "DE*ABC*E1", "DE*ABC*EG1"), ("chargingStationGroups", "chargingStationIds", "DE*ABC*S1", "DE*ABC*SG1"),
            ("chargingPoolGroups", "chargingPoolIds", "DE*ABC*P1", "DE*ABC*PG1"), ("chargingTariffGroups", "chargingTariffIds", "DE*ABC*T1", "DE*ABC*TG1") })
        {
            var group = new JObject { ["@id"] = id, [memberField] = new JArray(member),
                ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" };
            if (field != "chargingTariffGroups") group["name"] = new JObject { ["en"] = "Preparation group" };
            owner[field] = new JArray(group);
        }
        document["eMobilityProviders"] = new JArray(new JObject { ["@id"] = "DE*EMP", ["name"] = new JObject { ["en"] = "Preparation provider" },
            ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" });
        document["chargingStationManufacturers"] = new JArray(new JObject { ["@id"] = "maker", ["name"] = new JObject { ["en"] = "Preparation manufacturer" } });
        document["parkingOperators"]![0]!["parkingSensors"] = new JArray(new JObject { ["@id"] = "sensor", ["chargingStationIds"] = new JArray("DE*ABC*S1"),
            ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" });
        return RoamingNetwork.Parse(document);
    }

    private static JObject Node(JObject document, InfrastructureEntityKey key)
    {
        JObject? selected = null;
        POIRepresentation.Visit(document, nameof(RoamingNetwork), "", (node, kind, _) => {
            if (kind == key.Type.ToString() && InfrastructureChangeSchema.SameId(key.Type,
                node[InfrastructureChangeSchema.IdField(key.Type)]!.Value<String>()!, key.Id) &&
                (key.Type != InfrastructureEntityType.ChargingConnector || InfrastructureChangeSchema.SameId(InfrastructureEntityType.EVSE,
                    ((JObject)node.Parent!.Parent!.Parent!)["@id"]!.Value<String>()!, key.Scope!))) selected ??= node;
        });
        return selected!;
    }

    private static void Same(RoamingNetworkDataSnapshot expected, RoamingNetworkDataSnapshot actual)
    {
        Assert.That(actual.Root, Is.EqualTo(expected.Root));
        Assert.That(actual.Revision, Is.EqualTo(expected.Revision));
        Assert.That(actual.AppliedChangeSetId, Is.EqualTo(expected.AppliedChangeSetId));
        Assert.That(actual.Entities.Keys, Is.EquivalentTo(expected.Entities.Keys));
        foreach (var (key, value) in expected.Entities)
        {
            var entity = actual.Entities[key];
            Assert.That(entity.Parent, Is.EqualTo(value.Parent));
            Assert.That(entity.Children, Is.EquivalentTo(value.Children));
            Assert.That(entity.Properties.Keys, Is.EquivalentTo(value.Properties.Keys));
            foreach (var (field, property) in value.Properties)
                Assert.That(entity.Properties[field].GetRawText(), Is.EqualTo(property.GetRawText()), key + "/" + field);
        }
        Assert.That(actual.References.Keys, Is.EquivalentTo(expected.References.Keys));
        foreach (var (key, consumers) in expected.References)
            Assert.That(actual.References[key], Is.EquivalentTo(consumers));
        Assert.That(actual.ToCanonicalJSON(), Is.EqualTo(expected.ToCanonicalJSON()));
        Assert.That(actual.ToCanonicalCBOR(), Is.EqualTo(expected.ToCanonicalCBOR()));
        Assert.That(actual.ETags, Is.EqualTo(expected.ETags));
    }

    public static IEnumerable<InfrastructureEntityType> Kinds() => Network().DataSnapshot.Entities.Keys.Select(key => key.Type).Distinct().Order();

    [TestCaseSource(nameof(Kinds))]
    public void Changed_property_reuses_other_entities_property_values_children_and_reference_catalogs(InfrastructureEntityType type)
    {
        var snapshot = Network().DataSnapshot;
        var key = snapshot.Entities.Keys.Where(key => key.Type == type).OrderBy(key => key.Id, StringComparer.Ordinal).First();
        var field = type switch {
            InfrastructureEntityType.ChargingConnector => "lockable",
            InfrastructureEntityType.ParkingProduct => "stopParkingAfterTime",
            InfrastructureEntityType.TransparencySoftware => "version",
            InfrastructureEntityType.TransparencySoftwareCertificate => "documentNumber",
            _ => "description"
        };
        var document = snapshot.GetDocument();
        Node(document, key)[field] = field == "lockable" ? new JValue(!Node(document, key)[field]!.Value<Boolean>()) : field == "stopParkingAfterTime" ? new JValue("10800 s") :
            field == "description" ? new JObject { ["en"] = "<changed>" } : new JValue("changed");
        var before = snapshot.ToCanonicalJSON(); var input = document.ToString();
        var expected = RoamingNetworkDataSnapshot.Capture(document, 17, "prepared");
        var actual = RoamingNetworkDataSnapshot.Capture(document, 17, "prepared", snapshot);
        Same(expected, actual);
        Assert.That(actual.Entities[key], Is.Not.SameAs(snapshot.Entities[key]));
        Assert.That(actual.Entities[key].Children, Is.SameAs(snapshot.Entities[key].Children));
        foreach (var (unchanged, value) in snapshot.Entities[key].Properties.Where(property => property.Key != field))
            Assert.That(actual.Entities[key].Properties[unchanged].Equals(value), Is.True, key + "/" + unchanged);
        foreach (var entity in snapshot.Entities.Values.Where(entity => entity.Key != key))
            Assert.That(actual.Entities[entity.Key], Is.SameAs(entity));
        Assert.That(actual.References, Is.SameAs(snapshot.References));
        Assert.That(snapshot.ToCanonicalJSON(), Is.EqualTo(before)); Assert.That(document.ToString(), Is.EqualTo(input));
    }

    [TestCase(0L)] [TestCase(17L)] [TestCase(Int64.MaxValue)]
    public void Exact_normalized_capture_reuses_maps_without_reusing_revision_bookkeeping(Int64 revision)
    {
        var snapshot = Network().DataSnapshot;
        var actual = RoamingNetworkDataSnapshot.Capture(snapshot.GetDocument(), revision, "new-bookkeeping", snapshot);
        Same(RoamingNetworkDataSnapshot.Capture(snapshot.GetDocument(), revision, "new-bookkeeping"), actual);
        Assert.That(actual, Is.Not.SameAs(snapshot));
        Assert.That(actual.Entities, Is.SameAs(snapshot.Entities)); Assert.That(actual.References, Is.SameAs(snapshot.References));
        Assert.That(actual.ChildETags, Is.SameAs(snapshot.ChildETags));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Added_removed_or_moved_children_keep_the_ordinary_capture_and_update_only_affected_maps(Int32 variant)
    {
        var snapshot = Network().DataSnapshot; var document = snapshot.GetDocument();
        var stations = (JArray)document["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]!;
        var evses = (JArray)stations[0]!["EVSEs"]!;
        if (variant == 0) { var added = (JObject)evses[0]!.DeepClone(); added["@id"] = "DE*ABC*Eadded"; evses.Add(added); }
        else if (variant == 1) evses.RemoveAt(evses.Count - 1);
        else { var moved = evses.Last!; moved.Remove(); ((JArray)stations[1]!["EVSEs"]!).Add(moved); }
        var expected = RoamingNetworkDataSnapshot.Capture(document, 5);
        var actual = RoamingNetworkDataSnapshot.Capture(document, 5, reuse: snapshot); Same(expected, actual);
        foreach (var key in snapshot.Entities.Keys.Where(key => key.Type is InfrastructureEntityType.GridOperator or InfrastructureEntityType.ChargingTariff))
            Assert.That(actual.Entities[key], Is.SameAs(snapshot.Entities[key]));
        Assert.That(actual.References, variant == 2 ? Is.SameAs(snapshot.References) : Is.Not.SameAs(snapshot.References));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void Invalid_imports_keep_error_type_message_and_source_and_do_not_poison_retry(Int32 variant)
    {
        var snapshot = Network().DataSnapshot; var before = snapshot.ToCanonicalJSON(); var document = snapshot.GetDocument();
        var owner = document["chargingStationOperators"]![0]!; var station = owner["chargingPools"]![0]!["chargingStations"]![0]!;
        var evse = (JObject)station["EVSEs"]![0]!;
        if (variant == 0) ((JArray)station["EVSEs"]!).Add(evse.DeepClone());
        if (variant == 1) evse.Remove("@id");
        if (variant == 2) evse["unknownField"] = true;
        if (variant == 3) evse["chargingStationId"] = "DE*ABC*Swrong";
        if (variant == 4) evse["tariffIds"] = new JArray("DE*ABC*T1", "DE*ABC*T1");
        if (variant == 5) evse["tariffIds"] = new JArray("DE*ABC*Tmissing");
        if (variant == 6) evse["tariffIds"] = new JArray(false);
        if (variant == 7) ((JObject)owner["chargingPools"]![0]!["gridConnectionPoint"]!)["gridOperatorId"] = "missing-grid";
        var input = document.ToString();
        var expected = Assert.Catch(() => RoamingNetworkDataSnapshot.Capture(document, 1))!;
        var actual = Assert.Catch(() => RoamingNetworkDataSnapshot.Capture(document, 1, reuse: snapshot))!;
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.That(snapshot.ToCanonicalJSON(), Is.EqualTo(before)); Assert.That(document.ToString(), Is.EqualTo(input));
        Same(snapshot, RoamingNetworkDataSnapshot.Capture(snapshot.GetDocument(), snapshot.Revision, snapshot.AppliedChangeSetId, snapshot));
    }

    [TestCase(16, 1)] [TestCase(16, 2)] [TestCase(16, 3)] [TestCase(16, 4)]
    [TestCase(64, 1)] [TestCase(64, 2)] [TestCase(64, 3)] [TestCase(64, 4)]
    public void Replay_normalizes_escaped_timestamps_and_shares_every_other_property_and_catalog(Int32 evses, Int32 profile)
    {
        using var fixture = new DomainRecoveryFixture(evses, profile);
        var normalized = 0;
        foreach (var commit in fixture.History.Commits)
        {
            var source = fixture.History.GetSnapshot(commit.Id); var before = source.ToCanonicalJSON();
            var expected = RoamingNetwork.Parse(source.GetDocument());
            var actual = RoamingNetwork.ParseSnapshot(source); Same(expected.DataSnapshot, actual.DataSnapshot);
            foreach (var (key, entity) in source.Entities)
            {
                var changed = entity.Properties.Any(property => property.Value.GetRawText() != actual.DataSnapshot.Entities[key].Properties[property.Key].GetRawText());
                if (!changed) Assert.That(actual.DataSnapshot.Entities[key], Is.SameAs(entity));
                else
                {
                    normalized++;
                    Assert.That(actual.DataSnapshot.Entities[key].Children, Is.SameAs(entity.Children));
                    foreach (var (field, value) in entity.Properties)
                        if (value.GetRawText() == actual.DataSnapshot.Entities[key].Properties[field].GetRawText())
                            Assert.That(actual.DataSnapshot.Entities[key].Properties[field].Equals(value), Is.True, key + "/" + field);
                }
            }
            Assert.That(actual.DataSnapshot.References, Is.SameAs(source.References));
            Assert.That(source.ToCanonicalJSON(), Is.EqualTo(before));
        }
        Assert.That(normalized, Is.GreaterThan(0));
    }

    [Test]
    public void Direct_binding_identities_equal_the_preceding_full_projection_for_every_owned_kind_and_nested_value()
    {
        var network = Network();
        var children = typeof(POISnapshotRepresentation).GetMethod("Children", BindingFlags.NonPublic | BindingFlags.Static)!;
        var identity = typeof(POISnapshotRepresentation).GetMethod("Identity", BindingFlags.NonPublic | BindingFlags.Static, [typeof(IImmutablePOI)])!;
        var kinds = new HashSet<String>(); var count = 0;
        void Visit(IImmutablePOI value)
        {
            kinds.Add(value.GetType().Name); count++;
            var document = POIRepresentation.WithoutETags(() => POIJSON.Document(value));
            var id = (document["@id"] ?? document["id"])?.Value<String>();
            var expected = id is null ? null : Enum.TryParse<InfrastructureEntityType>(value.GetType().Name, out var type)
                ? InfrastructureChangeSchema.Identity(type, id) : value is EnergyMeter ? EnergyMeter_Id.Parse(id).ToString().ToUpperInvariant() : id;
            Assert.That(identity.Invoke(null, [value]), Is.EqualTo(expected), value.GetType().Name);
            foreach (var (_, child) in (IEnumerable<(String, IImmutablePOI)>)children.Invoke(null, [value])!) Visit(child);
        }
        Visit(network);
        // Transparency software and its documents are values of the network, bound with it.
        Assert.That(kinds.Intersect(Enum.GetNames<InfrastructureEntityType>()).Count(), Is.EqualTo(20));
        Assert.That(kinds, Does.Contain(nameof(EnergyMeter)).And.Contain(nameof(ChargingTariffElement)).And.Contain(nameof(ChargingPriceComponent)));
        Assert.That(count, Is.GreaterThan(100));
    }

    [TestCase("1.0")] [TestCase("1e0")] [TestCase("-0")] [TestCase("12345678901234567890123456789")]
    public void Customer_numbers_escaped_strings_nested_arrays_and_SI_units_follow_exact_ordinary_capture(String number)
    {
        var snapshot = Network().DataSnapshot; var document = snapshot.GetDocument();
        document["customData"] = InfrastructureJson.ReadObject("{\"number\":" + number + ",\"text\":\"\\u003Ccustomer\\u003E\",\"array\":[null,[],{}],\"reading\":\"250 kW\",\"ETags\":[\"customer\"]}");
        Same(RoamingNetworkDataSnapshot.Capture(document, 3), RoamingNetworkDataSnapshot.Capture(document, 3, reuse: snapshot));
    }

    [TestCase(InfrastructureEntityType.EVSE)] [TestCase(InfrastructureEntityType.ChargingStation)]
    [TestCase(InfrastructureEntityType.ChargingStationOperator)]
    public void Equivalent_identity_aliases_preserve_the_ordinary_visible_key_parent_and_child_spellings(InfrastructureEntityType type)
    {
        var snapshot = Network().DataSnapshot; var document = snapshot.GetDocument();
        var key = snapshot.Entities.Keys.First(key => key.Type == type);
        Node(document, key)[InfrastructureChangeSchema.IdField(type)] = key.Id.Replace("DE*ABC", "DEABC", StringComparison.Ordinal);
        var expected = RoamingNetworkDataSnapshot.Capture(document, 1);
        var actual = RoamingNetworkDataSnapshot.Capture(document, 1, reuse: snapshot); Same(expected, actual);
        Assert.That(actual.Entities.Keys.Select(key => (key.Type, key.Id, key.Scope)), Is.EquivalentTo(expected.Entities.Keys.Select(key => (key.Type, key.Id, key.Scope))));
        foreach (var (id, value) in expected.Entities)
        {
            Assert.That(actual.Entities[id].Key.Id, Is.EqualTo(value.Key.Id));
            Assert.That(actual.Entities[id].Key.Scope, Is.EqualTo(value.Key.Scope));
            Assert.That(actual.Entities[id].Parent?.Id, Is.EqualTo(value.Parent?.Id));
            Assert.That(actual.Entities[id].Children.Select(child => (child.Type, child.Id, child.Scope)),
                Is.EquivalentTo(value.Children.Select(child => (child.Type, child.Id, child.Scope))));
        }
    }

    [Test]
    public void Missing_properties_are_removed_while_other_property_values_and_child_maps_remain_shared()
    {
        var snapshot = Network().DataSnapshot; var document = snapshot.GetDocument();
        var key = snapshot.Entities.Keys.First(key => key.Type == InfrastructureEntityType.ChargingConnector);
        Node(document, key).Remove("lockable");
        var actual = RoamingNetworkDataSnapshot.Capture(document, 1, reuse: snapshot);
        Same(RoamingNetworkDataSnapshot.Capture(document, 1), actual);
        Assert.That(actual.Entities[key].Properties.ContainsKey("lockable"), Is.False);
        Assert.That(actual.Entities[key].Children, Is.SameAs(snapshot.Entities[key].Children));
        foreach (var (field, value) in actual.Entities[key].Properties)
            Assert.That(value.Equals(snapshot.Entities[key].Properties[field]), Is.True);
    }
}
