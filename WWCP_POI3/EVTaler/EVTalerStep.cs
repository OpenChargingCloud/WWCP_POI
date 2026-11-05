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
using System.Text.RegularExpressions;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a step of an EV Taler session pays for, counted from the start of the session: from, to, and the unit, kWh or
    /// minutes (concepts/EVTaler.md, 5.2).
    /// </summary>
    public sealed class EVTalerCovers : IEquatable<EVTalerCovers>
    {

        #region Data

        /// <summary>
        /// Energy, in kWh.
        /// </summary>
        public const String KWh      = "kWh";

        /// <summary>
        /// Time, in minutes.
        /// </summary>
        public const String Minutes  = "min";

        #endregion

        #region Properties

        /// <summary>
        /// Where the step begins, counted from the start of the session.
        /// </summary>
        public Decimal  From    { get; }

        /// <summary>
        /// Where it ends.
        /// </summary>
        public Decimal  To      { get; }

        /// <summary>
        /// The unit: "kWh" or "min".
        /// </summary>
        public String   Unit    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create what a step covers: from less than to, neither negative, in kWh or minutes.
        /// </summary>
        public EVTalerCovers(Decimal  From,
                             Decimal  To,
                             String   Unit)
        {

            if (From < 0 || To <= From)
                throw new ArgumentException($"A step covers from {From} to {To}: from not negative and less than to!");

            if (Unit is not KWh and not Minutes)
                throw new ArgumentException($"A step covers kWh or minutes, not '{Unit}'!", nameof(Unit));

            this.From  = From;
            this.To    = To;
            this.Unit  = Unit;

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return a JSON representation: the decimals as texts.
        /// </summary>
        public JObject ToJSON()

            => new (new JProperty("from",  EVTalerAmounts.WriteDecimal(From)),
                    new JProperty("to",    EVTalerAmounts.WriteDecimal(To)),
                    new JProperty("unit",  Unit));

        #endregion

        #region (static) TryParse(JSON, out Covers, out ErrorResponse)

        /// <summary>
        /// Try to read what a step covers.
        /// </summary>
        public static Boolean TryParse(JToken?                                  JSON,
                                       [NotNullWhen(true)]  out EVTalerCovers?  Covers,
                                       [NotNullWhen(false)] out String?         ErrorResponse)
        {

            Covers = null;

            if (JSON is not JObject json ||
                json.Properties().Any(property => property.Name is not "from" and not "to" and not "unit"))
            {
                ErrorResponse = "What a step covers is a JSON object of from, to and unit!";
                return false;
            }

            if (!EVTalerAmounts.TryParseDecimal(json, "from", out var from, out ErrorResponse) ||
                !EVTalerAmounts.TryParseDecimal(json, "to",   out var to,   out ErrorResponse))
            {
                return false;
            }

            var unit = json["unit"]?.Type == JTokenType.String ? json["unit"]!.Value<String>() : null;

            if (unit is not KWh and not Minutes || to <= from)
            {
                ErrorResponse = "A step covers from less than to, in 'kWh' or 'min'!";
                return false;
            }

            Covers         = new EVTalerCovers(from, to, unit);
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region Equals / GetHashCode / ToString

        /// <inheritdoc/>
        public Boolean Equals(EVTalerCovers? Other)
            => Other is not null && From == Other.From && To == Other.To && Unit == Other.Unit;

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)
            => Equals(Object as EVTalerCovers);

        /// <inheritdoc/>
        public override Int32 GetHashCode()
            => HashCode.Combine(From, To, Unit);

        /// <inheritdoc/>
        public override String ToString()
            => $"{EVTalerAmounts.WriteDecimal(From)} to {EVTalerAmounts.WriteDecimal(To)} {Unit}";

        #endregion

    }


    /// <summary>
    /// A step of an EV Taler session in WWCP terms, the object of "extra": { "wwcp": … } of its order, signed by the
    /// merchant with the rest of the contract terms (concepts/EVTaler.md, 5.2): where, which session, which step after
    /// which order, the EV Taler tariff and the price statement, what it covers, and from step 2 on the signed meter value
    /// at the start of the session.
    /// </summary>
    public sealed partial class EVTalerStep
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of a step.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/evTalerStep/v1";

        [GeneratedRegex("^[A-Za-z0-9_-]{22}$")]
        private static partial Regex SessionIdText();

        [GeneratedRegex("^[0-9a-f]{64}$")]
        private static partial Regex DigestText();

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// The JSON keys.
        /// </summary>
        public static class PropertyKeys
        {

            public const String  Context            = "@context";
            public const String  RoamingNetworkId   = "roamingNetworkId";
            public const String  EVSEId             = "EVSEId";
            public const String  SessionId          = "sessionId";
            public const String  Step               = "step";
            public const String  PreviousOrderId    = "previousOrderId";
            public const String  ChargingTariffId   = "chargingTariffId";
            public const String  PriceStatement     = "priceStatement";
            public const String  Covers             = "covers";
            public const String  StartMeterValue    = "startMeterValue";

        }

        private static readonly String[] Known = [ PropertyKeys.Context,          PropertyKeys.RoamingNetworkId, PropertyKeys.EVSEId,
                                                   PropertyKeys.SessionId,        PropertyKeys.Step,             PropertyKeys.PreviousOrderId,
                                                   PropertyKeys.ChargingTariffId, PropertyKeys.PriceStatement,   PropertyKeys.Covers,
                                                   PropertyKeys.StartMeterValue ];

        #endregion

        #region Properties

        /// <summary>
        /// The roaming network.
        /// </summary>
        public RoamingNetwork_Id         RoamingNetworkId    { get; }

        /// <summary>
        /// The EVSE, as the roaming network writes it.
        /// </summary>
        public String                    EVSEId              { get; }

        /// <summary>
        /// The session at the offers server: 128 random bits, base64url. Not GNU Taler's session_id of an order.
        /// </summary>
        public String                    SessionId           { get; }

        /// <summary>
        /// The number of the step: 1, 2, …
        /// </summary>
        public UInt32                    Step                { get; }

        /// <summary>
        /// The order_id of the step before; none in step 1.
        /// </summary>
        public String?                   PreviousOrderId     { get; }

        /// <summary>
        /// The EV Taler tariff of the EVSE (4.3).
        /// </summary>
        public String                    ChargingTariffId    { get; }

        /// <summary>
        /// The digest of the price statement whose elements price the session; none when the tariff's own do.
        /// </summary>
        public String?                   PriceStatement      { get; }

        /// <summary>
        /// What the step pays for.
        /// </summary>
        public EVTalerCovers             Covers              { get; }

        /// <summary>
        /// From step 2 on: the signed meter value at the start of the session, as the station sent it.
        /// </summary>
        public EVTalerSignedMeterValue?  StartMeterValue     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a step of an EV Taler session.
        /// </summary>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="EVSEId">The EVSE, as the roaming network writes it.</param>
        /// <param name="SessionId">The session at the offers server: 128 bits, base64url, 22 characters.</param>
        /// <param name="Step">The number of the step, from 1 on.</param>
        /// <param name="PreviousOrderId">The order_id of the step before; none in step 1, mandatory after it.</param>
        /// <param name="ChargingTariffId">The EV Taler tariff.</param>
        /// <param name="PriceStatement">The digest of the price statement, if one prices the session.</param>
        /// <param name="Covers">What the step pays for; from 0 in step 1.</param>
        /// <param name="StartMeterValue">The signed meter value at the start; none in step 1, mandatory after it.</param>
        public EVTalerStep(RoamingNetwork_Id         RoamingNetworkId,
                           String                    EVSEId,
                           String                    SessionId,
                           UInt32                    Step,
                           String?                   PreviousOrderId,
                           String                    ChargingTariffId,
                           String?                   PriceStatement,
                           EVTalerCovers             Covers,
                           EVTalerSignedMeterValue?  StartMeterValue)
        {

            this.RoamingNetworkId  = RoamingNetworkId;
            this.EVSEId            = EVSEId;
            this.SessionId         = SessionId;
            this.Step              = Step;
            this.PreviousOrderId   = PreviousOrderId;
            this.ChargingTariffId  = ChargingTariffId;
            this.PriceStatement    = PriceStatement;
            this.Covers            = Covers;
            this.StartMeterValue   = StartMeterValue;

            var error = Invalid();

            if (error is not null)
                throw new ArgumentException(error);

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON of "extra": { "wwcp": … }.
        /// </summary>
        public JObject ToJSON()

            => JSONObject.Create(
                   new JProperty(PropertyKeys.Context,           JSONLDContext),
                   new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString()),
                   new JProperty(PropertyKeys.EVSEId,            EVSEId),
                   new JProperty(PropertyKeys.SessionId,         SessionId),
                   new JProperty(PropertyKeys.Step,              Step),
                   PreviousOrderId is not null
                       ? new JProperty(PropertyKeys.PreviousOrderId,  PreviousOrderId)
                       : null,
                   new JProperty(PropertyKeys.ChargingTariffId,  ChargingTariffId),
                   PriceStatement is not null
                       ? new JProperty(PropertyKeys.PriceStatement,   PriceStatement)
                       : null,
                   new JProperty(PropertyKeys.Covers,            Covers.ToJSON()),
                   StartMeterValue is not null
                       ? new JProperty(PropertyKeys.StartMeterValue,  StartMeterValue.ToJSON())
                       : null
               );

        #endregion

        #region (static) TryParse(JSON, out Step, out ErrorResponse)

        /// <summary>
        /// Try to read a step of an EV Taler session, the object of "extra": { "wwcp": … }: its context, nothing it does
        /// not know, and the rules of the constructor.
        /// </summary>
        public static Boolean TryParse(JToken?                                JSON,
                                       [NotNullWhen(true)]  out EVTalerStep?  Step,
                                       [NotNullWhen(false)] out String?       ErrorResponse)
        {

            Step = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "A step of an EV Taler session must be a JSON object!";
                return false;
            }

            if (json.Properties().FirstOrDefault(property => !Known.Contains(property.Name)) is JProperty unknown)
            {
                ErrorResponse = $"A step of an EV Taler session has no '{unknown.Name}'!";
                return false;
            }

            if (json[PropertyKeys.Context]?.Type != JTokenType.String || json[PropertyKeys.Context]!.Value<String>() != JSONLDContext)
            {
                ErrorResponse = $"The JSON-LD context of a step of an EV Taler session must be '{JSONLDContext}'!";
                return false;
            }

            if (new[] { PropertyKeys.RoamingNetworkId, PropertyKeys.EVSEId, PropertyKeys.SessionId, PropertyKeys.ChargingTariffId }.
                    FirstOrDefault(name => json[name]?.Type != JTokenType.String) is String missing ||
                new[] { PropertyKeys.PreviousOrderId, PropertyKeys.PriceStatement }.
                    FirstOrDefault(name => json[name] is not null && json[name]!.Type != JTokenType.String) is String notText)
            {
                ErrorResponse = "A step of an EV Taler session names its roaming network, EVSE, session and charging tariff, and maybe its previous order and its price statement, as texts!";
                return false;
            }

            if (!RoamingNetwork_Id.TryParse(json[PropertyKeys.RoamingNetworkId]!.Value<String>()!, out var roamingNetworkId))
            {
                ErrorResponse = $"Invalid roaming network '{json[PropertyKeys.RoamingNetworkId]}' of a step!";
                return false;
            }

            if (json[PropertyKeys.Step] is not JValue { Type: JTokenType.Integer } stepJSON ||
                stepJSON.Value<Int64>() is < 1 or > UInt32.MaxValue)
            {
                ErrorResponse = "The number of a step is an integer from 1 on!";
                return false;
            }

            if (!EVTalerCovers.TryParse(json[PropertyKeys.Covers], out var covers, out ErrorResponse))
                return false;

            EVTalerSignedMeterValue? startMeterValue = null;

            if (json[PropertyKeys.StartMeterValue] is JToken startJSON &&
                !EVTalerSignedMeterValue.TryParse(startJSON, out startMeterValue, out ErrorResponse))
            {
                ErrorResponse = $"The start meter value of a step is invalid: {ErrorResponse}";
                return false;
            }

            var step = new EVTalerStep(roamingNetworkId,
                                       json[PropertyKeys.EVSEId]!.          Value<String>()!,
                                       json[PropertyKeys.SessionId]!.       Value<String>()!,
                                       (UInt32) stepJSON.Value<Int64>(),
                                       json[PropertyKeys.PreviousOrderId]?. Value<String>(),
                                       json[PropertyKeys.ChargingTariffId]!.Value<String>()!,
                                       json[PropertyKeys.PriceStatement]?.  Value<String>(),
                                       covers,
                                       startMeterValue,
                                       Checked: false);

            ErrorResponse = step.Invalid();

            if (ErrorResponse is not null)
                return false;

            Step = step;
            return true;

        }

        // Read as written: the checks of the public constructor are made by TryParse.
        private EVTalerStep(RoamingNetwork_Id         RoamingNetworkId,
                            String                    EVSEId,
                            String                    SessionId,
                            UInt32                    Step,
                            String?                   PreviousOrderId,
                            String                    ChargingTariffId,
                            String?                   PriceStatement,
                            EVTalerCovers             Covers,
                            EVTalerSignedMeterValue?  StartMeterValue,
                            Boolean                   Checked)
        {

            this.RoamingNetworkId  = RoamingNetworkId;
            this.EVSEId            = EVSEId;
            this.SessionId         = SessionId;
            this.Step              = Step;
            this.PreviousOrderId   = PreviousOrderId;
            this.ChargingTariffId  = ChargingTariffId;
            this.PriceStatement    = PriceStatement;
            this.Covers            = Covers;
            this.StartMeterValue   = StartMeterValue;

        }

        #endregion


        #region (static) IsSessionId(Text)

        /// <summary>
        /// Whether the given text is a session of the offers server: 128 bits, canonical base64url without padding.
        /// </summary>
        public static Boolean IsSessionId([NotNullWhen(true)] String? Text)
        {

            if (Text is null || !SessionIdText().IsMatch(Text))
                return false;

            var bytes = Convert.FromBase64String(Text.Replace('-', '+').Replace('_', '/') + "==");

            return bytes.Length == 16 &&
                   Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_') == Text;

        }

        #endregion

        #region (private) Invalid()

        private String? Invalid()
        {

            if (!EVSE_Id.TryParse(EVSEId, out _))
                return $"Invalid EVSE '{EVSEId}' of a step!";

            if (!IsSessionId(SessionId))
                return "The session of a step is 128 bits, base64url without padding: 22 characters!";

            if (Step < 1)
                return "The number of a step is from 1 on!";

            if ((Step == 1) != (PreviousOrderId is null))
                return "Every step after the first one, and only those, names the order of the step before!";

            if (PreviousOrderId is not null && PreviousOrderId.Trim().Length == 0)
                return "The previous order of a step must be a nonempty text!";

            if (!ChargingTariff_Id.TryParse(ChargingTariffId, out _))
                return $"Invalid charging tariff '{ChargingTariffId}' of a step!";

            if (PriceStatement is not null && !DigestText().IsMatch(PriceStatement))
                return "The price statement of a step is its digest: 64 lowercase hexadecimal digits!";

            if (Step == 1 && Covers.From != 0)
                return "Step 1 covers from the start of the session, from 0!";

            if ((Step == 1) != (StartMeterValue is null))
                return "Every step after the first one, and only those, names the signed meter value at the start of the session!";

            return null;

        }

        #endregion

    }

}
