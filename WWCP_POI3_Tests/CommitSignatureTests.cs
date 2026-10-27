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
using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// Commits signed by the leader and by others, e.g. a roaming network operator
    /// that received the data of a charging station operator and checked it.
    /// </summary>
    [TestFixture]
    public class CommitSignatureTests
    {

        #region Data

        internal static readonly Ed25519PrivateKeyParameters  LeaderKey  = new (Convert.FromHexString("101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f"), 0);
        internal static readonly Ed25519PrivateKeyParameters  RNOKey     = new (Convert.FromHexString("202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"), 0);
        internal static readonly Ed25519PrivateKeyParameters  CSOKey     = new (Convert.FromHexString("303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f"), 0);

        private  static readonly DateTimeOffset               T0         = DateTimeOffset.Parse("2026-10-12T12:00:00Z");

        /// <summary>
        /// The trusted public keys by their key identifications.
        /// </summary>
        internal static readonly Dictionary<String, AsymmetricKeyParameter> TrustedKeys = new () {
                                                                                  { "leader", LeaderKey.GeneratePublicKey() },
                                                                                  { "rno",    RNOKey.   GeneratePublicKey() }
                                                                              };

        /// <summary>
        /// Verifies commit signatures with the trusted keys, whatever the version.
        /// </summary>
        internal static readonly VerifyCommitSignatureDelegate VerifyCommit = (commit, version, signature) =>
            TrustedKeys.TryGetValue(signature.KeyId, out var publicKey) &&
            commit.VerifySignature(signature, publicKey, signature.KeyId, out _);

        /// <summary>
        /// Verifies commit signatures with the signing keys the roaming network operator certifies in the given version only.
        /// </summary>
        internal static readonly VerifyCommitSignatureDelegate VerifyByCertifiedKeys = (commit, version, signature) =>
            version.Operator?.SigningKeyCertificates.FirstOrDefault(certificate => certificate.SigningKey.Id == signature.KeyId) is RoamingNetworkSigningKeyCertificate certificate &&
            commit.VerifySignature(signature, certificate.SigningKey.Key, certificate.SigningKey.Id, out _);

        #endregion


        #region (internal static) LeaderSigner

        internal static RoamingNetworkCommitSigner LeaderSigner

            => new (LeaderKey, "leader", COSEAlgorithm.Ed25519, Signer: "leader.example.org");

        #endregion

        #region (private static) NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    DataSource:  "v0",
                    Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")).WithKeys();

        #endregion

        #region (private static) Publish(History, Value)

        /// <summary>
        /// Publish a change of the data source, signed by the roaming network operator.
        /// </summary>
        private static RoamingNetworkCommit Publish(RoamingNetworkHistory  History,
                                                    String                 Value)
        {

            var changeSet = History.Head.RoamingNetwork.CreateSignedChangeSet(
                                [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                                CreatedAt: T0.AddMinutes((Double) History.Head.Commit.Revision)
                            );

            var result = History.Commit(changeSet);

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region (private static) RNOSignature(Commit, Role = roamingNetworkOperator, Key = null)

        private static RoamingNetworkCommitSignature RNOSignature(RoamingNetworkCommit  Commit,
                                                                  String                Role   = RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                                  AsymmetricKeyParameter? Key  = null)

            => Commit.CreateSignature(Key ?? RNOKey,
                                      "rno",
                                      COSEAlgorithm.Ed25519,
                                      Role,
                                      Signer:    "DE*RNO",
                                      SignedAt:  T0.AddHours(1));

        #endregion


        #region A_leader_signs_every_commit()

        [Test]
        public void A_leader_signs_every_commit()
        {

            var leader     = RoamingNetworkHistory.Start(NewNetwork(), [ LeaderSigner ]);
            var commit     = Publish(leader, "v1");

            var json       = commit.ToJSON();
            var unsigned   = (JObject) json.DeepClone();
            unsigned.Remove("signatures");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(leader.Root.Signatures.Select(signature => signature.Role),    Is.EqualTo(new[] { "leader" }));
                Assert.That(commit.Signatures.Select(signature => signature.Role),         Is.EqualTo(new[] { "leader" }));
                Assert.That(commit.Signatures[0].Signer,                                   Is.EqualTo("leader.example.org"));
                Assert.That(VerifyCommit(leader.Root, NewNetwork(), leader.Root.Signatures[0]),           Is.True);
                Assert.That(VerifyCommit(commit,      leader.Head.RoamingNetwork, commit.Signatures[0]),  Is.True);
                Assert.That(leader.Head.Commit,                                            Is.SameAs(commit));

                // Signatures are not part of the content the identification is calculated over.
                Assert.That(RoamingNetworkCommit.TryParse(unsigned, out var unsignedCommit, out var unsignedError),  Is.True, unsignedError);
                Assert.That(unsignedCommit!.Id,                                            Is.EqualTo(commit.Id));
                Assert.That(unsignedCommit.Signatures,                                     Is.Empty);

                // JSON and CBOR keep them.
                Assert.That(RoamingNetworkCommit.TryParse(json.ToString(), out var fromJSON, out var jsonError),     Is.True, jsonError);
                Assert.That(RoamingNetworkCommit.TryParse(commit.ToCBOR(), out var fromCBOR, out var cborError),     Is.True, cborError);
                Assert.That(fromJSON!.Signatures,                                          Is.EqualTo(commit.Signatures));
                Assert.That(fromCBOR!.Signatures,                                          Is.EqualTo(commit.Signatures));
                Assert.That(VerifyCommit(fromCBOR, leader.Head.RoamingNetwork, fromCBOR.Signatures[0]),  Is.True);

            }

        }

        #endregion

        #region A_signature_covers_the_commit_and_the_signatures_of_its_change_set()

        [Test]
        public void A_signature_covers_the_commit_and_the_signatures_of_its_change_set()
        {

            var leader    = RoamingNetworkHistory.Start(NewNetwork());
            var commit    = Publish(leader, "v1");
            var signed    = commit.WithSignatures([ RNOSignature(commit) ]);
            var json      = signed.ToJSON();

            // Another signature of the change set does not change the commit identification...
            var forgedChangeSet = (JObject) json.DeepClone();
            forgedChangeSet["changeSet"]!["signatures"]![0]!["value"] = Convert.ToBase64String(new Byte[64]);

            // ...but the role, the signer and the signing time are signed, too.
            var forgedRole      = (JObject) json.DeepClone();
            forgedRole["signatures"]![0]!["role"]      = RoamingNetworkCommitSignatureRoles.Leader;

            var forgedSigner    = (JObject) json.DeepClone();
            forgedSigner["signatures"]![0]!["signer"]  = "DE*XYZ";

            var forgedTime      = (JObject) json.DeepClone();
            forgedTime["signatures"]![0]!["signedAt"]  = "2026-10-12T14:00:00.000Z";

            var otherFormat     = (JObject) json.DeepClone();
            otherFormat["signatures"]![0]!["signedAt"] = "2026-10-12T13:00:00Z";

            Boolean Verifies(JObject JSON)
                => RoamingNetworkCommit.TryParse(JSON, out var parsed, out var error) &&
                   VerifyCommit(parsed, leader.Head.RoamingNetwork, parsed.Signatures[0]);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Verifies(json),                                                                Is.True);
                Assert.That(RoamingNetworkCommit.TryParse(forgedChangeSet, out var forged, out _),         Is.True);
                Assert.That(forged!.Id,                                                                    Is.EqualTo(commit.Id));
                Assert.That(VerifyCommit(forged, leader.Head.RoamingNetwork, forged.Signatures[0]),        Is.False);
                Assert.That(Verifies(forgedRole),                                                          Is.False);
                Assert.That(Verifies(forgedSigner),                                                        Is.False);
                Assert.That(Verifies(forgedTime),                                                          Is.False);

                Assert.That(RoamingNetworkCommit.TryParse(otherFormat, out _, out var formatError),        Is.False);
                Assert.That(formatError,                                                                   Does.Contain("yyyy-MM-ddTHH:mm:ss.fffZ"));

                // A signature of another key under the trusted key identification.
                Assert.That(VerifyCommit(commit, leader.Head.RoamingNetwork, RNOSignature(commit, Key: CSOKey)),  Is.False);

            }

        }

        #endregion

        #region Signatures_are_added_later()

        [Test]
        public void Signatures_are_added_later()
        {

            var leader      = RoamingNetworkHistory.Start(NewNetwork(), [ LeaderSigner ]);
            var commit      = Publish(leader, "v1");
            var told        = new List<(RoamingNetworkCommit Commit, IReadOnlyList<RoamingNetworkCommitSignature> NewSignatures)>();

            leader.OnCommitSignatures += (commit, newSignatures) => told.Add((commit, newSignatures));

            var signature   = RNOSignature(commit);

            var noVerifier  = leader.TryAddSignatures(commit.Id, [ signature ],                        null,         out _,          out _,           out var noVerifierError);
            var invalid     = leader.TryAddSignatures(commit.Id, [ RNOSignature(commit, Key: CSOKey) ], VerifyCommit, out _,          out _,           out var invalidError);
            var unknown     = leader.TryAddSignatures(leader.Root.ParentId ?? RoamingNetworkCommit_Id.Parse(new String('0', 64)),
                                                                 [ signature ],                        VerifyCommit, out _,          out _,           out var unknownError);
            var added       = leader.TryAddSignatures(commit.Id, [ signature ],                        VerifyCommit, out var signed, out var newOnes, out var addedError);
            var again       = leader.TryAddSignatures(commit.Id, [ signature ],                        VerifyCommit, out _,          out var noneNew, out _);

            leader.TryGetCommit(commit.Id, out var stored);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(noVerifier,                                          Is.False);
                Assert.That(noVerifierError,                                     Does.Contain("no verifier"));
                Assert.That(invalid,                                             Is.False);
                Assert.That(invalidError,                                        Does.Contain("is invalid"));
                Assert.That(unknown,                                             Is.False);
                Assert.That(unknownError,                                        Does.Contain("unknown"));

                Assert.That(added,                                               Is.True, addedError);
                Assert.That(newOnes,                                             Has.Count.EqualTo(1));
                Assert.That(again,                                               Is.True);
                Assert.That(noneNew,                                             Is.Empty);

                Assert.That(signed!.Id,                                          Is.EqualTo(commit.Id));
                Assert.That(signed.Signatures.Select(signature => signature.Role),
                            Is.EqualTo(new[] { "leader", "roamingNetworkOperator" }));
                Assert.That(leader.Head.Commit,                                  Is.SameAs(signed));
                Assert.That(stored,                                              Is.SameAs(signed));
                Assert.That(told.Select(t => t.NewSignatures.Count),             Is.EqualTo(new[] { 1 }));

                // Every signature on its own: the one of the leader stays valid.
                Assert.That(signed.Signatures.All(signature => VerifyCommit(signed, leader.Head.RoamingNetwork, signature)),  Is.True);

            }

        }

        #endregion

        #region Followers_check_the_signatures_of_commits()

        [Test]
        public void Followers_check_the_signatures_of_commits()
        {

            var leader        = RoamingNetworkHistory.Start(NewNetwork(), [ LeaderSigner ]);

            RoamingNetworkHistory NewFollower()
            {
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out var error), Is.True, error);
                return follower!;
            }

            var follower1     = NewFollower();
            var follower2     = NewFollower();
            var follower3     = NewFollower();

            var commit        = Publish(leader, "v1");

            var noVerifier    = follower1.TryAppend(commit, out var noVerifierError);
            var missingRole   = follower2.TryAppend(commit, out var missingRoleError, VerifyCommitSignature: VerifyCommit, RequiredSignatureRoles: [ "leader", "roamingNetworkOperator" ]);
            var appended      = follower3.TryAppend(commit, out var appendedError,    VerifyCommitSignature: VerifyCommit, RequiredSignatureRoles: [ "leader" ]);

            // Signed later by the roaming network operator: a commit held already brings its new signatures.
            leader.TryAddSignatures(commit.Id, [ RNOSignature(commit) ], VerifyCommit, out var signed, out _, out _);

            var again         = follower3.TryAppend(signed!, out var againError, VerifyCommitSignature: VerifyCommit);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(noVerifier,                                  Is.False);
                Assert.That(noVerifierError,                             Does.Contain("no verifier"));
                Assert.That(missingRole,                                 Is.False);
                Assert.That(missingRoleError,                            Does.Contain("not signed in the role 'roamingNetworkOperator'"));
                Assert.That(appended,                                    Is.True, appendedError);
                Assert.That(again,                                       Is.True, againError);

                Assert.That(follower1.Head.Commit.Revision,              Is.Zero);
                Assert.That(follower3.Head.Commit.Id,                    Is.EqualTo(signed!.Id));
                Assert.That(follower3.Head.Commit.Signatures,            Has.Length.EqualTo(2));

                Assert.That(RoamingNetworkHistory.CheckSignatures(follower3.Head.Commit, follower3.Head.RoamingNetwork, VerifyCommit, [ "leader", "roamingNetworkOperator" ], out var checkError),
                            Is.True, checkError);

            }

        }

        #endregion

        #region A_verifier_is_given_the_version_a_commit_leads_to()

        [Test]
        public void A_verifier_is_given_the_version_a_commit_leads_to()
        {

            // Keeping the versions of the root and the head only: those of older commits are calculated again.
            var leader     = RoamingNetworkHistory.Start(NewNetwork(),
                                                         [ LeaderSigner ],
                                                         Retention: new RoamingNetworkHistoryRetention {
                                                                        VersionCheckpointInterval  = 0,
                                                                        RecentVersions             = 1
                                                                    });

            RoamingNetworkHistory NewFollower()
            {
                Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out var error), Is.True, error);
                return follower!;
            }

            var follower1  = NewFollower();
            var follower2  = NewFollower();

            var commit1    = Publish(leader, "v1");
            var commit2    = Publish(leader, "v2");
            var commit3    = Publish(leader, "v3");

            var given      = new List<(RoamingNetworkCommit_Id CommitId, String? DataSource, Boolean HasItsETags)>();

            Boolean Recording(RoamingNetworkCommit Commit, RoamingNetwork Version, RoamingNetworkCommitSignature Signature)
            {
                given.Add((Commit.Id, Version.DataSource, Version.MatchesCommitETags(Commit.ETags)));
                return VerifyCommit(Commit, Version, Signature);
            }

            // a) Appended: each one in the version calculated for it, neither in the head before nor in the one after.
            var appended    = follower1.TryAppend([ commit1, commit2, commit3 ], out var count, out var appendError, VerifyCommitSignature: Recording, RequiredSignatureRoles: [ "leader" ]);
            var ofAppended  = given.ToArray();
            given.Clear();

            // b) Signatures added later to local commits: to the head, and to an older one, whose version is not kept.
            var toHead      = leader.TryAddSignatures(commit3.Id, [ RNOSignature(commit3) ], Recording, out _,           out _, out var toHeadError);
            var toOlder     = leader.TryAddSignatures(commit1.Id, [ RNOSignature(commit1) ], Recording, out var signed1, out _, out var toOlderError);
            var ofAdded     = given.ToArray();
            given.Clear();

            // ...and to a commit held already, appended again with them.
            var again       = follower1.TryAppend(signed1!, out var againError, VerifyCommitSignature: Recording);
            var ofAgain     = given.ToArray();
            given.Clear();

            // c) The head commit of a snapshot: in the version of the snapshot, the only one it has.
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var fromSnapshot, out var snapshotError), Is.True, snapshotError);

            var ofSnapshot  = RoamingNetworkHistory.CheckSignatures(fromSnapshot!.Head.Commit, fromSnapshot.Head.RoamingNetwork, Recording, [ "leader", "roamingNetworkOperator" ], out var snapshotCheckError);
            var ofHead      = given.ToArray();
            given.Clear();

            // d) A commit with an invalid signature is refused once its version is calculated, and nothing is kept of it.
            var forged      = follower2.TryAppend(commit1.WithSignatures([ RNOSignature(commit1, Key: CSOKey) ]), out var forgedError, VerifyCommitSignature: Recording);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(appended,                                    Is.True, appendError);
                Assert.That(count,                                       Is.EqualTo(3));
                Assert.That(ofAppended,                                  Is.EqualTo(new[] { (commit1.Id, (String?) "v1", true),
                                                                                            (commit2.Id, (String?) "v2", true),
                                                                                            (commit3.Id, (String?) "v3", true) }));

                Assert.That(toHead,                                      Is.True, toHeadError);
                Assert.That(toOlder,                                     Is.True, toOlderError);
                Assert.That(ofAdded,                                     Is.EqualTo(new[] { (commit3.Id, (String?) "v3", true),
                                                                                            (commit1.Id, (String?) "v1", true) }));

                Assert.That(again,                                       Is.True, againError);
                Assert.That(ofAgain,                                     Is.EqualTo(new[] { (commit1.Id, (String?) "v1", true) }));

                Assert.That(ofSnapshot,                                  Is.True, snapshotCheckError);
                Assert.That(ofHead,                                      Is.EqualTo(new[] { (commit3.Id, (String?) "v3", true),
                                                                                            (commit3.Id, (String?) "v3", true) }));

                Assert.That(forged,                                      Is.False);
                Assert.That(forgedError,                                 Does.Contain("is invalid"));
                Assert.That(follower2.Contains(commit1.Id),              Is.False);
                Assert.That(follower2.Head.Commit.Revision,              Is.Zero);

            }

        }

        #endregion

        #region A_signing_key_a_later_commit_removes_still_verifies_the_commits_it_signed()

        [Test]
        public void A_signing_key_a_later_commit_removes_still_verifies_the_commits_it_signed()
        {

            var network   = NewNetwork();
            var leader    = RoamingNetworkHistory.Start(network, [ network.OperatorSigner() ]);

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(leader.ToSnapshotJSON(), out var follower, out var snapshotError), Is.True, snapshotError);

            var first     = Publish(leader, "v1");

            // The operator replaces its signing key by its next one, certified by its identity key, and signs with it from
            // the commit replacing it on: that commit is signed by a key its parent does not name.
            var rno       = leader.Head.RoamingNetwork.Operator!;
            var identity  = TestParties.Key(TestParties.Party(network), POIKeyKind.Identity);
            var next      = TestParties.Key(new POIPartyId(nameof(RoamingNetworkOperator), $"{rno.Id}/next"));

            leader.Signers = [ new RoamingNetworkCommitSigner(next.Private, next.Public.Id, COSEAlgorithm.ES256, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator) ];

            var rotated   = leader.Commit(leader.Head.RoamingNetwork.CreateSignedChangeSet(
                                [ RoamingNetworkChange.UpdateProperty(rno,
                                                                      RoamingNetworkSigningKeyCertificate.JSONProperty,
                                                                      null,
                                                                      new JArray(RoamingNetworkSigningKeyCertificate.Create(identity.Private, COSEAlgorithm.ES512, rno.Id, next.Public, T0).ToJSON(Embedded: true))) ],
                                CreatedAt: T0.AddMinutes((Double) leader.Head.Commit.Revision)
                            ));

            Assert.That(rotated.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), rotated.ErrorResponse);

            // Its change sets are signed by the next key from then on, as well.
            var published = leader.Commit(leader.Head.RoamingNetwork.CreateChangeSet(
                                [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, "v3") ],
                                CreatedAt: T0.AddMinutes((Double) leader.Head.Commit.Revision)
                            ).Sign(next.Private, next.Public.Id, COSEAlgorithm.ES256));

            Assert.That(published.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), published.ErrorResponse);

            var second    = rotated.Commit!;
            var third     = published.Commit!;

            // Appended in their order, each checked by the signing keys certified in its own version only.
            var appended  = follower!.TryAppend([ first, second, third ],
                                                out var count,
                                                out var appendError,
                                                VerifyCommitSignature:   VerifyByCertifiedKeys,
                                                RequiredSignatureRoles:  [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(appended,                                                                Is.True, appendError);
                Assert.That(count,                                                                   Is.EqualTo(3));
                Assert.That(follower.Head.Commit.Id,                                                 Is.EqualTo(third.Id));

                Assert.That(first. Signatures.Single().KeyId,                                        Is.EqualTo(TestParties.Key(TestParties.Party(network)).Public.Id));
                Assert.That(second.Signatures.Single().KeyId,                                        Is.EqualTo(next.Public.Id));
                Assert.That(third. Signatures.Single().KeyId,                                        Is.EqualTo(next.Public.Id));

                // In the head, the key of the first commit is certified no longer; before the second, the next one was not yet.
                Assert.That(VerifyByCertifiedKeys(first,  follower.Head.RoamingNetwork, first. Signatures[0]),  Is.False);
                Assert.That(VerifyByCertifiedKeys(second, network,                      second.Signatures[0]),  Is.False);

            }

        }

        #endregion

    }

}
