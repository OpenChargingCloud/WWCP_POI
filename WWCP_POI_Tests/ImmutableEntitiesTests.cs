/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests;

[TestFixture]
public sealed class ImmutableEntitiesTests
{
    private static RoamingNetwork Network() => RoamingNetwork.Parse("""
        {
          "@id":"immutable-network", "name":{"en":"Network"},
          "status":{"value":"available","timestamp":"2026-01-01T00:00:00Z"},
          "eMobilityProviders":[{"@id":"DE-GHI","name":{"en":"Provider"}}],
          "chargingStationOperators":[{
            "@id":"DE*ABC", "name":{"en":"Operator"},
            "chargingTariffs":[{"@id":"DE*ABC*T1", "currency":"EUR",
              "elements":[{"priceComponents":[{"type":"ENERGY","price":0.25,"stepSize":1000}]}]}],
            "chargingPools":[{"@id":"DE*ABC*P1", "chargingStations":[{
              "@id":"DE*ABC*S1", "EVSEs":[{"@id":"DE*ABC*E1","maxPower":"100000 W",
                "status":{"value":"available","timestamp":"2026-01-01T00:00:00Z"},
                "adminStatus":{"value":"operational","timestamp":"2026-01-01T00:00:00Z"},
                "currentType":["DC"], "socketOutlets":[{"@id":"1","type":"CCS"}]}]
            }]}]
          }]
        }
        """);

    private static RoamingNetworkChangeSet Batch(RoamingNetwork source, params RoamingNetworkChange[] changes)
        => source.CreateChangeSet("immutable-change", DateTimeOffset.UtcNow, [.. changes]);
    private static JsonElement Json(String value) => JsonSerializer.Deserialize<JsonElement>(value);

    [TestCase(typeof(ChargingConnector))]
    [TestCase(typeof(EVSE))]
    [TestCase(typeof(ChargingStation))]
    [TestCase(typeof(ChargingPool))]
    [TestCase(typeof(ChargingStationOperator))]
    [TestCase(typeof(EMobilityProvider))]
    [TestCase(typeof(RoamingNetwork))]
    [TestCase(typeof(ChargingTariff))]
    public void Static_data_has_no_public_mutation_surface(Type type)
    {
        var dynamic = new HashSet<String> { "Status", "AdminStatus", "LastStatusUpdate", "StatusAggregationDelegate" };
        Assert.That(type.IsSealed, Is.True);
        Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.SetMethod?.IsPublic == true)
            .Where(property => !dynamic.Contains(property.Name) && !property.Name.EndsWith("RealTime") && !property.Name.EndsWith("Prognoses")), Is.Empty);
        Assert.That(type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .Where(method => method.Name.StartsWith("Add") || method.Name.StartsWith("Remove") ||
                             method.Name.StartsWith("GetOrCreate") || method.Name == "UpdateWith"), Is.Empty);
    }

    [Test]
    public void Constructor_detaches_text_and_connector_enumerations_and_parent_links()
    {
        var name = I18NString.Create("Original");
        var tariffs = new List<ChargingTariff_Id> { ChargingTariff_Id.Parse("DE*ABC*T1") };
        var connector = new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.Parse("CCS"), TariffIds: tariffs);
        var connectors = new List<ChargingConnector> { connector };
        var station = Network().ChargingStations.Single();
        var evse = new EVSE(EVSE_Id.Parse("DE*ABC*E2"), station, Name: name, ChargingConnectors: connectors);
        name.Set(Languages.en, "Changed");
        tariffs.Clear();
        connectors.Clear();
        Assert.That(evse.Name.FirstText(), Is.EqualTo("Original"));
        Assert.That(evse.ChargingConnectors.Single().TariffIds.Single(), Is.EqualTo(ChargingTariff_Id.Parse("DE*ABC*T1")));
        Assert.That(connector.EVSE, Is.Null);
        Assert.That(evse.ChargingConnectors.Single().EVSE, Is.SameAs(evse));
        var other = new EVSE(EVSE_Id.Parse("DE*ABC*E3"), station, ChargingConnectors: evse.ChargingConnectors);
        Assert.That(other.ChargingConnectors.Single().EVSE, Is.SameAs(other));
        Assert.That(evse.ChargingConnectors.Single().EVSE, Is.SameAs(evse));
    }

    [Test]
    public void Immutable_opening_hours_detach_input_and_mutable_exports()
    {
        var hours = new OpeningTimes("Original");
        hours.AddRegularOpening(DayOfWeek.Monday, HourMin.Parse("08:00"), HourMin.Parse("18:00"));
        var station = new ChargingStation(ChargingStation_Id.Parse("DE*ABC*S2"), OpeningTimes: hours);
        hours.FreeText = "Changed";
        hours.AddRegularOpening(DayOfWeek.Tuesday, HourMin.Parse("08:00"), HourMin.Parse("18:00"));
        station.OpeningTimes.ToMutable().FreeText = "Changed export";
        Assert.That(station.OpeningTimes.FreeText, Is.EqualTo("Original"));
        Assert.That(station.OpeningTimes.RegularOpenings.Keys, Is.EquivalentTo(new[] { DayOfWeek.Monday }));
    }

    [Test]
    public void Dependency_metadata_casts_cannot_modify_POI_timestamps()
    {
        var network = Network();
        var created = network.Created;
        var lastChange = network.LastChangeDate;
        var dependency = (IInternalData) network;
        Assert.Throws<InvalidOperationException>(() => dependency.Created = DateTimeOffset.MinValue);
        Assert.Throws<InvalidOperationException>(() => dependency.LastChangeDate = DateTimeOffset.MaxValue);
        ((IEntity) network).Name.Set(Languages.en, "Detached");
        Assert.That(network.Name.FirstText(), Is.EqualTo("Network"));
        Assert.That(network.Created, Is.EqualTo(created));
        Assert.That(network.LastChangeDate, Is.EqualTo(lastChange));
    }

    [Test]
    public void Dynamic_status_is_live_in_exports_without_changing_revision_or_POI_metadata()
    {
        var network = Network();
        var snapshot = network.DataSnapshot;
        var evse = network.EVSEs.Single();
        var lastChange = evse.LastChangeDate;
        evse.SetStatus(new Timestamped<EVSEStatusType>(DateTimeOffset.UtcNow.AddSeconds(-1), EVSEStatusType.Charging));
        evse.SetAdminStatus(new Timestamped<EVSEAdminStatusType>(DateTimeOffset.UtcNow.AddSeconds(-1), EVSEAdminStatusType.OutOfService));
        Assert.That(network.DataSnapshot, Is.SameAs(snapshot));
        Assert.That(network.Revision, Is.Zero);
        Assert.That(evse.LastChangeDate, Is.EqualTo(lastChange));
        var restored = RoamingNetwork.Parse(network.ToJSONSnapshot());
        Assert.That(restored.EVSEs.Single().Status.Value, Is.EqualTo(EVSEStatusType.Charging));
        Assert.That(restored.EVSEs.Single().AdminStatus.Value, Is.EqualTo(EVSEAdminStatusType.OutOfService));
    }

    [Test]
    public void Derived_versions_capture_status_history_and_measurements_before_lazy_materialization()
    {
        var source = Network();
        _ = source.DataSnapshot;
        var evse = source.EVSEs.Single();
        evse.SetStatus(new Timestamped<EVSEStatusType>(DateTimeOffset.UtcNow.AddSeconds(-2), EVSEStatusType.Charging));
        evse.MaxPowerRealTime = new Timestamped<Watt>(DateTimeOffset.UtcNow.AddSeconds(-2), Watt.Parse("90000"));
        source.SetStatus(new Timestamped<RoamingNetworkStatusType>(DateTimeOffset.UtcNow.AddSeconds(-2), RoamingNetworkStatusType.Offline));
        var before = evse.StatusSchedule().ToImmutableArray();
        var next = source.ApplyChangeSet(Batch(source,
            RoamingNetworkChange.UpdateProperty("EVSE", evse.Id.ToString(), "maxPower", Json("\"100000 W\""), Json("\"150000 W\""))));
        Assert.That(next.Status.Value, Is.EqualTo(RoamingNetworkStatusType.Offline));
        evse.SetStatus(new Timestamped<EVSEStatusType>(DateTimeOffset.UtcNow.AddSeconds(-1), EVSEStatusType.Available));
        var derivedEVSE = next.EVSEs.Single();
        Assert.That(derivedEVSE.Status.Value, Is.EqualTo(EVSEStatusType.Charging));
        Assert.That(derivedEVSE.StatusSchedule(), Is.EqualTo(before));
        Assert.That(derivedEVSE.MaxPowerRealTime!.Value.Value, Is.EqualTo(evse.MaxPowerRealTime!.Value.Value));
        derivedEVSE.SetStatus(new Timestamped<EVSEStatusType>(DateTimeOffset.UtcNow, EVSEStatusType.OutOfService));
        Assert.That(evse.Status.Value, Is.EqualTo(EVSEStatusType.Available));
        Assert.That(next.Revision, Is.EqualTo(1));
    }

    [Test]
    public void Add_then_remove_in_one_batch_is_valid_with_runtime_state_capture()
    {
        var source = Network();
        var result = source.ApplyChangeSet(Batch(source,
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E2", Json("""{"@id":"DE*ABC*E2","currentType":["DC"]}"""), "ChargingStation", "DE*ABC*S1"),
            RoamingNetworkChange.Remove("EVSE", "DE*ABC*E2")));
        Assert.That(result.EVSEs.Select(evse => evse.Id), Is.EquivalentTo(source.EVSEs.Select(evse => evse.Id)));
    }
}
