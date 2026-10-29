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

using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate deciding whether the sender of the given HTTP request may add the given signature to the given checkpoint,
    /// e.g. whether it is a witness this server knows.
    /// </summary>
    /// <param name="Request">The HTTP request, or the WebSocket upgrade request of the sender.</param>
    /// <param name="Checkpoint">The checkpoint.</param>
    /// <param name="Signature">The signature to add.</param>
    public delegate Boolean AuthorizeCheckpointSignatureDelegate(HTTPRequest                    Request,
                                                                 RoamingNetworkCheckpoint       Checkpoint,
                                                                 RoamingNetworkCommitSignature  Signature);

    /// <summary>
    /// A delegate deciding whether the sender of the given HTTP request may push the given checkpoints, e.g. a peer.
    /// </summary>
    /// <param name="Request">The HTTP request, or the WebSocket upgrade request of the sender.</param>
    /// <param name="Checkpoints">The checkpoints.</param>
    public delegate Boolean AuthorizeCheckpointPushDelegate(HTTPRequest                              Request,
                                                            IReadOnlyList<RoamingNetworkCheckpoint>  Checkpoints);


    /// <summary>
    /// The checkpoints of the histories of roaming networks over HTTP and the WebSocket (SPECIFICATION.md, section 10).
    /// </summary>
    /// <remarks>
    /// GET    ~/RNs/{roamingNetworkId}/history/checkpoints?since={sequence}&amp;max={n}           the checkpoints after the given sequence
    /// GET    ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}                         one checkpoint
    /// GET    ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/snapshot                the version of its commit, to join from
    /// GET    ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/commits/{commitId}/proof the proof of a commit in it
    /// GET    ~/RNs/{roamingNetworkId}/history/commits/{commitId}/proof                           the proof of a commit in the first checkpoint including it
    /// POST   ~/RNs/{roamingNetworkId}/history/checkpoints                                        push checkpoints: { checkpoints: [ … ], priceStatements? }
    /// POST   ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/signatures              add signatures, e.g. of witnesses: { signatures: [ … ] }
    ///
    /// WebSocket actions: checkpoints { since?, max? } or { latest: true }, checkpoint { checkpointId }, checkpointSnapshot { checkpointId },
    /// commitProof { commitId, checkpointId? }, signCheckpoint { checkpointId, signatures }, pushCheckpoints { checkpoints, priceStatements? };
    /// priceStatements? are those the checkpoints name: [ { checkpointId, priceStatements: [ … ] }, … ];
    /// topics: checkpoints (action checkpoint), checkpointSignatures (action checkpointSignatures, { checkpointId, signatures }).
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The topic of new checkpoints.
        /// </summary>
        public const String CheckpointsTopic           = "checkpoints";

        /// <summary>
        /// The topic of signatures added to checkpoints later, e.g. of witnesses.
        /// </summary>
        public const String CheckpointSignaturesTopic  = "checkpointSignatures";

        #endregion

        #region Properties

        /// <summary>
        /// Decides who may add which signatures to checkpoints, e.g. witnesses. Without it, adding signatures is refused.
        /// </summary>
        public AuthorizeCheckpointSignatureDelegate?  AuthorizeCheckpointSignature    { get; set; }

        /// <summary>
        /// Decides who may push checkpoints, e.g. peers. Without it, pushing checkpoints is refused.
        /// </summary>
        public AuthorizeCheckpointPushDelegate?       AuthorizeCheckpointPush         { get; set; }

        /// <summary>
        /// Verifies the signatures of checkpoints with trusted keys; without it, signed checkpoints and signatures are refused.
        /// </summary>
        public VerifyCheckpointSignatureDelegate?     VerifyCheckpointSignature       { get; set; }

        /// <summary>
        /// The roles a pushed checkpoint must be signed in: the roaming network operator by default,
        /// so that nobody takes the place of the next checkpoint with one of their own.
        /// </summary>
        public IReadOnlyCollection<String>            RequiredCheckpointSignatureRoles   { get; set; } = [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ];

        #endregion


        #region (private) RegisterCheckpointURLTemplates(History)

        private void RegisterCheckpointURLTemplates(HTTPPath History)
        {

            RegisterArchiveURLTemplates(History);
            RegisterHistoryPriceStatementURLTemplates(History);

            #region GET ~/RNs/{roamingNetworkId}/history/checkpoints?since={sequence}&max={n}

            AddHandler(History + "/checkpoints",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : !TryGetSequence(request.QueryString.GetString("since"), out var since) ||
                                 !TryGetMax     (request.QueryString.GetString("max"),   out var max)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid since or max!")

                                     : JSONResponse(request,
                                                    HTTPStatusCode.OK,
                                                    new JArray(roamingNetworkHistory.GetCheckpointsSince(since, max).Select(checkpoint => checkpoint.ToJSON(Embedded: true))))

                       ));

            AddReadPreflight(History + "/checkpoints", HTTPMethod.POST);

            #endregion

            #region POST ~/RNs/{roamingNetworkId}/history/checkpoints

            AddHandler(History + "/checkpoints",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  Changes(SubmitCheckpoints));

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}

            AddHandler(History + "/checkpoints/{checkpointId}",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetCheckpoint(request, out var roamingNetworkHistory, out var checkpoint, out var errorResponse)
                               ? errorResponse
                               : JSONResponse(request, HTTPStatusCode.OK, checkpoint.ToJSON())

                       ));

            AddReadPreflight(History + "/checkpoints/{checkpointId}");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/snapshot

            AddHandler(History + "/checkpoints/{checkpointId}/snapshot",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  HeavyRead(request => Task.FromResult(

                           !TryGetCheckpoint(request, out var roamingNetworkHistory, out var checkpoint, out var errorResponse)
                               ? errorResponse

                               : roamingNetworkHistory.TryGetCheckpointSnapshotJSON(checkpoint.Id, out var snapshot, out var snapshotError)
                                     ? JSONResponse(request, HTTPStatusCode.OK, snapshot)
                                     : ErrorResponse(request, HTTPStatusCode.NotFound, snapshotError)

                       )));

            AddReadPreflight(History + "/checkpoints/{checkpointId}/snapshot");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/commits/{commitId}/proof

            AddHandler(History + "/checkpoints/{checkpointId}/commits/{commitId}/proof",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetCheckpoint(request, out var roamingNetworkHistory, out var checkpoint, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 3 ||
                                 !RoamingNetworkCommit_Id.TryParse(request.ParsedURLParameters[2], out var commitId)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid commit identification!")

                                     : roamingNetworkHistory.TryGetCommitProof(commitId, out var proof, out var proofError, checkpoint.Id)
                                           ? JSONResponse(request, HTTPStatusCode.OK, proof.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, proofError)

                       ));

            AddReadPreflight(History + "/checkpoints/{checkpointId}/commits/{commitId}/proof");

            #endregion

            #region GET ~/RNs/{roamingNetworkId}/history/commits/{commitId}/proof

            AddHandler(History + "/commits/{commitId}/proof",
                       HTTPMethod:    HTTPMethod.GET,
                       HTTPDelegate:  request => Task.FromResult(

                           !TryGetHistory(request, out var roamingNetworkHistory, out var errorResponse)
                               ? errorResponse

                               : request.ParsedURLParameters.Length < 2 ||
                                 !RoamingNetworkCommit_Id.TryParse(request.ParsedURLParameters[1], out var commitId)
                                     ? ErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid commit identification!")

                                     : roamingNetworkHistory.TryGetCommitProof(commitId, out var proof, out var proofError)
                                           ? JSONResponse(request, HTTPStatusCode.OK, proof.ToJSON())
                                           : ErrorResponse(request, HTTPStatusCode.NotFound, proofError)

                       ));

            AddReadPreflight(History + "/commits/{commitId}/proof");

            #endregion

            #region POST ~/RNs/{roamingNetworkId}/history/checkpoints/{checkpointId}/signatures

            AddHandler(History + "/checkpoints/{checkpointId}/signatures",
                       HTTPMethod:    HTTPMethod.POST,
                       HTTPDelegate:  Changes(SubmitCheckpointSignatures));

            #endregion

        }

        #endregion


        #region (private) SubmitCheckpoints(Request)

        /// <summary>
        /// Take the checkpoints in the body of the request, { checkpoints: [ … ], priceStatements? }, in their order, with the
        /// price statements they name when given: [ { checkpointId, priceStatements: [ … ] }, … ].
        /// 200 OK:             all taken, or held already; { checkpoints: [ ids ] }
        /// 400 Bad Request:    no valid checkpoints
        /// 403 Forbidden:      pushing checkpoints is not enabled, or not allowed for the sender
        /// 422 Unprocessable:  one was refused, e.g. as it does not fit this history or is not signed as required
        /// </summary>
        private HTTPResponse SubmitCheckpoints(HTTPRequest Request)
        {

            if (!TryGetHistory(Request, out var history, out var errorResponse))
                return errorResponse;

            if (!TryReadBody(Request, out var json) || json[PropertyKeys.Checkpoints] is not JArray checkpointsJSON)
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain { checkpoints: [ … ] }!");

            if (!TryParseCheckpoints(checkpointsJSON, out var checkpoints, out var parseError))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, parseError);

            if (!TryParseCheckpointPriceStatements(json[PropertyKeys.PriceStatements], out var priceStatements, out var priceStatementsError))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, priceStatementsError);

            var result = PushCheckpoints(Request, history, checkpoints, priceStatements);

            return result.Error is null
                       ? JSONResponse (Request, HTTPStatusCode.OK, result.Answer)
                       : ErrorResponse(Request, result.Forbidden ? HTTPStatusCode.Forbidden : HTTPStatusCode.UnprocessableEntity, result.Error);

        }

        #endregion

        #region (private) SubmitCheckpointSignatures(Request)

        /// <summary>
        /// Add the signatures in the body of the request, { signatures: [ … ] }, to a checkpoint.
        /// 201 Created: at least one added; 200 OK: all known; 400; 403; 404; 422 invalid.
        /// </summary>
        private HTTPResponse SubmitCheckpointSignatures(HTTPRequest Request)
        {

            if (!TryGetCheckpoint(Request, out var history, out var checkpoint, out var errorResponse))
                return errorResponse;

            if (!TryReadBody(Request, out var json))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, "The request must contain { signatures: [ … ] }!");

            if (!TryParseSignatures(checkpoint.Id, json[RoamingNetworkCommitSignature.PropertyKeys.Signatures], out var signatures, out var signaturesError))
                return ErrorResponse(Request, HTTPStatusCode.BadRequest, signaturesError);

            var result = AddCheckpointSignatures(Request, history, checkpoint.Id, signatures);

            return result.Status switch {
                       CommitSignaturesStatus.Added      => JSONResponse (Request, HTTPStatusCode.Created,              result.Answer),
                       CommitSignaturesStatus.Known      => JSONResponse (Request, HTTPStatusCode.OK,                   result.Answer),
                       CommitSignaturesStatus.NotFound   => ErrorResponse(Request, HTTPStatusCode.NotFound,             result.Error),
                       CommitSignaturesStatus.Forbidden  => ErrorResponse(Request, HTTPStatusCode.Forbidden,            result.Error),
                       _                                 => ErrorResponse(Request, HTTPStatusCode.UnprocessableEntity,  result.Error)
                   };

        }

        #endregion


        #region AddCheckpointSignatures(Request, History, CheckpointId, Signatures)

        /// <summary>
        /// Add the given signatures of the sender of the given request to a checkpoint of the given history,
        /// when allowed for the sender and verified with trusted keys.
        /// </summary>
        /// <param name="Request">The HTTP request, or the WebSocket upgrade request of the sender.</param>
        /// <param name="History">The history.</param>
        /// <param name="CheckpointId">The identification of the checkpoint.</param>
        /// <param name="Signatures">The signatures to add.</param>
        /// <returns>The outcome, the answer { checkpointId, signatures, added } and why it failed.</returns>
        public (CommitSignaturesStatus Status, JObject Answer, String? Error) AddCheckpointSignatures(HTTPRequest                                 Request,
                                                                                                    RoamingNetworkHistory                       History,
                                                                                                    RoamingNetworkCheckpoint_Id                 CheckpointId,
                                                                                                    IEnumerable<RoamingNetworkCommitSignature>  Signatures)
        {

            if (AuthorizeCheckpointSignature is null)
                return (CommitSignaturesStatus.Forbidden, [], "Adding signatures to checkpoints is not enabled here!");

            if (!History.TryGetCheckpoint(CheckpointId, out var checkpoint))
                return (CommitSignaturesStatus.NotFound,  [], $"The checkpoint '{CheckpointId}' is unknown here!");

            var signatures = Signatures.ToArray();

            foreach (var signature in signatures)
                if (!AuthorizeCheckpointSignature(Request, checkpoint, signature))
                    return (CommitSignaturesStatus.Forbidden, [], $"The sender may not add the signature by {signature}!");

            if (!History.TryAddCheckpointSignatures(CheckpointId, signatures, VerifyCheckpointSignature, out var signed, out var newSignatures, out var errorResponse))
                return (CommitSignaturesStatus.Invalid, [], errorResponse);

            return (newSignatures.Count > 0 ? CommitSignaturesStatus.Added : CommitSignaturesStatus.Known,
                    new JObject(
                        new JProperty(PropertyKeys.CheckpointId,  CheckpointId.ToString()),
                        new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Signatures,    new JArray(signed.Signatures.Select(signature => signature.ToJSON(Embedded: true)))),
                        new JProperty(PropertyKeys.Added,         newSignatures.Count)
                    ),
                    null);

        }

        #endregion

        #region PushCheckpoints(Request, History, Checkpoints, PriceStatements = null)

        /// <summary>
        /// Take the given checkpoints of the sender of the given request, in their order, when allowed for the sender,
        /// checked against the history and signed in the RequiredCheckpointSignatureRoles, with the price statements they name when
        /// given.
        /// </summary>
        /// <returns>The answer { checkpoints: [ ids ] }, whether it was forbidden, and why it failed.</returns>
        public (JObject Answer, Boolean Forbidden, String? Error) PushCheckpoints(HTTPRequest                                                                                Request,
                                                                                  RoamingNetworkHistory                                                                      History,
                                                                                  IReadOnlyList<RoamingNetworkCheckpoint>                                                    Checkpoints,
                                                                                  IReadOnlyDictionary<RoamingNetworkCheckpoint_Id, IReadOnlyList<RoamingNetworkPriceStatement>>?  PriceStatements   = null)
        {

            if (AuthorizeCheckpointPush is null)
                return ([], true, "Pushing checkpoints is not enabled here!");

            if (!AuthorizeCheckpointPush(Request, Checkpoints))
                return ([], true, "The sender may not push these checkpoints!");

            var taken = new JArray();

            foreach (var checkpoint in Checkpoints)
            {

                if (!History.TryAddCheckpoint(checkpoint,
                                              out var errorResponse,
                                              VerifyCheckpointSignature,
                                              RequiredCheckpointSignatureRoles,
                                              PriceStatements?.GetValueOrDefault(checkpoint.Id)))
                    return ([], false, $"Checkpoint {taken.Count}: {errorResponse}");

                taken.Add(checkpoint.Id.ToString());

            }

            return (new JObject(new JProperty(PropertyKeys.Checkpoints, taken)), false, null);

        }

        #endregion


        #region (private) AddCheckpointCommands(WebSocketServer, History)

        /// <summary>
        /// The checkpoint actions and topics of the WebSocket of the given history.
        /// </summary>
        private void AddCheckpointCommands(POIWebSocketServer     WebSocketServer,
                                           RoamingNetworkHistory  History)
        {

            AddArchiveCommands(WebSocketServer, History);
            AddProofCommands  (WebSocketServer, History);
            AddPriceStatementCommands(WebSocketServer, History);
            AddCoSigningCommands     (WebSocketServer, History);
            AddEpochCommands         (WebSocketServer, History);

            WebSocketServer.AddCommand("checkpoints", request =>

                Task.FromResult(

                    // { latest: true }: only the latest one, e.g. to know what a peer misses.
                    request.Payload[PropertyKeys.Latest]?.Type == JTokenType.Boolean && request.Payload[PropertyKeys.Latest]!.Value<Boolean>()
                        ? request.Respond(new JObject(new JProperty(PropertyKeys.Checkpoints, new JArray(History.LatestCheckpoint is RoamingNetworkCheckpoint latest
                                                                                                    ? [ latest.ToJSON(Embedded: true) ]
                                                                                                    : Array.Empty<JObject>()))))

                        : !TryGetSequence(request.Payload[PropertyKeys.Since]?.ToString(), out var since) ||
                    !TryGetMax     (request.Payload[PropertyKeys.Max]?.  ToString(), out var max)
                        ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Invalid since or max!")
                        : request.Respond(new JObject(new JProperty(PropertyKeys.Checkpoints, new JArray(History.GetCheckpointsSince(since, max).Select(checkpoint => checkpoint.ToJSON(Embedded: true))))))

                )

            );

            WebSocketServer.AddCommand("checkpoint", request =>

                Task.FromResult(

                    !RoamingNetworkCheckpoint_Id.TryParse(request.Payload[PropertyKeys.CheckpointId]?.Value<String>() ?? "", out var checkpointId)
                        ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid checkpoint identification!")

                        : History.TryGetCheckpoint(checkpointId, out var checkpoint)
                              ? request.Respond(checkpoint.ToJSON())
                              : request.Error(POIWebSocketErrorCodes.NotFound, $"The checkpoint '{checkpointId}' is unknown here!")

                )

            );

            WebSocketServer.AddCommand("checkpointSnapshot", HeavyCommand(request =>

                Task.FromResult(

                    !RoamingNetworkCheckpoint_Id.TryParse(request.Payload[PropertyKeys.CheckpointId]?.Value<String>() ?? "", out var checkpointId)
                        ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid checkpoint identification!")

                        : History.TryGetCheckpointSnapshotJSON(checkpointId, out var snapshot, out var snapshotError)
                              ? request.Respond(snapshot)
                              : request.Error(POIWebSocketErrorCodes.NotFound, snapshotError)

                )

            ));

            WebSocketServer.AddCommand("commitProof", request => {

                if (!RoamingNetworkCommit_Id.TryParse(request.Payload[RoamingNetworkCommitSignature.PropertyKeys.CommitId]?.Value<String>() ?? "", out var commitId))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid commit identification!"));

                RoamingNetworkCheckpoint_Id? checkpointId = null;

                if (request.Payload[PropertyKeys.CheckpointId] is JToken)
                {

                    if (!RoamingNetworkCheckpoint_Id.TryParse(request.Payload[PropertyKeys.CheckpointId]?.Value<String>() ?? "", out var parsedCheckpointId))
                        return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "Invalid checkpoint identification!"));

                    checkpointId = parsedCheckpointId;

                }

                return Task.FromResult(
                           History.TryGetCommitProof(commitId, out var proof, out var proofError, checkpointId)
                               ? request.Respond(proof.ToJSON())
                               : request.Error(POIWebSocketErrorCodes.NotFound, proofError)
                       );

            });

            WebSocketServer.AddCommand("signCheckpoint", request => {

                if (request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The signer is unknown!"));

                if (!RoamingNetworkCheckpoint_Id.TryParse(request.Payload[PropertyKeys.CheckpointId]?.Value<String>() ?? "", out var checkpointId))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "Missing or invalid checkpoint identification!"));

                if (!TryParseSignatures(checkpointId, request.Payload[RoamingNetworkCommitSignature.PropertyKeys.Signatures], out var signatures, out var signaturesError))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, signaturesError));

                var result = AddCheckpointSignatures(upgradeRequest, History, checkpointId, signatures);

                return Task.FromResult(result.Status switch {
                                           CommitSignaturesStatus.Added or
                                           CommitSignaturesStatus.Known     => request.Respond(result.Answer),
                                           CommitSignaturesStatus.NotFound  => request.Error(POIWebSocketErrorCodes.NotFound,      result.Error),
                                           _                                => request.Error(POIWebSocketErrorCodes.SecurityError, result.Error)
                                       });

            }, Batches: true);

            WebSocketServer.AddCommand("pushCheckpoints", request => {

                if (request.Session.UpgradeRequest is not HTTPRequest upgradeRequest)
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.SecurityError, "The sender of the checkpoints is unknown!"));

                if (request.Payload[PropertyKeys.Checkpoints] is not JArray checkpointsJSON ||
                    !TryParseCheckpoints(checkpointsJSON, out var checkpoints, out var parseError))
                {
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "The request must contain { checkpoints: [ … ] }!"));
                }

                if (!TryParseCheckpointPriceStatements(request.Payload[PropertyKeys.PriceStatements], out var priceStatements, out var priceStatementsError))
                    return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, priceStatementsError));

                var result = PushCheckpoints(upgradeRequest, History, checkpoints, priceStatements);

                return Task.FromResult(result.Error is null
                                           ? request.Respond(result.Answer)
                                           : request.Error(POIWebSocketErrorCodes.SecurityError, result.Error));

            });

            WebSocketServer.AddTopic(new POIWebSocketTopic(CheckpointsTopic,
                                                           "checkpoint",
                                                           "Every new checkpoint: a signed statement that a commit, and every commit it follows, is part of the history."));

            WebSocketServer.AddTopic(new POIWebSocketTopic(CheckpointSignaturesTopic,
                                                           "checkpointSignatures",
                                                           "The signatures added to a checkpoint later, e.g. of witnesses: { checkpointId, signatures }."));

        }

        #endregion

        #region (private) ConnectCheckpoints(History, WebSocketServer)

        /// <summary>
        /// Push the new checkpoints and their signatures of the given history to the subscribers; returns how to stop it.
        /// </summary>
        private static Action ConnectCheckpoints(RoamingNetworkHistory  History,
                                                 POIWebSocketServer     WebSocketServer)
        {

            OnRoamingNetworkCheckpointDelegate onCheckpoint = checkpoint =>
                WebSocketServer.Push(CheckpointsTopic, checkpoint.ToJSON());

            OnRoamingNetworkCheckpointSignaturesDelegate onCheckpointSignatures = (checkpoint, newSignatures) =>
                WebSocketServer.Push(CheckpointSignaturesTopic,
                                     new JObject(
                                         new JProperty(PropertyKeys.CheckpointId,  checkpoint.Id.ToString()),
                                         new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Signatures,    new JArray(newSignatures.Select(signature => signature.ToJSON(Embedded: true))))
                                     ));

            History.OnCheckpoint            += onCheckpoint;
            History.OnCheckpointSignatures  += onCheckpointSignatures;

            var disconnectAttestations     = ConnectAttestations   (History, WebSocketServer);
            var disconnectPriceStatements  = ConnectPriceStatements(History, WebSocketServer);
            var disconnectPending          = ConnectPendingChangeSets(History, WebSocketServer);

            return () => {
                History.OnCheckpoint            -= onCheckpoint;
                History.OnCheckpointSignatures  -= onCheckpointSignatures;
                disconnectAttestations();
                disconnectPriceStatements();
                disconnectPending();
            };

        }

        #endregion


        #region (private) TryGetCheckpoint(Request, out History, out Checkpoint, out ErrorResponse)

        private Boolean TryGetCheckpoint(HTTPRequest                                                                Request,
                                         [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]  out RoamingNetworkHistory?     History,
                                         [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]  out RoamingNetworkCheckpoint?  Checkpoint,
                                         [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out HTTPResponse?              ErrorResponse)
        {

            Checkpoint = null;

            if (!TryGetHistory(Request, out History, out ErrorResponse))
                return false;

            if (Request.ParsedURLParameters.Length < 2 ||
                !RoamingNetworkCheckpoint_Id.TryParse(Request.ParsedURLParameters[1], out var checkpointId))
            {
                ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.BadRequest, "Invalid checkpoint identification!");
                return false;
            }

            if (!History.TryGetCheckpoint(checkpointId, out Checkpoint))
            {
                ErrorResponse = this.ErrorResponse(Request, HTTPStatusCode.NotFound, $"The checkpoint '{checkpointId}' is unknown here!");
                return false;
            }

            return true;

        }

        #endregion

        #region (private static) TryReadBody(Request, out JSON)

        /// <summary>
        /// The JSON object of the body, also read from CBOR; texts stay texts, as signed values are signed as written.
        /// </summary>
        private static Boolean TryReadBody(HTTPRequest Request, out JObject JSON)
        {

            JSON = [];

            try
            {

                if (Request.HTTPBody is not { Length: > 0 } body)
                    return false;

                var json = HasCBORBody(Request)
                               ? CBORJSON.ToJSON(CBORValue.Parse(body)) as JObject
                               : POIJSON.Read(Encoding.UTF8.GetString(body)) as JObject;

                if (json is null)
                    return false;

                JSON = json;
                return true;

            }
            catch
            {
                return false;
            }

        }

        #endregion

        #region (private static) TryParseCheckpoints(JSON, out Checkpoints, out ErrorResponse)

        private static Boolean TryParseCheckpoints(JArray                                                                   JSON,
                                                   out IReadOnlyList<RoamingNetworkCheckpoint>                              Checkpoints,
                                                   [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out String?         ErrorResponse)
        {

            var checkpoints = new List<RoamingNetworkCheckpoint>();
            Checkpoints = checkpoints;

            foreach (var checkpointJSON in JSON)
            {

                if (checkpointJSON is not JObject checkpointObject ||
                    !RoamingNetworkCheckpoint.TryParse(checkpointObject, out var checkpoint, out var checkpointError))
                {
                    ErrorResponse = $"Invalid checkpoint {checkpoints.Count}!";
                    return false;
                }

                checkpoints.Add(checkpoint);

            }

            if (checkpoints.Count == 0)
            {
                ErrorResponse = "At least one checkpoint is needed!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) TryParseSignatures(CheckpointId, JSON, out Signatures, out ErrorResponse)

        private static Boolean TryParseSignatures(RoamingNetworkCheckpoint_Id                                         CheckpointId,
                                                  JToken?                                                             JSON,
                                                  out IReadOnlyList<RoamingNetworkCommitSignature>                    Signatures,
                                                  [System.Diagnostics.CodeAnalysis.NotNullWhen(false)] out String?    ErrorResponse)
        {

            Signatures = [];

            // The same form as the signatures of a commit.
            if (!RoamingNetworkCommitSignature.TryParseCommitSignatures(new JObject(
                                                                            new JProperty(RoamingNetworkCommitSignature.PropertyKeys.CommitId,    CheckpointId.ToString()),
                                                                            new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Signatures,  JSON)
                                                                        ),
                                                                        out _,
                                                                        out var signatures,
                                                                        out ErrorResponse))
            {
                return false;
            }

            Signatures = signatures;
            return true;

        }

        #endregion

        #region (private static) TryGetSequence(Text, out Sequence)

        private static Boolean TryGetSequence(String? Text, out UInt64? Sequence)
        {

            Sequence = null;

            if (String.IsNullOrEmpty(Text))
                return true;

            if (!UInt64.TryParse(Text, out var sequence))
                return false;

            Sequence = sequence;
            return true;

        }

        #endregion

        #region (private static) TryGetMax(Text, out Max)

        private static Boolean TryGetMax(String? Text, out Int32 Max)
        {

            Max = MaxCommitsPerRequest;

            if (String.IsNullOrEmpty(Text))
                return true;

            if (!Int32.TryParse(Text, out var max) || max < 1)
                return false;

            Max = Math.Min(max, MaxCommitsPerRequest);
            return true;

        }

        #endregion

    }

}
