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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// What bounds the change sets waiting for signatures (SPECIFICATION.md, section 4.2): a withdrawn one is refused
    /// again until the version it was made for is too old for it anyway, and the key of a withdrawal must be valid when
    /// it is taken as well (POI3-ISSUES.md S26); one party has a quota of change sets waiting, and a change set waits
    /// for a while at most (S24).
    /// </summary>
    [TestFixture]
    public class PendingChangeSetBoundsTests
    {

        #region Data

        private static readonly DateTimeOffset  T0  = DateTimeOffset.Parse("2026-10-22T12:00:00Z");

        #endregion


        #region (private static) NetworkWithGammaUntil(NotAfter)

        /// <summary>
        /// The network of the co-signing tests, whose holder Gamma signs with a key valid until the given time.
        /// </summary>
        private static RoamingNetwork NetworkWithGammaUntil(DateTimeOffset NotAfter)
        {

            var network       = CoSigningTests.Network();
            var json          = network.ToJSON(IncludeETags: false);
            var operatorJSON  = (JObject) json[RoamingNetwork.PropertyKeys.Operator]!;

            operatorJSON[RoamingNetworkSigningKeyCertificate.JSONProperty] = new JArray(new[] { "Alpha", "Beta", "Gamma" }.
                                                                                 Select (holder => RoamingNetworkSigningKeyCertificate.Create(TestParties.QuorumKey(network.Id, holder, POIKeyKind.Identity).Private,
                                                                                                                                              COSEAlgorithm.ES512,
                                                                                                                                              TestParties.OperatorId(network.Id),
                                                                                                                                              holder == "Gamma"
                                                                                                                                                  ? WithNotAfter(TestParties.QuorumKey(network.Id, holder, POIKeyKind.Signing).Public, NotAfter)
                                                                                                                                                  : TestParties.QuorumKey(network.Id, holder, POIKeyKind.Signing).Public,
                                                                                                                                              DateTimeOffset.Parse("2026-01-01T00:00:00Z"))).
                                                                                 OrderBy(certificate => certificate.SigningKey.Id, StringComparer.Ordinal).
                                                                                 Select (certificate => certificate.ToJSON(Embedded: true)));

            return RoamingNetwork.TryParse(json, out var withGamma, out var error)
                       ? withGamma
                       : throw new ArgumentException(error);

        }

        private static POIPublicKey WithNotAfter(POIPublicKey    Key,
                                                 DateTimeOffset  NotAfter)

            => new (Key.Key, Key.NotBefore, NotAfter, Key.Holder, Key.Usages.IsDefaultOrEmpty ? null : Key.Usages);

        #endregion

        #region (private static) Withdrawal(ChangeSet, Network, Holder, WithdrawnAt)

        private static RoamingNetworkChangeSetWithdrawal Withdrawal(RoamingNetworkChangeSet  ChangeSet,
                                                                    RoamingNetwork           Network,
                                                                    String                   Holder,
                                                                    DateTimeOffset?          WithdrawnAt   = null)
        {
            var (privateKey, publicKey) = TestParties.QuorumKey(Network.Id, Holder, POIKeyKind.Signing);
            return RoamingNetworkChangeSetWithdrawal.Create(ChangeSet, privateKey, publicKey.Id, COSEAlgorithm.ES256, WithdrawnAt ?? T0.AddMinutes(5));
        }

        #endregion


        #region A_withdrawn_change_set_is_refused_again()

        /// <summary>
        /// Withdrawn, a change set is refused again with the signatures it had, also with those of all its owners, to be
        /// committed at once, and also by a history loaded from the store; a change set of its own, with new signatures,
        /// is taken.
        /// </summary>
        [Test]
        public void A_withdrawn_change_set_is_refused_again()
        {

            var network    = CoSigningTests.Network();
            var store      = new RoamingNetworkHistoryMemoryStore();
            var history    = RoamingNetworkHistory.Start(network, Store: store);
            var changeSet  = CoSigningTests.ChangeSet(network);

            Assert.That(history.CoSign(CoSigningTests.SignedBy(changeSet, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));
            Assert.That(history.Withdraw(Withdrawal(changeSet, network, "Gamma")).Status,           Is.EqualTo(RoamingNetworkCoSigningStatus.Withdrawn));

            var again      = history.CoSign(CoSigningTests.SignedBy(changeSet, network, "Alpha"));
            var atOnce     = history.Commit(CoSigningTests.SignedBy(changeSet, network, "Alpha", "Beta"));

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError), Is.True, loadError);

            var afterLoad  = loaded!.CoSign(CoSigningTests.SignedBy(changeSet, network, "Alpha", "Beta"));

            // A change set of its own: created at another time, so with another identification, and new signatures.
            var anew       = loaded.CoSign(CoSigningTests.SignedBy(network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "quorum") ], CreatedAt: T0.AddMinutes(10)),
                                                                   network, "Alpha", "Beta"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(again.Status,              Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(again.ErrorResponse,       Does.Contain($"The change set '{changeSet.Id}' was withdrawn"));
                Assert.That(atOnce.Status,             Is.EqualTo(RoamingNetworkCommitStatus.Refused));
                Assert.That(atOnce.ErrorResponse,      Does.Contain("was withdrawn"));
                Assert.That(afterLoad.Status,          Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(afterLoad.ErrorResponse,   Does.Contain("was withdrawn"));
                Assert.That(anew.Status,               Is.EqualTo(RoamingNetworkCoSigningStatus.Committed), anew.ErrorResponse);
            }

        }

        #endregion

        #region A_withdrawal_is_forgotten_once_its_version_is_too_old()

        /// <summary>
        /// Once the version a withdrawn change set was made for is too old for a change set, it is refused for that, and
        /// the withdrawal is let go: the withdrawals kept bound themselves.
        /// </summary>
        [Test]
        public void A_withdrawal_is_forgotten_once_its_version_is_too_old()
        {

            var network    = CoSigningTests.Network();
            var history    = RoamingNetworkHistory.Start(network);
            var changeSet  = CoSigningTests.ChangeSet(network);

            history.MaxRevisionsBehindHead = 1;

            Assert.That(history.CoSign(CoSigningTests.SignedBy(changeSet, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));
            Assert.That(history.Withdraw(Withdrawal(changeSet, network, "Gamma")).Status,           Is.EqualTo(RoamingNetworkCoSigningStatus.Withdrawn));

            // Two revisions on: the genesis is too old for a change set now.
            for (var revision = 1; revision <= 2; revision++)
            {
                var head = history.Head.RoamingNetwork;
                Assert.That(history.Commit(CoSigningTests.SignedBy(head.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(head, "dataSource", null, $"v{revision}") ], CreatedAt: T0.AddMinutes(revision)),
                                                                   head, "Alpha", "Beta")).Status,
                            Is.EqualTo(RoamingNetworkCommitStatus.Committed));
            }

            var again = history.CoSign(CoSigningTests.SignedBy(changeSet, network, "Alpha", "Beta"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(again.Status,              Is.EqualTo(RoamingNetworkCoSigningStatus.Refused));
                Assert.That(again.ErrorResponse,       Does.Contain("revisions behind the head"));
                Assert.That(again.ErrorResponse,       Does.Not.Contain("was withdrawn"));
            }

        }

        #endregion

        #region The_key_of_a_withdrawal_must_be_valid_when_it_is_taken()

        /// <summary>
        /// A withdrawal is checked by its key at its own time and when a server takes it, as change sets are: one taken
        /// while its key is not valid is refused, whatever time it names.
        /// </summary>
        [Test]
        public void The_key_of_a_withdrawal_must_be_valid_when_it_is_taken()
        {

            // Gamma's key expires a day after the withdrawal it signs.
            var network    = NetworkWithGammaUntil(T0.AddDays(1));
            var history    = RoamingNetworkHistory.Start(network);
            var changeSet  = CoSigningTests.ChangeSet(network);

            Assert.That(history.CoSign(CoSigningTests.SignedBy(changeSet, network, "Alpha")).Status,  Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            var tooLate    = history.Withdraw(Withdrawal(changeSet, network, "Gamma"), At: T0.AddDays(2));
            var inTime     = history.Withdraw(Withdrawal(changeSet, network, "Gamma"), At: T0.AddMinutes(10));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tooLate.Status,            Is.EqualTo(RoamingNetworkCoSigningStatus.Refused), "taken after its key expired, though withdrawn before");
                Assert.That(tooLate.ErrorResponse,     Does.Contain($"is not valid at {T0.AddDays(2).ToISO8601()}"));
                Assert.That(inTime.Status,             Is.EqualTo(RoamingNetworkCoSigningStatus.Withdrawn), inTime.ErrorResponse);
            }

        }

        #endregion

        #region A_party_has_a_quota_of_change_sets_waiting()

        /// <summary>
        /// One party, the roaming network operator here, has as many change sets waiting as its quota lets it, counted by
        /// the first signature of each one, and not the places of all the others.
        /// </summary>
        [Test]
        public void A_party_has_a_quota_of_change_sets_waiting()
        {

            var network  = CoSigningTests.Network();
            var history  = RoamingNetworkHistory.Start(network);

            history.MaxPendingChangeSetsPerParty = 2;

            var results  = Enumerable.Range(1, 3).Select(number => history.CoSign(CoSigningTests.SignedBy(CoSigningTests.ChangeSet(network, $"quorum{number}"), network, "Alpha"))).ToArray();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(results.Select(result => result.Status),  Is.EqualTo(new[] { RoamingNetworkCoSigningStatus.Pending, RoamingNetworkCoSigningStatus.Pending, RoamingNetworkCoSigningStatus.Refused }));
                Assert.That(results[2].ErrorResponse,                 Does.Contain("as many as one party may have waiting"));
                Assert.That(history.PendingChangeSets,                Has.Count.EqualTo(2));
            }

        }

        #endregion

        #region A_change_set_waits_for_a_while_at_most()

        /// <summary>
        /// A change set waits from when it came here, for PendingChangeSetExpiry at most, also over a load from the store:
        /// one waiting longer is let go when the next one comes.
        /// </summary>
        [Test]
        public void A_change_set_waits_for_a_while_at_most()
        {

            var network  = CoSigningTests.Network();
            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = RoamingNetworkHistory.Start(network, Store: store);
            var now      = Timestamp.Now;
            var first    = CoSigningTests.ChangeSet(network, "first");
            var second   = CoSigningTests.ChangeSet(network, "second");
            var third    = CoSigningTests.ChangeSet(network, "third");

            history.PendingChangeSetExpiry = TimeSpan.FromDays(30);

            Assert.That(history.CoSign(CoSigningTests.SignedBy(first,  network, "Alpha"), At: now).Status,               Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));
            Assert.That(history.CoSign(CoSigningTests.SignedBy(second, network, "Alpha"), At: now.AddDays(20)).Status,   Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            var stillBoth  = history.PendingChangeSets.Select(pending => pending.Id).ToArray();

            // Loaded from the store: since when they wait is kept.
            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError), Is.True, loadError);

            loaded!.PendingChangeSetExpiry = TimeSpan.FromDays(30);

            Assert.That(loaded.CoSign(CoSigningTests.SignedBy(third, network, "Alpha"), At: now.AddDays(31)).Status,     Is.EqualTo(RoamingNetworkCoSigningStatus.Pending));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stillBoth,                                                Is.EqualTo(new[] { first.Id, second.Id }));
                Assert.That(loaded.PendingChangeSets.Select(pending => pending.Id),  Is.EqualTo(new[] { second.Id, third.Id }), "the first one waited longer than 30 days");
            }

        }

        #endregion

    }

}
