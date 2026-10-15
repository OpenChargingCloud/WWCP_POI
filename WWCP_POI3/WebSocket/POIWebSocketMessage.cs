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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A message on a POI WebSocket, framed like OCPP-J:
    /// in a text frame as a JSON array, in a binary frame as a CBOR array with the same items.
    /// </summary>
    /// <remarks>
    /// [2, messageId, action, payload]                             request
    /// [3, messageId, payload]                                     response
    /// [4, messageId, errorCode, errorDescription, errorDetails]   request error
    /// [5, messageId, errorCode, errorDescription, errorDetails]   response error
    /// [6, messageId, action, payload]                             send, without any answer
    /// A payload and the error details are objects. A message identification has at most 36 characters;
    /// an answer has the identification of the request. An error about a message whose identification
    /// could not be read has the identification "-1".
    /// </remarks>
    public sealed class POIWebSocketMessage
    {

        #region Data

        /// <summary>
        /// The maximum length of a message identification.
        /// </summary>
        public const           Int32   MaxMessageIdLength  = 36;

        /// <summary>
        /// The message identification of an error about a message whose identification could not be read.
        /// </summary>
        public const           String  UnknownMessageId    = "-1";

        /// <summary>
        /// The default maximum depth of a message, including the array and the payload.
        /// </summary>
        public const           Int32   DefaultMaxDepth     = 64;

        #endregion

        #region Properties

        /// <summary>
        /// The type of the message.
        /// </summary>
        public POIWebSocketMessageType  MessageType         { get; }

        /// <summary>
        /// The identification of the message, or of the request it answers.
        /// </summary>
        public String                   MessageId           { get; }

        /// <summary>
        /// The action of a request or of a send.
        /// </summary>
        public String?                  Action              { get; }

        /// <summary>
        /// The JSON payload of a request, of a response or of a send.
        /// Null when only a CBOR payload was given; see GetPayloadJSON().
        /// </summary>
        public JObject?                 Payload             { get; }

        /// <summary>
        /// The CBOR payload of a request, of a response or of a send,
        /// when it was read from a binary frame or given for one.
        /// </summary>
        public CBORValue?               PayloadCBOR         { get; }

        /// <summary>
        /// The error code of an error.
        /// </summary>
        public String?                  ErrorCode           { get; }

        /// <summary>
        /// The error description of an error.
        /// </summary>
        public String?                  ErrorDescription    { get; }

        /// <summary>
        /// The error details of an error.
        /// </summary>
        public JObject?                 ErrorDetails        { get; }


        /// <summary>
        /// Whether the message is a request error or a response error.
        /// </summary>
        public Boolean                  IsError

            => MessageType is POIWebSocketMessageType.RequestError
                           or POIWebSocketMessageType.ResponseError;

        #endregion

        #region Constructor(s)

        private POIWebSocketMessage(POIWebSocketMessageType  MessageType,
                                    String                   MessageId,
                                    String?                  Action,
                                    JObject?                 Payload,
                                    CBORValue?               PayloadCBOR,
                                    String?                  ErrorCode,
                                    String?                  ErrorDescription,
                                    JObject?                 ErrorDetails)
        {

            if (MessageId.IsNullOrEmpty() || MessageId.Length > MaxMessageIdLength)
                throw new ArgumentException($"A message identification must have 1 to {MaxMessageIdLength} characters!", nameof(MessageId));

            this.MessageType       = MessageType;
            this.MessageId         = MessageId;
            this.Action            = Action;
            this.Payload           = Payload;
            this.PayloadCBOR       = PayloadCBOR;
            this.ErrorCode         = ErrorCode;
            this.ErrorDescription  = ErrorDescription;
            this.ErrorDetails      = ErrorDetails;

        }

        #endregion


        #region (static) Request      (MessageId, Action, Payload = null, PayloadCBOR = null)

        /// <summary>
        /// A request.
        /// </summary>
        /// <param name="MessageId">The identification of the request.</param>
        /// <param name="Action">The action.</param>
        /// <param name="Payload">The optional payload, an empty object otherwise.</param>
        /// <param name="PayloadCBOR">An optional CBOR payload for a binary frame, a map.</param>
        public static POIWebSocketMessage Request(String      MessageId,
                                                  String      Action,
                                                  JObject?    Payload       = null,
                                                  CBORValue?  PayloadCBOR   = null)
        {

            if (Action.IsNullOrEmpty())
                throw new ArgumentException("An action must not be empty!", nameof(Action));

            if (PayloadCBOR.HasValue && PayloadCBOR.Value.Kind != CBORValueKind.Map)
                throw new ArgumentException("A CBOR payload must be a map!", nameof(PayloadCBOR));

            return new (POIWebSocketMessageType.Request,
                        MessageId,
                        Action,
                        Payload ?? (PayloadCBOR.HasValue ? null : []),
                        PayloadCBOR,
                        null,
                        null,
                        null);

        }

        #endregion

        #region (static) Response     (MessageId, Payload, PayloadCBOR = null)

        /// <summary>
        /// The response to a request.
        /// </summary>
        /// <param name="MessageId">The identification of the request.</param>
        /// <param name="Payload">The payload, or null when a CBOR payload is given.</param>
        /// <param name="PayloadCBOR">An optional CBOR payload for a binary frame, a map.</param>
        public static POIWebSocketMessage Response(String      MessageId,
                                                   JObject?    Payload,
                                                   CBORValue?  PayloadCBOR   = null)
        {

            if (Payload is null && PayloadCBOR is null)
                throw new ArgumentException("A response needs a payload!", nameof(Payload));

            if (PayloadCBOR.HasValue && PayloadCBOR.Value.Kind != CBORValueKind.Map)
                throw new ArgumentException("A CBOR payload must be a map!", nameof(PayloadCBOR));

            return new (POIWebSocketMessageType.Response,
                        MessageId,
                        null,
                        Payload,
                        PayloadCBOR,
                        null,
                        null,
                        null);

        }

        #endregion

        #region (static) RequestError (MessageId, ErrorCode, ErrorDescription = null, ErrorDetails = null)

        /// <summary>
        /// The error answering a request.
        /// </summary>
        /// <param name="MessageId">The identification of the request, or "-1" when it could not be read.</param>
        /// <param name="ErrorCode">The error code, see POIWebSocketErrorCodes.</param>
        /// <param name="ErrorDescription">An optional error description.</param>
        /// <param name="ErrorDetails">Optional error details.</param>
        public static POIWebSocketMessage RequestError(String    MessageId,
                                                       String    ErrorCode,
                                                       String?   ErrorDescription   = null,
                                                       JObject?  ErrorDetails       = null)

            => Error(POIWebSocketMessageType.RequestError,
                     MessageId,
                     ErrorCode,
                     ErrorDescription,
                     ErrorDetails);

        #endregion

        #region (static) ResponseError(MessageId, ErrorCode, ErrorDescription = null, ErrorDetails = null)

        /// <summary>
        /// The error answering a response.
        /// </summary>
        /// <param name="MessageId">The identification of the response.</param>
        /// <param name="ErrorCode">The error code, see POIWebSocketErrorCodes.</param>
        /// <param name="ErrorDescription">An optional error description.</param>
        /// <param name="ErrorDetails">Optional error details.</param>
        public static POIWebSocketMessage ResponseError(String    MessageId,
                                                        String    ErrorCode,
                                                        String?   ErrorDescription   = null,
                                                        JObject?  ErrorDetails       = null)

            => Error(POIWebSocketMessageType.ResponseError,
                     MessageId,
                     ErrorCode,
                     ErrorDescription,
                     ErrorDetails);

        #endregion

        #region (static) Send         (MessageId, Action, Payload, PayloadCBOR = null)

        /// <summary>
        /// A message without any answer, e.g. a push of a subscription.
        /// </summary>
        /// <param name="MessageId">The identification of the message.</param>
        /// <param name="Action">The action.</param>
        /// <param name="Payload">The payload, or null when a CBOR payload is given.</param>
        /// <param name="PayloadCBOR">An optional CBOR payload for a binary frame, a map.</param>
        public static POIWebSocketMessage Send(String      MessageId,
                                               String      Action,
                                               JObject?    Payload,
                                               CBORValue?  PayloadCBOR   = null)
        {

            if (Action.IsNullOrEmpty())
                throw new ArgumentException("An action must not be empty!", nameof(Action));

            if (Payload is null && PayloadCBOR is null)
                throw new ArgumentException("A send needs a payload!", nameof(Payload));

            if (PayloadCBOR.HasValue && PayloadCBOR.Value.Kind != CBORValueKind.Map)
                throw new ArgumentException("A CBOR payload must be a map!", nameof(PayloadCBOR));

            return new (POIWebSocketMessageType.Send,
                        MessageId,
                        Action,
                        Payload,
                        PayloadCBOR,
                        null,
                        null,
                        null);

        }

        #endregion

        #region (private static) Error(MessageType, MessageId, ErrorCode, ErrorDescription, ErrorDetails)

        private static POIWebSocketMessage Error(POIWebSocketMessageType  MessageType,
                                                 String                   MessageId,
                                                 String                   ErrorCode,
                                                 String?                  ErrorDescription,
                                                 JObject?                 ErrorDetails)
        {

            if (ErrorCode.IsNullOrEmpty())
                throw new ArgumentException("An error code must not be empty!", nameof(ErrorCode));

            return new (MessageType,
                        MessageId,
                        null,
                        null,
                        null,
                        ErrorCode,
                        ErrorDescription ?? "",
                        ErrorDetails     ?? []);

        }

        #endregion


        #region GetPayloadJSON()

        /// <summary>
        /// The JSON payload, also when only a CBOR payload was given, or null for an error.
        /// </summary>
        public JObject? GetPayloadJSON()

            => Payload ?? (PayloadCBOR.HasValue
                               ? CBORJSON.ToJSON(PayloadCBOR.Value) as JObject
                               : null);

        #endregion

        #region GetPayloadCBOR()

        /// <summary>
        /// The CBOR payload, also when only a JSON payload was given, or null for an error.
        /// </summary>
        public CBORValue? GetPayloadCBOR()

            => PayloadCBOR ?? (Payload is not null
                                   ? CBORJSON.ToCBOR(Payload)
                                   : (CBORValue?) null);

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON array of this message.
        /// </summary>
        public JArray ToJSON()

            => MessageType switch {

                   POIWebSocketMessageType.Request or
                   POIWebSocketMessageType.Send      => new JArray((Byte) MessageType, MessageId, Action!, GetPayloadJSON() ?? []),

                   POIWebSocketMessageType.Response  => new JArray((Byte) MessageType, MessageId,         GetPayloadJSON() ?? []),

                   _                                 => new JArray((Byte) MessageType, MessageId, ErrorCode!, ErrorDescription ?? "", ErrorDetails ?? [])

               };

        #endregion

        #region ToJSONText()

        /// <summary>
        /// Return the JSON text of this message, for a text frame.
        /// </summary>
        public String ToJSONText()

            => POIJSON.ToText(ToJSON());

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR array of this message: the items of the JSON array.
        /// </summary>
        public CBORValue ToCBOR()
        {

            var type       = CBORValue.FromUInt64((Byte) MessageType);
            var messageId  = CBORValue.FromText(MessageId);

            return MessageType switch {

                       POIWebSocketMessageType.Request or
                       POIWebSocketMessageType.Send      => CBORValue.FromArray(type, messageId, CBORValue.FromText(Action!), GetPayloadCBOR() ?? CBORValue.FromMap([])),

                       POIWebSocketMessageType.Response  => CBORValue.FromArray(type, messageId,                             GetPayloadCBOR() ?? CBORValue.FromMap([])),

                       _                                 => CBORValue.FromArray(type, messageId, CBORValue.FromText(ErrorCode!),
                                                                                                 CBORValue.FromText(ErrorDescription ?? ""),
                                                                                                 CBORJSON.ToCBOR(ErrorDetails ?? []))

                   };

        }

        #endregion

        #region ToCBORBytes()

        /// <summary>
        /// Return the CBOR bytes of this message, for a binary frame.
        /// </summary>
        public Byte[] ToCBORBytes()

            => ToCBOR().ToByteArray(CBORWriterOptions.Canonical);

        #endregion


        #region (static) TryParse(Text, out Message, out ErrorMessage, MaxDepth = DefaultMaxDepth)

        /// <summary>
        /// Try to parse the JSON text of a text frame.
        /// </summary>
        /// <param name="Text">The JSON text.</param>
        /// <param name="Message">The parsed message.</param>
        /// <param name="ErrorMessage">The request error to answer with, when the text is no valid message.</param>
        /// <param name="MaxDepth">The maximum depth of the message.</param>
        public static Boolean TryParse(String                                         Text,
                                       [NotNullWhen(true)]  out POIWebSocketMessage?  Message,
                                       [NotNullWhen(false)] out POIWebSocketMessage?  ErrorMessage,
                                       Int32                                          MaxDepth   = DefaultMaxDepth)
        {

            Message = null;

            JToken json;

            try
            {

                using var reader = new JsonTextReader(new StringReader(Text)) {
                                       DateParseHandling   = DateParseHandling.None,
                                       FloatParseHandling  = FloatParseHandling.Decimal,
                                       MaxDepth            = MaxDepth
                                   };

                json = JToken.ReadFrom(reader);

                if (reader.Read())
                {
                    ErrorMessage = RequestError(UnknownMessageId, POIWebSocketErrorCodes.RpcFrameworkError, "The message must be one JSON array!");
                    return false;
                }

            }
            catch (Exception e)
            {
                ErrorMessage = RequestError(UnknownMessageId, POIWebSocketErrorCodes.RpcFrameworkError, "Invalid JSON: " + e.Message);
                return false;
            }

            if (json is not JArray array)
            {
                ErrorMessage = RequestError(UnknownMessageId, POIWebSocketErrorCodes.RpcFrameworkError, "The message must be a JSON array!");
                return false;
            }

            return TryParse(array.Select(item => new Item(item)).ToArray(),
                            out Message,
                            out ErrorMessage);

        }

        #endregion

        #region (static) TryParse(CBOR, out Message, out ErrorMessage, MaxDepth = DefaultMaxDepth)

        /// <summary>
        /// Try to parse the CBOR bytes of a binary frame.
        /// </summary>
        /// <param name="CBOR">The CBOR bytes.</param>
        /// <param name="Message">The parsed message.</param>
        /// <param name="ErrorMessage">The request error to answer with, when the bytes are no valid message.</param>
        /// <param name="MaxDepth">The maximum depth of the message.</param>
        public static Boolean TryParse(ReadOnlySpan<Byte>                             CBOR,
                                       [NotNullWhen(true)]  out POIWebSocketMessage?  Message,
                                       [NotNullWhen(false)] out POIWebSocketMessage?  ErrorMessage,
                                       Int32                                          MaxDepth   = DefaultMaxDepth)
        {

            Message = null;

            if (!CBORValue.TryParse(CBOR,
                                    out var cbor,
                                    out var cborError,
                                    new CBORReaderOptions { MaxDepth = MaxDepth }))
            {
                ErrorMessage = RequestError(UnknownMessageId, POIWebSocketErrorCodes.RpcFrameworkError, "Invalid CBOR: " + cborError);
                return false;
            }

            if (cbor.Kind != CBORValueKind.Array)
            {
                ErrorMessage = RequestError(UnknownMessageId, POIWebSocketErrorCodes.RpcFrameworkError, "The message must be a CBOR array!");
                return false;
            }

            return TryParse(cbor.AsArray().Select(item => new Item(item)).ToArray(),
                            out Message,
                            out ErrorMessage);

        }

        #endregion

        #region (private static) TryParse(Items, out Message, out ErrorMessage)

        /// <summary>
        /// An item of a JSON array or of a CBOR array.
        /// </summary>
        private readonly struct Item
        {

            private readonly JToken?     json;
            private readonly CBORValue?  cbor;

            public Item(JToken     JSON) { json = JSON; }
            public Item(CBORValue  CBOR) { cbor = CBOR; }

            public Boolean TryGetText([NotNullWhen(true)] out String? Text)
            {

                if (json is not null)
                {
                    Text = json.Type == JTokenType.String ? json.Value<String>() : null;
                    return Text is not null;
                }

                Text = null;
                return cbor!.Value.TryGetText(out Text);

            }

            public Boolean TryGetMessageType(out UInt64 MessageType)
            {

                if (json is not null)
                {

                    MessageType = 0;

                    if (json.Type != JTokenType.Integer)
                        return false;

                    try
                    {
                        MessageType = json.Value<UInt64>();
                        return true;
                    }
                    catch
                    {
                        return false;
                    }

                }

                return cbor!.Value.TryGetUInt64(out MessageType);

            }

            public Boolean TryGetObject([NotNullWhen(true)] out JObject? JSON, out CBORValue? CBOR)
            {

                if (json is not null)
                {
                    JSON = json as JObject;
                    CBOR = null;
                    return JSON is not null;
                }

                JSON = null;
                CBOR = null;

                if (cbor!.Value.Kind != CBORValueKind.Map)
                    return false;

                JSON = CBORJSON.ToJSON(cbor.Value) as JObject;
                CBOR = cbor;

                return JSON is not null;

            }

        }

        private static Boolean TryParse(Item[]                                         Items,
                                        [NotNullWhen(true)]  out POIWebSocketMessage?  Message,
                                        [NotNullWhen(false)] out POIWebSocketMessage?  ErrorMessage)
        {

            Message       = null;
            ErrorMessage  = null;

            try
            {

                #region The message identification

                if (Items.Length < 2                         ||
                    !Items[1].TryGetText(out var messageId)  ||
                    messageId.Length == 0                    ||
                    messageId.Length > MaxMessageIdLength)
                {
                    ErrorMessage = RequestError(UnknownMessageId, POIWebSocketErrorCodes.RpcFrameworkError, $"The message identification must be a text of 1 to {MaxMessageIdLength} characters!");
                    return false;
                }

                #endregion

                #region The message type

                if (!Items[0].TryGetMessageType(out var messageTypeNumber))
                {
                    ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.RpcFrameworkError, "The message type must be a number!");
                    return false;
                }

                if (messageTypeNumber is < 2 or > 6)
                {
                    ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.MessageTypeNotSupported, $"The message type {messageTypeNumber} is not supported!");
                    return false;
                }

                var messageType   = (POIWebSocketMessageType) messageTypeNumber;

                var expectedItems = messageType switch {
                                        POIWebSocketMessageType.Response       => 3,
                                        POIWebSocketMessageType.RequestError   => 5,
                                        POIWebSocketMessageType.ResponseError  => 5,
                                        _                                      => 4
                                    };

                if (Items.Length != expectedItems)
                {
                    ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.RpcFrameworkError, $"A message of type {messageTypeNumber} must have {expectedItems} items!");
                    return false;
                }

                #endregion

                switch (messageType)
                {

                    #region Request or send

                    case POIWebSocketMessageType.Request:
                    case POIWebSocketMessageType.Send:

                        if (!Items[2].TryGetText(out var action) || action.Length == 0)
                        {
                            ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.RpcFrameworkError, "The action must be a non-empty text!");
                            return false;
                        }

                        if (!Items[3].TryGetObject(out var payload, out var payloadCBOR))
                        {
                            ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.FormatViolation, "The payload must be an object!");
                            return false;
                        }

                        Message = new POIWebSocketMessage(messageType, messageId, action, payload, payloadCBOR, null, null, null);
                        return true;

                    #endregion

                    #region Response

                    case POIWebSocketMessageType.Response:

                        if (!Items[2].TryGetObject(out var responsePayload, out var responsePayloadCBOR))
                        {
                            ErrorMessage = ResponseError(messageId, POIWebSocketErrorCodes.FormatViolation, "The payload must be an object!");
                            return false;
                        }

                        Message = new POIWebSocketMessage(messageType, messageId, null, responsePayload, responsePayloadCBOR, null, null, null);
                        return true;

                    #endregion

                    #region Request error or response error

                    default:

                        if (!Items[2].TryGetText(out var errorCode) || errorCode.Length == 0)
                        {
                            ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.RpcFrameworkError, "The error code must be a non-empty text!");
                            return false;
                        }

                        if (!Items[3].TryGetText(out var errorDescription))
                        {
                            ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.RpcFrameworkError, "The error description must be a text!");
                            return false;
                        }

                        if (!Items[4].TryGetObject(out var errorDetails, out _))
                        {
                            ErrorMessage = RequestError(messageId, POIWebSocketErrorCodes.RpcFrameworkError, "The error details must be an object!");
                            return false;
                        }

                        Message = new POIWebSocketMessage(messageType, messageId, null, null, null, errorCode, errorDescription, errorDetails);
                        return true;

                    #endregion

                }

            }
            catch (Exception e)
            {
                ErrorMessage = RequestError(UnknownMessageId, POIWebSocketErrorCodes.RpcFrameworkError, e.Message);
                return false;
            }

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => IsError
                   ? $"{MessageType} '{MessageId}': {ErrorCode} {ErrorDescription}"
                   : $"{MessageType} '{MessageId}'{(Action is not null ? $" {Action}" : "")}";

        #endregion

    }

}
