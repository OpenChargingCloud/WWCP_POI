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
using System.Text;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Co-signing over HTTP and the WebSocket (SPECIFICATION.md, section 4.2): where a change set waits for the signatures
    /// of the holders of a quorum still missing, each holder signing it on its own; once its owners signed it, it is
    /// committed. Submitting is decided by AuthorizeChangeSetSubmission, as for COMMIT ~/history/changeSets.
    /// </summary>
    /// <remarks>
    /// GET    ~/RNs/{RN_Id}/history/changeSets/pending                             the change sets waiting, { pendingChangeSets: [ { status, changeSet, missing } ] }
    /// POST   ~/RNs/{RN_Id}/history/changeSets/pending                             a change set signed by an owner of one of its changes, to wait or to be committed
    /// GET    ~/RNs/{RN_Id}/history/changeSets/pending/{changeSetId}                one of them, { status, changeSet, missing }
    /// POST   ~/RNs/{RN_Id}/history/changeSets/pending/{changeSetId}/signatures     more signatures of it: { signatures: [ … ] }
    /// DELETE ~/RNs/{RN_Id}/history/changeSets/pending/{changeSetId}                withdraw it: the withdrawal, signed by an owner of one of its changes
    ///
    /// Answered with { status: pending | committed | alreadyCommitted | withdrawn | refused | notFound, changeSet?, missing?, commitId?, description? }:
    /// 202 Accepted while it waits, 201 Created once committed, 200 OK when committed before or withdrawn, 404 Not Found, 422 Unprocessable Entity.
    ///
    /// WebSocket actions: pendingChangeSets, pendingChangeSet { changeSetId }, coSignChangeSet { changeSet },
    /// addPendingSignatures { changeSetId, signatures }, withdrawChangeSet { withdrawal };
    /// topic: pendingChangeSets (action pendingChangeSet), every change set that starts or keeps waiting, or waits no longer.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The topic of the change sets waiting for signatures.
        /// </summary>
        public const String PendingChangeSetsTopic = "pendingChangeSets";

        #endregion


        #region (private) RegisterCoSigningURLTemplates()

        private void RegisterCoSigningURLTemplates()
        {

            var pending = HTTPPath.Root + "RNs/{roamingNetworkId}/history/changeSets/pending";

            #region GET    ~/RNs/{roamingNetworkId}/history/changeSets/pending

            AddHandler(pending,
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           TryGetHistory(request, out var history, out var errorResponse)
                               ? JSONResponse(request, HTTPStatusCode.OK, PendingChangeSetsJSON(history))
                               : errorResponse

                       ));

            #endregion

            #region POST   ~/RNs/{roamingNetworkId}/history/changeSets/pending

            AddHandler(pending,
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  Changes(SubmitPendingChangeSet));

            #endregion

            #region GET    ~/RNs/{roamingNetworkId}/history/changeSets/pending/{changeSetId}

            AddHandler(pending + "/{changeSetId}",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetPendingChangeSetId(request, out var history, out var changeSetId, out var errorResponse)
                               ? errorResponse

                               : history.TryGetPendingChangeSet(changeSetId, out var result)
                                     ? JSONResponse (request, HTTPStatusCode.OK,       result.ToJSON())
                                     : ErrorResponse(request, HTTPStatusCode.NotFound, $"No change set '{changeSetId}' waits for signatures here!")

                       ));

            #endregion

            #region POST   ~/RNs/{roamingNetworkId}/history/changeSets/pending/{changeSetId}/signatures

            AddHandler(pending + "/{changeSetId}/signatures",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  Changes(SubmitPendingSignatures));

            #endregion

            #region DELETE ~/RNs/{roamingNetworkId}/history/changeSets/pending/{changeSetId}

            AddHandler(pending + "/{changeSetId}",
                       HTTPMethod:    HTTPMethod.DELETE,
                       HTTPDelegate:  Changes(WithdrawPendingChangeSet));

            #endregion

        }

        #endregion


        #region (private) SubmitPendingChangeSet(Request)

        private HTTPResponse SubmitPendingChangeSet(HTTPRequest Request)
        {

            if (!TryGetHistory(Request, out var history, out var errorResponse))
                return errorResponse;

            if (AuthorizeChangeSetSubmission is null)
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "Submitting change sets is not enabled here!");

            var body = Request.HTTPBody;

            if (body is null || body.Length == 0)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain a change set!");

            RoamingNetworkChangeSet? changeSet;
            String?                  changeSetError;

            if (HasCBORBody(Request)
                    ? !RoamingNetworkChangeSet.TryParseCBOR(body,                          out changeSet, out changeSetError, ChangeSetLimits)
                    : !RoamingNetworkChangeSet.TryParse    (Encoding.UTF8.GetString(body), out changeSet, out changeSetError, ChangeSetLimits))
            {
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid change set: " + changeSetError);
            }

            if (changeSet.RoamingNetworkId != history.RoamingNetworkId)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, $"The change set is for the roaming network '{changeSet.RoamingNetworkId}', not for '{history.RoamingNetworkId}'!");

            if (!AuthorizeChangeSetSubmission(Request, changeSet))
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "The sender may not submit this change set!");

            return CoSigningResponse(Request, history.CoSign(changeSet, VerifyChangeSetSignature));

        }

        #endregion

        #region (private) SubmitPendingSignatures(Request)

        private HTTPResponse SubmitPendingSignatures(HTTPRequest Request)
        {

            if (!TryGetPendingChangeSetId(Request, out var history, out var changeSetId, out var errorResponse))
                return errorResponse;

            if (AuthorizeChangeSetSubmission is null)
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "Submitting change sets is not enabled here!");

            if (!TryReadJSON(Request, out var json) ||
                !TryParseChangeSetSignatures(json[PropertyKeys.Signatures], out var signatures, out var signaturesError))
            {
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain { signatures: [ … ] }!");
            }

            if (!history.TryGetPendingChangeSet(changeSetId, out var waiting))
                return ErrorResponse(Request, HTTPStatusCode.NotFound, $"No change set '{changeSetId}' waits for signatures here!");

            if (!AuthorizeChangeSetSubmission(Request, waiting.ChangeSet!))
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "The sender may not submit this change set!");

            return CoSigningResponse(Request, history.AddPendingSignatures(changeSetId, signatures, VerifyChangeSetSignature));

        }

        #endregion

        #region (private) WithdrawPendingChangeSet(Request)

        private HTTPResponse WithdrawPendingChangeSet(HTTPRequest Request)
        {

            if (!TryGetPendingChangeSetId(Request, out var history, out var changeSetId, out var errorResponse))
                return errorResponse;

            if (AuthorizeChangeSetSubmission is null)
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "Submitting change sets is not enabled here!");

            if (!TryReadJSON(Request, out var json) ||
                !RoamingNetworkChangeSetWithdrawal.TryParse(json, out var withdrawal, out var withdrawalError))
            {
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain the withdrawal of the change set!");
            }

            if (withdrawal.ChangeSetId != changeSetId)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, $"The withdrawal is one of the change set '{withdrawal.ChangeSetId}', not of '{changeSetId}'!");

            if (!history.TryGetPendingChangeSet(changeSetId, out var waiting))
                return ErrorResponse(Request, HTTPStatusCode.NotFound, $"No change set '{changeSetId}' waits for signatures here!");

            if (!AuthorizeChangeSetSubmission(Request, waiting.ChangeSet!))
                return ErrorResponse(Request, HTTPStatusCode.Forbidden, "The sender may not withdraw this change set!");

            return CoSigningResponse(Request, history.Withdraw(withdrawal));

        }

        #endregion


        #region (private) AddCoSigningCommands(WebSocketServer, History)

        /// <summary>
        /// The co-signing actions and the topic of the change sets waiting of the WebSocket of the given history.
        /// </summary>
        private void AddCoSigningCommands(POIWebSocketServer     WebSocketServer,
                                          RoamingNetworkHistory  History)
        {

            WebSocketServer.AddCommand("pendingChangeSets", request =>
                Task.FromResult(request.Respond(PendingChangeSetsJSON(History))));

            WebSocketServer.AddCommand("pendingChangeSet", request =>

                Task.FromResult(

                    !ChangeSet_Id.TryParse(request.Payload[PropertyKeys.ChangeSetId]?.Value<String>() ?? "", out var changeSetId)
                        ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid change set identification!")

                        : History.TryGetPendingChangeSet(changeSetId, out var result)
                              ? request.Respond(result.ToJSON())
                              : request.Error(POIWebSocketErrorCodes.NotFound, $"No change set '{changeSetId}' waits for signatures here!")

                )

            );

            WebSocketServer.AddCommand("coSignChangeSet", request => {

                if (AuthorizeChangeSetSubmission is null)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "Submitting change sets is not enabled here!"));

                if (request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The sender of the change set is unknown!"));

                RoamingNetworkChangeSet? changeSet       = null;
                String?                  changeSetError  = "The request must contain a change set!";

                var hasChangeSet = request.IsBinary

                                       ? request.Message.PayloadCBOR?.TryGetValue(CBORValue.FromText(PropertyKeys.ChangeSet), out var changeSetCBOR) == true &&
                                         RoamingNetworkChangeSet.TryParse(changeSetCBOR, out changeSet, out changeSetError, ChangeSetLimits)

                                       : request.Payload[PropertyKeys.ChangeSet] is JObject changeSetJSON &&
                                         RoamingNetworkChangeSet.TryParse(changeSetJSON, out changeSet, out changeSetError, ChangeSetLimits);

                if (!hasChangeSet || changeSet is null)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid change set: " + changeSetError));

                if (!AuthorizeChangeSetSubmission(upgradeRequest, changeSet))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The sender may not submit this change set!"));

                return Task.FromResult(request.Respond(History.CoSign(changeSet, VerifyChangeSetSignature).ToJSON()));

            }, Batches: true);

            WebSocketServer.AddCommand("addPendingSignatures", request => {

                if (AuthorizeChangeSetSubmission is null)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "Submitting change sets is not enabled here!"));

                if (request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The signer is unknown!"));

                if (!ChangeSet_Id.TryParse(request.Payload[PropertyKeys.ChangeSetId]?.Value<String>() ?? "", out var changeSetId) ||
                    !TryParseChangeSetSignatures(request.Payload[PropertyKeys.Signatures], out var signatures, out _))
                {
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "The request must contain { changeSetId, signatures: [ … ] }!"));
                }

                if (!History.TryGetPendingChangeSet(changeSetId, out var waiting))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.NotFound, $"No change set '{changeSetId}' waits for signatures here!"));

                if (!AuthorizeChangeSetSubmission(upgradeRequest, waiting.ChangeSet!))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The sender may not submit this change set!"));

                return Task.FromResult(request.Respond(History.AddPendingSignatures(changeSetId, signatures, VerifyChangeSetSignature).ToJSON()));

            }, Batches: true);

            WebSocketServer.AddCommand("withdrawChangeSet", request => {

                if (AuthorizeChangeSetSubmission is null)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "Submitting change sets is not enabled here!"));

                if (request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The sender is unknown!"));

                if (request.Payload[PropertyKeys.Withdrawal] is not JObject withdrawalJSON ||
                    !RoamingNetworkChangeSetWithdrawal.TryParse(withdrawalJSON, out var withdrawal, out var withdrawalError))
                {
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "The request must contain { withdrawal }!"));
                }

                if (!History.TryGetPendingChangeSet(withdrawal.ChangeSetId, out var waiting))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.NotFound, $"No change set '{withdrawal.ChangeSetId}' waits for signatures here!"));

                if (!AuthorizeChangeSetSubmission(upgradeRequest, waiting.ChangeSet!))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The sender may not withdraw this change set!"));

                return Task.FromResult(request.Respond(History.Withdraw(withdrawal).ToJSON()));

            }, Batches: true);

            WebSocketServer.AddTopic(new POIWebSocketTopic(PendingChangeSetsTopic,
                                                           "pendingChangeSet",
                                                           "Every change set that starts or keeps waiting for the signatures of its owners, with those it has and those still missing, or waits no longer."));

        }

        #endregion

        #region (private static) ConnectPendingChangeSets(History, WebSocketServer)

        /// <summary>
        /// Push the change sets of the given history waiting for signatures, or no longer, to the subscribers; returns how
        /// to stop it.
        /// </summary>
        private static Action ConnectPendingChangeSets(RoamingNetworkHistory  History,
                                                       POIWebSocketServer     WebSocketServer)
        {

            OnRoamingNetworkPendingChangeSetDelegate onPendingChangeSet = result =>
                WebSocketServer.Push(PendingChangeSetsTopic, result.ToJSON());

            History.OnPendingChangeSet += onPendingChangeSet;

            return () => History.OnPendingChangeSet -= onPendingChangeSet;

        }

        #endregion


        #region (private static) PendingChangeSetsJSON(History)

        /// <summary>
        /// { pendingChangeSets: [ { status, changeSet, missing } ] }
        /// </summary>
        private static JObject PendingChangeSetsJSON(RoamingNetworkHistory History)

            => new (new JProperty(PropertyKeys.PendingChangeSets,
                                  new JArray(History.PendingChangeSets.
                                                 Select(changeSet => History.TryGetPendingChangeSet(changeSet.Id, out var result) ? result.ToJSON() : null).
                                                 Where (json      => json is not null))));

        #endregion

        #region (private static) CoSigningResponse(Request, Result)

        private static HTTPResponse CoSigningResponse(HTTPRequest                    Request,
                                                      RoamingNetworkCoSigningResult  Result)

            => JSONResponse(Request,
                            Result.Status switch {
                                RoamingNetworkCoSigningStatus.Pending           => HTTPStatusCode.Accepted,
                                RoamingNetworkCoSigningStatus.Committed         => HTTPStatusCode.Created,
                                RoamingNetworkCoSigningStatus.AlreadyCommitted  => HTTPStatusCode.OK,
                                RoamingNetworkCoSigningStatus.Withdrawn         => HTTPStatusCode.OK,
                                RoamingNetworkCoSigningStatus.NotFound          => HTTPStatusCode.NotFound,
                                _                                               => HTTPStatusCode.UnprocessableEntity
                            },
                            Result.ToJSON());

        #endregion

        #region (private) TryGetPendingChangeSetId(Request, out History, out ChangeSetId, out ErrorResponse)

        private Boolean TryGetPendingChangeSetId(HTTPRequest                                      Request,
                                                 [NotNullWhen(true)]  out RoamingNetworkHistory?  History,
                                                 out ChangeSet_Id                                 ChangeSetId,
                                                 [NotNullWhen(false)] out HTTPResponse?           ErrorResponse)
        {

            ChangeSetId = default;

            if (!TryGetHistory(Request, out History, out ErrorResponse))
                return false;

            if (Request.ParsedURLParameters.Length < 2 ||
                !ChangeSet_Id.TryParse(Request.ParsedURLParameters[1], out ChangeSetId))
            {
                History        = null;
                ErrorResponse  = this.ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid change set identification!");
                return false;
            }

            return true;

        }

        #endregion

        #region (private static) TryReadJSON(Request, out JSON)

        /// <summary>
        /// The JSON object of the body of the given request, in JSON or CBOR, its texts as they are written: they are signed.
        /// </summary>
        private static Boolean TryReadJSON(HTTPRequest                       Request,
                                           [NotNullWhen(true)] out JObject?  JSON)
        {

            JSON = null;

            try
            {
                JSON = Request.HTTPBody is { Length: > 0 } body
                           ? HasCBORBody(Request)
                                 ? CBORJSON.ToJSON(CBORValue.Parse(body)) as JObject
                                 : POIJSON.Read(Encoding.UTF8.GetString(body)) as JObject
                           : null;
            }
            catch
            { }

            return JSON is not null;

        }

        #endregion

        #region (private static) TryParseChangeSetSignatures(JSON, out Signatures, out ErrorResponse)

        private static Boolean TryParseChangeSetSignatures(JToken?                                                                   JSON,
                                                           [NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkChangeSetSignature>?  Signatures,
                                                           [NotNullWhen(false)] out String?                                          ErrorResponse)
        {

            Signatures = null;

            if (JSON is not JArray signaturesJSON || signaturesJSON.Count == 0)
            {
                ErrorResponse = "The signatures must be a JSON array of at least one signature!";
                return false;
            }

            var signatures = new List<RoamingNetworkChangeSetSignature>();

            foreach (var signatureJSON in signaturesJSON)
            {

                if (signatureJSON is not JObject signatureObject ||
                    !RoamingNetworkChangeSetSignature.TryParse(signatureObject, out var signature, out var signatureError))
                {
                    ErrorResponse = $"Invalid signature {signatures.Count}!";
                    return false;
                }

                signatures.Add(signature);

            }

            Signatures     = signatures;
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
