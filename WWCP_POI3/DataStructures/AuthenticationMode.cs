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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The types of RFID cards a charging pool reads.
    /// </summary>
    public enum RFIDCardTypes
    {
        MifareClassic,
        MifareDESFire,
        Calypso
    }


    /// <summary>
    /// How a driver authenticates at a charging pool, with the names of WWCP:
    /// an immutable value without an identification.
    /// </summary>
    /// <remarks>
    /// RFID may name the card types it reads and the brands whose cards it takes, its own, embedded completely;
    /// SMS and PhoneCall need the number to call, SMS optionally a station code.
    /// Other types are given by their name alone.
    /// </remarks>
    public sealed class AuthenticationMode : IEquatable<AuthenticationMode>
    {

        #region Types

        public const String FreeChargingType              = "Free Charging";
        public const String RFIDType                      = "RFID";
        public const String PINPADType                    = "PINPAD";
        public const String ISO15118_PLCType              = "ISO/IEC 15118 PLC";
        public const String ISO15118_AirType              = "ISO/IEC 15118 Over-the-Air";
        public const String REMOTEType                    = "REMOTE";
        public const String CreditCardType                = "CreditCard";
        public const String DebitCardType                 = "DebitCard";
        public const String PrepaidCardType               = "PrepaidCard";
        public const String NFCType                       = "NFC";
        public const String BluetoothType                 = "Bluetooth";
        public const String WLANType                      = "WLAN";
        public const String NoAuthenticationRequiredType  = "No authentication required";
        public const String DirectPaymentType             = "DirectPayment";
        public const String SMSType                       = "SMS";
        public const String PhoneCallType                 = "PhoneCall";

        #endregion

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/authenticationMode/v1";

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
            public const String  Context       = "@context";

            /// <summary>
            /// The property of its type.
            /// </summary>
            public const String  Type          = "type";

            /// <summary>
            /// The property of its RFID card types.
            /// </summary>
            public const String  CardTypes     = "cardTypes";

            /// <summary>
            /// The property of its brands, embedded completely.
            /// </summary>
            public const String  Brands        = "brands";

            /// <summary>
            /// The property of the identifications of its brands, which it does not take.
            /// </summary>
            public const String  BrandIds      = "brandIds";

            /// <summary>
            /// The property of its number to send an SMS to or to call.
            /// </summary>
            public const String  Number        = "number";

            /// <summary>
            /// The property of its station code.
            /// </summary>
            public const String  StationCode   = "stationCode";

        }

        #endregion


        /// <summary>
        /// The type, e.g. RFID.
        /// </summary>
        public String                        Type           { get; }

        /// <summary>
        /// The RFID card types it reads, in the order of their names.
        /// </summary>
        public ImmutableArray<RFIDCardTypes>  CardTypes      { get; }

        /// <summary>
        /// The brands whose RFID cards it takes: its own, embedded completely, in the order of their identifications.
        /// </summary>
        public POICollection<Brand_Id, Brand>  Brands         { get; }

        /// <summary>
        /// The number to send an SMS to or to call.
        /// </summary>
        public String?                        Number         { get; }

        /// <summary>
        /// The code of the station to send by SMS.
        /// </summary>
        public String?                        StationCode    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new authentication mode.
        /// </summary>
        /// <param name="Type">The type, e.g. RFID.</param>
        /// <param name="CardTypes">The RFID card types it reads.</param>
        /// <param name="Brands">The brands whose RFID cards it takes.</param>
        /// <param name="Number">The number to send an SMS to or to call.</param>
        /// <param name="StationCode">The code of the station to send by SMS.</param>
        public AuthenticationMode(String                       Type,
                                  IEnumerable<RFIDCardTypes>?  CardTypes     = null,
                                  IEnumerable<Brand>?          Brands        = null,
                                  String?                      Number        = null,
                                  String?                      StationCode   = null)
        {

            if (String.IsNullOrWhiteSpace(Type) || Type.Trim() != Type)
                throw new ArgumentException("The type of an authentication mode must be a text without surrounding whitespace!", nameof(Type));

            var cardTypes = (CardTypes ?? []).ToArray();

            if (cardTypes.Distinct().Count() != cardTypes.Length)
                throw new ArgumentException("An RFID card type must not be given twice!", nameof(CardTypes));

            this.Type         = Type;
            this.CardTypes    = [.. cardTypes.OrderBy(cardType => cardType.ToString(), StringComparer.Ordinal)];
            this.Brands       = new (Brands, brand => brand.Id, "brand", "an RFID authentication mode");
            this.Number       = Number;
            this.StationCode  = StationCode;

            if ((this.CardTypes.Length > 0 || this.Brands.Count > 0) && Type != RFIDType)
                throw new ArgumentException($"Only RFID names card types and brands, not '{Type}'!");

            if (Type is SMSType or PhoneCallType)
            {
                if (String.IsNullOrWhiteSpace(Number))
                    throw new ArgumentException($"The authentication mode '{Type}' needs its number!", nameof(Number));
            }
            else if (Number is not null)
                throw new ArgumentException($"Only SMS and PhoneCall name a number, not '{Type}'!", nameof(Number));

            if (StationCode is not null && Type != SMSType)
                throw new ArgumentException($"Only SMS names a station code, not '{Type}'!", nameof(StationCode));

        }

        #endregion


        #region Static definitions

        public static AuthenticationMode FreeCharging              { get; } = new (FreeChargingType);
        public static AuthenticationMode PINPAD                    { get; } = new (PINPADType);
        public static AuthenticationMode ISO15118_PLC              { get; } = new (ISO15118_PLCType);
        public static AuthenticationMode ISO15118_Air              { get; } = new (ISO15118_AirType);
        public static AuthenticationMode REMOTE                    { get; } = new (REMOTEType);
        public static AuthenticationMode CreditCard                { get; } = new (CreditCardType);
        public static AuthenticationMode DebitCard                 { get; } = new (DebitCardType);
        public static AuthenticationMode PrepaidCard               { get; } = new (PrepaidCardType);
        public static AuthenticationMode NFC                       { get; } = new (NFCType);
        public static AuthenticationMode Bluetooth                 { get; } = new (BluetoothType);
        public static AuthenticationMode WLAN                      { get; } = new (WLANType);
        public static AuthenticationMode NoAuthenticationRequired  { get; } = new (NoAuthenticationRequiredType);
        public static AuthenticationMode DirectPayment             { get; } = new (DirectPaymentType);

        /// <summary>
        /// RFID, optionally with the card types it reads and the brands whose cards it takes.
        /// </summary>
        public static AuthenticationMode RFID(IEnumerable<RFIDCardTypes>?  CardTypes  = null,
                                              IEnumerable<Brand>?          Brands     = null)

            => new (RFIDType, CardTypes, Brands);

        /// <summary>
        /// An SMS to the given number, optionally with the code of the station.
        /// </summary>
        public static AuthenticationMode SMS(String   Number,
                                             String?  StationCode   = null)

            => new (SMSType, Number: Number, StationCode: StationCode);

        /// <summary>
        /// A call of the given number.
        /// </summary>
        public static AuthenticationMode PhoneCall(String Number)

            => new (PhoneCallType, Number: Number);

        #endregion


        #region ToJSON(Embedded = false, CustomAuthenticationModeSerializer = null)

        /// <summary>
        /// Return the JSON representation of this authentication mode.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomAuthenticationModeSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                               Embedded                             = false,
                              CustomJObjectSerializerDelegate<AuthenticationMode>?  CustomAuthenticationModeSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,      JSONLDContext);

            json.Add(PropertyKeys.Type,             Type);

            if (CardTypes.Length > 0)
                json.Add(PropertyKeys.CardTypes,    new JArray(CardTypes.Select(cardType => cardType.ToString())));

            if (Brands.Count > 0)
                json.Add(PropertyKeys.Brands,       new JArray(Brands.Select(brand => brand.ToJSON(Embedded: true))));

            if (Number is not null)
                json.Add(PropertyKeys.Number,       Number);

            if (StationCode is not null)
                json.Add(PropertyKeys.StationCode,  StationCode);

            return CustomAuthenticationModeSerializer is not null
                       ? CustomAuthenticationModeSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out AuthenticationMode, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an authentication mode.
        /// </summary>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out AuthenticationMode?  AuthenticationMode,
                                       [NotNullWhen(false)] out String?              ErrorResponse)
        {

            AuthenticationMode = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the authentication mode must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseString(JSON, PropertyKeys.Type,         "authentication mode type",  out var type,         out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, PropertyKeys.CardTypes,    "RFID card type",            POIJSON.TryParseName, out ImmutableArray<RFIDCardTypes> cardTypes, out ErrorResponse) ||
                    !POIJSON.TryParseChildren<Brand>(JSON, PropertyKeys.Brands, PropertyKeys.BrandIds, "brands", Brand.TryParse, out var brands, out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.Number,       "number",                    out var number,       out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.StationCode,  "station code",              out var stationCode,  out ErrorResponse))
                {
                    return false;
                }

                if (type is null)
                {
                    ErrorResponse = "An authentication mode needs its type!";
                    return false;
                }

                AuthenticationMode  = new AuthenticationMode(type, cardTypes, brands, number, stationCode);
                ErrorResponse       = null;
                return true;

            }
            catch (Exception e)
            {
                AuthenticationMode  = null;
                ErrorResponse       = "Invalid authentication mode: " + e.Message;
                return false;
            }

        }

        #endregion


        #region IEquatable

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is AuthenticationMode authenticationMode &&
               Equals(authenticationMode);

        /// <summary>
        /// Compares two authentication modes by their JSON representations.
        /// </summary>
        public Boolean Equals(AuthenticationMode? AuthenticationMode)

            => AuthenticationMode is not null &&
               POIJSON.SameValue(ToJSON(Embedded: true), AuthenticationMode.ToJSON(Embedded: true));

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Type.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Type;

        #endregion

    }

}
