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

namespace cloud.charging.open.protocols.WWCP.POI.tests.WebSockets
{

    /// <summary>
    /// The OCPP-J frames of a POI WebSocket, in JSON and in CBOR.
    /// </summary>
    [TestFixture]
    public class WebSocketMessageTests
    {

        #region Messages

        private static readonly POIWebSocketMessage[] messages = [
            POIWebSocketMessage.Request      ("r1", "commitInfos", new JObject(new JProperty("max", 5))),
            POIWebSocketMessage.Response     ("r1", new JObject(new JProperty("commitInfos", new JArray()))),
            POIWebSocketMessage.RequestError ("r2", POIWebSocketErrorCodes.NotFound, "The commit is unknown here!", new JObject(new JProperty("commitId", "abc"))),
            POIWebSocketMessage.ResponseError("r3", POIWebSocketErrorCodes.FormatViolation),
            POIWebSocketMessage.Send         ("s1", "commitAnnouncement", new JObject(new JProperty("revision", 42)))
        ];

        #endregion


        #region Messages_have_the_OCPP_J_layout()

        [Test]
        public void Messages_have_the_OCPP_J_layout()
        {

            using (Assert.EnterMultipleScope())
            {
                Assert.That(messages[0].ToJSONText(),  Is.EqualTo("""[2,"r1","commitInfos",{"max":5}]"""));
                Assert.That(messages[1].ToJSONText(),  Is.EqualTo("""[3,"r1",{"commitInfos":[]}]"""));
                Assert.That(messages[2].ToJSONText(),  Is.EqualTo("""[4,"r2","NotFound","The commit is unknown here!",{"commitId":"abc"}]"""));
                Assert.That(messages[3].ToJSONText(),  Is.EqualTo("""[5,"r3","FormatViolation","",{}]"""));
                Assert.That(messages[4].ToJSONText(),  Is.EqualTo("""[6,"s1","commitAnnouncement",{"revision":42}]"""));
            }

        }

        #endregion

        #region Messages_survive_JSON_and_CBOR()

        [Test]
        public void Messages_survive_JSON_and_CBOR()
        {

            foreach (var message in messages)
            {

                Assert.That(POIWebSocketMessage.TryParse(message.ToJSONText(),  out var fromJSON, out var errorJSON),  Is.True, errorJSON?.ErrorDescription);
                Assert.That(POIWebSocketMessage.TryParse(message.ToCBORBytes(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR?.ErrorDescription);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(fromJSON!.ToJSONText(),    Is.EqualTo(message.ToJSONText()));
                    Assert.That(fromCBOR!.ToJSONText(),    Is.EqualTo(message.ToJSONText()));
                    Assert.That(fromCBOR.MessageType,      Is.EqualTo(message.MessageType));
                    Assert.That(fromCBOR.PayloadCBOR.HasValue,  Is.EqualTo(message.Payload is not null));
                }

            }

        }

        #endregion

        #region A_CBOR_payload_is_kept()

        [Test]
        public void A_CBOR_payload_is_kept()
        {

            var payload   = CBORValue.FromMap([ new (CBORValue.FromText("bytes"), CBORValue.FromBytes([ 1, 2, 3 ])) ]);
            var response  = POIWebSocketMessage.Response("r1", null, payload);

            Assert.That(POIWebSocketMessage.TryParse(response.ToCBORBytes(), out var parsed, out var error),  Is.True, error?.ErrorDescription);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed!.PayloadCBOR!.Value.ToByteArray(CBORWriterOptions.Canonical),  Is.EqualTo(payload.ToByteArray(CBORWriterOptions.Canonical)));
                Assert.That(response.Payload,                                                    Is.Null);
                Assert.That(response.GetPayloadJSON(),                                           Is.Not.Null);
            }

        }

        #endregion

        #region Invalid_frames_are_answered_with_request_errors()

        [TestCase("no JSON",                            "-1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""{"not":"an array"}""",             "-1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[2]""",                            "-1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[2,42,"head",{}]""",               "-1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[2,"",  "head",{}]""",             "-1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[2,"0123456789012345678901234567890123456","head",{}]""", "-1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[7,"a1","head",{}]""",             "a1", POIWebSocketErrorCodes.MessageTypeNotSupported)]
        [TestCase("""["2","a1","head",{}]""",           "a1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[2,"a1","head"]""",                "a1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[2,"a1","",{}]""",                 "a1", POIWebSocketErrorCodes.RpcFrameworkError)]
        [TestCase("""[2,"a1","head",[]]""",             "a1", POIWebSocketErrorCodes.FormatViolation)]
        [TestCase("""[2,"a1","head",{}] [3]""",         "-1", POIWebSocketErrorCodes.RpcFrameworkError)]
        public void Invalid_frames_are_answered_with_request_errors(String Text, String MessageId, String ErrorCode)
        {

            Assert.That(POIWebSocketMessage.TryParse(Text, out _, out var error),  Is.False);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(error!.MessageType, Is.EqualTo(POIWebSocketMessageType.RequestError));
                Assert.That(error.MessageId,    Is.EqualTo(MessageId));
                Assert.That(error.ErrorCode,    Is.EqualTo(ErrorCode));
            }

        }

        #endregion

        #region Too_deep_messages_are_refused()

        [Test]
        public void Too_deep_messages_are_refused()
        {

            var deep = """[2,"a1","head",{"a":{"b":{"c":{}}}}]""";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(POIWebSocketMessage.TryParse(deep, out _, out _, MaxDepth: 3),                               Is.False);
                Assert.That(POIWebSocketMessage.TryParse(deep, out _, out _, MaxDepth: 5),                               Is.True);
                Assert.That(POIWebSocketMessage.TryParse(CBORJSON.ToCBOR(JArray.Parse(deep)).ToByteArray(), out _, out _, MaxDepth: 3),  Is.False);
            }

        }

        #endregion

    }

}
