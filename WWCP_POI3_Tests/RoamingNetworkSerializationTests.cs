/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The roaming network in JSON and in CBOR: everything it has is written under the keys of its PropertyKeys and
    /// read back, from its JSON as from its CBOR.
    /// </summary>
    [TestFixture]
    public class RoamingNetworkSerializationTests
    {

        #region Data

        private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-10-19T12:00:00Z");

        #endregion

        #region (static) Full()

        /// <summary>
        /// A roaming network with everything it can have: the children of a test network, an operator with a key
        /// statement, data licenses, two tree hash algorithms, metadata and its ETags.
        /// </summary>
        internal static RoamingNetwork Full()
        {

            var basis    = Proofs.StationProofTests.NewNetwork();

            var network  = new RoamingNetwork(
                               basis.Id,
                               Name:                              I18NString.Create(Languages.en, "Example Roaming"),
                               Description:                       I18NString.Create(Languages.de, "Ein Roaming-Netz zum Testen"),
                               DataSource:                        "test",
                               Created:                           T0,
                               LastChange:                        T0.AddHours(2),
                               CustomData:                        CustomDataNew.ParseJObject(new JObject(new JProperty("vendor", new JObject(new JProperty("tier", 2))))),
                               DataLicenses:                      [ DataLicense.OpenDatabaseLicense, DataLicense.CreativeCommons_BY_4 ],
                               ChargingStationOperators:          basis.ChargingStationOperators,
                               ChargingStationManufacturers:      basis.ChargingStationManufacturers,
                               EMobilityProviders:                basis.EMobilityProviders,
                               GridOperators:                     basis.GridOperators,
                               ParkingOperators:                  basis.ParkingOperators,
                               TransparencySoftwareManufacturers: basis.TransparencySoftwareManufacturers,
                               ConformityAssessmentBodies:        basis.ConformityAssessmentBodies,
                               EnergyMeterManufacturers:          basis.EnergyMeterManufacturers,
                               Operator:                          RoamingNetworkOperatorSerializationTests.Full(),
                               TreeHashAlgorithms:                [ ETagHashAlgorithm.SHA256, ETagHashAlgorithm.SHA512 ]
                           );

            network.CalcETags();

            return network;

        }

        #endregion

        #region (static) PropertyKeys(Filter)

        private static String[] PropertyKeys(Func<String, Boolean> Filter)

            => new[] { typeof(RoamingNetwork.PropertyKeys), typeof(RoamingNetwork.BasePropertyKeys) }.
                   SelectMany(type => type.GetFields()).
                   Distinct().
                   Where (field => field.IsLiteral).
                   Select(field => (String) field.GetRawConstantValue()!).
                   Where (Filter).
                   Order ().
                   ToArray();

        #endregion


        #region EveryPropertyIsAPropertyKey()

        /// <summary>
        /// The network with everything it can have writes only keys of its PropertyKeys and BasePropertyKeys, in JSON as
        /// in CBOR, and those of what it has: the identifications of its data licenses and charging station operators
        /// only when asked to show only those.
        /// </summary>
        [Test]
        public void EveryPropertyIsAPropertyKey()
        {

            var network     = Full();
            var allKeys     = PropertyKeys(key => true);
            var expected    = new[] { RoamingNetwork.BasePropertyKeys.Id,
                                      RoamingNetwork.BasePropertyKeys.Context,
                                      RoamingNetwork.BasePropertyKeys.Name,
                                      RoamingNetwork.BasePropertyKeys.Description,
                                      RoamingNetwork.BasePropertyKeys.DataSource,
                                      RoamingNetwork.BasePropertyKeys.DataLicenses,
                                      RoamingNetwork.BasePropertyKeys.Created,
                                      RoamingNetwork.BasePropertyKeys.LastChange,
                                      RoamingNetwork.BasePropertyKeys.CustomData,
                                      RoamingNetwork.BasePropertyKeys.ETags,
                                      RoamingNetwork.PropertyKeys.Operator,
                                      RoamingNetwork.PropertyKeys.ChargingStationOperators,
                                      RoamingNetwork.PropertyKeys.ChargingStationManufacturers,
                                      RoamingNetwork.PropertyKeys.GridOperators,
                                      RoamingNetwork.PropertyKeys.ParkingOperators,
                                      RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,
                                      RoamingNetwork.PropertyKeys.TransparencySoftwareManufacturers,
                                      RoamingNetwork.PropertyKeys.ConformityAssessmentBodies,
                                      RoamingNetwork.PropertyKeys.TreeHashAlgorithms };
            var idsOnly     = network.ToJSON(ExpandDataLicenses:                InfoStatus.ShowIdOnly,
                                             ExpandChargingStationOperatorIds:  InfoStatus.ShowIdOnly);

            using (Assert.EnterMultipleScope())
            {

                foreach (var (format, written) in new[] { ("JSON", network.ToJSON()), ("CBOR", (JObject) CBORJSON.ToJSON(network.ToCBOR())) })
                    Assert.That(written.Properties().Select(property => property.Name).Order(),  Is.EqualTo(expected.Order()), format);

                Assert.That(idsOnly.Properties().Select(property => property.Name),                     Is.SubsetOf(allKeys));
                Assert.That(idsOnly.ContainsKey(RoamingNetwork.BasePropertyKeys.DataLicenseIds),           Is.True);
                Assert.That(idsOnly.ContainsKey(RoamingNetwork.PropertyKeys.ChargingStationOperatorIds),   Is.True);

            }

        }

        #endregion

        #region AFullNetworkIsReadBackFromItsJSONAndFromItsCBOR()

        /// <summary>
        /// Read from its JSON and from its CBOR - its ETags checked on the way - the network is written as it was, its
        /// operator with its key statement among it.
        /// </summary>
        [Test]
        public void AFullNetworkIsReadBackFromItsJSONAndFromItsCBOR()
        {

            var network  = Full();
            var json     = network.ToJSON(IncludeETags: false);
            var cbor     = Convert.ToHexString(network.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical));

            Assert.That(RoamingNetwork.TryParse(JObject.Load(new Newtonsoft.Json.JsonTextReader(new StringReader(network.ToJSON().ToString())) { DateParseHandling = Newtonsoft.Json.DateParseHandling.None }),                            out var fromJSON, out var jsonError),  Is.True, jsonError);
            Assert.That(RoamingNetwork.TryParse(CBORValue.Parse(network.ToCBOR().ToByteArray(CBORWriterOptions.Canonical)),     out var fromCBOR, out var cborError),  Is.True, cborError);

            using (Assert.EnterMultipleScope())
            {

                foreach (var (format, again) in new[] { ("JSON", fromJSON), ("CBOR", fromCBOR) })
                {
                    Assert.That(JToken.DeepEquals(again.ToJSON(IncludeETags: false), json),                                                 Is.True,  format);
                    Assert.That(Convert.ToHexString(again.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical)),     Is.EqualTo(cbor), format);
                    Assert.That(again.Operator?.KeyStatement,                                                                               Is.Not.Null, format);
                    Assert.That(again.GivenTreeHashAlgorithms,                                                                              Is.EqualTo(new[] { ETagHashAlgorithm.SHA256, ETagHashAlgorithm.SHA512 }), format);
                    Assert.That(again.CalcETags(),                                                                                          Is.EqualTo(network.ETags), format);
                }

            }

        }

        #endregion

        #region TheCBORIsTheCBOROfTheJSON()

        /// <summary>
        /// The CBOR, built directly, has the keys and values of the JSON: its data licenses, its tree hash algorithms,
        /// its operator and its metadata included.
        /// </summary>
        [Test]
        public void TheCBORIsTheCBOROfTheJSON()
        {

            var network  = Full();
            var direct   = network.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical);
            var viaJSON  = CBORJSON.ToCBOR(network.ToJSON(IncludeETags: false)).ToByteArray(CBORWriterOptions.Canonical);

            Assert.That(Convert.ToHexString(direct), Is.EqualTo(Convert.ToHexString(viaJSON)));

        }

        #endregion

        #region WhatIsNoNetworkOrOperatorInCBORIsRefused()

        [Test]
        public void WhatIsNoNetworkOrOperatorInCBORIsRefused()
        {

            var json             = Full().ToJSON(IncludeETags: false);
            json[RoamingNetwork.PropertyKeys.Operator] = "GraphDefined";

            var withKeys         = Full().ToJSON(IncludeETags: false);
            withKeys[POIPartyKeys.SigningKeysProperty] = new JArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetwork.TryParse(CBORValue.FromText("rn1"),  out _, out var notAMap),       Is.False);
                Assert.That(notAMap,                                                                             Does.Contain("must be a map"));
                Assert.That(RoamingNetwork.TryParse(CBORJSON.ToCBOR(json),     out _, out var noOperator),    Is.False);
                Assert.That(noOperator,                                                                          Does.Contain("roaming network operator must be a map"));
                Assert.That(RoamingNetwork.TryParse(CBORJSON.ToCBOR(withKeys), out _, out var ownKeys),       Is.False);
                Assert.That(ownKeys,                                                                             Does.Contain($"'{RoamingNetwork.PropertyKeys.Operator}'"));
            }

        }

        #endregion

    }

}
