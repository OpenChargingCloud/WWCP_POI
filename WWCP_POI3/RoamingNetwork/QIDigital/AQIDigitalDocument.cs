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
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A QI-Digital document (the German initiative for a digital quality infrastructure): a digital calibration
    /// certificate, a digital declaration of conformity or a digital certificate of conformity, signed by its issuer with
    /// one of its signing keys of the roaming network.
    /// </summary>
    /// <remarks>
    /// A proof of concept: the documents follow the structure of their XML schemas (PTB DCC, NoBoMet D-CoC) and of the
    /// EU declaration of conformity (Decision 768/2008/EC, Annex III) where the roaming network needs them, in the JSON
    /// of the roaming network, and are not yet part of it: a party hands them out, and anybody checks them against the
    /// roaming network, its parties and keys, its models, charging stations and energy meters.
    /// </remarks>
    /// <typeparam name="TDocument">The type of the document.</typeparam>
    public abstract class AQIDigitalDocument<TDocument>

        where TDocument : AQIDigitalDocument<TDocument>

    {

        #region Data

        private String? digest;

        #endregion

        #region Properties

        /// <summary>
        /// Its signature by its issuer, if it is signed.
        /// </summary>
        public QIDigitalSignature?  Signature        { get; }

        /// <summary>
        /// Its JSON-LD context, which names the kind of document and its version: part of its signature input.
        /// </summary>
        public abstract String      Context          { get; }

        /// <summary>
        /// The usage a signing key of its issuer must have to sign it (POIKeyUsages).
        /// </summary>
        public abstract String      Usage            { get; }

        /// <summary>
        /// Its issuer in words, e.g. "the conformity assessment body 'PTB'".
        /// </summary>
        public abstract String      IssuerText       { get; }

        /// <summary>
        /// Itself in words, e.g. "the digital calibration certificate 'DCC-1'".
        /// </summary>
        public abstract String      DocumentText     { get; }

        /// <summary>
        /// The digest of this document: the SHA-256 of its signature input, its signature value left out, as 64 lowercase
        /// hexadecimal digits; of its content alone, when it is not signed. Another document names it by it.
        /// </summary>
        public String               Digest
            => digest ??= Convert.ToHexStringLower(SHA256.HashData(CanonicalJSON.ToUTF8Bytes(SignatureInput())));

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a QI-Digital document.
        /// </summary>
        /// <param name="Signature">Its signature by its issuer, if it is signed.</param>
        protected AQIDigitalDocument(QIDigitalSignature? Signature)
        {
            this.Signature = Signature;
        }

        #endregion


        #region (protected) Content()

        /// <summary>
        /// Its content: everything but its JSON-LD context and its signature.
        /// </summary>
        protected abstract JObject Content();

        #endregion

        #region (protected) WithSignature(Signature)

        /// <summary>
        /// The same document, with the given signature.
        /// </summary>
        protected abstract TDocument WithSignature(QIDigitalSignature Signature);

        #endregion


        #region Sign(PrivateKey, KeyId, Algorithm, SignedAt = null, Deterministic = true)

        /// <summary>
        /// Sign this document with the given signing key of its issuer: the same document, signed, replacing a signature
        /// it had.
        /// </summary>
        /// <param name="PrivateKey">The private signing key.</param>
        /// <param name="KeyId">The @id of the signing key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="SignedAt">When it is signed, now otherwise: its date of issue.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public TDocument Sign(AsymmetricKeyParameter  PrivateKey,
                              String                  KeyId,
                              COSEAlgorithm           Algorithm,
                              DateTimeOffset?         SignedAt        = null,
                              Boolean                 Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(PrivateKey);

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            // Without a value yet, only to get the signature input exactly as it will be written.
            var unsigned  = WithSignature(new QIDigitalSignature(SignedAt ?? Timestamp.Now, Algorithm.Name, KeyId, "-"));
            var value     = Algorithm.Sign(unsigned.GetSigningBytes(), PrivateKey, Deterministic: Deterministic);

            return WithSignature(new QIDigitalSignature(unsigned.Signature!.SignedAt,
                                                        unsigned.Signature. Algorithm,
                                                        unsigned.Signature. KeyId,
                                                        Convert.ToBase64String(value)));

        }

        #endregion

        #region GetSigningBytes()

        /// <summary>
        /// The signature input of this document, as Styx canonical JSON: its JSON-LD context, its content and its
        /// signature without its value.
        /// </summary>
        public Byte[] GetSigningBytes()
        {

            if (Signature is null)
                throw new InvalidOperationException($"{DocumentText} is not signed.");

            if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || !algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.");

            return CanonicalJSON.ToUTF8Bytes(SignatureInput());

        }

        #endregion

        #region VerifySignature(PublicKey, ExpectedKeyId, out ErrorResponse)

        /// <summary>
        /// Verify the signature of this document with the given public key and its expected key identification.
        /// Unsigned documents, unknown algorithms, noncanonical Base64, mismatched keys and malformed inputs return false.
        /// </summary>
        /// <param name="PublicKey">The public key.</param>
        /// <param name="ExpectedKeyId">The key identification of the public key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(AsymmetricKeyParameter            PublicKey,
                                       String                            ExpectedKeyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                ArgumentNullException.ThrowIfNull(PublicKey);

                if (Signature is null)
                    throw new ArgumentException($"{DocumentText} is not signed.");

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Verification requires a public asymmetric key.", nameof(PublicKey));

                if (Signature.KeyId != ExpectedKeyId)
                    throw new ArgumentException($"The key ID of {DocumentText} does not match the expected key ID.");

                if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || algorithm.Name != Signature.Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                var value = Convert.FromBase64String(Signature.Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Signature.Value)
                    throw new ArgumentException("The value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(), value, PublicKey, out ErrorResponse);

            }
            catch (Exception exception)
            {
                ErrorResponse = exception.Message;
                return false;
            }

        }

        #endregion

        #region TryVerify(SigningKeys, out ErrorResponse, At = null)

        /// <summary>
        /// Verify the signature of this document with the one of the given signing keys of its issuer it names, which must
        /// be valid when it was signed and may sign its usage (POIKeyUsages); and, when a time is given, valid then as well.
        /// </summary>
        /// <param name="SigningKeys">The signing keys of the issuer of the document.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When the key must be valid as well, if anything.</param>
        public Boolean TryVerify(IEnumerable<POIPublicKey>         SigningKeys,
                                 [NotNullWhen(false)] out String?  ErrorResponse,
                                 DateTimeOffset?                   At   = null)
        {

            if (Signature is null)
            {
                ErrorResponse = $"{Capitalized(DocumentText)} is not signed!";
                return false;
            }

            if (SigningKeys.FirstOrDefault(signingKey => signingKey.Id == Signature.KeyId) is not POIPublicKey signingKey)
            {
                ErrorResponse = $"The key '{Signature.KeyId}' of {DocumentText} is no signing key of {IssuerText}!";
                return false;
            }

            if (!signingKey.IsValidAt(Signature.SignedAt))
            {
                ErrorResponse = $"The signing key '{Signature.KeyId}' of {IssuerText} is not valid at {Signature.SignedAt.ToISO8601()}, when {DocumentText} was signed!";
                return false;
            }

            if (At is DateTimeOffset at && !signingKey.IsValidAt(at))
            {
                ErrorResponse = $"The signing key '{Signature.KeyId}' of {IssuerText} is not valid at {at.ToISO8601()}!";
                return false;
            }

            if (!signingKey.MaySign(Usage))
            {
                ErrorResponse = $"The signing key '{Signature.KeyId}' of {IssuerText} may not sign '{Usage}', only {String.Join(", ", signingKey.Usages.Select(usage => $"'{usage}'"))}!";
                return false;
            }

            if (!VerifySignature(signingKey.Key.ToPublicKey(), signingKey.Id, out var signatureError))
            {
                ErrorResponse = $"The signature of {DocumentText} is invalid: {signatureError}";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region ToJSON(Embedded = false)

        /// <summary>
        /// Return the JSON representation of this document: its JSON-LD context unless it is embedded, its content and its
        /// signature, if it is signed.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public JObject ToJSON(Boolean Embedded = false)
        {

            var json = Content();

            if (!Embedded)
                json.AddFirst(new JProperty(QIDigitalSignature.PropertyKeys.Context, Context));

            if (Signature is not null)
                json.Add(QIDigitalSignature.PropertyKeys.Signature, Signature.ToJSON());

            return json;

        }

        #endregion

        #region (protected static) TryParseSignature(JSON, out Signature, out ErrorResponse)

        /// <summary>
        /// The optional signature of a document.
        /// </summary>
        protected static Boolean TryParseSignature(JObject                           JSON,
                                                   out QIDigitalSignature?           Signature,
                                                   [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Signature = null;

            if (JSON[QIDigitalSignature.PropertyKeys.Signature] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JObject json)
            {
                ErrorResponse = "The signature must be a JSON object!";
                return false;
            }

            if (!QIDigitalSignature.TryParse(json, out Signature, out var signatureError))
            {
                ErrorResponse = "Invalid signature: " + signatureError;
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (protected static) Capitalized(Text)

        /// <summary>
        /// The given text, beginning with a capital letter.
        /// </summary>
        protected static String Capitalized(String Text)

            => Text.Length > 0
                   ? Char.ToUpperInvariant(Text[0]) + Text[1..]
                   : Text;

        #endregion

        #region (private) SignatureInput()

        /// <summary>
        /// The signature input of this document: its JSON-LD context, its content and its signature without its value;
        /// what it signs and what its digest covers.
        /// </summary>
        private JObject SignatureInput()
        {

            var json = Content();

            json.AddFirst(new JProperty(QIDigitalSignature.PropertyKeys.Context, Context));

            if (Signature is not null)
                json.Add(QIDigitalSignature.PropertyKeys.Signature, Signature.ToJSON(WithValue: false));

            return json;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => Capitalized(DocumentText) + (Signature is not null ? $", signed {Signature.SignedAt.ToISO8601()}" : ", not signed");

        #endregion

    }

}
