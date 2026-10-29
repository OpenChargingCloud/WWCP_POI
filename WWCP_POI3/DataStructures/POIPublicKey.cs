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
using System.Globalization;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A public key of a party of the roaming network, as its static data names it:
    /// a COSE key without private parts, identified by its COSE key thumbprint [RFC 9679].
    /// </summary>
    /// <remarks>
    /// The identification is the SHA-256 thumbprint as 64 lowercase hexadecimal digits:
    /// everyone holding the key can recompute it, so a key can not claim the identification of another one.
    /// </remarks>
    public sealed class POIPublicKey : IEquatable<POIPublicKey>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/publicKey/v1";

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
            public const String  Id          = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context     = "@context";

            /// <summary>
            /// The property of its COSE key.
            /// </summary>
            public const String  Key         = "key";

            /// <summary>
            /// The property of since when it may be used.
            /// </summary>
            public const String  NotBefore   = "notBefore";

            /// <summary>
            /// The property of until when it may be used.
            /// </summary>
            public const String  NotAfter    = "notAfter";

            /// <summary>
            /// The property of who holds its private key, e.g. one company of several holding the keys of a roaming
            /// network operator together (SPECIFICATION.md, section 4.1).
            /// </summary>
            public const String  Holder      = "holder";

            /// <summary>
            /// The property of what a signing key may sign, unless everything its party owns (POIKeyUsages).
            /// </summary>
            public const String  Usages      = "usages";

        }

        #endregion


        /// <summary>
        /// The identification: the SHA-256 COSE key thumbprint, as 64 lowercase hexadecimal digits.
        /// </summary>
        public String           Id           { get; }

        /// <summary>
        /// The COSE key, without private parts.
        /// </summary>
        public COSEKey          Key          { get; }

        /// <summary>
        /// Since when it may be used, unless always.
        /// </summary>
        public DateTimeOffset?  NotBefore    { get; }

        /// <summary>
        /// Until when it may be used, unless always.
        /// </summary>
        public DateTimeOffset?  NotAfter     { get; }

        /// <summary>
        /// Who holds its private key, if named: a quorum of a roaming network operator counts the holders of its keys, not
        /// its keys, so that a holder renewing its key has one voice (SPECIFICATION.md, section 4.1). Not part of its
        /// identification.
        /// </summary>
        public String?          Holder       { get; }

        /// <summary>
        /// Who holds its private key as a quorum counts it: its holder, or the one holder of all keys naming none.
        /// </summary>
        public String           HolderOrUnnamed
            => Holder ?? "";

        /// <summary>
        /// What a signing key may sign, in their ordinal order (POIKeyUsages): everything its party owns unless it names
        /// some. Not part of its identification.
        /// </summary>
        public ImmutableArray<String>  Usages    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new public key of a party.
        /// </summary>
        /// <param name="Key">The COSE key; private parts are dropped.</param>
        /// <param name="NotBefore">Since when it may be used, unless always.</param>
        /// <param name="NotAfter">Until when it may be used, unless always.</param>
        /// <param name="Holder">Who holds its private key, if named.</param>
        /// <param name="Usages">What a signing key may sign, unless everything its party owns (POIKeyUsages).</param>
        public POIPublicKey(COSEKey               Key,
                            DateTimeOffset?       NotBefore   = null,
                            DateTimeOffset?       NotAfter    = null,
                            String?               Holder      = null,
                            IEnumerable<String>?  Usages      = null)
        {

            ArgumentNullException.ThrowIfNull(Key);

            if (NotBefore.HasValue && NotAfter.HasValue && NotBefore.Value > NotAfter.Value)
                throw new ArgumentException("A public key must not expire before it becomes valid!");

            if (Holder is not null && (Holder.Length == 0 || Holder.Trim() != Holder))
                throw new ArgumentException("The holder of a public key must be a nonempty text without leading or trailing white space!");

            this.Key        = Key.IsPrivate ? Key.ToPublicCOSEKey() : Key;
            this.Id         = Convert.ToHexStringLower(this.Key.Thumbprint());
            this.NotBefore  = NotBefore?.ToUniversalTime();
            this.NotAfter   = NotAfter?. ToUniversalTime();
            this.Holder     = Holder;
            this.Usages     = [ .. (Usages ?? []).Order(StringComparer.Ordinal) ];

            if (this.Usages.FirstOrDefault(usage => !POIKeyUsages.All.Contains(usage)) is String unknownUsage)
                throw new ArgumentException($"The usage '{unknownUsage}' of a public key is unknown: it is one of {String.Join(", ", POIKeyUsages.All.Order(StringComparer.Ordinal))}!");

            if (this.Usages.Distinct(StringComparer.Ordinal).Count() != this.Usages.Length)
                throw new ArgumentException("A public key names each of its usages once!");

        }

        #endregion


        #region (static) From(Key, NotBefore = null, NotAfter = null, Holder = null, Usages = null)

        /// <summary>
        /// The public key of the given asymmetric key, e.g. of a private signing key.
        /// </summary>
        public static POIPublicKey From(AsymmetricKeyParameter  Key,
                                        DateTimeOffset?         NotBefore   = null,
                                        DateTimeOffset?         NotAfter    = null,
                                        String?                 Holder      = null,
                                        IEnumerable<String>?    Usages      = null)

            => new (COSEKey.From(Key), NotBefore, NotAfter, Holder, Usages);

        #endregion

        #region MaySign(Usage)

        /// <summary>
        /// Whether this signing key may sign what needs the given usage (POIKeyUsages): it names none, or that one.
        /// </summary>
        public Boolean MaySign(String Usage)

            => Usages.IsEmpty || Usages.Contains(Usage);

        #endregion

        #region IsValidAt(Timestamp)

        /// <summary>
        /// Whether it may be used at the given time.
        /// </summary>
        public Boolean IsValidAt(DateTimeOffset Timestamp)

            => (!NotBefore.HasValue || Timestamp >= NotBefore.Value) &&
               (!NotAfter. HasValue || Timestamp <= NotAfter. Value);

        #endregion


        #region ToJSON(Embedded = false, CustomPOIPublicKeySerializer = null)

        /// <summary>
        /// Return the JSON representation of this public key: its identification,
        /// its COSE key as Base64 of its deterministic CBOR, its validity, its holder and the usages of a signing key.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomPOIPublicKeySerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                         Embedded                       = false,
                              CustomJObjectSerializerDelegate<POIPublicKey>?  CustomPOIPublicKeySerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id,  Id)
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,    JSONLDContext);

            json.Add(PropertyKeys.Key,            Convert.ToBase64String(Key.ToByteArray(CBORWriterOptions.Canonical)));

            if (NotBefore.HasValue)
                json.Add(PropertyKeys.NotBefore,  NotBefore.Value.ToISO8601());

            if (NotAfter.HasValue)
                json.Add(PropertyKeys.NotAfter,   NotAfter. Value.ToISO8601());

            if (Holder is not null)
                json.Add(PropertyKeys.Holder,     Holder);

            if (!Usages.IsEmpty)
                json.Add(PropertyKeys.Usages,     new JArray(Usages));

            return CustomPOIPublicKeySerializer is not null
                       ? CustomPOIPublicKeySerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out PublicKey, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a public key.
        /// Its identification must be the thumbprint of its key, and the key must not be private.
        /// </summary>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out POIPublicKey?  PublicKey,
                                       [NotNullWhen(false)] out String?        ErrorResponse)
        {

            PublicKey = null;

            try
            {

                if (!POIJSON.TryParseString(JSON, PropertyKeys.Id,  "public key identification", out var id,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.Key, "public key",                out var key, out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the public key '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (id is null || key is null)
                {
                    ErrorResponse = "A public key needs its identification and its key!";
                    return false;
                }

                var bytes = Convert.FromBase64String(key);

                if (Convert.ToBase64String(bytes) != key)
                {
                    ErrorResponse = "A public key must be given as canonical Base64!";
                    return false;
                }

                if (!COSEKey.TryParse(bytes, out var coseKey, out var coseError))
                {
                    ErrorResponse = "Invalid COSE key: " + coseError;
                    return false;
                }

                // The key as the content holds it (SPECIFICATION.md 2 and 4.1): the deterministic CBOR of its COSE key, not
                // another CBOR of the same key, which would be written again as another text than the one hashed.
                if (!coseKey.ToByteArray(CBORWriterOptions.Canonical).AsSpan().SequenceEqual(bytes))
                {
                    ErrorResponse = $"Invalid COSE key: Invalid deterministic CBOR: the key of the public key '{id}' is not written as the deterministic CBOR of its COSE key!";
                    return false;
                }

                if (coseKey.IsPrivate)
                {
                    ErrorResponse = "A public key must not contain private parts!";
                    return false;
                }

                if (!TryParseTimestamp(JSON, PropertyKeys.NotBefore, id, out var notBefore, out ErrorResponse) ||
                    !TryParseTimestamp(JSON, PropertyKeys.NotAfter,  id, out var notAfter,  out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseString(JSON, PropertyKeys.Holder, "holder of a public key", out var holder, out ErrorResponse))
                    return false;

                if (holder is not null && (holder.Length == 0 || holder.Trim() != holder))
                {
                    ErrorResponse = $"The holder of the public key '{id}' must be a nonempty text without leading or trailing white space!";
                    return false;
                }

                var usages = new List<String>();

                if (JSON[PropertyKeys.Usages] is JToken usagesJSON)
                {

                    if (usagesJSON is not JArray { Count: > 0 } usagesArray || usagesArray.Any(usage => usage.Type != JTokenType.String))
                    {
                        ErrorResponse = $"The usages of the public key '{id}' must be a nonempty JSON array of texts!";
                        return false;
                    }

                    usages.AddRange(usagesArray.Select(usage => usage.Value<String>()!));

                    if (usages.FirstOrDefault(usage => !POIKeyUsages.All.Contains(usage)) is String unknownUsage)
                    {
                        ErrorResponse = $"The usage '{unknownUsage}' of the public key '{id}' is unknown: it is one of {String.Join(", ", POIKeyUsages.All.Order(StringComparer.Ordinal))}!";
                        return false;
                    }

                    if (usages.Distinct(StringComparer.Ordinal).Count() != usages.Count)
                    {
                        ErrorResponse = $"The public key '{id}' names each of its usages once!";
                        return false;
                    }

                    if (!usages.SequenceEqual(usages.Order(StringComparer.Ordinal)))
                    {
                        ErrorResponse = $"The usages of the public key '{id}' are written in their ordinal order!";
                        return false;
                    }

                }

                var publicKey = new POIPublicKey(coseKey, notBefore, notAfter, holder, usages);

                if (publicKey.Id != id)
                {
                    ErrorResponse = $"The identification '{id}' of a public key is not its thumbprint '{publicKey.Id}'!";
                    return false;
                }

                PublicKey      = publicKey;
                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                PublicKey      = null;
                ErrorResponse  = "Invalid public key: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (private static) TryParseTimestamp(JSON, PropertyName, Id, out Timestamp, out ErrorResponse)

        /// <summary>
        /// Try to parse an optional timestamp of a public key as the content holds it (SPECIFICATION.md 2, Written forms):
        /// a text, yyyy-MM-ddTHH:mm:ss.fffZ. Another form of the same instant, or null for none, is refused: written
        /// again, it would be another text than the one hashed.
        /// </summary>
        /// <param name="JSON">The JSON representation of the public key.</param>
        /// <param name="PropertyName">The name of the timestamp.</param>
        /// <param name="Id">The identification of the public key, for the error.</param>
        /// <param name="Timestamp">The parsed timestamp, unless none is given.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        private static Boolean TryParseTimestamp(JObject                           JSON,
                                                 String                            PropertyName,
                                                 String                            Id,
                                                 out DateTimeOffset?               Timestamp,
                                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Timestamp      = null;
            ErrorResponse  = null;

            if (JSON[PropertyName] is not JToken value)
                return true;

            if (value.Type != JTokenType.String)
            {
                ErrorResponse = $"The {PropertyName} of the key '{Id}' must be a text!";
                return false;
            }

            var text = value.Value<String>()!;

            if (!DateTimeOffset.TryParseExact(text,
                                              "yyyy-MM-dd'T'HH:mm:ss.fff'Z'",
                                              CultureInfo.InvariantCulture,
                                              DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                                              out var timestamp) ||
                timestamp.ToISO8601() != text)
            {
                ErrorResponse = $"The {PropertyName} of the key '{Id}' must be written as yyyy-MM-ddTHH:mm:ss.fffZ, not '{text}'!";
                return false;
            }

            Timestamp = timestamp;
            return true;

        }

        #endregion


        #region (internal static) Ordered(Keys, Description)

        /// <summary>
        /// The given keys in the order of their identifications, without one given twice.
        /// </summary>
        internal static System.Collections.Immutable.ImmutableArray<POIPublicKey> Ordered(IEnumerable<POIPublicKey>?  Keys,
                                                                                           String                     Description)

            => [.. POIJSON.Distinct(Keys, key => key.Id, Description).
                           OrderBy (key => key.Id, StringComparer.Ordinal)];

        #endregion


        #region IEquatable

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is POIPublicKey publicKey &&
               Equals(publicKey);

        /// <summary>
        /// Compares two public keys by their JSON representations.
        /// </summary>
        public Boolean Equals(POIPublicKey? PublicKey)

            => PublicKey is not null &&
               POIJSON.SameValue(ToJSON(Embedded: true), PublicKey.ToJSON(Embedded: true));

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id;

        #endregion

    }

}
