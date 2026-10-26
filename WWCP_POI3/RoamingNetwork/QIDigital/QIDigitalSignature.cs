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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The signature of a QI-Digital document by its issuer, with one of the signing keys of the issuer: in place of the
    /// XML signature (XAdES) of a PTB digital calibration certificate, a COSE algorithm over the Styx canonical JSON of
    /// the document, as everything else in the roaming network is signed.
    /// </summary>
    /// <remarks>
    /// Its issuer is named by the document itself, e.g. its calibration laboratory or its manufacturer; the signature
    /// names only the key, when it was signed and how.
    /// </remarks>
    public sealed class QIDigitalSignature
    {

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the JSON-LD context of a document, written unless it is embedded.
            /// </summary>
            public const String  Context    = "@context";

            /// <summary>
            /// The property of the signature of a document.
            /// </summary>
            public const String  Signature  = "signature";

            /// <summary>
            /// The property of its signing timestamp.
            /// </summary>
            public const String  SignedAt   = "signedAt";

            /// <summary>
            /// The property of its signature algorithm.
            /// </summary>
            public const String  Algorithm  = "algorithm";

            /// <summary>
            /// The property of the identification of its key.
            /// </summary>
            public const String  KeyId      = "keyId";

            /// <summary>
            /// The property of its signature value.
            /// </summary>
            public const String  Value      = "value";

        }

        #endregion


        /// <summary>
        /// When it was signed, signed as text: yyyy-MM-ddTHH:mm:ss.fffZ. It is the date of issue of the document.
        /// </summary>
        public DateTimeOffset  SignedAt     { get; }

        /// <summary>
        /// The COSE name of the signature algorithm, e.g. ES256.
        /// </summary>
        public String          Algorithm    { get; }

        /// <summary>
        /// The identification of the key: the @id of a signing key of the issuer of the document.
        /// </summary>
        public String          KeyId        { get; }

        /// <summary>
        /// The signature value, canonical Base64.
        /// </summary>
        public String          Value        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the signature of a QI-Digital document of its parts; a document signs itself by its Sign(…).
        /// </summary>
        /// <param name="SignedAt">When it was signed.</param>
        /// <param name="Algorithm">The COSE name of the signature algorithm.</param>
        /// <param name="KeyId">The @id of the signing key.</param>
        /// <param name="Value">The signature value, canonical Base64.</param>
        public QIDigitalSignature(DateTimeOffset  SignedAt,
                                  String          Algorithm,
                                  String          KeyId,
                                  String          Value)
        {

            if (String.IsNullOrWhiteSpace(Algorithm) || String.IsNullOrWhiteSpace(KeyId) || String.IsNullOrEmpty(Value))
                throw new ArgumentException("A signature of a QI-Digital document requires an algorithm, a key identification and a value.");

            this.SignedAt   = QIDigitalJSON.Timestamp(SignedAt);
            this.Algorithm  = Algorithm;
            this.KeyId      = KeyId;
            this.Value      = Value;

        }

        #endregion


        #region (static) TryParse(JSON, out Signature, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the signature of a QI-Digital document.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Signature">The parsed signature.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out QIDigitalSignature?  Signature,
                                       [NotNullWhen(false)] out String?              ErrorResponse)
        {

            Signature = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a signature", out ErrorResponse,
                                           PropertyKeys.SignedAt, PropertyKeys.Algorithm, PropertyKeys.KeyId, PropertyKeys.Value))
                {
                    return false;
                }

                if (!QIDigitalJSON.TryParseTimestamp(JSON, PropertyKeys.SignedAt, "signing timestamp", out var signedAt, out ErrorResponse))
                    return false;

                if (!signedAt.HasValue                                                                                 ||
                    QIDigitalJSON.Text(JSON, PropertyKeys.Algorithm) is not String algorithm || algorithm.Length == 0 ||
                    QIDigitalJSON.Text(JSON, PropertyKeys.KeyId)     is not String keyId     || keyId.    Length == 0 ||
                    QIDigitalJSON.Text(JSON, PropertyKeys.Value)     is not String value     || value.    Length == 0)
                {
                    ErrorResponse = "A signature requires a signing timestamp, an algorithm, a key identification and a value!";
                    return false;
                }

                Signature      = new QIDigitalSignature(signedAt.Value, algorithm, keyId, value);
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                Signature      = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(WithValue = true)

        /// <summary>
        /// Return the JSON representation of this signature: without its value, it is part of the signature input.
        /// </summary>
        /// <param name="WithValue">Whether to write its value.</param>
        public JObject ToJSON(Boolean WithValue = true)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.SignedAt,   SignedAt.ToISO8601()),
                           new JProperty(PropertyKeys.Algorithm,  Algorithm),
                           new JProperty(PropertyKeys.KeyId,      KeyId)
                       );

            if (WithValue)
                json.Add(PropertyKeys.Value, Value);

            return json;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Algorithm} signature by '{KeyId}' at {SignedAt.ToISO8601()}";

        #endregion

    }

}
