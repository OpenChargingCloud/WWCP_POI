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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The withdrawal of a change set waiting for the signatures of its owners (SPECIFICATION.md, section 4.2), signed by
    /// a key of an owner of one of its changes: the roaming network, the change set and when it was withdrawn.
    /// </summary>
    public sealed record RoamingNetworkChangeSetWithdrawal
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object, which names what its signature signs and in which version: it is part of its
        /// signature input also where it is embedded.
        /// </summary>
        public const String JSONLDContext   = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/changeSetWithdrawal/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context           = "@context";

            /// <summary>
            /// The property of the roaming network of the change set.
            /// </summary>
            public const String  RoamingNetworkId  = "roamingNetworkId";

            /// <summary>
            /// The property of the identification of the change set.
            /// </summary>
            public const String  ChangeSetId       = "changeSetId";

            /// <summary>
            /// The property of when the change set was withdrawn.
            /// </summary>
            public const String  WithdrawnAt       = "withdrawnAt";

            /// <summary>
            /// The property of its signature algorithm.
            /// </summary>
            public const String  Algorithm         = "algorithm";

            /// <summary>
            /// The property of the identification of its key.
            /// </summary>
            public const String  KeyId             = "keyId";

            /// <summary>
            /// The property of its signature value.
            /// </summary>
            public const String  Value             = "value";

        }

        #endregion


        /// <summary>
        /// The roaming network of the change set.
        /// </summary>
        public RoamingNetwork_Id  RoamingNetworkId    { get; }

        /// <summary>
        /// The identification of the change set.
        /// </summary>
        public ChangeSet_Id       ChangeSetId         { get; }

        /// <summary>
        /// When the change set was withdrawn, to the millisecond.
        /// </summary>
        public DateTimeOffset     WithdrawnAt         { get; }

        /// <summary>
        /// The canonical Styx COSE algorithm name.
        /// </summary>
        public String             Algorithm           { get; }

        /// <summary>
        /// The @id of the key that signed it.
        /// </summary>
        public String             KeyId               { get; }

        /// <summary>
        /// The signature bytes, as canonical standard Base64.
        /// </summary>
        public String             Value               { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a withdrawal of a change set; it is checked cryptographically when verified.
        /// </summary>
        /// <param name="RoamingNetworkId">The roaming network of the change set.</param>
        /// <param name="ChangeSetId">The identification of the change set.</param>
        /// <param name="WithdrawnAt">When the change set was withdrawn; kept to the millisecond.</param>
        /// <param name="Algorithm">The canonical Styx COSE algorithm name.</param>
        /// <param name="KeyId">The @id of the key that signed it.</param>
        /// <param name="Value">The signature bytes, as canonical standard Base64.</param>
        public RoamingNetworkChangeSetWithdrawal(RoamingNetwork_Id  RoamingNetworkId,
                                                 ChangeSet_Id       ChangeSetId,
                                                 DateTimeOffset     WithdrawnAt,
                                                 String             Algorithm,
                                                 String             KeyId,
                                                 String             Value)
        {

            this.RoamingNetworkId  = RoamingNetworkId;
            this.ChangeSetId       = ChangeSetId;
            this.WithdrawnAt       = DateTimeOffset.FromUnixTimeMilliseconds(WithdrawnAt.ToUnixTimeMilliseconds());
            this.Algorithm         = Required(Algorithm, nameof(Algorithm));
            this.KeyId             = Required(KeyId,     nameof(KeyId));
            this.Value             = Required(Value,     nameof(Value));

        }

        #endregion


        #region (static) Create(ChangeSet, PrivateKey, KeyId, Algorithm, WithdrawnAt = null)

        /// <summary>
        /// Withdraw the given change set, signed with the given private key of an owner of one of its changes.
        /// </summary>
        /// <param name="ChangeSet">The change set waiting for signatures.</param>
        /// <param name="PrivateKey">The private key.</param>
        /// <param name="KeyId">The @id of its public key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="WithdrawnAt">When it is withdrawn, now by default.</param>
        public static RoamingNetworkChangeSetWithdrawal Create(RoamingNetworkChangeSet  ChangeSet,
                                                               AsymmetricKeyParameter   PrivateKey,
                                                               String                   KeyId,
                                                               COSEAlgorithm            Algorithm,
                                                               DateTimeOffset?          WithdrawnAt   = null)
        {

            ArgumentNullException.ThrowIfNull(ChangeSet);
            ArgumentNullException.ThrowIfNull(PrivateKey);

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            var withdrawnAt  = DateTimeOffset.FromUnixTimeMilliseconds((WithdrawnAt ?? Timestamp.Now).ToUnixTimeMilliseconds());
            var value        = Algorithm.Sign(SigningBytes(ChangeSet.RoamingNetworkId, ChangeSet.Id, withdrawnAt, Algorithm.Name, KeyId),
                                              PrivateKey,
                                              Deterministic: true);

            return new (ChangeSet.RoamingNetworkId,
                        ChangeSet.Id,
                        withdrawnAt,
                        Algorithm.Name,
                        KeyId,
                        Convert.ToBase64String(value));

        }

        #endregion

        #region Verify(PublicKey, out ErrorResponse)

        /// <summary>
        /// Whether this withdrawal is signed with the private key of the given public key.
        /// Noncanonical Base64 and mismatched keys return false.
        /// </summary>
        /// <param name="PublicKey">The public key of its KeyId.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean Verify(AsymmetricKeyParameter            PublicKey,
                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            try
            {

                ArgumentNullException.ThrowIfNull(PublicKey);

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Verification requires a public asymmetric key.", nameof(PublicKey));

                if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || algorithm.Name != Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                var value = Convert.FromBase64String(Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Value)
                    throw new ArgumentException("The signature value must be nonempty canonical standard Base64.");

                return algorithm.Verify(SigningBytes(RoamingNetworkId, ChangeSetId, WithdrawnAt, Algorithm, KeyId),
                                        value,
                                        PublicKey,
                                        out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion


        #region TryParse(JSON, out Withdrawal, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the withdrawal of a change set.
        /// Its withdrawnAt must be written as this library writes it, e.g. 2026-10-22T12:00:00.000Z, as it is signed.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Withdrawal">The parsed withdrawal.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                      JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkChangeSetWithdrawal?  Withdrawal,
                                       [NotNullWhen(false)] out String?                             ErrorResponse)
        {

            Withdrawal = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the withdrawal of a change set must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory    (PropertyKeys.RoamingNetworkId,  "roaming network identification",  RoamingNetwork_Id.TryParse,  out RoamingNetwork_Id roamingNetworkId,  out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.ChangeSetId,       "change set identification",       out var changeSetIdText,  out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.WithdrawnAt,       "withdrawal timestamp",            out var withdrawnAt,      out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.Algorithm,         "signature algorithm",             out var algorithm,        out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.KeyId,             "key identification",              out var keyId,            out ErrorResponse) ||
                    !JSON.ParseMandatoryText(PropertyKeys.Value,             "signature value",                 out var value,            out ErrorResponse))
                {
                    return false;
                }

                if (!ChangeSet_Id.TryParse(changeSetIdText, out var changeSetId))
                {
                    ErrorResponse = $"Invalid change set identification '{changeSetIdText}'!";
                    return false;
                }

                if (!DateTimeOffset.TryParse(withdrawnAt, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out var withdrawnAtTimestamp) ||
                    withdrawnAtTimestamp.ToISO8601() != withdrawnAt)
                {
                    ErrorResponse = $"The withdrawal timestamp '{withdrawnAt}' must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                Withdrawal = new RoamingNetworkChangeSetWithdrawal(
                                 roamingNetworkId,
                                 changeSetId,
                                 withdrawnAtTimestamp,
                                 algorithm,
                                 keyId,
                                 value
                             );

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Withdrawal     = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false)

        /// <summary>
        /// Return a JSON representation of this withdrawal.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public JObject ToJSON(Boolean Embedded = false)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,       JSONLDContext);

            json.Add(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString());
            json.Add(PropertyKeys.ChangeSetId,       ChangeSetId.     ToString());
            json.Add(PropertyKeys.WithdrawnAt,       WithdrawnAt.     ToISO8601());
            json.Add(PropertyKeys.Algorithm,         Algorithm);
            json.Add(PropertyKeys.KeyId,             KeyId);
            json.Add(PropertyKeys.Value,             Value);

            return json;

        }

        #endregion


        #region (private static) SigningBytes(RoamingNetworkId, ChangeSetId, WithdrawnAt, Algorithm, KeyId)

        /// <summary>
        /// The canonical JSON of the withdrawal with its JSON-LD context, without its value: what its signature signs.
        /// </summary>
        private static Byte[] SigningBytes(RoamingNetwork_Id  RoamingNetworkId,
                                           ChangeSet_Id       ChangeSetId,
                                           DateTimeOffset     WithdrawnAt,
                                           String             Algorithm,
                                           String             KeyId)

            => CanonicalJSON.ToUTF8Bytes(new JObject(
                                             new JProperty(PropertyKeys.Context,           JSONLDContext),
                                             new JProperty(PropertyKeys.Algorithm,         Algorithm),
                                             new JProperty(PropertyKeys.KeyId,             KeyId),
                                             new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString()),
                                             new JProperty(PropertyKeys.ChangeSetId,       ChangeSetId.     ToString()),
                                             new JProperty(PropertyKeys.WithdrawnAt,       WithdrawnAt.     ToISO8601())
                                         ));

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Withdrawal of the change set '{ChangeSetId}' with '{KeyId}' at {WithdrawnAt.ToISO8601()}";

        #endregion

        #region (private static) Required(Value, ParameterName)

        private static String Required(String  Value,
                                       String  ParameterName)

            => !String.IsNullOrWhiteSpace(Value)
                   ? Value
                   : throw new ArgumentException("Value must not be empty.", ParameterName);

        #endregion

    }

}
