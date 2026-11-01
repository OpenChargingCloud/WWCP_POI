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

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The experimental compact trust succession and the charging document keys of a CPO, as WWCP_POI.TS 0.1.2-dev.0
    /// verifies them (tests/trustTransitions.test.ts), of its vector trust-transitions.json; the local journal of observed
    /// versions is the one of an app and stays there.
    /// </summary>
    [TestFixture]
    public class TrustTransitionTests
    {

        #region Data

        private const String EVSEId = "DE*POI*E1*1";

        private JObject                     data       = null!;
        private RoamingNetworkTrustAnchor   anchor     = null!;
        private JObject                     @operator  = null!;
        private RoamingNetworkKeyStatement  statement  = null!;
        private DateTimeOffset              now;

        #endregion

        [SetUp]
        public void SetUp()
        {

            data       = TSVectors.Read("trust-transitions");
            now        = DateTimeOffset.Parse(data["now"]!.Value<String>()!);
            @operator  = (JObject) data["operator"]!;

            Assert.That(RoamingNetworkTrustAnchor. TryParse(data["anchor"]!.Value<String>()!,         out var parsedAnchor,    out var error),  Is.True, error);
            Assert.That(RoamingNetworkKeyStatement.TryParse((JObject) @operator["keyStatement"]!,     out var parsedStatement, out error),      Is.True, error);

            anchor     = parsedAnchor!;
            statement  = parsedStatement!;

        }

        private RoamingNetworkTrustContext Context()

            => TSVectors.ContextOf(anchor, statement, now);

        private static TrustTransition Parse(JToken JSON)

            => TrustTransition.TryParse(JSON, out var transition, out var error)
                   ? transition
                   : throw new ArgumentException(error);

        private Boolean TryVerify(JToken JSON, out VerifiedTrustTransition? Verified, out String? ErrorResponse, Int32 Sequence = 0)
        {
            Verified = null;
            return TrustTransition.TryParse(JSON, out var transition, out ErrorResponse) &&
                   VerifiedTrustTransition.TryVerify(Context(), @operator, transition, now, RoamingNetworkEpoch.First, Sequence, out Verified, out ErrorResponse);
        }

        private VerifiedTrustTransition Verified(String Name = "policy")
        {
            Assert.That(TryVerify(data[Name]!, out var verified, out var error),  Is.True, error);
            return verified!;
        }


        #region Both_quorums_authorize_the_exact_target_policy()

        /// <summary>
        /// Quorums of the holders of both policies, each counted apart, authorize the trust anchor and the identity keys it
        /// delegates to.
        /// </summary>
        [Test]
        public void Both_quorums_authorize_the_exact_target_policy()
        {

            var result = Verified();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Context.RequiredConsensus,                           Is.EqualTo(2));
                Assert.That(result.SignedByOld,                                         Has.Length.EqualTo(2));
                Assert.That(result.SignedByNew,                                         Has.Length.EqualTo(2));
                Assert.That(result.Context.TrustAnchor.URLs[0].ToString(),              Does.Contain("next.poi.example"));
                Assert.That(result.Context.TrustAnchor.Keys.Any(anchor.Keys.Contains),  Is.False);
                Assert.That(result.Source.FromPolicyId,                                 Is.EqualTo(TrustTransition.PolicyIdOf(anchor)), "the policy identity of WWCP_POI.TS");
            }

        }

        #endregion

        #region One_side_alone_establishes_no_succession()

        /// <summary>
        /// The signatures of the new holders alone, or of the old ones alone, establish no succession.
        /// </summary>
        [Test]
        public void One_side_alone_establishes_no_succession()
        {

            foreach (var side in new[] { TrustTransition.Old, TrustTransition.New })
            {

                var changed = (JObject) data["policy"]!.DeepClone();
                changed["signatures"] = new JArray(((JArray) changed["signatures"]!).Where(signature => signature["side"]!.Value<String>() == side));

                Assert.That(TryVerify(changed, out _, out var error),  Is.False);
                Assert.That(error,  Does.Contain("quorum"));

            }

        }

        #endregion

        #region Changes_do_not_keep_the_authorization()

        /// <summary>
        /// Another target anchor, statement digest or interval, or another place in the chain, is not authorized.
        /// </summary>
        [Test]
        public void Changes_do_not_keep_the_authorization()
        {

            using (Assert.EnterMultipleScope())
            {

                foreach (var (name, value) in new (String, JToken)[] { ("toAnchor",            anchor.ToQRCode()),
                                                                        ("fromStatementDigest", String.Concat(Enumerable.Repeat("00", 32))),
                                                                        ("signedAt",            "2031-01-01T00:00:00.000Z") })
                {
                    var changed = (JObject) data["policy"]!.DeepClone();
                    changed[name] = value;
                    Assert.That(TryVerify(changed, out _, out _),  Is.False, name);
                }

                Assert.That(TryVerify(data["policy"]!, out _, out _, Sequence: 1),  Is.False, "sequence");

            }

        }

        #endregion

        #region Epoch_profiles_constrain_station_proofs()

        /// <summary>
        /// An epoch of a trust context constrains the identifications of commits, the signature algorithms and the start of
        /// the attestations of station proofs verified with it.
        /// </summary>
        [Test]
        public void Epoch_profiles_constrain_station_proofs()
        {

            var proof        = TSVectors.ProofOf(data["baseProof"]!);
            var signedAt     = proof.Attestations.Max(attestation => attestation.SignedAt);

            RoamingNetworkTrustContext In(ETagHashAlgorithm Hash, String[]? Algorithms = null, DateTimeOffset? StartedAt = null)
                => RoamingNetworkTrustContext.WithEpoch(Context(), new RoamingNetworkEpoch(2, new RoamingNetworkEpochProfiles(Hash, Algorithms)), StartedAt);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(VerifiedChargingStation.TryVerify(In(ETagHashAlgorithm.SHA256),                               proof, out var station, out var error, EVSEId: EVSEId),  Is.True, error);
                Assert.That(station!.Verification.AttestedBy,  Has.Length.EqualTo(2));

                Assert.That(VerifiedChargingStation.TryVerify(In(ETagHashAlgorithm.SHA512),                               proof, out _, out error, EVSEId: EVSEId),  Is.False, "a commit identified otherwise");
                Assert.That(error,  Does.Contain("verified epoch 2"));
                Assert.That(VerifiedChargingStation.TryVerify(In(ETagHashAlgorithm.SHA256, [ "ML-DSA-65" ]),              proof, out _, out _,     EVSEId: EVSEId),  Is.False, "attestations of algorithms the epoch does not take");
                Assert.That(VerifiedChargingStation.TryVerify(In(ETagHashAlgorithm.SHA256, StartedAt: signedAt.AddSeconds(1)), proof, out _, out _, EVSEId: EVSEId),  Is.False, "attestations signed before the epoch began");

            }

        }

        #endregion

        #region An_epoch_change_needs_commits_of_all_ETags()

        /// <summary>
        /// The epoch change of the vector opens its epoch by a genesis commit naming the effectiveTree ETag only, which
        /// WWCP_POI.TS takes; a commit names its json, cbor and effectiveTree ETags (SPECIFICATION.md, section 2), so here it
        /// is refused.
        /// </summary>
        [Test]
        public void An_epoch_change_needs_commits_of_all_ETags()
        {

            Assert.That(TryVerify(data["epoch"]!, out _, out var error),  Is.False);
            Assert.That(error,  Does.Contain("json, cbor and effectiveTree"));

        }

        #endregion

        #region An_epoch_change_of_complete_commits_opens_the_next_epoch()

        /// <summary>
        /// The epoch change of the vector, made again here of commits naming all their ETags: the head and the final
        /// checkpoint signed by the signing keys of the seeds 42 and 44, the genesis of the next epoch calculated of the final
        /// checkpoint, its first checkpoint following it, the transition signed by the identity keys of the seeds 41 and 43
        /// on both sides. Its context then verifies proofs of the next epoch, not those attested before it began.
        /// </summary>
        [Test]
        public void An_epoch_change_of_complete_commits_opens_the_next_epoch()
        {

            var given    = (JObject) data["epoch"]!;
            var network  = RoamingNetwork_Id.Parse("CHARGY-POI-DEMO");
            var genesis1 = RoamingNetworkCommit.Genesis(new RoamingNetwork(network, Created: DateTimeOffset.Parse("2026-01-01T00:00:00.000Z")));
            var next     = new RoamingNetworkEpoch(2, new RoamingNetworkEpochProfiles(ETagHashAlgorithm.SHA256, [ "Ed25519" ]));

            RoamingNetworkCheckpoint Signed(RoamingNetworkCheckpoint Checkpoint)
                => Checkpoint.WithSignatures([ .. new Byte[] { 42, 44 }.Select(seed => {
                                                      var privateKey = new Ed25519PrivateKeyParameters(Enumerable.Repeat(seed, 32).ToArray());
                                                      return Checkpoint.CreateSignature(privateKey, POIPublicKey.From(privateKey).Id, COSEAlgorithm.Ed25519,
                                                                                        RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, "CHARGY-DEMO-RNO", Checkpoint.CreatedAt);
                                                  }) ]);

            var head     = Signed(RoamingNetworkCheckpoint.Create(network, 1, 0, null,    genesis1, [ genesis1 ], DateTimeOffset.Parse("2026-11-01T00:10:00.000Z")));
            var final    = Signed(RoamingNetworkCheckpoint.Create(network, 1, 1, head.Id, genesis1, [ genesis1 ], DateTimeOffset.Parse("2026-11-01T00:11:00.000Z"), Next: next));
            var genesis2 = RoamingNetworkCommit.GenesisOf(network, final.ETags, next);
            var first    = Signed(RoamingNetworkCheckpoint.Create(network, 2, 0, null,    genesis2, [ genesis2 ], DateTimeOffset.Parse("2026-11-01T00:11:40.000Z"), PreviousEpoch: RoamingNetworkEpochLink.To(final)));

            var made     = TrustTransition.Create(0,
                                                  null,
                                                  anchor,
                                                  statement,
                                                  anchor,
                                                  @operator,
                                                  DateTimeOffset.Parse(given["signedAt"]!.Value<String>()!),
                                                  DateTimeOffset.Parse(given["validUntil"]!.Value<String>()!),
                                                  [ head, final ],
                                                  genesis2,
                                                  first);

            foreach (var side in new[] { TrustTransition.Old, TrustTransition.New })
                foreach (var seed in new Byte[] { 41, 43 })
                {
                    var privateKey = new Ed25519PrivateKeyParameters(Enumerable.Repeat(seed, 32).ToArray());
                    made = made.WithSignature(side, privateKey, POIPublicKey.From(privateKey).Id, COSEAlgorithm.Ed25519);
                }

            Assert.That(VerifiedTrustTransition.TryVerify(Context(), @operator, made, now, RoamingNetworkEpoch.First, 0, out var result, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(result!.Epoch.Number,                                Is.EqualTo(2));
                Assert.That(result.EpochStartedAt,                               Is.EqualTo(first.CreatedAt));
                Assert.That(result.Context.Epochs.Select(epoch => epoch.Number), Is.EquivalentTo(new UInt32[] { 1, 2 }));
                Assert.That(result.Checkpoints,                                  Has.Length.EqualTo(2));

                Assert.That(VerifiedChargingStation.TryVerify(result.Context, TSVectors.ProofOf(data["epochProof"]!), out _, out error, EVSEId: EVSEId),  Is.True,  error);
                Assert.That(VerifiedChargingStation.TryVerify(result.Context, TSVectors.ProofOf(data["baseProof"]!),  out _, out _,     EVSEId: EVSEId),  Is.False, "attested before the epoch began");

                var otherGenesis = (JObject) made.JSON.DeepClone();
                otherGenesis["epochChange"]!["genesis"] = genesis1.ToJSON();
                Assert.That(TryVerify(otherGenesis, out _, out error),  Is.False, "another genesis");

            }

        }

        #endregion

        #region A_malformed_target_certificate_is_not_authorized()

        /// <summary>
        /// Even signed by both quorums, a transition to an operator with a malformed certificate is refused.
        /// </summary>
        [Test]
        public void A_malformed_target_certificate_is_not_authorized()
        {
            Assert.That(TryVerify(data["invalidCertificate"]!, out _, out _),  Is.False);
        }

        #endregion

        #region An_RNO_makes_the_transition_WWCP_POI_TS_verifies()

        /// <summary>
        /// A transition made here, signed by the old identity keys of the seeds 41 and 43 and the new ones of 51 and 53, is
        /// the one of the vector, Ed25519 signing deterministically.
        /// </summary>
        [Test]
        public void An_RNO_makes_the_transition_WWCP_POI_TS_verifies()
        {

            var given = (JObject) data["policy"]!;

            Assert.That(RoamingNetworkTrustAnchor.TryParse(given["toAnchor"]!.Value<String>()!, out var target, out var error),  Is.True, error);

            var made  = TrustTransition.Create(0,
                                               null,
                                               anchor,
                                               statement,
                                               target!,
                                               (JObject) given["toOperator"]!,
                                               DateTimeOffset.Parse(given["signedAt"]!.Value<String>()!),
                                               DateTimeOffset.Parse(given["validUntil"]!.Value<String>()!),
                                               Checkpoints: []);

            foreach (var (side, seed) in new[] { (TrustTransition.Old, (Byte) 41), (TrustTransition.Old, (Byte) 43),
                                                 (TrustTransition.New, (Byte) 51), (TrustTransition.New, (Byte) 53) })
            {
                var privateKey = new Ed25519PrivateKeyParameters(Enumerable.Repeat(seed, 32).ToArray());
                made = made.WithSignature(side, privateKey, POIPublicKey.From(privateKey).Id, COSEAlgorithm.Ed25519);
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(made.JSON.ToString(),  Is.EqualTo(given.ToString()));
                Assert.That(made.Digest(),         Is.EqualTo(Parse(given).Digest()));
                Assert.That(VerifiedTrustTransition.TryVerify(Context(), @operator, made, now, RoamingNetworkEpoch.First, 0, out _, out error),  Is.True, error);
            }

        }

        #endregion

        #region Charging_document_keys_of_the_proven_CPO()

        /// <summary>
        /// The keys of the CPO a proof gives for charge transparency records, mapped to COSE keys, and none of another CPO.
        /// </summary>
        [Test]
        public void Charging_document_keys_of_the_proven_CPO()
        {

            var proof = TSVectors.ProofOf(data["baseProof"]!);

            Assert.That(AuthorizedChargingDocumentKeys.TryVerify(Context(), proof, EVSEId, "DE*POI", DateTimeOffset.Parse("2026-11-01T00:05:01.000Z"), out var keys, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(keys!.Keys[0].Algorithm,  Is.EqualTo("EdDSA-Ed25519"));
                Assert.That(keys.Keys[0].Encodings,   Is.EqualTo(new[] { "COSE_Key", "CBOR", "base64" }));
                Assert.That(keys.Keys[0].KeyUsage,    Is.EqualTo(new[] { "signCTRs" }));
                Assert.That(AuthorizedChargingDocumentKeys.TryVerify(Context(), proof, EVSEId, "FOREIGN", now, out _, out _),  Is.False);
            }

        }

        #endregion

    }

}
