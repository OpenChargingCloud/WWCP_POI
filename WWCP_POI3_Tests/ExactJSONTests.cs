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
    /// JSON text read as it was written: a number a decimal cannot hold as written, an object naming a property twice,
    /// and what RFC 8259 does not call JSON, are refused instead of being read as something else.
    /// </summary>
    /// <remarks>
    /// Read as a decimal, 1e-30 was 0 with 28 decimal places, and 0.1 with 30 zeros after it lost two of them: a
    /// change set with such a value in its custom data was taken, and its content was not the one its sender signed.
    /// Of a property named twice the last one won, where another reader takes the first. Newtonsoft took a trailing
    /// comma, a comment, 0x10 and more, and dropped a second value after the first: a change set so written was taken
    /// here and refused by every reader of JSON (WWCPPOIConformanceTests, D1).
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

        #region A_change_set_refuses_what_is_no_JSON(CustomData, Behind)

        /// <summary>
        /// A change set whose text is no JSON as RFC 8259 writes it, which Newtonsoft takes, is refused: identified by its
        /// content as Newtonsoft reads it, it is refused for its text alone.
        /// </summary>
        [TestCase("{\"value\":[1,]}",           "")]
        [TestCase("{\"value\":1,}",             "")]
        [TestCase("{\"value\":1/* a comment */}", "")]
        [TestCase("{'value':1}",                "")]
        [TestCase("{value:1}",                  "")]
        [TestCase("{\"value\":01}",             "")]
        [TestCase("{\"value\":0x10}",           "")]
        [TestCase("{\"value\":\"a\tb\"}",       "")]
        [TestCase("{\"value\":1}",              " {\"value\":2}")]
        [TestCase("{\"value\":1}",              " // a comment")]
        public void A_change_set_refuses_what_is_no_JSON(String CustomData, String Behind)
        {

            Assert.That(RoamingNetworkChangeSet.TryParse(ChangeSetText(CustomData) + Behind, out var changeSet, out var error),
                        Is.False,
                        changeSet is not null ? CanonicalJSON.Serialize(changeSet.Changes[0].NewValue!) : null);

            Assert.That(error, Does.Contain("RFC 8259"));

        }

        #endregion

        #region A_change_set_takes_JSON_as_RFC_8259_writes_it(CustomData, Around)

        /// <summary>
        /// JSON as RFC 8259 writes it is taken: whitespace around and within it, escapes, also of an unpaired surrogate.
        /// </summary>
        [TestCase("{ \"value\" :\r\n\t[ 1 , 2 ] }",  " \r\n\t")]
        [TestCase("{\"value\":\"a\\tb\\u0000\"}",    "")]
        [TestCase("{\"value\":\"\\uD800x\"}",         "")]
        [TestCase("{\"value\":-0.5e-3}",             "\n")]
        public void A_change_set_takes_JSON_as_RFC_8259_writes_it(String CustomData, String Around)
        {

            Assert.That(RoamingNetworkChangeSet.TryParse(Around + ChangeSetText(CustomData) + Around, out _, out var error), Is.True, error);

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

                // What RFC 8259 does not call JSON, and a second value after the message.
                Assert.That(POIWebSocketMessage.TryParse("""[2, "m4", "setStatus", { "value": 1, }]""",                 out _, out _),          Is.False);
                Assert.That(POIWebSocketMessage.TryParse("""[2, "m5", "setStatus", { "value": 0x10 }]""",               out _, out _),          Is.False);
                Assert.That(POIWebSocketMessage.TryParse("""[2, "m6", "setStatus", { /* a comment */ "value": 1 }]""",  out _, out _),          Is.False);
                Assert.That(POIWebSocketMessage.TryParse("""[2, "m7", "setStatus", { "value": 1 }] [3, "m7", {}]""",    out _, out _),          Is.False);

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

        #region The_base_of_a_store_is_refused_where_it_is_no_JSON(Last)

        /// <summary>
        /// The base of a file store that is no JSON as RFC 8259 writes it is refused, also where it shows behind more than
        /// one buffer of the reader: a trailing comma, a comment, a second value after the first.
        /// </summary>
        [TestCase("\"value\":1,}")]
        [TestCase("\"value\":1/* a comment */}")]
        [TestCase("\"value\":1} {\"value\":2}")]
        public void The_base_of_a_store_is_refused_where_it_is_no_JSON(String Last)
        {

            var directory  = Path.Combine(Path.GetTempPath(), "poi3-base-" + Guid.NewGuid().ToString("N"));
            var filler     = String.Join(",", Enumerable.Range(0, 30_000).Select(i => $"{{\"name\":\"Ladepark Jena {i}\",\"power\":{i}}}"));
            var text       = $"{{\"pools\":[{filler}],\"value\":1}}";
            var snapshot   = (JObject) JToken.ReadFrom(new JsonTextReader(new StringReader(text)) { FloatParseHandling = FloatParseHandling.Decimal,
                                                                                                     DateParseHandling  = DateParseHandling.None });

            try
            {

                using (var store = new RoamingNetworkHistoryFileStore(directory))
                    store.WriteBase(snapshot);

                var baseFile = Path.Combine(directory, File.ReadAllText(Path.Combine(directory, "CURRENT")).Trim(), "base.json");

                File.WriteAllText(baseFile, text.Replace("\"value\":1}", Last));

                using (var store = new RoamingNetworkHistoryFileStore(directory))
                    Assert.That(() => store.Load(), Throws.InstanceOf<JsonReaderException>().With.Message.Contains("RFC 8259"));

            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }

        }

        #endregion


        #region POIJSON_reads_as_section_1_asks()

        /// <summary>
        /// POIJSON.Read, public for a client that verifies what it is given itself (WWCPPOIConformanceTests, O1): numbers
        /// with a fraction are the decimals they are written as, whole numbers integers, and a string that looks like a
        /// point in time stays the string it is signed as.
        /// </summary>
        [Test]
        public void POIJSON_reads_as_section_1_asks()
        {

            var json = POIJSON.Read("{ \"price\": 1.50, \"power\": 150000, \"signedAt\": \"2026-10-20T12:00:00.000Z\" }");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json["price"]!.Type,             Is.EqualTo(JTokenType.Float));
                Assert.That(json["power"]!.Type,             Is.EqualTo(JTokenType.Integer));
                Assert.That(json["signedAt"]!.Type,          Is.EqualTo(JTokenType.String));
                Assert.That(CanonicalJSON.Serialize(json),   Is.EqualTo("{\"power\":150000,\"price\":1.50,\"signedAt\":\"2026-10-20T12:00:00.000Z\"}"));
            }

        }

        #endregion

        #region POIJSON_refuses_what_section_1_refuses(Text, Error)

        /// <summary>
        /// POIJSON.Read refuses a property named twice, a second value after the first, what RFC 8259 does not call JSON,
        /// and a number a decimal cannot hold as written.
        /// </summary>
        [TestCase("{\"value\":1,\"value\":2}",    "value")]
        [TestCase("{\"value\":1} {\"value\":2}",  "RFC 8259")]
        [TestCase("{\"value\":1,}",               "RFC 8259")]
        [TestCase("{\"value\":1e-30}",            "1e-30")]
        public void POIJSON_refuses_what_section_1_refuses(String Text, String Error)
        {

            Assert.That(() => POIJSON.Read(Text), Throws.InstanceOf<JsonReaderException>().With.Message.Contains(Error));

        }

        #endregion

        #region POIJSON_reads_a_first_value_and_leaves_what_follows_to_its_caller()

        /// <summary>
        /// POIJSON.ReadFrom reads the first value of a reader from POIJSON.Reader as Read reads it, and leaves what
        /// follows it to its caller, e.g. a WebSocket message that is one JSON array.
        /// </summary>
        [Test]
        public void POIJSON_reads_a_first_value_and_leaves_what_follows_to_its_caller()
        {

            const String Alone      = "[1.50] ";
            const String Text       = "[1.50] [2]";
            const String Duplicate  = "{\"value\":1,\"value\":2} [2]";

            using var alone      = POIJSON.Reader(Alone);
            var firstAlone       = POIJSON.ReadFrom(alone, Alone);

            using var reader     = POIJSON.Reader(Text);
            var first            = POIJSON.ReadFrom(reader, Text);

            using var duplicate  = POIJSON.Reader(Duplicate);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CanonicalJSON.Serialize(firstAlone),                   Is.EqualTo("[1.50]"));
                Assert.That(alone.Read(),                                          Is.False, "there is no more");
                Assert.That(CanonicalJSON.Serialize(first),                        Is.EqualTo("[1.50]"));
                Assert.That(() => reader.Read(),                                   Throws.InstanceOf<JsonReaderException>(), "there is more");
                Assert.That(() => POIJSON.ReadFrom(duplicate, Duplicate),          Throws.InstanceOf<JsonReaderException>().With.Message.Contains("value"));
            }

        }

        #endregion


        #region A_roaming_network_is_read_from_its_JSON_text()

        /// <summary>
        /// A roaming network is read from the JSON text its ToJSON() writes, indented or canonical, with the ETags it was
        /// written with: its decimals as written, 150000 W and 22.0 kW, and a coordinate that is no short decimal.
        /// </summary>
        [Test]
        public void A_roaming_network_is_read_from_its_JSON_text()
        {

            var network  = tests.History.NumberRoundTripTests.NewNetwork();
            var eTags    = network.CalcETags();
            var json     = network.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json.ToString(),                out var fromText,       out var textError),       Is.True, textError);
            Assert.That(RoamingNetwork.TryParse(CanonicalJSON.Serialize(json),  out var fromCanonical,  out var canonicalError),  Is.True, canonicalError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(json["eTags"],                                              Is.Not.Null, "the ETags are checked as it is read");
                Assert.That(fromText!.CalcETags(),                                      Is.EqualTo(eTags));
                Assert.That(fromCanonical!.CalcETags(),                                 Is.EqualTo(eTags));
                Assert.That(CanonicalJSON.Serialize(fromText.ToJSON()),                 Is.EqualTo(CanonicalJSON.Serialize(json)));
                Assert.That(RoamingNetwork.Parse(json.ToString()).CalcETags(),          Is.EqualTo(eTags));
                Assert.That(() => RoamingNetwork.Parse(json.ToString() + " {}"),        Throws.ArgumentException.With.Message.Contains("RFC 8259"));
            }

        }

        #endregion

        #region A_roaming_network_is_refused_where_its_text_is_not_read_as_section_1_asks(CustomData, Behind, Error)

        /// <summary>
        /// The JSON text of a roaming network with a property named twice, a second value after the first, or a number a
        /// decimal cannot hold as written, is refused for its text; with neither, the same text is read.
        /// </summary>
        [TestCase("{\"value\":1,\"value\":2}",  "",                "value")]
        [TestCase("{\"value\":1}",              " {\"value\":2}",  "RFC 8259")]
        [TestCase("{\"value\":1e-30}",          "",                "1e-30")]
        public void A_roaming_network_is_refused_where_its_text_is_not_read_as_section_1_asks(String CustomData, String Behind, String Error)
        {

            String Text(String Data)

                => "{\"customData\":" + Data + "," + tests.History.NumberRoundTripTests.NewNetwork().ToJSON(IncludeETags: false).ToString(Formatting.None)[1..];

            Assert.That(RoamingNetwork.TryParse(Text("{\"value\":1}"), out var network, out var error),  Is.True, error);
            Assert.That(CanonicalJSON.Serialize(network!.ToJSON()["customData"]!),                       Is.EqualTo("{\"value\":1}"));

            Assert.That(RoamingNetwork.TryParse(Text(CustomData) + Behind, out network, out error),     Is.False);
            Assert.That(error,                                                                           Does.Contain(Error));

        }

        #endregion

        #region What_a_client_reads_from_text_is_refused_where_it_is_not_read_as_section_1_asks(TryParse, Text, Error)

        /// <summary>
        /// Every TryParse(Text, …) of what a client reads from text: before it is parsed, a property named twice, a second
        /// value after the first, and a number a decimal cannot hold as written are refused.
        /// </summary>
        private static IEnumerable<TestCaseData> TextParsers()
        {

            var parsers = new (String, Func<String, String?>)[] {
                ("RoamingNetwork",                      text => RoamingNetwork.                     TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkChangeSet",             text => RoamingNetworkChangeSet.            TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkChangeSetWithdrawal",   text => RoamingNetworkChangeSetWithdrawal.  TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkCommit",                text => RoamingNetworkCommit.               TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkCheckpoint",            text => RoamingNetworkCheckpoint.           TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkCheckpointProof",       text => RoamingNetworkCheckpointProof.      TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkAttestation",           text => RoamingNetworkAttestation.          TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkProof",                 text => RoamingNetworkProof.                TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkPriceStatement",        text => RoamingNetworkPriceStatement.       TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkPriceStatementProof",   text => RoamingNetworkPriceStatementProof.  TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkKeyStatement",          text => RoamingNetworkKeyStatement.         TryParse(text, out _, out var error) ? null : error),
                ("RoamingNetworkSigningKeyCertificate", text => RoamingNetworkSigningKeyCertificate.TryParse(text, out _, out var error) ? null : error)
            };

            var texts = new (String, String, String)[] {
                ("PropertyNamedTwice",  "{\"twice\":1,\"twice\":2}",  "twice"),
                ("SecondValue",         "{} {}",                      "RFC 8259"),
                ("NoDecimal",           "{\"value\":1e-30}",          "1e-30")
            };

            foreach (var (name, tryParse) in parsers)
                foreach (var (kind, text, error) in texts)
                    yield return new TestCaseData(tryParse, text, error).
                                     SetName($"What_a_client_reads_from_text_is_refused_where_it_is_not_read_as_section_1_asks({name},{kind})");

        }

        [TestCaseSource(nameof(TextParsers))]
        public void What_a_client_reads_from_text_is_refused_where_it_is_not_read_as_section_1_asks(Func<String, String?>  TryParse,
                                                                                                     String                 Text,
                                                                                                     String                 Error)
        {

            Assert.That(TryParse(Text), Does.Contain(Error));

        }

        #endregion

    }

}
