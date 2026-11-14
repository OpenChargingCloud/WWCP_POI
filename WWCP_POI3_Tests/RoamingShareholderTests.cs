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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The shareholders of a roaming network operator (SPECIFICATION.md, sections 4.1 and 8.6): an organization with
    /// its identity key, its signing key and the URL of its copy, holding one voice among those who decide as the
    /// operator, and the one change set that admits one more of them.
    /// </summary>
    [TestFixture]
    public class RoamingShareholderTests
    {

        #region Data

        internal static readonly DateTimeOffset             T0    = DateTimeOffset.Parse("2026-11-01T12:00:00Z");
        private static readonly RoamingNetwork_Id          QA     = RoamingNetwork_Id.        Parse("QA");
        internal static readonly RoamingNetworkOperator_Id  OCC   = RoamingNetworkOperator_Id.Parse("OCC");

        #endregion

        #region (static) Shareholder(Name, IdentityKey = null)

        /// <summary>
        /// A new shareholder serving its copy of QA at a host of its own name.
        /// </summary>
        internal static RoamingShareholder Shareholder(String        Name,
                                                      COSEKeyPair?  IdentityKey   = null)

            => RoamingShareholder.Create(Name,
                                         IdentityKey ?? COSEKeyPair.NewES512(),
                                         URL: URL.Parse($"https://{Name.ToLowerInvariant()}.example/RNs/{QA}"));

        #endregion

        #region (static) Statement(MinConsensus, Shareholders)

        /// <summary>
        /// The key statement of the given shareholders, stated by the first one and signed by all others as well.
        /// </summary>
        internal static RoamingNetworkKeyStatement Statement(Byte                         MinConsensus,
                                                            params RoamingShareholder[]  Shareholders)
        {

            var statement = Shareholders[0].StateKeys(QA,
                                                      OCC,
                                                      Shareholders.Select(shareholder => shareholder.IdentityPublicKey),
                                                      MinConsensus,
                                                      T0,
                                                      T0.AddYears(1));

            foreach (var shareholder in Shareholders.Skip(1))
                statement = shareholder.CoSign(statement);

            return statement;

        }

        #endregion

        #region (static) Begun(Founder)

        /// <summary>
        /// QA as the given shareholder begins it, alone: its key statement, the certificate of its signing key, its URL.
        /// </summary>
        internal static RoamingNetwork Begun(RoamingShareholder Founder)

            => new RoamingNetworkOperator(
                   Statement(1, Founder),
                   Name:                    I18NString.Create("Open Charging Cloud"),
                   SigningKeyCertificates:  [ Founder.CertifySigningKey(OCC, T0, NotAfter: T0.AddYears(1)) ]
               ).CreateRoamingNetwork(
                   QA,
                   Name:  ImmutableI18NString.Create("Quality Assurance"),
                   URLs:  [ Founder.URL!.Value ]
               );

        #endregion


        #region A_shareholder_is_the_holder_its_keys_name()

        /// <summary>
        /// Its public identity key names it as its holder and no usages; its signing key is for the party only and
        /// certified by its identity key; it signs commits in the role of the roaming network operator.
        /// </summary>
        [Test]
        public void A_shareholder_is_the_holder_its_keys_name()
        {

            var alpha        = Shareholder("Alpha");
            var certificate  = alpha.CertifySigningKey(OCC, T0, NotAfter: T0.AddYears(1));
            var signer       = alpha.CommitSignerFor(OCC);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(alpha.IdentityPublicKey.Holder,                 Is.EqualTo("Alpha"));
                Assert.That(alpha.IdentityPublicKey.Usages,                 Is.Empty);
                Assert.That(alpha.SigningKey.Algorithm,                     Is.EqualTo(COSEAlgorithm.ES256), "the signing key it was given by default");
                Assert.That(alpha.SigningKeyId,                             Is.Not.EqualTo(alpha.IdentityPublicKey.Id));

                Assert.That(certificate.Holder,                             Is.EqualTo("Alpha"));
                Assert.That(certificate.RoamingNetworkOperatorId,           Is.EqualTo(OCC));
                Assert.That(certificate.KeyId,                              Is.EqualTo(alpha.IdentityPublicKey.Id));
                Assert.That(certificate.SigningKey.Id,                      Is.EqualTo(alpha.SigningKeyId));
                Assert.That(certificate.SigningKey.Usages,                  Is.EqualTo([ POIKeyUsages.Party ]));
                Assert.That(certificate.SigningKey.NotAfter,                Is.EqualTo(T0.AddYears(1)));
                Assert.That(certificate.TryVerify([ alpha.IdentityPublicKey ], out _, out var error),  Is.True, error);

                Assert.That(signer.KeyId,                                   Is.EqualTo(alpha.SigningKeyId));
                Assert.That(signer.Role,                                    Is.EqualTo(RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator));
                Assert.That(signer.Signer,                                  Is.EqualTo(OCC.ToString()));

            }

        }

        #endregion

        #region A_shareholder_needs_a_name_and_two_different_keys()

        /// <summary>
        /// A holder is a nonempty text without white space around it, as a public key names it; one key pair is not
        /// both the identity and the signing key.
        /// </summary>
        [Test]
        public void A_shareholder_needs_a_name_and_two_different_keys()
        {

            var keyPair = COSEKeyPair.NewES256();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => RoamingShareholder.Create("",        COSEKeyPair.NewES512()),  Throws.ArgumentException.With.Message.Contains("nonempty text"));
                Assert.That(() => RoamingShareholder.Create(" Alpha",  COSEKeyPair.NewES512()),  Throws.ArgumentException.With.Message.Contains("nonempty text"));
                Assert.That(() => RoamingShareholder.Create("Alpha",   default),                 Throws.ArgumentException.With.Message.Contains("identity key pair"));
                Assert.That(() => RoamingShareholder.Create("Alpha",   keyPair, keyPair),        Throws.ArgumentException.With.Message.Contains("must not be its signing key as well"));

            }

        }

        #endregion

        #region A_shareholder_with_other_keys_is_the_same_holder()

        /// <summary>
        /// WithKeys(…) replaces the keys, e.g. by post-quantum ones, and nothing else: the name a quorum counts and the
        /// URL stay.
        /// </summary>
        [Test]
        public void A_shareholder_with_other_keys_is_the_same_holder()
        {

            var alpha    = Shareholder("Alpha");
            var renewed  = alpha.WithKeys(COSEKeyPair.NewMLDsa87(), COSEKeyPair.NewMLDsa65());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(renewed.Name,                      Is.EqualTo(alpha.Name));
                Assert.That(renewed.URL,                       Is.EqualTo(alpha.URL));
                Assert.That(renewed.IdentityPublicKey.Holder,  Is.EqualTo("Alpha"));
                Assert.That(renewed.IdentityPublicKey.Id,      Is.Not.EqualTo(alpha.IdentityPublicKey.Id));
                Assert.That(renewed.SigningKeyId,              Is.Not.EqualTo(alpha.SigningKeyId));
                Assert.That(renewed.SigningKey.Algorithm,      Is.EqualTo(COSEAlgorithm.MLDsa65));
            }

        }

        #endregion

        #region The_shareholders_join_one_after_the_other()

        /// <summary>
        /// As example A1 tells it: Alpha begins alone, Beta is admitted, two of two, and Gamma, two of three. Each
        /// change set is refused without the new identity key, the proof that its holder has it, and without the quorum
        /// before; the second one does not change minConsensus, as it stays.
        /// </summary>
        [Test]
        public void The_shareholders_join_one_after_the_other()
        {

            var alpha    = Shareholder("Alpha");
            var beta     = Shareholder("Beta",  COSEKeyPair.NewEd448());
            var gamma    = Shareholder("Gamma", COSEKeyPair.NewMLDsa65());

            var history  = RoamingNetworkHistory.Start(Begun(alpha), Signers: [ alpha.CommitSignerFor(OCC) ]);

            #region Beta joins: two of two

            var betaJoins = history.Head.RoamingNetwork.Admit(beta,
                                                              Statement(2, alpha, beta),
                                                              SigningKeyNotAfter:  T0.AddYears(1),
                                                              CreatedAt:           T0.AddMinutes(1));

            Assert.That(betaJoins.Changes.Select(change => change.EntityPropertyName),
                        Is.EqualTo([ POIPartyKeys.IdentityKeysProperty,
                                     RoamingNetworkKeyStatement.JSONProperty,
                                     RoamingNetworkSigningKeyCertificate.JSONProperty,
                                     POIPartyKeys.MinConsensusProperty,
                                     POINetworkURLs.JSONProperty ]));

            betaJoins     = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(betaJoins));

            var withoutBeta = history.Commit(betaJoins);

            Assert.That(withoutBeta.IsCommitted,    Is.False);
            Assert.That(withoutBeta.ErrorResponse,  Does.Contain("needs a signature by the new identity key").And.Contain(beta.IdentityPublicKey.Id));

            history.Signers = [ alpha.CommitSignerFor(OCC), beta.CommitSignerFor(OCC) ];

            var betaJoined  = history.Commit(beta.SignWithIdentityKey(betaJoins));

            Assert.That(betaJoined.IsCommitted,     Is.True, betaJoined.ErrorResponse);

            var both        = history.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(POIPartyKeys.Holders(both.Operator!.IdentityKeys),  Is.EquivalentTo([ "Alpha", "Beta" ]));
                Assert.That(POIPartyKeys.Holders(both.Operator. SigningKeys),   Is.EquivalentTo([ "Alpha", "Beta" ]));
                Assert.That(both.Operator.MinConsensus,                         Is.EqualTo(2));
                Assert.That(both.URLs,                                          Is.EqualTo([ alpha.URL!.Value, beta.URL!.Value ]));
                Assert.That(history.Head.Commit.Signatures,                     Has.Length.EqualTo(2));
            }

            #endregion

            #region Gamma joins: two of three

            var gammaJoins = both.Admit(gamma,
                                        Statement(2, alpha, beta, gamma),
                                        SigningKeyNotAfter:  T0.AddYears(1),
                                        CreatedAt:           T0.AddMinutes(2));

            Assert.That(gammaJoins.Changes.Select(change => change.EntityPropertyName),
                        Has.No.Member(POIPartyKeys.MinConsensusProperty),
                        "two of three after two of two: the quorum stays");

            // Gamma, and Alpha alone of the two so far.
            gammaJoins     = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(gamma.SignWithIdentityKey(gammaJoins)));

            Assert.That(history.Commit(gammaJoins).IsCommitted,  Is.False, "one of the two shareholders so far is no quorum");

            history.Signers = [ alpha.CommitSignerFor(OCC), beta.CommitSignerFor(OCC), gamma.CommitSignerFor(OCC) ];

            var gammaJoined = history.Commit(beta.SignWithSigningKey(beta.SignWithIdentityKey(gammaJoins)));

            Assert.That(gammaJoined.IsCommitted,    Is.True, gammaJoined.ErrorResponse);

            var all         = history.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(POIPartyKeys.Holders(all.Operator!.IdentityKeys),   Is.EquivalentTo([ "Alpha", "Beta", "Gamma" ]));
                Assert.That(POIPartyKeys.Holders(all.Operator. SigningKeys),    Is.EquivalentTo([ "Alpha", "Beta", "Gamma" ]));
                Assert.That(all.Operator.MinConsensus,                          Is.EqualTo(2));
                Assert.That(all.URLs,                                           Is.EqualTo([ alpha.URL!.Value, beta.URL!.Value, gamma.URL!.Value ]));
                Assert.That(all.Operator.KeyStatement!.IdentityKeys,            Has.Length.EqualTo(3));
            }

            #endregion

        }

        #endregion

        #region Whoever_knows_only_what_a_new_shareholder_publishes_admits_it_the_same()

        /// <summary>
        /// The shareholders so far have no private key of the new one: its public identity key, the certificate of its
        /// signing key and its URL make the same change set as the shareholder itself.
        /// </summary>
        [Test]
        public void Whoever_knows_only_what_a_new_shareholder_publishes_admits_it_the_same()
        {

            var alpha      = Shareholder("Alpha");
            var beta       = Shareholder("Beta");
            var alone      = Begun(alpha);
            var statement  = Statement(2, alpha, beta);

            var byBeta     = alone.Admit(beta,
                                         statement,
                                         SigningKeyNotAfter:  T0.AddYears(1),
                                         CreatedAt:           T0);

            var byAlpha    = alone.Admit(beta.IdentityPublicKey,
                                         beta.CertifySigningKey(OCC, T0, NotAfter: T0.AddYears(1)),
                                         statement,
                                         beta.URL,
                                         CreatedAt:           T0);

            Assert.That(byAlpha.Id,  Is.EqualTo(byBeta.Id), "a change set is identified by its content");

        }

        #endregion

        #region Nobody_is_admitted_twice_or_with_the_keys_of_another()

        /// <summary>
        /// A holder of the keys is no new shareholder; the certificate is one of the signing key of the new shareholder;
        /// an identity key naming no holder admits nobody; and a second holder without a quorum of two is no version.
        /// </summary>
        [Test]
        public void Nobody_is_admitted_twice_or_with_the_keys_of_another()
        {

            var alpha  = Shareholder("Alpha");
            var beta   = Shareholder("Beta");
            var alone  = Begun(alpha);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => alone.Admit(alpha.WithKeys(COSEKeyPair.NewES512(), COSEKeyPair.NewES256()), Statement(1, alpha)),
                            Throws.ArgumentException.With.Message.Contains("'Alpha' holds keys of the roaming network operator 'OCC' already"));

                Assert.That(() => alone.Admit(beta.IdentityPublicKey, alpha.CertifySigningKey(OCC, T0), Statement(2, alpha, beta)),
                            Throws.ArgumentException.With.Message.Contains("is one of 'Alpha', not of 'Beta'"));

                Assert.That(() => alone.Admit(POIPublicKey.From(beta.IdentityKey.Private), beta.CertifySigningKey(OCC, T0), Statement(2, alpha, beta)),
                            Throws.ArgumentException.With.Message.Contains("names no holder"));

                // One of two is not more than half: the key statement must state the quorum the keys need.
                Assert.That(() => alone.Admit(beta, Statement(1, alpha, beta)),
                            Throws.ArgumentException.With.Message.Contains("must be more than half of the 2 holders"));

            }

        }

        #endregion


        #region (static) Joined(Founder, Others)

        /// <summary>
        /// The history of QA begun by the given founder, the given others admitted one after the other: two decide as
        /// soon as there are two, and every commit is signed by all of them.
        /// </summary>
        internal static RoamingNetworkHistory Joined(RoamingShareholder           Founder,
                                                    params RoamingShareholder[]  Others)
        {

            var history       = RoamingNetworkHistory.Start(Begun(Founder), Signers: [ Founder.CommitSignerFor(OCC) ]);
            var shareholders  = new List<RoamingShareholder> { Founder };

            foreach (var joining in Others)
            {

                var before     = shareholders.ToArray();

                shareholders.Add(joining);

                var changeSet  = joining.SignWithIdentityKey(
                                     history.Head.RoamingNetwork.Admit(joining,
                                                                       Statement(2, [ .. shareholders ]),
                                                                       SigningKeyNotAfter:  T0.AddYears(1),
                                                                       CreatedAt:           T0.AddMinutes(shareholders.Count))
                                 );

                foreach (var shareholder in before)
                    changeSet = shareholder.SignWithSigningKey(shareholder.SignWithIdentityKey(changeSet));

                history.Signers = [ .. shareholders.Select(shareholder => shareholder.CommitSignerFor(OCC)) ];

                var joined     = history.Commit(changeSet);

                Assert.That(joined.IsCommitted, Is.True, joined.ErrorResponse);

            }

            return history;

        }

        #endregion

        #region Two_of_three_dismiss_the_third_without_it()

        /// <summary>
        /// Gamma is dismissed by Alpha and Beta, the quorum of the version before: without its signature, and never by
        /// one of them alone. Two of two afterwards, so minConsensus stays; its keys, the certificate of its signing
        /// key and the URL of its copy are gone, and its signing key signs no commit any more: not even this one.
        /// </summary>
        [Test]
        public void Two_of_three_dismiss_the_third_without_it()
        {

            var alpha    = Shareholder("Alpha");
            var beta     = Shareholder("Beta",  COSEKeyPair.NewEd448());
            var gamma    = Shareholder("Gamma", COSEKeyPair.NewMLDsa65());
            var history  = Joined(alpha, beta, gamma);

            var gammaLeaves = history.Head.RoamingNetwork.Dismiss(gamma,
                                                                  Statement(2, alpha, beta),
                                                                  CreatedAt: T0.AddMinutes(10));

            Assert.That(gammaLeaves.Changes.Select(change => change.EntityPropertyName),
                        Is.EqualTo([ POIPartyKeys.IdentityKeysProperty,
                                     RoamingNetworkKeyStatement.JSONProperty,
                                     RoamingNetworkSigningKeyCertificate.JSONProperty,
                                     POINetworkURLs.JSONProperty ]),
                        "two of two after two of three: the quorum stays");

            gammaLeaves     = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(gammaLeaves));

            var byAlphaOnly = history.Commit(gammaLeaves);

            Assert.That(byAlphaOnly.IsCommitted,    Is.False);
            Assert.That(byAlphaOnly.ErrorResponse,  Does.Contain("identity keys of 2 holders"));

            gammaLeaves     = beta.SignWithSigningKey(beta.SignWithIdentityKey(gammaLeaves));

            // The version after this commit certifies the signing key of Gamma no longer: while it still signs here, the
            // commit is refused, as no verifier would take its signature. Nothing is committed, the head stays.
            var head        = history.Head.Commit.Id;
            var withGamma   = history.Commit(gammaLeaves);

            Assert.That(withGamma.IsCommitted,      Is.False);
            Assert.That(withGamma.ErrorResponse,    Does.Contain(gamma.SigningKeyId).And.Contain("a key it takes the role 'roamingNetworkOperator' from"));
            Assert.That(history.Head.Commit.Id,     Is.EqualTo(head));

            history.Signers = [ alpha.CommitSignerFor(OCC), beta.CommitSignerFor(OCC) ];

            var left        = history.Commit(gammaLeaves);

            Assert.That(left.IsCommitted,           Is.True, left.ErrorResponse);

            var both        = history.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(POIPartyKeys.Holders(both.Operator!.IdentityKeys),  Is.EquivalentTo([ "Alpha", "Beta" ]));
                Assert.That(POIPartyKeys.Holders(both.Operator. SigningKeys),   Is.EquivalentTo([ "Alpha", "Beta" ]));
                Assert.That(both.Operator.MinConsensus,                         Is.EqualTo(2));
                Assert.That(both.URLs,                                          Is.EqualTo([ alpha.URL!.Value, beta.URL!.Value ]));
            }

            // Whoever pinned the keys when Alpha was alone follows the key statements, one after the other, to the two.
            Assert.That(RoamingNetworkKeyStatement.TryVerifyChain(history.KeyStatements(),
                                                                  [[ alpha.IdentityPublicKey.Id ]],
                                                                  out var verified,
                                                                  out var holderOf,
                                                                  out var error,
                                                                  At: T0.AddMinutes(11)),
                        Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(history.KeyStatements().Count(),     Is.EqualTo(4));
                Assert.That(verified!.MinConsensus,              Is.EqualTo(2));
                Assert.That(holderOf!.Values.Distinct(),         Is.EquivalentTo([ "Alpha", "Beta" ]));
            }

        }

        #endregion

        #region Of_two_the_one_leaving_signs_as_well()

        /// <summary>
        /// Two deciding together: Beta is not dismissed by Alpha alone, it resigns, signing as well. One of one
        /// afterwards: minConsensus is removed, as one is not written, and Alpha decides alone again.
        /// </summary>
        [Test]
        public void Of_two_the_one_leaving_signs_as_well()
        {

            var alpha    = Shareholder("Alpha");
            var beta     = Shareholder("Beta");
            var history  = Joined(alpha, beta);

            var betaLeaves  = history.Head.RoamingNetwork.Dismiss(beta,
                                                                  Statement(1, alpha),
                                                                  CreatedAt: T0.AddMinutes(10));

            Assert.That(betaLeaves.Changes.Select(change => (change.Kind, change.EntityPropertyName)),
                        Is.EqualTo([ (RoamingNetworkChangeKind.UpdateProperty,  POIPartyKeys.IdentityKeysProperty),
                                     (RoamingNetworkChangeKind.UpdateProperty,  RoamingNetworkKeyStatement.JSONProperty),
                                     (RoamingNetworkChangeKind.UpdateProperty,  RoamingNetworkSigningKeyCertificate.JSONProperty),
                                     (RoamingNetworkChangeKind.RemoveProperty,  POIPartyKeys.MinConsensusProperty),
                                     (RoamingNetworkChangeKind.UpdateProperty,  POINetworkURLs.JSONProperty) ]));

            betaLeaves      = alpha.SignWithSigningKey(alpha.SignWithIdentityKey(betaLeaves));

            var byAlphaOnly = history.Commit(betaLeaves);

            Assert.That(byAlphaOnly.IsCommitted,    Is.False, "by the one staying alone");
            Assert.That(byAlphaOnly.ErrorResponse,  Does.Contain("identity keys of 2 holders"));

            history.Signers = [ alpha.CommitSignerFor(OCC) ];

            var left        = history.Commit(beta.SignWithSigningKey(beta.SignWithIdentityKey(betaLeaves)));

            Assert.That(left.IsCommitted,           Is.True, left.ErrorResponse);

            var alone       = history.Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(POIPartyKeys.Holders(alone.Operator!.IdentityKeys),                       Is.EquivalentTo([ "Alpha" ]));
                Assert.That(alone.Operator.MinConsensus,                                              Is.EqualTo(1));
                Assert.That(alone.Operator.ToJSON().ContainsKey(POIPartyKeys.MinConsensusProperty),   Is.False, "written unless one");
                Assert.That(alone.URLs,                                                               Is.EqualTo([ alpha.URL!.Value ]));
            }

        }

        #endregion

        #region Nobody_is_dismissed_who_holds_no_keys_and_never_the_last_one()

        /// <summary>
        /// Only a holder of the keys is dismissed, never the last one, and only with a URL the roaming network names.
        /// </summary>
        [Test]
        public void Nobody_is_dismissed_who_holds_no_keys_and_never_the_last_one()
        {

            var alpha  = Shareholder("Alpha");
            var beta   = Shareholder("Beta");
            var alone  = Begun(alpha);
            var both   = Joined(alpha, beta).Head.RoamingNetwork;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => both. Dismiss("Gamma", Statement(2, alpha, beta)),
                            Throws.ArgumentException.With.Message.Contains("'Gamma' holds no keys of the roaming network operator 'OCC'"));

                Assert.That(() => alone.Dismiss(alpha, Statement(1, alpha)),
                            Throws.ArgumentException.With.Message.Contains("is the last one holding keys"));

                Assert.That(() => both. Dismiss("Beta", Statement(1, alpha), URL.Parse($"https://elsewhere.example/RNs/{QA}")),
                            Throws.ArgumentException.With.Message.Contains("does not name the URL"));

                // The key statement must state the quorum the keys that stay need: two of one is none.
                Assert.That(() => both. Dismiss(beta, Statement(2, alpha, beta)),
                            Throws.ArgumentException);

            }

        }

        #endregion

    }

}
