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

using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What an app trusts of a roaming network (SPECIFICATION.md, section 8.6): where its proofs come from, the @ids of the
    /// identity keys of its operator, their SHA-256 thumbprints, and how many holders of them sign, its minConsensus; with
    /// its name and its description, to show. For a QR code, e.g. on the website or in the documents of the roaming network
    /// operator, as the text WWCPPOI1: and the Base45 of its deterministic CBOR [RFC 9285], or, without name and description,
    /// as one URI:
    /// https://hub.example/poi/RNs/rn1?identityKeys=a1…,c3…&amp;minConsensus=2. Only the thumbprints and the minConsensus are
    /// trusted; the keys themselves come with every proof, in the key statement and the operator it gives.
    /// </summary>
    public sealed class RoamingNetworkTrustAnchor
    {

        #region Data

        /// <summary>
        /// The query parameter naming the @ids of the identity keys, separated by commas.
        /// </summary>
        public const String IdentityKeysParameter  = "identityKeys";

        /// <summary>
        /// The query parameter naming the minConsensus, unless one.
        /// </summary>
        public const String MinConsensusParameter  = "minConsensus";

        /// <summary>
        /// What the text of a QR code of a trust anchor starts with, before the Base45 of its CBOR.
        /// </summary>
        public const String QRCodePrefix           = "WWCPPOI1:";

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext          = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/trustAnchor/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context.
            /// </summary>
            public const String  Context            = "@context";

            /// <summary>
            /// The property of the identification of the roaming network.
            /// </summary>
            public const String  RoamingNetworkId   = "roamingNetworkId";

            /// <summary>
            /// The property of the multilingual name of the roaming network, to show.
            /// </summary>
            public const String  Name               = "name";

            /// <summary>
            /// The property of the multilingual description of the roaming network, to show.
            /// </summary>
            public const String  Description        = "description";

            /// <summary>
            /// The property of the URI of the roaming network, ~/RNs/{RN_Id}, below which its proofs are served.
            /// </summary>
            public const String  RoamingNetworkURI  = "roamingNetworkURI";

            /// <summary>
            /// The property of the @ids of the identity keys of the roaming network operator: hexadecimal in JSON, the bytes
            /// in CBOR.
            /// </summary>
            public const String  IdentityKeys       = POIPartyKeys.IdentityKeysProperty;

            /// <summary>
            /// The property of how many holders of the identity keys sign a key statement, written unless one.
            /// </summary>
            public const String  MinConsensus       = POIPartyKeys.MinConsensusProperty;

        }

        #endregion


        /// <summary>
        /// The URI of the roaming network, ~/RNs/{RN_Id}, below which its proofs are served.
        /// </summary>
        public URL                     RoamingNetworkURL    { get; }

        /// <summary>
        /// The roaming network, the last segment of its URI.
        /// </summary>
        public RoamingNetwork_Id       RoamingNetworkId     { get; }

        /// <summary>
        /// The @ids of the identity keys of the roaming network operator: their SHA-256 thumbprints, lowercase hex.
        /// </summary>
        public ImmutableArray<String>  IdentityKeyIds       { get; }

        /// <summary>
        /// How many holders of the identity keys must sign a key statement, and of the signing keys it states attest a
        /// version: the minConsensus of the roaming network operator (SPECIFICATION.md, section 4.1).
        /// </summary>
        public Byte                    MinConsensus         { get; }

        /// <summary>
        /// The multilingual name of the roaming network, to show; not trusted.
        /// </summary>
        public ImmutableI18NString     Name                 { get; }

        /// <summary>
        /// The multilingual description of the roaming network, to show; not trusted.
        /// </summary>
        public ImmutableI18NString     Description          { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a trust anchor of the given roaming network and its URL, ~/RNs/{RN_Id}, the @ids of the identity keys of
        /// its operator and its minConsensus, with the name and description of the roaming network.
        /// </summary>
        /// <exception cref="ArgumentException">The URL is not the http or https URL of the roaming network, ~/RNs/{RN_Id} without a query, an @id is not a thumbprint, or the minConsensus not from 1 to the number of identity keys.</exception>
        public RoamingNetworkTrustAnchor(RoamingNetwork_Id       Id,
                                         URL                     RoamingNetworkURL,
                                         ImmutableArray<String>  IdentityKeyIds,
                                         Byte                    MinConsensus   = 1,
                                         ImmutableI18NString?    Name           = null,
                                         ImmutableI18NString?    Description    = null)
        {

            if (!TryCheck(RoamingNetworkURL, IdentityKeyIds, MinConsensus, out var roamingNetworkId, out _, out var error))
                throw new ArgumentException(error);

            if (roamingNetworkId != Id)
                throw new ArgumentException($"The URL of the trust anchor of the roaming network '{Id}' is the one of '{roamingNetworkId}'!");

            this.RoamingNetworkId   = Id;
            this.RoamingNetworkURL  = RoamingNetworkURL;
            this.IdentityKeyIds     = IdentityKeyIds;
            this.MinConsensus       = MinConsensus;
            this.Name               = Name        ?? ImmutableI18NString.Empty;
            this.Description        = Description ?? ImmutableI18NString.Empty;

        }

        #endregion


        #region ToURI()

        /// <summary>
        /// The URI for a QR code: the URI of the roaming network with the @ids of the identity keys and the minConsensus,
        /// unless one; without name, description and further information.
        /// </summary>
        public String ToURI()

            => $"{RoamingNetworkURL}?{IdentityKeysParameter}={String.Join(",", IdentityKeyIds)}" +
               (MinConsensus > 1 ? $"&{MinConsensusParameter}={MinConsensus}" : "");

        #endregion

        #region ToJSON()

        /// <summary>
        /// The JSON representation of this trust anchor: the @ids of the identity keys as 64 lowercase hexadecimal digits.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Context,           JSONLDContext),
                           new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,               Name.             ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,        Description.      ToJSON());

            json.Add(PropertyKeys.RoamingNetworkURI,      RoamingNetworkURL.ToString());

            json.Add(PropertyKeys.IdentityKeys,           new JArray(IdentityKeyIds));

            if (MinConsensus > 1)
                json.Add(PropertyKeys.MinConsensus,       MinConsensus);

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// The CBOR representation of this trust anchor, for a QR code: the keys and values of its JSON representation, the
        /// @ids of the identity keys as their 32 bytes. Write it with CBORWriterOptions.Canonical for deterministic bytes.
        /// </summary>
        public CBORValue ToCBOR()
        {

            var json  = ToJSON();
            var cbor  = new CBORMap();

            foreach (var property in json.Properties())
                cbor.Add(CBORValue.FromText(property.Name),
                         property.Name == PropertyKeys.IdentityKeys
                             ? CBORValue.FromArray(IdentityKeyIds.Select(id => CBORValue.FromBytes(Convert.FromHexString(id))))
                             : CBORJSON.ToCBOR(property.Value));

            return cbor;

        }

        #endregion

        #region ToQRCode()

        /// <summary>
        /// The text of a QR code of this trust anchor: WWCPPOI1: and the Base45 of its deterministic CBOR [RFC 9285], all of
        /// it of the alphanumeric mode of QR codes.
        /// </summary>
        public String ToQRCode()

            => QRCodePrefix + ToCBOR().ToByteArray(CBORWriterOptions.Canonical).ToBase45();

        #endregion


        #region (static) TryParse(URI, out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given trust anchor, e.g. of a QR code: its text WWCPPOI1: and the Base45 of its CBOR, or its URI.
        /// </summary>
        public static Boolean TryParse(String                                               URI,
                                       [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor = null;

            var text = URI?.Trim() ?? "";

            if (text.StartsWith(QRCodePrefix, StringComparison.Ordinal))
                return TryParseQRCode(text, out TrustAnchor, out ErrorResponse);

            // The URL of the roaming network, below which its proofs are served, is the one without the query.
            var queryAt = text.IndexOfAny([ '?', '#' ]);

            if (!URL.TryParse(text, out var url) ||
                !URL.TryParse(queryAt < 0 ? text : text[..queryAt], out var roamingNetworkURL))
            {
                ErrorResponse = "A trust anchor is an URL, or the text of its QR code!";
                return false;
            }

            var parameters    = (url.QueryString?.ToString() ?? "").TrimStart('?').
                                    Split('&', StringSplitOptions.RemoveEmptyEntries).
                                    Select (parameter => parameter.Split('=', 2)).
                                    ToArray();

            var identityKeys  = parameters.Where(parameter => parameter.Length == 2 && parameter[0] == IdentityKeysParameter).ToArray();
            var minConsensus  = parameters.Where(parameter => parameter.Length == 2 && parameter[0] == MinConsensusParameter).ToArray();

            if (identityKeys.Length != 1)
            {
                ErrorResponse = $"A trust anchor names the identity keys once: ?{IdentityKeysParameter}=…!";
                return false;
            }

            Byte quorum = 1;

            if (minConsensus.Length > 1 ||
               (minConsensus.Length == 1 && (!Byte.TryParse(minConsensus[0][1], out quorum) || quorum.ToString() != minConsensus[0][1])))
            {
                ErrorResponse = $"A trust anchor names its {MinConsensusParameter} at most once, as a whole number from 1 to {Byte.MaxValue}!";
                return false;
            }

            var ids = Uri.UnescapeDataString(identityKeys[0][1]).Split(',');

            if (!TryCheck(roamingNetworkURL, ids, quorum, out var rnId, out _, out ErrorResponse))
                return false;

            TrustAnchor = new RoamingNetworkTrustAnchor(rnId, roamingNetworkURL, [.. ids], quorum);
            return true;

        }

        #endregion

        #region (static) TryParseQRCode(Text, out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given text of a QR code of a trust anchor: WWCPPOI1: and the Base45 of its CBOR.
        /// </summary>
        public static Boolean TryParseQRCode(String                                              Text,
                                             [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                             [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor = null;

            if (Text is null || !Text.StartsWith(QRCodePrefix, StringComparison.Ordinal))
            {
                ErrorResponse = $"The QR code of a trust anchor starts with '{QRCodePrefix}'!";
                return false;
            }

            if (!Text[QRCodePrefix.Length..].TryParseBASE45(out var bytes, out var base45Error))
            {
                ErrorResponse = "The QR code of a trust anchor is not Base45: " + base45Error;
                return false;
            }

            try
            {
                return TryParse(CBORValue.Parse(bytes), out TrustAnchor, out ErrorResponse);
            }
            catch (Exception e)
            {
                ErrorResponse = "The QR code of a trust anchor is no CBOR: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a trust anchor: the keys and values of its JSON representation,
        /// the @ids of the identity keys as their 32 bytes.
        /// </summary>
        public static Boolean TryParse(CBORValue                                           CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor = null;

            if (CBOR.Kind != CBORValueKind.Map)
            {
                ErrorResponse = "The CBOR representation of a trust anchor must be a map!";
                return false;
            }

            var json = new JObject();

            foreach (var (key, value) in CBOR.AsMap())
            {

                if (!key.TryGetText(out var name))
                {
                    ErrorResponse = "The CBOR representation of a trust anchor must be a map with text keys!";
                    return false;
                }

                if (name == PropertyKeys.IdentityKeys)
                {

                    if (value.Kind != CBORValueKind.Array)
                    {
                        ErrorResponse = "The identity keys of a trust anchor must be an array of byte strings!";
                        return false;
                    }

                    var ids = new JArray();

                    foreach (var id in value.AsArray())
                    {
                        if (!id.TryGetBytes(out var thumbprint) || thumbprint.Length != 32)
                        {
                            ErrorResponse = "The identity keys of a trust anchor must be their SHA-256 thumbprints, 32 bytes each!";
                            return false;
                        }
                        ids.Add(Convert.ToHexStringLower(thumbprint));
                    }

                    json.Add(name, ids);

                }

                else
                    json.Add(name, CBORJSON.ToJSON(value));

            }

            return TryParse(json, out TrustAnchor, out ErrorResponse);

        }

        #endregion

        #region (static) TryParse(JSON, out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a trust anchor.
        /// </summary>
        public static Boolean TryParse(JObject                                              JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor    = null;
            ErrorResponse  = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of a trust anchor must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory(PropertyKeys.RoamingNetworkId,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.RoamingNetworkURI, "roaming network URI", out var uriText, out ErrorResponse))
                {
                    return false;
                }

                if (uriText is null || !URL.TryParse(uriText, out var url))
                {
                    ErrorResponse = "A trust anchor names the absolute URL of its roaming network!";
                    return false;
                }

                if (JSON[PropertyKeys.IdentityKeys] is not JArray identityKeysJSON ||
                    identityKeysJSON.Any(id => id.Type != JTokenType.String))
                {
                    ErrorResponse = "A trust anchor names the identity keys of the roaming network operator by their @ids!";
                    return false;
                }

                Byte minConsensus = 1;

                if (JSON[PropertyKeys.MinConsensus] is JToken minConsensusJSON)
                {

                    if (minConsensusJSON.Type != JTokenType.Integer || minConsensusJSON.Value<Int64>() is < 1 or > Byte.MaxValue)
                    {
                        ErrorResponse = $"The {PropertyKeys.MinConsensus} of a trust anchor must be a whole number from 1 to {Byte.MaxValue}!";
                        return false;
                    }

                    minConsensus = (Byte) minConsensusJSON.Value<Int64>();

                }

                ImmutableI18NString? name         = null;
                ImmutableI18NString? description  = null;

                if ((JSON[PropertyKeys.Name]        is JToken nameJSON        && (nameJSON        is not JObject nameObject        || !ImmutableI18NString.TryParse(nameObject,        out name,        out ErrorResponse))) ||
                    (JSON[PropertyKeys.Description] is JToken descriptionJSON && (descriptionJSON is not JObject descriptionObject || !ImmutableI18NString.TryParse(descriptionObject, out description, out ErrorResponse))))
                {
                    ErrorResponse ??= "The name and the description of a trust anchor are multilingual texts!";
                    return false;
                }

                var ids = identityKeysJSON.Select(id => id.Value<String>()!).ToArray();

                if (!TryCheck(url, ids, minConsensus, out var rnId, out _, out ErrorResponse))
                    return false;

                if (roamingNetworkId != rnId)
                {
                    ErrorResponse = $"The trust anchor of the roaming network '{roamingNetworkId}' names the URL of '{rnId}'!";
                    return false;
                }

                TrustAnchor    = new RoamingNetworkTrustAnchor(
                                     roamingNetworkId,
                                     url,
                                     [.. ids],
                                     minConsensus,
                                     name,
                                     description
                                 );

                return true;

            }
            catch (Exception e)
            {
                TrustAnchor    = null;
                ErrorResponse  = "Invalid trust anchor: " + e.Message;
                return false;
            }

        }

        #endregion


        #region (private static) TryCheck(URL, IdentityKeyIds, MinConsensus, out RoamingNetworkId, out Ids, out ErrorResponse)

        private static Boolean TryCheck(URL                               URL,
                                        IEnumerable<String>               IdentityKeyIds,
                                        Byte                              MinConsensus,
                                        out RoamingNetwork_Id             RoamingNetworkId,
                                        out ImmutableArray<String>        Ids,
                                        [NotNullWhen(false)] out String?  ErrorResponse)
        {

            RoamingNetworkId  = default;
            Ids               = [ .. IdentityKeyIds ];

            if (!IsHTTP(URL))
            {
                ErrorResponse = "A trust anchor is an http or https URL!";
                return false;
            }

            var segments = URL.Path.ToString().TrimEnd('/').Split('/');

            if (URL.QueryString is not null || URL.Fragment is not null ||
                segments.Length < 2 || segments[^2] != "RNs" ||
                !RoamingNetwork_Id.TryParse(Uri.UnescapeDataString(segments[^1]), out RoamingNetworkId))
            {
                ErrorResponse = "The URL of a trust anchor is the one of a roaming network, ~/RNs/{RN_Id}, without a query!";
                return false;
            }

            if (Ids.IsEmpty ||
                Ids.Any(id => id.Length != 64 || id.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0) ||
                Ids.Distinct().Count() != Ids.Length)
            {
                ErrorResponse = "A trust anchor names at least one identity key by its @id, 64 lowercase hexadecimal digits, each once!";
                return false;
            }

            if (MinConsensus < 1 || MinConsensus > Ids.Length)
            {
                ErrorResponse = $"The minConsensus {MinConsensus} of a trust anchor needs as many identity keys, not {Ids.Length}!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) IsHTTP(URL)

        /// <summary>
        /// Whether the given URL is an http or an https URL.
        /// </summary>
        private static Boolean IsHTTP(URL URL)

            => URL.Scheme == URIScheme.https || URL.Scheme == URIScheme.http;

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => ToURI();

        #endregion

    }

}
