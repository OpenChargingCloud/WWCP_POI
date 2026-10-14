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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Charging station manufacturers, and charging pools, charging stations and EVSEs embedded in their operators.
    /// </summary>
    [TestFixture]
    public class InfrastructureTests
    {

        #region Data

        private static readonly DateTimeOffset                  Created   = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset                  T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id      GEF       = ChargingStationOperator_Id.    Parse("DE*GEF");
        private static readonly ChargingPool_Id                 P1        = ChargingPool_Id.               Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id              S1        = ChargingStation_Id.            Parse("DE*GEF*S1");
        private static readonly EVSE_Id                         E1        = EVSE_Id.                       Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                         E2        = EVSE_Id.                       Parse("DE*GEF*E1*2");
        private static readonly ChargingStationManufacturer_Id  ACME      = ChargingStationManufacturer_Id.Parse("ACME");

        #endregion

        #region NewEVSE(Id, MaxPower = 22000)

        private static EVSE NewEVSE(EVSE_Id Id, Decimal MaxPower = 22000)

            => new (Id,
                    PhysicalReference:   Id.Suffix,
                    CurrentTypes:        CurrentTypes.AC_ThreePhases,
                    ChargingModes:       ChargingModes.Mode_3,
                    MaxVoltage:          Volt.  FromV(400),
                    MaxCurrent:          Ampere.FromA(32),
                    MaxPower:            Watt.  FromW(MaxPower),
                    ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2, Lockable: true) ],
                    Created:             Created);

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                       Created,
                    ChargingStationManufacturers:  [ new ChargingStationManufacturer(ACME,
                                                                                     I18NString.Create(Languages.en, "ACME Chargers"),
                                                                                     Created: Created) ],
                    ChargingStationOperators:      [ new ChargingStationOperator(GEF,
                                                                                 I18NString.Create(Languages.en, "GraphDefined"),
                                                                                 Created:        Created,
                                                                                 ChargingPools:  [ new ChargingPool(P1,
                                                                                                                    I18NString.Create(Languages.en, "Pool 1"),
                                                                                                                    GeoLocation:       GeoCoordinate.Parse(50.92, 11.58),
                                                                                                                    ParkingType:       ParkingType.PARKING_LOT,
                                                                                                                    Accessibility:     AccessibilityType.FreePubliclyAccessible,
                                                                                                                    Created:           Created,
                                                                                                                    ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                             PhysicalReference:  "Station 1",
                                                                                                                                                             ManufacturerId:     ACME,
                                                                                                                                                             ModelId:            ChargingStationModel_Id.Parse("Wallbox-22"),
                                                                                                                                                             EVSEs:              [ NewEVSE(E1), NewEVSE(E2) ],
                                                                                                                                                             Created:            Created) ]) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region The_hierarchy_survives_JSON_and_CBOR()

        [Test]
        public void The_hierarchy_survives_JSON_and_CBOR()
        {

            var rn1       = NewNetwork();
            var json      = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var evse      = json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),   Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),   Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),            Is.EqualTo(rn1.CalcETags()));

                Assert.That(json["chargingStationManufacturers"]![0]!["@id"]!.ToString(),   Is.EqualTo("ACME"));
                Assert.That(evse["currentTypes"]!.Values<String>(),                          Is.EqualTo(new[] { "AC_ThreePhases" }));
                Assert.That(evse["chargingModes"]!.Values<String>(),                         Is.EqualTo(new[] { "Mode_3" }));
                Assert.That(evse["maxPower"]!.Value<Decimal>(),                              Is.EqualTo(22000));
                Assert.That(evse["chargingConnectors"]![0]!["type"]!.ToString(),             Is.EqualTo("IEC_62196_T2"));

                var gef = fromJSON.GetChargingStationOperatorById(GEF)!;
                Assert.That(gef.TryGetEVSE(E2, out var e2),                                  Is.True);
                Assert.That(e2!.MaxCurrent,                                                  Is.EqualTo(Ampere.FromA(32)));
                Assert.That(gef.TryGetChargingStation(S1, out var s1),                       Is.True);
                Assert.That(s1!.ManufacturerId,                                              Is.EqualTo(ACME));

            }

        }

        #endregion

        #region Invalid_hierarchies_are_refused()

        [Test]
        public void Invalid_hierarchies_are_refused()
        {

            var json = NewNetwork().ToJSON();

            var unknownManufacturer = (JObject) json.DeepClone();
            unknownManufacturer.Remove("chargingStationManufacturers");

            var twiceEVSE = (JObject) json.DeepClone();
            var evses     = (JArray) twiceEVSE["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]!;
            evses[1]!["@id"] = E1.ToString();

            var foreignPool = (JObject) json.DeepClone();
            foreignPool["chargingStationOperators"]![0]!["chargingPools"]![0]!["@id"] = "DE*XYZ*P1";

            var onlyIds = (JObject) json.DeepClone();
            var pool    = (JObject) onlyIds["chargingStationOperators"]![0]!["chargingPools"]![0]!;
            pool["chargingStationIds"] = new JArray(S1.ToString());
            pool.Remove("chargingStations");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetwork.TryParse(unknownManufacturer, out _, out var error1),  Is.False);
                Assert.That(error1,                                                               Does.Contain("manufacturer 'ACME'"));
                Assert.That(RoamingNetwork.TryParse(twiceEVSE,           out _, out _),           Is.False);
                Assert.That(RoamingNetwork.TryParse(foreignPool,         out _, out _),           Is.False);
                Assert.That(RoamingNetwork.TryParse(onlyIds,             out _, out var error4),  Is.False);
                Assert.That(error4,                                                               Does.Contain("completely"));
            }

        }

        #endregion

        #region Pools_stations_and_EVSEs_are_changed_by_change_sets()

        [Test]
        public void Pools_stations_and_EVSEs_are_changed_by_change_sets()
        {

            var rn1  = NewNetwork();
            var gef  = rn1.GetChargingStationOperatorById(GEF)!;
            gef.TryGetEVSE(E1, out var e1);

            var p2   = ChargingPool_Id.   Parse("DE*GEF*P2");
            var s2   = ChargingStation_Id.Parse("DE*GEF*S2");
            var e3   = EVSE_Id.           Parse("DE*GEF*E2*1");

            var cs   = rn1.CreateSignedChangeSet([
                           RoamingNetworkChange.Add           (new ChargingPool(p2, I18NString.Create(Languages.en, "Pool 2"))),
                           RoamingNetworkChange.Add           (new ChargingStation(s2, ManufacturerId: ACME, Created: Created), p2),
                           RoamingNetworkChange.Add           (NewEVSE(e3, 50000), s2),
                           RoamingNetworkChange.UpdateProperty(e1!, "maxPower", null, 11000),
                           RoamingNetworkChange.Remove        (E2)
                       ], CreatedAt: T0);

            var rn2  = rn1.ApplyChangeSet(cs);
            var gef2 = rn2.GetChargingStationOperatorById(GEF)!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(gef2.ChargingPools.Select(pool => pool.Id.ToString()),          Is.EqualTo(new[] { "DE*GEF*P1", "DE*GEF*P2" }));
                Assert.That(gef2.TryGetChargingStation(s2, out var station2),               Is.True);
                Assert.That(station2!.EVSEs.Single().MaxPower,                              Is.EqualTo(Watt.FromW(50000)));
                Assert.That(station2.Created,                                               Is.EqualTo(Created), "Added: its creation as given.");
                Assert.That(station2.LastChangeDate,                                        Is.EqualTo(T0),      "Added: its last change by the change set.");

                Assert.That(gef2.TryGetEVSE(E1, out var changedE1),                         Is.True);
                Assert.That(changedE1!.MaxPower,                                            Is.EqualTo(Watt.FromW(11000)));
                Assert.That(changedE1.LastChangeDate,                                       Is.EqualTo(T0));
                Assert.That(gef2.TryGetEVSE(E2, out _),                                     Is.False);

                // Old values for a later check, as for operators.
                Assert.That(cs.Changes[3].OldValue!.Value<Decimal>(),                       Is.EqualTo(22000));
                Assert.That(cs.Changes[4].OldValue!["@id"]!.ToString(),                     Is.EqualTo(E2.ToString()));

                // rn1 stays as it was.
                Assert.That(gef.ChargingPools.Length,                                       Is.EqualTo(1));
                Assert.That(rn2.Fits(cs),                                                   Is.False);

            }

        }

        #endregion

        #region Entities_are_read_only_with_their_creation()

        /// <summary>
        /// Read without its creation, an entity would be created now, and everyone applying the same change set
        /// would get another version: the creation is mandatory when reading JSON or CBOR, except for the entity
        /// a change adds, which is created by the change set unless given.
        /// </summary>
        [Test]
        public void Entities_are_read_only_with_their_creation()
        {

            var rn1         = NewNetwork();
            var s8          = ChargingStation_Id.Parse("DE*GEF*S8");

            var station     = new ChargingStation(s8, EVSEs: [ NewEVSE(EVSE_Id.Parse("DE*GEF*E8*1")) ], Created: Created).ToJSON();
            station.Remove("created");
            station.Remove("lastChange");

            var nestedEVSE  = (JObject) station.DeepClone();
            ((JObject) nestedEVSE["EVSEs"]![0]!).Remove("created");
            ((JObject) nestedEVSE["EVSEs"]![0]!).Remove("lastChange");

            var network     = rn1.ToJSON();
            network.Remove("created");

            using (Assert.EnterMultipleScope())
            {

                // The added station is created by the change set, its EVSE is given with its creation.
                var rn2 = Apply(rn1, RoamingNetworkChange.Add("ChargingStation", s8.ToString(), station, "ChargingPool", P1.ToString()));
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.TryGetChargingStation(s8, out var added),  Is.True);
                Assert.That(added!.Created,                                                                     Is.EqualTo(T0));
                Assert.That(added.EVSEs.Single().Created,                                                       Is.EqualTo(Created));

                // An EVSE embedded without its creation is refused.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add("ChargingStation", s8.ToString(), nestedEVSE, "ChargingPool", P1.ToString())),
                            Throws.ArgumentException.With.Message.Contains("The creation timestamp 'created' of 'DE*GEF*E8*1' is missing"));

                Assert.That(RoamingNetwork.TryParse(network, out _, out var error),                              Is.False);
                Assert.That(error,                                                                               Does.Contain("'created' of 'rn1' is missing"));

            }

        }

        #endregion

        #region Invalid_infrastructure_changes_are_refused()

        [Test]
        public void Invalid_infrastructure_changes_are_refused()
        {

            var rn1 = NewNetwork();
            var s1  = rn1.GetChargingStationOperatorById(GEF)!.ChargingStations.Single();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add("ChargingStation", "DE*GEF*S9", new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S9")).ToJSON())),
                            Throws.ArgumentException.With.Message.Contains("needs its ChargingPool"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(NewEVSE(EVSE_Id.Parse("DE*GEF*E9*1")), ChargingStation_Id.Parse("DE*GEF*S9"))),
                            Throws.ArgumentException.With.Message.Contains("'DE*GEF*S9' of the EVSE"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add(NewEVSE(E1), S1)),
                            Throws.ArgumentException.With.Message.Contains("already exists"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty("EVSE", E1.ToString(), "maxPower", null, 1, "ChargingStation", "DE*GEF*S9")),
                            Throws.ArgumentException.With.Message.Contains("belongs to ChargingStation 'DE*GEF*S1'"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(s1, "EVSEs", null, new JArray())),
                            Throws.ArgumentException.With.Message.Contains("can not be changed"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty(s1, "manufacturerId", null, "NOBODY")),
                            Throws.ArgumentException.With.Message.Contains("'NOBODY'"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.Remove(ACME)),
                            Throws.ArgumentException.With.Message.Contains("manufacturer 'ACME' of the charging station"));

                Assert.That(() => Apply(rn1, RoamingNetworkChange.Remove(EVSE_Id.Parse("DE*XYZ*E1*1"))),
                            Throws.ArgumentException.With.Message.Contains("operator 'DE*XYZ'"));

            }

        }

        #endregion

        #region Entities_are_found_in_every_spelling_of_their_identification()

        /// <summary>
        /// A change names an entity and its parent in any spelling their identification types consider equal,
        /// e.g. without the '*' of the operator, in lower case, without the '*' in the suffix of an EVSE,
        /// and changes the same entity, with the same result, as with the spelling it is kept with.
        /// </summary>
        [Test]
        public void Entities_are_found_in_every_spelling_of_their_identification()
        {

            var rn1        = NewNetwork();
            var e3         = EVSE_Id.Parse("DE*GEF*E1*3");

            var canonical  = Apply(rn1, RoamingNetworkChange.UpdateProperty("ChargingStation", "DE*GEF*S1",   "physicalReference", null, "Station One"));
            var respelled  = Apply(rn1, RoamingNetworkChange.UpdateProperty("ChargingStation", "DEGEF*S1",    "physicalReference", null, "Station One"));

            var rn2        = Apply(rn1,
                                   RoamingNetworkChange.UpdateProperty("EVSE",            "DE*GEF*E11",  "maxPower", null, 11000, "ChargingStation", "de*gef*S1"),
                                   RoamingNetworkChange.Add           ("EVSE",            e3.ToString(), NewEVSE(e3).ToJSON(),    "ChargingStation", "DEGEF*S1"),
                                   RoamingNetworkChange.Add           ("ChargingStation", "DE*GEF*S7",   new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S7"), Created: Created).ToJSON(),
                                                                                                                                  "ChargingPool",    "degef*P1"),
                                   RoamingNetworkChange.Remove        ("EVSE",            "DE*GEF*E12"));

            var gef2       = rn2.GetChargingStationOperatorById(GEF)!;

            using (Assert.EnterMultipleScope())
            {

                // The same change, the same result.
                Assert.That(respelled.CalcETags(),                                                  Is.EqualTo(canonical.CalcETags()));
                Assert.That(respelled.GetChargingStationOperatorById(GEF)!.ChargingStations.
                                Select(chargingStation => chargingStation.Id.ToString()),           Is.EqualTo(new[] { "DE*GEF*S1" }));
                Assert.That(respelled.GetChargingStationOperatorById(GEF)!.ChargingStations.
                                Single().PhysicalReference,                                         Is.EqualTo("Station One"));

                Assert.That(gef2.TryGetEVSE(E1, out var changedE1),                                 Is.True);
                Assert.That(changedE1!.MaxPower,                                                    Is.EqualTo(Watt.FromW(11000)));
                Assert.That(gef2.TryGetChargingStation(S1, out var s1),                             Is.True);
                Assert.That(s1!.EVSEs.Select(evse => evse.Id.ToString()),                           Is.EqualTo(new[] { "DE*GEF*E1*1", "DE*GEF*E1*3" }));
                Assert.That(gef2.ChargingStations.Select(chargingStation => chargingStation.Id.ToString()),
                                                                                                    Is.EqualTo(new[] { "DE*GEF*S1", "DE*GEF*S7" }));

                // An entity added in another spelling of an existing one is one already there.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.Add("EVSE", "DE*GEF*E11", NewEVSE(EVSE_Id.Parse("DE*GEF*E11")).ToJSON(), "ChargingStation", "DE*GEF*S1")),
                            Throws.ArgumentException.With.Message.Contains("already exists"));

                // Another parent is still another one.
                Assert.That(() => Apply(rn1, RoamingNetworkChange.UpdateProperty("EVSE", "DE*GEF*E11", "maxPower", null, 1, "ChargingStation", "DEGEF*S9")),
                            Throws.ArgumentException.With.Message.Contains("belongs to ChargingStation 'DE*GEF*S1'"));

            }

        }

        #endregion

        #region Manufacturers_are_changed_by_change_sets()

        [Test]
        public void Manufacturers_are_changed_by_change_sets()
        {

            var rn1   = NewNetwork();
            var acme  = rn1.ChargingStationManufacturers.Single();

            var rn2   = Apply(rn1,
                              RoamingNetworkChange.Add           (new ChargingStationManufacturer(ChargingStationManufacturer_Id.Parse("ZAP"))),
                              RoamingNetworkChange.UpdateProperty(acme, "homepage", null, "https://acme.example"));

            rn2.TryGetChargingStationManufacturerById(ACME, out var changedAcme);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.ChargingStationManufacturers.Select(manufacturer => manufacturer.Id.ToString()).Order(),  Is.EqualTo(new[] { "ACME", "ZAP" }));
                Assert.That(changedAcme!.Homepage?.ToString(),                                                          Is.EqualTo("https://acme.example"));
            }

        }

        #endregion

        #region The_runtime_status_reaches_every_level()

        [Test]
        public void The_runtime_status_reaches_every_level()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var updates  = new List<POIStatusUpdate>();

            history.RuntimeStatus.OnStatusUpdate += updates.Add;

            var evse     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EVSE),                        E1.ToString(), "Charging",    T0, out _, out _);
            var station  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(ChargingStation),             S1.ToString(), "OutOfService", T0, out _, out _);
            var pool     = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ChargingPool),                P1.ToString(), "Charging",    T0, out _, out _);
            var maker    = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(ChargingStationManufacturer), "ACME",        "Offline",     T0, out _, out _);
            var unknown  = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status,      nameof(EVSE),                        "DE*GEF*E9*9", "Charging",    T0, out _, out _);

            // Changes of the EVSE and of its operator keep it.
            var gef      = history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!;
            gef.TryGetEVSE(E1, out var e1);

            history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet([
                                RoamingNetworkChange.UpdateProperty(e1!,  "maxPower",  null, 11000),
                                RoamingNetworkChange.UpdateProperty(gef,  "telephone", null, "+49 30 1234567")
                            ], CreatedAt: T0.AddMinutes(1)));

            var kept     = history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EVSE), E1.ToString(), out var evseStatus);

            // A change of the new version's EVSE is told of too, and only once.
            history.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!.TryGetEVSE(E1, out var newE1);
            newE1!.SetStatus(EVSEStatusType.Available, T0.AddMinutes(2));

            // Removed: its status goes with it.
            history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.Remove(E1) ], CreatedAt: T0.AddMinutes(3)));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(new[] { evse, station, pool, maker },    Is.All.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(unknown,                                  Is.EqualTo(POIStatusUpdateResult.Refused));

                Assert.That(kept,                                     Is.True);
                Assert.That(evseStatus!.Value,                        Is.EqualTo("Charging"));

                Assert.That(updates.Select(update => $"{update.EntityType} {update.Value}"),
                            Is.EqualTo(new[] {
                                "EVSE Charging",
                                "ChargingStation OutOfService",
                                "ChargingPool Charging",
                                "ChargingStationManufacturer Offline",
                                "EVSE available"
                            }));

                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EVSE), E1.ToString(), out _),  Is.False);
                Assert.That(history.RuntimeStatus.All.Count(status => status.EntityType == nameof(EVSE)),                     Is.EqualTo(2), "E2, admin status and status.");

            }

        }

        #endregion

        #region A_follower_gets_the_whole_hierarchy()

        [Test]
        public void A_follower_gets_the_whole_hierarchy()
        {

            var leader = RoamingNetworkHistory.Start(NewNetwork());

            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EVSE), E1.ToString(), "Charging", T0, out _, out _);

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotCBOR(), out var follower, out var error),  Is.True, error);

            var gef = leader.Head.RoamingNetwork.GetChargingStationOperatorById(GEF)!;
            gef.TryGetEVSE(E1, out var e1);

            leader.Commit(leader.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(e1!, "maxPower", null, 11000) ], CreatedAt: T0.AddMinutes(1)));
            leader.TryGetCommitsSince(follower!.Head.Commit.Id, out var commits, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(follower.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EVSE), E1.ToString(), out var status),  Is.True);
                Assert.That(status!.Value,                                                                                       Is.EqualTo("Charging"));
                Assert.That(follower.TryAppend(commits!, out _, out var appendError),                                            Is.True, appendError);
                Assert.That(follower.Head.Commit.Id,                                                                             Is.EqualTo(leader.Head.Commit.Id));
            }

        }

        #endregion

    }

}
