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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The attestations of the heads of roaming networks and the proofs of their charging stations over HTTP and the
    /// WebSocket (SPECIFICATION.md, sections 8.4 and 8.5): a driver's app gets one small document with everything the
    /// roaming network says about a charging station, and checks it offline against an attestation of a key it trusts.
    /// </summary>
    /// <remarks>
    /// GET ~/RNs/{RN_Id}/attestations                                               the attestations of the head valid now
    /// GET ~/RNs/{RN_Id}/keyStatement                                               the key statement of the operator of the head
    /// GET ~/RNs/{RN_Id}/trustAnchor                                                what an app pins: { roamingNetworkId, identityKeyIds, uri }
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/proof                 the proof of a charging station of the head,
    ///                                                                              ?hashAlgorithm= one of the network's, its first by default
    /// GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/proof the proof of the charging station of an EVSE
    /// GET ~/RNs/{RN_Id}/CSs/{CS_Id}/proof, ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/proof        the same, found by the identification alone
    ///
    /// WebSocket actions: attestations {}, chargingStationProof { chargingStationId | EVSEId, hashAlgorithm? };
    /// topic: attestations (action attestation), every attestation the history takes.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The topic of new attestations.
        /// </summary>
        public const String AttestationsTopic = "attestations";

        #endregion


        #region (private) RegisterProofURLTemplates()

        private void RegisterProofURLTemplates()
        {

            var roamingNetworkPath   = HTTPPath.Root + "RNs/{roamingNetworkId}";
            var chargingStationPath  = roamingNetworkPath + "/CSOs/{chargingStationOperatorId}/CPs/{chargingPoolId}/CSs/{chargingStationId}";

            #region GET ~/RNs/{RN_Id}/attestations

            AddEntityHandlers(roamingNetworkPath + "/attestations",
                              request => TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse)
                                             ? EntityResponse(request,
                                                              new JObject(
                                                                  new JProperty(PropertyKeys.Attestations,
                                                                                new JArray((roamingNetworkHistories.TryGetValue(roamingNetwork.Id, out var history)
                                                                                                ? history.CurrentAttestations(TimeProvider.GetUtcNow())
                                                                                                : []).Select(attestation => attestation.ToJSON(Embedded: true))))
                                                              ))
                                             : errorResponse);

            #endregion

            #region GET ~/RNs/{RN_Id}/keyStatement

            AddEntityHandlers(roamingNetworkPath + "/keyStatement",
                              request => !TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse)
                                             ? errorResponse
                                             : roamingNetwork.Operator?.KeyStatement is RoamingNetworkKeyStatement keyStatement
                                                   ? EntityResponse(request, keyStatement.ToJSON(Embedded: true))
                                                   : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The operator of the roaming network '{roamingNetwork.Id}' states no keys!"));

            #endregion

            #region GET ~/RNs/{RN_Id}/trustAnchor

            // What an app pins, for a QR code: the roaming network as this request names it, the identity keys of its
            // operator and its minConsensus, with the name and description of the network;
            // as a URI and as the Base45 of its CBOR. Over https, unless the request is one of this machine.
            AddEntityHandlers(roamingNetworkPath + "/trustAnchor",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  if (roamingNetwork.Operator is null || roamingNetwork.Operator.IdentityKeys.IsEmpty)
                                      return EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The roaming network '{roamingNetwork.Id}' has no operator with identity keys!");

                                  var host          = request.Host;
                                  var local         = host.Name is "localhost" or "127.0.0.1" or "[::1]" or "::1";
                                  var path          = request.Path.ToString();
                                  var trustAnchor   = new RoamingNetworkTrustAnchor(roamingNetwork.Id,
                                                                                    URL.Parse($"{(local ? "http" : "https")}://{host}{path[..path.LastIndexOf("/trustAnchor", StringComparison.Ordinal)]}"),
                                                                                    [ .. roamingNetwork.Operator.IdentityKeys.Select(identityKey => identityKey.Id) ],
                                                                                    roamingNetwork.Operator.MinConsensus,
                                                                                    roamingNetwork.Name,
                                                                                    roamingNetwork.Description);

                                  return EntityResponse(request,
                                                        new JObject(
                                                            new JProperty(PropertyKeys.RoamingNetworkId,  roamingNetwork.Id.ToString()),
                                                            new JProperty(PropertyKeys.IdentityKeyIds,    new JArray(trustAnchor.IdentityKeyIds)),
                                                            new JProperty(PropertyKeys.MinConsensus,      trustAnchor.MinConsensus),
                                                            new JProperty(PropertyKeys.URI,               trustAnchor.ToURI()),
                                                            new JProperty(PropertyKeys.QRCode,            trustAnchor.ToQRCode()),
                                                            new JProperty(PropertyKeys.TrustAnchor,       trustAnchor.ToJSON())
                                                        ));

                              });

            #endregion

            #region GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/proof

            AddEntityHandlers(chargingStationPath + "/proof",
                              request => TryGetRequestedChargingStation(request, out var chargingStation, out var errorResponse)
                                             ? ProofResponse(request, chargingStation.Id, null)
                                             : errorResponse);

            #endregion

            #region GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/proof

            AddEntityHandlers(chargingStationPath + "/EVSEs/{evseId}/proof",
                              request => TryGetRequestedEVSE(request, out var evse, out var errorResponse)
                                             ? ProofResponse(request, null, evse.Id)
                                             : errorResponse);

            #endregion

            #region GET ~/RNs/{RN_Id}/CSs/{CS_Id}/proof

            AddEntityHandlers(roamingNetworkPath + "/CSs/{id}/proof",
                              request => ChargingStation_Id.TryParse(request.ParsedURLParameters.ElementAtOrDefault(1) ?? "", out var chargingStationId)
                                             ? ProofResponse(request, chargingStationId, null)
                                             : EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid charging station identification!"));

            #endregion

            #region GET ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/proof

            AddEntityHandlers(roamingNetworkPath + "/EVSEs/{id}/proof",
                              request => EVSE_Id.TryParse(request.ParsedURLParameters.ElementAtOrDefault(1) ?? "", out var evseId)
                                             ? ProofResponse(request, null, evseId)
                                             : EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid EVSE identification!"));

            #endregion

        }

        #endregion

        #region (private) ProofResponse(Request, ChargingStationId, EVSEId)

        /// <summary>
        /// The proof of the given charging station, or of the one of the given EVSE, of the requested roaming network, with
        /// the tree of the hash algorithm the request names, in JSON or in CBOR.
        /// </summary>
        private HTTPResponse ProofResponse(HTTPRequest          Request,
                                           ChargingStation_Id?  ChargingStationId,
                                           EVSE_Id?             EVSEId)
        {

            if (!TryGetRequestedRoamingNetwork(Request, out var roamingNetwork, out var errorResponse))
                return errorResponse;

            ETagHashAlgorithm? hashAlgorithm = null;

            if (Request.QueryString.GetString("hashAlgorithm") is String hashAlgorithmText)
            {

                if (!ETagHashAlgorithmExtensions.TryParse(hashAlgorithmText, out var parsed) || !roamingNetwork.TreeHashAlgorithms.Contains(parsed))
                    return EntityErrorResponse(Request,
                                               HTTPStatusCode.BadRequest,
                                               $"The roaming network '{roamingNetwork.Id}' has no tree of '{hashAlgorithmText}', but of {String.Join(", ", roamingNetwork.TreeHashAlgorithms.Select(algorithm => algorithm.ToText()))}!");

                hashAlgorithm = parsed;

            }

            return TryGetProof(roamingNetwork, ChargingStationId, EVSEId, hashAlgorithm, out var proof, out var proofError)
                       ? EntityResponse(Request, proof.ToJSON())
                       : EntityErrorResponse(Request, HTTPStatusCode.NotFound, proofError);

        }

        #endregion

        #region (private) TryGetProof(RoamingNetwork, ChargingStationId, EVSEId, HashAlgorithm, out Proof, out ErrorResponse)

        /// <summary>
        /// The proof of the given charging station, or of the one of the given EVSE: of the head of the history of the given
        /// roaming network, for its attestations, or of the roaming network itself when it has no history here.
        /// </summary>
        private Boolean TryGetProof(RoamingNetwork                                 RoamingNetwork,
                                    ChargingStation_Id?                            ChargingStationId,
                                    EVSE_Id?                                       EVSEId,
                                    ETagHashAlgorithm?                             HashAlgorithm,
                                    [NotNullWhen(true)]  out RoamingNetworkProof?  Proof,
                                    [NotNullWhen(false)] out String?               ErrorResponse)
        {

            if (roamingNetworkHistories.TryGetValue(RoamingNetwork.Id, out var history))
                return ChargingStationId.HasValue
                           ? history.TryGetChargingStationProof(ChargingStationId.Value, out Proof, out ErrorResponse, HashAlgorithm, TimeProvider.GetUtcNow())
                           : history.TryGetChargingStationProof(EVSEId!.Value,           out Proof, out ErrorResponse, HashAlgorithm, TimeProvider.GetUtcNow());

            var tree = RoamingNetwork.EffectiveTree(HashAlgorithm ?? RoamingNetwork.TreeHashAlgorithms[0]);

            return ChargingStationId.HasValue
                       ? tree.TryGetChargingStationProof(ChargingStationId.Value, out Proof, out ErrorResponse)
                       : tree.TryGetChargingStationProof(EVSEId!.Value,           out Proof, out ErrorResponse);

        }

        #endregion


        #region (private) AddProofCommands(WebSocketServer, History)

        /// <summary>
        /// The actions and the topic of attestations and proofs of the WebSocket of the given history.
        /// </summary>
        private void AddProofCommands(POIWebSocketServer     WebSocketServer,
                                      RoamingNetworkHistory  History)
        {

            WebSocketServer.AddCommand("attestations", request =>

                Task.FromResult(
                    request.Respond(new JObject(
                                        new JProperty(PropertyKeys.Attestations, new JArray(History.CurrentAttestations(TimeProvider.GetUtcNow()).Select(attestation => attestation.ToJSON(Embedded: true))))
                                    ))
                )

            );

            WebSocketServer.AddCommand("chargingStationProof", request => {

                ETagHashAlgorithm? hashAlgorithm = null;

                if (request.Payload[PropertyKeys.HashAlgorithm] is JToken hashAlgorithmJSON)
                {

                    if (hashAlgorithmJSON.Type != JTokenType.String || !ETagHashAlgorithmExtensions.TryParse(hashAlgorithmJSON.Value<String>(), out var parsed))
                        return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "Unknown hash algorithm: sha256, sha384 or sha512!"));

                    hashAlgorithm = parsed;

                }

                if (request.Payload[PropertyKeys.ChargingStationId] is JToken chargingStationIdJSON)
                    return Task.FromResult(
                               chargingStationIdJSON.Type != JTokenType.String || !ChargingStation_Id.TryParse(chargingStationIdJSON.Value<String>() ?? "", out var chargingStationId)
                                   ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Invalid charging station identification!")
                                   : History.TryGetChargingStationProof(chargingStationId, out var proof, out var proofError, hashAlgorithm, TimeProvider.GetUtcNow())
                                         ? request.Respond(proof.ToJSON())
                                         : request.Error(POIWebSocketErrorCodes.NotFound, proofError)
                           );

                if (request.Payload[PropertyKeys.EVSEId] is JToken evseIdJSON)
                    return Task.FromResult(
                               evseIdJSON.Type != JTokenType.String || !EVSE_Id.TryParse(evseIdJSON.Value<String>() ?? "", out var evseId)
                                   ? request.Error(POIWebSocketErrorCodes.FormatViolation, "Invalid EVSE identification!")
                                   : History.TryGetChargingStationProof(evseId, out var proof, out var proofError, hashAlgorithm, TimeProvider.GetUtcNow())
                                         ? request.Respond(proof.ToJSON())
                                         : request.Error(POIWebSocketErrorCodes.NotFound, proofError)
                           );

                return Task.FromResult(request.Error(POIWebSocketErrorCodes.FormatViolation, "The request must name a chargingStationId or an EVSEId!"));

            });

            WebSocketServer.AddTopic(new POIWebSocketTopic(AttestationsTopic,
                                                           "attestation",
                                                           "Every attestation of the head: a signed, short-lived statement of its effectiveTree ETag, which proofs of charging stations are checked against."));

        }

        #endregion

        #region (private static) ConnectAttestations(History, WebSocketServer)

        /// <summary>
        /// Push the attestations of the given history to the subscribers; returns how to stop it.
        /// </summary>
        private static Action ConnectAttestations(RoamingNetworkHistory  History,
                                                  POIWebSocketServer     WebSocketServer)
        {

            OnRoamingNetworkAttestationDelegate onAttestation = attestation =>
                WebSocketServer.Push(AttestationsTopic, attestation.ToJSON());

            History.OnAttestation += onAttestation;

            return () => History.OnAttestation -= onAttestation;

        }

        #endregion

    }

}
