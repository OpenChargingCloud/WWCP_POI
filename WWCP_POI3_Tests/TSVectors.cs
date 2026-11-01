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
using Org.BouncyCastle.Crypto.Signers;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The test vectors of WWCP_POI.TS 0.1.2-dev.0 in Vectors/TS, which ChargyWebApp generated with public test keys: the
    /// identity keys of the seeds 41 and 43, the signing keys of the seeds 42 and 44, all Ed25519.
    /// </summary>
    internal static class TSVectors
    {

        /// <summary>
        /// The JSON of the given vector, read as POI reads JSON.
        /// </summary>
        /// <param name="Name">The name of the vector, without ".json".</param>
        internal static JObject Read(String Name)

            => (JObject) POIJSON.Read(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Vectors", "TS", Name + ".json")));

        /// <summary>
        /// The JSON of the given vector, of any kind, read as POI reads JSON.
        /// </summary>
        /// <param name="Name">The name of the vector, without ".json".</param>
        internal static JToken ReadToken(String Name)

            => POIJSON.Read(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Vectors", "TS", Name + ".json")));

        /// <summary>
        /// The trust anchor the given bootstrap of an evidence names.
        /// </summary>
        internal static RoamingNetworkTrustAnchor TrustAnchorOf(JObject Bootstrap)

            => RoamingNetworkTrustAnchor.TryParse(Bootstrap["trustAnchor"]!.Value<String>()!, out var anchor, out var error)
                   ? anchor
                   : throw new ArgumentException(error);

        /// <summary>
        /// The key statement of the roaming network operator the given bootstrap of an evidence names.
        /// </summary>
        internal static RoamingNetworkKeyStatement KeyStatementOf(JObject Bootstrap)

            => RoamingNetworkKeyStatement.TryParse((JObject) Bootstrap["keyStatement"]!["keyStatement"]!, out var statement, out var error)
                   ? statement
                   : throw new ArgumentException(error);

        /// <summary>
        /// The given proof.
        /// </summary>
        internal static RoamingNetworkProof ProofOf(JToken JSON)

            => RoamingNetworkProof.TryParse((JObject) JSON, out var proof, out var error)
                   ? proof
                   : throw new ArgumentException(error);

        /// <summary>
        /// The trust context of every identity key of the given trust anchor, of the given key statement, at the given time.
        /// </summary>
        internal static RoamingNetworkTrustContext ContextOf(RoamingNetworkTrustAnchor   TrustAnchor,
                                                             RoamingNetworkKeyStatement  KeyStatement,
                                                             DateTimeOffset              At)

            => RoamingNetworkTrustContext.TryCreate(TrustAnchor, KeyStatement, TrustAnchor.Keys, out var context, out var error, At: At)
                   ? context
                   : throw new ArgumentException(error);

        /// <summary>
        /// The Ed25519 signature of the given data by the test key of the given seed, as Base64.
        /// </summary>
        internal static String Sign(Byte    Seed,
                                    Byte[]  Data)
        {

            var signer = new Ed25519Signer();
            signer.Init(true, new Ed25519PrivateKeyParameters(Enumerable.Repeat(Seed, 32).ToArray()));
            signer.BlockUpdate(Data, 0, Data.Length);

            return Convert.ToBase64String(signer.GenerateSignature());

        }

    }

}
