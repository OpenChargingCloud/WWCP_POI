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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// A version as the content holds it (SPECIFICATION.md 2, Written forms): what a roaming network writes again once
    /// it has read it. A version in another form would be written in its own, another text than the one its ETags were
    /// calculated over by whoever wrote it, and is refused, in the words of WWCP_POI.TS.
    /// </summary>
    [TestFixture]
    public class VersionWrittenFormTests
    {

        #region (static) Version()

        /// <summary>
        /// The network of the station proof tests with its test keys, as POI writes it, read as POIJSON.Read() reads it.
        /// </summary>
        private static JObject Version()

            => (JObject) POIJSON.Read(StationProofTests.NewNetwork().ToJSON().ToString(Formatting.None));

        #endregion

        #region (static) Refusal(Change)

        /// <summary>
        /// The refusal of the version changed as given, read from its text without its ETags; null when it is taken.
        /// </summary>
        private static String? Refusal(Action<JObject> Change)
        {

            var json = Version();
            json.Remove("eTags");

            Change(json);

            return RoamingNetwork.TryParse(json.ToString(Formatting.None), out _, out var error)
                       ? null
                       : error;

        }

        #endregion

        #region (static) GEF(Network) / Pool(Network)

        private static JObject GEF (JObject Network)
            => (JObject) Network["chargingStationOperators"]![0]!;

        private static JObject Pool(JObject Network)
            => (JObject) GEF(Network)["chargingPools"]![0]!;

        #endregion


        #region A_version_as_written_is_taken()

        /// <summary>
        /// A version as POI writes it is taken: with or without its ETags and its JSON-LD context, and also read by a reader
        /// making dates of its timestamps, whose instants are compared then.
        /// </summary>
        [Test]
        public void A_version_as_written_is_taken()
        {

            var json = Version();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetwork.TryParse(json.ToString(Formatting.None), out var network, out var error),  Is.True, error);
                Assert.That(network!.ToJSON().ToString(Formatting.None),                                              Is.EqualTo(json.ToString(Formatting.None)), "written again as read");

                Assert.That(Refusal(_ => { }),                                                                        Is.Null, "without its ETags");
                Assert.That(Refusal(version => version.Remove("@context")),                                           Is.Null, "without its JSON-LD context");

                Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString(Formatting.None)), out _, out error),  Is.True, error);

            }

        }

        #endregion

        #region A_version_in_CBOR_as_written()

        /// <summary>
        /// The CBOR of a version, read as JSON as its entities read it, is the one it writes again: in the same words.
        /// </summary>
        [Test]
        public void A_version_in_CBOR_as_written()
        {

            var cbor  = StationProofTests.NewNetwork().ToCBOR();
            var json  = (JObject) CBORJSON.ToJSON(cbor);

            ((JObject) json["chargingStationOperators"]![0]!["chargingPools"]![0]!)["paymentOptions"] = new JArray("Contract", "Cash");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetwork.TryParse(cbor, out _, out var error),                     Is.True, error);

                Assert.That(RoamingNetwork.TryParse(CBORJSON.ToCBOR(json), out _, out error),         Is.False);
                Assert.That(error,                                                                    Is.EqualTo("'chargingStationOperators[0].chargingPools[0].paymentOptions' is not written as the content holds it: its elements are in the order the content holds them in!"));

            }

        }

        #endregion

        #region Lists_in_the_order_the_content_holds_them_in()

        /// <summary>
        /// Embedded entities in the ordinal order of their identifications, predefined values in the ordinal order of their
        /// texts, trimmed.
        /// </summary>
        [Test]
        public void Lists_in_the_order_the_content_holds_them_in()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Refusal(version => GEF(version)["chargingTariffs"] = new JArray(((JArray) GEF(version)["chargingTariffs"]!).Reverse())),
                            Is.EqualTo("'chargingStationOperators[0].chargingTariffs' is not written as the content holds it: its elements are in the order the content holds them in!"));

                Assert.That(Refusal(version => Pool(version)["paymentOptions"] = new JArray("Cash", "Contract")),  Is.Null);

                Assert.That(Refusal(version => Pool(version)["paymentOptions"] = new JArray("Contract", "Cash")),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].paymentOptions' is not written as the content holds it: its elements are in the order the content holds them in!"));

                Assert.That(Refusal(version => Pool(version)["paymentOptions"] = new JArray(" Cash", "Contract")),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].paymentOptions' is not written as the content holds it: the texts of its elements are trimmed!"));

            }

        }

        #endregion

        #region Texts_timestamps_and_numbers_as_written()

        /// <summary>
        /// The languages of a text in lower case and known to Styx, timestamps UTC to the millisecond, physical quantities
        /// with the decimal places given, geographic coordinates without trailing zeros.
        /// </summary>
        [Test]
        public void Texts_timestamps_and_numbers_as_written()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Refusal(version => Pool(version)["name"] = new JObject(new JProperty("en", "Pool 1"))),  Is.Null);

                Assert.That(Refusal(version => Pool(version)["name"] = new JObject(new JProperty("EN", "Pool 1"))),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].name' is not written as the content holds it: a text in languages names each language as Styx writes it, in lower case, with a text!"));

                Assert.That(Refusal(version => Pool(version)["name"] = new JObject(new JProperty("en", "Pool 1"), new JProperty("xx", "?"))),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].name' is not written as the content holds it: a text in languages names each language as Styx writes it, in lower case, with a text!"));

                Assert.That(Refusal(version => Pool(version)["created"] = "2026-01-01T01:00:00.000+01:00"),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].created' is not written as the content holds it: a timestamp is UTC to the millisecond, yyyy-MM-ddTHH:mm:ss.fffZ!"));

                Assert.That(GEF(Version())["EVSETemplates"]![0]!["maxPower"]!.ToString(Formatting.None),            Is.EqualTo("22000"));
                Assert.That(Refusal(version => GEF(version)["EVSETemplates"]![0]!["maxPower"] = 22000.0m),  Is.Null);

                Assert.That(Refusal(version => Pool(version)["geoLocation"] = new JObject(new JProperty("lat", 52.5m),  new JProperty("lng", 13.4m))),   Is.Null);

                Assert.That(Refusal(version => Pool(version)["geoLocation"] = new JObject(new JProperty("lat", 52.50m), new JProperty("lng", 13.4m))),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].geoLocation.lat' is not written as the content holds it: the content holds it as '52.5'!"));

            }

        }

        #endregion

        #region What_the_content_leaves_out()

        /// <summary>
        /// Neither a list without elements nor a property without a value, no JSON-LD context of an embedded object, no
        /// property the content does not know; and what it holds, e.g. a last change, is not left out.
        /// </summary>
        [Test]
        public void What_the_content_leaves_out()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Refusal(version => Pool(version)["paymentOptions"] = new JArray()),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].paymentOptions' is not written as the content holds it: a list without elements is left out!"));

                Assert.That(Refusal(version => Pool(version)["description"] = JValue.CreateNull()),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].description' is not written as the content holds it: a property without a value is left out!"));

                Assert.That(Refusal(version => GEF(version)["signingKeys"]![0]!["@context"] = POIPublicKey.JSONLDContext),
                            Is.EqualTo("'chargingStationOperators[0].signingKeys[0]' is not written as the content holds it: embedded, an object names no JSON-LD context!"));

                Assert.That(Refusal(version => Pool(version)["colour"] = "green"),
                            Is.EqualTo("'chargingStationOperators[0].chargingPools[0].colour' is not written as the content holds it: the content leaves it out!"));

                Assert.That(Refusal(version => Pool(version).Remove("lastChange")),
                            Does.StartWith("'chargingStationOperators[0].chargingPools[0].lastChange' is not written as the content holds it: the content holds it as '\""));

            }

        }

        #endregion

        #region A_key_of_a_party_names_nothing_else()

        /// <summary>
        /// A key of a party names nothing else than what a key has: another property is refused where the key is read, in
        /// the words of WWCP_POI.TS, before the version is compared with what it writes.
        /// </summary>
        [Test]
        public void A_key_of_a_party_names_nothing_else()
        {

            var keyId = GEF(Version())["signingKeys"]![0]!["@id"]!.Value<String>();

            Assert.That(Refusal(version => GEF(version)["signingKeys"]![0]!["x"] = 1),
                        Does.Contain($"The public key '{keyId}' must not name the property 'x'!"));

        }

        #endregion

    }

}
