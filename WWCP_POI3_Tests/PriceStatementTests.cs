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

using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Proofs
{

    /// <summary>
    /// Price statements: the price status of a charging pool, a charging station or an EVSE, signed by its charging station
    /// operator, chained per entity, checked by a driver's app with the proof of the station, and named by checkpoints
    /// (SPECIFICATION.md, section 12).
    /// </summary>
    [TestFixture]
    public class PriceStatementTests
    {

        #region Data

        private const           String                      EVSEType   = nameof(EVSE);
        private const           String                      PoolType   = nameof(ChargingPool);

        private static readonly RoamingNetwork_Id           RN         = RoamingNetwork_Id.         Parse("rn1");
        private static readonly ChargingStationOperator_Id  GEF        = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  XYZ        = ChargingStationOperator_Id.Parse("DE*XYZ");
        private static readonly ChargingStation_Id          S1         = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly String                      P1         = "DE*GEF*P1";
        private static readonly String                      E11        = "DE*GEF*E1*1";
        private static readonly String                      E12        = "DE*GEF*E1*2";
        private static readonly String                      E21        = "DE*GEF*E2*1";
        private static readonly ChargingTariff_Id           T1         = ChargingTariff_Id.         Parse("DE*GEF*T1");

        /// <summary>
        /// Since when the price statuses of a test are set, one second after another: in the past, so that they are current.
        /// </summary>
        private DateTimeOffset since;

        #endregion

        #region SetUp

        [SetUp]
        public void SetUp()

            => since = DateTimeOffset.FromUnixTimeSeconds(Timestamp.Now.AddHours(-1).ToUnixTimeSeconds());

        #endregion


        #region (static) Signer(Operator)

        /// <summary>
        /// The signing key of the given charging station operator, signing its price statements.
        /// </summary>
        internal static RoamingNetworkCommitSigner Signer(ChargingStationOperator_Id Operator)
        {

            var (privateKey, publicKey) = TestParties.Key(TestParties.Party(Operator));

            return new RoamingNetworkCommitSigner(privateKey,
                                                  publicKey.Id,
                                                  COSEAlgorithm.ES256,
                                                  RoamingNetworkCommitSignatureRoles.ChargingStationOperator,
                                                  Operator.ToString());

        }

        #endregion

        #region (static) Prices(Price, TariffId = null)

        /// <summary>
        /// A price status of one new energy price of a charging tariff, T1 by default.
        /// </summary>
        private static ChargingPrices Prices(Decimal             Price,
                                             ChargingTariff_Id?  TariffId   = null)

            => new ([ new ChargingTariffPrices(TariffId ?? T1,
                                               [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]) ]) ]);

        #endregion

        #region (static) NewHistory(Signing = true, Store = null)

        /// <summary>
        /// A history of the network of the station proofs, signing the price statements of DE*GEF, unless not signing.
        /// </summary>
        private static RoamingNetworkHistory NewHistory(Boolean                       Signing   = true,
                                                        IRoamingNetworkHistoryStore?  Store     = null)
        {

            var history = RoamingNetworkHistory.Start(StationProofTests.NewNetwork(), Store: Store);

            if (Signing)
                history.PriceStatementSigners = [ Signer(GEF) ];

            return history;

        }

        #endregion

        #region (static) Attested(Now)

        /// <summary>
        /// A history signing the price statements of DE*GEF, whose operator states its signing key, attesting its head now.
        /// </summary>
        private static RoamingNetworkHistory Attested(DateTimeOffset Now, out String IdentityKeyId)
        {

            var history   = NewHistory();
            var network   = history.Head.RoamingNetwork;
            var signing   = TestParties.Key(TestParties.Party(network), POIKeyKind.Signing);
            var identity  = TestParties.Key(TestParties.Party(network), POIKeyKind.Identity);

            var keyStatement = RoamingNetworkKeyStatement.Create(identity.Private,
                                                                 COSEAlgorithm.ES512,
                                                                 1,
                                                                 network.Id,
                                                                 network.Operator!.Id,
                                                                 [ identity.Public ],
                                                                 Now.AddMinutes(-1),
                                                                 Now.AddDays(30));

            var certificate  = RoamingNetworkSigningKeyCertificate.Create(identity.Private,
                                                                          COSEAlgorithm.ES512,
                                                                          network.Operator!.Id,
                                                                          signing.Public,
                                                                          Now.AddMinutes(-1));

            var result = history.Commit(network.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkKeyStatement.JSONProperty, null, keyStatement.ToJSON()),
                                                                        RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkSigningKeyCertificate.JSONProperty, null, new JArray(certificate.ToJSON(Embedded: true))) ],
                                                                      CreatedAt: Now.AddMinutes(-1)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            history.AttestationSigners = [ new RoamingNetworkCommitSigner(signing.Private,
                                                                          signing.Public.Id,
                                                                          COSEAlgorithm.ES256,
                                                                          RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator) ];

            Assert.That(history.TryAttest(out _, out var attestError, Now), Is.True, attestError);

            IdentityKeyId = identity.Public.Id;

            return history;

        }

        #endregion

        #region SetPrice(History, EntityType, EntityId, Price, Second, At = null)

        /// <summary>
        /// Set the price status of the given entity, the given seconds after the start of the test or at the given time, and
        /// return the price statement it is stated by.
        /// </summary>
        private RoamingNetworkPriceStatement SetPrice(RoamingNetworkHistory  History,
                                                      String                 EntityType,
                                                      String                 EntityId,
                                                      Decimal                Price,
                                                      Int32                  Second,
                                                      DateTimeOffset?        At   = null)
        {

            var result = History.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, EntityType, EntityId, Prices(Price).Text, At ?? since.AddSeconds(Second), out _, out var error);

            Assert.That(result,                                                                   Is.EqualTo(POIStatusUpdateResult.Updated), error);
            Assert.That(History.TryGetPriceStatement(EntityType, EntityId, out var priceStatement),  Is.True, "The price status was not stated.");

            return priceStatement!;

        }

        #endregion

        #region (static) Commit(History, Value)

        private static void Commit(RoamingNetworkHistory  History,
                                   String                 Value)
        {

            var result = History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(
                                            [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                                            CreatedAt: Timestamp.Now));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

        }

        #endregion

        #region (static) Read(Text)

        /// <summary>
        /// JSON as an app reads it: texts stay texts, numbers with a fraction decimals.
        /// </summary>
        private static JObject Read(String Text)

            => (JObject) JToken.ReadFrom(new JsonTextReader(new StringReader(Text)) {
                                             DateParseHandling   = DateParseHandling.None,
                                             FloatParseHandling  = FloatParseHandling.Decimal
                                         });

        #endregion

        #region (static) Digests(PriceStatements)

        private static String[] Digests(IEnumerable<RoamingNetworkPriceStatement>? PriceStatements)

            => [ .. (PriceStatements ?? []).Select(priceStatement => priceStatement.Digest) ];

        #endregion

        #region (static) TwinOf(PriceStatement)

        /// <summary>
        /// The order n of the curve P-256.
        /// </summary>
        private static readonly BigInteger P256Order = BigInteger.Parse("0FFFFFFFF00000000FFFFFFFFFFFFFFFFBCE6FAADA7179E84F3B9CAC2FC632551", NumberStyles.HexNumber);

        /// <summary>
        /// The ECDSA twin of the given ES256 price statement: its signature (r, s) as (r, n − s), valid as well, which
        /// anybody makes without the key.
        /// </summary>
        private static RoamingNetworkPriceStatement TwinOf(RoamingNetworkPriceStatement PriceStatement)
        {

            var value      = Convert.FromBase64String(PriceStatement.Value);
            var s          = new BigInteger(value.AsSpan(32, 32), isUnsigned: true, isBigEndian: true);
            var twinS      = (P256Order - s).ToByteArray(isUnsigned: true, isBigEndian: true);

            var json       = PriceStatement.ToJSON();
            json["value"]  = Convert.ToBase64String([ .. value.AsSpan(0, 32), .. new Byte[32 - twinS.Length], .. twinS ]);

            Assert.That(RoamingNetworkPriceStatement.TryParse(json, out var twin, out var error), Is.True, error);

            return twin!;

        }

        #endregion


        #region A_price_status_set_is_stated_in_a_chain_of_its_entity()

        [Test]
        public void A_price_status_set_is_stated_in_a_chain_of_its_entity()
        {

            var history   = NewHistory();
            var first     = SetPrice(history, EVSEType, E11, 0.29m, 1);
            var second    = SetPrice(history, EVSEType, E11, 0.31m, 2);

            // The same price status again is no new statement.
            var unchanged = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, EVSEType, E11, Prices(0.31m).Text, since.AddSeconds(3), out _, out _);

            history.TryGetPriceStatement(EVSEType, E11, out var current);

            Assert.That(history.TryStatePrices(EVSEType, E11, out var third, out var stateError), Is.True, stateError);

            var pool      = SetPrice(history, PoolType, P1, 0.25m, 4);

            // Without a signer of the operator, nothing is stated.
            var unsigned  = NewHistory(Signing: false);

            unsigned.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, EVSEType, E11, Prices(0.29m).Text, since.AddSeconds(1), out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(first.Sequence,                  Is.Zero);
                Assert.That(first.Previous,                  Is.Null);
                Assert.That(first.Signer,                    Is.EqualTo(GEF));
                Assert.That(first.KeyId,                     Is.EqualTo(TestParties.Key(TestParties.Party(GEF)).Public.Id));
                Assert.That(first.After,                     Is.Null, "There is no checkpoint yet.");
                Assert.That(first.ValidUntil,                Is.Null, "Valid until the next statement of the EVSE.");
                Assert.That(first.PriceStatus,               Is.EqualTo(Prices(0.29m)));

                Assert.That(second.Sequence,                 Is.EqualTo(1));
                Assert.That(second.Previous,                 Is.EqualTo(first.Digest));
                Assert.That(second.Follows(first),           Is.True);

                Assert.That(current!.Digest,                 Is.EqualTo(second.Digest), "The same price status again is no new statement.");
                Assert.That(third!.Follows(second),          Is.True);
                Assert.That(third.PriceStatus,               Is.EqualTo(Prices(0.31m)));

                Assert.That(pool.Sequence,                   Is.Zero, "Every entity has a chain of its own.");

                Assert.That(Digests(history.PendingPriceStatements),         Is.EqualTo(Digests([ first, second, third, pool ])));
                Assert.That(Digests(history.PriceStatementsOf(S1)),          Is.EqualTo(Digests([ pool, third ])), "Of the pool, the station and its EVSEs.");
                Assert.That(Digests(history.PriceStatementsOf(EVSE_Id.Parse(E12))),  Is.EqualTo(Digests([ pool ])));

                Assert.That(unchanged,                                       Is.Not.EqualTo(POIStatusUpdateResult.Updated));
                Assert.That(unsigned.TryGetPriceStatement(EVSEType, E11, out _),  Is.False);
                Assert.That(unsigned.TryStatePrices(EVSEType, E11, out _, out var noSigner),  Is.False);
                Assert.That(noSigner,                                        Does.Contain("no price statement signer"));

            }

        }

        #endregion

        #region A_price_statement_states_the_prices_as_they_were_set(Price)

        /// <summary>
        /// The price status is read as JSON text: its prices were read as doubles, and the statement signed 0.39 for
        /// 0.390 and 0.123456789012346 for 0.1234567890123456789.
        /// </summary>
        [TestCase("0.390")]
        [TestCase("0.1234567890123456789")]
        public void A_price_statement_states_the_prices_as_they_were_set(String Price)
        {

            var history    = NewHistory();
            var statement  = SetPrice(history, EVSEType, E11, Decimal.Parse(Price, CultureInfo.InvariantCulture), 1);

            Assert.That(CanonicalJSON.Serialize(statement.PriceStatus.ToJSON()), Does.Contain("\"price\":" + Price + ","));

        }

        #endregion

        #region A_price_statement_reads_back_and_its_digest_leaves_its_signature_out()

        [Test]
        public void A_price_statement_reads_back_and_its_digest_leaves_its_signature_out()
        {

            var statement = Signer(GEF).StatePrices(RN, EVSEType, E11, Prices(0.29m), 0, null, since, since.AddSeconds(1), since.AddMinutes(15));
            var text      = statement.ToJSON().ToString();
            var gefKey    = TestParties.Key(TestParties.Party(GEF)).Public;

            Assert.That(RoamingNetworkPriceStatement.TryParse(text, out var parsed, out var parseError),  Is.True, parseError);

            var json      = Read(text);
            json["value"] = Convert.ToBase64String(new Byte[64]);

            var cbor      = statement.ToCBOR();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(parsed!.Digest,                         Is.EqualTo(statement.Digest));
                Assert.That(parsed.ToJSON().ToString(),             Is.EqualTo(text));
                Assert.That(parsed.ValidFrom,                       Is.EqualTo(since.AddSeconds(1)));
                Assert.That(parsed.ValidUntil,                      Is.EqualTo(since.AddMinutes(15)));
                Assert.That(parsed.IsValidAt(since),                Is.False, "Not before it is valid from.");
                Assert.That(parsed.IsValidAt(since.AddMinutes(1)),  Is.True);
                Assert.That(parsed.IsValidAt(since.AddMinutes(16)), Is.False);
                Assert.That(text,                                   Does.Contain($"\"@context\": \"{RoamingNetworkPriceStatement.JSONLDContext}\""));
                Assert.That(text,                                   Does.Not.Contain("\"profile\"").And.Not.Contain("\"encoding\""));

                Assert.That(RoamingNetworkPriceStatement.TryParse(json, out var otherValue, out _),  Is.True);
                Assert.That(otherValue!.Digest,                     Is.EqualTo(statement.Digest), "The digest leaves the signature value out.");
                Assert.That(statement.TryVerify([ gefKey ], out var verifyError),   Is.True, verifyError);
                Assert.That(otherValue.TryVerify([ gefKey ], out _),                Is.False, "Its signature is still checked.");

                Assert.That(RoamingNetworkPriceStatement.TryParse(cbor, out var fromCBOR, out var cborError),  Is.True, cborError);
                Assert.That(fromCBOR!.Digest,                       Is.EqualTo(statement.Digest));

                // The operator of the entity signs, in its role; a later statement names the one before.
                Assert.That(RoamingNetworkPriceStatement.TryParse(text.Replace("\"DE*GEF\"", "\"DE*XYZ\""),                  out _, out var signerError),    Is.False);
                Assert.That(signerError,                            Does.Contain("signed by its operator 'DE*GEF'"));
                Assert.That(RoamingNetworkPriceStatement.TryParse(text.Replace("\"sequence\": 0", "\"sequence\": 1"),          out _, out var previousError),  Is.False);
                Assert.That(previousError,                          Does.Contain("names the digest of the one before"));
                Assert.That(RoamingNetworkPriceStatement.TryParse(text.Replace(RoamingNetworkPriceStatement.Role, "witness"),   out _, out _),                  Is.False);
                Assert.That(RoamingNetworkPriceStatement.TryParse(text.Replace("\"entityType\": \"EVSE\"", "\"entityType\": \"ChargingStationOperator\""), out _, out _), Is.False);

            }

        }

        #endregion

        #region An_app_checks_a_price_statement_with_the_proof_of_its_station()

        [Test]
        public void An_app_checks_a_price_statement_with_the_proof_of_its_station()
        {

            var now        = Timestamp.Now;
            var history    = Attested(now, out var identityKeyId);
            var statement  = SetPrice(history, EVSEType, E11, 0.29m, 1);
            var signer     = Signer(GEF);

            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var proofError), Is.True, proofError);

            // What the app got, as text.
            Assert.That(RoamingNetworkProof.         TryParse(proof!.ToJSON().ToString(),   out var appProof,      out var readProofError),      Is.True, readProofError);
            Assert.That(RoamingNetworkPriceStatement.TryParse(statement.ToJSON().ToString(), out var appStatement,  out var readStatementError),  Is.True, readStatementError);

            var tamperedJSON              = statement.ToJSON();
            tamperedJSON["priceStatus"]   = Prices(0.19m).ToJSON();

            RoamingNetworkPriceStatement.TryParse(tamperedJSON, out var tampered, out _);

            var xyz                       = TestParties.Key(TestParties.Party(XYZ));
            var byAnotherOperator         = RoamingNetworkPriceStatement.Create(xyz.Private, xyz.Public.Id, COSEAlgorithm.ES256, RN, EVSEType, E11, Prices(0.29m), 1, statement.Digest);
            var ofAnotherStation          = signer.StatePrices(RN, EVSEType, E21, Prices(0.29m), 0, null);
            var expired                   = signer.StatePrices(RN, EVSEType, E12, Prices(0.29m), 0, null, now.AddMinutes(-10), now.AddMinutes(-10), now.AddMinutes(-5));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(appStatement!.TryVerify(appProof!, [ identityKeyId ], out var attestation, out var verifyError),  Is.True, verifyError);
                Assert.That(attestation!.CommitId,                                                                           Is.EqualTo(history.Head.Commit.Id));

                Assert.That(tampered!.TryVerify(appProof!, [ identityKeyId ], out _, out var tamperedError),                  Is.False);
                Assert.That(tamperedError,                                                                                   Does.Contain("signature of the price statement is invalid"));

                Assert.That(byAnotherOperator.TryVerify(appProof!, [ identityKeyId ], out _, out var keyError),                Is.False);
                Assert.That(keyError,                                                                                        Does.Contain("is no signing key of the charging station operator 'DE*GEF'"));

                Assert.That(ofAnotherStation.TryVerify(appProof!, [ identityKeyId ], out _, out var entityError),              Is.False);
                Assert.That(entityError,                                                                                     Does.Contain($"gives no EVSE '{E21}'"));

                Assert.That(expired.TryVerify(appProof!, [ identityKeyId ], out _, out var expiredError),                      Is.False);
                Assert.That(expiredError,                                                                                    Does.Contain("is not valid at"));

                Assert.That(appStatement.TryVerify(appProof!, [ "ab".PadRight(64, 'c') ], out _, out var untrustedError),      Is.False, "Not with an identity key the app does not trust.");
                Assert.That(untrustedError,                                                                                  Is.Not.Null);

            }

        }

        #endregion

        #region Price_statements_of_other_servers_are_checked_and_follow_the_one_before()

        [Test]
        public void Price_statements_of_other_servers_are_checked_and_follow_the_one_before()
        {

            var history       = NewHistory(Signing: false);
            var signer        = Signer(GEF);
            var t             = Timestamp.Now.AddMinutes(-10);

            var s0            = signer.StatePrices(RN, EVSEType, E11, Prices(0.29m), 0, null,      t);
            var s1            = signer.StatePrices(RN, EVSEType, E11, Prices(0.31m), 1, s0.Digest, t.AddMinutes(1));
            var s2            = signer.StatePrices(RN, EVSEType, E11, Prices(0.33m), 2, s1.Digest, t.AddMinutes(2));
            var splitView     = signer.StatePrices(RN, EVSEType, E11, Prices(0.99m), 2, s1.Digest, t.AddMinutes(2));
            var notFollowing  = signer.StatePrices(RN, EVSEType, E11, Prices(0.35m), 3, s0.Digest, t.AddMinutes(3));

            var xyz           = TestParties.Key(TestParties.Party(XYZ));

            var refused       = new[] {
                                    ("unknown EVSE",        signer.StatePrices(RN, EVSEType, "DE*GEF*E9*9", Prices(0.29m), 0, null, t)),
                                    ("unknown tariff",      signer.StatePrices(RN, EVSEType, E12, Prices(0.29m, ChargingTariff_Id.Parse("DE*GEF*T9")), 0, null, t)),
                                    ("key of DE*XYZ",       RoamingNetworkPriceStatement.Create(xyz.Private, xyz.Public.Id, COSEAlgorithm.ES256, RN, EVSEType, E12, Prices(0.29m), 0, null, t)),
                                    ("no longer valid",     signer.StatePrices(RN, EVSEType, E12, Prices(0.29m), 0, null, t, t, t.AddMinutes(1))),
                                    ("another network",     signer.StatePrices(RoamingNetwork_Id.Parse("rn2"), EVSEType, E12, Prices(0.29m), 0, null, t))
                                };

            var taken         = new List<RoamingNetworkPriceStatement>();
            history.OnPriceStatement += taken.Add;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(history.TryAddPriceStatement(s0, out var e0),            Is.EqualTo(POIPriceStatementResult.Taken),     e0);
                Assert.That(history.TryAddPriceStatement(s0, out _),                 Is.EqualTo(POIPriceStatementResult.Known));
                Assert.That(history.TryAddPriceStatement(s2, out var e2),            Is.EqualTo(POIPriceStatementResult.Taken),     "One in between missed: " + e2);
                Assert.That(history.TryAddPriceStatement(s1, out var e1),            Is.EqualTo(POIPriceStatementResult.Outdated));
                Assert.That(e1,                                                       Does.Contain("has the price statement 2 already"));

                Assert.That(history.TryAddPriceStatement(splitView, out var eSplit),     Is.EqualTo(POIPriceStatementResult.Refused));
                Assert.That(eSplit,                                                       Does.Contain("split view"));
                Assert.That(history.TryAddPriceStatement(notFollowing, out var eFollow), Is.EqualTo(POIPriceStatementResult.Refused));
                Assert.That(eFollow,                                                      Does.Contain("does not name the digest of the statement 2 before"));

                foreach (var (why, priceStatement) in refused)
                    Assert.That(history.TryAddPriceStatement(priceStatement, out _),  Is.EqualTo(POIPriceStatementResult.Refused), why);

                Assert.That(Digests(taken),                                           Is.EqualTo(Digests([ s0, s2 ])));
                Assert.That(Digests(history.PriceStatements),                         Is.EqualTo(Digests([ s2 ])));

                // Taken, it sets the price status of the entity.
                Assert.That(history.Head.RoamingNetwork.TryGetChargingStationOperatorById(GEF, out var chargingStationOperator) &&
                            chargingStationOperator.TryGetEVSE(EVSE_Id.Parse(E11), out var evse) &&
                            evse.PriceStatus?.Value.Equals(Prices(0.33m)) == true,
                            Is.True);

            }

        }

        #endregion

        #region The_ECDSA_twin_of_a_price_statement_is_the_same_statement()

        [Test]
        public void The_ECDSA_twin_of_a_price_statement_is_the_same_statement()
        {

            var history  = NewHistory(Signing: false);
            var signer   = Signer(GEF);
            var t        = Timestamp.Now.AddMinutes(-10);

            var s0       = signer.StatePrices(RN, EVSEType, E11, Prices(0.29m), 0, null,      t);
            var s1       = signer.StatePrices(RN, EVSEType, E11, Prices(0.31m), 1, s0.Digest, t.AddMinutes(1));
            var twin     = TwinOf(s0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(twin.Value,   Is.Not.EqualTo(s0.Value));
                Assert.That(twin.Digest,  Is.EqualTo(s0.Digest), "The digest leaves the signature value out.");

                // Whoever relays the twin first makes the server take it: the statement of the operator is the same one,
                // and the next one of the operator follows it.
                Assert.That(history.TryAddPriceStatement(twin, out var eTwin),  Is.EqualTo(POIPriceStatementResult.Taken), eTwin);
                Assert.That(history.TryAddPriceStatement(s0,   out var e0),     Is.EqualTo(POIPriceStatementResult.Known), e0);
                Assert.That(history.TryAddPriceStatement(s1,   out var e1),     Is.EqualTo(POIPriceStatementResult.Taken), e1);

            }

        }

        #endregion

        #region A_checkpoint_names_the_price_statements_taken_since_the_one_before()

        [Test]
        public void A_checkpoint_names_the_price_statements_taken_since_the_one_before()
        {

            var history  = NewHistory();
            var s0       = SetPrice(history, EVSEType, E11, 0.29m, 1);
            var s1       = SetPrice(history, EVSEType, E12, 0.31m, 2);
            var s2       = SetPrice(history, EVSEType, E11, 0.33m, 3);

            Assert.That(history.TryMakeCheckpoint(out var cp0, out var cp0Error),  Is.True, cp0Error);

            var named    = history.PriceStatementsOf(cp0!.Id);
            var pending  = history.PendingPriceStatements;

            Assert.That(history.TryGetPriceStatementProof(cp0.Id, s1.Digest, out var proof, out var proofError),  Is.True, proofError);
            Assert.That(RoamingNetworkPriceStatementProof.TryParse(Read(proof!.ToJSON().ToString()), out var readProof, out var readError),  Is.True, readError);

            // Made after the checkpoint, the next statement names it, and the next checkpoint names only it.
            var s3       = SetPrice(history, EVSEType, E11, 0.35m, 4);

            Commit(history, "v1");
            Assert.That(history.TryMakeCheckpoint(out var cp1, out var cp1Error),  Is.True, cp1Error);

            Commit(history, "v2");
            Assert.That(history.TryMakeCheckpoint(out var cp2, out var cp2Error),  Is.True, cp2Error);

            Assert.That(RoamingNetworkCheckpoint.TryParse(cp0.ToJSON().ToString(), out var readCheckpoint, out var readCheckpointError),  Is.True, readCheckpointError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(cp0.PriceStatementsSize,                     Is.EqualTo(3));
                Assert.That(Digests(named),                              Is.EqualTo(Digests([ s0, s2, s1 ])), "By their entities, then by their sequences.");
                Assert.That(cp0.PriceStatementsRoot,                     Is.EqualTo(RoamingNetworkPriceStatement.RootOf(named!)));
                Assert.That(cp0.ToJSON()["priceStatements"]?["@size"]?.Value<Int32>(),  Is.EqualTo(3));
                Assert.That(readCheckpoint!.Id,                          Is.EqualTo(cp0.Id), "The price statements are part of the identification.");
                Assert.That(pending,                                     Is.Empty);

                Assert.That(readProof!.Verify(cp0, out var verifyError), Is.True, verifyError);
                Assert.That(readProof.Index,                             Is.EqualTo(2));
                Assert.That(readProof.Verify(cp1!, out _),               Is.False, "Not of another checkpoint.");
                Assert.That(history.TryGetPriceStatementProof(cp0.Id, s3.Digest, out _, out var notNamed),  Is.False);
                Assert.That(notNamed,                                    Does.Contain("names no price statement"));

                Assert.That(s3.After,                                    Is.EqualTo(cp0.Id));
                Assert.That(Digests(history.PriceStatementsOf(cp1!.Id)), Is.EqualTo(Digests([ s3 ])));

                Assert.That(cp2!.PriceStatementsSize,                    Is.Zero);
                Assert.That(cp2.ToJSON().ContainsKey("priceStatements"), Is.False, "A checkpoint naming none is written as before.");
                Assert.That(history.PriceStatementsOf(cp2.Id),           Is.Null);

            }

        }

        #endregion

        #region A_witness_signs_a_checkpoint_naming_price_statements_once_it_checked_them()

        [Test]
        public void A_witness_signs_a_checkpoint_naming_price_statements_once_it_checked_them()
        {

            var maker    = NewHistory();
            SetPrice(maker, EVSEType, E11, 0.29m, 1);
            SetPrice(maker, EVSEType, E12, 0.31m, 2);
            var latest   = SetPrice(maker, EVSEType, E11, 0.33m, 3);

            Assert.That(maker.TryMakeCheckpoint(out var checkpoint, out var makeError),  Is.True, makeError);

            var named    = maker.PriceStatementsOf(checkpoint!.Id)!;

            var witness  = NewHistory(Signing: false);
            Assert.That(witness.TryAddCheckpoint(checkpoint, out var addError),  Is.True, addError);

            var signedUnchecked  = witness.TryWitness(checkpoint.Id, History.CheckpointTests.Witness(), out _, out _, out var uncheckedError);
            var oneMissing       = witness.TryAddCheckpointPriceStatements(checkpoint.Id, named.Skip(1), out var missingError);
            var checkedThem      = witness.TryAddCheckpointPriceStatements(checkpoint.Id, named.Reverse(), out var checkError);
            var signedChecked    = witness.TryWitness(checkpoint.Id, History.CheckpointTests.Witness(), out _, out _, out var witnessError);

            // Given along with the checkpoint, another server checks them at once.
            var third    = NewHistory(Signing: false);
            var took     = third.TryAddCheckpoint(checkpoint, out var thirdError, PriceStatements: named);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(signedUnchecked,                                   Is.False);
                Assert.That(uncheckedError,                                    Does.Contain("are not checked here"));
                Assert.That(oneMissing,                                        Is.False);
                Assert.That(missingError,                                      Does.Contain("are not those the checkpoint"));
                Assert.That(checkedThem,                                       Is.True, checkError);
                Assert.That(signedChecked,                                     Is.True, witnessError);
                Assert.That(Digests(witness.PriceStatementsOf(checkpoint.Id)), Is.EqualTo(Digests(named)));

                Assert.That(took,                                              Is.True, thirdError);
                Assert.That(third.TryGetPriceStatement(EVSEType, E11, out var current) && current.Digest == latest.Digest,  Is.True,
                            "The latest statement of an entity a checkpoint names is its current one.");

            }

        }

        #endregion

        #region Price_statements_and_those_of_checkpoints_survive_a_restart_and_a_compaction()

        [Test]
        public void Price_statements_and_those_of_checkpoints_survive_a_restart_and_a_compaction()
        {

            var store    = new RoamingNetworkHistoryMemoryStore();
            var history  = NewHistory(Store: store);

            SetPrice(history, EVSEType, E11, 0.29m, 1);
            SetPrice(history, EVSEType, E12, 0.31m, 2);

            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var makeError),  Is.True, makeError);

            var s2       = SetPrice(history, EVSEType, E11, 0.33m, 3);

            Assert.That(RoamingNetworkHistory.TryLoad(store, out var loaded, out var loadError),  Is.True, loadError);

            var current  = Digests(loaded!.PriceStatements);
            var named    = Digests(loaded.PriceStatementsOf(checkpoint!.Id));
            var pending  = Digests(loaded.PendingPriceStatements);

            // The chain goes on after the restart.
            loaded.PriceStatementSigners = [ Signer(GEF) ];
            var s3       = SetPrice(loaded, EVSEType, E11, 0.35m, 4);

            Assert.That(loaded.Compact(out var compactError),                                    Is.True, compactError);
            Assert.That(RoamingNetworkHistory.TryLoad(store, out var again, out var againError), Is.True, againError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(current,                                          Is.EqualTo(Digests(history.PriceStatements)));
                Assert.That(named,                                            Is.EqualTo(Digests(history.PriceStatementsOf(checkpoint.Id))));
                Assert.That(named,                                            Has.Length.EqualTo(2));
                Assert.That(pending,                                          Is.EqualTo(Digests([ s2 ])));

                Assert.That(s3.Follows(s2),                                   Is.True);
                Assert.That(s3.After,                                         Is.EqualTo(checkpoint.Id));

                Assert.That(Digests(again!.PriceStatements),                  Is.EqualTo(Digests(loaded.PriceStatements)));
                Assert.That(Digests(again.PriceStatementsOf(checkpoint.Id)),  Is.EqualTo(named));
                Assert.That(Digests(again.PendingPriceStatements),            Is.EqualTo(Digests([ s2, s3 ])));

            }

        }

        #endregion

        #region Price_statements_made_with_a_validity_are_stated_anew_before_they_end()

        [Test]
        public void Price_statements_made_with_a_validity_are_stated_anew_before_they_end()
        {

            var history                      = NewHistory();
            history.PriceStatementValidity   = TimeSpan.FromMinutes(15);

            var first    = SetPrice(history, EVSEType, E11, 0.29m, 1);
            var early    = history.RefreshPriceStatements(first.SignedAt.AddMinutes(5));
            var due      = history.RefreshPriceStatements(first.SignedAt.AddMinutes(11));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(first.ValidUntil,           Is.EqualTo(first.SignedAt.AddMinutes(15)));
                Assert.That(early,                      Is.Empty);
                Assert.That(due,                        Has.Count.EqualTo(1));
                Assert.That(due[0].Follows(first),      Is.True);
                Assert.That(due[0].PriceStatus,         Is.EqualTo(first.PriceStatus));
                Assert.That(due[0].ValidUntil,          Is.EqualTo(due[0].SignedAt.AddMinutes(15)));
            }

        }

        #endregion


        #region An_app_gets_the_proof_and_the_price_statements_of_a_station_over_HTTP()

        [Test]
        public async Task An_app_gets_the_proof_and_the_price_statements_of_a_station_over_HTTP()
        {

            var now                    = Timestamp.Now;
            var history                = Attested(now, out var identityKeyId);

            var pool                   = SetPrice(history, PoolType, P1,  0.25m, 1);
            var evse                   = SetPrice(history, EVSEType, E11, 0.29m, 2);

            await using var httpServer = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI     = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

            using var httpClient       = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/") };

            var proofText              = await httpClient.GetStringAsync("CSs/DE*GEF*S1/proof");
            var statementsText         = await httpClient.GetStringAsync("CSs/DE*GEF*S1/priceStatements");
            var ofEVSE                 = Read(await httpClient.GetStringAsync("EVSEs/DE*GEF*E1*2/priceStatements"));
            var ofFullPath             = Read(await httpClient.GetStringAsync("CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/priceStatements"));

            Assert.That(RoamingNetworkProof.TryParse(proofText, out var proof, out var proofError),  Is.True, proofError);

            var statements             = Read(statementsText)["priceStatements"]!.OfType<JObject>().
                                             Select(json => RoamingNetworkPriceStatement.TryParse(json, out var statement, out _) ? statement : null).
                                             ToArray();

            // A server of the operator gives its statements: one taken, one refused.
            var e12                    = Signer(GEF).StatePrices(RN, EVSEType, E12, Prices(0.31m), 0, null);
            var xyz                    = TestParties.Key(TestParties.Party(XYZ));
            var forged                 = RoamingNetworkPriceStatement.Create(xyz.Private, xyz.Public.Id, COSEAlgorithm.ES256, RN, EVSEType, E12, Prices(0.01m), 1, e12.Digest);

            var posted                 = await httpClient.PostAsync("history/priceStatements", Body(e12));
            var postedForged           = await httpClient.PostAsync("history/priceStatements", Body(forged));

            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var checkpointError),  Is.True, checkpointError);

            var ofCheckpoint           = Read(await httpClient.GetStringAsync($"history/checkpoints/{checkpoint!.Id}/priceStatements"));
            var statementProofText     = await httpClient.GetStringAsync($"history/checkpoints/{checkpoint.Id}/priceStatements/{evse.Digest}/proof");
            var current                = Read(await httpClient.GetStringAsync("history/priceStatements"));

            Assert.That(RoamingNetworkPriceStatementProof.TryParse(Read(statementProofText), out var statementProof, out var statementProofError),  Is.True, statementProofError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Digests(statements!),                         Is.EqualTo(Digests([ pool, evse ])));

                foreach (var statement in statements)
                    Assert.That(statement!.TryVerify(proof!, [ identityKeyId ], out _, out var verifyError),  Is.True, verifyError);

                Assert.That(ofEVSE["priceStatements"]!.Count(),           Is.EqualTo(1), "Of the pool, the station and the EVSE E1*2: only the pool has one.");
                Assert.That(ofFullPath["priceStatements"]!.Count(),       Is.EqualTo(2));

                Assert.That(posted.StatusCode,                            Is.EqualTo(HttpStatusCode.OK));
                Assert.That(Read(await posted.Content.ReadAsStringAsync())["taken"]!.Value<Int32>(),  Is.EqualTo(1));
                Assert.That(postedForged.StatusCode,                      Is.EqualTo(HttpStatusCode.UnprocessableEntity));
                Assert.That(Read(await postedForged.Content.ReadAsStringAsync())["refusals"]![0]!["description"]!.Value<String>(),
                            Does.Contain("is no signing key"));

                Assert.That(ofCheckpoint["priceStatements"]!.Count(),     Is.EqualTo(3));
                Assert.That(statementProof!.Verify(checkpoint, out var statementVerifyError),  Is.True, statementVerifyError);
                Assert.That(current["priceStatements"]!.Count(),          Is.EqualTo(3));

            }

        }

        #endregion

        #region A_server_of_an_operator_gives_its_price_statements_to_the_one_of_the_network()

        [Test]
        public async Task A_server_of_an_operator_gives_its_price_statements_to_the_one_of_the_network()
        {

            var hub                    = NewHistory(Signing: false);
            hub.CheckpointSigners      = [ History.CheckpointTests.RNOSigner ];

            await using var httpServer = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI     = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(hub);

            // The server of DE*GEF: it signs the price statements of its operator, and follows the hub.
            var operatorServer         = NewHistory();
            var before                 = SetPrice(operatorServer, EVSEType, E11, 0.29m, 1);

            await using var peer       = new RoamingNetworkPeer(new Uri($"ws://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/history/ws"),
                                                                operatorServer,
                                                                new RoamingNetworkPeerOptions {
                                                                    VerifyCheckpointSignature  = History.CheckpointTests.Verify,
                                                                    MinReconnectDelay          = TimeSpan.FromMilliseconds(50),
                                                                    MaxReconnectDelay          = TimeSpan.FromMilliseconds(200)
                                                                });

            using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                await peer.StartAsync(timeout.Token);

            var tookMissed             = await Eventually(() => hub.TryGetPriceStatement(EVSEType, E11, out var statement) && statement.Digest == before.Digest);

            // Now: the hub set the price status as of the statement it took, and gave it back.
            var after                  = SetPrice(operatorServer, EVSEType, E11, 0.31m, 2, Timestamp.Now);
            var tookNew                = await Eventually(() => hub.TryGetPriceStatement(EVSEType, E11, out var statement) && statement.Digest == after.Digest);

            // The checkpoint of the hub names them, and comes back with them.
            Assert.That(hub.TryMakeCheckpoint(out var checkpoint, out var checkpointError),  Is.True, checkpointError);

            var tookCheckpoint         = await Eventually(() => operatorServer.PriceStatementsOf(checkpoint!.Id) is not null);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(tookMissed,                                                     Is.True, "The hub did not take the price statement made before connecting.");
                Assert.That(tookNew,                                                        Is.True, "The hub did not take the new price statement.");
                Assert.That(Digests(hub.PriceStatementsOf(checkpoint!.Id)),                 Is.EqualTo(Digests([ before, after ])));
                Assert.That(tookCheckpoint,                                                 Is.True, "The server of the operator did not take the checkpoint with its price statements.");
                Assert.That(hub.TryGetPriceStatement(EVSEType, E11, out var atHub) && atHub.Sequence == 1,  Is.True);
                Assert.That(operatorServer.TryGetPriceStatement(EVSEType, E11, out var atOperator) && atOperator.Digest == after.Digest,  Is.True,
                            "Taking the price status back from the hub states nothing anew.");

            }

        }

        #endregion


        #region (static) Body(PriceStatement)

        private static StringContent Body(RoamingNetworkPriceStatement PriceStatement)

            => new (new JObject(new JProperty("priceStatements", new JArray(PriceStatement.ToJSON()))).ToString(),
                    Encoding.UTF8,
                    "application/json");

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

    }

}
