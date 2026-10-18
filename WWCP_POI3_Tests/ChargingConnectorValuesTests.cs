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
    /// The current types, maximum voltage, current and power and the charging tariffs of a charging connector: its own,
    /// else those of its EVSE, e.g. a CHAdeMO connector of 50 kW beside a CCS one of 150 kW.
    /// </summary>
    [TestFixture]
    public class ChargingConnectorValuesTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-18T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingTariff_Id           Fast     = ChargingTariff_Id.         Parse("DE*GEF*TFAST");
        private static readonly ChargingTariff_Id           Normal   = ChargingTariff_Id.         Parse("DE*GEF*TNORMAL");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");

        #endregion

        #region (private static) ATariff(Id) / AnEVSE(CHAdeMO) / ANetwork(CHAdeMO)

        private static ChargingTariff ATariff(ChargingTariff_Id Id)

            => new (Id,
                    Currency.EUR,
                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.59m, VAT: 19, StepSize: 1) ]) ],
                    Created: Created);

        /// <summary>
        /// A DC EVSE of 150 kW with the fast tariff: a CCS connector as the EVSE is, and a CHAdeMO one of 50 kW and 125 A
        /// with the normal tariff.
        /// </summary>
        private static EVSE AnEVSE(ChargingConnector? CHAdeMO = null)

            => new (E1,
                    CurrentTypes:        CurrentTypes.DC,
                    MaxVoltage:          Volt.  FromV(920),
                    MaxCurrent:          Ampere.FromA(200),
                    MaxPower:            Watt.  FromW(150000),
                    ChargingTariffIds:   [ Fast ],
                    ChargingConnectors:  [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2_COMBO),
                                           CHAdeMO ?? new ChargingConnector(ChargingConnector_Id.Parse("2"),
                                                                            ChargingConnectorType.CHAdeMO,
                                                                            MaxVoltage:         Volt.  FromV(500),
                                                                            MaxCurrent:         Ampere.FromA(125),
                                                                            MaxPower:           Watt.  FromW(50000),
                                                                            ChargingTariffIds:  [ Normal ]) ],
                    Created:             Created);

        private static RoamingNetwork ANetwork(ChargingConnector? CHAdeMO = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                           Created:          Created,
                                                                                           ChargingTariffs:  [ ATariff(Fast), ATariff(Normal) ],
                                                                                           ChargingPools:    [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                                Created:           Created,
                                                                                                                                ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                         Created:  Created,
                                                                                                                                                                         EVSEs:    [ AnEVSE(CHAdeMO) ]) ]) ]) ]).WithKeys();

        #endregion


        #region A_connector_has_its_own_values_else_those_of_its_EVSE()

        [Test]
        public void A_connector_has_its_own_values_else_those_of_its_EVSE()
        {

            var evse     = AnEVSE();
            var ccs      = evse.ChargingConnectors.Single(connector => connector.Type == ChargingConnectorType.IEC_62196_T2_COMBO);
            var chademo  = evse.ChargingConnectors.Single(connector => connector.Type == ChargingConnectorType.CHAdeMO);
            var json     = (JObject) evse.ToJSON()["chargingConnectors"]!.Single(connector => connector["@id"]!.Value<String>() == "2")!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(ccs.    MaxPowerAt         (evse)?.Value,  Is.EqualTo(150000));
                Assert.That(ccs.    ChargingTariffIdsAt(evse),         Is.EqualTo(new[] { Fast }));
                Assert.That(ccs.    CurrentTypesAt     (evse),         Is.EqualTo(CurrentTypes.DC));
                Assert.That(chademo.MaxPowerAt         (evse)?.Value,  Is.EqualTo(50000));
                Assert.That(chademo.MaxVoltageAt       (evse)?.Value,  Is.EqualTo(500));
                Assert.That(chademo.MaxCurrentAt       (evse)?.Value,  Is.EqualTo(125));
                Assert.That(chademo.ChargingTariffIdsAt(evse),         Is.EqualTo(new[] { Normal }));
                Assert.That(chademo.CurrentTypesAt     (evse),         Is.EqualTo(CurrentTypes.DC), "Of its EVSE.");

                // Its own only: what it takes from its EVSE is not written.
                Assert.That(json.Properties().Select(property => property.Name),
                            Is.EqualTo(new[] { "@id", "type", "maxVoltage", "maxCurrent", "maxPower", "chargingTariffIds" }));
                Assert.That(json["maxPower"]!.Value<Decimal>(),                Is.EqualTo(50000));
                Assert.That(json["chargingTariffIds"]!.Values<String>(),       Is.EqualTo(new[] { Normal.ToString() }));

                Assert.That(ChargingConnector.TryParse(json, out var parsed, out var error),  Is.True, error);
                Assert.That(parsed,                                            Is.EqualTo(chademo));

                // Unspecified current types are none of its own.
                Assert.That(new ChargingConnector(ChargingConnector_Id.Parse("3"), ChargingConnectorType.CHAdeMO, CurrentTypes: CurrentTypes.Unspecified).CurrentTypes,
                            Is.Null);

            }

        }

        #endregion

        #region A_network_with_connector_values_survives_JSON_and_CBOR()

        [Test]
        public void A_network_with_connector_values_survives_JSON_and_CBOR()
        {

            var rn1   = ANetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromJSON!.ToJSON().ToString(),  Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),  Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR.CalcETags(),           Is.EqualTo(rn1.CalcETags()));
            }

        }

        #endregion

        #region Invalid_connector_values_are_refused()

        [Test]
        public void Invalid_connector_values_are_refused()
        {

            var json = ANetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change((JObject) changed["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["chargingConnectors"]![1]!);
                return changed;
            }

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("negative power",    Changed(connector => connector["maxPower"]          = -1),                                      "electrical limits of the charging connector '2' must not be negative"),
                ("unknown tariff",    Changed(connector => connector["chargingTariffIds"] = new JArray("DE*GEF*TNONE")),              "charging tariff 'DE*GEF*TNONE' of the charging connector '2'"),
                ("tariff twice",      Changed(connector => connector["chargingTariffIds"] = new JArray(Normal.ToString(), Normal.ToString())), "twice"),
                ("unknown current",   Changed(connector => connector["currentTypes"]      = new JArray("Plasma")),                    "current type")
            };

            using (Assert.EnterMultipleScope())
            {
                foreach (var (name, invalid, error) in cases)
                {
                    Assert.That(RoamingNetwork.TryParse(invalid, out _, out var errorResponse),  Is.False, name);
                    Assert.That(errorResponse,                                                   Does.Contain(error), name);
                }
            }

        }

        #endregion

        #region Connector_values_change_with_the_connectors_of_their_EVSE()

        [Test]
        public void Connector_values_change_with_the_connectors_of_their_EVSE()
        {

            var rn1       = ANetwork();
            var evse      = rn1.GetChargingStationOperatorById(GEF)!.EVSEs.Single();

            // The CHAdeMO connector now of 62.5 kW, with the tariffs of its EVSE.
            var faster    = new JArray(evse.ChargingConnectors.Select(connector => connector.Type == ChargingConnectorType.CHAdeMO
                                                                                       ? new ChargingConnector(connector.Id, connector.Type, MaxPower: Watt.FromW(62500)).ToJSON()
                                                                                       : connector.ToJSON()));

            var rn2       = rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "chargingConnectors", null, faster) ], CreatedAt: T0));
            var evse2     = rn2.GetChargingStationOperatorById(GEF)!.EVSEs.Single();
            var chademo   = evse2.ChargingConnectors.Single(connector => connector.Type == ChargingConnectorType.CHAdeMO);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(chademo.MaxPowerAt(evse2)?.Value,       Is.EqualTo(62500));
                Assert.That(chademo.ChargingTariffIdsAt(evse2),     Is.EqualTo(new[] { Fast }));

                // A tariff a connector names is not removed.
                Assert.That(() => rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([ RoamingNetworkChange.Remove(Normal) ], CreatedAt: T0)),
                            Throws.ArgumentException.With.Message.Contains("charging connector '2'"));
            }

        }

        #endregion

    }

}
