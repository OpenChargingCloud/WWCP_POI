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

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The public retrieval of a price before the signed start of a session, witnessed by organizations a verifier accepted,
    /// as WWCP_POI.TS 0.1.2-dev.0 verifies it (tests/pricePublication.test.ts), of its vector price-publication.json: the
    /// witness A of the Ed25519 seeds 181 and 184, the witness B of the ML-DSA-65 seed 183, the CPO of the seed 161.
    /// </summary>
    [TestFixture]
    public class PricePublicationTests
    {

        #region (private) Example(Variant)

        private sealed record Example(JObject                   Raw,
                                      HistoricalTrustContext    Context,
                                      JObject                   Evidence,
                                      WitnessPublicationPolicy  Policy,
                                      DateTimeOffset            ChargingStarted,
                                      String                    StartDocumentSHA256);

        private static Example Of(String Variant = "session-before-start")
        {

            var raw        = TSVectors.Read("price-publication");
            var bootstrap  = (JObject) raw["session"]!["poiEvidence"]!["bootstrap"]!;
            var anchor     = TSVectors.TrustAnchorOf(bootstrap);

            Assert.That(HistoricalTrustContext.TryCreate(anchor, TSVectors.KeyStatementOf(bootstrap), anchor.Keys, out var context, out var error, Now: DateTimeOffset.Parse(raw["now"]!.Value<String>()!)),  Is.True, error);

            return new Example(raw,
                               context!,
                               (JObject) raw["variants"]![Variant + ".chargy"]!,
                               PolicyOf(raw["policy"]!),
                               DateTimeOffset.FromUnixTimeMilliseconds(raw["request"]!["chargingStarted"]!.Value<Int64>()),
                               raw["request"]!["startDocumentSHA256"]!.Value<String>()!);

        }

        private static WitnessPublicationPolicy PolicyOf(JToken JSON)

            => WitnessPublicationPolicy.TryParse(JSON, out var policy, out var error)
                   ? policy
                   : throw new ArgumentException(error);

        private static SessionPriceVerification Verify(Example Example, WitnessPublicationPolicy? Policy = null, Boolean WithPolicy = true)
        {

            Assert.That(SessionPriceEvidence.TryParse(Example.Evidence, out var evidence, out var error),  Is.True, error);
            Assert.That(evidence!.TryVerify(Example.Context, "DE*POI*E1*1", "CHARGY-POI-METER-001", Example.ChargingStarted, Example.StartDocumentSHA256,
                                            out var result, out error, PublicationPolicy: WithPolicy ? Policy ?? Example.Policy : null),  Is.True, error);

            return result!;

        }

        #endregion


        #region Two_organizations_witness_the_price_before_the_start()

        /// <summary>
        /// Ed25519 and ML-DSA-65 witnesses of two organizations the verifier accepted witness the price before the signed start.
        /// </summary>
        [Test]
        public void Two_organizations_witness_the_price_before_the_start()
        {

            var example = Of();
            var result  = Verify(example);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Publication,                                Is.EqualTo("valid"));
                Assert.That(result.Applicability,                              Is.EqualTo("bound"));
                Assert.That(result.PublicationBeforeStart,                     Is.EqualTo("witnessed-before-signed-start"), String.Join("; ", result.PublicationWitnesses!.Receipts.Select(row => row.Detail)));
                Assert.That(result.PublicationWitnesses!.Organizations,        Has.Length.EqualTo(2));
                Assert.That(result.PublicationWitnesses.ActualStartTime,       Is.EqualTo("not-independently-proven"));
                Assert.That(result.CustomerConsent,                            Is.EqualTo("not-proven"));
                Assert.That(result.LatestOrSoleOffer,                          Is.EqualTo("not-proven"));
                Assert.That(result.PublicationWitnesses.Receipts.All(row => row.LatestPublicationAt < example.ChargingStarted),  Is.True);
            }

        }

        #endregion

        #region Receipts_and_recommendations_do_not_authorize_themselves()

        /// <summary>
        /// Without a policy of the verifier, genuinely signed receipts and the recommendations of the evidence count nothing.
        /// </summary>
        [Test]
        public void Receipts_and_recommendations_do_not_authorize_themselves()
        {

            var result = Verify(Of(), WithPolicy: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Publication,             Is.EqualTo("valid"));
                Assert.That(result.PublicationBeforeStart,  Is.EqualTo("not-proven"));
                Assert.That(result.PublicationWitnesses!.Receipts.All(row => row.Status == "untrusted"),  Is.True);
            }

        }

        #endregion

        #region No_two_organizations_before_the_start()

        /// <summary>
        /// A receipt too late, one overlapping the start by the clock error, two of one organization, an unknown witness or
        /// none establish no claim of two organizations before the start.
        /// </summary>
        [TestCase("late-receipt")]
        [TestCase("clock-overlap")]
        [TestCase("same-organization")]
        [TestCase("unknown-witness")]
        [TestCase("no-receipts")]
        public void No_two_organizations_before_the_start(String Variant)
        {

            var result = Verify(Of(Variant));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Signatures,                            Is.EqualTo("valid"));
                Assert.That(result.Publication,                           Is.EqualTo("valid"));
                Assert.That(result.PublicationBeforeStart,                Is.EqualTo("not-proven"));
                Assert.That(result.PublicationWitnesses!.Organizations,   Has.Length.LessThan(2));
            }

        }

        #endregion

        #region Wrong_receipts_are_refused_apart_from_the_valid_price()

        /// <summary>
        /// A receipt of another price, signed price, tariff or checkpoint, a tampered, a future one and one of a private source
        /// are refused, apart from the price of the CPO, which stays valid.
        /// </summary>
        [TestCase("wrong-price")]
        [TestCase("wrong-signed-price")]
        [TestCase("wrong-tariff")]
        [TestCase("wrong-checkpoint")]
        [TestCase("tampered-receipt")]
        [TestCase("future-receipt")]
        [TestCase("private-source")]
        public void Wrong_receipts_are_refused_apart_from_the_valid_price(String Variant)
        {

            var result = Verify(Of(Variant));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Signatures,              Is.EqualTo("valid"));
                Assert.That(result.Publication,             Is.EqualTo("valid"));
                Assert.That(result.PublicationBeforeStart,  Is.EqualTo("invalid"));
            }

        }

        #endregion

        #region Duplicates_and_aliases_add_no_organization()

        /// <summary>
        /// Receipts given twice and an alias of a witness key as another organization add no vote.
        /// </summary>
        [Test]
        public void Duplicates_and_aliases_add_no_organization()
        {

            var example   = Of("same-organization");
            var receipts  = (JArray) example.Evidence["publicationReceipts"]!;
            foreach (var receipt in receipts.ToArray())
                receipts.Add(receipt.DeepClone());

            var policy    = (JObject) example.Policy.JSON.DeepClone();
            var alias     = (JObject) policy["witnesses"]![0]!.DeepClone();
            alias["id"]            = "ALIAS";
            alias["organization"]  = "FAKE-SECOND-ORGANIZATION";
            ((JArray) policy["witnesses"]!).Add(alias);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Verify(example).PublicationWitnesses!.Organizations,  Has.Length.EqualTo(1));
                Assert.That(WitnessPublicationPolicy.TryParse(policy, out _, out var error),  Is.False);
                Assert.That(error,  Does.Contain("same witness key"));
            }

        }

        #endregion

        #region Local_authorization_is_not_relaxed()

        /// <summary>
        /// A policy of no organization, of a key for price statements, of another role or of a private endpoint is refused;
        /// one whose witness is of another network, valid later or of a larger clock error establishes no claim.
        /// </summary>
        [Test]
        public void Local_authorization_is_not_relaxed()
        {

            var example = Of();

            JObject Changed(Action<JObject> Change)
            {
                var json = (JObject) example.Policy.JSON.DeepClone();
                Change(json);
                return json;
            }

            using (Assert.EnterMultipleScope())
            {

                foreach (var change in new Action<JObject>[] {
                                           json => json["requiredOrganizations"] = 0,
                                           json => json["witnesses"]![0]!["keys"]![0]!["usages"] = new JArray("priceStatements"),
                                           json => json["witnesses"]![0]!["role"] = "roamingNetworkOperator",
                                           json => json["witnesses"]![0]!["endpoints"] = new JArray("https://127.0.0.1/receipts")
                                       })
                    Assert.That(WitnessPublicationPolicy.TryParse(Changed(change), out _, out _),  Is.False);

                foreach (var change in new Action<JObject>[] {
                                           json => json["witnesses"]![1]!["networks"] = new JArray("OTHER-NETWORK"),
                                           json => json["witnesses"]![1]!["notBefore"] = "2026-07-01T00:00:00.000Z",
                                           json => json["witnesses"]![1]!["clockErrorMs"] = 30000
                                       })
                    Assert.That(Verify(example, PolicyOf(Changed(change))).PublicationBeforeStart,  Is.Not.EqualTo("witnessed-before-signed-start"));

            }

        }

        #endregion

        #region A_CPO_key_relabeled_as_a_witness_is_excluded()

        /// <summary>
        /// A signing key of the CPO named as an independent witness is excluded, though its receipt is signed genuinely.
        /// </summary>
        [Test]
        public void A_CPO_key_relabeled_as_a_witness_is_excluded()
        {

            var example  = Of();
            var policy   = (JObject) example.Policy.JSON.DeepClone();
            var witness  = (JObject) policy["witnesses"]![0]!;
            var key      = (JObject) example.Evidence["chargingStationProof"]!["roamingNetwork"]!["chargingStationOperators"]![0]!["signingKeys"]![0]!.DeepClone();

            key["usages"]   = new JArray("witnessPublications");
            witness["keys"] = new JArray(key);
            witness["id"]   = "CPO-ALIAS";

            var receipt = (JObject) example.Evidence["publicationReceipts"]![0]!;
            receipt["witnessId"]  = "CPO-ALIAS";
            receipt["keyId"]      = key["@id"]!.Value<String>();

            Assert.That(PricePublicationReceipt.TryParse(receipt, out var parsed, out var error),  Is.True, error);
            receipt["value"] = TSVectors.Sign(161, parsed!.GetSigningBytes());

            var result = Verify(example, PolicyOf(policy));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.PublicationBeforeStart,                       Is.EqualTo("invalid"));
                Assert.That(result.PublicationWitnesses!.Receipts[0].Detail,     Does.Contain("RNO/CPO"));
            }

        }

        #endregion

        #region Receipts_replace_neither_inclusion_nor_signatures()

        /// <summary>
        /// Receipts replace neither the inclusion of the price in a checkpoint nor the signature of the CPO.
        /// </summary>
        [Test]
        public void Receipts_replace_neither_inclusion_nor_signatures()
        {

            var withoutInclusion = Of();
            withoutInclusion.Evidence.Remove("priceStatementProofs");
            withoutInclusion.Evidence.Remove("checkpoints");

            var badSignature = Of();
            badSignature.Evidence["priceStatements"]![0]!["value"] = "AAAA";

            var result = Verify(withoutInclusion);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Publication,                                  Is.EqualTo("missing"));
                Assert.That(result.PublicationBeforeStart,                       Is.Not.EqualTo("witnessed-before-signed-start"));
                Assert.That(Verify(badSignature).PublicationBeforeStart,         Is.EqualTo("not-proven"));
            }

        }

        #endregion

        #region A_witness_makes_the_receipt_WWCP_POI_TS_verifies()

        /// <summary>
        /// A receipt the witness A makes here with its key of the seed 181 is the one of the vector, Ed25519 signing
        /// deterministically, and the evidence reads back as written.
        /// </summary>
        [Test]
        public void A_witness_makes_the_receipt_WWCP_POI_TS_verifies()
        {

            var example     = Of();
            var given       = (JObject) example.Evidence["publicationReceipts"]![0]!;
            var statement   = (JObject) example.Evidence["priceStatements"]![0]!;
            var tariff      = (JObject) example.Evidence["chargingStationProof"]!["roamingNetwork"]!["chargingStationOperators"]![0]!["chargingTariffs"]![0]!;
            var privateKey  = new Ed25519PrivateKeyParameters(Enumerable.Repeat((Byte) 181, 32).ToArray());

            var made        = PricePublicationReceipt.Create(privateKey,
                                                             POIPublicKey.From(privateKey).Id,
                                                             COSEAlgorithm.Ed25519,
                                                             RoamingNetwork_Id.Parse(given["roamingNetworkId"]!.Value<String>()!),
                                                             given["EVSEId"]!.Value<String>()!,
                                                             statement,
                                                             tariff,
                                                             RoamingNetworkCheckpoint_Id.Parse(given["checkpointId"]!.Value<String>()!),
                                                             given["witnessId"]!.Value<String>()!,
                                                             given["publicSourceURL"]!.Value<String>()!,
                                                             DateTimeOffset.Parse(given["observedAt"]!.Value<String>()!),
                                                             DateTimeOffset.Parse(given["signedAt"]!.Value<String>()!));

            Assert.That(SessionPriceEvidence.TryParse(example.Evidence, out var evidence, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(made.JSON.ToString(),                                     Is.EqualTo(given.ToString()));
                Assert.That(JToken.DeepEquals(evidence!.ToJSON(), example.Evidence),  Is.True);
                Assert.That(evidence.WitnessCandidates,                               Has.Length.EqualTo(2));
            }

        }

        #endregion

    }

}
