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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public static class RoamingNetworkTrustAnchorExtensions
    {

        /// <summary>
        /// The trust anchor of the given roaming network as the given key statement states its keys and quorum, with the
        /// given URLs of the roaming network, the preferred one first, and its name and description.
        /// </summary>
        public static RoamingNetworkTrustAnchor CreateTrustAnchor(this RoamingNetwork          RoamingNetwork,
                                                                  RoamingNetworkKeyStatement   KeyStatement,
                                                                  params IEnumerable<URL>      URLs)

            => new (KeyStatement.RoamingNetworkId,
                    URLs,
                    [.. KeyStatement.IdentityKeys.Select(publicKey => publicKey.Id)],
                    KeyStatement.MinConsensus,
                    RoamingNetwork.Name,
                    RoamingNetwork.Description);

    }


    /// <summary>
    /// What an app trusts of a roaming network (SPECIFICATION.md, section 8.6): where its proofs come from, one URL or
    /// more, so that it still finds them when one server is down; the @ids of the identity keys of its operator, their
    /// SHA-256 thumbprints, and how many holders of them sign, its minConsensus; with its name and its description, to
    /// show. For a QR code, e.g. on the website or in the documents of the roaming network operator, as the text WWCP/TA-2:
    /// and the Base45 of its deterministic CBOR [RFC 9285], or, without name and description, as one URL:
    /// https://hub.example/poi/RNs/rn1?keys=a1…,c3…&amp;minConsensus=2&amp;alsoAt=https%3A%2F%2Fmirror.example%2Fpoi%2FRNs%2Frn1.
    /// Only the thumbprints and the minConsensus are trusted; the keys themselves come with every proof, in the key statement
    /// and the operator it gives.
    /// </summary>
    public sealed class RoamingNetworkTrustAnchor
    {

        #region Data

        /// <summary>
        /// The query parameter naming the @ids of the identity keys, separated by commas.
        /// </summary>
        public const String IdentityKeysParameter  = "keys";

        /// <summary>
        /// The query parameter naming the minConsensus, unless one.
        /// </summary>
        public const String MinConsensusParameter  = "minConsensus";

        /// <summary>
        /// The query parameter naming a further URL of the roaming network, escaped, once for each.
        /// </summary>
        public const String AlsoAtParameter        = "alsoAt";

        /// <summary>
        /// What the text of a QR code of a trust anchor starts with, before the Base45 of its CBOR.
        /// </summary>
        public const String QRCodePrefix           = "WWCP/TA-2:";

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext          = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/trustAnchor/v2";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the identification of the roaming network.
            /// </summary>
            public const String  Id            = "@id";

            /// <summary>
            /// The property of its JSON-LD context.
            /// </summary>
            public const String  Context       = "@context";

            /// <summary>
            /// The property of the multilingual name of the roaming network, to show.
            /// </summary>
            public const String  Name          = "name";

            /// <summary>
            /// The property of the multilingual description of the roaming network, to show.
            /// </summary>
            public const String  Description   = "description";

            /// <summary>
            /// The property of the URLs of the roaming network, ~/RNs/{RN_Id}, below which its proofs are served: the
            /// preferred one first.
            /// </summary>
            public const String  URLs          = "urls";

            /// <summary>
            /// The property of the @ids of the identity keys of the roaming network operator: hexadecimal in JSON, the bytes
            /// in CBOR.
            /// </summary>
            public const String  Keys          = "keys";

            /// <summary>
            /// The property of how many holders of the identity keys sign a key statement, written unless one.
            /// </summary>
            public const String  MinConsensus  = POIPartyKeys.MinConsensusProperty;

        }

        #endregion


        /// <summary>
        /// The URLs of the roaming network, ~/RNs/{RN_Id}, below which its proofs are served, the preferred one first: an
        /// app tries the next when one does not answer. The proofs are signed, so no server needs to be trusted.
        /// </summary>
        public ImmutableArray<URL>     URLs            { get; }

        /// <summary>
        /// The roaming network, the last segment of its URLs.
        /// </summary>
        public RoamingNetwork_Id       Id              { get; }

        /// <summary>
        /// The SHA-256 thumbprints of the identity keys of the roaming network operator as lowercase hex.
        /// </summary>
        public ImmutableArray<String>  Keys            { get; }

        /// <summary>
        /// How many holders of the identity keys must sign a key statement, and of the signing keys it states attest a
        /// version: the minConsensus of the roaming network operator (SPECIFICATION.md, section 4.1).
        /// </summary>
        public Byte                    MinConsensus    { get; }

        /// <summary>
        /// The multilingual name of the roaming network, to show; not trusted.
        /// </summary>
        public ImmutableI18NString     Name            { get; }

        /// <summary>
        /// The multilingual description of the roaming network, to show; not trusted.
        /// </summary>
        public ImmutableI18NString     Description     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a trust anchor of the given roaming network and its URLs, ~/RNs/{RN_Id}, the preferred one first, the
        /// @ids of the identity keys of its operator and its minConsensus, with the name and description of the roaming
        /// network.
        /// </summary>
        /// <exception cref="ArgumentException">A URL is not an http or https URL of the roaming network, ~/RNs/{RN_Id} without a query, or given twice; an @id is not a thumbprint, or the minConsensus not from 1 to the number of identity keys.</exception>
        public RoamingNetworkTrustAnchor(RoamingNetwork_Id       Id,
                                         IEnumerable<URL>        URLs,
                                         ImmutableArray<String>  Keys,
                                         Byte                    MinConsensus   = 1,
                                         ImmutableI18NString?    Name           = null,
                                         ImmutableI18NString?    Description    = null)
        {

            var urls = URLs?.ToImmutableArray() ?? [];

            if (!TryCheck(urls, Keys, MinConsensus, out var roamingNetworkId, out _, out var error))
                throw new ArgumentException(error);

            if (roamingNetworkId != Id)
                throw new ArgumentException($"The URLs of the trust anchor of the roaming network '{Id}' are those of '{roamingNetworkId}'!");

            this.Id            = Id;
            this.URLs          = urls;
            this.Keys          = Keys;
            this.MinConsensus  = MinConsensus;
            this.Name          = Name        ?? ImmutableI18NString.Empty;
            this.Description   = Description ?? ImmutableI18NString.Empty;

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// The JSON representation of this trust anchor: the @ids of the identity keys as 64 lowercase hexadecimal digits.
        /// </summary>
        public JObject ToJSON()
        {

            var json = JSONObject.Create(

                                 new JProperty(PropertyKeys.Id,             Id.         ToString()),
                                 new JProperty(PropertyKeys.Context,        JSONLDContext),
                                 new JProperty(PropertyKeys.URLs,           new JArray(URLs.Select(url => url.ToString()))),
                                 new JProperty(PropertyKeys.Keys,           new JArray(Keys)),

                           Name.IsNotNullOrEmpty()
                               ? new JProperty(PropertyKeys.Name,           Name.       ToJSON())
                               : null,

                           Description.IsNotNullOrEmpty()
                               ? new JProperty(PropertyKeys.Description,    Description.ToJSON())
                               : null,

                           MinConsensus > 1
                               ? new JProperty(PropertyKeys.MinConsensus,   MinConsensus)
                               : null

                       );

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

            var cbor = new CBORMap {
                           { CBORValue.FromText(PropertyKeys.Id),       CBORValue.FromText (Id.ToString())  },
                           { CBORValue.FromText(PropertyKeys.Context),  CBORValue.FromText (JSONLDContext)  },
                           { CBORValue.FromText(PropertyKeys.URLs),     CBORValue.FromArray(URLs.Select(url => CBORValue.FromText(url.ToString()))) },
                           { CBORValue.FromText(PropertyKeys.Keys),     CBORValue.FromArray(Keys.Select(id  => CBORValue.FromBytes(Convert.FromHexString(id)))) }
                       };

            if (Name.       IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Name),           Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Description),    Description.ToCBOR());

            if (MinConsensus > 1)
                cbor.Add(CBORValue.FromText(PropertyKeys.MinConsensus),   CBORValue.FromUInt64(MinConsensus));

            return cbor;

        }

        #endregion

        #region ToURL()

        /// <summary>
        /// The URL for a QR code: the preferred URL of the roaming network with the @ids of the identity keys, the
        /// minConsensus, unless one, and the further URLs, unless none, each escaped; without name and description.
        /// </summary>
        public String ToURL()

            => $"{URLs[0]}?{IdentityKeysParameter}={String.Join(",", Keys)}" +
               (MinConsensus > 1 ? $"&{MinConsensusParameter}={MinConsensus}" : "") +
               String.Concat(URLs.Skip(1).Select(url => $"&{AlsoAtParameter}={Uri.EscapeDataString(url.ToString())}"));

        #endregion

        #region ToQRCode()

        /// <summary>
        /// The text of a QR code of this trust anchor: WWCP/TA-2: and the Base45 of its deterministic CBOR [RFC 9285], all of
        /// it of the alphanumeric mode of QR codes.
        /// </summary>
        public String ToQRCode()

            => QRCodePrefix + ToCBOR().ToByteArray(CBORWriterOptions.Canonical).ToBase45();

        #endregion


        #region (static) TryParse (URL,  out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given URL of a trust anchor: the preferred URL of the roaming network, ~/RNs/{RN_Id}, with the
        /// @ids of the identity keys, the minConsensus, unless one, and its further URLs, unless none, as its query, e.g.
        /// https://hub.example/poi/RNs/rn1?keys=a1…,c3…&amp;minConsensus=2&amp;alsoAt=https%3A%2F%2Fmirror.example%2Fpoi%2FRNs%2Frn1.
        /// </summary>
        public static Boolean TryParse(URL                                                  URL,
                                       [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor = null;

            // The URL of the roaming network, below which its proofs are served, is the one without the query.
            var text     = URL.ToString();
            var queryAt  = text.IndexOfAny([ '?', '#' ]);

            if (!URL.TryParse(queryAt < 0 ? text : text[..queryAt], out var roamingNetworkURL))
            {
                ErrorResponse = "A trust anchor is the URL of its roaming network, ~/RNs/{RN_Id}, with the @ids of its identity keys as its query!";
                return false;
            }

            // The query as written: the further URLs are escaped once, and read so.
            var fragmentAt    = text.IndexOf('#');
            var query         = queryAt < 0 || text[queryAt] != '?' ? "" : text[(queryAt + 1)..(fragmentAt < 0 ? text.Length : fragmentAt)];

            var parameters    = query.Split('&', StringSplitOptions.RemoveEmptyEntries).
                                    Select (parameter => parameter.Split('=', 2)).
                                    ToArray();

            var identityKeys  = parameters.Where(parameter => parameter.Length == 2 && parameter[0] == IdentityKeysParameter).ToArray();
            var minConsensus  = parameters.Where(parameter => parameter.Length == 2 && parameter[0] == MinConsensusParameter).ToArray();
            var alsoAt        = parameters.Where(parameter => parameter.Length == 2 && parameter[0] == AlsoAtParameter).      ToArray();

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

            var urls = new List<URL> { roamingNetworkURL };

            foreach (var further in alsoAt.Select(parameter => parameter[1]))
            {

                if (!URL.TryParse(Uri.UnescapeDataString(further), out var furtherURL))
                {
                    ErrorResponse = $"A further URL of a trust anchor, '{Uri.UnescapeDataString(further)}', is no URL!";
                    return false;
                }

                urls.Add(furtherURL);

            }

            var ids = Uri.UnescapeDataString(identityKeys[0][1]).Split(',');

            if (!TryCheck(urls, ids, quorum, out var rnId, out _, out ErrorResponse))
                return false;

            TrustAnchor = new RoamingNetworkTrustAnchor(rnId, urls, [.. ids], quorum);
            return true;

        }

        #endregion

        #region (static) TryParse (Text, out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given text of a trust anchor, e.g. as scanned from a QR code: WWCP/TA-2: and the Base45 of its
        /// CBOR, or else its URL.
        /// </summary>
        public static Boolean TryParse(String                                               Text,
                                       [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                       [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor = null;

            var text = Text?.Trim() ?? "";

            if (!text.StartsWith(QRCodePrefix, StringComparison.Ordinal))
            {

                if (URL.TryParse(text, out var url))
                    return TryParse(url, out TrustAnchor, out ErrorResponse);

                ErrorResponse = $"A trust anchor is the text of its QR code, '{QRCodePrefix}' and the Base45 of its CBOR, or its URL!";
                return false;

            }

            if (!text[QRCodePrefix.Length..].TryParseBASE45(out var bytes, out var base45Error))
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

        #region (static) TryParse (JSON, out TrustAnchor, out ErrorResponse)

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

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse))
                {
                    return false;
                }

                var urls = new List<URL>();

                if (JSON[PropertyKeys.URLs] is not JArray urlsJSON)
                {
                    ErrorResponse = "A trust anchor names the absolute URLs of its roaming network!";
                    return false;
                }

                foreach (var urlJSON in urlsJSON)
                {

                    if (urlJSON.Type != JTokenType.String || !URL.TryParse(urlJSON.Value<String>()!, out var url))
                    {
                        ErrorResponse = "A trust anchor names the absolute URLs of its roaming network!";
                        return false;
                    }

                    urls.Add(url);

                }

                if (JSON[PropertyKeys.Keys] is not JArray identityKeysJSON ||
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

                return TryCreate(roamingNetworkId,
                                 urls,
                                 identityKeysJSON.Select(id => id.Value<String>()!),
                                 minConsensus,
                                 name,
                                 description,
                                 out TrustAnchor,
                                 out ErrorResponse);

            }
            catch (Exception e)
            {
                TrustAnchor    = null;
                ErrorResponse  = "Invalid trust anchor: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse (CBOR, out TrustAnchor, out ErrorResponse)

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

            var values = new Dictionary<String, CBORValue>(StringComparer.Ordinal);

            foreach (var (key, value) in CBOR.AsMap())
            {

                if (!key.TryGetText(out var property))
                {
                    ErrorResponse = "The CBOR representation of a trust anchor must be a map with text keys!";
                    return false;
                }

                if (!values.TryAdd(property, value))
                {
                    ErrorResponse = $"The CBOR representation of a trust anchor names '{property}' twice!";
                    return false;
                }

            }

            if (values.TryGetValue(PropertyKeys.Context, out var context) &&
                !(context.TryGetText(out var contextText) && contextText == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of a trust anchor must be '{JSONLDContext}'!";
                return false;
            }

            if (!values.TryGetValue(PropertyKeys.Id, out var idCBOR) ||
                !idCBOR.TryGetText(out var idText) ||
                !RoamingNetwork_Id.TryParse(idText, out var roamingNetworkId))
            {
                ErrorResponse = "A trust anchor names its roaming network by its identification!";
                return false;
            }

            if (!values.TryGetValue(PropertyKeys.URLs, out var urlsCBOR) ||
                urlsCBOR.Kind != CBORValueKind.Array)
            {
                ErrorResponse = "A trust anchor names the absolute URLs of its roaming network!";
                return false;
            }

            var urls = new List<URL>();

            foreach (var urlCBOR in urlsCBOR.AsArray())
            {

                if (!urlCBOR.TryGetText(out var urlText) || !URL.TryParse(urlText, out var url))
                {
                    ErrorResponse = "A trust anchor names the absolute URLs of its roaming network!";
                    return false;
                }

                urls.Add(url);

            }

            if (!values.TryGetValue(PropertyKeys.Keys, out var keysCBOR) ||
                keysCBOR.Kind != CBORValueKind.Array)
            {
                ErrorResponse = "The identity keys of a trust anchor must be an array of byte strings!";
                return false;
            }

            var ids = new List<String>();

            foreach (var id in keysCBOR.AsArray())
            {

                if (!id.TryGetBytes(out var thumbprint) || thumbprint.Length != 32)
                {
                    ErrorResponse = "The identity keys of a trust anchor must be their SHA-256 thumbprints, 32 bytes each!";
                    return false;
                }

                ids.Add(Convert.ToHexStringLower(thumbprint));

            }

            Byte minConsensus = 1;

            if (values.TryGetValue(PropertyKeys.MinConsensus, out var minConsensusCBOR))
            {

                if (!minConsensusCBOR.TryGetUInt64(out var quorum) || quorum is < 1 or > Byte.MaxValue)
                {
                    ErrorResponse = $"The {PropertyKeys.MinConsensus} of a trust anchor must be a whole number from 1 to {Byte.MaxValue}!";
                    return false;
                }

                minConsensus = (Byte) quorum;

            }

            ImmutableI18NString? name         = null;
            ImmutableI18NString? description  = null;

            if (values.TryGetValue(PropertyKeys.Name, out var nameCBOR))
            {
                if (!I18NString.TryParse(nameCBOR, out var nameText, out ErrorResponse))
                    return false;
                name = new ImmutableI18NString(nameText);
            }

            if (values.TryGetValue(PropertyKeys.Description, out var descriptionCBOR))
            {
                if (!I18NString.TryParse(descriptionCBOR, out var descriptionText, out ErrorResponse))
                    return false;
                description = new ImmutableI18NString(descriptionText);
            }

            return TryCreate(roamingNetworkId,
                             urls,
                             ids,
                             minConsensus,
                             name,
                             description,
                             out TrustAnchor,
                             out ErrorResponse);

        }

        #endregion


        #region (private static) TryCreate(Id, URLs, IdentityKeyIds, MinConsensus, Name, Description, out TrustAnchor, out ErrorResponse)

        /// <summary>
        /// The trust anchor of the given values as its JSON or CBOR representation names them: checked as the constructor
        /// checks them, and the roaming network the one its URLs name.
        /// </summary>
        private static Boolean TryCreate(RoamingNetwork_Id                                    Id,
                                         IReadOnlyList<URL>                                   URLs,
                                         IEnumerable<String>                                  IdentityKeyIds,
                                         Byte                                                 MinConsensus,
                                         ImmutableI18NString?                                 Name,
                                         ImmutableI18NString?                                 Description,
                                         [NotNullWhen(true)]  out RoamingNetworkTrustAnchor?  TrustAnchor,
                                         [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            TrustAnchor = null;

            if (!TryCheck(URLs, IdentityKeyIds, MinConsensus, out var rnId, out var ids, out ErrorResponse))
                return false;

            if (Id != rnId)
            {
                ErrorResponse = $"The trust anchor of the roaming network '{Id}' names the URL of '{rnId}'!";
                return false;
            }

            TrustAnchor = new RoamingNetworkTrustAnchor(Id, URLs, ids, MinConsensus, Name, Description);
            return true;

        }

        #endregion

        #region (private static) TryCheck(URLs, IdentityKeyIds, MinConsensus, out RoamingNetworkId, out Ids, out ErrorResponse)

        /// <summary>
        /// Whether the given URLs, at least one, each once, are http or https URLs of one roaming network, ~/RNs/{RN_Id}
        /// without a query, and the given @ids thumbprints of a quorum of the given minConsensus.
        /// </summary>
        private static Boolean TryCheck(IReadOnlyList<URL>                URLs,
                                        IEnumerable<String>               IdentityKeyIds,
                                        Byte                              MinConsensus,
                                        out RoamingNetwork_Id             RoamingNetworkId,
                                        out ImmutableArray<String>        Ids,
                                        [NotNullWhen(false)] out String?  ErrorResponse)
        {

            RoamingNetworkId  = default;
            Ids               = [ .. IdentityKeyIds ];

            if (URLs.Count == 0)
            {
                ErrorResponse = "A trust anchor names at least one URL of its roaming network!";
                return false;
            }

            if (URLs.Select(url => url.ToString()).Distinct(StringComparer.Ordinal).Count() != URLs.Count)
            {
                ErrorResponse = "A trust anchor names each URL of its roaming network once!";
                return false;
            }

            for (var index = 0; index < URLs.Count; index++)
            {

                var url = URLs[index];

                if (!IsHTTP(url))
                {
                    ErrorResponse = $"The URL '{url}' of a trust anchor is no http or https URL!";
                    return false;
                }

                var segments = url.Path.ToString().TrimEnd('/').Split('/');

                if (url.QueryString is not null || url.Fragment is not null ||
                    segments.Length < 2 || segments[^2] != "RNs" ||
                    !RoamingNetwork_Id.TryParse(Uri.UnescapeDataString(segments[^1]), out var urlNetworkId))
                {
                    ErrorResponse = $"The URL '{url}' of a trust anchor is not the one of a roaming network, ~/RNs/{{RN_Id}}, without a query!";
                    return false;
                }

                if (index == 0)
                    RoamingNetworkId = urlNetworkId;

                else if (urlNetworkId != RoamingNetworkId)
                {
                    ErrorResponse = $"The URLs of a trust anchor name one roaming network, not '{RoamingNetworkId}' and '{urlNetworkId}'!";
                    return false;
                }

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

            => ToURL();

        #endregion

    }

}
