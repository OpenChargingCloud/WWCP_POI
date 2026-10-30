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
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The merchant of a charging station operator at a GNU Taler merchant backend, and the exchanges whose coins it
    /// accepts (concepts/EVTaler.md, section 4.2): an immutable value, the operator's evTaler. Every contract of a step
    /// is signed with its public key, so that a driver's app can check, against the proof of a charging station, that
    /// an offer takes money for the station's operator.
    /// </summary>
    public sealed class EVTalerMerchant
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const   String  JSONLDContext      = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/evTalerMerchant/v1";

        /// <summary>
        /// The alphabet of Crockford's Base32, in which GNU Taler writes its keys.
        /// </summary>
        private const  String  CrockfordBase32    = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

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
            public const String  Context             = "@context";

            /// <summary>
            /// The property of the base URL of its instance at the merchant backend.
            /// </summary>
            public const String  MerchantBaseURL     = "merchantBaseURL";

            /// <summary>
            /// The property of the public key of its instance.
            /// </summary>
            public const String  MerchantPublicKey   = "merchantPublicKey";

            /// <summary>
            /// The property of the exchanges it accepts.
            /// </summary>
            public const String  Exchanges           = "exchanges";

        }

        #endregion

        /// <summary>
        /// The base URL of its instance at the merchant backend: HTTPS, ending in '/', the merchant_base_url of its contract terms.
        /// </summary>
        public URL                               MerchantBaseURL      { get; }

        /// <summary>
        /// The public key of its instance, as GNU Taler writes it: the merchant_pub every contract of a step is signed with.
        /// </summary>
        public String                            MerchantPublicKey    { get; }

        /// <summary>
        /// The exchanges it accepts, at least one, in the ordinal order of their master public keys, each once.
        /// </summary>
        public ImmutableArray<EVTalerExchange>   Exchanges            { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the merchant of a charging station operator at a GNU Taler merchant backend.
        /// </summary>
        /// <param name="MerchantBaseURL">The base URL of its instance at the merchant backend: HTTPS, ending in '/'.</param>
        /// <param name="MerchantPublicKey">The public key of its instance, as GNU Taler writes it.</param>
        /// <param name="Exchanges">The exchanges it accepts, at least one, each once; in any order.</param>
        /// <exception cref="ArgumentException">A value is not one of a merchant.</exception>
        public EVTalerMerchant(URL                           MerchantBaseURL,
                               String                        MerchantPublicKey,
                               IEnumerable<EVTalerExchange>  Exchanges)
        {

            if (InvalidBaseURL(MerchantBaseURL, "merchant base URL") is String baseURLError)
                throw new ArgumentException(baseURLError, nameof(MerchantBaseURL));

            if (!IsTalerPublicKey(MerchantPublicKey))
                throw new ArgumentException($"The merchant public key '{MerchantPublicKey}' is no public key as GNU Taler writes it!", nameof(MerchantPublicKey));

            var exchanges = (Exchanges ?? []).ToArray();

            if (exchanges.Length == 0)
                throw new ArgumentException("A merchant accepts at least one exchange!", nameof(Exchanges));

            if (exchanges.Any(exchange => exchange is null))
                throw new ArgumentException("The exchanges must not contain null entries!", nameof(Exchanges));

            if (exchanges.Select(exchange => exchange.MasterPublicKey).Distinct(StringComparer.Ordinal).Count() != exchanges.Length)
                throw new ArgumentException("An exchange must not be given twice!", nameof(Exchanges));

            this.MerchantBaseURL    = MerchantBaseURL;
            this.MerchantPublicKey  = MerchantPublicKey;
            this.Exchanges          = [.. exchanges.OrderBy(exchange => exchange.MasterPublicKey, StringComparer.Ordinal)];

        }

        #endregion


        #region ExchangesFor(Currency)

        /// <summary>
        /// The exchanges it accepts in the given currency, e.g. the one of the EV Taler tariff of an EVSE.
        /// </summary>
        /// <param name="Currency">A currency.</param>
        public IEnumerable<EVTalerExchange> ExchangesFor(Currency Currency)

            => Exchanges.Where(exchange => exchange.Currency.ISOCode == Currency.ISOCode);

        #endregion

        #region (static) IsTalerPublicKey(Text)

        /// <summary>
        /// Whether the given text is an EdDSA public key as GNU Taler writes it, e.g. the merchant_pub of contract terms
        /// or the master public key of an exchange: its 32 bytes in Crockford's Base32, in upper case, 52 characters of 5
        /// bits each, the last 4 of them zero. Every other spelling of the same key is refused, so that the key is compared
        /// with the one of a contract as a text.
        /// </summary>
        /// <param name="Text">A text.</param>
        public static Boolean IsTalerPublicKey([NotNullWhen(true)] String? Text)

            => Text is { Length: 52 } &&
               Text.All(CrockfordBase32.Contains) &&
               (CrockfordBase32.IndexOf(Text[^1]) & 0x0F) == 0;

        #endregion

        #region (internal static) InvalidBaseURL(URL, Description)

        /// <summary>
        /// The error, when the given URL is no base URL of GNU Taler: HTTPS, without user information, query and
        /// fragment, ending in '/'; null otherwise.
        /// </summary>
        /// <param name="URL">An URL.</param>
        /// <param name="Description">What the URL is, for the error.</param>
        internal static String? InvalidBaseURL(URL     URL,
                                               String  Description)
        {

            var text = URL.ToString();

            if (URL.Scheme != URIScheme.https)
                return $"The {Description} '{text}' must be an HTTPS URL!";

            if (URL.Login is not null || URL.Password is not null)
                return $"The {Description} '{text}' must not name a user!";

            if (URL.QueryString is not null || URL.Fragment is not null)
                return $"The {Description} '{text}' must have neither a query nor a fragment!";

            if (!text.EndsWith('/'))
                return $"The {Description} '{text}' must end with '/'!";

            return null;

        }

        #endregion


        #region ToJSON(Embedded = false, CustomEVTalerMerchantSerializer = null, CustomEVTalerExchangeSerializer = null)

        /// <summary>
        /// Return the JSON representation of this merchant.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure, e.g. into its charging station operator.</param>
        /// <param name="CustomEVTalerMerchantSerializer">An optional custom serializer of merchants.</param>
        /// <param name="CustomEVTalerExchangeSerializer">An optional custom serializer of exchanges.</param>
        public JObject ToJSON(Boolean                                            Embedded                          = false,
                              CustomJObjectSerializerDelegate<EVTalerMerchant>?  CustomEVTalerMerchantSerializer   = null,
                              CustomJObjectSerializerDelegate<EVTalerExchange>?  CustomEVTalerExchangeSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,            JSONLDContext);

            json.Add(PropertyKeys.MerchantBaseURL,        MerchantBaseURL.ToString());
            json.Add(PropertyKeys.MerchantPublicKey,      MerchantPublicKey);
            json.Add(PropertyKeys.Exchanges,              new JArray(Exchanges.Select(exchange => exchange.ToJSON(Embedded: true,
                                                                                                                  CustomEVTalerExchangeSerializer))));

            return CustomEVTalerMerchantSerializer is not null
                       ? CustomEVTalerMerchantSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out EVTalerMerchant, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a merchant.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EVTalerMerchant">The parsed merchant.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out EVTalerMerchant?  EVTalerMerchant,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            EVTalerMerchant = null;

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of an EV Taler merchant must be '{JSONLDContext}'!";
                return false;
            }

            if (!POIJSON.TryParseText(JSON, PropertyKeys.MerchantBaseURL, "merchant base URL", URL.TryParse, out URL? merchantBaseURL, out ErrorResponse))
                return false;

            if (!merchantBaseURL.HasValue)
            {
                ErrorResponse = "An EV Taler merchant needs its merchant base URL!";
                return false;
            }

            if (JSON[PropertyKeys.MerchantPublicKey] is not JToken merchantPublicKeyJSON ||
                merchantPublicKeyJSON.Type != JTokenType.String ||
                !IsTalerPublicKey(merchantPublicKeyJSON.Value<String>()))
            {
                ErrorResponse = $"An EV Taler merchant needs its merchant public key, as GNU Taler writes it, not '{JSON[PropertyKeys.MerchantPublicKey]}'!";
                return false;
            }

            if (JSON[PropertyKeys.Exchanges] is not JArray exchangesJSON)
            {
                ErrorResponse = "The exchanges of an EV Taler merchant must be a JSON array!";
                return false;
            }

            var exchanges = new List<EVTalerExchange>(exchangesJSON.Count);

            foreach (var exchangeJSON in exchangesJSON)
            {

                if (exchangeJSON is not JObject exchangeObject)
                {
                    ErrorResponse = "An exchange of an EV Taler merchant must be a JSON object!";
                    return false;
                }

                if (!EVTalerExchange.TryParse(exchangeObject, out var exchange, out var exchangeError))
                {
                    ErrorResponse = $"Invalid exchange of an EV Taler merchant: {exchangeError}";
                    return false;
                }

                exchanges.Add(exchange);

            }

            try
            {
                EVTalerMerchant = new EVTalerMerchant(merchantBaseURL.Value,
                                                      merchantPublicKeyJSON.Value<String>()!,
                                                      exchanges);
            }
            catch (ArgumentException e)
            {
                ErrorResponse = e.Message;
                return false;
            }

            // What it takes and writes otherwise, after all it refuses: the merchant, then its exchanges as given.
            ErrorResponse = NotWrittenAsRead(JSON, EVTalerMerchant);

            for (var index = 0; ErrorResponse is null && index < exchanges.Count; index++)
                if (EVTalerExchange.NotWrittenAsRead((JObject) exchangesJSON[index], exchanges[index]) is String exchangeError)
                    ErrorResponse = $"Invalid exchange of an EV Taler merchant: {exchangeError}";

            if (ErrorResponse is not null)
            {
                EVTalerMerchant = null;
                return false;
            }

            return true;

        }

        #endregion

        #region (private static) NotWrittenAsRead(JSON, EVTalerMerchant)

        /// <summary>
        /// What a merchant names, as TryParse reads it.
        /// </summary>
        private static readonly String[] ReadProperties = [ PropertyKeys.Context, PropertyKeys.MerchantBaseURL, PropertyKeys.MerchantPublicKey, PropertyKeys.Exchanges ];

        /// <summary>
        /// The error, when the given merchant, read from the given JSON, is not written as it was given: a property it does
        /// not read, which it leaves out, or a base URL it writes otherwise, e.g. with its scheme or trimmed. The content
        /// holds it only as it writes it (SPECIFICATION.md, section 2, Written forms).
        /// </summary>
        private static String? NotWrittenAsRead(JObject          JSON,
                                                EVTalerMerchant  EVTalerMerchant)
        {

            if (JSON.Properties().FirstOrDefault(property => !ReadProperties.Contains(property.Name)) is JProperty unread)
                return $"The EV Taler merchant must not name the property '{unread.Name}'!";

            var merchantBaseURL = JSON[PropertyKeys.MerchantBaseURL]!.Value<String>();

            if (merchantBaseURL != EVTalerMerchant.MerchantBaseURL.ToString())
                return $"The merchant base URL '{merchantBaseURL}' is written as '{EVTalerMerchant.MerchantBaseURL}'!";

            return null;

        }

        #endregion

    }


    /// <summary>
    /// An exchange of GNU Taler whose coins a charging station operator accepts (concepts/EVTaler.md, section 4.2):
    /// an immutable value, identified by its master public key.
    /// </summary>
    public sealed class EVTalerExchange
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/evTalerExchange/v1";

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
            /// The property of its master public key.
            /// </summary>
            public const String  MasterPublicKey   = "masterPublicKey";

            /// <summary>
            /// The property of its base URL.
            /// </summary>
            public const String  BaseURL           = "baseURL";

            /// <summary>
            /// The property of the currency of its coins.
            /// </summary>
            public const String  Currency          = "currency";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name              = "name";

        }

        #endregion

        /// <summary>
        /// Its master public key, as GNU Taler writes it: what identifies it.
        /// </summary>
        public String               MasterPublicKey    { get; }

        /// <summary>
        /// Its base URL: HTTPS, ending in '/'.
        /// </summary>
        public URL                  BaseURL            { get; }

        /// <summary>
        /// The currency of its coins, an ISO 4217 code as of a charging tariff.
        /// </summary>
        public Currency             Currency           { get; }

        /// <summary>
        /// Its name, as drivers see it: the provider of "EV Taler (provider)".
        /// </summary>
        public ImmutableI18NString  Name               { get; }

        /// <summary>
        /// The payment option as drivers see it at an EVSE whose EV Taler tariff is in its currency, in the languages of
        /// its name, e.g. "EV Taler (Example Exchange, EUR)".
        /// </summary>
        public ImmutableI18NString  PaymentOptionName
            => new (Name.Select(name => new I18NPair(name.Language, $"EV Taler ({name.Text}, {Currency.ISOCode})")));

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create an exchange of GNU Taler a charging station operator accepts.
        /// </summary>
        /// <param name="MasterPublicKey">Its master public key, as GNU Taler writes it.</param>
        /// <param name="BaseURL">Its base URL: HTTPS, ending in '/'.</param>
        /// <param name="Currency">The currency of its coins.</param>
        /// <param name="Name">Its name, as drivers see it.</param>
        /// <exception cref="ArgumentException">A value is not one of an exchange.</exception>
        public EVTalerExchange(String               MasterPublicKey,
                               URL                  BaseURL,
                               Currency             Currency,
                               ImmutableI18NString  Name)
        {

            if (!EVTalerMerchant.IsTalerPublicKey(MasterPublicKey))
                throw new ArgumentException($"The master public key '{MasterPublicKey}' of an exchange is no public key as GNU Taler writes it!", nameof(MasterPublicKey));

            if (EVTalerMerchant.InvalidBaseURL(BaseURL, $"base URL of the exchange '{MasterPublicKey}'") is String baseURLError)
                throw new ArgumentException(baseURLError, nameof(BaseURL));

            if (Currency is null)
                throw new ArgumentException($"The exchange '{MasterPublicKey}' needs the currency of its coins!", nameof(Currency));

            if (Name.IsNullOrEmpty())
                throw new ArgumentException($"The exchange '{MasterPublicKey}' needs its name, as drivers see it!", nameof(Name));

            this.MasterPublicKey  = MasterPublicKey;
            this.BaseURL          = BaseURL;
            this.Currency         = Currency;
            this.Name             = Name;

        }

        #endregion


        #region ToJSON(Embedded = false, CustomEVTalerExchangeSerializer = null)

        /// <summary>
        /// Return the JSON representation of this exchange.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure, e.g. into a merchant.</param>
        /// <param name="CustomEVTalerExchangeSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                            Embedded                          = false,
                              CustomJObjectSerializerDelegate<EVTalerExchange>?  CustomEVTalerExchangeSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,        JSONLDContext);

            json.Add(PropertyKeys.MasterPublicKey,    MasterPublicKey);
            json.Add(PropertyKeys.BaseURL,            BaseURL.ToString());
            json.Add(PropertyKeys.Currency,           Currency.ISOCode);
            json.Add(PropertyKeys.Name,               Name.ToJSON());

            return CustomEVTalerExchangeSerializer is not null
                       ? CustomEVTalerExchangeSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out EVTalerExchange, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an exchange.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EVTalerExchange">The parsed exchange.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out EVTalerExchange?  EVTalerExchange,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            EVTalerExchange = null;

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of an exchange must be '{JSONLDContext}'!";
                return false;
            }

            if (JSON[PropertyKeys.MasterPublicKey] is not JToken masterPublicKeyJSON ||
                masterPublicKeyJSON.Type != JTokenType.String ||
                !EVTalerMerchant.IsTalerPublicKey(masterPublicKeyJSON.Value<String>()))
            {
                ErrorResponse = $"An exchange needs its master public key, as GNU Taler writes it, not '{JSON[PropertyKeys.MasterPublicKey]}'!";
                return false;
            }

            var masterPublicKey = masterPublicKeyJSON.Value<String>()!;

            if (!POIJSON.TryParseText(JSON, PropertyKeys.BaseURL, "base URL of an exchange", URL.TryParse, out URL? baseURL, out ErrorResponse))
                return false;

            if (!baseURL.HasValue)
            {
                ErrorResponse = $"The exchange '{masterPublicKey}' needs its base URL!";
                return false;
            }

            if (JSON[PropertyKeys.Currency] is not JToken currencyJSON ||
                currencyJSON.Type != JTokenType.String ||
                !Currency.TryParseISO(currencyJSON.Value<String>()!, out var currency))
            {
                ErrorResponse = $"The currency of the exchange '{masterPublicKey}' must be an ISO 4217 code, not '{JSON[PropertyKeys.Currency]}'!";
                return false;
            }

            if (!POIJSON.TryParseI18N(JSON, PropertyKeys.Name, "name of an exchange", out var name, out ErrorResponse))
                return false;

            try
            {
                EVTalerExchange = new EVTalerExchange(masterPublicKey,
                                                      baseURL.Value,
                                                      currency,
                                                      name ?? ImmutableI18NString.Empty);
            }
            catch (ArgumentException e)
            {
                ErrorResponse = e.Message;
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (internal static) NotWrittenAsRead(JSON, EVTalerExchange)

        /// <summary>
        /// What an exchange names, as TryParse reads it.
        /// </summary>
        private static readonly String[] ReadProperties = [ PropertyKeys.Context, PropertyKeys.MasterPublicKey, PropertyKeys.BaseURL, PropertyKeys.Currency, PropertyKeys.Name ];

        /// <summary>
        /// The error, when the given exchange, read from the given JSON, is not written as it was given: a property it does
        /// not read, which it leaves out, a base URL it writes otherwise, e.g. with its scheme, or a currency it writes by
        /// its code. Its merchant checks it, after all it refuses (SPECIFICATION.md, section 2, Written forms).
        /// </summary>
        internal static String? NotWrittenAsRead(JObject          JSON,
                                                 EVTalerExchange  EVTalerExchange)
        {

            if (JSON.Properties().FirstOrDefault(property => !ReadProperties.Contains(property.Name)) is JProperty unread)
                return $"The exchange '{EVTalerExchange.MasterPublicKey}' must not name the property '{unread.Name}'!";

            var baseURL = JSON[PropertyKeys.BaseURL]!.Value<String>();

            if (baseURL != EVTalerExchange.BaseURL.ToString())
                return $"The base URL '{baseURL}' of the exchange '{EVTalerExchange.MasterPublicKey}' is written as '{EVTalerExchange.BaseURL}'!";

            var currency = JSON[PropertyKeys.Currency]!.Value<String>();

            if (currency != EVTalerExchange.Currency.ISOCode)
                return $"The currency '{currency}' of the exchange '{EVTalerExchange.MasterPublicKey}' is written as '{EVTalerExchange.Currency.ISOCode}'!";

            return null;

        }

        #endregion

    }

}
