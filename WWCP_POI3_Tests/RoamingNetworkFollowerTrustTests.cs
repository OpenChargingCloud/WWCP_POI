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

using org.GraphDefined.Vanaheimr.Illias;

using static cloud.charging.open.protocols.WWCP.POI.tests.RoamingShareholderTests;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Whom a server following a roaming network of another server trusts to sign as its operator
    /// (RoamingNetworkFollowerTrust; SPECIFICATION.md, section 8.6): the signing keys the trust anchor leads to, and
    /// the keys a version states anew once the ones trusted so far signed that.
    /// </summary>
    [TestFixture]
    public class RoamingNetworkFollowerTrustTests
    {

        #region (static) TrustAnchorOf(Network)

        private static RoamingNetworkTrustAnchor TrustAnchorOf(RoamingNetwork Network)

            => Network.CreateTrustAnchor(Network.Operator!.KeyStatement!);

        #endregion


        #region The_signing_keys_the_trust_anchor_leads_to_sign_as_the_operator()

        /// <summary>
        /// A commit signed in the role of the roaming network operator is taken where its key is certified in the
        /// version it leads to by an identity key the trust anchor names; in another role, by a key the version does not
        /// certify, or in a roaming network of the same identification that others operate, it is not.
        /// </summary>
        [Test]
        public void The_signing_keys_the_trust_anchor_leads_to_sign_as_the_operator()
        {

            var alpha      = Shareholder("Alpha");
            var history    = Joined(alpha);
            var commit     = history.Head.Commit;
            var version    = history.Head.RoamingNetwork;
            var trust      = new RoamingNetworkFollowerTrust(TrustAnchorOf(version));

            // The same roaming network as somebody else began it: other keys, the same identification.
            var stranger   = Shareholder("Alpha");
            var other      = Joined(stranger);
            var strangers  = new RoamingNetworkFollowerTrust(TrustAnchorOf(other.Head.RoamingNetwork));

            // The same commit, signed by a key the version does not certify, and in a role that is not the operator's.
            var uncertified  = new RoamingNetworkCommitSigner(stranger.SigningKey.Private, stranger.SigningKeyId, stranger.SigningKey.Algorithm,
                                                              RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, OCC.ToString()).Sign(commit);

            var asLeader     = new RoamingNetworkCommitSigner(alpha.SigningKey.Private, alpha.SigningKeyId, alpha.SigningKey.Algorithm,
                                                              RoamingNetworkCommitSignatureRoles.Leader).Sign(commit);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(trust.VerifyCommitSignature(commit, version, commit.Signatures.Single()),      Is.True);
                Assert.That(trust.SigningKeyOf(version,
                                               RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                               alpha.SigningKeyId,
                                               T0)?.Holder,                                                 Is.EqualTo("Alpha"));

                Assert.That(trust.VerifyCommitSignature(commit, version, uncertified),                      Is.False, "a key the version does not certify");
                Assert.That(trust.VerifyCommitSignature(commit, version, asLeader),                         Is.False, "another role than the operator's");
                Assert.That(strangers.VerifyCommitSignature(commit, version, commit.Signatures.Single()),   Is.False, "trusting others");

                // A checkpoint and an attestation name no version: nothing is verified while there is none to check them in.
                Assert.That(trust.VerifyAttestation(() => null)(alpha.CommitSignerFor(OCC).Attest(version.Id, commit.Id, commit.Revision, commit.ETags.Last(), T0)),  Is.False);
                Assert.That(trust.VerifyAttestation(() => version)(alpha.CommitSignerFor(OCC).Attest(version.Id, commit.Id, commit.Revision, commit.ETags.Last(), T0)),  Is.True);

            }

        }

        #endregion

        #region The_trust_anchor_is_followed_through_the_versions_checked()

        /// <summary>
        /// Beta joins Alpha: the version of two holders states their keys, signed by Alpha, the one trusted so far, so
        /// the trust anchor names both from then on, two deciding - and told so, once. A version whose key statement
        /// the trusted keys did not sign leads nowhere.
        /// </summary>
        [Test]
        public void The_trust_anchor_is_followed_through_the_versions_checked()
        {

            var alpha     = Shareholder("Alpha");
            var beta      = Shareholder("Beta");
            var alone     = Begun(alpha);
            var both      = Joined(alpha, beta).Head.RoamingNetwork;
            var trust     = new RoamingNetworkFollowerTrust(TrustAnchorOf(alone));
            var told      = new List<RoamingNetworkTrustAnchor>();

            trust.OnFollowed += (_, trustAnchor) => told.Add(trustAnchor);

            // Somebody else's roaming network of the same identification.
            var stranger  = Begun(Shareholder("Gamma"));

            var same      = trust.Follow(alone);
            var strange   = trust.Follow(stranger);
            var followed  = trust.Follow(both);
            var again     = trust.Follow(both);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(same,                                 Is.False, "the version the trust anchor is of");
                Assert.That(strange,                              Is.False, "a key statement the trusted keys did not sign");
                Assert.That(followed,                             Is.True);
                Assert.That(again,                                Is.False);

                Assert.That(trust.TrustAnchor.Holders,            Has.Count.EqualTo(2));
                Assert.That(trust.TrustAnchor.MinConsensus,       Is.EqualTo(2));
                Assert.That(trust.TrustAnchor.Keys,               Is.EquivalentTo(new[] { alpha.IdentityPublicKey.Id, beta.IdentityPublicKey.Id }));
                Assert.That(trust.TrustAnchor.URLs,               Is.EqualTo(both.URLs));
                Assert.That(told,                                 Has.Count.EqualTo(1));

                // The signing key of Beta signs as the operator now, as the version of both certifies it.
                Assert.That(trust.SigningKeyOf(both,
                                               RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                               beta.SigningKeyId,
                                               T0.AddMinutes(5))?.Holder,   Is.EqualTo("Beta"));

            }

        }

        #endregion

    }

}
