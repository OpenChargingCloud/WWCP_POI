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

        private const String ContentJSON     = "{\"@context\":\"https://open.charging.cloud/contexts/wwcp+json/roamingNetwork\",\"@id\":\"rn1\",\"chargingStationOperators\":[{\"@id\":\"DE*GEF\",\"chargingPools\":[{\"@id\":\"DE*GEF*P1\",\"chargingStations\":[{\"@id\":\"DE*GEF*S1\",\"EVSEs\":[{\"@id\":\"DE*GEF*E1*1\",\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\",\"maxPower\":22000,\"maxVoltage\":230.5}],\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\"}],\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\"}],\"created\":\"2026-01-01T00:00:00.000Z\",\"dataLicenses\":[],\"lastChange\":\"2026-01-01T00:00:00.000Z\",\"name\":{\"en\":\"Charging Station Operator DE*GEF\"}}],\"created\":\"2026-01-01T00:00:00.000Z\",\"lastChange\":\"2026-01-01T00:00:00.000Z\",\"name\":{\"en\":\"Example\"}}";
        private const String JSONETag        = "json:sha256:hex:a3432d3ec7622d4272a7d4c0eba47883ee562c5501804285d9b01aea0dd692de";
        private const String CBORETag        = "cbor:sha256:hex:631d24121cc4ee0177f262b6703b1ddfd63552dc69109c77971ae079e679f421";
        private const String ETagsJSON       = "[[\"json\",\"sha256\",\"hex\",\"a3432d3ec7622d4272a7d4c0eba47883ee562c5501804285d9b01aea0dd692de\"],[\"cbor\",\"sha256\",\"hex\",\"631d24121cc4ee0177f262b6703b1ddfd63552dc69109c77971ae079e679f421\"]]";

        private const String CommitContent   = "{\"eTags\":" + ETagsJSON + ",\"profile\":\"wwcp-poi-commit-v1\",\"revision\":0,\"roamingNetworkId\":\"rn1\"}";
        private const String CommitId        = "f572ecd1b3ae708cb7eacf0e9da3a34537aae55c94d88ad7224a9bef1663c45f";

        private const String SigningInput    = "{\"algorithm\":\"Ed25519\",\"commit\":{\"@id\":\"" + CommitId + "\",\"eTags\":" + ETagsJSON + ",\"revision\":0,\"roamingNetworkId\":\"rn1\"},\"encoding\":\"base64\",\"keyId\":\"leader\",\"profile\":\"wwcp-poi-commit-signature-v1\",\"role\":\"leader\",\"signedAt\":\"2026-10-12T12:00:00.000Z\",\"signer\":\"leader.example.org\"}";
        private const String SignatureValue  = "su8SeDqPhzRxZYvEvQ89OiaIDKjVhnMShfO+iPsoGzqAAel6JMykYRKlttQGBYB5uyRIRe+P15XYrNmiDPf0CA==";
        private const String PublicKey       = "d3bocLkzVPKgskwj8qNsxOgOIjIYwbl5Jv3QGDlqK5s=";

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

                Assert.That(eTags.Select(eTag => eTag.ToString()),                           Is.EqualTo(new[] { JSONETag, CBORETag }));
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

    }

}
