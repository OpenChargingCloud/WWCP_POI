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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.ChangeSets
{

    /// <summary>
    /// JSON text read as it was written: a number a decimal cannot hold as written, and an object naming a property
    /// twice, are refused instead of being read as something else.
    /// </summary>
    /// <remarks>
    /// Read as a decimal, 1e-30 was 0 with 28 decimal places, and 0.1 with 30 zeros after it lost two of them: a
    /// change set with such a value in its custom data was taken, and its content was not the one its sender signed.
    /// Of a property named twice the last one won, where another reader takes the first.
    /// </remarks>
    [TestFixture]
    public class ExactJSONTests
    {

        #region Data

        private const String Placeholder = "\"@@value@@\"";

        /// <summary>
        /// The JSON text of a change set with the given JSON text as the custom data of its roaming network.
        /// </summary>
        private static String ChangeSetText(String CustomData)
        {

            var rn1    = new RoamingNetwork(Id: RoamingNetwork_Id.Parse("rn1"), DataSource: "test");
            var eTags  = rn1.CalcETags();

            var text   = new RoamingNetworkChangeSet(
                             Id:                ChangeSet_Id.Parse("cs1"),
                             RoamingNetworkId:  rn1.Id,
                             CreatedAt:         DateTimeOffset.Parse("2026-10-20T12:00:00Z"),
                             Changes:           [ RoamingNetworkChange.UpdateProperty(rn1, "customData", null, new JObject(new JProperty("value", "@@value@@"))) ],
                             BeforeETags:       eTags,
                             AfterETags:        eTags
                         ).ToJSON().ToString(Newtonsoft.Json.Formatting.None).
                           Replace("{\"value\":" + Placeholder + "}", CustomData);

            // Identified by its content as written, where a decimal holds it: otherwise it is refused for what it holds.
            try
            {

                var json = (JObject) JToken.ReadFrom(new JsonTextReader(new StringReader(text)) { FloatParseHandling = FloatParseHandling.Decimal,
                                                                                                  DateParseHandling  = DateParseHandling.None });
                json.Remove("@id");

                return text.Replace("\"cs1\"", $"\"{Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(CanonicalJSON.ToUTF8Bytes(json)))}\"");

            }
            catch (JsonException)
            {
                return text;
            }

        }

        #endregion


        #region A_change_set_keeps_the_numbers_of_its_custom_data_as_written(Number)

        [TestCase("1.50")]
        [TestCase("0.0000001")]
        [TestCase("0.1234567890123456789012345678")]
        [TestCase("7922816251426433759354395033.5")]
        [TestCase("-0.000")]
        [TestCase("123456789012345678901234567890")]
        public void A_change_set_keeps_the_numbers_of_its_custom_data_as_written(String Number)
        {

            Assert.That(RoamingNetworkChangeSet.TryParse(ChangeSetText("{\"value\":" + Number + "}"), out var changeSet, out var error), Is.True, error);

            Assert.That(CanonicalJSON.Serialize(changeSet!.Changes[0].NewValue!),
                        Is.EqualTo("{\"value\":" + Number.TrimStart('-') + "}"));

        }

        #endregion

        #region A_change_set_refuses_a_number_a_decimal_cannot_hold_as_written(Number)

        [TestCase("1e-30")]
        [TestCase("0.100000000000000000000000000000")]
        [TestCase("0.12345678901234567890123456789")]
        [TestCase("7922816251426433759354395033.6")]
        [TestCase("1e30")]
        public void A_change_set_refuses_a_number_a_decimal_cannot_hold_as_written(String Number)
        {

            Assert.That(RoamingNetworkChangeSet.TryParse(ChangeSetText("{\"value\":" + Number + "}"), out var changeSet, out var error),
                        Is.False,
                        changeSet is not null ? CanonicalJSON.Serialize(changeSet.Changes[0].NewValue!) : null);

            Assert.That(error, Does.Contain(Number));

        }

        #endregion

        #region A_change_set_refuses_a_property_named_twice()

        [Test]
        public void A_change_set_refuses_a_property_named_twice()
        {

            Assert.That(RoamingNetworkChangeSet.TryParse(ChangeSetText("{\"value\":1,\"value\":2}"), out var changeSet, out var error),
                        Is.False,
                        changeSet is not null ? CanonicalJSON.Serialize(changeSet.Changes[0].NewValue!) : null);

            Assert.That(error, Does.Contain("value"));

        }

        #endregion

        #region A_WebSocket_message_refuses_a_number_a_decimal_cannot_hold_and_a_property_named_twice()

        [Test]
        public void A_WebSocket_message_refuses_a_number_a_decimal_cannot_hold_and_a_property_named_twice()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(POIWebSocketMessage.TryParse("""[2, "m1", "setStatus", { "value": 1.5 }]""",                out _, out var error1), Is.True,  error1?.ToString());
                Assert.That(POIWebSocketMessage.TryParse("""[2, "m2", "setStatus", { "value": 1e-30 }]""",              out _, out _),          Is.False);
                Assert.That(POIWebSocketMessage.TryParse("""[2, "m3", "setStatus", { "value": 1, "value": 2 }]""",      out _, out _),          Is.False);

            }

        }

        #endregion


        #region The_base_of_a_store_is_written_and_read_as_a_stream(Number, Held)

        /// <summary>
        /// The base of a file store, written and read as a stream, as the base of a large roaming network is longer than a
        /// string may be: it has the bytes of the canonical JSON, and is read as text is read, also across the buffers of
        /// the reader, a number cut at the end of one, and a number a decimal cannot hold, which is refused.
        /// </summary>
        [TestCase("1.50",                              true)]
        [TestCase("150000",                            true)]
        [TestCase("1e-30",                             false)]
        [TestCase("7922816251426433759354395033.6",    false)]
        public void The_base_of_a_store_is_written_and_read_as_a_stream(String Number, Boolean Held)
        {

            var directory  = Path.Combine(Path.GetTempPath(), "poi3-base-" + Guid.NewGuid().ToString("N"));
            var filler     = String.Join(",", Enumerable.Range(0, 30_000).Select(i => $"{{\"name\":\"Ladepark Jena {i}\",\"power\":{i}.5}}"));
            var text       = $"{{\"pools\":[{filler}],\"value\":{(Held ? Number : "1")}}}";
            var snapshot   = (JObject) JToken.ReadFrom(new JsonTextReader(new StringReader(text)) { FloatParseHandling = FloatParseHandling.Decimal,
                                                                                                     DateParseHandling  = DateParseHandling.None });

            try
            {

                using (var store = new RoamingNetworkHistoryFileStore(directory))
                    store.WriteBase(snapshot);

                var baseFile = Path.Combine(directory, File.ReadAllText(Path.Combine(directory, "CURRENT")).Trim(), "base.json");

                Assert.That(File.ReadAllBytes(baseFile), Is.EqualTo(CanonicalJSON.ToUTF8Bytes(snapshot)));

                // The number last, behind more than one buffer of the reader.
                if (!Held)
                    File.WriteAllText(baseFile, text.Replace("\"value\":1}", $"\"value\":{Number}}}"));

                using (var store = new RoamingNetworkHistoryFileStore(directory))
                {

                    if (!Held)
                    {
                        Assert.That(() => store.Load(), Throws.InstanceOf<JsonReaderException>().With.Message.Contains(Number));
                        return;
                    }

                    var loaded = store.Load()!.Base;

                    Assert.That(JToken.DeepEquals(loaded, snapshot), Is.True);
                    Assert.That(loaded["value"]!.ToString(Formatting.None), Is.EqualTo(Number));

                }

            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }

        }

        #endregion

    }

}
