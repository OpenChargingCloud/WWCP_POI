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
    /// Checkpoints: one line of signed statements over the graph of commits, made by the roaming network operator,
    /// signed by witnesses, with proofs of single commits (SPECIFICATION.md, section 10).
    /// </summary>
    [TestFixture]
    public class CheckpointTests
    {

        #region Data

        private static readonly Ed25519PrivateKeyParameters  RNOKey      = new (Convert.FromHexString("404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f"), 0);
        private static readonly Ed25519PrivateKeyParameters  WitnessKey  = new (Convert.FromHexString("505152535455565758595a5b5c5d5e5f606162636465666768696a6b6c6d6e6f"), 0);

        private static readonly DateTimeOffset               T0          = DateTimeOffset.Parse("2026-10-19T12:00:00Z");

        private static readonly Dictionary<String, AsymmetricKeyParameter> TrustedKeys = new () {
                                                                                 { "rno",      RNOKey.    GeneratePublicKey() },
                                                                                 { "witness",  WitnessKey.GeneratePublicKey() }
                                                                             };

        internal static readonly VerifyCheckpointSignatureDelegate Verify = (checkpoint, signature) =>
            TrustedKeys.TryGetValue(signature.KeyId, out var publicKey) &&
            checkpoint.VerifySignature(signature, publicKey, signature.KeyId, out _);

        internal static RoamingNetworkCommitSigner RNOSigner
            => new (RNOKey,     "rno",     COSEAlgorithm.Ed25519, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, "DE*RNO");

        internal static RoamingNetworkCommitSigner WitnessSigner
            => new (WitnessKey, "witness", COSEAlgorithm.Ed25519, RoamingNetworkCommitSignatureRoles.Witness, "witness.example.org");

        /// <summary>
        /// The witness of the given journal, a new one in memory otherwise.
        /// </summary>
        internal static RoamingNetworkWitness Witness(RoamingNetworkWitnessJournal? Journal = null)
            => new (WitnessSigner, Journal ?? new RoamingNetworkWitnessJournal());

        #endregion


        #region (private static) NewHistory(Store = null, Retention = null)

        private static RoamingNetworkHistory NewHistory(IRoamingNetworkHistoryStore?     Store       = null,
                                                        RoamingNetworkHistoryRetention?  Retention   = null)
        {

            var history = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                                         DataSource:  "v0",
                                                                         Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")).WithKeys(),
                                                      Store:      Store,
                                                      Retention:  Retention);

            history.CheckpointSigners = [ RNOSigner ];

            return history;

        }

        #endregion

        #region (private static) Commit(History, Value)

        private static RoamingNetworkCommit Commit(RoamingNetworkHistory  History,
                                                   String                 Value)
        {

            var result = History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(
                                            [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                                            CreatedAt: T0.AddMinutes((Double) History.Head.Commit.Revision)
                                        ));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            return result.Commit!;

        }

        #endregion

        #region (private static) Checkpoint(History, Minutes)

        private static RoamingNetworkCheckpoint Checkpoint(RoamingNetworkHistory History, Double Minutes)
        {
            Assert.That(History.TryMakeCheckpoint(out var checkpoint, out var error, CreatedAt: T0.AddMinutes(Minutes)), Is.True, error);
            return checkpoint!;
        }

        #endregion

        #region (private static) Follower(Leader)

        /// <summary>
        /// A history of another server holding the same commits, started from the genesis.
        /// </summary>
        private static RoamingNetworkHistory Follower(RoamingNetworkHistory Leader)
        {

            var follower = NewHistory();

            Assert.That(follower.Root.Id,                                                                            Is.EqualTo(Leader.Root.Id));
            Assert.That(Leader.TryGetCommitsSince(Leader.Root.Id, out var commits, out var error),                   Is.True, error);
            Assert.That(follower.TryAppend(commits!, out _, out error),                                              Is.True, error);

            return follower;

        }

        #endregion


        #region The_Merkle_tree_is_the_one_of_RFC_9162()

        [Test]
        public void The_Merkle_tree_is_the_one_of_RFC_9162()
        {

            // The leaves and roots of the test vectors of Certificate Transparency (RFC 6962, RFC 9162).
            Byte[][] leaves = [ [], [ 0x00 ], [ 0x10 ], [ 0x20, 0x21 ], [ 0x30, 0x31 ], [ 0x40, 0x41, 0x42, 0x43 ],
                                [ 0x50, 0x51, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57 ],
                                [ 0x60, 0x61, 0x62, 0x63, 0x64, 0x65, 0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0x6e, 0x6f ] ];

            String[] roots  = [ "6e340b9cffb37a989ca544e6bb780a2c78901d3fb33738768511a30617afa01d",
                                "fac54203e7cc696cf0dfcb42c92a1d9dbaf70ad9e621f4bd8d98662f00e3c125",
                                "aeb6bcfe274b70a14fb067a5e5578264db0fa9b51af5e0ba159158f329e06e77",
                                "d37ee418976dd95753c1c73862b9398fa2a2cf9b4ff0fdfe8b30cd95209614b7",
                                "4e3bbb1f7b478dcfe71fb631631519a3bca12c9aefca1612bfce4c13a86264d4",
                                "76e67dadbcdf1e10e1b74ddc608abd2f98dfb16fbce75277b5232a127f2087ef",
                                "ddb89be403809e325750d3d263cd78929c2942b7942a34b77e122c9594a74c8c",
                                "5dc9da79a70659a9ad559cb701ded9a2ab9d823aad2f4960cfe370eff4604328" ];

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Convert.ToHexStringLower(POIMerkleTree.RootHash([])),
                            Is.EqualTo("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855"));

                for (var size = 1; size <= leaves.Length; size++)
                {

                    var tree = leaves.Take(size).ToArray();
                    var root = POIMerkleTree.RootHash(tree);

                    Assert.That(Convert.ToHexStringLower(root), Is.EqualTo(roots[size - 1]), $"size {size}");

                    for (var index = 0; index < size; index++)
                    {

                        var path = POIMerkleTree.AuditPath(tree, index);

                        Assert.That(POIMerkleTree.TryCalculateRoot(tree[index], (UInt64) index, (UInt64) size, path, out var calculated) &&
                                    calculated.SequenceEqual(root),
                                    Is.True,
                                    $"leaf {index} of {size}");

                        // Another leaf, index or size leads elsewhere.
                        Assert.That(POIMerkleTree.TryCalculateRoot([ 0xff ], (UInt64) index, (UInt64) size, path, out var other) &&
                                    other.SequenceEqual(root),
                                    Is.False);

                        if (size > 1)
                            Assert.That(POIMerkleTree.TryCalculateRoot(tree[index], (UInt64) ((index + 1) % size), (UInt64) size, path, out var moved) &&
                                        moved.SequenceEqual(root),
                                        Is.False,
                                        $"leaf {index} of {size} at another index");

                    }

                }

            }

        }

        #endregion

        #region Checkpoints_form_a_line_over_the_commits()

        [Test]
        public void Checkpoints_form_a_line_over_the_commits()
        {

            var history  = NewHistory();
            var c1       = Commit(history, "v1");
            var c2       = Commit(history, "v2");
            var c3       = Commit(history, "v3");

            var cp0      = Checkpoint(history, 10);
            var again    = history.TryMakeCheckpoint(out _, out var againError, CreatedAt: T0.AddMinutes(11));

            var c4       = Commit(history, "v4");
            var c5       = Commit(history, "v5");
            var cp1      = Checkpoint(history, 20);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(cp0.Sequence,                                           Is.Zero);
                Assert.That(cp0.PreviousId,                                         Is.Null);
                Assert.That(cp0.CommitId,                                           Is.EqualTo(c3.Id));
                Assert.That(cp0.Revision,                                           Is.EqualTo(c3.Revision));
                Assert.That(cp0.ETags,                                              Is.EqualTo(c3.ETags));
                Assert.That(cp0.CommitsSize,                                        Is.EqualTo(4), "the genesis and three commits");
                Assert.That(cp0.CommitsRoot,
                            Is.EqualTo(RoamingNetworkCheckpoint.CommitsRootOf(RoamingNetworkCheckpoint.InOrder([ history.Root, c1, c2, c3 ]))));

                Assert.That(again,                                                  Is.False);
                Assert.That(againError,                                             Does.Contain("already"));

                Assert.That(cp1.Sequence,                                           Is.EqualTo(1));
                Assert.That(cp1.PreviousId,                                         Is.EqualTo(cp0.Id));
                Assert.That(cp1.CommitId,                                           Is.EqualTo(c5.Id));
                Assert.That(cp1.CommitsSize,                                        Is.EqualTo(2), "the two commits since");

                Assert.That(history.Checkpoints.Select(checkpoint => checkpoint.Id), Is.EqualTo(new[] { cp0.Id, cp1.Id }));
                Assert.That(history.LatestCheckpoint,                               Is.SameAs(cp1));

                // Signed by the roaming network operator; the identification without the signatures.
                Assert.That(cp1.Signatures.Select(signature => signature.Role),     Is.EqualTo(new[] { "roamingNetworkOperator" }));
                Assert.That(Verify(cp1, cp1.Signatures[0]),                         Is.True);
                Assert.That(cp1.WithSignatures([]).Id,                              Is.EqualTo(cp1.Id));

                // JSON and CBOR give the same checkpoint again; another content is another identification.
                Assert.That(RoamingNetworkCheckpoint.TryParse(cp1.ToJSON().ToString(), out var fromJSON, out var jsonError),  Is.True, jsonError);
                Assert.That(RoamingNetworkCheckpoint.TryParse(cp1.ToCBOR(),            out var fromCBOR, out var cborError),  Is.True, cborError);
                Assert.That(fromJSON!.Id,                                           Is.EqualTo(cp1.Id));
                Assert.That(fromCBOR!.Signatures,                                   Is.EqualTo(cp1.Signatures));
                Assert.That(Verify(fromCBOR, fromCBOR.Signatures[0]),               Is.True);

                var forged = cp1.ToJSON();
                forged["commits"]!["@size"] = 3;
                Assert.That(RoamingNetworkCheckpoint.TryParse(forged, out _, out var forgedError),  Is.False);
                Assert.That(forgedError,                                            Does.Contain("is not the one of its content"));

            }

        }

        #endregion

        #region Commits_are_proved_to_be_part_of_a_checkpoint()

        [Test]
        public void Commits_are_proved_to_be_part_of_a_checkpoint()
        {

            var history  = NewHistory();
            var c1       = Commit(history, "v1");
            var c2       = Commit(history, "v2");
            var cp0      = Checkpoint(history, 10);
            var c3       = Commit(history, "v3");
            var cp1      = Checkpoint(history, 20);
            var c4       = Commit(history, "v4");

            Assert.That(history.TryGetCommitProof(c1.Id, out var proof1, out var error1),  Is.True, error1);
            Assert.That(history.TryGetCommitProof(c3.Id, out var proof3, out var error3),  Is.True, error3);

            var notYet   = history.TryGetCommitProof(c4.Id, out _, out var notYetError);
            var wrong    = history.TryGetCommitProof(c1.Id, out _, out var wrongError, cp1.Id);

            Assert.That(RoamingNetworkCheckpointProof.TryParse(proof1!.ToJSON(),            out var parsed,   out var parseError),  Is.True, parseError);
            Assert.That(RoamingNetworkCheckpointProof.TryParse(proof1.ToJSON().ToString(),  out var fromText, out var textError),   Is.True, textError);

            var forged   = proof1.ToJSON();
            forged["leaf"]!["revision"] = 7;
            RoamingNetworkCheckpointProof.TryParse(forged, out var forgedProof, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(proof1.CheckpointId,                                    Is.EqualTo(cp0.Id));
                Assert.That(proof1.Size,                                            Is.EqualTo(3));
                Assert.That(proof1.Verify(cp0, out var verifyError1),               Is.True, verifyError1);
                Assert.That(proof3!.CheckpointId,                                   Is.EqualTo(cp1.Id));
                Assert.That(proof3.Verify(cp1, out var verifyError3),               Is.True, verifyError3);

                Assert.That(parsed!.Verify(cp0, out var parsedError),               Is.True, parsedError);
                Assert.That(fromText!.Verify(cp0, out var fromTextError),           Is.True, fromTextError);
                Assert.That(proof1.Verify(cp1, out _),                              Is.False, "another checkpoint");
                Assert.That(forgedProof!.Verify(cp0, out var forgedError),          Is.False);
                Assert.That(forgedError,                                            Does.Contain("does not lead"));

                Assert.That(notYet,                                                 Is.False);
                Assert.That(notYetError,                                            Does.Contain("No checkpoint"));
                Assert.That(wrong,                                                  Is.False);
                Assert.That(wrongError,                                             Does.Contain("not one of the commits"));

                Assert.That(c2.Id,                                                  Is.Not.EqualTo(c1.Id));

            }

        }

        #endregion

        #region Signatures_are_verified_with_a_COSE_key_and_its_algorithm_restriction()

        [Test]
        public void Signatures_are_verified_with_a_COSE_key_and_its_algorithm_restriction()
        {

            var history     = NewHistory();
            Commit(history, "v1");
            var checkpoint  = Checkpoint(history, 10);
            var signature   = checkpoint.Signatures.Single();
            var publicKey   = RNOKey.GeneratePublicKey();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(checkpoint.VerifySignature(signature, COSEKey.From(publicKey),                                    "rno", out var error),       Is.True, error);
                Assert.That(checkpoint.VerifySignature(signature, COSEKey.From(publicKey, Algorithm: COSEAlgorithm.Ed25519),  "rno", out error),           Is.True, error);

                // A key restricted to another algorithm, and a private key, verify nothing.
                Assert.That(checkpoint.VerifySignature(signature, COSEKey.From(publicKey, Algorithm: COSEAlgorithm.EdDSA),    "rno", out var restricted),  Is.False);
                Assert.That(restricted,                                                                                                                      Does.Contain("algorithm restriction"));
                Assert.That(checkpoint.VerifySignature(signature, COSEKey.From(RNOKey),                                       "rno", out var privateKey),  Is.False);
                Assert.That(privateKey,                                                                                                                      Does.Contain("public COSE key"));

            }

        }

        #endregion

        #region Witnesses_check_and_sign_checkpoints()

        [Test]
        public void Witnesses_check_and_sign_checkpoints()
        {

            var leader    = NewHistory();
            Commit(leader, "v1");
            var cp0       = Checkpoint(leader, 10);
            Commit(leader, "v2");
            var cp1       = Checkpoint(leader, 20);

            var witness   = Follower(leader);

            var unsigned  = witness.TryAddCheckpoint(cp0, out var unsignedError);
            var taken0    = witness.TryAddCheckpoint(cp0, out var error0, Verify, [ RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator ]);
            var tooEarly  = witness.TryAddCheckpoint(RoamingNetworkCheckpoint.Create(cp1.RoamingNetworkId, 1, 2, cp1.Id, leader.Head.Commit, [ leader.Head.Commit ], T0), out var tooEarlyError);
            var taken1    = witness.TryAddCheckpoint(cp1, out var error1, Verify);

            // Another checkpoint of the sequence 1: a split view.
            var other     = RoamingNetworkCheckpoint.Create(cp1.RoamingNetworkId, 1, 1, cp0.Id, leader.Head.Commit, [ leader.Head.Commit ], T0.AddMinutes(21));
            var split     = witness.TryAddCheckpoint(other, out var splitError);

            Assert.That(witness.TryWitness(cp1.Id, Witness(), out var witnessed, out var signature, out var witnessError, T0.AddMinutes(25)),  Is.True, witnessError);

            var told      = new List<Int32>();
            leader.OnCheckpointSignatures += (checkpoint, newSignatures) => told.Add(newSignatures.Count);

            var added     = leader.TryAddCheckpointSignatures(cp1.Id, [ signature! ], Verify, out var signed, out var newSignatures, out var addedError);
            var forgedSig = leader.TryAddCheckpointSignatures(cp1.Id, [ cp1.CreateSignature(RNOKey, "witness", COSEAlgorithm.Ed25519, RoamingNetworkCommitSignatureRoles.Witness) ], Verify, out _, out _, out var forgedError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(unsigned,                                               Is.False);
                Assert.That(unsignedError,                                          Does.Contain("no verifier"));
                Assert.That(taken0,                                                 Is.True, error0);
                Assert.That(tooEarly,                                               Is.False);
                Assert.That(tooEarlyError,                                          Does.Contain("does not follow the latest checkpoint"));
                Assert.That(taken1,                                                 Is.True, error1);
                Assert.That(split,                                                  Is.False);
                Assert.That(splitError,                                             Does.Contain("split view"));

                Assert.That(witnessed!.Signatures.Select(sig => sig.Role),          Is.EqualTo(new[] { "roamingNetworkOperator", "witness" }));
                Assert.That(added,                                                  Is.True, addedError);
                Assert.That(newSignatures,                                          Has.Count.EqualTo(1));
                Assert.That(signed!.Signatures.All(sig => Verify(signed, sig)),     Is.True);
                Assert.That(leader.LatestCheckpoint!.Signatures,                    Has.Length.EqualTo(2));
                Assert.That(told,                                                   Is.EqualTo(new[] { 1 }));
                Assert.That(forgedSig,                                              Is.False);
                Assert.That(forgedError,                                            Does.Contain("is invalid"));

            }

        }

        #endregion

        #region (private static) Joined(Leader, CheckpointId)

        /// <summary>
        /// A history of another server joined from the snapshot of the given checkpoint of the leader, with every commit and
        /// checkpoint of the leader since.
        /// </summary>
        private static RoamingNetworkHistory Joined(RoamingNetworkHistory        Leader,
                                                    RoamingNetworkCheckpoint_Id  CheckpointId)
        {

            Assert.That(Leader.TryGetCheckpointSnapshotJSON(CheckpointId, out var snapshot, out var error),           Is.True, error);
            Assert.That(RoamingNetworkHistory.TryFromCheckpointSnapshot(snapshot!, out var joined, out error, Verify),  Is.True, error);
            Assert.That(Leader.TryGetCommitsSince(joined!.Head.Commit.Id, out var commits, out error),                 Is.True, error);
            Assert.That(joined.TryAppend(commits!, out _, out error),                                                   Is.True, error);

            foreach (var checkpoint in Leader.Checkpoints.SkipWhile(checkpoint => checkpoint.Id != CheckpointId).Skip(1))
                Assert.That(joined.TryAddCheckpoint(checkpoint, out error, Verify),                                     Is.True, error);

            return joined;

        }

        #endregion

        #region A_witness_signs_no_checkpoint_it_did_not_check()

        /// <summary>
        /// A history joined from the snapshot of a checkpoint takes it without its commits, which it does not hold: a witness
        /// does not sign it, as its commits were not taken one by one (finding 5 of the review in WWCP_POI.TS); the next one,
        /// checked against the commits since, it signs.
        /// </summary>
        [Test]
        public void A_witness_signs_no_checkpoint_it_did_not_check()
        {

            var leader   = NewHistory();
            Commit(leader, "v1");
            var cp0      = Checkpoint(leader, 10);
            Commit(leader, "v2");
            var cp1      = Checkpoint(leader, 20);

            var joined   = Joined(leader, cp0.Id);
            var witness  = Witness();

            var first    = joined.TryWitness(cp0.Id, witness, out _, out _, out var firstError);
            var next     = joined.TryWitness(cp1.Id, witness, out _, out _, out var nextError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(joined.Checkpoints.Select(checkpoint => checkpoint.Id),       Is.EqualTo(new[] { cp0.Id, cp1.Id }));
                Assert.That(first,                                                        Is.False);
                Assert.That(firstError,                                                   Does.Contain("are not checked here, as this history started from a snapshot"));
                Assert.That(next,                                                         Is.True, nextError);
                Assert.That(witness.Journal.Entries.Select(entry => entry.CheckpointId),  Is.EqualTo(new[] { cp1.Id }));
            }

        }

        #endregion

        #region A_witness_signs_one_checkpoint_of_a_sequence_across_histories_and_restarts()

        /// <summary>
        /// The journal of a witness keeps what it signed beyond the history it signed it from: after a restart, another
        /// history showing it another checkpoint of a sequence it signed is refused, the one it signed is signed again.
        /// </summary>
        [Test]
        public void A_witness_signs_one_checkpoint_of_a_sequence_across_histories_and_restarts()
        {

            var directory  = Path.Combine(Path.GetTempPath(), "poi3-witness-" + Guid.NewGuid().ToString("N"));
            var path       = Path.Combine(directory, "journal.jsonl");

            var leader     = NewHistory();
            Commit(leader, "v1");
            var cp0        = Checkpoint(leader, 10);

            // Another history of the roaming network, of another commit: another checkpoint 0, a split view.
            var other      = NewHistory();
            Commit(other, "w1");
            var cp0b       = Checkpoint(other, 10);

            try
            {

                using (var journal = new RoamingNetworkWitnessJournal(path))
                    Assert.That(leader.TryWitness(cp0.Id, Witness(journal), out _, out _, out var signError, T0.AddMinutes(11)),  Is.True, signError);

                // Restarted: the journal read from its file.
                var restarted  = new RoamingNetworkWitnessJournal(path);

                var split      = other. TryWitness(cp0b.Id, Witness(restarted), out _, out _, out var splitError);
                var again      = leader.TryWitness(cp0. Id, Witness(restarted), out _, out _, out var againError);
                var entries    = restarted.Entries;

                restarted.Dispose();

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(cp0b.Id,                                                                    Is.Not.EqualTo(cp0.Id));
                    Assert.That(entries.Single().CheckpointId,                                              Is.EqualTo(cp0.Id));
                    Assert.That(entries.Single().SignedAt,                                                  Is.EqualTo(T0.AddMinutes(11)));
                    Assert.That(split,                                                                      Is.False);
                    Assert.That(splitError,                                                                 Does.Contain($"signed the checkpoint '{cp0.Id}' of the sequence 0 of the epoch 1 already"));
                    Assert.That(again,                                                                      Is.True, againError);
                    Assert.That(File.ReadAllLines(path),                                                    Has.Length.EqualTo(1));
                }

            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }

        }

        #endregion

        #region A_witness_signs_only_the_line_it_signed()

        /// <summary>
        /// A history that does not hold the checkpoint a witness signed last of the epoch, e.g. one joined later from a snapshot,
        /// can not tell that a checkpoint is of the same line: the witness does not sign from it. From one holding it, it signs
        /// the checkpoints after it, and those before it.
        /// </summary>
        [Test]
        public void A_witness_signs_only_the_line_it_signed()
        {

            var leader   = NewHistory();
            Commit(leader, "v1");
            var cp0      = Checkpoint(leader, 10);
            Commit(leader, "v2");
            var cp1      = Checkpoint(leader, 20);
            Commit(leader, "v3");
            var cp2      = Checkpoint(leader, 30);

            var witness  = Witness();

            Assert.That(leader.TryWitness(cp0.Id, witness, out _, out _, out var signError),  Is.True, signError);

            var joined   = Joined(leader, cp1.Id);

            var fromJoined  = joined.TryWitness(cp2.Id, witness, out _, out _, out var joinedError);
            var after       = leader.TryWitness(cp2.Id, witness, out _, out _, out var afterError);
            var before      = leader.TryWitness(cp1.Id, witness, out _, out _, out var beforeError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromJoined,                                                       Is.False);
                Assert.That(joinedError,                                                      Does.Contain($"does not hold the checkpoint '{cp0.Id}' of the sequence 0 this witness signed last"));
                Assert.That(after,                                                            Is.True, afterError);
                Assert.That(before,                                                           Is.True, beforeError);
                Assert.That(witness.Journal.Entries.Select(entry => entry.Sequence),          Is.EqualTo(new UInt64[] { 0, 1, 2 }));
            }

        }

        #endregion

        #region The_journal_of_a_witness_drops_a_line_written_partly()

        /// <summary>
        /// A last line written only partly, e.g. by a crash before the signature was given, is dropped when the journal is read;
        /// an invalid line before it, or two checkpoints of one sequence, are an error.
        /// </summary>
        [Test]
        public void The_journal_of_a_witness_drops_a_line_written_partly()
        {

            var directory  = Path.Combine(Path.GetTempPath(), "poi3-witness-" + Guid.NewGuid().ToString("N"));
            var path       = Path.Combine(directory, "journal.jsonl");

            var leader     = NewHistory();
            Commit(leader, "v1");
            var cp0        = Checkpoint(leader, 10);

            try
            {

                using (var journal = new RoamingNetworkWitnessJournal(path))
                    Assert.That(leader.TryWitness(cp0.Id, Witness(journal), out _, out _, out var signError),  Is.True, signError);

                var line = File.ReadAllLines(path).Single();

                File.AppendAllText(path, line[..20]);

                using (var reread = new RoamingNetworkWitnessJournal(path))
                    Assert.That(reread.Entries.Single().CheckpointId,  Is.EqualTo(cp0.Id));

                using (Assert.EnterMultipleScope())
                {

                    Assert.That(File.ReadAllText(path),  Is.EqualTo(line + "\n"));

                    File.WriteAllText(path, line[..20] + "\n" + line + "\n");
                    Assert.That(() => new RoamingNetworkWitnessJournal(path),  Throws.InstanceOf<InvalidDataException>().With.Message.Contains("Line 1"));

                    File.WriteAllText(path, line + "\n" + line.Replace(cp0.Id.ToString(), new String('0', 64)) + "\n");
                    Assert.That(() => new RoamingNetworkWitnessJournal(path),  Throws.InstanceOf<InvalidDataException>().With.Message.Contains("names two checkpoints of the sequence 0"));

                }

            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, recursive: true);
            }

        }

        #endregion

        #region Checkpoints_survive_a_restart_and_bound_the_compaction()

        [Test]
        public void Checkpoints_survive_a_restart_and_bound_the_compaction()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = NewHistory(store, new RoamingNetworkHistoryRetention {
                                                  VersionCheckpointInterval  = 1,
                                                  RecentVersions             = 100,
                                                  RetainCommits              = 2,
                                                  CompactAfterLogEntries     = Int64.MaxValue
                                              });

            Commit(history, "v1");
            Commit(history, "v2");
            var cp0      = Checkpoint(history, 10);
            Commit(history, "v3");
            Commit(history, "v4");
            Commit(history, "v5");
            Commit(history, "v6");

            history.TryWitness(cp0.Id, Witness(), out _, out _, out var witnessError);

            Assert.That(history.Compact(out var compactError),                                                       Is.True, compactError);

            var rootAfterCompaction = history.Root.Id;

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError),                       Is.True, loadError);

            loaded!.CheckpointSigners = [ RNOSigner ];

            var cp1      = Checkpoint(loaded, 30);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(witnessError,                                            Is.Null);

                // Compacted to the commit of the checkpoint, not to the latest commit the retention would allow.
                Assert.That(rootAfterCompaction,                                     Is.EqualTo(cp0.CommitId));

                Assert.That(loaded.Checkpoints.Select(checkpoint => checkpoint.Id),  Is.EqualTo(new[] { cp0.Id, cp1.Id }));
                Assert.That(loaded.Checkpoints[0].Signatures,                        Has.Length.EqualTo(2));
                Assert.That(cp1.PreviousId,                                          Is.EqualTo(cp0.Id));
                Assert.That(cp1.CommitsSize,                                         Is.EqualTo(4));
                Assert.That(loaded.TryGetCommitProof(cp1.CommitId, out var proof, out var proofError) &&
                            proof.Verify(cp1, out proofError),                       Is.True, proofError);

            }

        }

        #endregion

        #region A_server_joins_from_a_checkpoint()

        [Test]
        public void A_server_joins_from_a_checkpoint()
        {

            var leader  = NewHistory();
            Commit(leader, "v1");
            Commit(leader, "v2");
            var cp0     = Checkpoint(leader, 10);
            Commit(leader, "v3");

            Assert.That(leader.TryGetCheckpointSnapshotJSON(cp0.Id, out var snapshot, out var snapshotError),  Is.True, snapshotError);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot!, out var joined, out var joinError),   Is.True, joinError);
            Assert.That(leader.TryGetCommitsSince(cp0.CommitId, out var since, out var sinceError),            Is.True, sinceError);

            var appended   = joined!.TryAppend(since!, out _, out var appendError);
            var takenFirst = joined.TryAddCheckpoint(cp0, out var takenError, Verify);

            joined.CheckpointSigners = [ RNOSigner ];

            var next       = joined.TryMakeCheckpoint(out var cp1, out var nextError, CreatedAt: T0.AddMinutes(20));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(snapshot!["checkpoint"]!["@id"]!.Value<String>(),     Is.EqualTo(cp0.Id.ToString()));
                Assert.That(snapshot.ContainsKey("runtimeStatus"),                 Is.False);
                Assert.That(joined.Root.Id,                                        Is.EqualTo(cp0.CommitId));
                Assert.That(appended,                                              Is.True, appendError);
                Assert.That(takenFirst,                                            Is.True, takenError);
                Assert.That(next,                                                  Is.True, nextError);
                Assert.That(cp1!.PreviousId,                                       Is.EqualTo(cp0.Id));
                Assert.That(cp1.CommitId,                                          Is.EqualTo(leader.Head.Commit.Id));
            }

        }

        #endregion

    }

}
