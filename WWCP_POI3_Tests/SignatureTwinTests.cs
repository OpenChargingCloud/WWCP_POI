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

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// An ECDSA signature (r, s) has a twin, (r, n - s), as valid, which anybody makes from it without the key
    /// (POI3-ISSUES.md S11): a history keeps an attestation and its twin once, a commit a signature and its twin once.
    /// And a trusted attester signing many attestations evicts its own ones, not those of the others (S29).
    /// </summary>
    [TestFixture]
    public class SignatureTwinTests
    {

        #region Data

        private static readonly ECPrivateKeyParameters  ECDSAKey        = TestParties.Key(new POIPartyId(nameof(RoamingNetworkOperator), "twins")).Private;
        private static readonly ECPublicKeyParameters   ECDSAPublicKey  = new (ECDSAKey.Parameters.G.Multiply(ECDSAKey.D).Normalize(), ECDSAKey.Parameters);

        private static RoamingNetworkCommitSigner Signer
            => new (ECDSAKey, "rno-ecdsa", COSEAlgorithm.ES256, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, "DE*RNO");

        #endregion


        #region (private static) NewNetwork() / Twin(Value)

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    DataSource:  "v0",
                    Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")).WithKeys();

        /// <summary>
        /// The twin of the given ECDSA signature value on P-256, r and n - s, in canonical Base64: made without the key.
        /// </summary>
        private static String Twin(String Value)
        {

            var bytes  = Convert.FromBase64String(Value);
            var half   = bytes.Length / 2;
            var s      = new Org.BouncyCastle.Math.BigInteger(1, bytes, half, half);
            var otherS = ECDSAKey.Parameters.N.Subtract(s).ToByteArrayUnsigned();
            var twin   = new Byte[bytes.Length];

            Array.Copy(bytes,  twin, half);
            Array.Copy(otherS, 0, twin, bytes.Length - otherS.Length, otherS.Length);

            return Convert.ToBase64String(twin);

        }

        #endregion


        #region A_history_keeps_an_attestation_and_its_twin_once()

        [Test]
        public void A_history_keeps_an_attestation_and_its_twin_once()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var head      = history.Head;
            var original  = Signer.Attest(head.RoamingNetwork.Id, head.Commit.Id, head.Commit.Revision, head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256), Timestamp.Now, Timestamp.Now.AddMinutes(15));

            var json      = original.ToJSON();
            json["value"] = Twin(original.Value);

            Assert.That(RoamingNetworkAttestation.TryParse(json, out var twin, out var parseError), Is.True, parseError);

            VerifyAttestationDelegate trusted = attestation => attestation.VerifySignature(ECDSAPublicKey, "rno-ecdsa", out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(twin!.Value,                                              Is.Not.EqualTo(original.Value));
                Assert.That(trusted(twin),                                            Is.True, "the twin is as valid");
                Assert.That(history.TryAddAttestation(original, trusted, out var error1),  Is.True, error1);
                Assert.That(history.TryAddAttestation(twin,     trusted, out var error2),  Is.True, error2);
                Assert.That(history.CurrentAttestations().Select(attestation => attestation.Value),  Is.EqualTo(new[] { original.Value }), "kept once");
            }

        }

        #endregion

        #region A_commit_keeps_a_signature_and_its_twin_once()

        [Test]
        public void A_commit_keeps_a_signature_and_its_twin_once()
        {

            var history    = RoamingNetworkHistory.Start(NewNetwork());
            var commit     = history.Head.Commit;
            var signature  = Signer.Sign(commit);

            var json       = signature.ToJSON();
            json["value"]  = Twin(signature.Value);

            Assert.That(RoamingNetworkCommitSignature.TryParse(json, out var twin, out var parseError), Is.True, parseError);

            VerifyCommitSignatureDelegate verify = (commit, version, signature) => commit.VerifySignature(signature, ECDSAPublicKey, "rno-ecdsa", out _);

            var added  = history.TryAddSignatures(commit.Id, [ signature ], verify, out _,          out var first,  out var error1);
            var again  = history.TryAddSignatures(commit.Id, [ twin! ],     verify, out var signed, out var second, out var error2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(twin!.Value,                                              Is.Not.EqualTo(signature.Value));
                Assert.That(verify(commit, history.Head.RoamingNetwork, twin),        Is.True, "the twin is as valid");
                Assert.That(added,                                                    Is.True, error1);
                Assert.That(first,                                                    Has.Count.EqualTo(1));
                Assert.That(again,                                                    Is.True, error2);
                Assert.That(second,                                                   Is.Empty, "the twin is no new signature");
                Assert.That(signed!.Signatures.Select(kept => kept.Value),           Is.EqualTo(new[] { signature.Value }));
                Assert.That(signed. WithSignatures([ twin ]),                         Is.SameAs(signed));
            }

        }

        #endregion

        #region An_attester_signing_many_evicts_its_own_attestations()

        [Test]
        public void An_attester_signing_many_evicts_its_own_attestations()
        {

            var otherKey  = TestParties.Key(new POIPartyId(nameof(RoamingNetworkOperator), "twins-other")).Private;
            var other     = new RoamingNetworkCommitSigner(otherKey, "rno-other", COSEAlgorithm.ES256, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, "DE*RNO");
            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var head      = history.Head;
            var eTag      = head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256);
            var now       = Timestamp.Now;

            VerifyAttestationDelegate trusted = attestation => attestation.KeyId == "rno-ecdsa"
                                                                   ? attestation.VerifySignature(ECDSAPublicKey, "rno-ecdsa", out _)
                                                                   : attestation.VerifySignature(new ECPublicKeyParameters(otherKey.Parameters.G.Multiply(otherKey.D).Normalize(), otherKey.Parameters), "rno-other", out _);

            // The other one first, the oldest of all; then more than the history keeps of the one key, each of another time.
            var theOther  = other.Attest(head.RoamingNetwork.Id, head.Commit.Id, head.Commit.Revision, eTag, now.AddSeconds(-30), now.AddMinutes(15));

            Assert.That(history.TryAddAttestation(theOther, trusted, out var error), Is.True, error);

            for (var i = 0; i <= RoamingNetworkHistory.MaxAttestations; i++)
                Assert.That(history.TryAddAttestation(Signer.Attest(head.RoamingNetwork.Id, head.Commit.Id, head.Commit.Revision, eTag, now.AddSeconds(-20).AddMilliseconds(i), now.AddMinutes(15)), trusted, out error),
                            Is.True, error);

            var kept      = history.CurrentAttestations();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(kept,                                                     Has.Count.EqualTo(RoamingNetworkHistory.MaxAttestations));
                Assert.That(kept.Count(attestation => attestation.KeyId == "rno-other"),  Is.EqualTo(1), "the other one is kept");
                Assert.That(kept.Min(attestation => attestation.KeyId == "rno-ecdsa" ? attestation.SignedAt : DateTimeOffset.MaxValue),
                            Is.EqualTo(now.AddSeconds(-20).AddMilliseconds(2)).Within(TimeSpan.FromMilliseconds(1)), "the oldest two of the one key went");
            }

        }

        #endregion

        #region A_commit_keeps_one_signature_per_key_and_role()

        /// <summary>
        /// A key signing a commit in the same role at another time adds no further signature: a commit keeps one of each
        /// key in each role, the first (POI3-ISSUES.md S24).
        /// </summary>
        [Test]
        public void A_commit_keeps_one_signature_per_key_and_role()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var commit   = history.Head.Commit;
            var first    = Signer.Sign(commit, Timestamp.Now.AddMinutes(-2));
            var later    = Signer.Sign(commit, Timestamp.Now.AddMinutes(-1));

            VerifyCommitSignatureDelegate verify = (commit, version, signature) => commit.VerifySignature(signature, ECDSAPublicKey, "rno-ecdsa", out _);

            var added    = history.TryAddSignatures(commit.Id, [ first ], verify, out _,          out var firstNew, out var error1);
            var again    = history.TryAddSignatures(commit.Id, [ later ], verify, out var signed, out var laterNew, out var error2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(later.SignedAt,                                           Is.Not.EqualTo(first.SignedAt));
                Assert.That(added,                                                    Is.True, error1);
                Assert.That(firstNew,                                                 Has.Count.EqualTo(1));
                Assert.That(again,                                                    Is.True, error2);
                Assert.That(laterNew,                                                 Is.Empty, "no further signature of the same key in the same role");
                Assert.That(signed!.Signatures.Select(kept => kept.SignedAt),        Is.EqualTo(new[] { first.SignedAt }));
            }

        }

        #endregion

    }

}
