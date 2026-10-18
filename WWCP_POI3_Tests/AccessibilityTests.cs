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
    /// What a charging pool, an EVSE and a charging connector tell about their accessibility, as the OCPI
    /// Accessibility Extension: the services and standards of a pool, the reach distance and timeouts of an EVSE,
    /// the format, cable and standards of a connector.
    /// </summary>
    [TestFixture]
    public class AccessibilityTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-16T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(P1,
                                                                                                                Services:                  [ LocationService.ASSISTANCE, LocationService.ACCESSIBLE_CHARGING ],
                                                                                                                AssistanceServiceDetails:  I18NString.Create(Languages.en, "Ring at the kiosk, 6:00 to 22:00"),
                                                                                                                Standards:                 [ Standard.PAS1899_2022 ],
                                                                                                                Created:                   Created,
                                                                                                                ChargingStations:          [ new ChargingStation(S1,
                                                                                                                                                                 Created:  Created,
                                                                                                                                                                 EVSEs:    [ new EVSE(E1,
                                                                                                                                                                                      ChargingConnectors:        [ new ChargingConnector(ChargingConnector_Id.Parse("1"),
                                                                                                                                                                                                                                         ChargingConnectorType.IEC_62196_T2_COMBO,
                                                                                                                                                                                                                                         Format:                 ChargingConnectorFormat.CABLE,
                                                                                                                                                                                                                                         CableLength:            Meter.   From_m (5.5m),
                                                                                                                                                                                                                                         CableWeight:            Kilogram.FromKG(2.4m),
                                                                                                                                                                                                                                         CableManagementSystem:  true,
                                                                                                                                                                                                                                         Standards:              [ Standard.PAS1899_2022 ],
                                                                                                                                                                                                                                         Images:                 [ new Image(URL.Parse("https://gef.example/connector.jpg"), HTTPContentType.Image.JPEG, ImageCategory.CHARGER) ]) ],
                                                                                                                                                                                      ReachDistance:             Meter.From_m(0.6m),
                                                                                                                                                                                      OperationTimeout:          TimeSpan.FromSeconds(90),
                                                                                                                                                                                      ExtendedOperationTimeout:  true,
                                                                                                                                                                                      Standards:                 [ Standard.PAS1899_2022 ],
                                                                                                                                                                                      Created:                   Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion

        #region Pool(Network) / EVSE(Network) / Connector(Network)

        private static JObject Pool     (JObject Network)
            => (JObject) Network["chargingStationOperators"]![0]!["chargingPools"]![0]!;

        private static JObject EVSE     (JObject Network)
            => (JObject) Pool(Network)["chargingStations"]![0]!["EVSEs"]![0]!;

        private static JObject Connector(JObject Network)
            => (JObject) EVSE(Network)["chargingConnectors"]![0]!;

        #endregion


        #region Accessibility_survives_JSON_JSON_text_and_CBOR()

        [Test]
        public void Accessibility_survives_JSON_JSON_text_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var pool       = Pool     (json);
            var evse       = EVSE     (json);
            var connector  = Connector(json);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromText.CalcETags(),                                              Is.EqualTo(rn1.CalcETags()));
                Assert.That(fromCBOR.CalcETags(),                                              Is.EqualTo(rn1.CalcETags()));

                // Sets in the order of their names.
                Assert.That(pool["services"]!.Values<String>(),                                Is.EqualTo(new[] { "ACCESSIBLE_CHARGING", "ASSISTANCE" }));
                Assert.That(pool["assistanceServiceDetails"]!["en"]!.Value<String>(),          Is.EqualTo("Ring at the kiosk, 6:00 to 22:00"));
                Assert.That(pool["standards"]!.Values<String>(),                               Is.EqualTo(new[] { "PAS 1899:2022" }));

                // In meters and seconds.
                Assert.That(evse["reachDistance"]!.Value<Decimal>(),                           Is.EqualTo(0.6m));
                Assert.That(evse["operationTimeout"]!.Value<Decimal>(),                        Is.EqualTo(90m));
                Assert.That(evse["extendedOperationTimeout"]!.Value<Boolean>(),                Is.True);
                Assert.That(evse["standards"]!.Values<String>(),                               Is.EqualTo(new[] { "PAS 1899:2022" }));

                // In meters and kilograms.
                Assert.That(connector["format"]!.Value<String>(),                              Is.EqualTo("CABLE"));
                Assert.That(connector["cableLength"]!.Value<Decimal>(),                        Is.EqualTo(5.5m));
                Assert.That(connector["cableWeight"]!.Value<Decimal>(),                        Is.EqualTo(2.4m));
                Assert.That(connector["cableManagementSystem"]!.Value<Boolean>(),              Is.True);
                Assert.That(connector["images"]!.Count(),                                      Is.EqualTo(1));

                var p1 = fromCBOR.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
                var e1 = p1.ChargingStations.Single().EVSEs.Single();
                var c1 = e1.ChargingConnectors.Single();

                Assert.That(p1.Services,                                                       Is.EqualTo(new[] { LocationService.ACCESSIBLE_CHARGING, LocationService.ASSISTANCE }));
                Assert.That(e1.ReachDistance?.m,                                               Is.EqualTo(0.6m));
                Assert.That(e1.OperationTimeout,                                               Is.EqualTo(TimeSpan.FromSeconds(90)));
                Assert.That(c1.Format,                                                         Is.EqualTo(ChargingConnectorFormat.CABLE));
                Assert.That(c1.CableWeight?.Value,                                             Is.EqualTo(2.4m));

                // Connectors are equal by what they say, their standards and images as well.
                Assert.That(c1,                                                                Is.EqualTo(rn1.GetChargingStationOperatorById(GEF)!.EVSEs.Single().ChargingConnectors.Single()));

            }

        }

        #endregion

        #region Nothing_is_written_without_accessibility()

        [Test]
        public void Nothing_is_written_without_accessibility()
        {

            var pool = new ChargingPool(P1, Created: Created).ToJSON();
            var evse = new EVSE(E1, ChargingConnectors: [ new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2) ], Created: Created).ToJSON();

            using (Assert.EnterMultipleScope())
            {
                foreach (var key in new[] { "services", "assistanceServiceDetails", "standards" })
                    Assert.That(pool[key],  Is.Null, key);

                foreach (var key in new[] { "reachDistance", "operationTimeout", "extendedOperationTimeout", "standards" })
                    Assert.That(evse[key],  Is.Null, key);

                Assert.That(((JObject) evse["chargingConnectors"]![0]!).Properties().Select(property => property.Name),
                            Is.EqualTo(new[] { "@id", "type" }));
            }

        }

        #endregion

        #region Invalid_accessibility_is_refused()

        [Test]
        public void Invalid_accessibility_is_refused()
        {

            var json = NewNetwork().ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("service twice",              Changed(network => Pool(network)["services"]                 = new JArray("ASSISTANCE", "assistance")),  "must not be given twice"),
                ("empty standard",             Changed(network => Pool(network)["standards"]                = new JArray(" ")),                         "standard ' '"),
                ("details not I18N",           Changed(network => Pool(network)["assistanceServiceDetails"] = 42),                                      "assistanceServiceDetails"),
                ("negative reach distance",    Changed(network => EVSE(network)["reachDistance"]            = -0.1),                                    "reach distance must not be negative"),
                ("negative timeout",           Changed(network => EVSE(network)["operationTimeout"]         = -1),                                      "operation timeout must not be negative"),
                ("extended timeout text",      Changed(network => EVSE(network)["extendedOperationTimeout"] = "yes"),                                   "extended operation timeout"),
                ("lower case format",          Changed(network => Connector(network)["format"]              = "cable"),                                 "charging connector format"),
                ("zero cable length",          Changed(network => Connector(network)["cableLength"]         = 0),                                       "cable length of the charging connector '1' must be positive"),
                ("negative cable weight",      Changed(network => Connector(network)["cableWeight"]         = -2),                                      "cable weight must not be negative"),
                ("socket with a cable",        Changed(network => Connector(network)["format"]              = "SOCKET"),                                "is a socket: it has no cable of its own")
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

        #region Accessibility_is_changed_by_change_sets()

        [Test]
        public void Accessibility_is_changed_by_change_sets()
        {

            var rn1    = NewNetwork();
            var pool   = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            var evse   = pool.ChargingStations.Single().EVSEs.Single();

            var socket = new JArray(new ChargingConnector(ChargingConnector_Id.Parse("1"), ChargingConnectorType.IEC_62196_T2, Format: ChargingConnectorFormat.SOCKET).ToJSON());

            var rn2    = Apply(rn1,
                               RoamingNetworkChange.UpdateProperty(pool,  "services",                  null, new JArray("PERIMETER_FENCE")),
                               RoamingNetworkChange.RemoveProperty(pool,  "assistanceServiceDetails"),
                               RoamingNetworkChange.UpdateProperty(evse,  "reachDistance",             null, 0.5m),
                               RoamingNetworkChange.RemoveProperty(evse,  "operationTimeout"),
                               RoamingNetworkChange.UpdateProperty(evse,  "chargingConnectors",        null, socket));

            var pool2  = rn2.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            var evse2  = pool2.ChargingStations.Single().EVSEs.Single();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(pool2.Services,                                         Is.EqualTo(new[] { LocationService.PERIMETER_FENCE }));
                Assert.That(pool2.AssistanceServiceDetails.IsNullOrEmpty(),         Is.True);
                Assert.That(pool2.Standards,                                        Is.EqualTo(new[] { Standard.PAS1899_2022 }));
                Assert.That(evse2.ReachDistance?.m,                                 Is.EqualTo(0.5m));
                Assert.That(evse2.OperationTimeout,                                 Is.Null);
                Assert.That(evse2.ChargingConnectors.Single().Format,                Is.EqualTo(ChargingConnectorFormat.SOCKET));
                Assert.That(evse2.ChargingConnectors.Single().CableLength,          Is.Null);

                // A socket with a cable of its own is refused.
                var cable = new JArray(socket[0]!.DeepClone());
                cable[0]!["cableLength"] = 3;

                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(evse2, "chargingConnectors", null, cable)),
                            Throws.ArgumentException.With.Message.Contains("is a socket: it has no cable of its own"));

            }

        }

        #endregion

        #region Floor_level_and_accepted_providers_of_EVSEs_and_their_groups()

        [Test]
        public void Floor_level_and_accepted_providers_of_EVSEs_and_their_templates()
        {

            var GDF   = EMobilityProvider_Id.Parse("DE-GDF");
            var ABC   = EMobilityProvider_Id.Parse("DE-ABC");
            var XYZ   = EMobilityProvider_Id.Parse("DE-XYZ");

            var evse  = new EVSE(E1, FloorLevel: "-1", AcceptedEMobilityProviderIds: [ GDF, ABC ], Created: Created);
            var json  = evse.ToJSON();

            // An EVSE without floor level and providers, which its template gives.
            var rn1   = new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                           Created:                   Created,
                                           ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                                    Created:        Created,
                                                                                                    ChargingPools:  [ new ChargingPool(P1,
                                                                                                                                       Created:           Created,
                                                                                                                                       ChargingStations:  [ new ChargingStation(S1, Created: Created, EVSEs: [ new EVSE(E1, AcceptedEMobilityProviderIds: [ ABC ], TemplateIds: [ EVSETemplate_Id.Parse(GEF, "1") ], Created: Created) ]) ]) ],
                                                                                                    EVSETemplates:  [ new EVSETemplate(EVSETemplate_Id.Parse(GEF, "1"),
                                                                                                                                    FloorLevel:                    "-2",
                                                                                                                                    AcceptedEMobilityProviderIds:  [ XYZ ],
                                                                                                                                    Created:                       Created) ]) ]).WithKeys();

            var cso   = rn1.GetChargingStationOperatorById(GEF)!;
            var rn2   = Apply(rn1, RoamingNetworkChange.UpdateProperty(cso.EVSEs.Single(), "floorLevel", null, "2B"));

            using (Assert.EnterMultipleScope())
            {

                // Providers in the order of their identifications.
                Assert.That(json["floorLevel"]!.Value<String>(),                                Is.EqualTo("-1"));
                Assert.That(json["acceptedEMobilityProviderIds"]!.Values<String>(),             Is.EqualTo(new[] { "DE-ABC", "DE-GDF" }));
                Assert.That(global::cloud.charging.open.protocols.WWCP.POI.EVSE.TryParse(json, out var parsed, out var error),                 Is.True, error);
                Assert.That(parsed!.ToJSON().ToString(),                                        Is.EqualTo(json.ToString()));

                Assert.That(new EVSE(E1, Created: Created).ToJSON()["acceptedEMobilityProviderIds"],  Is.Null);

                var twice = (JObject) json.DeepClone();
                twice["acceptedEMobilityProviderIds"] = new JArray("DE-GDF", "DE-GDF");
                Assert.That(global::cloud.charging.open.protocols.WWCP.POI.EVSE.TryParse(twice, out _, out _),                                 Is.False);

                // The template gives its floor level, and its providers besides the EVSE's own.
                Assert.That(cso.EffectiveEVSE(cso.EVSEs.Single()).FloorLevel,                   Is.EqualTo("-2"));
                Assert.That(cso.EffectiveEVSE(cso.EVSEs.Single()).AcceptedEMobilityProviderIds,  Is.EquivalentTo(new[] { ABC, XYZ }));
                var networkJSON = rn1.ToJSON(IncludeETags: false);
                Assert.That(RoamingNetwork.TryParse(networkJSON, out var fromJSON, out var errorJSON),  Is.True, errorJSON);
                Assert.That(fromJSON!.ToJSON(IncludeETags: false).ToString(),                  Is.EqualTo(networkJSON.ToString()));

                // Changed by change sets; its own floor level before its template's.
                var cso2 = rn2.GetChargingStationOperatorById(GEF)!;
                Assert.That(cso2.EVSEs.Single().FloorLevel,                                     Is.EqualTo("2B"));
                Assert.That(cso2.EffectiveEVSE(cso2.EVSEs.Single()).FloorLevel,                 Is.EqualTo("2B"));

            }

        }

        #endregion

    }

}
