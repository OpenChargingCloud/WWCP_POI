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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The attestations of a history and the proofs of its head (SPECIFICATION.md, sections 8.4 and 8.5): signed, short-lived
    /// statements of the effectiveTree ETags of the head, made here by the AttestationSigners whenever the head changes
    /// and again by RefreshAttestations(…), or taken from another server, e.g. of the roaming network operator; and proofs
    /// of charging stations of the head, made for the attestations of it.
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        /// <summary>
        /// The most attestations a history keeps.
        /// </summary>
        public const Int32 MaxAttestations = 1000;

        private readonly List<RoamingNetworkAttestation>                                 attestations     = [];
        private readonly Lock                                                            treeLock         = new ();
        private readonly Dictionary<ETagHashAlgorithm, RoamingNetworkEffectiveTree>      effectiveTrees   = [];

        #endregion

        #region Properties

        /// <summary>
        /// The keys that attest the head here whenever it changes, e.g. the signing key of the roaming network operator.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommitSigner>  AttestationSigners    { get; set; } = [];

        /// <summary>
        /// How long an attestation made here is valid: 15 minutes by default.
        /// </summary>
        public TimeSpan                                    AttestationValidity   { get; set; } = TimeSpan.FromMinutes(15);

        /// <summary>
        /// How old the newest attestation of a signer may be before RefreshAttestations(…) attests the head again:
        /// 5 minutes by default.
        /// </summary>
        public TimeSpan                                    AttestationInterval   { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The attestations of the head valid now.
        /// </summary>
        public IReadOnlyList<RoamingNetworkAttestation>    Attestations
            => CurrentAttestations();

        #endregion

        #region Events

        /// <summary>
        /// Called for every attestation this history takes, made here or taken from another server,
        /// while the history is locked: return quickly and do not change the history from there.
        /// </summary>
        public event OnRoamingNetworkAttestationDelegate?  OnAttestation;

        #endregion


        #region CurrentAttestations(At = null)

        /// <summary>
        /// The attestations of the head valid at the given time, now by default, in the order they were taken.
        /// </summary>
        /// <param name="At">When they must be valid, now by default.</param>
        public IReadOnlyList<RoamingNetworkAttestation> CurrentAttestations(DateTimeOffset? At = null)
        {

            var at = At ?? Timestamp.Now;

            lock (historyLock)
            {
                return [ .. attestations.Where(attestation => attestation.CommitId == head.Commit.Id && attestation.IsValidAt(at)) ];
            }

        }

        #endregion

        #region TryAttest(out Attestations, out ErrorResponse, At = null)

        /// <summary>
        /// Attest the head with every AttestationSigner, one attestation per effectiveTree ETag of its version.
        /// </summary>
        /// <param name="Attestations">The new attestations.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When they are signed, now by default.</param>
        public Boolean TryAttest([NotNullWhen(true)]  out IReadOnlyList<RoamingNetworkAttestation>?  Attestations,
                                 [NotNullWhen(false)] out String?                                    ErrorResponse,
                                 DateTimeOffset?                                                     At   = null)
        {

            Attestations = null;

            var signers = AttestationSigners;

            if (signers.IsDefaultOrEmpty)
            {
                ErrorResponse = "This history has no attestation signers!";
                return false;
            }

            lock (historyLock)
            {

                try
                {
                    Attestations   = AttestHead(signers, At ?? Timestamp.Now);
                    ErrorResponse  = null;
                    return true;
                }
                catch (Exception e)
                {
                    ErrorResponse  = "The head can not be attested: " + e.Message;
                    return false;
                }

            }

        }

        #endregion

        #region RefreshAttestations(At = null)

        /// <summary>
        /// Attest the head again with every AttestationSigner whose newest attestation of it is older than the
        /// AttestationInterval, or that has none: call it regularly, e.g. every minute, so that the head stays attested
        /// without new commits as well.
        /// </summary>
        /// <param name="At">The time, now by default.</param>
        /// <returns>The new attestations.</returns>
        public IReadOnlyList<RoamingNetworkAttestation> RefreshAttestations(DateTimeOffset? At = null)
        {

            var at = At ?? Timestamp.Now;

            lock (historyLock)
            {

                var stale = AttestationSigners.IsDefaultOrEmpty
                                ? []
                                : AttestationSigners.Where(signer => !attestations.Any(attestation => attestation.CommitId == head.Commit.Id  &&
                                                                                                      attestation.KeyId    == signer.KeyId    &&
                                                                                                      attestation.Role     == signer.Role     &&
                                                                                                      attestation.IsValidAt(at)               &&
                                                                                                      at - attestation.SignedAt < AttestationInterval)).
                                                     ToImmutableArray();

                return stale.IsEmpty
                           ? []
                           : AttestHead(stale, at);

            }

        }

        #endregion

        #region TryAddAttestation(Attestation, VerifyAttestation, out ErrorResponse, At = null)

        /// <summary>
        /// Take an attestation of another server, e.g. of the roaming network operator, of a commit this history holds:
        /// its effectiveTree ETag must be one of the version of the commit, and its signature verified by the given
        /// delegate with a trusted key. One taken before is taken again without error.
        /// </summary>
        /// <param name="Attestation">The attestation.</param>
        /// <param name="VerifyAttestation">Verifies its signature with a trusted key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When it must be valid, now by default.</param>
        public Boolean TryAddAttestation(RoamingNetworkAttestation         Attestation,
                                         VerifyAttestationDelegate         VerifyAttestation,
                                         [NotNullWhen(false)] out String?  ErrorResponse,
                                         DateTimeOffset?                   At   = null)
        {

            if (Attestation.RoamingNetworkId != RoamingNetworkId)
            {
                ErrorResponse = $"The attestation is of the roaming network '{Attestation.RoamingNetworkId}', not of '{RoamingNetworkId}'!";
                return false;
            }

            if (!Attestation.IsValidAt(At ?? Timestamp.Now))
            {
                ErrorResponse = "The attestation is no longer valid!";
                return false;
            }

            if (!VerifyAttestation(Attestation))
            {
                ErrorResponse = "The signature of the attestation is not one of a trusted key!";
                return false;
            }

            lock (historyLock)
            {

                if (!commitIndex.TryGetValue(Attestation.CommitId, out var index))
                {
                    ErrorResponse = $"The commit '{Attestation.CommitId}' of the attestation is unknown here!";
                    return false;
                }

                var commit = commits[index];

                if (commit.Revision != Attestation.Revision)
                {
                    ErrorResponse = $"The commit '{commit.Id}' has the revision {commit.Revision}, not {Attestation.Revision}!";
                    return false;
                }

                // The ETags of the commit tell it.
                var eTag = commit.ETags.FirstOrDefault(eTag => eTag.Format == ETagFormat.EffectiveTree && eTag.HashAlgorithm == Attestation.ETag.HashAlgorithm);

                if (!eTag.IsValid || !eTag.Equals(Attestation.ETag))
                {
                    ErrorResponse = $"The version of the commit '{commit.Id}' does not have the ETag {Attestation.ETag}!";
                    return false;
                }

                if (Attestation.Subject is not null)
                {
                    ErrorResponse = "Attestations of single entities are not taken by a history!";
                    return false;
                }

                if (!attestations.Any(taken => taken.Value == Attestation.Value && taken.KeyId == Attestation.KeyId))
                    TakeAttestation(Attestation);

            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region TryGetChargingStationProof(ChargingStationId, out Proof, out ErrorResponse, HashAlgorithm = null, At = null)

        /// <summary>
        /// Try to make the proof of the given charging station of the head (SPECIFICATION.md, section 8.4), with the tree of
        /// the given hash algorithm, the first one of the roaming network by default, for the attestations of the head valid
        /// at the given time. The tree of the head is kept until the head changes.
        /// </summary>
        /// <param name="ChargingStationId">The identification of a charging station.</param>
        /// <param name="Proof">The proof.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="HashAlgorithm">One of the tree hash algorithms of the roaming network, its first one by default.</param>
        /// <param name="At">When the attestations must be valid, now by default.</param>
        public Boolean TryGetChargingStationProof(ChargingStation_Id                             ChargingStationId,
                                                  [NotNullWhen(true)]  out RoamingNetworkProof?  Proof,
                                                  [NotNullWhen(false)] out String?               ErrorResponse,
                                                  ETagHashAlgorithm?                             HashAlgorithm   = null,
                                                  DateTimeOffset?                                At              = null)
        {

            Proof = null;

            var head = Head;

            if (!TryGetEffectiveTree(head.RoamingNetwork, HashAlgorithm, out var tree, out ErrorResponse))
                return false;

            return tree.TryGetChargingStationProof(ChargingStationId,
                                                   out Proof,
                                                   out ErrorResponse,
                                                   head.Commit.Id,
                                                   head.Commit.Revision,
                                                   AttestationsOf(head.Commit.Id, At));

        }

        #endregion

        #region TryGetChargingStationProof(EVSEId,            out Proof, out ErrorResponse, HashAlgorithm = null, At = null)

        /// <summary>
        /// Try to make the proof of the charging station of the given EVSE of the head.
        /// </summary>
        /// <param name="EVSEId">The identification of an EVSE.</param>
        /// <param name="Proof">The proof.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="HashAlgorithm">One of the tree hash algorithms of the roaming network, its first one by default.</param>
        /// <param name="At">When the attestations must be valid, now by default.</param>
        public Boolean TryGetChargingStationProof(EVSE_Id                                        EVSEId,
                                                  [NotNullWhen(true)]  out RoamingNetworkProof?  Proof,
                                                  [NotNullWhen(false)] out String?               ErrorResponse,
                                                  ETagHashAlgorithm?                             HashAlgorithm   = null,
                                                  DateTimeOffset?                                At              = null)
        {

            Proof = null;

            var head = Head;

            if (!TryGetEffectiveTree(head.RoamingNetwork, HashAlgorithm, out var tree, out ErrorResponse))
                return false;

            return tree.TryGetChargingStationProof(EVSEId,
                                                   out Proof,
                                                   out ErrorResponse,
                                                   head.Commit.Id,
                                                   head.Commit.Revision,
                                                   AttestationsOf(head.Commit.Id, At));

        }

        #endregion


        #region (private) AttestHead(Signers, At)

        /// <summary>
        /// Attest the head with the given signers, while the history is locked: one attestation per effectiveTree ETag of
        /// its commit.
        /// </summary>
        private IReadOnlyList<RoamingNetworkAttestation> AttestHead(ImmutableArray<RoamingNetworkCommitSigner>  Signers,
                                                                    DateTimeOffset                              At)
        {

            var commit  = head.Commit;
            var eTags   = commit.ETags.Where(eTag => eTag.Format == ETagFormat.EffectiveTree).ToArray();

            var made    = new List<RoamingNetworkAttestation>();

            foreach (var signer in Signers)
                foreach (var eTag in eTags)
                {

                    var attestation = signer.Attest(RoamingNetworkId,
                                                    commit.Id,
                                                    commit.Revision,
                                                    eTag,
                                                    At,
                                                    At + AttestationValidity);

                    TakeAttestation(attestation);
                    made.Add(attestation);

                }

            return made;

        }

        #endregion

        #region (private) AttestNewHead()

        /// <summary>
        /// Attest the head, which just changed, with the AttestationSigners, while the history is locked. Failing to attest
        /// never fails a commit.
        /// </summary>
        private void AttestNewHead()
        {

            var signers = AttestationSigners;

            if (signers.IsDefaultOrEmpty)
                return;

            try
            {
                AttestHead(signers, Timestamp.Now);
            }
            catch
            { }

        }

        #endregion

        #region (private) TakeAttestation(Attestation)

        /// <summary>
        /// Take the given attestation, while the history is locked: keep it, drop those no longer valid and those of commits
        /// no longer held, and announce it.
        /// </summary>
        private void TakeAttestation(RoamingNetworkAttestation Attestation)
        {

            var now = Timestamp.Now;

            attestations.RemoveAll(taken => !taken.IsValidAt(now) || !commitIndex.ContainsKey(taken.CommitId));
            attestations.Add(Attestation);

            if (attestations.Count > MaxAttestations)
                attestations.RemoveRange(0, attestations.Count - MaxAttestations);

            // One failing subscriber must neither fail the attestation nor keep it from the others.
            foreach (var subscriber in OnAttestation?.GetInvocationList().Cast<OnRoamingNetworkAttestationDelegate>() ?? [])
            {
                try
                {
                    subscriber(Attestation);
                }
                catch
                { }
            }

        }

        #endregion

        #region (private) AttestationsOf(CommitId, At)

        /// <summary>
        /// The attestations of the given commit valid at the given time, now by default.
        /// </summary>
        private IReadOnlyList<RoamingNetworkAttestation> AttestationsOf(RoamingNetworkCommit_Id  CommitId,
                                                                        DateTimeOffset?          At)
        {

            var at = At ?? Timestamp.Now;

            lock (historyLock)
            {
                return [ .. attestations.Where(attestation => attestation.CommitId == CommitId && attestation.IsValidAt(at)) ];
            }

        }

        #endregion

        #region (private) TryGetEffectiveTree(RoamingNetwork, HashAlgorithm, out Tree, out ErrorResponse)

        /// <summary>
        /// The tree of the given version with the given hash algorithm, one of the tree hash algorithms of the version, its
        /// first one by default: kept for the head, calculated again when the head changed.
        /// </summary>
        private Boolean TryGetEffectiveTree(RoamingNetwork                                         RoamingNetwork,
                                            ETagHashAlgorithm?                                     HashAlgorithm,
                                            [NotNullWhen(true)]  out RoamingNetworkEffectiveTree?  Tree,
                                            [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            Tree = null;

            var hashAlgorithm = HashAlgorithm ?? RoamingNetwork.TreeHashAlgorithms[0];

            if (!RoamingNetwork.TreeHashAlgorithms.Contains(hashAlgorithm))
            {
                ErrorResponse = $"The roaming network '{RoamingNetwork.Id}' has no tree of {hashAlgorithm.ToText()}, but of {String.Join(", ", RoamingNetwork.TreeHashAlgorithms.Select(algorithm => algorithm.ToText()))}!";
                return false;
            }

            lock (treeLock)
            {

                if (!effectiveTrees.TryGetValue(hashAlgorithm, out Tree) || !ReferenceEquals(Tree.RoamingNetwork, RoamingNetwork))
                {

                    try
                    {
                        Tree = RoamingNetwork.EffectiveTree(hashAlgorithm);
                    }
                    catch (Exception e)
                    {
                        ErrorResponse = e.Message;
                        return false;
                    }

                    effectiveTrees[hashAlgorithm] = Tree;

                }

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
