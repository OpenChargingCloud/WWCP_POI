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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The types of the messages on a POI WebSocket, with the numbers of OCPP-J.
    /// </summary>
    public enum POIWebSocketMessageType : Byte
    {

        /// <summary>
        /// A request: [2, messageId, action, payload]
        /// </summary>
        Request        = 2,

        /// <summary>
        /// The response to a request: [3, messageId, payload]
        /// </summary>
        Response       = 3,

        /// <summary>
        /// The error answering a request: [4, messageId, errorCode, errorDescription, errorDetails]
        /// </summary>
        RequestError   = 4,

        /// <summary>
        /// The error answering a response: [5, messageId, errorCode, errorDescription, errorDetails]
        /// </summary>
        ResponseError  = 5,

        /// <summary>
        /// A message without any answer, e.g. a push of a subscription: [6, messageId, action, payload]
        /// </summary>
        Send           = 6

    }

}
