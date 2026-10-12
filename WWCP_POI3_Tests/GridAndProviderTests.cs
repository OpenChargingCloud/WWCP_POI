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

#region Usings

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// E-mobility providers and grid operators embedded in the roaming network,
    /// grid connection points and energy meters embedded in the charging infrastructure.
    /// </summary>
    [TestFixture]
    public class GridAndProviderTests
    {

        #region Data

        private static readonly DateTimeOffset          Created   = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset          T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF   = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id         P1        = ChargingPool_Id.        Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id      S1        = ChargingStation_Id.     Parse("DE*GEF*S1");
        private static readonly EVSE_Id                 E1        = EVSE_Id.                Parse("DE*GEF*E1*1");
        private static readonly EMobilityProvider_Id    EMP       = EMobilityProvider_Id.   Parse("DE*EMP");
        private static readonly GridOperator_Id         NET       = GridOperator_Id.        Parse("DE*NET");
        private static readonly GridConnectionPoint_Id  GCP       = GridConnectionPoint_Id. Parse("connection-42");
        private static readonly EnergyMeter_Id          GridMeter = EnergyMeter_Id.         Parse("grid-meter");
        private static readonly EnergyMeter_Id          PVMeter   = EnergyMeter_Id.         Parse("pv-meter");
        private static readonly EnergyMeter_Id          EVSEMeter = EnergyMeter_Id.         Parse("evse-meter");
        private static readonly EnergyMeterManufacturer_Id  ACME  = EnergyMeterManufacturer_Id.Parse("ACME");
        private static readonly EnergyMeterModel_Id     ACME3000  = EnergyMeterModel_Id.    Parse("ACME-3000");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new (RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    EMobilityProviders:        [ new EMobilityProvider(EMP,
                                                                       I18NString.Create(Languages.en, "Example Mobility"),
                                                                       Created: Created) ],
                    EnergyMeterManufacturers:  [ new EnergyMeterManufacturer(ACME,
                                                                             new ImmutableI18NString(I18NString.Create(Languages.en, "ACME Meters")),
                                                                             Homepage:  URL.Parse("https://meters.example.org"),
                                                                             Created:   Created) ],
                    GridOperators:             [ new GridOperator(NET,
                                                                  I18NString.Create(Languages.en, "Example Netz"),
                                                                  Created: Created) ],
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             I18NString.Create(Languages.en, "GraphDefined"),
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(P1,
                                                                                                                Created:              Created,
                                                                                                                GridConnectionPoint:  new GridConnectionPoint(GCP,
                                                                                                                                                              NET,
                                                                                                                                                              VoltageLevel:                   GridVoltageLevel.LowVoltage,
                                                                                                                                                              ConnectionType:                 GridConnectionTypes.AC_ThreePhases,
                                                                                                                                                              NominalVoltage:                 Volt.FromV(400),
                                                                                                                                                              NominalFrequency:               Hertz.FromHz(50),
                                                                                                                                                              ContractedImportPower:          Watt.FromW(250000),
                                                                                                                                                              ContractedExportPower:          Watt.FromW(100000),
                                                                                                                                                              ContractedImportApparentPower:  VoltAmpere.FromVA(300000),
                                                                                                                                                              MarketLocationIds:              [ "DE0001", "DE0002" ],
                                                                                                                                                              EnergyMeter:                    new EnergyMeter(GridMeter, Role: "grid", Created: Created)),
                                                                                                                EnergyMeters:         [ new EnergyMeter(PVMeter, Role: "pv", SerialNumber: "PV-1", Created: Created) ],
                                                                                                                ChargingStations:     [ new ChargingStation(S1,
                                                                                                                                                            Created:  Created,
                                                                                                                                                            EVSEs:    [ new EVSE(E1,
                                                                                                                                                                                 EnergyMeter:  new EnergyMeter(EVSEMeter, ManufacturerId: ACME, ModelId: ACME3000, Created: Created),
                                                                                                                                                                                 Created:      Created) ]) ]) ]) ]);

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Providers_grid_operators_and_meters_survive_JSON_and_CBOR()

        [Test]
        public void Providers_grid_operators_and_meters_survive_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var pool  = json["chargingStationOperators"]![0]!["chargingPools"]![0]!;
            var gcp   = pool["gridConnectionPoint"]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                           Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                           Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),                                    Is.EqualTo(rn1.CalcETags()));

                Assert.That(json["eMobilityProviders"]![0]!["@id"]!.ToString(),      Is.EqualTo("DE*EMP"));
                Assert.That(json["gridOperators"]![0]!["@id"]!.ToString(),           Is.EqualTo("DE*NET"));
                Assert.That(gcp["gridOperatorId"]!.ToString(),                        Is.EqualTo("DE*NET"));
                Assert.That(gcp["voltageLevel"]!.ToString(),                          Is.EqualTo("LowVoltage"));
                Assert.That(gcp["nominalFrequency"]!.Value<Decimal>(),                Is.EqualTo(50));
                Assert.That(gcp["contractedImportPower"]!.Value<Decimal>(),           Is.EqualTo(250000));
                Assert.That(gcp["marketLocationIds"]!.Values<String>(),               Is.EqualTo(new[] { "DE0001", "DE0002" }));
                Assert.That(gcp["energyMeter"]!["role"]!.ToString(),                  Is.EqualTo("grid"));
                Assert.That(pool["energyMeters"]![0]!["serialNumber"]!.ToString(),    Is.EqualTo("PV-1"));

                Assert.That(fromJSON.TryGetEMobilityProviderById(EMP, out _),        Is.True);
                Assert.That(fromJSON.TryGetGridOperatorById(NET, out _),             Is.True);
                Assert.That(fromJSON.EnergyMeters.Select(meter => meter.Id.ToString()).Order(),
                            Is.EqualTo(new[] { "evse-meter", "grid-meter", "pv-meter" }));
                Assert.That(fromJSON.TryGetEnergyMeterById(EVSEMeter, out var evseMeter),  Is.True);
                Assert.That(evseMeter!.ManufacturerId,                               Is.EqualTo(ACME));
                Assert.That(evseMeter.ModelId,                                       Is.EqualTo(ACME3000));
                Assert.That(fromCBOR.TryGetEnergyMeterManufacturerById(ACME, out var acme),  Is.True);
                Assert.That(acme!.Homepage.ToString(),                               Is.EqualTo("https://meters.example.org"));
                Assert.That(json["energyMeterManufacturers"]![0]!["name"]!["en"]!.ToString(),  Is.EqualTo("ACME Meters"));

            }

        }

        #endregion

        #region Invalid_grids_and_meters_are_refused()

        [Test]
        public void Invalid_grids_and_meters_are_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject Pool(JObject Network)
                => (JObject) Network["chargingStationOperators"]![0]!["chargingPools"]![0]!;

            JObject Point(JObject Network)
                => (JObject) Pool(Network)["gridConnectionPoint"]!;

            var unknownGridOperator  = Changed(network => network.Remove("gridOperators"));
            var meterTwice           = Changed(network => Point(network)["energyMeter"]!["@id"] = "pv-meter");
            var embeddedOperator     = Changed(network => Point(network)["gridOperator"] = new JObject(new JProperty("@id", "DE*NET")));
            var tooMuchActivePower   = Changed(network => Point(network)["contractedImportPower"] = 400000);
            var negativeVoltage      = Changed(network => Point(network)["nominalVoltage"] = -400);
            var unknownVoltageLevel  = Changed(network => Point(network)["voltageLevel"] = "lowVoltage");
            var numericVoltageLevel  = Changed(network => Point(network)["voltageLevel"] = "1");
            var marketTwice          = Changed(network => Point(network)["marketLocationIds"] = new JArray("DE0001", "DE0001"));

            // The same meter in two operators.
            var otherOperator        = Changed(network => {
                                           var gef2 = (JObject) network["chargingStationOperators"]![0]!.DeepClone();
                                           gef2["@id"] = "DE*XYZ";
                                           var pool = (JObject) gef2["chargingPools"]![0]!;
                                           pool["@id"] = "DE*XYZ*P1";
                                           pool.Remove("gridConnectionPoint");
                                           pool.Remove("chargingStations");
                                           ((JArray) network["chargingStationOperators"]!).Add(gef2);
                                       });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetwork.TryParse(unknownGridOperator,  out _, out var error1),  Is.False);
                Assert.That(error1,                                                                Does.Contain("grid operator 'DE*NET'"));
                Assert.That(RoamingNetwork.TryParse(meterTwice,           out _, out var error2),  Is.False);
                Assert.That(error2,                                                                Does.Contain("energy meter"));
                Assert.That(RoamingNetwork.TryParse(embeddedOperator,     out _, out var error3),  Is.False);
                Assert.That(error3,                                                                Does.Contain("gridOperatorId"));
                Assert.That(RoamingNetwork.TryParse(tooMuchActivePower,   out _, out var error4),  Is.False);
                Assert.That(error4,                                                                Does.Contain("apparent power"));
                Assert.That(RoamingNetwork.TryParse(negativeVoltage,      out _, out _),           Is.False);
                Assert.That(RoamingNetwork.TryParse(unknownVoltageLevel,  out _, out _),           Is.False);
                Assert.That(RoamingNetwork.TryParse(numericVoltageLevel,  out _, out _),           Is.False);
                Assert.That(RoamingNetwork.TryParse(marketTwice,          out _, out _),           Is.False);
                Assert.That(RoamingNetwork.TryParse(otherOperator,        out _, out var error9),  Is.False);
                Assert.That(error9,                                                                Does.Contain("'pv-meter' must not be given twice in the roaming network"));
            }

        }

        #endregion

        #region Providers_grid_operators_and_meters_are_changed_by_change_sets()

        [Test]
        public void Providers_grid_operators_and_meters_are_changed_by_change_sets()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var rn1      = history.Head.RoamingNetwork;
            var net      = rn1.GridOperators.Single();
            var pool     = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            var e1       = pool.EVSEs.Single();

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EnergyMeter), "grid-meter", "offline", T0, out _, out _);

            var point    = (JObject) pool.GridConnectionPoint!.ToJSON();
            point["contractedImportPower"] = 200000;

            var newMeter = new EnergyMeter(EnergyMeter_Id.Parse("evse-meter-2"), Role: "evse").ToJSON();

            history.Publish(rn1.CreateChangeSet([
                                RoamingNetworkChange.Add           (new EMobilityProvider(EMobilityProvider_Id.Parse("DE*ZAP"))),
                                RoamingNetworkChange.UpdateProperty(net,  "homepage",            null, "https://netz.example"),
                                RoamingNetworkChange.UpdateProperty(pool, "gridConnectionPoint", null, point),
                                RoamingNetworkChange.UpdateProperty(e1,   "energyMeter",         null, newMeter),
                                RoamingNetworkChange.Remove        (EMP)
                            ], CreatedAt: T0.AddMinutes(1)));

            var rn2      = history.Head.RoamingNetwork;
            var pool2    = rn2.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.EMobilityProviders.Select(provider => provider.Id.ToString()),  Is.EqualTo(new[] { "DE*ZAP" }));
                Assert.That(rn2.GridOperators.Single().Homepage?.ToString(),                    Is.EqualTo("https://netz.example"));
                Assert.That(pool2.GridConnectionPoint!.ContractedImportPower,                   Is.EqualTo(Watt.FromW(200000)));
                Assert.That(rn2.TryGetEnergyMeterById(EVSEMeter, out _),                        Is.False);
                Assert.That(rn2.TryGetEnergyMeterById(EnergyMeter_Id.Parse("evse-meter-2"), out _),  Is.True);

                // The grid meter survived the change of its grid connection point, and so did its status.
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EnergyMeter), "grid-meter", out var gridMeterStatus),  Is.True);
                Assert.That(gridMeterStatus!.Value,                                                                                                 Is.EqualTo("offline"));

                // A grid operator still named by a grid connection point can not be removed.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Remove(NET)),
                            Throws.ArgumentException.With.Message.Contains("grid operator 'DE*NET' of the grid connection point"));

                // A meter given twice by a change is refused.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(pool2, "energyMeters", null, new JArray(new EnergyMeter(GridMeter).ToJSON()))),
                            Throws.ArgumentException.With.Message.Contains("energy meter"));

            }

        }

        #endregion

        #region Energy_meter_manufacturers_are_named_and_changed()

        [Test]
        public void Energy_meter_manufacturers_are_named_and_changed()
        {

            var rn1   = NewNetwork();
            var acme  = rn1.EnergyMeterManufacturers.Single();
            var e1    = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single().EVSEs.Single();
            var zap   = EnergyMeterManufacturer_Id.Parse("ZAP");

            var rn2   = Apply(rn1,
                              RoamingNetworkChange.Add           (new EnergyMeterManufacturer(zap)),
                              RoamingNetworkChange.UpdateProperty(acme, "hotline",     null, "+49 30 1234567"),
                              RoamingNetworkChange.UpdateProperty(e1,   "energyMeter", null, new EnergyMeter(EVSEMeter, ManufacturerId: zap).ToJSON()));

            var rn3   = Apply(rn2, RoamingNetworkChange.Remove(ACME));

            rn2.TryGetEnergyMeterManufacturerById(ACME, out var changedAcme);
            rn2.TryGetEnergyMeterById(EVSEMeter, out var changedMeter);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(changedAcme!.HotlinePhoneNumber?.ToString(),                         Is.EqualTo("+49 30 1234567"));
                Assert.That(changedAcme.LastChangeDate,                                          Is.EqualTo(T0));
                Assert.That(changedMeter!.ManufacturerId,                                        Is.EqualTo(zap));
                Assert.That(rn3.EnergyMeterManufacturers.Select(manufacturer => manufacturer.Id), Is.EqualTo(new[] { zap }));

                // A manufacturer still named by an energy meter can not be removed...
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Remove(ACME)),
                            Throws.ArgumentException.With.Message.Contains("unknown energy meter manufacturer 'ACME'"));

                // ...and an energy meter names only manufacturers of its network.
                Assert.That(() => Apply(rn3, RoamingNetworkChange.UpdateProperty(e1, "energyMeter", null, new EnergyMeter(EVSEMeter, ManufacturerId: ACME).ToJSON())),
                            Throws.ArgumentException.With.Message.Contains("unknown energy meter manufacturer 'ACME'"));

                Assert.That(() => new RoamingNetwork(RoamingNetwork_Id.Parse("rn2"),
                                                     ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                                              ChargingPools: [ new ChargingPool(P1, EnergyMeters: [ new EnergyMeter(PVMeter, ManufacturerId: ACME) ]) ]) ]),
                            Throws.ArgumentException.With.Message.Contains("names the unknown energy meter manufacturer 'ACME'"));

            }

        }

        #endregion

        #region The_runtime_status_reaches_providers_grid_operators_and_meters()

        [Test]
        public void The_runtime_status_reaches_providers_grid_operators_and_meters()
        {

            var leader   = RoamingNetworkHistory.Start(NewNetwork());
            var updates  = new List<POIStatusUpdate>();

            leader.RuntimeStatus.OnStatusUpdate += updates.Add;

            var provider = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EMobilityProvider), "DE*EMP",     "offline",      T0, out _, out _);
            var grid     = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(GridOperator),      "DE*NET",     "outOfService", T0, out _, out _);
            var meter    = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EnergyMeter),       "evse-meter", "error",        T0, out _, out _);
            var unknown  = leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EnergyMeter),       "no-meter",   "error",        T0, out _, out _);

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotCBOR(), out var follower, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(new[] { provider, grid, meter },  Is.All.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(unknown,                          Is.EqualTo(POIStatusUpdateResult.Refused));

                Assert.That(updates.Select(update => $"{update.EntityType} {update.Value}"),
                            Is.EqualTo(new[] {
                                "EMobilityProvider offline",
                                "GridOperator outOfService",
                                "EnergyMeter error"
                            }));

                Assert.That(follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EnergyMeter), "evse-meter", out var meterStatus),  Is.True);
                Assert.That(meterStatus!.Value,                                                                                                     Is.EqualTo("error"));
                Assert.That(follower.RuntimeStatus.TryGet(POIRuntimeStatusKind.AdminStatus, nameof(GridOperator), "DE*NET", out var gridStatus),   Is.True);
                Assert.That(gridStatus!.Value,                                                                                                      Is.EqualTo("outOfService"));

            }

        }

        #endregion

    }

}
