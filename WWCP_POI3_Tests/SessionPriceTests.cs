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

using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The assignment of a price to a charging session by its CPO, and its publication apart, as WWCP_POI.TS 0.2.0
    /// verifies them (tests/sessionPrices.test.ts), of its vectors session-price-ed, -pqc and -decimal.json.
    /// </summary>
    [TestFixture]
    public class SessionPriceTests
    {

        #region Data

        private const String EVSEId  = "DE*POI*E1*1";
        private const String Serial  = "CHARGY-POI-METER-001";

        private static readonly DateTimeOffset  Now      = DateTimeOffset.Parse("2026-11-01T00:16:21Z");
        private static readonly DateTimeOffset  Started  = DateTimeOffset.Parse("2026-06-01T10:00:00Z");

        #endregion

        #region (private) Example(Name)

        private sealed record Example(HistoricalTrustContext  Context,
                                      JObject                 Evidence,
                                      String                  StartDocumentSHA256,
                                      RoamingNetworkTrustAnchor   Anchor,
                                      RoamingNetworkKeyStatement  Statement);

        private static Example Of(String Name = "ed")
        {

            var raw        = TSVectors.Read($"session-price-{Name}");
            var poi        = (JObject) raw["poiEvidence"]!;
            var bootstrap  = (JObject) poi["bootstrap"]!;
            var anchor     = TSVectors.TrustAnchorOf(bootstrap);
            var statement  = TSVectors.KeyStatementOf(bootstrap);

            Assert.That(HistoricalTrustContext.TryCreate(anchor, statement, anchor.Keys, out var context, out var error, Now: Now),  Is.True, error);

            return new Example(context!,
                               (JObject) poi["priceEvidence"]!,
                               Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(raw["ocmfBegin"]!.Value<String>()!))),
                               anchor,
                               statement);

        }

        private static SessionPriceVerification Verify(Example Example, JObject? Evidence = null, HistoricalPolicy? Policy = null, String? EVSE = null)
        {

            Assert.That(SessionPriceEvidence.TryParse(Evidence ?? Example.Evidence, out var evidence, out var error),  Is.True, error);
            Assert.That(evidence!.TryVerify(Example.Context, EVSE ?? EVSEId, Serial, Started, Example.StartDocumentSHA256, out var result, out error, Policy),  Is.True, error);

            return result!;

        }

        #endregion


        #region Generic_algorithms_and_a_fixed_price_at_the_start()

        /// <summary>
        /// Ed25519 and ML-DSA-65 price statements and assignments, a price fixed at the original start, published.
        /// </summary>
        [TestCase("ed",  "Ed25519")]
        [TestCase("pqc", "ML-DSA-65")]
        public void Generic_algorithms_and_a_fixed_price_at_the_start(String Name, String Algorithm)
        {

            var result = Verify(Of(Name));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Signatures,                    Is.EqualTo("valid"), String.Join("; ", result.Diagnostics));
                Assert.That(result.Agreement,                     Is.EqualTo("valid"));
                Assert.That(result.Applicability,                 Is.EqualTo("bound"));
                Assert.That(result.Publication,                   Is.EqualTo("valid"), String.Join("; ", result.Diagnostics));
                Assert.That(result.Selected!.Statement.Algorithm, Is.EqualTo(Algorithm));
                Assert.That(result.Selected.Rates!.EnergyPerKWh,  Is.EqualTo("0.35"));
                Assert.That(result.PublicationBeforeStart,        Is.EqualTo("not-proven"));
                Assert.That(result.CustomerConsent,               Is.EqualTo("not-proven"));
                Assert.That(result.WitnessedBy,                   Is.Empty);
            }

        }

        #endregion

        #region Signed_decimals_are_kept_as_written()

        /// <summary>
        /// Signed decimals survive writing and reading again, never normalized through binary floating point.
        /// </summary>
        [Test]
        public void Signed_decimals_are_kept_as_written()
        {

            var example  = Of("decimal");
            var written  = (JObject) POIJSON.Read(example.Evidence.ToString());
            var result   = Verify(example, written);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Selected!.Rates!.EnergyPerKWh,  Is.EqualTo("0.3500"));
                Assert.That(result.Applicability,                  Is.EqualTo("bound"));
            }

        }

        #endregion

        #region A_missing_or_changed_assignment_binds_nothing()

        /// <summary>
        /// Without an assignment a price is unbound; one of another start document is invalid.
        /// </summary>
        [Test]
        public void A_missing_or_changed_assignment_binds_nothing()
        {

            var example   = Of();
            var without   = (JObject) example.Evidence.DeepClone();
            without.Remove("agreement");

            var changed   = (JObject) example.Evidence.DeepClone();
            changed["agreement"]!["startDocumentSHA256"] = String.Concat(Enumerable.Repeat("ff", 32));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Verify(example, without).Applicability,  Is.EqualTo("unbound"));
                Assert.That(Verify(example, changed).Agreement,      Is.EqualTo("invalid"));
            }

        }

        #endregion

        #region Publication_is_apart_and_needs_the_witnesses_of_the_verifier()

        /// <summary>
        /// The publication is checked apart: missing without inclusion, invalid where witnesses are required that the
        /// verifier does not trust, valid with those it trusts.
        /// </summary>
        [Test]
        public void Publication_is_apart_and_needs_the_witnesses_of_the_verifier()
        {

            var example   = Of();
            var pending   = (JObject) example.Evidence.DeepClone();
            pending.Remove("priceStatementProofs");
            pending.Remove("checkpoints");

            Assert.That(TrustedWitness.TryParseAll(TSVectors.ReadToken("history-witnesses"), out var witnesses, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Verify(example, pending).Applicability,                                              Is.EqualTo("bound"));
                Assert.That(Verify(example, pending).Publication,                                                Is.EqualTo("missing"));
                Assert.That(Verify(example, Policy: new HistoricalPolicy(RequiredWitnesses: 1)).Publication,     Is.EqualTo("invalid"));
                Assert.That(Verify(example, Policy: new HistoricalPolicy(witnesses, 1)).Publication,             Is.EqualTo("valid"));
                Assert.That(Verify(example, Policy: new HistoricalPolicy(witnesses, 1)).WitnessedBy,             Is.EqualTo(new[] { "CHARGY-DEMO-WITNESS" }));
            }

        }

        #endregion

        #region Foreign_subjects_and_revoked_keys_are_refused()

        /// <summary>
        /// Another EVSE is refused, a revoked CPO key makes the signatures invalid, and nobody is authorized by the evidence.
        /// </summary>
        [Test]
        public void Foreign_subjects_and_revoked_keys_are_refused()
        {

            var example = Of();
            var keyId   = example.Evidence["priceStatements"]![0]!["keyId"]!.Value<String>()!;

            Assert.That(SessionPriceEvidence.TryParse(example.Evidence, out var evidence, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(evidence!.TryVerify(example.Context, "FOREIGN", Serial, Started, example.StartDocumentSHA256, out _, out _),  Is.False);
                Assert.That(Verify(example, Policy: new HistoricalPolicy(Revocations: [ new KeyRevocation(keyId, DateTimeOffset.UnixEpoch) ])).Signatures,  Is.EqualTo("invalid"));
                Assert.That(HistoricalTrustContext.TryCreate(example.Anchor, example.Statement, [], out _, out _, Now: Now),  Is.False);
            }

        }

        #endregion

        #region A_CPO_assigns_a_price_as_WWCP_POI_TS_verifies_it()

        /// <summary>
        /// An assignment a CPO makes here is the one of the vector, written and signed alike by the CPO test key of the seed
        /// 161 (Ed25519 signs deterministically), and the evidence reads back as written.
        /// </summary>
        [Test]
        public void A_CPO_assigns_a_price_as_WWCP_POI_TS_verifies_it()
        {

            var example  = Of();
            var given    = (JObject) example.Evidence["agreement"]!;

            Assert.That(SessionPriceAgreement.TryParse(given, out var agreement, out var error),  Is.True, error);
            Assert.That(SessionPriceEvidence.TryParse(example.Evidence, out var evidence, out error),  Is.True, error);

            var made = SessionPriceAgreement.Create(new Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters(Enumerable.Repeat((Byte) 161, 32).ToArray()),
                                                    agreement!.KeyId,
                                                    COSEAlgorithm.Ed25519,
                                                    RoamingNetwork_Id.Parse(agreement.RoamingNetworkId),
                                                    agreement.EVSEId,
                                                    agreement.MeterSerialNumber,
                                                    agreement.ChargingStarted,
                                                    agreement.StartDocumentSHA256,
                                                    agreement.ChargingTariffId,
                                                    agreement.PriceStatementDigest,
                                                    ChargingStationOperator_Id.Parse(agreement.Signer),
                                                    SignedAt: agreement.SignedAt);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(made.JSON.ToString(),                                        Is.EqualTo(given.ToString()));
                Assert.That(JToken.DeepEquals(evidence!.ToJSON(), example.Evidence),     Is.True);
                Assert.That(Verify(example).Selected!.Digest,                            Is.EqualTo(agreement.PriceStatementDigest));
            }

        }

        #endregion

    }

}
