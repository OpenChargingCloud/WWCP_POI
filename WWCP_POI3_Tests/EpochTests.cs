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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// Epochs (SPECIFICATION.md, section 11): an epoch is closed by its final checkpoint naming the next one, which starts
    /// from the version of its commit with a genesis of its own, identified with the hash of its profiles, and accepts only
    /// its signature algorithms.
    /// </summary>
    [TestFixture]
    public class EpochTests
    {

        #region Data

        private const           String          Operator   = nameof(ChargingStationOperator);
        private static readonly DateTimeOffset  T0         = DateTimeOffset.Parse("2026-10-20T12:00:00Z");

        /// <summary>
        /// The next epoch: SHA-512 identifications, every signature algorithm.
        /// </summary>
        private static readonly RoamingNetworkEpochProfiles  Strong      = new (ETagHashAlgorithm.SHA512);

        /// <summary>
        /// A next epoch of SHA-512 identifications, Ed25519 and the post-quantum ML-DSA-65 only: no more ES256.
        /// </summary>
        private static readonly RoamingNetworkEpochProfiles  Restricted  = new (ETagHashAlgorithm.SHA512, [ "Ed25519", "ML-DSA-65" ]);

        /// <summary>
        /// A post-quantum signing key of the roaming network operator.
        /// </summary>
        private static readonly RoamingNetworkCommitSigner   PostQuantum = new (Org.BouncyCastle.Crypto.Parameters.MLDsaPrivateKeyParameters.FromSeed(COSEAlgorithm.MLDsa65.MLDsaParameterSet!, Convert.FromHexString("707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f")),
                                                                           "rno-pq",
                                                                           COSEAlgorithm.MLDsa65,
                                                                           RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                                           "DE*RNO");

        /// <summary>
        /// An Ed25519 signing key of DE*GEF, besides its ES256 one: of an algorithm the epoch Restricted takes.
        /// </summary>
        private static readonly Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters  GEFEd25519 = new (Convert.FromHexString("909192939495969798999a9b9c9d9e9fa0a1a2a3a4a5a6a7a8a9aaabacadaeaf"), 0);

        #endregion


        #region (static) NewHistory(Store = null)

        private static RoamingNetworkHistory NewHistory(IRoamingNetworkHistoryStore? Store = null)
        {

            var history = RoamingNetworkHistory.Start(Proofs.StationProofTests.NewNetwork(), Store: Store);

            history.CheckpointSigners = [ CheckpointTests.RNOSigner ];

            return history;

        }

        #endregion

        #region (static) Commit(History, Value, Minutes)

        private static RoamingNetworkCommitResult Commit(RoamingNetworkHistory  History,
                                                         String                 Value,
                                                         Int32                  Minutes)

            => History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(
                                  [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                                  CreatedAt: T0.AddMinutes(Minutes)));

        #endregion

        #region (static) Close(Profiles = null, Before = null)

        /// <summary>
        /// A history in the first epoch with two commits and a checkpoint, closed by its final checkpoint naming the next
        /// epoch, of the given profiles, Strong by default; what else is given happens before.
        /// </summary>
        private static (RoamingNetworkHistory History, RoamingNetworkCheckpoint Final) Close(RoamingNetworkEpochProfiles?    Profiles   = null,
                                                                                              Action<RoamingNetworkHistory>?  Before     = null)
        {

            var history = NewHistory();

            Assert.That(Commit(history, "v1", 1).IsCommitted);
            Assert.That(history.TryMakeCheckpoint(out _, out var checkpointError, CreatedAt: T0.AddMinutes(2)), Is.True, checkpointError);
            Assert.That(Commit(history, "v2", 3).IsCommitted);

            Before?.Invoke(history);

            history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0.AddDays(-30), out _, out _);

            Assert.That(history.TryCloseEpoch(Profiles ?? Strong, out var final, out var closeError, T0.AddMinutes(4)), Is.True, closeError);

            return (history, final!);

        }

        #endregion


        #region Profiles_name_what_an_epoch_calculates()

        [Test]
        public void Profiles_name_what_an_epoch_calculates()
        {

            var json = Restricted.ToJSON(Embedded: true);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json.ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("{\"canonicalization\":\"styx-canonical-json-v1\",\"identificationHash\":\"sha512\",\"commit\":\"https://open.charging.cloud/contexts/wwcp/roamingNetwork/commit/v3\",\"checkpoint\":\"https://open.charging.cloud/contexts/wwcp/roamingNetwork/checkpoint/v1\",\"priceStatement\":\"https://open.charging.cloud/contexts/wwcp/roamingNetwork/priceStatement/v1\",\"signatureAlgorithms\":[\"Ed25519\",\"ML-DSA-65\"]}"));

                Assert.That(RoamingNetworkEpochProfiles.TryParse(json, out var parsed, out var parseError),   Is.True, parseError);
                Assert.That(parsed,                                                                           Is.EqualTo(Restricted));

                Assert.That(RoamingNetworkEpochProfiles.Default.AllowsSignatureAlgorithm("ES256"),            Is.True, "The first epoch takes every algorithm this implementation signs with.");
                Assert.That(Restricted.AllowsSignatureAlgorithm("ES256"),                                     Is.False);

                // Only what this implementation calculates.
                foreach (var (property, value) in new[] { ("canonicalization",    (JToken) "jcs-rfc8785"),
                                                          ("identificationHash",  "sha3-256"),
                                                          ("commit",              "wwcp-poi-commit-v4"),
                                                          ("signatureAlgorithms", new JArray("HMAC 256/256")) })
                {
                    var other = (JObject) json.DeepClone();
                    other[property] = value;
                    Assert.That(RoamingNetworkEpochProfiles.TryParse(other, out _, out _),  Is.False, property);
                }

            }

        }

        #endregion

        #region An_epoch_closes_with_its_final_checkpoint_and_the_next_one_starts_from_its_version()

        [Test]
        public void An_epoch_closes_with_its_final_checkpoint_and_the_next_one_starts_from_its_version()
        {

            var (history, final)  = Close();

            var commitAfter       = Commit(history, "v3", 5);
            var checkpointAfter   = history.TryMakeCheckpoint(out _, out var checkpointAfterError);
            var closing           = history.EpochStatus;

            Assert.That(history.TryStartNextEpoch(out var next, out var startError, Signers: [ CheckpointTests.RNOSigner ]),  Is.True, startError);

            var genesis           = next!.Root;

            next.CheckpointSigners = [ CheckpointTests.RNOSigner ];

            var v3                = Commit(next, "v3", 5);

            Assert.That(next.TryMakeCheckpoint(out var first, out var firstError, CreatedAt: T0.AddMinutes(6)),  Is.True, firstError);

            Assert.That(RoamingNetworkCommit.    TryParse(v3.Commit!.ToJSON().ToString(), out var readCommit,     out var readCommitError),      Is.True, readCommitError);
            Assert.That(RoamingNetworkCheckpoint.TryParse(first!.ToJSON().ToString(),     out var readCheckpoint, out var readCheckpointError),  Is.True, readCheckpointError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(final.IsFinal,                                Is.True);
                Assert.That(final.Next,                                   Is.EqualTo(new RoamingNetworkEpoch(2, Strong)));
                Assert.That(final.ToJSON()["final"]?.Value<Boolean>(),    Is.True);
                Assert.That(final.CommitId,                               Is.EqualTo(history.Head.Commit.Id));

                Assert.That(commitAfter.Status,                           Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(commitAfter.ErrorResponse,                    Does.Contain("is closed by its final checkpoint"));
                Assert.That(checkpointAfter,                              Is.False);
                Assert.That(checkpointAfterError,                         Does.Contain("is closed"));
                Assert.That(closing,                                      Is.EqualTo(RoamingNetworkEpochStatus.Closing));
                Assert.That(history.EpochStatus,                          Is.EqualTo(RoamingNetworkEpochStatus.Closed));

                Assert.That(next.Epoch,                                   Is.EqualTo(new RoamingNetworkEpoch(2, Strong)));
                Assert.That(next.PreviousEpoch,                           Is.EqualTo(RoamingNetworkEpochLink.To(final)));
                Assert.That(genesis.IsGenesis,                            Is.True);
                Assert.That(genesis.Epoch,                                Is.EqualTo(2));
                Assert.That(genesis.Revision,                             Is.Zero);
                Assert.That(genesis.Id.ToString(),                        Has.Length.EqualTo(128), "Identified with SHA-512.");
                Assert.That(genesis.ETags,                                Is.EqualTo(final.ETags), "The version of the final checkpoint.");
                Assert.That(genesis.ToJSON()["profiles"],                 Is.Not.Null);
                Assert.That(next.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status) ? status.Value : null,
                            Is.EqualTo("offline"), "The runtime status is taken along.");

                Assert.That(v3.IsCommitted,                               Is.True, v3.ErrorResponse);
                Assert.That(v3.Commit.Epoch,                              Is.EqualTo(2));
                Assert.That(v3.Commit.ToJSON()["epoch"]?.Value<UInt32>(), Is.EqualTo(2));
                Assert.That(v3.Commit.ToJSON()["profiles"],               Is.Null, "Only the genesis names the profiles.");
                Assert.That(readCommit!.Id,                               Is.EqualTo(v3.Commit.Id));

                Assert.That(first.Epoch,                                  Is.EqualTo(2));
                Assert.That(first.Sequence,                               Is.Zero);
                Assert.That(first.PreviousEpoch,                          Is.EqualTo(RoamingNetworkEpochLink.To(final)));
                Assert.That(first.Id.ToString(),                          Has.Length.EqualTo(128));
                Assert.That(first.CommitsRoot,                            Has.Length.EqualTo(128));
                Assert.That(first.CommitsSize,                            Is.EqualTo(2), "The genesis of the epoch and the commit after it.");
                Assert.That(readCheckpoint!.Id,                           Is.EqualTo(first.Id));

                Assert.That(history.TryGetCommitProof(final.CommitId, out var proof, out var proofError),  Is.True, proofError);
                Assert.That(proof!.Verify(final, out var verifyError),    Is.True, verifyError);

            }

        }

        #endregion

        #region A_witness_signs_the_next_epoch_only_from_the_final_checkpoint_it_signed()

        /// <summary>
        /// A witness that signed the final checkpoint of an epoch signs a checkpoint of the next epoch only where that epoch
        /// starts from it: not one of a next epoch started from another final checkpoint of the same epoch.
        /// </summary>
        [Test]
        public void A_witness_signs_the_next_epoch_only_from_the_final_checkpoint_it_signed()
        {

            var (history, final)  = Close(Strong);
            var (another, other)  = Close(Restricted);
            var witness           = CheckpointTests.Witness();

            Assert.That(history.TryWitness(final.Id, witness, out _, out _, out var finalError),          Is.True, finalError);

            Assert.That(history.TryStartNextEpoch(out var next,        out var startError),               Is.True, startError);
            Assert.That(another.TryStartNextEpoch(out var anotherNext, out var anotherStartError),        Is.True, anotherStartError);

            next!.       CheckpointSigners = [ CheckpointTests.RNOSigner ];
            anotherNext!.CheckpointSigners = [ CheckpointTests.RNOSigner ];

            Assert.That(next.       TryMakeCheckpoint(out var first,      out var firstError,      CreatedAt: T0.AddMinutes(5)),  Is.True, firstError);
            Assert.That(anotherNext.TryMakeCheckpoint(out var otherFirst, out var otherFirstError, CreatedAt: T0.AddMinutes(5)),  Is.True, otherFirstError);

            var fromOther  = anotherNext.TryWitness(otherFirst!.Id, witness, out _, out _, out var otherError);
            var fromFinal  = next.       TryWitness(first!.     Id, witness, out _, out _, out var nextError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(other.Id,                         Is.Not.EqualTo(final.Id));
                Assert.That(otherFirst.PreviousEpoch!.CheckpointId,  Is.EqualTo(other.Id));
                Assert.That(fromOther,                        Is.False);
                Assert.That(otherError,                       Does.Contain($"starts from the final checkpoint '{other.Id}', but this witness signed '{final.Id}'"));
                Assert.That(fromFinal,                        Is.True, nextError);
            }

        }

        #endregion

        #region Every_server_starts_the_same_next_epoch_from_the_same_final_checkpoint()

        [Test]
        public void Every_server_starts_the_same_next_epoch_from_the_same_final_checkpoint()
        {

            var (history, final)  = Close();

            // Another server took the commits and the checkpoints of the first epoch.
            var other             = RoamingNetworkHistory.Start(Proofs.StationProofTests.NewNetwork());

            Assert.That(history.TryGetCommitsSince(other.Head.Commit.Id, out var commits, out var commitsError),  Is.True, commitsError);
            Assert.That(other.TryAppend(commits!, out _, out var appendError),                                      Is.True, appendError);

            foreach (var checkpoint in history.Checkpoints)
                Assert.That(other.TryAddCheckpoint(checkpoint, out var addError, CheckpointTests.Verify), Is.True, addError);

            Assert.That(history.TryStartNextEpoch(out var next,      out var startError),       Is.True, startError);
            Assert.That(other.  TryStartNextEpoch(out var otherNext, out var otherStartError),  Is.True, otherStartError);

            Assert.That(next!.Commit(next.Head.RoamingNetwork.CreateSignedChangeSet(
                                         [ RoamingNetworkChange.UpdateProperty(next.Head.RoamingNetwork, "dataSource", null, "v3") ],
                                         CreatedAt: T0.AddMinutes(5))).IsCommitted);

            Assert.That(next.TryGetCommitsSince(otherNext!.Head.Commit.Id, out var nextCommits, out var nextCommitsError),  Is.True, nextCommitsError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(other.FinalCheckpoint?.Id,                                         Is.EqualTo(final.Id));
                Assert.That(otherNext.Root.Id,                                                 Is.EqualTo(next.Root.Id), "The same genesis.");
                Assert.That(otherNext.TryAppend(nextCommits!, out _, out var nextAppendError),  Is.True, nextAppendError);
                Assert.That(otherNext.Head.Commit.Id,                                          Is.EqualTo(next.Head.Commit.Id));
            }

        }

        #endregion

        #region An_epoch_accepts_only_its_signature_algorithms()

        [Test]
        public void An_epoch_accepts_only_its_signature_algorithms()
        {

            var (history, final)  = Close(Restricted);

            Assert.That(history.TryStartNextEpoch(out var next, out var startError),  Is.True, startError);

            // The parties of the test network sign their change sets with ES256 keys.
            var refusedCommit       = Commit(next!, "v3", 5);

            // The first checkpoint of the epoch is signed with the algorithms of both epochs.
            next.CheckpointSigners  = [ PostQuantum, Proofs.PriceStatementTests.Signer(ChargingStationOperator_Id.Parse("DE*GEF")) ];
            var first               = next.TryMakeCheckpoint(out _, out var firstError, CreatedAt: T0.AddMinutes(6));

            next.AttestationSigners = [ Proofs.PriceStatementTests.Signer(ChargingStationOperator_Id.Parse("DE*GEF")) ];
            var attested            = next.TryAttest(out var attestations, out var attestError);

            next.AttestationSigners = [ PostQuantum ];
            var attestedPostQuantum = next.TryAttest(out var postQuantumAttestations, out var postQuantumError);

            var priceStatement      = Proofs.PriceStatementTests.Signer(ChargingStationOperator_Id.Parse("DE*GEF")).
                                          StatePrices(next.RoamingNetworkId, nameof(EVSE), "DE*GEF*E1*1",
                                                      new ChargingPrices([ new ChargingTariffPrices(ChargingTariff_Id.Parse("DE*GEF*T1"),
                                                                                                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.29m, StepSize: 1) ]) ]) ]),
                                                      0, null);

            var statementResult     = next.TryAddPriceStatement(priceStatement, out var statementError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refusedCommit.Status,              Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(refusedCommit.ErrorResponse,       Does.Contain("The signature algorithm 'ES256'").And.Contain("is not one of the epoch 2"));

                Assert.That(first,                             Is.True, firstError);

                Assert.That(attested,                          Is.True, attestError);
                Assert.That(attestations,                      Is.Empty, "An attestation signer with another algorithm attests nothing.");
                Assert.That(attestedPostQuantum,               Is.True, postQuantumError);
                Assert.That(postQuantumAttestations!.Select(attestation => attestation.Algorithm),  Is.EqualTo(new[] { "ML-DSA-65" }));
                Assert.That(next.Checkpoints[0].Signatures.Select(signature => signature.Algorithm),  Is.EquivalentTo(new[] { "ML-DSA-65", "ES256" }));

                Assert.That(statementResult,                   Is.EqualTo(POIPriceStatementResult.Refused));
                Assert.That(statementError,                    Does.Contain("The signature algorithm 'ES256' of the price statement"));

                Assert.That(history.TryStartNextEpoch(out _, out var signerError, Signers: [ Proofs.PriceStatementTests.Signer(ChargingStationOperator_Id.Parse("DE*GEF")) ]),  Is.False);
                Assert.That(signerError,                       Does.Contain("is not one of the epoch 2"));

            }

        }

        #endregion

        #region Only_the_price_statements_of_its_algorithms_are_taken_along_and_the_chains_of_the_others_go_on()

        /// <summary>
        /// The next epoch takes along only the current price statements signed with one of its algorithms (SPECIFICATION.md,
        /// section 11.4): DE*GEF*E1*2, signed with Ed25519, is current there, DE*GEF*E1*1, signed with ES256, is not. Its
        /// chain goes on all the same, also after a restart and a compaction: the next statement made there has the next
        /// sequence and names the last one of the first epoch. Nothing taken there is checked against that one: a statement
        /// beginning its chain anew is taken as well.
        /// </summary>
        [Test]
        public void Only_the_price_statements_of_its_algorithms_are_taken_along_and_the_chains_of_the_others_go_on()
        {

            const String  E11      = "DE*GEF*E1*1";
            const String  E12      = "DE*GEF*E1*2";

            var gefId              = ChargingStationOperator_Id.Parse("DE*GEF");
            var ed25519Key         = new POIPublicKey(COSEKey.From(GEFEd25519.GeneratePublicKey()));
            var es256              = Proofs.PriceStatementTests.Signer(gefId);
            var ed25519            = new RoamingNetworkCommitSigner(GEFEd25519, ed25519Key.Id, COSEAlgorithm.Ed25519, RoamingNetworkCommitSignatureRoles.ChargingStationOperator, gefId.ToString());

            static ChargingPrices Prices(Decimal Price)
                => new ([ new ChargingTariffPrices(ChargingTariff_Id.Parse("DE*GEF*T1"),
                                                   [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]) ]) ]);

            RoamingNetworkPriceStatement? first   = null;
            RoamingNetworkPriceStatement? second  = null;
            RoamingNetworkPriceStatement? other   = null;

            var (history, _)       = Close(Restricted, firstEpoch => {

                // DE*GEF signs with Ed25519 as well, its ES256 key stays.
                Assert.That(firstEpoch.Head.RoamingNetwork.TryGetChargingStationOperatorById(gefId, out var gef),  Is.True);

                var signingKeys    = (JArray) gef!.ToEmbeddedJSON()[POIPartyKeys.SigningKeysProperty]!;
                var withEd25519    = new JArray(signingKeys.Concat([ ed25519Key.ToJSON() ]));

                var keyCommit      = firstEpoch.Commit(firstEpoch.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty, signingKeys, withEd25519) ],
                                                                                                             CreatedAt: T0.AddMinutes(3).AddSeconds(30)));

                Assert.That(keyCommit.IsCommitted,  Is.True, keyCommit.ErrorResponse);

                first              = es256.  StatePrices(firstEpoch.RoamingNetworkId, nameof(EVSE), E11, Prices(0.29m), 0, null);
                second             = es256.  StatePrices(firstEpoch.RoamingNetworkId, nameof(EVSE), E11, Prices(0.31m), 1, first.Digest);
                other              = ed25519.StatePrices(firstEpoch.RoamingNetworkId, nameof(EVSE), E12, Prices(0.33m), 0, null);

                foreach (var priceStatement in new[] { first, second, other })
                    Assert.That(firstEpoch.TryAddPriceStatement(priceStatement, out var addError),  Is.EqualTo(POIPriceStatementResult.Taken), addError);

            });

            var store              = new RoamingNetworkHistoryMemoryStore();

            Assert.That(history.TryStartNextEpoch(out var next, out var startError, Store: store),  Is.True, startError);

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError),    Is.True, loadError);
            Assert.That(loaded!.Compact(out var compactError),                                      Is.True, compactError);
            Assert.That(RoamingNetworkHistory.TryLoad(store, out var again, out var againError),    Is.True, againError);

            var takenAlong         = next!.PriceStatements;
            var notTakenAlong      = next.TryGetPriceStatement(nameof(EVSE), E11, out _);

            // A server holding a signing key of DE*GEF of the next epoch states the price status of DE*GEF*E1*1 there.
            next. PriceStatementSigners = [ ed25519 ];
            again!.PriceStatementSigners = [ ed25519 ];

            Assert.That(next. TryStatePrices(nameof(EVSE), E11, out var third,      out var thirdError),       Is.True, thirdError);
            Assert.That(again.TryStatePrices(nameof(EVSE), E11, out var thirdAgain, out var thirdAgainError),  Is.True, thirdAgainError);

            // Another server beginning the chain anew.
            var anew               = loaded.TryAddPriceStatement(ed25519.StatePrices(loaded.RoamingNetworkId, nameof(EVSE), E11, Prices(0.35m), 0, null), out var anewError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(takenAlong.Select(priceStatement => priceStatement.Digest),  Is.EqualTo(new[] { other!.Digest }), "Only the statement signed with Ed25519 is taken along.");
                Assert.That(notTakenAlong,                                               Is.False, "The statement signed with ES256 is not.");
                Assert.That(loaded.TryGetPriceStatement(nameof(EVSE), E12, out var restarted) ? restarted.Digest : null,  Is.EqualTo(other.Digest));

                foreach (var statement in new[] { third!, thirdAgain! })
                {
                    Assert.That(statement.Sequence,                                      Is.EqualTo(2), "The chain of DE*GEF*E1*1 goes on.");
                    Assert.That(statement.Previous,                                      Is.EqualTo(second!.Digest));
                    Assert.That(statement.Algorithm,                                     Is.EqualTo("Ed25519"));
                }

                Assert.That(next.TryGetPriceStatement(nameof(EVSE), E11, out var current) ? current.Digest : null,  Is.EqualTo(third!.Digest));

                Assert.That(anew,                                                        Is.EqualTo(POIPriceStatementResult.Taken), anewError);

            }

        }

        #endregion

        #region The_first_checkpoint_of_a_later_epoch_takes_the_algorithms_of_both_epochs_alone()

        /// <summary>
        /// The first checkpoint of the epoch 3 is signed with the algorithms of the epochs 2 and 3 alone (SPECIFICATION.md,
        /// section 11.3): the epoch 2 takes ES256 and ML-DSA-65, the epoch 3 ML-DSA-65 alone; Ed25519, which only the first
        /// epoch took, is refused. A snapshot of the epoch 3 names the profiles of the epoch 2; a history started from one
        /// without them takes the signatures of the first checkpoint with the algorithms of the epoch 3 alone.
        /// </summary>
        [Test]
        public void The_first_checkpoint_of_a_later_epoch_takes_the_algorithms_of_both_epochs_alone()
        {

            RoamingNetworkEpochProfiles  middle          = new (ETagHashAlgorithm.SHA512, [ "ES256", "ML-DSA-65" ]);
            RoamingNetworkEpochProfiles  postQuantum     = new (ETagHashAlgorithm.SHA512, [ "ML-DSA-65" ]);
            var                          es256           = Proofs.PriceStatementTests.Signer(ChargingStationOperator_Id.Parse("DE*GEF"));

            // The keys are not what this test is about.
            VerifyCheckpointSignatureDelegate  anyKey    = (checkpoint, signature) => true;

            var (history, _)  = Close(middle);

            Assert.That(history.TryStartNextEpoch(out var second, out var secondError),  Is.True, secondError);
            second!.CheckpointSigners = [ es256 ];
            Assert.That(second.TryCloseEpoch(postQuantum, out _, out var closeError, T0.AddMinutes(6)),  Is.True, closeError);
            Assert.That(second.TryStartNextEpoch(out var third, out var thirdError),  Is.True, thirdError);

            third!.CheckpointSigners  = [ PostQuantum, CheckpointTests.RNOSigner ];
            var withEd25519           = third.TryMakeCheckpoint(out _, out var ed25519SignerError, CreatedAt: T0.AddMinutes(7));

            third.CheckpointSigners   = [ PostQuantum, es256 ];
            Assert.That(third.TryMakeCheckpoint(out var first, out var firstError, CreatedAt: T0.AddMinutes(7)),  Is.True, firstError);

            var addedEd25519          = third.TryAddCheckpointSignatures(first!.Id, [ CheckpointTests.RNOSigner.Sign(first) ], anyKey, out _, out _, out var ed25519Error);

            var snapshot              = third.ToSnapshotJSON(IncludeRuntimeStatus: false);
            var withoutProfiles       = (JObject) snapshot.DeepClone();
            ((JObject) withoutProfiles["epoch"]!).Remove("previousProfiles");

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot.       ToString(), out var joined,   out var joinError),    Is.True, joinError);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(withoutProfiles.ToString(), out var unknowing, out var unknowError), Is.True, unknowError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(third.PreviousEpochProfiles,                                           Is.EqualTo(middle));
                Assert.That(second.PreviousEpochProfiles,                                          Is.EqualTo(RoamingNetworkEpochProfiles.Default));
                Assert.That(second.ToSnapshotJSON(IncludeRuntimeStatus: false)["epoch"]?["previousProfiles"],  Is.Null, "Those of the first epoch are the default ones.");

                Assert.That(withEd25519,                                                           Is.False);
                Assert.That(ed25519SignerError,                                                    Does.Contain("The signature algorithm 'Ed25519' of the checkpoint signer").And.EndWith("is neither one of the epoch 3, ML-DSA-65, nor of the epoch 2, ES256, ML-DSA-65!"));
                Assert.That(first.Signatures.Select(signature => signature.Algorithm),            Is.EquivalentTo(new[] { "ML-DSA-65", "ES256" }));

                Assert.That(addedEd25519,                                                          Is.False);
                Assert.That(ed25519Error,                                                          Is.EqualTo($"The signature algorithm 'Ed25519' of a signature of the checkpoint '{first.Id}' is neither one of the epoch 3, ML-DSA-65, nor of the epoch 2, ES256, ML-DSA-65!"));

                Assert.That(joined!.PreviousEpochProfiles,                                         Is.EqualTo(middle));
                Assert.That(joined.TryAddCheckpoint(first, out var joinedError, anyKey),           Is.True, joinedError);

                Assert.That(unknowing!.PreviousEpochProfiles,                                      Is.Null);
                Assert.That(unknowing.TryAddCheckpoint(first, out var unknowingError, anyKey),     Is.False);
                Assert.That(unknowingError,                                                        Is.EqualTo($"The signature algorithm 'ES256' of a signature of the checkpoint '{first.Id}' is not one of the epoch 3, ML-DSA-65, and the profiles of the epoch 2 are not known here!"));

            }

        }

        #endregion

        #region Commits_and_checkpoints_of_another_epoch_are_refused()

        [Test]
        public void Commits_and_checkpoints_of_another_epoch_are_refused()
        {

            var (history, final)  = Close();

            Assert.That(history.TryStartNextEpoch(out var next, out var startError),  Is.True, startError);

            // A first epoch going on elsewhere, without the final checkpoint.
            var elsewhere         = NewHistory();
            Commit(elsewhere, "v1", 1);
            var olderEpoch        = Commit(elsewhere, "w2", 3).Commit!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(next!.TryAppend(olderEpoch, out var appendError),                     Is.False);
                Assert.That(appendError,                                                          Does.Contain("unknown here").Or.Contain("epoch"));

                Assert.That(history.TryAppend(next.Root, out var genesisError),                   Is.False);
                Assert.That(genesisError,                                                         Does.Contain("is closed"));

                Assert.That(next.TryAddCheckpoint(history.Checkpoints[0], out var checkpointError, CheckpointTests.Verify),  Is.False);
                Assert.That(checkpointError,                                                      Does.Contain("of the epoch 1, but this history is of the epoch 2"));

            }

        }

        #endregion

        #region The_first_epoch_is_named_by_its_checkpoints_not_by_its_commits()

        /// <summary>
        /// Every history begins in epoch 1: its checkpoints name it, as every checkpoint names its epoch, its commits do not
        /// (SPECIFICATION.md, sections 5.1, 10.1 and 11). A checkpoint without its epoch is refused, as is a commit naming
        /// the first one, which would be a second written form of it, of another identification.
        /// </summary>
        [Test]
        public void The_first_epoch_is_named_by_its_checkpoints_not_by_its_commits()
        {

            var history = NewHistory();
            var commit  = Commit(history, "v1", 1).Commit!;

            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var checkpointError, CreatedAt: T0.AddMinutes(2)),  Is.True, checkpointError);

            var checkpointJSON  = checkpoint!.ToJSON();
            var commitJSON      = commit.ToJSON();

            var withoutEpoch    = (JObject) checkpointJSON.DeepClone();
            withoutEpoch.Remove("epoch");

            var namingEpoch     = (JObject) commitJSON.DeepClone();
            namingEpoch.Add("epoch", 1);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(checkpointJSON["epoch"]?.Value<UInt32>(),                                              Is.EqualTo(1));
                Assert.That(commitJSON.ContainsKey("epoch"),                                                       Is.False);

                Assert.That(RoamingNetworkCheckpoint.TryParse(withoutEpoch.ToString(), out _, out var withoutEpochError),  Is.False);
                Assert.That(withoutEpochError,                                                                     Is.EqualTo("Missing or invalid 'epoch'!"));

                Assert.That(RoamingNetworkCommit.    TryParse(namingEpoch.ToString(),  out _, out var namingEpochError),   Is.False);
                Assert.That(namingEpochError,                                                                      Is.EqualTo("Invalid epoch: a commit names it from the second one on!"));

            }

        }

        #endregion

        #region A_final_checkpoint_may_name_the_commit_of_the_checkpoint_before()

        [Test]
        public void A_final_checkpoint_may_name_the_commit_of_the_checkpoint_before()
        {

            var history = NewHistory();

            Assert.That(Commit(history, "v1", 1).IsCommitted);
            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var checkpointError, CreatedAt: T0.AddMinutes(2)),  Is.True, checkpointError);
            Assert.That(history.TryCloseEpoch(Strong, out var final, out var closeError, T0.AddMinutes(3)),                  Is.True, closeError);

            var other   = RoamingNetworkHistory.Start(Proofs.StationProofTests.NewNetwork());

            Assert.That(history.TryGetCommitsSince(other.Head.Commit.Id, out var commits, out _),  Is.True);
            Assert.That(other.TryAppend(commits!, out _, out _),                                    Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(final!.CommitId,                                                         Is.EqualTo(checkpoint!.CommitId));
                Assert.That(final.CommitsSize,                                                       Is.EqualTo(1));
                Assert.That(other.TryAddCheckpoint(checkpoint, out var addError,      CheckpointTests.Verify),  Is.True, addError);
                Assert.That(other.TryAddCheckpoint(final,      out var addFinalError, CheckpointTests.Verify),  Is.True, addFinalError);
                Assert.That(other.EpochStatus,                                                       Is.EqualTo(RoamingNetworkEpochStatus.Closing));
            }

        }

        #endregion

        #region A_peer_follows_the_server_into_the_next_epoch()

        [Test]
        public async Task A_peer_follows_the_server_into_the_next_epoch()
        {

            // The operator of the roaming network attests its head and signs its checkpoints: anchored in its trust anchor.
            var leader                 = RoamingNetworkHistory.Start(Proofs.StationProofTests.NewNetwork().WithKeyStatement()).Attesting();
            var verify                 = TestParties.VerifyOperatorCheckpointSignature(leader.Head.RoamingNetwork, CheckpointTests.Verify);

            leader.CheckpointSigners   = [ leader.Head.RoamingNetwork.OperatorSigner() ];

            await using var httpServer = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI     = new POI_API(httpServer) { VerifyCheckpointSignature = verify };

            poiAPI.AddRoamingNetworkHistory(leader);

            Assert.That(Commit(leader, "v1", 1).IsCommitted);
            Assert.That(leader.TryMakeCheckpoint(out var checkpoint, out var checkpointError, CreatedAt: T0.AddMinutes(2)),  Is.True, checkpointError);

            var replaced               = new List<(UInt32 Old, UInt32 New)>();

            await using var peer       = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/history/ws"),
                                                                null,
                                                                new RoamingNetworkPeerOptions {
                                                                    TrustAnchor                = leader.Head.RoamingNetwork.TrustAnchor(),
                                                                    VerifyCheckpointSignature  = verify,
                                                                    MinReconnectDelay          = TimeSpan.FromMilliseconds(50),
                                                                    MaxReconnectDelay          = TimeSpan.FromMilliseconds(200)
                                                                });

            peer.OnHistoryReplaced += (_, oldHistory, newHistory) => replaced.Add((oldHistory?.Epoch.Number ?? 0, newHistory.Epoch.Number));

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await peer.StartAsync(timeout.Token);

            var joined                 = peer.History!.Root.Id == checkpoint!.CommitId;

            var v2                     = Commit(leader, "v2", 3).Commit!;

            Assert.That(leader.TryCloseEpoch(Strong, out var final, out var closeError, T0.AddMinutes(4)),  Is.True, closeError);

            var tookFinal              = await Eventually(() => peer.History?.FinalCheckpoint?.Id == final!.Id);

            // The server moves on: the peer connects again, and starts the next epoch from the final checkpoint it has.
            Assert.That(leader.TryStartNextEpoch(out var next, out var startError),  Is.True, startError);

            next!.CheckpointSigners    = [ CheckpointTests.RNOSigner ];

            Assert.That(poiAPI.TryServeNextEpoch(next, out var serveError),  Is.True, serveError);

            var moved                  = await Eventually(() => peer.History?.Epoch.Number == 2 && peer.IsConnected);

            Assert.That(Commit(next, "v3", 5).IsCommitted);
            Assert.That(next.TryMakeCheckpoint(out var first, out var firstError, CreatedAt: T0.AddMinutes(6)),  Is.True, firstError);

            var inSync                 = await Eventually(() => peer.History?.Head.Commit.Id == next.Head.Commit.Id &&
                                                                peer.History.LatestCheckpoint?.Id == first!.Id);

            using var httpClient       = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/history/") };

            var epoch                  = JObject.Parse(await httpClient.GetStringAsync("epoch"));
            var epochs                 = JArray. Parse(await httpClient.GetStringAsync("epochs"));
            var closedCheckpoints      = JArray. Parse(await httpClient.GetStringAsync("epochs/1/checkpoints"));
            var proofText              = await httpClient.GetStringAsync($"epochs/1/commits/{v2.Id}/proof");

            Assert.That(RoamingNetworkCheckpointProof.TryParse(proofText, out var proof, out var proofError),  Is.True, proofError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(joined,                                             Is.True, "The peer did not join from the checkpoint.");
                Assert.That(tookFinal,                                          Is.True, "The peer did not take the final checkpoint.");
                Assert.That(moved,                                              Is.True, "The peer did not move to the next epoch.");
                Assert.That(peer.History!.Root.Id,                              Is.EqualTo(next.Root.Id), "The genesis it made is the one of the server.");
                Assert.That(inSync,                                             Is.True, "The peer did not follow the next epoch.");
                Assert.That(replaced,                                           Does.Contain((1u, 2u)));

                Assert.That(epoch["epoch"]?.Value<UInt32>(),                    Is.EqualTo(2));
                Assert.That(epoch["status"]?.Value<String>(),                   Is.EqualTo("active"));
                Assert.That(epoch["previous"]?["checkpointId"]?.Value<String>(), Is.EqualTo(final!.Id.ToString()));
                Assert.That(epochs.Select(item => (item["epoch"]!.Value<UInt32>(), item["status"]!.Value<String>())),
                            Is.EqualTo(new[] { (1u, "closed"), (2u, "active") }));
                Assert.That(closedCheckpoints.Last()?["@id"]?.Value<String>(),  Is.EqualTo(final.Id.ToString()));
                Assert.That(proof!.Verify(final, out var verifyError),          Is.True, verifyError);

            }

        }

        #endregion

        #region (static) Eventually(Condition)

        private static async Task<Boolean> Eventually(Func<Boolean> Condition)
        {

            for (var i = 0; i < 200; i++)
            {

                if (Condition())
                    return true;

                await Task.Delay(25);

            }

            return Condition();

        }

        #endregion

        #region An_epoch_after_the_first_one_survives_a_restart()

        [Test]
        public void An_epoch_after_the_first_one_survives_a_restart()
        {

            var (history, final)  = Close();
            var store             = new RoamingNetworkHistoryMemoryStore();

            Assert.That(history.TryStartNextEpoch(out var next, out var startError, Store: store),  Is.True, startError);

            next!.CheckpointSigners = [ CheckpointTests.RNOSigner ];
            Assert.That(next.TryMakeCheckpoint(out var first, out var firstError, CreatedAt: T0.AddMinutes(6)),  Is.True, firstError);

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError),  Is.True, loadError);
            Assert.That(loaded!.Compact(out var compactError),                                    Is.True, compactError);
            Assert.That(RoamingNetworkHistory.TryLoad(store, out var again, out var againError),  Is.True, againError);

            Assert.That(next.TryGetCheckpointSnapshotJSON(first!.Id, out var snapshot, out var snapshotError),  Is.True, snapshotError);

            var joined = RoamingNetworkHistory.TryFromCheckpointSnapshot(snapshot!.ToString(),
                                                                         out var fromCheckpoint,
                                                                         out var joinError,
                                                                         CheckpointTests.Verify);

            using (Assert.EnterMultipleScope())
            {

                foreach (var restarted in new[] { loaded, again! })
                {
                    Assert.That(restarted.Epoch,                Is.EqualTo(next.Epoch));
                    Assert.That(restarted.PreviousEpoch,        Is.EqualTo(RoamingNetworkEpochLink.To(final)));
                    Assert.That(restarted.Root.Id,              Is.EqualTo(next.Root.Id));
                    Assert.That(restarted.LatestCheckpoint?.Id, Is.EqualTo(first.Id));
                }

                Assert.That(joined,                             Is.True, joinError);
                Assert.That(fromCheckpoint!.Epoch,              Is.EqualTo(next.Epoch));
                Assert.That(fromCheckpoint.PreviousEpoch,       Is.EqualTo(RoamingNetworkEpochLink.To(final)));

            }

        }

        #endregion

    }

}
