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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.TeleTrust;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A public key an energy meter signs its measurements with: its DER encoded SubjectPublicKeyInfo [RFC 5280],
    /// identified by its SHA-256, and optionally the certificate chain of the key.
    /// </summary>
    /// <remarks>
    /// Not a COSE key, as the keys of the parties are: energy meters sign with curves COSE does not know, e.g.
    /// secp192r1 or secp224k1. A SubjectPublicKeyInfo names its algorithm and curve itself, and a transparency
    /// software, or an app, shows it as the meter shows it, e.g. as hexadecimal coordinates.
    /// </remarks>
    public sealed class EnergyMeterPublicKey : IEquatable<EnergyMeterPublicKey>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/energyMeterPublicKey/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                 = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context            = "@context";

            /// <summary>
            /// The property of its key.
            /// </summary>
            public const String  Key                = "key";

            /// <summary>
            /// The property of its certificate chain.
            /// </summary>
            public const String  CertificateChain   = "certificateChain";

        }

        #endregion


        /// <summary>
        /// The identification: the SHA-256 of the DER encoded SubjectPublicKeyInfo, as 64 lowercase hexadecimal digits.
        /// </summary>
        public String                               Id                  { get; }

        /// <summary>
        /// The DER encoded SubjectPublicKeyInfo of the key.
        /// </summary>
        public ImmutableArray<Byte>                 Key                 { get; }

        /// <summary>
        /// The algorithm of the key, for people: the name of its curve, e.g. "secp256r1" or "brainpoolP256r1",
        /// "Ed25519", "Ed448" or "RSA", else the object identifier of its algorithm.
        /// </summary>
        public String                               Algorithm           { get; }

        /// <summary>
        /// The certificate chain of the key, each certificate DER encoded: first the one of the key,
        /// then each one followed by the one that issued it.
        /// </summary>
        public ImmutableArray<ImmutableArray<Byte>> CertificateChain    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new public key of an energy meter.
        /// </summary>
        /// <param name="Key">The DER encoded SubjectPublicKeyInfo of the key.</param>
        /// <param name="CertificateChain">The optional certificate chain of the key, each certificate DER encoded, the one of the key first.</param>
        public EnergyMeterPublicKey(IEnumerable<Byte>                 Key,
                                    IEnumerable<IEnumerable<Byte>>?   CertificateChain   = null)
        {

            var key = Key?.ToArray() ?? throw new ArgumentNullException(nameof(Key), "The public key of an energy meter must not be null!");

            if (!TryCheck(key,
                          [.. (CertificateChain ?? []).Select(certificate => certificate?.ToArray() ?? throw new ArgumentException("A certificate of the public key of an energy meter must not be null!"))],
                          out var algorithm,
                          out var errorResponse))
            {
                throw new ArgumentException(errorResponse);
            }

            this.Key               = [.. key];
            this.Id                = Convert.ToHexStringLower(SHA256.HashData(key));
            this.Algorithm         = algorithm;
            this.CertificateChain  = [.. (CertificateChain ?? []).Select(certificate => ImmutableArray.CreateRange(certificate))];

        }

        #endregion


        #region (private static) TryCheck(Key, CertificateChain, out Algorithm, out ErrorResponse)

        /// <summary>
        /// Whether the given key is a DER encoded SubjectPublicKeyInfo of a valid public key, and the given
        /// certificates are DER encoded certificates, the first of the key, each one issued by the next one.
        /// </summary>
        private static Boolean TryCheck(Byte[]                            Key,
                                        Byte[][]                          CertificateChain,
                                        [NotNullWhen(true)]  out String?  Algorithm,
                                        [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Algorithm = null;

            SubjectPublicKeyInfo subjectPublicKeyInfo;

            try
            {

                subjectPublicKeyInfo = SubjectPublicKeyInfo.GetInstance(Asn1Object.FromByteArray(Key));

                // Only DER, so that a key has one representation, one identification, and one ETag of its meter.
                if (!subjectPublicKeyInfo.GetDerEncoded().AsSpan().SequenceEqual(Key))
                {
                    ErrorResponse = "The public key of an energy meter must be DER encoded!";
                    return false;
                }

                // A valid key of a known algorithm, e.g. a point on its curve.
                PublicKeyFactory.CreateKey(subjectPublicKeyInfo);

            }
            catch (Exception e)
            {
                ErrorResponse = "The public key of an energy meter must be a SubjectPublicKeyInfo: " + e.Message;
                return false;
            }

            var certificates = new List<X509Certificate>();

            for (var i = 0; i < CertificateChain.Length; i++)
            {

                try
                {

                    var certificate = new X509CertificateParser().ReadCertificate(CertificateChain[i]);

                    if (certificate is null || !certificate.GetEncoded().AsSpan().SequenceEqual(CertificateChain[i]))
                    {
                        ErrorResponse = $"The certificate {i + 1} of the public key of an energy meter must be DER encoded!";
                        return false;
                    }

                    certificates.Add(certificate);

                }
                catch (Exception e)
                {
                    ErrorResponse = $"The certificate {i + 1} of the public key of an energy meter is invalid: " + e.Message;
                    return false;
                }

            }

            if (certificates.Count > 0 &&
                !certificates[0].SubjectPublicKeyInfo.GetDerEncoded().AsSpan().SequenceEqual(Key))
            {
                ErrorResponse = "The first certificate of the public key of an energy meter must be the one of the key!";
                return false;
            }

            for (var i = 0; i < certificates.Count - 1; i++)
            {
                try
                {
                    certificates[i].Verify(certificates[i + 1].GetPublicKey());
                }
                catch
                {
                    ErrorResponse = $"The certificate {i + 1} of the public key of an energy meter is not issued by the certificate {i + 2}!";
                    return false;
                }
            }

            Algorithm      = AlgorithmOf(subjectPublicKeyInfo.Algorithm);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (private static) AlgorithmOf(AlgorithmIdentifier)

        private static String AlgorithmOf(AlgorithmIdentifier AlgorithmIdentifier)
        {

            var oid = AlgorithmIdentifier.Algorithm.Id;

            if (oid == X9ObjectIdentifiers.IdECPublicKey.Id &&
                AlgorithmIdentifier.Parameters is DerObjectIdentifier curve)
            {
                // The names of SEC 2 and of the Brainpool curves, as meters and transparency software name them,
                // e.g. secp256r1 rather than prime256v1.
                return SecNamedCurves.     GetName(curve) ??
                       TeleTrusTNamedCurves.GetName(curve) ??
                       ECNamedCurveTable.  GetName(curve) ??
                       curve.Id;
            }

            return oid switch {
                       "1.3.101.112"           => "Ed25519",
                       "1.3.101.113"           => "Ed448",
                       "1.2.840.113549.1.1.1"  => "RSA",
                       _                       => oid
                   };

        }

        #endregion


        #region ToJSON(Embedded = false, CustomEnergyMeterPublicKeySerializer = null)

        /// <summary>
        /// Return the JSON representation of this public key: its identification, its key as Base64 of its
        /// DER encoded SubjectPublicKeyInfo, and its certificate chain as Base64 of each DER encoded certificate.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEnergyMeterPublicKeySerializer">A delegate to serialize custom public key JSON objects.</param>
        public JObject ToJSON(Boolean                                                 Embedded                               = false,
                              CustomJObjectSerializerDelegate<EnergyMeterPublicKey>?  CustomEnergyMeterPublicKeySerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id,  Id)
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,           JSONLDContext);

            json.Add(PropertyKeys.Key,                   Convert.ToBase64String(Key.AsSpan()));

            if (CertificateChain.Length > 0)
                json.Add(PropertyKeys.CertificateChain,  new JArray(CertificateChain.Select(certificate => Convert.ToBase64String(certificate.AsSpan()))));

            return CustomEnergyMeterPublicKeySerializer is not null
                       ? CustomEnergyMeterPublicKeySerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out PublicKey, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a public key of an energy meter.
        /// Its identification must be the SHA-256 of its key, the key and the certificates canonical Base64 of DER.
        /// </summary>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out EnergyMeterPublicKey?  PublicKey,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            PublicKey = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the public key of an energy meter must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseString (JSON, PropertyKeys.Id,                "public key identification",  out var id,                out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, PropertyKeys.Key,               "public key",                 out var key,               out ErrorResponse) ||
                    !POIJSON.TryParseStrings(JSON, PropertyKeys.CertificateChain,  "certificate chain",          out var certificateChain,  out ErrorResponse))
                {
                    return false;
                }

                if (id is null || key is null)
                {
                    ErrorResponse = "A public key of an energy meter needs its identification and its key!";
                    return false;
                }

                if (!TryFromBase64(key, "public key", out var keyBytes, out ErrorResponse))
                    return false;

                var certificates = new List<Byte[]>();

                foreach (var certificate in certificateChain)
                {

                    if (!TryFromBase64(certificate, "certificate", out var certificateBytes, out ErrorResponse))
                        return false;

                    certificates.Add(certificateBytes);

                }

                EnergyMeterPublicKey publicKey;

                try
                {
                    publicKey = new EnergyMeterPublicKey(keyBytes, certificates);
                }
                catch (ArgumentException e)
                {
                    ErrorResponse = e.Message;
                    return false;
                }

                if (publicKey.Id != id)
                {
                    ErrorResponse = $"The identification '{id}' of a public key of an energy meter is not the SHA-256 '{publicKey.Id}' of its key!";
                    return false;
                }

                PublicKey      = publicKey;
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                PublicKey      = null;
                ErrorResponse  = "Invalid public key of an energy meter: " + e.Message;
                return false;
            }

        }

        private static Boolean TryFromBase64(String                            Text,
                                             String                            Description,
                                             [NotNullWhen(true)]  out Byte[]?  Bytes,
                                             [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Bytes = null;

            try
            {

                var bytes = Convert.FromBase64String(Text);

                if (Convert.ToBase64String(bytes) != Text)
                {
                    ErrorResponse = $"The {Description} of an energy meter must be given as canonical Base64!";
                    return false;
                }

                Bytes          = bytes;
                ErrorResponse  = null;
                return true;

            }
            catch (FormatException)
            {
                ErrorResponse = $"The {Description} of an energy meter must be given as Base64!";
                return false;
            }

        }

        #endregion


        #region (internal static) Ordered(Keys, Description)

        /// <summary>
        /// The given keys in the order of their identifications, without one given twice.
        /// </summary>
        internal static ImmutableArray<EnergyMeterPublicKey> Ordered(IEnumerable<EnergyMeterPublicKey>?  Keys,
                                                                     String                              Description)

            => [.. POIJSON.Distinct(Keys, key => key.Id, Description).
                           OrderBy (key => key.Id, StringComparer.Ordinal)];

        #endregion


        #region IEquatable

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterPublicKey publicKey &&
               Equals(publicKey);

        /// <summary>
        /// Compares two public keys by their JSON representations: the key and its certificate chain.
        /// </summary>
        public Boolean Equals(EnergyMeterPublicKey? PublicKey)

            => PublicKey is not null &&
               POIJSON.SameValue(ToJSON(Embedded: true), PublicKey.ToJSON(Embedded: true));

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => $"{Algorithm} {Id}";

        #endregion

    }

}
