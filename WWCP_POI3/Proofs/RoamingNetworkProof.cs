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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A proof of entities of a roaming network (SPECIFICATION.md, section 8.4): the roaming network in its effective
    /// content, pruned, each child array complete, pruned or partial, and the attestations of its root it was made for.
    /// A verifier calculates the root from it, and checks it against an attestation it trusts, or against the ETags of a
    /// commit a checkpoint proves (section 10.4). Everything the proof gives completely is then attested, nothing else.
    /// </summary>
    /// <remarks>
    /// Read it from text with TryParse(Text, …): numbers with a fraction must stay decimals and strings must stay strings,
    /// never points in time, as the digests are calculated over their canonical JSON again.
    /// </remarks>
    public sealed class RoamingNetworkProof
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of a proof.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/proof/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context         = "@context";

            /// <summary>
            /// The property of the hash algorithm of its tree.
            /// </summary>
            public const String  HashAlgorithm   = "hashAlgorithm";

            /// <summary>
            /// The property of the identification of the commit of the proven version.
            /// </summary>
            public const String  CommitId        = "commitId";

            /// <summary>
            /// The property of the revision of the commit.
            /// </summary>
            public const String  Revision        = "revision";

            /// <summary>
            /// The property of the pruned roaming network.
            /// </summary>
            public const String  RoamingNetwork  = "roamingNetwork";

            /// <summary>
            /// The property of the attestations of its root.
            /// </summary>
            public const String  Attestations    = "attestations";

        }

        #endregion


        /// <summary>
        /// The hash algorithm of the tree.
        /// </summary>
        public ETagHashAlgorithm                          HashAlgorithm     { get; }

        /// <summary>
        /// The roaming network, pruned. Keep it as it is: it is the proof.
        /// </summary>
        public JObject                                    RoamingNetwork    { get; }

        /// <summary>
        /// The commit of the proven version, if the proof names it.
        /// </summary>
        public RoamingNetworkCommit_Id?                   CommitId          { get; }

        /// <summary>
        /// The revision of the commit, if the proof names it.
        /// </summary>
        public UInt64?                                    Revision          { get; }

        /// <summary>
        /// The attestations of its root it was made for.
        /// </summary>
        public ImmutableArray<RoamingNetworkAttestation>  Attestations      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a proof of its parts.
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm of the tree.</param>
        /// <param name="RoamingNetwork">The roaming network, pruned.</param>
        /// <param name="CommitId">The commit of the proven version, if known.</param>
        /// <param name="Revision">The revision of the commit, if known.</param>
        /// <param name="Attestations">The attestations of its root.</param>
        public RoamingNetworkProof(ETagHashAlgorithm                        HashAlgorithm,
                                   JObject                                  RoamingNetwork,
                                   RoamingNetworkCommit_Id?                 CommitId       = null,
                                   UInt64?                                  Revision       = null,
                                   IEnumerable<RoamingNetworkAttestation>?  Attestations   = null)
        {

            this.HashAlgorithm   = HashAlgorithm;
            this.RoamingNetwork  = RoamingNetwork;
            this.CommitId        = CommitId;
            this.Revision        = Revision;
            this.Attestations    = [ .. Attestations ?? [] ];

        }

        #endregion


        #region TryCalculateETag(out ETag, out ErrorResponse)

        /// <summary>
        /// Try to calculate the root this proof leads to, bottom-up: the effectiveTree ETag of the version it proves,
        /// unless it is no proof of any.
        /// </summary>
        /// <param name="ETag">The effectiveTree ETag it leads to.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryCalculateETag(out ETag                          ETag,
                                        [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ETag = default;

            if (!POIEffectiveTree.TryDigest((JObject) RoamingNetwork.DeepClone(),
                                            POIEffectiveTree.RoamingNetworkKind,
                                            HashAlgorithm,
                                            out var digest,
                                            out ErrorResponse))
            {
                return false;
            }

            ETag = new ETag(ETagFormat.EffectiveTree, HashAlgorithm, digest);
            return true;

        }

        #endregion

        #region Verify(VerifyAttestation, out Attestation, out ErrorResponse, At = null)

        /// <summary>
        /// Verify this proof against its attestations: one of them must be valid at the given time, now by default, its
        /// signature verified by the given delegate with a key trusted independently of the network, and it must attest
        /// the root this proof leads to, or the digest of an entity it gives, e.g. of a charging station operator.
        /// </summary>
        /// <param name="VerifyAttestation">Verifies the signature of an attestation with a trusted key.</param>
        /// <param name="Attestation">The attestation it was verified against.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When it must be valid, now by default.</param>
        public Boolean Verify(VerifyAttestationDelegate                             VerifyAttestation,
                              [NotNullWhen(true)]  out RoamingNetworkAttestation?   Attestation,
                              [NotNullWhen(false)] out String?                      ErrorResponse,
                              DateTimeOffset?                                       At   = null)
        {

            Attestation = null;

            if (!TryCalculateETag(out var eTag, out ErrorResponse))
                return false;

            Attestation = AttestationsOf(eTag, At ?? Timestamp.Now).FirstOrDefault(attestation => VerifyAttestation(attestation));

            if (Attestation is not null)
            {
                ErrorResponse = null;
                return true;
            }

            ErrorResponse = $"No valid attestation of a trusted key attests the root {eTag} of the proof!";
            return false;

        }

        #endregion

        #region (private) AttestationsOf(ETag, At)

        /// <summary>
        /// The attestations of this proof of its roaming network and commit, valid at the given time, that attest the given
        /// root this proof leads to, or the digest of an entity it gives: those to verify.
        /// </summary>
        private IEnumerable<RoamingNetworkAttestation> AttestationsOf(ETag            ETag,
                                                                       DateTimeOffset  At)
        {

            var roamingNetworkId = RoamingNetwork[POI.RoamingNetwork.BasePropertyKeys.Id]?.Type == JTokenType.String ? RoamingNetwork[POI.RoamingNetwork.BasePropertyKeys.Id]!.Value<String>() : null;

            foreach (var attestation in Attestations)
            {

                if (attestation.RoamingNetworkId.ToString() != roamingNetworkId                ||
                    (CommitId.HasValue && attestation.CommitId != CommitId.Value)              ||
                    attestation.ETag.HashAlgorithm != HashAlgorithm                            ||
                    !attestation.IsValidAt(At))
                {
                    continue;
                }

                if (attestation.Subject is null
                        ? !attestation.ETag.Equals(ETag)
                        : !AttestsSubject(attestation.Subject))
                {
                    continue;
                }

                yield return attestation;

            }

        }

        #endregion

        #region Verify(TrustAnchor, out Attestation, out ErrorResponse, At = null)

        /// <summary>
        /// Verify this proof against the given trust anchor of its roaming network, e.g. of a QR code a verifier scanned:
        /// against the identity keys of the roaming network operator it names, with its minConsensus.
        /// </summary>
        /// <param name="TrustAnchor">The trust anchor.</param>
        /// <param name="Attestation">One of the attestations it was verified against.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When it must be valid, now by default.</param>
        public Boolean Verify(RoamingNetworkTrustAnchor                             TrustAnchor,
                              [NotNullWhen(true)]  out RoamingNetworkAttestation?   Attestation,
                              [NotNullWhen(false)] out String?                      ErrorResponse,
                              DateTimeOffset?                                       At   = null)
        {

            if (TrustAnchor.Id.ToString() != RoamingNetwork[POI.RoamingNetwork.BasePropertyKeys.Id]?.Value<String>())
            {
                Attestation    = null;
                ErrorResponse  = $"The trust anchor is one of the roaming network '{TrustAnchor.Id}'!";
                return false;
            }

            return Verify(TrustAnchor.Keys, out Attestation, out ErrorResponse, At, TrustAnchor.MinConsensus);

        }

        #endregion

        #region Verify(TrustedIdentityKeyIds, out Attestation, out ErrorResponse, At = null, MinConsensus = 1)

        /// <summary>
        /// Verify this proof against the identity keys of the roaming network operator the verifier trusts by their @ids,
        /// e.g. those of a QR code it scanned (RoamingNetworkTrustAnchor): the key statement of the operator the proof gives
        /// must be signed by trusted identity keys of as many holders as the given minConsensus and its own require, and be
        /// valid at the given time, now by default; and the attestations of the proof signed by signing keys it states of
        /// as many holders, valid when they were signed (SPECIFICATION.md, sections 4.1 and 8.6): so that no holder of a
        /// quorum attests a version alone.
        /// </summary>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys the verifier trusts: their SHA-256 thumbprints.</param>
        /// <param name="Attestation">One of the attestations it was verified against.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When it must be valid, now by default.</param>
        /// <param name="MinConsensus">How many holders must have signed the key statement and attested the root at least, e.g. as a trust anchor names it.</param>
        public Boolean Verify(IEnumerable<String>                                   TrustedIdentityKeyIds,
                              [NotNullWhen(true)]  out RoamingNetworkAttestation?   Attestation,
                              [NotNullWhen(false)] out String?                      ErrorResponse,
                              DateTimeOffset?                                       At             = null,
                              Byte                                                  MinConsensus   = 1)
        {

            Attestation = null;

            if (!TryGetKeyStatement(out var keyStatement, out var operatorIdentityKeys, out ErrorResponse))
                return false;

            var at = At ?? Timestamp.Now;

            if (!keyStatement.TryVerify(TrustedIdentityKeyIds, operatorIdentityKeys, out ErrorResponse, MinConsensus, at))
                return false;

            if (keyStatement.RoamingNetworkId.ToString() != RoamingNetwork[POI.RoamingNetwork.BasePropertyKeys.Id]?.Value<String>())
            {
                ErrorResponse = $"The key statement is one of the roaming network '{keyStatement.RoamingNetworkId}'!";
                return false;
            }

            var required = Math.Max(MinConsensus, keyStatement.MinConsensus);

            if (required == 1)
                return Verify(attestation => keyStatement.SigningKeyOf(attestation.KeyId, attestation.SignedAt) is POIPublicKey signingKey &&
                                             attestation.VerifySignature(signingKey.Key.ToPublicKey(), signingKey.Id, out _),
                              out Attestation,
                              out ErrorResponse,
                              at);

            #region Attested by as many holders of the signing keys it states

            if (!TryCalculateETag(out var eTag, out ErrorResponse))
                return false;

            var holders = new HashSet<String>(StringComparer.Ordinal);

            foreach (var attestation in AttestationsOf(eTag, at))
            {
                if (keyStatement.SigningKeyOf(attestation.KeyId, attestation.SignedAt) is POIPublicKey signingKey &&
                    attestation.VerifySignature(signingKey.Key.ToPublicKey(), signingKey.Id, out _))
                {
                    Attestation ??= attestation;
                    holders.Add(signingKey.HolderOrUnnamed);
                }
            }

            if (holders.Count >= required)
            {
                ErrorResponse = null;
                return true;
            }

            Attestation    = null;
            ErrorResponse  = $"Valid attestations of trusted keys of {holders.Count} holder{(holders.Count == 1 ? "" : "s")} attest the root {eTag} of the proof, not of {required}!";
            return false;

            #endregion

        }

        #endregion

        #region TryGetKeyStatement(out KeyStatement, out IdentityKeys, out ErrorResponse)

        /// <summary>
        /// The key statement of the roaming network operator this proof gives, and the identity keys of the operator.
        /// </summary>
        public Boolean TryGetKeyStatement([NotNullWhen(true)]  out RoamingNetworkKeyStatement?  KeyStatement,
                                          out IEnumerable<POIPublicKey>                         IdentityKeys,
                                          [NotNullWhen(false)] out String?                      ErrorResponse)
        {

            KeyStatement  = null;
            IdentityKeys  = [];

            if (RoamingNetwork[POI.RoamingNetwork.PropertyKeys.Operator] is not JObject operatorJSON ||
                operatorJSON[RoamingNetworkOperator.PropertyKeys.KeyStatement] is not JObject keyStatementJSON)
            {
                ErrorResponse = "The proof gives no key statement of the roaming network operator!";
                return false;
            }

            if (!POIPartyKeys.TryParse(operatorJSON, out _, out var identityKeys, out ErrorResponse) ||
                !RoamingNetworkKeyStatement.TryParse(keyStatementJSON, out KeyStatement, out ErrorResponse))
            {
                return false;
            }

            IdentityKeys = identityKeys;
            return true;

        }

        #endregion

        #region Verify(ETag, out ErrorResponse)

        /// <summary>
        /// Verify this proof against the given effectiveTree ETag, e.g. one of the ETags of a commit a checkpoint proves
        /// (SPECIFICATION.md, section 10.4).
        /// </summary>
        /// <param name="ETag">The effectiveTree ETag of the version it must prove.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean Verify(ETag                              ETag,
                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (!TryCalculateETag(out var eTag, out ErrorResponse))
                return false;

            if (!eTag.Equals(ETag))
            {
                ErrorResponse = $"The proof leads to {eTag}, not to {ETag}!";
                return false;
            }

            return true;

        }

        #endregion

        #region Given(ChildArrays)

        /// <summary>
        /// The entities this proof gives along the given child arrays from the roaming network on, completely or as leaves of
        /// partial arrays, e.g. Given("chargingStationOperators", "chargingPools", "chargingStations") for the charging
        /// stations it gives. Whether they are attested, Verify tells.
        /// </summary>
        /// <param name="ChildArrays">The names of child arrays, one per level.</param>
        public IEnumerable<JObject> Given(params String[] ChildArrays)
        {

            IEnumerable<JObject> entities = [ RoamingNetwork ];

            foreach (var childArray in ChildArrays)
                entities = entities.SelectMany(entity => Children(entity, childArray)).ToArray();

            return entities;

        }

        #endregion

        #region GivenBelow(Entity, ChildArrays)

        /// <summary>
        /// The entities this proof gives along the given child arrays from the given entity of it on, e.g.
        /// GivenBelow(chargingStationOperator, "chargingPools", "chargingStations") for the charging stations of one
        /// charging station operator it gives.
        /// </summary>
        /// <param name="Entity">An entity this proof gives.</param>
        /// <param name="ChildArrays">The names of child arrays, one per level.</param>
        public IEnumerable<JObject> GivenBelow(JObject          Entity,
                                               params String[]  ChildArrays)
        {

            IEnumerable<JObject> entities = [ Entity ];

            foreach (var childArray in ChildArrays)
                entities = entities.SelectMany(entity => Children(entity, childArray)).ToArray();

            return entities;

        }

        #endregion


        #region (static) TryParse(Text, out Proof, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of a proof, numbers with a fraction as decimals and strings as strings.
        /// </summary>
        public static Boolean TryParse(String                                         Text,
                                       [NotNullWhen(true)]  out RoamingNetworkProof?  Proof,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            Proof = null;

            try
            {

                if (POIJSON.Read(Text) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json, out Proof, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(JSON, out Proof, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a proof, read with numbers with a fraction as decimals and strings
        /// as strings. Whether it leads to a root, TryCalculateETag tells.
        /// </summary>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkProof?  Proof,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            Proof = null;

            try
            {

                // Embedded, a proof is written without its context.
                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of a proof must be '{JSONLDContext}'!";
                    return false;
                }

                if (JSON[PropertyKeys.HashAlgorithm]?.Type != JTokenType.String ||
                    !ETagHashAlgorithmExtensions.TryParse(JSON[PropertyKeys.HashAlgorithm]!.Value<String>(), out var hashAlgorithm))
                {
                    ErrorResponse = "Missing or unknown hash algorithm: sha256, sha384 or sha512!";
                    return false;
                }

                if (JSON[PropertyKeys.RoamingNetwork] is not JObject roamingNetwork)
                {
                    ErrorResponse = "The roaming network must be a JSON object!";
                    return false;
                }

                RoamingNetworkCommit_Id? commitId = null;

                if (JSON[PropertyKeys.CommitId] is JToken commitIdJSON)
                {

                    if (commitIdJSON.Type != JTokenType.String || !RoamingNetworkCommit_Id.TryParse(commitIdJSON.Value<String>() ?? "", out var parsedCommitId))
                    {
                        ErrorResponse = "Invalid commit identification!";
                        return false;
                    }

                    commitId = parsedCommitId;

                }

                UInt64? revision = null;

                if (JSON[PropertyKeys.Revision] is JToken revisionJSON)
                {

                    if (revisionJSON is not JValue { Type: JTokenType.Integer } revisionValue ||
                        revisionValue.Value<Decimal>() < 0 || revisionValue.Value<Decimal>() > UInt64.MaxValue)
                    {
                        ErrorResponse = "Invalid revision!";
                        return false;
                    }

                    revision = revisionValue.Value<UInt64>();

                }

                var attestations = new List<RoamingNetworkAttestation>();

                if (JSON[PropertyKeys.Attestations] is JToken attestationsJSON)
                {

                    if (attestationsJSON is not JArray attestationsArray)
                    {
                        ErrorResponse = "The attestations must be a JSON array!";
                        return false;
                    }

                    foreach (var attestationJSON in attestationsArray)
                    {

                        if (attestationJSON is not JObject attestationObject)
                        {
                            ErrorResponse = $"Invalid attestation {attestations.Count}: it must be a JSON object!";
                            return false;
                        }

                        if (!RoamingNetworkAttestation.TryParse(attestationObject, out var attestation, out var attestationError))
                        {
                            ErrorResponse = $"Invalid attestation {attestations.Count}: {attestationError}";
                            return false;
                        }

                        attestations.Add(attestation);

                    }

                }

                Proof          = new RoamingNetworkProof(hashAlgorithm,
                                                         roamingNetwork,
                                                         commitId,
                                                         revision,
                                                         attestations);

                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out Proof, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a proof: the keys and values of its JSON representation.
        /// </summary>
        public static Boolean TryParse(CBORValue                                      CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkProof?  Proof,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            Proof = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map || CBORJSON.ToJSON(CBOR) is not JObject json)
                {
                    ErrorResponse = "The CBOR representation of a proof must be a map with text keys!";
                    return false;
                }

                return TryParse(json, out Proof, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomProofSerializer = null)

        /// <summary>
        /// Return the JSON representation of this proof.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomProofSerializer">An optional custom serializer of proofs.</param>
        public JObject ToJSON(Boolean                                                Embedded                = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkProof>?  CustomProofSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,        JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.HashAlgorithm,  HashAlgorithm.ToText())

                       );

            if (CommitId.HasValue)
                json.Add(PropertyKeys.CommitId,    CommitId.Value.ToString());

            if (Revision.HasValue)
                json.Add(PropertyKeys.Revision,    Revision.Value);

            json.Add(PropertyKeys.RoamingNetwork,  RoamingNetwork.DeepClone());
            json.Add(PropertyKeys.Attestations,    new JArray(Attestations.Select(attestation => attestation.ToJSON(Embedded: true))));

            return CustomProofSerializer is not null
                       ? CustomProofSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomProofSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this proof: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomProofSerializer">An optional custom serializer of proofs.</param>
        public CBORValue ToCBOR(Boolean                                             Embedded                = false,
                                CustomCBORSerializerDelegate<RoamingNetworkProof>?  CustomProofSerializer   = null)
        {

            var cbor = new CBORMap();

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),         CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(PropertyKeys.HashAlgorithm),       CBORValue.FromText(HashAlgorithm.ToText()));

            if (CommitId.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.CommitId),        CBORValue.FromText(CommitId.Value.ToString()));

            if (Revision.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Revision),        CBORValue.FromUInt64(Revision.Value));

            // The pruned roaming network is the proof as it is given: the keys and values of its JSON.
            cbor.Add(CBORValue.FromText(PropertyKeys.RoamingNetwork),      CBORJSON.ToCBOR(RoamingNetwork));
            cbor.Add(CBORValue.FromText(PropertyKeys.Attestations),        CBORValue.FromArray(Attestations.Select(attestation => attestation.ToCBOR(Embedded: true))));

            return CustomProofSerializer is not null
                       ? CustomProofSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region (private) AttestsSubject(Subject)

        /// <summary>
        /// Whether the entity the given subject names is given by this proof, with the digest it names: a charging
        /// station operator, as its own node of the tree.
        /// </summary>
        private Boolean AttestsSubject(RoamingNetworkAttestationSubject Subject)
        {

            if (Subject.EntityType != nameof(ChargingStationOperator))
                return false;

            foreach (var chargingStationOperator in Children(RoamingNetwork, POI.RoamingNetwork.PropertyKeys.ChargingStationOperators))
                if (chargingStationOperator[POI.RoamingNetwork.BasePropertyKeys.Id]?.Type == JTokenType.String &&
                    chargingStationOperator[POI.RoamingNetwork.BasePropertyKeys.Id]!.Value<String>() == Subject.EntityId &&
                    POIEffectiveTree.TryDigest((JObject) chargingStationOperator.DeepClone(), POI.RoamingNetwork.PropertyKeys.ChargingStationOperators, HashAlgorithm, out var digest, out _) &&
                    Convert.ToHexStringLower(digest) == Subject.Digest)
                {
                    return true;
                }

            return false;

        }

        #endregion

        #region (private static) Children(Entity, ChildArray)

        /// <summary>
        /// The children of the given child array of the given entity this proof gives: all of a complete one, the leaves of
        /// a partial one, none of a pruned one.
        /// </summary>
        private static IEnumerable<JObject> Children(JObject  Entity,
                                                     String   ChildArray)

            => Entity[ChildArray] switch {
                   JArray  complete  => complete.OfType<JObject>(),
                   JObject partialForm => (partialForm[POIEffectiveTree.LeavesProperty] as JArray ?? []).
                                            OfType<JObject>().
                                            Select(leaf => leaf[POIEffectiveTree.EntityProperty] as JObject).
                                            OfType<JObject>(),
                   _                 => []
               };

        #endregion

    }

}
