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

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Co-signing (SPECIFICATION.md, section 4.2): the holders of a quorum sign a change set each on its own, at the place
    /// where it waits for the signatures still missing; once its owners signed it, it is committed. It waits only once an
    /// owner signed it, until it is committed, withdrawn by an owner, or fits no version, and the store keeps it.
    /// </summary>
    [TestFixture]
    public class CoSigningTests
    {

        #region Data

        private static readonly DateTimeOffset  T0       = DateTimeOffset.Parse("2026-10-22T12:00:00Z");
        private static readonly String[]        Holders  = [ "Alpha", "Beta", "Gamma" ];

        #endregion

        #region (static) Network()

        /// <summary>
        /// The network of the station proof tests, its operator with the signing and identity keys of three holders, two
        /// of which must sign what it owns.
        /// </summary>
        private static RoamingNetwork Network()
        {

            var network       = StationProofTests.NewNetwork();
            var json          = network.ToJSON(IncludeETags: false);
            var operatorJSON  = (JObject) json[RoamingNetwork.PropertyKeys.Operator]!;

            // The signing keys of each holder, certified by its identity key.
            operatorJSON.Remove(POIPartyKeys.SigningKeysProperty);
            operatorJSON[RoamingNetworkSigningKeyCertificate.JSONProperty] = new JArray(Holders.Select(holder => RoamingNetworkSigningKeyCertificate.Create(TestParties.QuorumKey(network.Id, holder, POIKeyKind.Identity).Private,
                                                                                                                             COSEAlgorithm.ES512,
                                                                                                                             TestParties.OperatorId(network.Id),
                                                                                                                             TestParties.QuorumKey(network.Id, holder, POIKeyKind.Signing).Public,
                                                                                                                             DateTimeOffset.Parse("2026-01-01T00:00:00Z")).ToJSON(Embedded: true)));
            operatorJSON[POIPartyKeys.IdentityKeysProperty]  = new JArray(Holders.Select(holder => TestParties.QuorumKey(network.Id, holder, POIKeyKind.Identity).Public.ToJSON(Embedded: true)));
            operatorJSON[POIPartyKeys.MinConsensusProperty]  = 2;

            return RoamingNetwork.TryParse(json, out var quorumNetwork, out var error)
                       ? quorumNetwork
                       : throw new ArgumentException(error);

        }

        #endregion

        #region (static) ChangeSet(Network, DataSource = "quorum")

        /// <summary>
        /// An unsigned change set of the data source of the given network, which its operator owns.
        /// </summary>
        private static RoamingNetworkChangeSet ChangeSet(RoamingNetwork  Network,
                                                         String          DataSource   = "quorum")

            => Network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(Network, "dataSource", null, DataSource) ], CreatedAt: T0);

        #endregion

        #region (static) SignedBy(ChangeSet, Network, Holders)

        /// <summary>
        /// The given change set signed by the signing keys of the given holders of the quorum.
        /// </summary>
        private static RoamingNetworkChangeSet SignedBy(RoamingNetworkChangeSet  ChangeSet,
                                                        RoamingNetwork           Network,
                                                        params String[]          Holders)
        {

            foreach (var holder in Holders)
            {
                var (privateKey, publicKey) = TestParties.QuorumKey(Network.Id, holder, POIKeyKind.Signing);
                ChangeSet = ChangeSet.Sign(privateKey, publicKey.Id, COSEAlgorithm.ES256);
            }

            return ChangeSet;

        }

        #endregion

        #region (static) Withdrawal(ChangeSet, Network, Holder)

        private static RoamingNetworkChangeSetWithdrawal Withdrawal(RoamingNetworkChangeSet  ChangeSet,
                                                                    RoamingNetwork           Network,
                                                                    String                   Holder)
        {
            var (privateKey, publicKey) = TestParties.QuorumKey(Network.Id, Holder, POIKeyKind.Signing);
            return RoamingNetworkChangeSetWithdrawal.Create(ChangeSet, privateKey, publicKey.Id, COSEAlgorithm.ES256, T0.AddMinutes(5));
        }

        #endregion


        #region A_change_set_waits_until_a_quorum_signed_it()

        [Test]
        public void A_change_set_waits_until_a_quorum_signed_it()
        {

            var network    = Network();
            var history    = RoamingNetworkHistory.Start(network);
            var changeSet  = ChangeSet(network);
            var announced  = new List<RoamingNetworkCoSigningResult>();

            history.OnPendingChangeSet += announced.Add;

            var alpha      = history.CoSign(SignedBy(changeSet, network, "Alpha"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alpha.Status,                                Is.EqualTo(RoamingNetworkCoSigningStatus.Pending), alpha.ErrorResponse);
                Assert.That(alpha.Missing,                               Has.Count.EqualTo(1));
                Assert.That(alpha.Missing![0],                           Does.Contain("signing keys of 2 holders of"));
                Assert.That(history.PendingChangeSets.Select(pending => pending.Id),  Is.EqualTo(new[] { changeSet.Id }));
                Assert.That(history.Head.RoamingNetwork.DataSource,      Is.Not.EqualTo("quorum"));
                Assert.That(history.TryGetPendingChangeSet(changeSet.Id, out var waiting),  Is.True);
                Assert.That(waiting!.ChangeSet!.Signatures,              Has.Length.EqualTo(1));
            }

            // The same holder again counts once: it still waits, unchanged.
            var again      = history.AddPendingSignatures(changeSet.Id, SignedBy(changeSet, network, "Alpha").Signatures);
            var beta       = history.AddPendingSignatures(changeSet.Id, SignedBy(changeSet, network, "Beta"). Signatures);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(again.Status,                                Is.EqualTo(RoamingNetworkCoSigningStatus.Pending), again.ErrorResponse);
                Assert.That(beta.Status,                                 Is.EqualTo(RoamingNetworkCoSigningStatus.Committed), beta.ErrorResponse);
                Assert.That(beta.Commit,                                 Is.Not.Null);
                Assert.That(beta.ChangeSet!.Signatures,                  Has.Length.EqualTo(2));
                Assert.That(history.Head.Commit.Id,                      Is.EqualTo(beta.Commit!.Id));
                Assert.That(history.Head.RoamingNetwork.DataSource,      Is.EqualTo("quorum"));
                Assert.That(history.PendingChangeSets,                   Is.Empty);
                Assert.That(announced.Select(result => result.Status),   Is.EqualTo(new[] { RoamingNetworkCoSigningStatus.Pending, RoamingNetworkCoSigningStatus.Committed }));
                Assert.That(history.CoSign(beta.ChangeSet).Status,       Is.EqualTo(RoamingNetworkCoSigningStatus.AlreadyCommitted));
            }

        }

        #endregion

        #region A_change_set_signed_by_its_owners_is_committed_at_once()

        [Test]
        public void A_change_set_signed_by_its_owners_is_committed_at_once()
        {

            var network  = Network();
            var history  = RoamingNetworkHistory.Start(network);
            var result   = history.CoSign(SignedBy(ChangeSet(network), network, "Alpha", "Gamma"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status,                           Is.EqualTo(RoamingNetworkCoSigningStatus.Committed), result.ErrorResponse);
                Assert.That(history.Head.RoamingNetwork.DataSource,  Is.EqualTo("quorum"));
                Assert.That(history.PendingChangeSets,               Is.Empty);
            }

        }

        #endregion

        #region A_change_set_waits_only_once_an_owner_signed_it()

        [Test]
        public void A_change_set_waits_only_once_an_owner_signed_it()
        {

            var network      = Network();
            var history      = RoamingNetworkHistory.Start(network);
            var changeSet    = ChangeSet(network);

            var unsigned     = history.CoSign(changeSet);
            var byOperator   = history.CoSign(changeSet.SignedBy(TestParties.Party(ChargingStationOperator_Id.Parse("DE*GEF"))));
            var invalid      = history.CoSign(changeSet.WithSignature(new RoamingNetworkChangeSetSignature(COSEAlgorithm.ES256.Name,
                                                                                                           TestParties.QuorumKey(network.Id, "Alpha").Public.Id,
                                                                                                           Convert.ToBase64String(new Byte[64]))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unsigned.Status,             Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(byOperator.Status,           Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(byOperator.ErrorResponse,    Does.Contain("an owner of one of its changes"));
                Assert.That(invalid.Status,              Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(invalid.ErrorResponse,       Does.Contain("is invalid"));
                Assert.That(history.PendingChangeSets,   Is.Empty);
            }

        }

        #endregion

        #region Another_change_set_of_the_same_identification_is_refused()

        [Test]
        public void Another_change_set_of_the_same_identification_is_refused()
        {

            var network   = Network();
            var history   = RoamingNetworkHistory.Start(network);
            var waiting   = ChangeSet(network);
            // Another change set can be named so only by an identification other than its content's.
            var content   = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "other") ], CreatedAt: T0);
            var other     = new RoamingNetworkChangeSet(waiting.Id, content.RoamingNetworkId, content.CreatedAt, content.Changes, content.BeforeETags, content.AfterETags, content.Description, content.Metadata);

            Assert.That(history.CoSign(SignedBy(waiting, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            var result    = history.CoSign(SignedBy(other, network, "Beta"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status,                                Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(result.ErrorResponse,                         Does.Contain("is not identified by its content"));
                Assert.That(history.PendingChangeSets.Single().Signatures,  Has.Length.EqualTo(1));
                Assert.That(history.Head.RoamingNetwork.DataSource,       Is.Not.EqualTo("other"));
            }

        }

        #endregion

        #region A_change_set_waiting_is_not_taken_by_another_one_naming_it()

        /// <summary>
        /// Another change set committed under the identification of one waiting for signatures, or naming it as the one it
        /// was rebased from, made it count as committed: it was let go of, told as committed, and never applied (finding 4
        /// of the review in WWCP_POI.TS). Identified by its content, neither is taken, nor one without changes, and the one
        /// waiting is committed once signed.
        /// </summary>
        [Test]
        public void A_change_set_waiting_is_not_taken_by_another_one_naming_it()
        {

            var network      = Network();
            var history      = RoamingNetworkHistory.Start(network);
            var waiting      = ChangeSet(network);
            var announced    = new List<RoamingNetworkCoSigningResult>();

            Assert.That(history.CoSign(SignedBy(waiting, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            history.OnPendingChangeSet += announced.Add;

            // Another change set: under the identification of the one waiting, ...
            var other        = ChangeSet(network, "other");
            var sameId       = new RoamingNetworkChangeSet(waiting.Id, other.RoamingNetworkId, other.CreatedAt, other.Changes, other.BeforeETags, other.AfterETags, other.Description, other.Metadata);

            // ... naming it, with its ETags, as the one it was rebased from, ...
            var metadata     = other.Metadata;
            metadata[RoamingNetworkChangeSet.PropertyKeys.RebasedFrom] = new JArray(new JObject(
                                                                             new JProperty(RoamingNetworkChangeSet.PropertyKeys.Id,           waiting.Id.ToString()),
                                                                             new JProperty(RoamingNetworkChangeSet.PropertyKeys.BeforeETags,  new JArray(waiting.BeforeETags.Select(eTag => eTag.ToJSON()))),
                                                                             new JProperty(RoamingNetworkChangeSet.PropertyKeys.AfterETags,   new JArray(waiting.AfterETags. Select(eTag => eTag.ToJSON())))));

            var rebasedFrom  = new RoamingNetworkChangeSet(other.Id, other.RoamingNetworkId, other.CreatedAt, other.Changes, other.BeforeETags, other.AfterETags, other.Description, metadata).WithContentId();

            // ... and one without changes.
            var empty        = network.CreateChangeSet([], CreatedAt: T0);

            var byId         = history.Commit(SignedBy(sameId,      network, "Beta", "Gamma"));
            var byRebase     = history.Commit(SignedBy(rebasedFrom, network, "Beta", "Gamma"));
            var none         = history.Commit(SignedBy(empty,       network, "Beta", "Gamma"));
            var noneWaiting  = history.CoSign(SignedBy(empty,       network, "Beta"));

            var coSigned     = history.AddPendingSignatures(waiting.Id, SignedBy(waiting, network, "Beta").Signatures);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(byId.Status,                                Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(byId.ErrorResponse,                         Does.Contain("is not identified by its content"));
                Assert.That(byRebase.Status,                            Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(byRebase.ErrorResponse,                     Does.Contain("not the change set it was rebased from"));
                Assert.That(none.Status,                                Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(none.ErrorResponse,                         Does.Contain("has no changes"));
                Assert.That(noneWaiting.Status,                         Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));

                Assert.That(coSigned.Status,                            Is.EqualTo(RoamingNetworkCoSigningStatus.Committed), coSigned.ErrorResponse);
                Assert.That(history.Head.RoamingNetwork.DataSource,     Is.EqualTo("quorum"));
                Assert.That(history.PendingChangeSets,                  Is.Empty);
                Assert.That(announced.Single().Status,                  Is.EqualTo(RoamingNetworkCoSigningStatus.Committed));
                Assert.That(announced.Single().Commit!.Id,              Is.EqualTo(coSigned.Commit!.Id));

            }

        }

        #endregion

        #region An_owner_withdraws_a_change_set()

        [Test]
        public void An_owner_withdraws_a_change_set()
        {

            var network      = Network();
            var history      = RoamingNetworkHistory.Start(network);
            var changeSet    = ChangeSet(network);

            Assert.That(history.CoSign(SignedBy(changeSet, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            var (operatorKey, _)  = TestParties.Key(TestParties.Party(ChargingStationOperator_Id.Parse("DE*GEF")), POIKeyKind.Signing);
            var byOperator   = history.Withdraw(RoamingNetworkChangeSetWithdrawal.Create(changeSet, operatorKey, POIPublicKey.From(operatorKey).Id, COSEAlgorithm.ES256, T0));
            var byGamma      = history.Withdraw(Withdrawal(changeSet, network, "Gamma"));
            var again        = history.Withdraw(Withdrawal(changeSet, network, "Gamma"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(byOperator.Status,                   Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(byGamma.Status,                      Is.EqualTo(RoamingNetworkCoSigningStatus.Withdrawn), byGamma.ErrorResponse);
                Assert.That(again.Status,                        Is.EqualTo(RoamingNetworkCoSigningStatus.NotFound));
                Assert.That(history.PendingChangeSets,           Is.Empty);
                Assert.That(history.AddPendingSignatures(changeSet.Id, SignedBy(changeSet, network, "Beta").Signatures).Status,
                                                                 Is.EqualTo(RoamingNetworkCoSigningStatus.NotFound));
            }

        }

        #endregion

        #region A_tampered_withdrawal_is_refused()

        [Test]
        public void A_tampered_withdrawal_is_refused()
        {

            var network    = Network();
            var history    = RoamingNetworkHistory.Start(network);
            var changeSet  = ChangeSet(network);

            Assert.That(history.CoSign(SignedBy(changeSet, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            var withdrawal = Withdrawal(changeSet, network, "Gamma");
            var tampered   = new RoamingNetworkChangeSetWithdrawal(withdrawal.RoamingNetworkId, withdrawal.ChangeSetId, withdrawal.WithdrawnAt.AddHours(1), withdrawal.Algorithm, withdrawal.KeyId, withdrawal.Value);
            var result     = history.Withdraw(tampered);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Status,                Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(result.ErrorResponse,         Does.Contain("is invalid"));
                Assert.That(history.PendingChangeSets,    Has.Count.EqualTo(1));
                Assert.That(RoamingNetworkChangeSetWithdrawal.TryParse(withdrawal.ToJSON(), out var read, out var error),  Is.True, error);
                Assert.That(read,                         Is.EqualTo(withdrawal));
                Assert.That(RoamingNetworkChangeSetWithdrawal.TryParse(withdrawal.ToJSON().ToString(), out var fromText, out var textError),  Is.True, textError);
                Assert.That(fromText,                     Is.EqualTo(withdrawal));
            }

        }

        #endregion

        #region A_change_set_committed_elsewhere_waits_no_longer()

        [Test]
        public void A_change_set_committed_elsewhere_waits_no_longer()
        {

            var network    = Network();
            var history    = RoamingNetworkHistory.Start(network);
            var changeSet  = ChangeSet(network);
            var announced  = new List<RoamingNetworkCoSigningResult>();

            Assert.That(history.CoSign(SignedBy(changeSet, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            history.OnPendingChangeSet += announced.Add;

            var commit     = history.Commit(SignedBy(changeSet, network, "Beta", "Gamma"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(commit.IsCommitted,                         Is.True, commit.ErrorResponse);
                Assert.That(history.PendingChangeSets,                  Is.Empty);
                Assert.That(announced.Single().Status,                  Is.EqualTo(RoamingNetworkCoSigningStatus.Committed));
                Assert.That(announced.Single().Commit!.Id,              Is.EqualTo(commit.Commit!.Id));
            }

        }

        #endregion

        #region The_store_keeps_the_change_sets_waiting()

        [Test]
        public void The_store_keeps_the_change_sets_waiting()
        {

            var network    = Network();
            var store      = new RoamingNetworkHistoryMemoryStore();
            var history    = RoamingNetworkHistory.Start(network, Store: store);
            var first      = ChangeSet(network);
            var second     = ChangeSet(network, "second");
            var third      = ChangeSet(network, "third");

            Assert.That(history.CoSign(SignedBy(first,  network, "Alpha")).Status,           Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));
            Assert.That(history.CoSign(SignedBy(second, network, "Alpha")).Status,           Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));
            Assert.That(history.CoSign(SignedBy(third,  network, "Alpha")).Status,           Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));
            Assert.That(history.AddPendingSignatures(second.Id, SignedBy(second, network, "Gamma").Signatures).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Committed));
            Assert.That(history.Withdraw(Withdrawal(third, network, "Beta")).Status,         Is.EqualTo(RoamingNetworkCoSigningStatus.Withdrawn));

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(loaded!.PendingChangeSets.Select(pending => pending.Id),  Is.EqualTo(new[] { first.Id }));
                Assert.That(loaded.PendingChangeSets[0].Signatures,                   Has.Length.EqualTo(1));
                Assert.That(loaded.Head.RoamingNetwork.DataSource,                    Is.EqualTo("second"));
            }

            // Made for the genesis, it is committed on a branch and merged with the head.
            var beta       = loaded.AddPendingSignatures(first.Id, SignedBy(first, network, "Beta").Signatures);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beta.Status,                  Is.EqualTo(RoamingNetworkCoSigningStatus.Committed).Or.EqualTo(RoamingNetworkCoSigningStatus.Refused), beta.ErrorResponse);
                Assert.That(loaded.PendingChangeSets,     Is.Empty);
            }

        }

        #endregion

        #region The_number_of_change_sets_waiting_is_limited()

        [Test]
        public void The_number_of_change_sets_waiting_is_limited()
        {

            var network  = Network();
            var history  = RoamingNetworkHistory.Start(network);

            history.MaxPendingChangeSets = 2;

            var results  = Enumerable.Range(1, 3).Select(number => history.CoSign(SignedBy(ChangeSet(network, $"quorum{number}"), network, "Alpha"))).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results.Select(result => result.Status),  Is.EqualTo(new[] { RoamingNetworkCoSigningStatus.Pending, RoamingNetworkCoSigningStatus.Pending, RoamingNetworkCoSigningStatus.Refused }));
                Assert.That(results[2].ErrorResponse,                 Does.Contain("as many as may wait"));
                Assert.That(history.PendingChangeSets,                Has.Count.EqualTo(2));
            }

        }

        #endregion

    }

}
