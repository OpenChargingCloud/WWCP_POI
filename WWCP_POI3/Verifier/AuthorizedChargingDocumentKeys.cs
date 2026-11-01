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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A signing key of a charging station operator authorized for its charge transparency records, normalized for the
    /// shared envelope verifier of Chargy Core: as COSE key, Base64 of its CBOR.
    /// </summary>
    /// <param name="Id">The @id of the key.</param>
    /// <param name="Algorithm">Its algorithm, e.g. "ECDSA-secp256r1" or "EdDSA-Ed25519".</param>
    /// <param name="Value">The COSE key, Base64 of its CBOR.</param>
    public sealed record ChargingDocumentKey(String  Id,
                                             String  Algorithm,
                                             String  Value)
    {

        /// <summary>
        /// The encodings of Value: COSE_Key, CBOR, base64.
        /// </summary>
        public ImmutableArray<String>  Encodings  { get; } = [ "COSE_Key", "CBOR", "base64" ];

        /// <summary>
        /// What the key signs, in the words of the envelope verifier: signCTRs.
        /// </summary>
        public ImmutableArray<String>  KeyUsage   { get; } = [ "signCTRs" ];

    }


    /// <summary>
    /// The signing keys of the charging station operator of an EVSE authorized for its charge transparency records at the
    /// time of a document (WWCP_POI.TS verifyChargingDocumentKeys): of the original proof of the station, of the exact
    /// operator, valid then and naming "chargeTransparencyRecords" or no usage. A key only for price statements does not
    /// sign documents, and no key a document supplies itself is an authority.
    /// </summary>
    /// <param name="EVSEId">The EVSE.</param>
    /// <param name="ChargingStationId">Its charging station.</param>
    /// <param name="OperatorId">Its charging station operator.</param>
    /// <param name="At">The time of the document.</param>
    /// <param name="CommitId">The commit of the version of the proof.</param>
    /// <param name="Revision">The revision of the version of the proof.</param>
    /// <param name="Keys">The keys.</param>
    public sealed record AuthorizedChargingDocumentKeys(String                               EVSEId,
                                                        String                               ChargingStationId,
                                                        String                               OperatorId,
                                                        DateTimeOffset                       At,
                                                        RoamingNetworkCommit_Id?             CommitId,
                                                        UInt64?                              Revision,
                                                        ImmutableArray<ChargingDocumentKey>  Keys)
    {

        #region (static) TryVerify(Current, Proof, EVSEId, OperatorId, At, out Keys, out ErrorResponse)

        /// <summary>
        /// Verify the given original proof of the station of the given EVSE under the given local authorization, with the
        /// roaming network operator it gives at the given time of a document, and give the signing keys of the given
        /// operator of the EVSE authorized for charge transparency records then.
        /// </summary>
        /// <param name="Current">The current local authorization.</param>
        /// <param name="Proof">The original proof of the charging station.</param>
        /// <param name="EVSEId">The EVSE.</param>
        /// <param name="OperatorId">The charging station operator the document names.</param>
        /// <param name="At">The time of the document.</param>
        /// <param name="Keys">The authorized keys.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryVerify(IRoamingNetworkTrustAuthority                             Current,
                                        RoamingNetworkProof                                       Proof,
                                        String                                                    EVSEId,
                                        String                                                    OperatorId,
                                        DateTimeOffset                                            At,
                                        [NotNullWhen(true)]  out AuthorizedChargingDocumentKeys?  Keys,
                                        [NotNullWhen(false)] out String?                          ErrorResponse)
        {

            Keys = null;

            if (At > Current.EvaluatedAt)
            {
                ErrorResponse = "Invalid charging-document evaluation time!";
                return false;
            }

            if (!HistoricalTrustContext.TryGetOperatorContext(Current, Proof.RoamingNetwork[RoamingNetwork.PropertyKeys.Operator], At, out var historical, out ErrorResponse) ||
                !VerifiedChargingStation.TryVerify(historical, Proof, out var station, out ErrorResponse, EVSEId: EVSEId))
            {
                return false;
            }

            if (VerifiedChargingStation.IdOf(station.Operator) != OperatorId)
            {
                ErrorResponse = "The live document CPO is not the operator of the proven EVSE!";
                return false;
            }

            var keys = new List<ChargingDocumentKey>();

            foreach (var key in station.OperatorSigningKeys.Where(key => key.IsValidAt(At) && key.MaySign(POIKeyUsages.ChargeTransparencyRecords)))
            {

                var algorithm = key.Key.Curve is COSECurve curve
                                    ? AlgorithmOf(curve)
                                    : key.Key.Algorithm?.Name;

                if (algorithm is null)
                {
                    ErrorResponse = "Unsupported CPO charging-document key algorithm!";
                    return false;
                }

                keys.Add(new ChargingDocumentKey(key.Id, algorithm, Convert.ToBase64String(key.Key.ToByteArray())));

            }

            if (keys.Count == 0)
            {
                ErrorResponse = "The proven CPO supplies no valid keys authorized for charge transparency records!";
                return false;
            }

            Keys = new AuthorizedChargingDocumentKeys(EVSEId,
                                                      VerifiedChargingStation.IdOf(station.ChargingStation),
                                                      OperatorId,
                                                      At,
                                                      station.CommitId,
                                                      station.Revision,
                                                      [.. keys]);

            return true;

        }

        #endregion

        #region (private static) AlgorithmOf(Curve)

        /// <summary>
        /// The algorithm of a key of the given curve, as the envelope verifier names it.
        /// </summary>
        private static String? AlgorithmOf(COSECurve Curve)

            => Curve.Value switch {
                   1 => "ECDSA-secp256r1",
                   2 => "ECDSA-secp384r1",
                   3 => "ECDSA-secp521r1",
                   6 => "EdDSA-Ed25519",
                   7 => "EdDSA-Ed448",
                   8 => "ECDSA-secp256k1",
                   _ => null
               };

        #endregion

    }

}
