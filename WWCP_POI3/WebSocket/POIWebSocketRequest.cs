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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A command handler of a POI WebSocket server: the response or the request error for a request.
    /// </summary>
    /// <param name="Request">The request.</param>
    public delegate Task<POIWebSocketMessage> POIWebSocketCommandDelegate(POIWebSocketRequest Request);


    /// <summary>
    /// A request on a POI WebSocket, as given to its command handler.
    /// </summary>
    public sealed class POIWebSocketRequest
    {

        #region Properties

        /// <summary>
        /// The session of the connection the request came in on.
        /// </summary>
        public POIWebSocketSession  Session              { get; }

        /// <summary>
        /// The request message.
        /// </summary>
        public POIWebSocketMessage  Message              { get; }

        /// <summary>
        /// The identification of the request.
        /// </summary>
        public String               MessageId
            => Message.MessageId;

        /// <summary>
        /// The action of the request.
        /// </summary>
        public String               Action
            => Message.Action!;

        /// <summary>
        /// The JSON payload of the request.
        /// </summary>
        public JObject              Payload              { get; }

        /// <summary>
        /// Whether the request came in a binary frame, as CBOR; it is answered the same way.
        /// </summary>
        public Boolean              IsBinary             { get; }

        /// <summary>
        /// Cancelled when the connection is closed.
        /// </summary>
        public CancellationToken    CancellationToken    { get; }

        #endregion

        #region Constructor(s)

        internal POIWebSocketRequest(POIWebSocketSession  Session,
                                     POIWebSocketMessage  Message,
                                     Boolean              IsBinary,
                                     CancellationToken    CancellationToken)
        {

            this.Session            = Session;
            this.Message            = Message;
            this.Payload            = Message.GetPayloadJSON() ?? [];
            this.IsBinary           = IsBinary;
            this.CancellationToken  = CancellationToken;

        }

        #endregion


        #region Respond(JSON)

        /// <summary>
        /// The response with the given payload.
        /// </summary>
        /// <param name="JSON">The payload.</param>
        public POIWebSocketMessage Respond(JObject JSON)

            => POIWebSocketMessage.Response(MessageId, JSON);

        #endregion

        #region Respond(JSON, CBOR)

        /// <summary>
        /// The response with the given payload, made in the format of the request only.
        /// </summary>
        /// <param name="JSON">The JSON payload.</param>
        /// <param name="CBOR">The CBOR payload, a map.</param>
        public POIWebSocketMessage Respond(Func<JObject>    JSON,
                                           Func<CBORValue>  CBOR)

            => IsBinary
                   ? POIWebSocketMessage.Response(MessageId, null,   CBOR())
                   : POIWebSocketMessage.Response(MessageId, JSON());

        #endregion

        #region Error  (ErrorCode, ErrorDescription, ErrorDetails = null)

        /// <summary>
        /// The request error with the given code and description.
        /// </summary>
        /// <param name="ErrorCode">The error code, see POIWebSocketErrorCodes.</param>
        /// <param name="ErrorDescription">The error description.</param>
        /// <param name="ErrorDetails">Optional error details.</param>
        public POIWebSocketMessage Error(String    ErrorCode,
                                         String    ErrorDescription,
                                         JObject?  ErrorDetails   = null)

            => POIWebSocketMessage.RequestError(MessageId,
                                                ErrorCode,
                                                ErrorDescription,
                                                ErrorDetails);

        #endregion

    }

}
