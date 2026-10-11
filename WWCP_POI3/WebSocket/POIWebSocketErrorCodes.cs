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
    /// The error codes of request errors on a POI WebSocket: the ones of OCPP-J, and NotFound.
    /// </summary>
    public static class POIWebSocketErrorCodes
    {

        /// <summary>
        /// The payload of the action is syntactically or semantically incorrect.
        /// </summary>
        public const String FormatViolation                = "FormatViolation";

        /// <summary>
        /// Any other error.
        /// </summary>
        public const String GenericError                   = "GenericError";

        /// <summary>
        /// The receiver failed to process the action, for an internal reason.
        /// </summary>
        public const String InternalError                  = "InternalError";

        /// <summary>
        /// The receiver does not support messages of this type.
        /// </summary>
        public const String MessageTypeNotSupported        = "MessageTypeNotSupported";

        /// <summary>
        /// The receiver does not know the action.
        /// </summary>
        public const String NotImplemented                 = "NotImplemented";

        /// <summary>
        /// The receiver knows the action, but does not support it.
        /// </summary>
        public const String NotSupported                   = "NotSupported";

        /// <summary>
        /// The payload is syntactically correct, but at least one field violates occurrence constraints.
        /// </summary>
        public const String OccurrenceConstraintViolation  = "OccurrenceConstraintViolation";

        /// <summary>
        /// The payload is syntactically correct, but at least one field contains an invalid value.
        /// </summary>
        public const String PropertyConstraintViolation    = "PropertyConstraintViolation";

        /// <summary>
        /// The payload of the action is incomplete.
        /// </summary>
        public const String ProtocolError                  = "ProtocolError";

        /// <summary>
        /// The message is no valid frame, e.g. its message identification could not be read.
        /// </summary>
        public const String RpcFrameworkError              = "RpcFrameworkError";

        /// <summary>
        /// The sender may not do this.
        /// </summary>
        public const String SecurityError                  = "SecurityError";

        /// <summary>
        /// The payload is syntactically correct, but at least one field has a wrong data type.
        /// </summary>
        public const String TypeConstraintViolation        = "TypeConstraintViolation";

        /// <summary>
        /// The requested commit or roaming network is unknown here.
        /// </summary>
        public const String NotFound                       = "NotFound";

    }

}
