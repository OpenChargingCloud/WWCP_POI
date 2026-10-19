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
        private const String ETagsV2JSON     = "[[\"json\",\"sha256\",\"hex\",\"8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224\"],[\"cbor\",\"sha256\",\"hex\",\"291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549\"]]";
        private const String TreeETagJSON    = "[\"effectiveTree\",\"sha256\",\"hex\",\"87923ecc05779a7440e8da51589252f95eeed4977afabd211190c7b3b6602221\"]";
        private const String ETagsJSON       = "[[\"json\",\"sha256\",\"hex\",\"8319cc9794532447aa505995a52e2be10c4b02198e280fc430bc4b6e5d1d5224\"],[\"cbor\",\"sha256\",\"hex\",\"291ec92c68d771f97a3e90869f7735beda533584f69668cf7a2ca26f3246e549\"]," + TreeETagJSON + "]";

        private const String CommitContent   = "{\"eTags\":" + ETagsJSON + ",\"profile\":\"wwcp-poi-commit-v3\",\"revision\":0,\"roamingNetworkId\":\"rn1\"}";
        private const String CommitId        = "0471b41509fd8458152ecece3970be190d137a514d7e78f86621a63ac2d45042";

        private const String CommitV2Content = "{\"eTags\":" + ETagsV2JSON + ",\"profile\":\"wwcp-poi-commit-v2\",\"revision\":0,\"roamingNetworkId\":\"rn1\"}";
        private const String CommitV2Id      = "d48066e0a5e68f9019c8c2e0a911ff7d99e2ff054a79e3343e5de2ea14e33117";

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
                Assert.That(network.ToJSON()["eTags"]!.ToString(Formatting.None),           Is.EqualTo(ETagsJSON));

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

        #region A_commit_before_effective_trees()

        [Test]
        public void A_commit_before_effective_trees()
        {

            var json   = "{\"@id\":\"" + CommitV2Id + "\",\"eTags\":" + ETagsV2JSON + ",\"revision\":0,\"roamingNetworkId\":\"rn1\"}";
            var parsed = RoamingNetworkCommit.TryParse(json, out var commit, out var error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(CommitV2Content))),  Is.EqualTo(CommitV2Id));
                Assert.That(parsed,                                                                              Is.True, error);
                Assert.That(commit!.Profile,                                                                     Is.EqualTo(RoamingNetworkCommit.IdentityProfile));
                Assert.That(Network().MatchesCommitETags(commit.ETags),                                          Is.True);
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
