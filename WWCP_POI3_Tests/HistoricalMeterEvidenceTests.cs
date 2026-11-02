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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Historical meter evidence under the current local authorization, as WWCP_POI.TS 0.2.0 verifies it
    /// (tests/historicalMeasurements.test.ts), of its vector history-session.json: what it established there, here.
    /// </summary>
    [TestFixture]
    public class HistoricalMeterEvidenceTests
    {

        #region Data

        private const String EVSEId  = "DE*POI*E1*1";
        private const String Serial  = "CHARGY-POI-METER-001";

        private static readonly DateTimeOffset    Now    = DateTimeOffset.Parse("2026-11-01T00:16:21.000Z");
        private static readonly DateTimeOffset[]  Times  = [ DateTimeOffset.Parse("2026-06-01T10:00:00.000Z"),
                                                             DateTimeOffset.Parse("2026-06-01T10:30:00.000Z") ];

        private JObject                     poi        = null!;
        private RoamingNetworkTrustAnchor   anchor     = null!;
        private RoamingNetworkKeyStatement  statement  = null!;
        private RoamingNetworkTrustContext  context    = null!;
        private HistoricalMeterEvidence     evidence   = null!;

        #endregion

        [SetUp]
        public void SetUp()
        {

            poi        = (JObject) TSVectors.Read("history-session")["poiEvidence"]!;

            var bootstrap = (JObject) poi["bootstrap"]!;

            anchor     = TSVectors.TrustAnchorOf(bootstrap);
            statement  = TSVectors.KeyStatementOf(bootstrap);
            context    = TSVectors.ContextOf(anchor, statement, Now);
            evidence   = Parse(poi["historicalEvidence"]!);

        }

        private static HistoricalMeterEvidence Parse(JToken JSON)

            => HistoricalMeterEvidence.TryParse(JSON, out var evidence, out var error)
                   ? evidence
                   : throw new ArgumentException(error);

        private HistoricalMeterVerification Verified(IRoamingNetworkTrustAuthority? Current = null, HistoricalPolicy? Policy = null)
        {
            Assert.That(evidence.TryVerify(Current ?? context, EVSEId, Serial, Times, MeasurementKeyFormat.SPKIDER, out var verification, out var error, Policy),  Is.True, error);
            return verification!;
        }


        #region Expired_statements_and_certificates_verify_at_their_own_times()

        /// <summary>
        /// Key statements and certificates ended since verify at the times of the measurements, later checkpoints with their
        /// own authority: what WWCP_POI.TS establishes of the same vector.
        /// </summary>
        [Test]
        public void Expired_statements_and_certificates_verify_at_their_own_times()
        {

            var result = Verified();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(result.Bindings,                                              Has.Length.EqualTo(2));
                Assert.That(result.Bindings[0].Evidence.Association,                      Is.EqualTo(VerifiedMeterKeyBinding.HistoricalAttestation));
                Assert.That(result.Bindings[0].Evidence.AttestationValidUntil,            Is.EqualTo(DateTimeOffset.Parse("2026-06-01T11:00:00.000Z")));
                Assert.That(result.Bindings[0].Evidence.AttestationValidUntil,            Is.LessThan(context.EvaluatedAt));
                Assert.That(result.Bindings[0].Evidence.VerifiedAt,                       Is.EqualTo(context.EvaluatedAt));
                Assert.That(result.Bindings[0].Evidence.ValidUntil,                       Is.EqualTo(context.ValidUntil));
                Assert.That(result.Bindings[0].Evidence.CommitId.ToString(),              Is.EqualTo("be2d88ca3c83eb385ac813293d5c82061ac847cee44b384cbe115e00fb434fcc"));
                Assert.That(result.Bindings[0].Evidence.TreeRoot.ToString(),              Is.EqualTo("effectiveTree:sha256:hex:b169f5595b6797e21497b0aee261b00eadac6d182bcbade28d39071b01da619c"));
                Assert.That(result.Bindings[0].Evidence.HistoricalStatementDigest,        Is.EqualTo("fb5d392b09a76f5354c114f7d3376268f041d46682f32ff645a3e041ce4a350d"));
                Assert.That(result.Bindings[0].Evidence.HistoricalStatementDigest,        Is.Not.EqualTo(context.KeyStatementDigest));
                Assert.That(result.Bindings[0].Evidence.MeasurementTimes,                 Is.EqualTo(new[] { Times[0] }));
                Assert.That(result.Bindings[1].Evidence.MeasurementTimes,                 Is.EqualTo(new[] { Times[1] }));
                Assert.That(result.Bindings[0].Key.CanonicalId,                           Is.EqualTo("6a6bd5873a23a0eeb02d986504b6705df8584533e0135da9df52f790276efee7"));

                Assert.That(result.Versions,                                              Has.Length.EqualTo(1));
                Assert.That(result.Versions[0].Publication,                               Is.EqualTo("rno-checkpoint"));
                Assert.That(result.Versions[0].CheckpointId.ToString(),                   Is.EqualTo("a65327ba6821312a68d1b34bfb8a8ed5d5e6266c73a296fee8ba6e2c15ddfbb9"));
                Assert.That(result.IndependentTime,                                       Is.EqualTo("not-proven"));

            }

        }

        #endregion

        #region Witnesses_need_a_policy_of_the_verifier()

        /// <summary>
        /// Witnesses count only as the verifier trusts them: the signatures of the bundle do not authorize themselves.
        /// </summary>
        [Test]
        public void Witnesses_need_a_policy_of_the_verifier()
        {

            Assert.That(TrustedWitness.TryParseAll(TSVectors.ReadToken("history-witnesses"), out var witnesses, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(evidence.TryVerify(context, EVSEId, Serial, Times, MeasurementKeyFormat.SPKIDER, out _, out error, new HistoricalPolicy(RequiredWitnesses: 1)),  Is.False);
                Assert.That(error,  Does.Contain("quorum"));
                Assert.That(Verified(Policy: new HistoricalPolicy(witnesses, 1)).Versions[0].Publication,  Is.EqualTo("witnessed-checkpoint"));
            }

        }

        #endregion

        #region Missing_inclusion_stays_visible_malformed_inclusion_fails()

        /// <summary>
        /// A missing inclusion is shown as missing, fails where required, and a malformed one is never ignored.
        /// </summary>
        [Test]
        public void Missing_inclusion_stays_visible_malformed_inclusion_fails()
        {

            var changed = (JObject) poi["historicalEvidence"]!.DeepClone();
            var version = (JObject) changed["versions"]![0]!;
            version.Remove("commitProof");
            version.Remove("checkpoints");

            var malformed = (JObject) poi["historicalEvidence"]!.DeepClone();
            malformed["versions"]![0]!["commitProof"]!["leaf"]!["commitId"] = String.Concat(Enumerable.Repeat("ff", 32));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Parse(changed).TryVerify(context, EVSEId, Serial, Times, MeasurementKeyFormat.SPKIDER, out var result, out var error),  Is.True, error);
                Assert.That(result!.Versions[0].Publication,  Is.EqualTo("missing"));
                Assert.That(Parse(changed).TryVerify(context, EVSEId, Serial, Times, MeasurementKeyFormat.SPKIDER, out _, out error, new HistoricalPolicy(RequirePublication: true)),  Is.False);
                Assert.That(error,  Does.Contain("missing"));
                Assert.That(Parse(malformed).TryVerify(context, EVSEId, Serial, Times, MeasurementKeyFormat.SPKIDER, out _, out _),  Is.False);
            }

        }

        #endregion

        #region No_historical_association_otherwise()

        /// <summary>
        /// Another EVSE, another time, too little trust and a compromise establish no historical association; a compromise
        /// after the measurements does not end them.
        /// </summary>
        [Test]
        public void No_historical_association_otherwise()
        {

            var keyId = Verified().Bindings[0].Key.Id;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(evidence.TryVerify(context, "FOREIGN", Serial, Times, MeasurementKeyFormat.SPKIDER, out _, out _),  Is.False);

                Assert.That(evidence.TryVerify(context, EVSEId, Serial, [ DateTimeOffset.Parse("2026-06-02T00:00:00Z") ], MeasurementKeyFormat.SPKIDER, out _, out var error),  Is.False);
                Assert.That(error,  Does.Contain("coverage"));

                Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, statement, [], out _, out _, At: Now),  Is.False);

                Assert.That(evidence.TryVerify(context, EVSEId, Serial, Times, MeasurementKeyFormat.SPKIDER, out _, out error, new HistoricalPolicy(Revocations: [ new KeyRevocation(keyId, DateTimeOffset.UnixEpoch) ])),  Is.False);
                Assert.That(error,  Does.Contain("coverage"));

                Assert.That(Verified(Policy: new HistoricalPolicy(Revocations: [ new KeyRevocation(keyId, DateTimeOffset.Parse("2026-07-01T00:00:00Z")) ])).Bindings,  Has.Length.EqualTo(2));

            }

        }

        #endregion

        #region A_historical_permission_works_offline_of_an_expired_reference()

        /// <summary>
        /// A historical trust context of a saved key statement of reference, ended today, verifies offline; it is a deadline
        /// of one minute, and needs locally trusted keys.
        /// </summary>
        [Test]
        public void A_historical_permission_works_offline_of_an_expired_reference()
        {

            var oldOperator = (JObject) poi["historicalEvidence"]!["versions"]![0]!["chargingStationProof"]!["roamingNetwork"]!["operator"]!;

            Assert.That(RoamingNetworkKeyStatement.TryParse((JObject) oldOperator["keyStatement"]!, out var oldStatement, out var error),  Is.True, error);
            Assert.That(oldStatement!.ValidUntil,  Is.LessThan(context.EvaluatedAt));

            Assert.That(HistoricalTrustContext.TryCreate(anchor, oldStatement, anchor.Keys, out var permission, out error, Now: Now),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Verified(permission).Bindings,  Has.Length.EqualTo(2));
                Assert.That(permission!.ValidUntil,         Is.EqualTo(Now.AddMinutes(1)));
                Assert.That(HistoricalTrustContext.TryCreate(anchor, oldStatement, [], out _, out _, Now: Now),  Is.False);
            }

        }

        #endregion

        #region The_evidence_reads_back_as_written()

        /// <summary>
        /// The evidence an issuer makes reads back as written, and names nothing else.
        /// </summary>
        [Test]
        public void The_evidence_reads_back_as_written()
        {

            var json = evidence.ToJSON();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(JToken.DeepEquals(json, poi["historicalEvidence"]),  Is.True);
                json["unknown"] = 1;
                Assert.That(HistoricalMeterEvidence.TryParse(json, out _, out var error),  Is.False);
                Assert.That(error,  Does.Contain("'unknown'"));
            }

        }

        #endregion

    }

}
