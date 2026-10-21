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

using Newtonsoft.Json;

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The test vectors of SPECIFICATION.md: what is written there is what these tests check.
    /// </summary>
    [TestFixture]
    public class SpecificationVectorsTests
    {

        #region Data

        private static readonly DateTimeOffset Created = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private const String ContentJSON     = "{\"@context\":\"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork\",\"@id\":\"rn1\",\"chargingStationOperators\":[{\"@id\":\"DE*GEF\",\"chargingPools\":[{\"@id\":\"DE*GEF*P1\",\"chargingStations\":[{\"@id\":\"DE*GEF*S1\",\"EVSEs\":[{\"@id\":\"DE*GEF*E1*1\",\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\",\"maxPower\":22000,\"maxVoltage\":230.5}],\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\"}],\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\"}],\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\",\"name\":{\"en\":\"Charging Station Operator DE*GEF\"}}],\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\",\"name\":{\"en\":\"Example\"}}";
        private const String JSONETag        = "json:sha256:hex:8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224";
        private const String CBORETag        = "cbor:sha256:hex:291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549";
        private const String TreeETag        = "effectiveTree:sha256:hex:87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221";
        private const String NoTreeETagsJSON = "[[\"json\",\"sha256\",\"hex\",\"8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224\"],[\"cbor\",\"sha256\",\"hex\",\"291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549\"]]";
        private const String TreeETagJSON    = "[\"effectiveTree\",\"sha256\",\"hex\",\"87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221\"]";
        private const String ETagsJSON       = "[[\"json\",\"sha256\",\"hex\",\"8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224\"],[\"cbor\",\"sha256\",\"hex\",\"291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549\"]," + TreeETagJSON + "]";

        private const String CommitContent   = "{\"eTags\":" + ETagsJSON + ",\"profile\":\"wwcp-poi-commit-v3\",\"revision\":0,\"roamingNetworkId\":\"rn1\"}";
        private const String CommitId        = "0471b41509fd8458152ecece3970be190d137a514d7e78f86621a63ac2d45042";

        private const String SigningInput    = "{\"algorithm\":\"Ed25519\",\"commit\":{\"@id\":\"" + CommitId + "\",\"eTags\":" + ETagsJSON + ",\"revision\":0,\"roamingNetworkId\":\"rn1\"},\"encoding\":\"base64\",\"keyId\":\"leader\",\"profile\":\"wwcp-poi-commit-signature-v1\",\"role\":\"leader\",\"signedAt\":\"2026-10-12T12:00:00.000Z\",\"signer\":\"leader.example.org\"}";
        private const String SignatureValue  = "JK3c2nNFxEaf4Z4IJnC8m1tfipZMlY2D3Qbvf7ua7M7am9JwDY2CmddzTVlMkyhnVizVx41BazSLsezsmMhBAg==";
        private const String PublicKey       = "d3bocLkzVPKgskwj8qNsxOgOIjIYwbl5Jv3QGDlqK5s=";

        private const String Timestamps      = "\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\"";
        private const String EVSESkeleton    = "{\"@id\":\"DE*GEF*E1*1\"," + Timestamps + ",\"maxPower\":22000,\"maxVoltage\":230.5}";
        private const String EVSEDigest      = "f37f52daaf5db8a3db648ac8ea7f3ef9955a1afff133803d1a9ff84b99fdc443";
        private const String StationDigest   = "d111c66efd73a1ff910cf674270046cfe3b6a2e0cab85034993df80025623f40";
        private const String PoolDigest      = "8d4f6ed9ca2ffabf1862488a9cff9c8a224a86a247a9bb48b33751d0df5042fe";
        private const String OperatorDigest  = "b137995cb4aacc1bc4f5c1b70f9620187d427bd150c3f9bc405ea079827de2ef";

        private const String AttestationInput = "{\"algorithm\":\"Ed25519\",\"commitId\":\"" + CommitId + "\",\"eTag\":" + TreeETagJSON + ",\"encoding\":\"base64\",\"keyId\":\"rno\",\"profile\":\"wwcp-poi-attestation-v1\",\"revision\":0,\"roamingNetworkId\":\"rn1\",\"role\":\"roamingNetworkOperator\",\"signedAt\":\"2026-10-17T18:00:00.000Z\",\"signer\":\"DE*RNO\",\"validUntil\":\"2026-10-17T18:15:00.000Z\"}";
        private const String AttestationValue = "p8lSj39s8tNH2kaoJXIMcGqLGCHlm7x2IVrZje4IxFfakk6juMlk3QG5MDOJeYN4Q7QyNFoOmLVZcGShypK1CA==";
        private const String RNOPublicKey     = "Kay64UG8yvCyLhqU000LxzYeUm0L/hLIl5S8kyKWbdc=";

        #endregion

        #region (private static) Digest(Skeleton) / Pruned(Root)

        /// <summary>
        /// The digest of a skeleton, SHA-256(0x00 || its canonical JSON), in lowercase hex.
        /// </summary>
        private static String Digest(String Skeleton)

            => Convert.ToHexStringLower(SHA256.HashData([ 0x00, .. Encoding.UTF8.GetBytes(Skeleton) ]));

        /// <summary>
        /// A child array of one child, pruned.
        /// </summary>
        private static String Pruned(String Root)

            => "{\"@merkleRoot\":\"" + Root + "\",\"@size\":1}";

        #endregion

        #region (private static) Network()

        private static RoamingNetwork Network()

            => new (RoamingNetwork_Id.Parse("rn1"),
                    Name:                      new ImmutableI18NString(I18NString.Create(Languages.en, "Example")),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                Created:           Created,
                                                                                                                ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                         Created:  Created,
                                                                                                                                                         EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                                                                                                                                                              MaxPower:    Watt.FromW(22000),
                                                                                                                                                                              MaxVoltage:  Volt.FromV(230.5m),
                                                                                                                                                                              Created:     Created) ]) ]) ]) ]);

        #endregion


        #region The_content_and_its_ETags()

        [Test]
        public void The_content_and_its_ETags()
        {

            var network = Network();
            var eTags   = network.CalcETags();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(CanonicalJSON.Serialize(network.ToJSON(IncludeETags: false)),    Is.EqualTo(ContentJSON));
                Assert.That(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(ContentJSON))),
                            Is.EqualTo(JSONETag[16..]));

                Assert.That(eTags.Select(eTag => eTag.ToString()),                           Is.EqualTo(new[] { JSONETag, CBORETag, TreeETag }));
                // In hex, as a commit writes them; a roaming network writes them in Base64 unless asked otherwise.
                Assert.That(network.ToJSON(ETagDigestEncoding: ETagDigestEncoding.HEX)["eTags"]!.ToString(Formatting.None),  Is.EqualTo(ETagsJSON));

                // The CBOR ETag follows from the canonical JSON alone, and is the one of the CBOR the library writes.
                Assert.That(Convert.ToHexStringLower(SHA256.HashData(CBORJSON.ToCBOR(Encoding.UTF8.GetBytes(ContentJSON)).ToByteArray(CBORWriterOptions.Canonical))),
                            Is.EqualTo(CBORETag[16..]));

                Assert.That(Convert.ToHexStringLower(SHA256.HashData(network.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical))),
                            Is.EqualTo(CBORETag[16..]));

            }

        }

        #endregion

        #region The_genesis_commit_and_its_signature()

        [Test]
        public void The_genesis_commit_and_its_signature()
        {

            var genesis    = RoamingNetworkCommit.Genesis(Network());
            var leaderKey  = new Ed25519PrivateKeyParameters(Convert.FromHexString("101112131415161718191a1b1c1d1e1f202122232425262728292a2b2c2d2e2f"), 0);
            var signature  = genesis.CreateSignature(leaderKey,
                                                     "leader",
                                                     COSEAlgorithm.Ed25519,
                                                     RoamingNetworkCommitSignatureRoles.Leader,
                                                     "leader.example.org",
                                                     DateTimeOffset.Parse("2026-10-12T12:00:00Z"));

            var publicKey  = new Ed25519PublicKeyParameters(Convert.FromBase64String(PublicKey), 0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(CommitContent))),  Is.EqualTo(CommitId));
                Assert.That(genesis.Id.ToString(),                                                            Is.EqualTo(CommitId));

                Assert.That(Encoding.UTF8.GetString(genesis.GetSigningBytes(signature)),                      Is.EqualTo(SigningInput));
                Assert.That(signature.Value,                                                                  Is.EqualTo(SignatureValue));
                Assert.That(Convert.ToBase64String(leaderKey.GeneratePublicKey().GetEncoded()),               Is.EqualTo(PublicKey));
                Assert.That(genesis.VerifySignature(signature, publicKey, "leader", out var error),           Is.True, error);

            }

        }

        #endregion

        #region The_genesis_of_a_second_epoch()

        [Test]
        public void The_genesis_of_a_second_epoch()
        {

            const String Input  = "{\"eTags\":[[\"json\",\"sha256\",\"hex\",\"8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224\"],[\"cbor\",\"sha256\",\"hex\",\"291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549\"],[\"effectiveTree\",\"sha256\",\"hex\",\"87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221\"]],\"epoch\":2,\"profile\":\"wwcp-poi-commit-v3\",\"profiles\":{\"canonicalization\":\"styx-canonical-json-v1\",\"checkpoint\":\"wwcp-poi-checkpoint-v1\",\"commit\":\"wwcp-poi-commit-v3\",\"identificationHash\":\"sha512\",\"priceStatement\":\"wwcp-poi-price-statement-v1\",\"signatureAlgorithms\":[\"Ed25519\",\"ML-DSA-65\"]},\"revision\":0,\"roamingNetworkId\":\"rn1\"}";
            const String Id     = "4ce205c4e6d07d54838b0740f8b9c0ff12941a6f38c4ab37145f115f734a870cbe8b0dd429b569a59bcb34dfe3a924a066fbe2fbeee8e0c07ea2e8d983254a3f";

            var genesis = RoamingNetworkCommit.Genesis(Network(),
                                                       new RoamingNetworkEpoch(2, new RoamingNetworkEpochProfiles(ETagHashAlgorithm.SHA512, [ "Ed25519", "ML-DSA-65" ])));

            var content = genesis.ToJSON(Embedded: true);
            content.Remove("@id");
            content.AddFirst(new Newtonsoft.Json.Linq.JProperty("profile", RoamingNetworkCommit.IdentityProfile));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(CanonicalJSON.Serialize(content),                                                        Is.EqualTo(Input));
                Assert.That(genesis.Id.ToString(),                                                                   Is.EqualTo(Id));
                Assert.That(Convert.ToHexStringLower(SHA512.HashData(Encoding.UTF8.GetBytes(Input))),                Is.EqualTo(Id));
                Assert.That(RoamingNetworkCommit.TryParse(genesis.ToJSON().ToString(), out var parsed, out var error), Is.True, error);
                Assert.That(parsed!.Id,                                                                              Is.EqualTo(genesis.Id));
            }

        }

        #endregion

        #region A_commit_without_effective_trees_is_refused()

        [Test]
        public void A_commit_without_effective_trees_is_refused()
        {

            var json   = "{\"@id\":\"" + CommitId + "\",\"eTags\":" + NoTreeETagsJSON + ",\"revision\":0,\"roamingNetworkId\":\"rn1\"}";
            var parsed = RoamingNetworkCommit.TryParse(json, out _, out var error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed,                                                                         Is.False);
                Assert.That(error,                                                                          Does.Contain("effectiveTree"));
                Assert.That(Network().MatchesCommitETags([ .. Newtonsoft.Json.Linq.JArray.Parse(NoTreeETagsJSON).Select(eTag => ETag.Parse(eTag)) ]),  Is.False);
            }

        }

        #endregion

        #region The_effective_tree_of_the_example()

        [Test]
        public void The_effective_tree_of_the_example()
        {

            var stationSkeleton   = "{\"@id\":\"DE*GEF*S1\",\"EVSEs\":"             + Pruned(EVSEDigest)     + "," + Timestamps + "}";
            var poolSkeleton      = "{\"@id\":\"DE*GEF*P1\",\"chargingStations\":"  + Pruned(StationDigest)  + "," + Timestamps + "}";
            var operatorSkeleton  = "{\"@id\":\"DE*GEF\",\"chargingPools\":"        + Pruned(PoolDigest)     + "," + Timestamps + ",\"name\":{\"en\":\"Charging Station Operator DE*GEF\"}}";
            var networkSkeleton   = "{\"@context\":\"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork\",\"@id\":\"rn1\",\"chargingStationOperators\":" + Pruned(OperatorDigest) + "," + Timestamps + ",\"name\":{\"en\":\"Example\"}}";

            var network           = Network();
            var tree              = network.EffectiveTree(ETagHashAlgorithm.SHA256);

            using (Assert.EnterMultipleScope())
            {

                // A single child is the root of its array: MTH({d}) = H(0x00 || d).
                Assert.That(Digest(EVSESkeleton),                                                     Is.EqualTo(EVSEDigest));
                Assert.That(Digest(stationSkeleton),                                                  Is.EqualTo(StationDigest));
                Assert.That(Digest(poolSkeleton),                                                     Is.EqualTo(PoolDigest));
                Assert.That(Digest(operatorSkeleton),                                                 Is.EqualTo(OperatorDigest));
                Assert.That("effectiveTree:sha256:hex:" + Digest(networkSkeleton),                    Is.EqualTo(TreeETag));

                Assert.That(network.EffectiveTreeETag(ETagHashAlgorithm.SHA256).ToString(),           Is.EqualTo(TreeETag));
                Assert.That(CanonicalJSON.Serialize(tree.Root.Skeleton),                              Is.EqualTo(networkSkeleton));
                Assert.That(CanonicalJSON.Serialize(tree.Root.Arrays["chargingStationOperators"].Children![0].Skeleton),  Is.EqualTo(operatorSkeleton));

            }

        }

        #endregion

        #region A_key_statement()

        [Test]
        public void A_key_statement()
        {

            const String Input  = "{\"algorithm\":\"Ed25519\",\"encoding\":\"base64\",\"identityKeys\":[{\"@id\":\"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802\",\"key\":\"owEBIAYhWCCLsE4cG4Pd3zEfW83ffFDt48CAL0fseW4qExz0EpjZ8w==\"}],\"keyId\":\"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802\",\"profile\":\"wwcp-poi-key-statement-v1\",\"roamingNetworkId\":\"rn1\",\"roamingNetworkOperatorId\":\"DE*RNO\",\"signedAt\":\"2026-10-17T00:00:00.000Z\",\"signingKeys\":[{\"@id\":\"ea671729aa29c6140b5129b8d59e13fd9fbd4b96e37b3c5185fb11c3eeeac554\",\"key\":\"owEBIAYhWCAprLrhQbzK8LIuGpTTTQvHNh5SbQv+EsiXlLyTIpZt1w==\"}],\"validUntil\":\"2026-11-16T00:00:00.000Z\"}";
            const String Value  = "wStKI6DfjrT9oDvyenKRCUtHsyDj/7SCO2rnTq4+S5QoejTkaudDzqt+Ir8PXjXojYsiolJVdE/ZKhuyaEWVCg==";

            var signingKey   = new Ed25519PrivateKeyParameters(Convert.FromHexString("202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"), 0);
            var identityKey  = new Ed25519PrivateKeyParameters(Convert.FromHexString("303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f"), 0);
            var statement    = RoamingNetworkKeyStatement.Create(identityKey,
                                                                 COSEAlgorithm.Ed25519,
                                                                 RoamingNetwork_Id.Parse("rn1"),
                                                                 RoamingNetworkOperator_Id.Parse("DE*RNO"),
                                                                 [ POIPublicKey.From(signingKey) ],
                                                                 [ POIPublicKey.From(identityKey) ],
                                                                 DateTimeOffset.Parse("2026-10-17T00:00:00Z"),
                                                                 DateTimeOffset.Parse("2026-11-16T00:00:00Z"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Encoding.UTF8.GetString(statement.GetSigningBytes()),                                                 Is.EqualTo(Input));
                Assert.That(statement.Value,                                                                                      Is.EqualTo(Value));
                Assert.That(statement.TryVerify([ "1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802" ], null, out var error),  Is.True, error);
                Assert.That(statement.SigningKeyOf("ea671729aa29c6140b5129b8d59e13fd9fbd4b96e37b3c5185fb11c3eeeac554", DateTimeOffset.Parse("2026-10-20T00:00:00Z")),  Is.Not.Null);
            }

        }

        #endregion

        #region A_key_statement_of_a_quorum()

        /// <summary>
        /// A key statement of the profile wwcp-poi-key-statement-v2 of an operator whose keys two holders share, of which both
        /// must sign: each signs the same input with its own keyId.
        /// </summary>
        [Test]
        public void A_key_statement_of_a_quorum()
        {

            const String InputOfAlpha  = "{\"algorithm\":\"Ed25519\",\"encoding\":\"base64\",\"identityKeys\":[{\"@id\":\"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802\",\"holder\":\"Alpha\",\"key\":\"owEBIAYhWCCLsE4cG4Pd3zEfW83ffFDt48CAL0fseW4qExz0EpjZ8w==\"},{\"@id\":\"a287cebcd93b836a712b6393a914a86e63516d401285f3c642f734a28146f83f\",\"holder\":\"Beta\",\"key\":\"owEBIAYhWCAc5WpIyC/5kWKhS8VEYSZ05dYfuTF+ZdQFV4D9vLTcNQ==\"}],\"keyId\":\"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802\",\"minConsensus\":2,\"profile\":\"wwcp-poi-key-statement-v2\",\"roamingNetworkId\":\"rn1\",\"roamingNetworkOperatorId\":\"DE*RNO\",\"signedAt\":\"2026-10-17T00:00:00.000Z\",\"signingKeys\":[{\"@id\":\"c1924d610e8d9f1c5efdbca564a2f19991517ab8252807651f94ac5332afe3f9\",\"holder\":\"Beta\",\"key\":\"owEBIAYhWCAXRVO0Vt3fxpCOyrHBAf5qsh4rqgYXeVt9Q6Y0gpk/1Q==\"},{\"@id\":\"ea671729aa29c6140b5129b8d59e13fd9fbd4b96e37b3c5185fb11c3eeeac554\",\"holder\":\"Alpha\",\"key\":\"owEBIAYhWCAprLrhQbzK8LIuGpTTTQvHNh5SbQv+EsiXlLyTIpZt1w==\"}],\"validUntil\":\"2026-11-16T00:00:00.000Z\"}";
            const String ValueOfAlpha  = "+aCgAP2Y/8nIwRDpnOkx8gaRnO9SP96dq31VSocHN3v0+TXsOE0mVU2DdO9mglFjZOaHsAWtNDMrv9H3Tjx9Cg==";
            const String ValueOfBeta   = "S9n8fy68dcR2YV08EVa1y3QllkuyqiBrAvY8iOZlGg1h7UJqUMYF6jnPmOT+tgUIFhFUAj7C8vLx3iuaYmoxBw==";

            var alphaSigning   = new Ed25519PrivateKeyParameters(Convert.FromHexString("202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"), 0);
            var alphaIdentity  = new Ed25519PrivateKeyParameters(Convert.FromHexString("303132333435363738393a3b3c3d3e3f404142434445464748494a4b4c4d4e4f"), 0);
            var betaSigning    = new Ed25519PrivateKeyParameters(Convert.FromHexString("606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f"), 0);
            var betaIdentity   = new Ed25519PrivateKeyParameters(Convert.FromHexString("707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f"), 0);

            var statement      = RoamingNetworkKeyStatement.Create([ (alphaIdentity, COSEAlgorithm.Ed25519) ],
                                                                   2,
                                                                   RoamingNetwork_Id.Parse("rn1"),
                                                                   RoamingNetworkOperator_Id.Parse("DE*RNO"),
                                                                   [ POIPublicKey.From(alphaSigning,  Holder: "Alpha"), POIPublicKey.From(betaSigning,  Holder: "Beta") ],
                                                                   [ POIPublicKey.From(alphaIdentity, Holder: "Alpha"), POIPublicKey.From(betaIdentity, Holder: "Beta") ],
                                                                   DateTimeOffset.Parse("2026-10-17T00:00:00Z"),
                                                                   DateTimeOffset.Parse("2026-11-16T00:00:00Z")).
                                     WithSignature(betaIdentity, COSEAlgorithm.Ed25519);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Encoding.UTF8.GetString(statement.GetSigningBytes()),  Is.EqualTo(InputOfAlpha));
                Assert.That(statement.Signatures[0].Value,                          Is.EqualTo(ValueOfAlpha));
                Assert.That(statement.Signatures[1].Value,                          Is.EqualTo(ValueOfBeta));
                Assert.That(statement.TryVerify(statement.IdentityKeys.Select(key => key.Id), null, out var error),  Is.True, error);
            }

        }

        #endregion

        #region A_trust_anchor()

        /// <summary>
        /// The trust anchor of the example network as the text of a QR code: WWCPPOI1: and the Base45 of its CBOR.
        /// </summary>
        [Test]
        public void A_trust_anchor()
        {

            const String QRCode  = "WWCPPOI1:Q1L..DF$D4IK1$CM2D$9FQ$DTVD38DUPCG/DB$CQWE-69MWEW9E0G7%:5I9E -DNPCGECD3D3/D+%5*VD$$EQ:5-3E7WEBAFCPES4FVPC:L5VQE$-D0LEBECZEDV2D7$CK4F-KEIUA7%EGVE:.D59D.KEPED1$CBWERWE-N9-FF$FGW24.+U3M0-$O1JV%LLP-T2OR-7U%J0WP7ZXAW7RI33:PKFFM%D0P$DZKEXC9X.C/KEIEC EDK/D-K3MWEW9E0G7-:5B/D.%5$9FQ$DTVD-:5S3EJ$D3/DV9ES3EJ$D3/D:+9PWE04EFPD4VC:KECB60LEBECZEDV2D7$CK4F-KEHZA/C9U/3MWEW9E0G7V:5Z$E.%5$9FQ$DTVD*:5 3E9:5D-9-:5*-D";

            var anchor = new RoamingNetworkTrustAnchor(new Uri("https://hub.example/poi/RNs/rn1"),
                                                       [ "1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802" ],
                                                       1,
                                                       new ImmutableI18NString(I18NString.Create(Languages.en, "Example")),
                                                       null,
                                                       new Uri("https://rno.example/roaming"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(anchor.ToJSON().ToString(Formatting.None),  Is.EqualTo("{\"@context\":\"https://open.charging.cloud/contexts/wwcp+json/roamingNetworkTrustAnchor\",\"roamingNetworkId\":\"rn1\",\"name\":{\"en\":\"Example\"},\"roamingNetworkURI\":\"https://hub.example/poi/RNs/rn1\",\"moreInformation\":\"https://rno.example/roaming\",\"identityKeys\":[\"1ef48003e1c482f88fa9f4ecb3d9cdeeb2037d3be4550ad6ee1854a2c5b0b802\"]}"));
                Assert.That(anchor.ToQRCode(),                          Is.EqualTo(QRCode));
                Assert.That(RoamingNetworkTrustAnchor.TryParse(QRCode, out var read, out var error),  Is.True, error);
            }

        }

        #endregion

        #region A_price_statement()

        [Test]
        public void A_price_statement()
        {

            const String OperatorPublicKey  = "JUO5L/EJVRFHatyDadtt3JM2ZaEZeN2hQE7hBmypVZ0=";
            const String KeyId              = "8cb46c0f98440da34ad4af304431f76c7cb7462fa8fdfd5d6261dc685809220d";
            const String Input              = "{\"algorithm\":\"Ed25519\",\"encoding\":\"base64\",\"entityId\":\"DE*GEF*E1*1\",\"entityType\":\"EVSE\",\"keyId\":\"8cb46c0f98440da34ad4af304431f76c7cb7462fa8fdfd5d6261dc685809220d\",\"priceStatus\":{\"prices\":[{\"chargingTariffId\":\"DE*GEF*T1\",\"elements\":[{\"priceComponents\":[{\"price\":0.39,\"stepSize\":1,\"type\":\"ENERGY\"}]}]}]},\"profile\":\"wwcp-poi-price-statement-v1\",\"roamingNetworkId\":\"rn1\",\"role\":\"chargingStationOperator\",\"sequence\":0,\"signedAt\":\"2026-10-19T11:59:58.000Z\",\"signer\":\"DE*GEF\",\"validFrom\":\"2026-10-19T12:00:00.000Z\",\"validUntil\":\"2026-10-19T12:15:00.000Z\"}";
            const String Value              = "95n+aTF2ZF3WqdHA8BSZPWqfSvxkMhzSQjCScPsZn13Jhehdi4d/aokA4507iZBy9fmuHRj9vNq5dfiaa0NQDw==";
            const String Digest             = "efbd7b8f0d28a0affe44a872dbaefdd37b6afa5366202d4653b8a5b58bcef772";

            var operatorKey  = new Ed25519PrivateKeyParameters(Convert.FromHexString("404142434445464748494a4b4c4d4e4f505152535455565758595a5b5c5d5e5f"), 0);
            var publicKey    = POIPublicKey.From(operatorKey);
            var prices       = new ChargingPrices([ new ChargingTariffPrices(ChargingTariff_Id.Parse("DE*GEF*T1"),
                                                                             [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ]) ]);

            var statement    = RoamingNetworkPriceStatement.Create(operatorKey,
                                                                   publicKey.Id,
                                                                   COSEAlgorithm.Ed25519,
                                                                   RoamingNetwork_Id.Parse("rn1"),
                                                                   nameof(EVSE),
                                                                   "DE*GEF*E1*1",
                                                                   prices,
                                                                   0,
                                                                   null,
                                                                   DateTimeOffset.Parse("2026-10-19T11:59:58Z"),
                                                                   DateTimeOffset.Parse("2026-10-19T12:00:00Z"),
                                                                   DateTimeOffset.Parse("2026-10-19T12:15:00Z"));

            // The digest is the SHA-256 of the canonical JSON of the statement, its signature value included.
            var withValue    = Input[..^1] + ",\"value\":\"" + Value + "\"}";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Convert.ToBase64String(operatorKey.GeneratePublicKey().GetEncoded()),               Is.EqualTo(OperatorPublicKey));
                Assert.That(publicKey.Id,                                                                       Is.EqualTo(KeyId));
                Assert.That(Encoding.UTF8.GetString(statement.GetSigningBytes()),                               Is.EqualTo(Input));
                Assert.That(statement.Value,                                                                    Is.EqualTo(Value));
                Assert.That(CanonicalJSON.Serialize(statement.ToJSON(Embedded: true)),                                        Is.EqualTo(withValue));
                Assert.That(statement.Digest,                                                                   Is.EqualTo(Digest));
                Assert.That(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(withValue))),       Is.EqualTo(Digest));
                Assert.That(statement.TryVerify([ publicKey ], out var error),                                  Is.True, error);
            }

        }

        #endregion

        #region An_attestation_and_a_station_proof()

        [Test]
        public void An_attestation_and_a_station_proof()
        {

            var rnoKey       = new Ed25519PrivateKeyParameters(Convert.FromHexString("202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"), 0);
            var network      = Network();
            var tree         = network.EffectiveTree(ETagHashAlgorithm.SHA256);
            var attestation  = RoamingNetworkAttestation.Create(rnoKey,
                                                                "rno",
                                                                COSEAlgorithm.Ed25519,
                                                                RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                                network.Id,
                                                                RoamingNetworkCommit_Id.Parse(CommitId),
                                                                0,
                                                                tree.ETag,
                                                                "DE*RNO",
                                                                DateTimeOffset.Parse("2026-10-17T18:00:00Z"),
                                                                DateTimeOffset.Parse("2026-10-17T18:15:00Z"));

            var publicKey    = new Ed25519PublicKeyParameters(Convert.FromBase64String(RNOPublicKey), 0);

            Assert.That(tree.TryGetChargingStationProof(ChargingStation_Id.Parse("DE*GEF*S1"), out var proof, out var error, RoamingNetworkCommit_Id.Parse(CommitId), 0, [ attestation ]), Is.True, error);
            Assert.That(RoamingNetworkProof.TryParse(proof!.ToJSON().ToString(), out var read, out error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Encoding.UTF8.GetString(attestation.GetSigningBytes()),                   Is.EqualTo(AttestationInput));
                Assert.That(attestation.Value,                                                        Is.EqualTo(AttestationValue));
                Assert.That(Convert.ToBase64String(rnoKey.GeneratePublicKey().GetEncoded()),          Is.EqualTo(RNOPublicKey));
                Assert.That(attestation.VerifySignature(publicKey, "rno", out error),                 Is.True, error);

                Assert.That(read!.Verify(candidate => candidate.VerifySignature(publicKey, "rno", out _), out _, out error, DateTimeOffset.Parse("2026-10-17T18:05:00Z")),  Is.True, error);
                Assert.That(read.Verify(candidate => candidate.VerifySignature(publicKey, "rno", out _), out _, out error, DateTimeOffset.Parse("2026-10-17T18:16:00Z")),   Is.False, "after its validUntil");

            }

        }


        #endregion

    }

}
