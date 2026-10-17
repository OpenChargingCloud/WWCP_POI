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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Energy meter groups: of a charging station operator, grouping the energy meters of its charging pools, charging
    /// stations and EVSEs, and of a grid operator, grouping those of its grid connection points; with the properties of an
    /// energy meter they give their members at runtime, and a status of their own.
    /// </summary>
    [TestFixture]
    public class EnergyMeterGroupTests
    {

        #region Data

        internal static readonly DateTimeOffset              Created       = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        internal static readonly DateTimeOffset              T0            = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        internal static readonly ChargingStationOperator_Id  GEF           = ChargingStationOperator_Id.Parse("DE*GEF");
        internal static readonly ChargingPool_Id             P1            = ChargingPool_Id.           Parse("DE*GEF*P1");
        internal static readonly ChargingStation_Id          S1            = ChargingStation_Id.        Parse("DE*GEF*S1");
        internal static readonly EVSE_Id                     E1            = EVSE_Id.                   Parse("DE*GEF*E1*1");
        internal static readonly EVSE_Id                     E2            = EVSE_Id.                   Parse("DE*GEF*E1*2");
        internal static readonly GridOperator_Id             NET           = GridOperator_Id.           Parse("DE*NET");
        internal static readonly GridConnectionPoint_Id      GCP           = GridConnectionPoint_Id.    Parse("connection-42");

        internal static readonly EnergyMeterManufacturer_Id  ACME          = EnergyMeterManufacturer_Id.Parse("ACME");
        internal static readonly EnergyMeterManufacturer_Id  ZAP           = EnergyMeterManufacturer_Id.Parse("ZAP");
        internal static readonly EnergyMeterModel_Id         ACME1000      = EnergyMeterModel_Id.       Parse("ACME-1000");
        internal static readonly EnergyMeterModel_Id         ACME3000      = EnergyMeterModel_Id.       Parse("ACME-3000");

        internal static readonly EnergyMeter_Id              PoolMeter     = EnergyMeter_Id.            Parse("pv-meter");
        internal static readonly EnergyMeter_Id              StationMeter  = EnergyMeter_Id.            Parse("station-meter");
        internal static readonly EnergyMeter_Id              Meter1        = EnergyMeter_Id.            Parse("evse-meter-1");
        internal static readonly EnergyMeter_Id              Meter2        = EnergyMeter_Id.            Parse("evse-meter-2");
        internal static readonly EnergyMeter_Id              GridMeter     = EnergyMeter_Id.            Parse("grid-meter");

        internal static readonly EnergyMeterGroup_Id         EVSEMeters    = EnergyMeterGroup_Id.       Parse("gef-evse-meters");
        internal static readonly EnergyMeterGroup_Id         GridMeters    = EnergyMeterGroup_Id.       Parse("net-grid-meters");

        #endregion

        #region OperatorGroup / GridGroup

        /// <summary>
        /// The EVSE meters of the charging station operator: the meters of an ACME model, signing OCMF.
        /// </summary>
        internal static EnergyMeterGroup OperatorGroup

            => new (EVSEMeters,
                    I18NString.Create(Languages.en, "EVSE meters"),
                    EnergyMeterIds:    [ Meter2, Meter1 ],
                    Role:              EnergyMeterRole.EVSE,
                    ManufacturerId:    ACME,
                    ModelId:           ACME3000,
                    FirmwareVersion:   "4.7.1",
                    SignatureMethods:  [ SignatureMethod.OCMF ],
                    Created:           Created);

        /// <summary>
        /// The grid meters of the grid operator: of ZAP, hardware version 2.0.
        /// </summary>
        internal static EnergyMeterGroup GridGroup

            => new (GridMeters,
                    EnergyMeterIds:   [ GridMeter ],
                    ManufacturerId:   ZAP,
                    HardwareVersion:  "2.0",
                    Created:          Created);

        #endregion

        #region NewNetwork(OperatorGroups = null, GridGroups = null)

        /// <summary>
        /// A charging pool with a PV meter, a charging station with a meter of its own and two EVSEs with a meter each, the
        /// first of its own model; the grid meter of its grid connection point. By default, the EVSE meters and the grid
        /// meter are grouped.
        /// </summary>
        internal static RoamingNetwork NewNetwork(IEnumerable<EnergyMeterGroup>?  OperatorGroups   = null,
                                                  IEnumerable<EnergyMeterGroup>?  GridGroups       = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   EnergyMeterManufacturers:  [ new EnergyMeterManufacturer(ACME, Created: Created),
                                                new EnergyMeterManufacturer(ZAP,  Created: Created) ],
                   GridOperators:             [ new GridOperator(NET,
                                                                 GridConnectionPoints:  [ new GridConnectionPoint(GCP,
                                                                                                                  EnergyMeter: new EnergyMeter(GridMeter, Role: EnergyMeterRole.Grid, Created: Created)) ],
                                                                 EnergyMeterGroups:     GridGroups ?? [ GridGroup ],
                                                                 Created:               Created) ],
                   ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                            Created:            Created,
                                                                            ChargingPools:      [ new ChargingPool(P1,
                                                                                                                   Created:                Created,
                                                                                                                   GridConnectionPointId:  GCP,
                                                                                                                   EnergyMeters:           [ new EnergyMeter(PoolMeter, Role: EnergyMeterRole.PV, Created: Created) ],
                                                                                                                   ChargingStations:       [ new ChargingStation(S1,
                                                                                                                                                                 Created:       Created,
                                                                                                                                                                 EnergyMeters:  [ new EnergyMeter(StationMeter, Role: EnergyMeterRole.Station, Created: Created) ],
                                                                                                                                                                 EVSEs:         [ new EVSE(E1,
                                                                                                                                                                                           EnergyMeter:  new EnergyMeter(Meter1, ModelId: ACME1000, SerialNumber: "M-1", Created: Created),
                                                                                                                                                                                           Created:      Created),
                                                                                                                                                                                  new EVSE(E2,
                                                                                                                                                                                           EnergyMeter:  new EnergyMeter(Meter2, SerialNumber: "M-2", Created: Created),
                                                                                                                                                                                           Created:      Created) ]) ]) ],
                                                                            EnergyMeterGroups:  OperatorGroups ?? [ OperatorGroup ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion

        #region EffectiveMeterOf(Network, EVSEId)

        private static EnergyMeter EffectiveMeterOf(RoamingNetwork  Network,
                                                    EVSE_Id         EVSEId)
        {

            Assert.That(Network.GetChargingStationOperatorById(GEF)!.TryGetEffectiveEVSE(EVSEId, out var evse),  Is.True);

            return evse!.EnergyMeter!;

        }

        #endregion


        #region Energy_meter_groups_survive_JSON_and_CBOR()

        [Test]
        public void Energy_meter_groups_survive_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var operatorGroup  = json["chargingStationOperators"]![0]!["energyMeterGroups"]![0]!;
            var gridGroup      = json["gridOperators"]![0]!["energyMeterGroups"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                    Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                    Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),                                             Is.EqualTo(rn1.CalcETags()));

                // The members in the order of their identifications, the properties as an energy meter writes them.
                Assert.That(operatorGroup["@id"]!.ToString(),                                 Is.EqualTo("gef-evse-meters"));
                Assert.That(operatorGroup["energyMeterIds"]!.Values<String>(),                Is.EqualTo(new[] { "evse-meter-1", "evse-meter-2" }));
                Assert.That(operatorGroup["role"]!.ToString(),                                Is.EqualTo("evse"));
                Assert.That(operatorGroup["modelId"]!.ToString(),                             Is.EqualTo("ACME-3000"));
                Assert.That(operatorGroup["signatureMethods"]!.Values<String>(),              Is.EqualTo(new[] { "OCMF" }));
                Assert.That(operatorGroup["serialNumber"],                                    Is.Null);
                Assert.That(gridGroup["@id"]!.ToString(),                                     Is.EqualTo("net-grid-meters"));
                Assert.That(gridGroup["manufacturerId"]!.ToString(),                          Is.EqualTo("ZAP"));

                // Unique within the roaming network, found without their owner.
                Assert.That(fromJSON.EnergyMeterGroups.Select(group => group.Id.ToString()).Order(),
                            Is.EqualTo(new[] { "gef-evse-meters", "net-grid-meters" }));
                Assert.That(fromJSON.TryGetEnergyMeterGroupById(EnergyMeterGroup_Id.Parse("GEF-EVSE-METERS"), out var found),  Is.True);
                Assert.That(found!.Id,                                                        Is.EqualTo(EVSEMeters));

                Assert.That(fromJSON.GetChargingStationOperatorById(GEF)!.EnergyMeterGroupsOf(Meter1).Select(group => group.Id),  Is.EqualTo(new[] { EVSEMeters }));
                Assert.That(fromJSON.GetChargingStationOperatorById(GEF)!.EnergyMeterGroupsOf(PoolMeter),                         Is.Empty);
                Assert.That(fromJSON.GridOperators.Single().EnergyMeterGroupsOf(GridMeter).Select(group => group.Id),             Is.EqualTo(new[] { GridMeters }));

            }

        }

        #endregion

        #region Energy_meters_take_the_properties_of_their_groups_at_runtime()

        [Test]
        public void Energy_meters_take_the_properties_of_their_groups_at_runtime()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var rn1      = history.Head.RoamingNetwork;
            var gef      = rn1.GetChargingStationOperatorById(GEF)!;
            var net      = rn1.GridOperators.Single();

            Assert.That(gef.TryGetEVSE(E1, out var storedE1),  Is.True);

            var meter1   = EffectiveMeterOf(rn1, E1);
            var meter2   = EffectiveMeterOf(rn1, E2);
            var pool     = gef.EffectiveChargingPools.Single();
            var grid     = net.EffectiveGridConnectionPoints.Single().EnergyMeter!;

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EnergyMeter), "evse-meter-1", "error", T0, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                // A value of its own wins, the others are those of its group; its serial number is its own.
                Assert.That(meter1.ModelId,                                       Is.EqualTo(ACME1000));
                Assert.That(meter1.ManufacturerId,                                Is.EqualTo(ACME));
                Assert.That(meter1.Role,                                          Is.EqualTo(EnergyMeterRole.EVSE));
                Assert.That(meter1.FirmwareVersion,                               Is.EqualTo("4.7.1"));
                Assert.That(meter1.SignatureMethods,                              Is.EqualTo(new[] { SignatureMethod.OCMF }));
                Assert.That(meter1.SerialNumber,                                  Is.EqualTo("M-1"));
                Assert.That(meter2.ModelId,                                       Is.EqualTo(ACME3000));
                Assert.That(meter2.SerialNumber,                                  Is.EqualTo("M-2"));

                // As it is given, it has only its own.
                Assert.That(storedE1!.EnergyMeter!.ManufacturerId,                Is.Null);
                Assert.That(storedE1.EnergyMeter.Role,                            Is.Null);

                // The effective charging station and charging pool embed the effective EVSEs and their meters;
                // meters without groups are the very ones.
                Assert.That(pool.AllEnergyMeters.Single(meter => meter.Id == Meter2).ModelId,  Is.EqualTo(ACME3000));
                Assert.That(pool.ChargingStations.Single().EnergyMeters.Single().Role,        Is.EqualTo(EnergyMeterRole.Station));
                Assert.That(pool.EnergyMeters.Single(),                                      Is.SameAs(gef.ChargingPools.Single().EnergyMeters.Single()));

                // The grid meter takes the properties of the group of its grid operator, but keeps its role.
                Assert.That(grid.ManufacturerId,                                  Is.EqualTo(ZAP));
                Assert.That(grid.HardwareVersion,                                 Is.EqualTo("2.0"));
                Assert.That(grid.Role,                                            Is.EqualTo(EnergyMeterRole.Grid));
                Assert.That(net.GridConnectionPoints.Single().EnergyMeter!.ManufacturerId,  Is.Null);
                Assert.That(net.TryGetEffectiveGridConnectionPoint(GridConnectionPoint_Id.Parse("CONNECTION-42"), out var point),  Is.True);
                Assert.That(point!.EnergyMeter!.HardwareVersion,                  Is.EqualTo("2.0"));

                // An effective meter has the runtime status of the meter.
                Assert.That(EffectiveMeterOf(history.Head.RoamingNetwork, E1).Status.Value,  Is.EqualTo(EnergyMeterStatusType.Error));

            }

        }

        #endregion

        #region Effective_energy_meters_keep_the_runtime_status_in_new_versions()

        [Test]
        public void Effective_energy_meters_keep_the_runtime_status_in_new_versions()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var rn1      = history.Head.RoamingNetwork;

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EnergyMeter), "evse-meter-1", "error",   T0, out _, out _);
            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EnergyMeter), "grid-meter",   "offline", T0, out _, out _);

            // New versions of both owners, changing nothing their groups give.
            history.Commit(rn1.CreateSignedChangeSet([
                               RoamingNetworkChange.UpdateProperty(rn1.GetChargingStationOperatorById(GEF)!, "homepage", null, "https://gef.example"),
                               RoamingNetworkChange.UpdateProperty(rn1.GridOperators.Single(),               "homepage", null, "https://netz.example")
                           ], CreatedAt: T0.AddMinutes(1)));

            var rn2 = history.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.Homepage?.ToString(),                                 Is.EqualTo("https://gef.example"));
                Assert.That(EffectiveMeterOf(rn2, E1).Status.Value,                                                         Is.EqualTo(EnergyMeterStatusType.Error));
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.EffectiveChargingPools.Single().
                                AllEnergyMeters.Single(meter => meter.Id == Meter1).Status.Value,                           Is.EqualTo(EnergyMeterStatusType.Error));
                Assert.That(rn2.GridOperators.Single().EffectiveGridConnectionPoints.Single().EnergyMeter!.Status.Value,   Is.EqualTo(EnergyMeterStatusType.Offline));
            }

        }

        #endregion

        #region Of_the_groups_of_a_meter_the_one_with_the_higher_priority_wins()

        [Test]
        public void Of_the_groups_of_a_meter_the_one_with_the_higher_priority_wins()
        {

            EnergyMeterGroup Group(String Id, EnergyMeterModel_Id ModelId, Int32? Priority, SignatureMethod SignatureMethod)

                => new (EnergyMeterGroup_Id.Parse(Id),
                        EnergyMeterIds:    [ Meter2 ],
                        Priority:          Priority,
                        ModelId:           ModelId,
                        SignatureMethods:  [ SignatureMethod ],
                        Created:           Created);

            var rn1 = NewNetwork(OperatorGroups: [ Group("group-a", ACME3000,                          1,    SignatureMethod.OCMF),
                                                   Group("group-b", EnergyMeterModel_Id.Parse("ACME-4000"), null, SignatureMethod.EDL40) ]);

            var meter2 = EffectiveMeterOf(rn1, E2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(meter2.ModelId,           Is.EqualTo(ACME3000));
                Assert.That(meter2.SignatureMethods,  Is.EquivalentTo(new[] { SignatureMethod.OCMF, SignatureMethod.EDL40 }));

                // Groups of the same priority must not give it different values.
                Assert.That(() => NewNetwork(OperatorGroups: [ Group("group-a", ACME3000,                          null, SignatureMethod.OCMF),
                                                               Group("group-b", EnergyMeterModel_Id.Parse("ACME-4000"), null, SignatureMethod.EDL40) ]),
                            Throws.ArgumentException.With.Message.Contains("give the energy meter 'evse-meter-2' different values of 'modelId' with the same priority"));

            }

        }

        #endregion

        #region Invalid_energy_meter_groups_are_refused()

        [Test]
        public void Invalid_energy_meter_groups_are_refused()
        {

            using (Assert.EnterMultipleScope())
            {

                // A charging station operator groups only its own meters, a grid operator only those of its grid connection points.
                Assert.That(() => NewNetwork(OperatorGroups: [ new EnergyMeterGroup(EVSEMeters, EnergyMeterIds: [ GridMeter ]) ]),
                            Throws.ArgumentException.With.Message.Contains("The member 'grid-meter' of the group 'gef-evse-meters' is unknown at the operator 'DE*GEF'"));

                Assert.That(() => NewNetwork(GridGroups: [ new EnergyMeterGroup(GridMeters, EnergyMeterIds: [ Meter1 ]) ]),
                            Throws.ArgumentException.With.Message.Contains("The member 'evse-meter-1' of the group 'net-grid-meters' is unknown at the grid operator 'DE*NET'"));

                // Unique within the roaming network.
                Assert.That(() => NewNetwork(GridGroups: [ new EnergyMeterGroup(EnergyMeterGroup_Id.Parse("GEF-EVSE-METERS"), EnergyMeterIds: [ GridMeter ]) ]),
                            Throws.ArgumentException.With.Message.Contains("must not be given twice in the roaming network"));

                // Manufacturers, transparency software releases and certificates of the network.
                Assert.That(() => NewNetwork(OperatorGroups: [ new EnergyMeterGroup(EVSEMeters, EnergyMeterIds: [ Meter1 ], ManufacturerId: EnergyMeterManufacturer_Id.Parse("NOBODY")) ]),
                            Throws.ArgumentException.With.Message.Contains("The energy meter group 'gef-evse-meters' names the unknown energy meter manufacturer 'NOBODY'"));

                Assert.That(() => NewNetwork(OperatorGroups: [ new EnergyMeterGroup(EVSEMeters,
                                                                                   EnergyMeterIds:              [ Meter1 ],
                                                                                   TransparencySoftwareStatus:  [ new TransparencySoftwareAssignment(TransparencySoftware_Id.Parse("chargy-9"), LegalStatus.ForInformationOnly) ]) ]),
                            Throws.ArgumentException.With.Message.Contains("The transparency software release 'chargy-9' of the energy meter group 'gef-evse-meters' is unknown"));

            }

        }

        #endregion

        #region Energy_meter_groups_are_changed_by_change_sets_of_their_owners()

        [Test]
        public void Energy_meter_groups_are_changed_by_change_sets_of_their_owners()
        {

            var rn1        = NewNetwork();
            var gef        = rn1.GetChargingStationOperatorById(GEF)!;
            var group      = gef.EnergyMeterGroups.Single();
            var gridGroup  = rn1.GridOperators.Single().EnergyMeterGroups.Single();
            var poolGroup  = new EnergyMeterGroup(EnergyMeterGroup_Id.Parse("gef-pool-meters"), EnergyMeterIds: [ PoolMeter ], ManufacturerId: ACME);
            var moreGrid   = new EnergyMeterGroup(EnergyMeterGroup_Id.Parse("net-firmware"),    EnergyMeterIds: [ GridMeter ], FirmwareVersion: "9.0");

            var rn2        = Apply(rn1,
                                   RoamingNetworkChange.UpdateProperty(group, "modelId", "ACME-3000", "ACME-5000"),
                                   RoamingNetworkChange.Add(poolGroup, GEF),
                                   RoamingNetworkChange.Add(moreGrid,  NET));

            var gef2       = rn2.GetChargingStationOperatorById(GEF)!;

            Assert.That(gef2.TryGetEVSE         (E2, out var storedE2),     Is.True);
            Assert.That(gef2.TryGetEffectiveEVSE(E2, out var effectiveE2),  Is.True);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(effectiveE2!.EnergyMeter!.ModelId,                    Is.EqualTo(EnergyMeterModel_Id.Parse("ACME-5000")));
                Assert.That(gef2.EffectiveChargingPools.Single().EnergyMeters.Single().ManufacturerId,                Is.EqualTo(ACME));
                Assert.That(rn2.GridOperators.Single().EffectiveGridConnectionPoints.Single().EnergyMeter!.FirmwareVersion,  Is.EqualTo("9.0"));

                // The change set's time: of the group, of its owner, and of the members it gives something else, as they
                // are at runtime; not of the members as they are given.
                Assert.That(gef2.EnergyMeterGroups.First().LastChangeDate,      Is.EqualTo(T0));
                Assert.That(gef2.LastChangeDate,                                 Is.EqualTo(T0));
                Assert.That(effectiveE2.LastChangeDate,                          Is.EqualTo(T0));
                Assert.That(effectiveE2.EnergyMeter.LastChangeDate,              Is.EqualTo(T0));
                Assert.That(storedE2!.LastChangeDate,                            Is.EqualTo(Created));
                Assert.That(rn2.GridOperators.Single().LastChangeDate,          Is.EqualTo(T0));

                // A meter still named by a group can not be removed...
                Assert.That(() => Apply(rn2, RoamingNetworkChange.RemoveProperty(storedE2, "energyMeter")),
                            Throws.ArgumentException.With.Message.Contains("The member 'evse-meter-2' of the group 'gef-evse-meters' is unknown at the operator 'DE*GEF'"));

                // ...the group can, and its members are as they are given again.
                var rn3 = Apply(rn2, RoamingNetworkChange.Remove(EVSEMeters));
                Assert.That(rn3.TryGetEnergyMeterGroupById(EVSEMeters, out _),  Is.False);
                Assert.That(EffectiveMeterOf(rn3, E2).ModelId,                   Is.Null);

                // A group is added with its owner, as the identification of another one.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add(new EnergyMeterGroup(GridMeters, EnergyMeterIds: [ PoolMeter ]), GEF)),
                            Throws.ArgumentException.With.Message.Contains("already exists"));

                // Only the owner signs a change of its groups: the grid operator those of its own, not the charging station operator.
                var gridChange      = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gridGroup, "hardwareVersion", "2.0", "3.0") ], CreatedAt: T0);
                var operatorChange  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(group,     "firmwareVersion", "4.7.1", "4.8.0") ], CreatedAt: T0);

                Assert.That(rn1.TryCheckOwnership(gridChange.    SignedBy(TestParties.Party(GEF)), out var gridError),      Is.False);
                Assert.That(gridError,                                                                                       Does.Contain("needs a signature by"));
                Assert.That(rn1.TryCheckOwnership(gridChange.    SignedBy(TestParties.Party(NET)), out var gridOK),         Is.True, gridOK);
                Assert.That(rn1.TryCheckOwnership(operatorChange.SignedBy(TestParties.Party(NET)), out var operatorError),  Is.False);
                Assert.That(operatorError,                                                                                   Does.Contain("needs a signature by"));
                Assert.That(rn1.TryCheckOwnership(operatorChange.SignedBy(TestParties.Party(GEF)), out var operatorOK),     Is.True, operatorOK);

            }

        }

        #endregion

        #region Changes_of_energy_meter_groups_on_two_branches_merge()

        [Test]
        public void Changes_of_energy_meter_groups_on_two_branches_merge()
        {

            var rn1        = NewNetwork();
            var group      = rn1.GetChargingStationOperatorById(GEF)!.EnergyMeterGroups.Single();
            var gridGroup  = rn1.GridOperators.Single().EnergyMeterGroups.Single();

            var left       = Apply(rn1, RoamingNetworkChange.UpdateProperty(group,     "firmwareVersion", "4.7.1", "4.8.0"));
            var right      = Apply(rn1, RoamingNetworkChange.UpdateProperty(gridGroup, "hardwareVersion", "2.0",   "3.0"),
                                        RoamingNetworkChange.Add(new EnergyMeterGroup(EnergyMeterGroup_Id.Parse("net-firmware"), EnergyMeterIds: [ GridMeter ], FirmwareVersion: "9.0"), NET));
            var other      = Apply(rn1, RoamingNetworkChange.UpdateProperty(group,     "firmwareVersion", "4.7.1", "5.0.0"));

            var merged     = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out _, out var conflicts1);
            var conflict   = RoamingNetwork.TryMerge(rn1, left, other, ChangeSet_Id.Parse("merge-2"), out _,         out _, out var conflicts2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(merged,                                                                Is.True, String.Join(" ", conflicts1.Select(conflict => conflict.Description)));
                Assert.That(EffectiveMeterOf(merge!, E1).FirmwareVersion,                          Is.EqualTo("4.8.0"));
                Assert.That(merge.GridOperators.Single().EffectiveGridConnectionPoints.Single().EnergyMeter!.HardwareVersion,  Is.EqualTo("3.0"));
                Assert.That(merge.GridOperators.Single().EffectiveGridConnectionPoints.Single().EnergyMeter!.FirmwareVersion,  Is.EqualTo("9.0"));

                Assert.That(conflict,                                                              Is.False);
                Assert.That(conflicts2.Single().PropertyName,                                      Is.EqualTo("firmwareVersion"));

            }

        }

        #endregion

        #region An_energy_meter_group_has_a_status_set_or_calculated_from_its_meters()

        [Test]
        public void An_energy_meter_group_has_a_status_set_or_calculated_from_its_meters()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());

            POIStatusUpdateResult SetStatus(String EntityType, Object EntityId, String Value, DateTimeOffset Timestamp)
                => history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, EntityType, EntityId.ToString()!, Value, Timestamp, out _, out _);

            String? StatusOf(String EntityType, Object EntityId)
                => history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, EntityType, EntityId.ToString()!, out var status)
                       ? status.Value
                       : null;

            history.RuntimeStatus.CalculateStatus(EVSEMeters, POIGroupStatus.OfEnergyMeters);

            using (Assert.EnterMultipleScope())
            {

                // Both meters are available since their creation, and so is the group.
                Assert.That(StatusOf(nameof(EnergyMeterGroup), EVSEMeters),                                  Is.EqualTo("available"));

                Assert.That(SetStatus(nameof(EnergyMeter), Meter1, "offline", T0),                           Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(nameof(EnergyMeterGroup), EVSEMeters),                                  Is.EqualTo("partialAvailable"));

                Assert.That(SetStatus(nameof(EnergyMeter), Meter2, "offline", T0.AddMinutes(1)),             Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(nameof(EnergyMeterGroup), EVSEMeters),                                  Is.EqualTo("offline"));

                Assert.That(SetStatus(nameof(EnergyMeter), Meter1, "error", T0.AddMinutes(2)),               Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(StatusOf(nameof(EnergyMeterGroup), EVSEMeters),                                  Is.EqualTo("error"));

                // The delegate has the say over a calculated status; the one of another group is set manually.
                Assert.That(SetStatus(nameof(EnergyMeterGroup), EVSEMeters, "available", T0.AddMinutes(3)),  Is.EqualTo(POIStatusUpdateResult.Refused));
                Assert.That(SetStatus(nameof(EnergyMeterGroup), GridMeters, "offline",   T0),                Is.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(SetStatus(nameof(EnergyMeterGroup), "nobody",   "offline",   T0),                Is.EqualTo(POIStatusUpdateResult.Refused));

                Assert.That(history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeterGroup), "net-grid-meters", "outOfService", T0, out _, out _),
                            Is.EqualTo(POIStatusUpdateResult.Updated));

                // The status goes along with a snapshot.
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(history.ToSnapshotCBOR(), out var follower, out var error),  Is.True, error);
                Assert.That(follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status,      nameof(EnergyMeterGroup), "net-grid-meters", out var gridStatus),   Is.True);
                Assert.That(gridStatus!.Value,                                                                                                                   Is.EqualTo("offline"));
                Assert.That(follower.RuntimeStatus. TryGet(POIRuntimeStatusKind.AdminStatus, nameof(EnergyMeterGroup), "net-grid-meters", out var adminStatus),  Is.True);
                Assert.That(adminStatus!.Value,                                                                                                                  Is.EqualTo("outOfService"));

            }

        }

        #endregion

    }

}
