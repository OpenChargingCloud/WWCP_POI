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

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Trust contexts, as WWCP_POI.TS 0.1.1 has them (tests/trustContext.test.ts there): an app that imports a trust anchor
    /// before its users trust its keys verifies against a context it evaluates of the identity keys it trusts locally, not
    /// against the anchor, which trusts every key it pins. The operator of the quorum tests: Alpha with two identity keys,
    /// Beta and Gamma with one each, a minConsensus of two.
    /// </summary>
    [TestFixture]
    public class TrustContextTests
    {

        #region Data

        private static readonly DateTimeOffset  T0  = DateTimeOffset.Parse("2026-10-21T12:00:00Z");
        private static readonly DateTimeOffset  At  = T0.AddHours(1);

        #endregion

        #region (static) Network()

        /// <summary>
        /// The network of the quorum tests, Alpha with a second identity key.
        /// </summary>
        private static RoamingNetwork Network()

            => QuorumTests.Network(2, ExtraIdentityKeyOf: "Alpha");

        #endregion

        #region (static) Id(Network, Holder, Number = 1)

        /// <summary>
        /// The @id of the given identity key of the given holder.
        /// </summary>
        private static String Id(RoamingNetwork  Network,
                                 String          Holder,
                                 Int32           Number   = 1)

            => QuorumTests.Key(Network, Holder, POIKeyKind.Identity, Number).Public.Id;

        #endregion

        #region (static) Anchor(Network, MinConsensus = 2)

        /// <summary>
        /// The trust anchor of the given network, its identity keys per holder.
        /// </summary>
        private static RoamingNetworkTrustAnchor Anchor(RoamingNetwork  Network,
                                                        Byte            MinConsensus   = 2)

            => new (Network.Id,
                    [ URL.Parse($"https://hub.example/poi/RNs/{Network.Id}") ],
                    QuorumTests.HoldersOf(Network),
                    MinConsensus);

        #endregion

        #region (static) Statement(Network, Signers, SignedAt = null, ValidUntil = null, RoamingNetworkId = null, RoamingNetworkOperatorId = null)

        /// <summary>
        /// A key statement of the operator of the given network, naming its identity keys, signed by the given identity keys
        /// of its holders in this order, valid from T0 for 30 days by default.
        /// </summary>
        private static RoamingNetworkKeyStatement Statement(RoamingNetwork                    Network,
                                                            (String Holder, Int32 Number)[]   Signers,
                                                            DateTimeOffset?                   SignedAt                   = null,
                                                            DateTimeOffset?                   ValidUntil                 = null,
                                                            RoamingNetwork_Id?                RoamingNetworkId           = null,
                                                            RoamingNetworkOperator_Id?        RoamingNetworkOperatorId   = null)

            => RoamingNetworkKeyStatement.Create([ .. Signers.Select(signer => ((AsymmetricKeyParameter) QuorumTests.Key(Network, signer.Holder, POIKeyKind.Identity, signer.Number).Private, COSEAlgorithm.ES512)) ],
                                                 2,
                                                 RoamingNetworkId         ?? Network.Id,
                                                 RoamingNetworkOperatorId ?? Network.Operator!.Id,
                                                 Network.Operator!.IdentityKeys,
                                                 SignedAt                 ?? T0,
                                                 ValidUntil               ?? T0.AddDays(30));

        #endregion

        #region (static) Signed(Network)

        /// <summary>
        /// The key statement signed by both identity keys of Alpha and by Beta.
        /// </summary>
        private static RoamingNetworkKeyStatement Signed(RoamingNetwork Network)

            => Statement(Network, [ ("Alpha", 1), ("Alpha", 2), ("Beta", 1) ]);

        #endregion

        #region (static) Context(Network, Statement)

        /// <summary>
        /// The trust context of the given key statement of the given network, of the first identity key of Alpha and the
        /// one of Beta, at At.
        /// </summary>
        private static RoamingNetworkTrustContext Context(RoamingNetwork              Network,
                                                          RoamingNetworkKeyStatement  Statement)
        {

            Assert.That(RoamingNetworkTrustContext.TryCreate(Anchor(Network), Statement, [ Id(Network, "Alpha"), Id(Network, "Beta") ], out var context, out var error, At: At),  Is.True, error);

            return context!;

        }

        #endregion


        #region Importing_valid_self_signed_data_authorizes_nothing()

        /// <summary>
        /// A key statement signed genuinely gives no context of a trust anchor whose keys the app trusts none of, nor of keys
        /// of one holder only.
        /// </summary>
        [Test]
        public void Importing_valid_self_signed_data_authorizes_nothing()
        {

            var network  = Network();
            var signed   = Signed(network);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetworkTrustContext.TryCreate(Anchor(network), signed, [],                                                   out _, out var error1, At: At),  Is.False);
                Assert.That(error1,  Does.Contain("not trusted").Or.Contain("of 0 holders"));

                Assert.That(RoamingNetworkTrustContext.TryCreate(Anchor(network), signed, [ Id(network, "Alpha"), Id(network, "Alpha", 2) ],  out _, out var error2, At: At),  Is.False);
                Assert.That(error2,  Does.Contain("of 1 holder, not of 2"));

            }

        }

        #endregion

        #region A_context_keeps_the_holders_and_the_quorum()

        /// <summary>
        /// A context keeps the holders of the trust anchor with the keys trusted locally, promotes no key pinned but not
        /// chosen, and keeps the quorum; the key statement verified with it delegates to the identity keys it names, which is
        /// no trust in the keys pinned.
        /// </summary>
        [Test]
        public void A_context_keeps_the_holders_and_the_quorum()
        {

            var network  = Network();
            var anchor   = Anchor(network);
            var signed   = Signed(network);
            var context  = Context(network, signed);
            var trusted  = new[] { Id(network, "Alpha"), Id(network, "Beta") };

            using (Assert.EnterMultipleScope())
            {

                Assert.That(context.RequiredConsensus,                  Is.EqualTo(2));
                Assert.That(context.AuthorizedTrustAnchor.Keys,         Is.EquivalentTo(trusted));
                Assert.That(context.AuthorizedTrustAnchor.MinConsensus, Is.EqualTo(2));
                Assert.That(context.TrustedHolders.Select(keys => String.Join(";", keys)),
                            Is.EqualTo(anchor.Holders.Select(keys => String.Join(";", keys.Where(trusted.Contains)))));
                Assert.That(context.TrustAnchor.Keys,                   Is.EqualTo(anchor.Keys));

                Assert.That(context.TryVerify(signed, null, out var holderOf, out var error),  Is.True, error);
                Assert.That(holderOf!.ContainsKey(Id(network, "Alpha", 2)),  Is.True, "A statement of the quorum delegates to the keys it names.");
                Assert.That(holderOf.ContainsKey(Id(network, "Gamma")),      Is.True);

            }

        }

        #endregion

        #region A_key_removed_locally_counts_once_the_context_is_evaluated_again()

        /// <summary>
        /// A key the app no longer trusts is left out of the next context: one evaluated of the key of Alpha alone is none.
        /// </summary>
        [Test]
        public void A_key_removed_locally_counts_once_the_context_is_evaluated_again()
        {

            var network = Network();

            Assert.That(RoamingNetworkTrustContext.TryCreate(Anchor(network), Signed(network), [ Id(network, "Alpha") ], out var context, out var error, At: At),  Is.False);
            Assert.That(context,  Is.Null);
            Assert.That(error,    Does.Contain("of 1 holder, not of 2"));

        }

        #endregion

        #region The_minConsensus_of_the_key_statement_is_kept()

        /// <summary>
        /// A trust anchor with a lower minConsensus does not lower the one of the key statement.
        /// </summary>
        [Test]
        public void The_minConsensus_of_the_key_statement_is_kept()
        {

            var network = Network();

            Assert.That(RoamingNetworkTrustContext.TryCreate(Anchor(network, 1), Signed(network), [ Id(network, "Alpha") ], out _, out var error, At: At),  Is.False);
            Assert.That(error,  Does.Contain("not of 2"));

        }

        #endregion

        #region Expired_future_tampered_and_foreign_key_statements_give_no_context()

        /// <summary>
        /// A key statement no longer valid, not yet valid, of another roaming network or with a changed content gives no
        /// context, though all its keys are trusted.
        /// </summary>
        [Test]
        public void Expired_future_tampered_and_foreign_key_statements_give_no_context()
        {

            var network   = Network();
            var anchor    = Anchor(network);
            var all       = network.Operator!.IdentityKeys.Select(key => key.Id).ToArray();
            var signers   = new[] { ("Alpha", 1), ("Beta", 1) };

            var json      = Signed(network).ToJSON();
            json[RoamingNetworkKeyStatement.PropertyKeys.RoamingNetworkOperatorId] = "FORGED";
            Assert.That(RoamingNetworkKeyStatement.TryParse(json, out var forged, out var parseError),  Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, Statement(network, signers, ValidUntil: T0.AddMinutes(30)), all, out _, out var expired, At: At),  Is.False);
                Assert.That(expired,  Does.Contain("is not valid at"));

                Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, Statement(network, signers, SignedAt: T0.AddDays(1)),       all, out _, out var future,  At: At),  Is.False);
                Assert.That(future,   Does.Contain("is not valid at"));

                var tampered = RoamingNetworkTrustContext.Evaluate(anchor, forged!, all, At: At);
                Assert.That(tampered.SignaturesValid,  Is.False);
                Assert.That(tampered.Context,          Is.Null);

                var other = Statement(network, signers, RoamingNetworkId: RoamingNetwork_Id.Parse("OTHER"));
                Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, other, all, out _, out var foreign, At: At),  Is.False);
                Assert.That(foreign,  Does.Contain("is one of the roaming network 'OTHER'"));
                Assert.That(() => RoamingNetworkTrustContext.Evaluate(anchor, other, all, At: At),  Throws.ArgumentException);

            }

        }

        #endregion

        #region A_context_is_bound_to_the_operator_and_the_content_of_its_key_statement()

        /// <summary>
        /// A context verifies only the key statement it was evaluated of: not one of another operator, not another one with
        /// genuine signatures; the same content signed by others of the quorum, or written again with its signatures in
        /// another order or embedded, every signature and the quorum checked again.
        /// </summary>
        [Test]
        public void A_context_is_bound_to_the_operator_and_the_content_of_its_key_statement()
        {

            var network   = Network();
            var signed    = Signed(network);
            var context   = Context(network, signed);

            var json      = signed.ToJSON();
            json[RoamingNetworkKeyStatement.PropertyKeys.Signatures] = new JArray(((JArray) json[RoamingNetworkKeyStatement.PropertyKeys.Signatures]!).Reverse());

            Assert.That(RoamingNetworkKeyStatement.TryParse(json,                           out var reordered, out var error1),  Is.True, error1);
            Assert.That(RoamingNetworkKeyStatement.TryParse(signed.ToJSON(Embedded: true),  out var embedded,  out var error2),  Is.True, error2);

            using (Assert.EnterMultipleScope())
            {

                foreach (var other in new[] { Statement(network, [ ("Alpha", 1), ("Beta", 1) ], RoamingNetworkOperatorId: RoamingNetworkOperator_Id.Parse("OTHER-RNO")),
                                              Statement(network, [ ("Alpha", 1), ("Beta", 1) ], ValidUntil: T0.AddDays(29)) })
                {
                    Assert.That(context.TryVerify(other, null, out _, out var differs),  Is.False);
                    Assert.That(differs,  Does.Contain("differs"));
                }

                Assert.That(context.TryVerify(Statement(network, [ ("Alpha", 1), ("Beta", 1) ]),         null, out _, out var error3),  Is.True, error3);
                Assert.That(context.TryVerify(Statement(network, [ ("Alpha", 1), ("Alpha", 2) ]),       null, out _, out var error4),  Is.False);
                Assert.That(error4,  Does.Contain("of 1 holder, not of 2"));

                Assert.That(context.TryVerify(reordered!,  null, out _, out var error5),  Is.True, error5);
                Assert.That(context.TryVerify(embedded!,   null, out _, out var error6),  Is.True, error6);

            }

        }

        #endregion

        #region A_context_verifies_at_the_time_it_was_evaluated_at()

        /// <summary>
        /// A context verifies at the time it was evaluated at only: another time needs another context.
        /// </summary>
        [Test]
        public void A_context_verifies_at_the_time_it_was_evaluated_at()
        {

            var network  = Network();
            var signed   = Signed(network);
            var context  = Context(network, signed);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(context.EvaluatedAt,  Is.EqualTo(At));
                Assert.That(context.TryGetEvaluationTime(null, out var time, out var error1),  Is.True, error1);
                Assert.That(time,                 Is.EqualTo(At));

                Assert.That(context.TryVerify(signed, null, out _, out var error2, At.AddMilliseconds(1)),  Is.False);
                Assert.That(error2,  Does.Contain("recreate it"));

            }

        }

        #endregion

        #region Neither_holders_named_otherwise_nor_two_keys_of_one_holder_are_two_holders()

        /// <summary>
        /// A trust anchor pinning both keys of Alpha as two holders gives no context of a statement naming them as Alpha's,
        /// and a statement signed by both keys of Alpha alone is signed by one holder.
        /// </summary>
        [Test]
        public void Neither_holders_named_otherwise_nor_two_keys_of_one_holder_are_two_holders()
        {

            var network  = Network();
            var anchor   = Anchor(network);
            var split    = new RoamingNetworkTrustAnchor(network.Id, anchor.URLs, anchor.Keys, 2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetworkTrustContext.TryCreate(split,  Signed(network),                                         anchor.Keys, out _, out var error1, At: At),  Is.False);
                Assert.That(error1,  Does.Contain("otherwise than it is trusted"));

                Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, Statement(network, [ ("Alpha", 1), ("Alpha", 2) ]),     anchor.Keys, out _, out var error2, At: At),  Is.False);
                Assert.That(error2,  Does.Contain("of 1 holder, not of 2"));

            }

        }

        #endregion

        #region The_evaluation_reports_signatures_signers_quorum_and_validity_apart()

        /// <summary>
        /// The evaluation lets an app import a key statement of signers it does not trust, without a context; a holder of the
        /// trust anchor with no key trusted locally keeps its place in the context, and leaves the authorized trust anchor.
        /// </summary>
        [Test]
        public void The_evaluation_reports_signatures_signers_quorum_and_validity_apart()
        {

            var network     = Network();
            var anchor      = Anchor(network);
            var signed      = Signed(network);
            var untrusted   = RoamingNetworkTrustContext.Evaluate(anchor, signed, [], At: At);

            var extra       = new RoamingNetworkTrustAnchor(network.Id, anchor.URLs, [ .. anchor.Holders, [ new String('f', 64) ] ], 2);

            Assert.That(RoamingNetworkTrustContext.TryCreate(extra, signed, anchor.Keys, out var context, out var error, At: At),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(untrusted.SignaturesValid,     Is.True);
                Assert.That(untrusted.SignersTrusted,      Is.False);
                Assert.That(untrusted.QuorumReached,       Is.False);
                Assert.That(untrusted.WithinValidity,      Is.True);
                Assert.That(untrusted.CanTrustKeys,        Is.True);
                Assert.That(untrusted.TrustedHolderCount,  Is.Zero);
                Assert.That(untrusted.SignatureCount,      Is.EqualTo(3));
                Assert.That(untrusted.Context,             Is.Null);

                Assert.That(context!.TrustedHolders,                       Has.Count.EqualTo(4));
                Assert.That(context.TrustedHolders.Count(keys => keys.Count == 0),  Is.EqualTo(1));
                Assert.That(context.AuthorizedTrustAnchor.Holders,         Has.Count.EqualTo(3));
                Assert.That(context.RequiredConsensus,                     Is.EqualTo(2));

            }

        }

        #endregion


        #region A_proof_a_checkpoint_and_a_head_verify_with_a_context_as_with_the_trust_anchor()

        /// <summary>
        /// A proof of a charging station, a checkpoint and the head of a history verify with a context of the key statement
        /// the proof gives as with the trust anchor; at another time, and with a context of another key statement, not.
        /// </summary>
        [Test]
        public void A_proof_a_checkpoint_and_a_head_verify_with_a_context_as_with_the_trust_anchor()
        {

            var history  = RoamingNetworkHistory.Start(StationProofTests.NewNetwork().WithKeys().WithKeyStatement());
            history.CheckpointSigners = [ history.Head.RoamingNetwork.OperatorSigner() ];
            history.Attesting();

            var network  = history.Head.RoamingNetwork;
            var anchor   = network.TrustAnchor();
            var at       = Timestamp.Now;

            Assert.That(history.TryGetChargingStationProof(ChargingStation_Id.Parse("DE*GEF*S1"), out var proof, out var error, At: at),  Is.True, error);
            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out error),                                                             Is.True, error);
            Assert.That(history.TryGetRootProof(out var rootProof, out error),                                                              Is.True, error);
            Assert.That(proof!.TryGetKeyStatement(out var statement, out var identityKeys, out error),                                      Is.True, error);
            Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, statement!, anchor.Keys, out var context, out error, identityKeys, at),  Is.True, error);

            // Of another key statement of the same keys: genuine, but not the one the proof gives.
            var identity = TestParties.Key(TestParties.Party(network), POIKeyKind.Identity);
            var renewed  = RoamingNetworkKeyStatement.Create(identity.Private,
                                                             COSEAlgorithm.ES512,
                                                             1,
                                                             network.Id,
                                                             network.Operator!.Id,
                                                             [ identity.Public ],
                                                             statement!.SignedAt,
                                                             statement.ValidUntil.AddDays(-1));

            Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, renewed, anchor.Keys, out var otherContext, out error, At: at),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(proof.Verify(anchor,   out var byAnchor,  out var error1, at),  Is.True, error1);
                Assert.That(proof.Verify(context!, out var byContext, out var error2),      Is.True, error2);
                Assert.That(byContext!.KeyId,  Is.EqualTo(byAnchor!.KeyId));

                Assert.That(rootProof!.Verify(checkpoint!, anchor,   out var error3, at),  Is.True, error3);
                Assert.That(rootProof. Verify(checkpoint!, context!, out var error4),      Is.True, error4);

                Assert.That(history.VerifyHead(anchor,  out var error5, At: at),  Is.True, error5);
                Assert.That(history.VerifyHead(context, out var error6),          Is.True, error6);

                Assert.That(proof.Verify(context, out _, out var error7, at.AddSeconds(1)),  Is.False);
                Assert.That(error7,  Does.Contain("recreate it"));
                Assert.That(history.VerifyHead(context, out var error8, At: at.AddSeconds(1)),  Is.False);
                Assert.That(error8,  Does.Contain("recreate it"));

                Assert.That(proof.     Verify(otherContext!, out _, out var error9),               Is.False);
                Assert.That(error9,   Does.Contain("differs"));
                Assert.That(rootProof. Verify(checkpoint!, otherContext!, out var error10),        Is.False);
                Assert.That(error10,  Does.Contain("differs"));

            }

        }

        #endregion

    }

}
