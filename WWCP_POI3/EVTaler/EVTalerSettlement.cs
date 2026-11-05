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

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a reader established of a settlement statement (concepts/EVTaler.md, 5.4).
    /// </summary>
    /// <param name="Settlement">The settlement statement.</param>
    /// <param name="Start">The signed meter value at the start, verified.</param>
    /// <param name="End">The signed meter value at the end, verified.</param>
    /// <param name="Energy">The energy of the session, in kWh: the end value less the start value.</param>
    /// <param name="ComputedPrice">The price of the session by its rates, where they are of the simple profile.</param>
    /// <param name="PriceCheck">EVTalerAmountChecks.Exact when the price was recalculated, else Unsupported.</param>
    /// <param name="Diagnostics">What a reader should know.</param>
    public sealed record EVTalerSettlementVerification(EVTalerSettlement          Settlement,
                                                       EVTalerVerifiedMeterValue  Start,
                                                       EVTalerVerifiedMeterValue  End,
                                                       Decimal                    Energy,
                                                       Decimal?                   ComputedPrice,
                                                       String                     PriceCheck,
                                                       ImmutableArray<String>     Diagnostics);


    /// <summary>
    /// The settlement statement of an EV Taler session (concepts/EVTaler.md, 5.4; decision D5 (a)): signed by its
    /// charging station operator with a signing key that may sign payments, naming both signed meter values, the energy,
    /// the orders of its steps, the price, what was paid and what was refunded. No content: no history and no checkpoint
    /// takes it; the app gets it as the last event of its session and keeps it.
    /// </summary>
    public sealed class EVTalerSettlement
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of a settlement statement.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/evTalerSettlement/v1";

        /// <summary>
        /// The JSON keys.
        /// </summary>
        public static class PropertyKeys
        {

            public const String  Context            = "@context";
            public const String  RoamingNetworkId   = "roamingNetworkId";
            public const String  EVSEId             = "EVSEId";
            public const String  SessionId          = "sessionId";
            public const String  MerchantPublicKey  = "merchantPublicKey";
            public const String  ChargingTariffId   = "chargingTariffId";
            public const String  PriceStatement     = "priceStatement";
            public const String  Start              = "start";
            public const String  End                = "end";
            public const String  MeterValues        = "meterValues";
            public const String  Energy             = "energy";
            public const String  Currency           = "currency";
            public const String  Orders             = "orders";
            public const String  OrderId            = "orderId";
            public const String  Amount             = "amount";
            public const String  Price              = "price";
            public const String  Paid               = "paid";
            public const String  Refunded           = "refunded";
            public const String  Role               = "role";
            public const String  Signer             = "signer";
            public const String  SignedAt           = "signedAt";
            public const String  Algorithm          = "algorithm";
            public const String  KeyId              = "keyId";
            public const String  Value              = "value";

        }

        private static readonly String[] Known = [ PropertyKeys.Context,          PropertyKeys.RoamingNetworkId,  PropertyKeys.EVSEId,
                                                   PropertyKeys.SessionId,        PropertyKeys.MerchantPublicKey, PropertyKeys.ChargingTariffId,
                                                   PropertyKeys.PriceStatement,   PropertyKeys.Start,             PropertyKeys.End,
                                                   PropertyKeys.MeterValues,      PropertyKeys.Energy,            PropertyKeys.Currency,
                                                   PropertyKeys.Orders,           PropertyKeys.Price,             PropertyKeys.Paid,
                                                   PropertyKeys.Refunded,         PropertyKeys.Role,              PropertyKeys.Signer,
                                                   PropertyKeys.SignedAt,         PropertyKeys.Algorithm,         PropertyKeys.KeyId,
                                                   PropertyKeys.Value ];

        private static readonly String[] Texts = [ PropertyKeys.RoamingNetworkId, PropertyKeys.EVSEId,            PropertyKeys.SessionId,
                                                   PropertyKeys.MerchantPublicKey, PropertyKeys.ChargingTariffId, PropertyKeys.Currency,
                                                   PropertyKeys.Signer,           PropertyKeys.Algorithm,         PropertyKeys.KeyId,
                                                   PropertyKeys.Value ];

        #endregion

        #region Properties

        /// <summary>
        /// The JSON as signed.
        /// </summary>
        public JObject                                           JSON                 { get; }

        /// <summary>
        /// The roaming network.
        /// </summary>
        public String                                            RoamingNetworkId     => Text(PropertyKeys.RoamingNetworkId);

        /// <summary>
        /// The EVSE of the session.
        /// </summary>
        public String                                            EVSEId               => Text(PropertyKeys.EVSEId);

        /// <summary>
        /// The session at the offers server.
        /// </summary>
        public String                                            SessionId            => Text(PropertyKeys.SessionId);

        /// <summary>
        /// The public key of the merchant instance that took the steps, as in evTaler.
        /// </summary>
        public String                                            MerchantPublicKey    => Text(PropertyKeys.MerchantPublicKey);

        /// <summary>
        /// The EV Taler tariff of the session.
        /// </summary>
        public String                                            ChargingTariffId     => Text(PropertyKeys.ChargingTariffId);

        /// <summary>
        /// The digest of the price statement that priced it, if any.
        /// </summary>
        public String?                                           PriceStatement       => JSON[PropertyKeys.PriceStatement]?.Value<String>();

        /// <summary>
        /// When it started: the time of the signed meter value at the start.
        /// </summary>
        public DateTimeOffset                                    Start                { get; }

        /// <summary>
        /// When it ended: the time of the signed meter value at the end.
        /// </summary>
        public DateTimeOffset                                    End                  { get; }

        /// <summary>
        /// The signed meter values at the start and at the end, as the station sent them.
        /// </summary>
        public ImmutableArray<EVTalerSignedMeterValue>           MeterValues          { get; }

        /// <summary>
        /// The energy, in kWh.
        /// </summary>
        public Decimal                                           Energy               { get; }

        /// <summary>
        /// The currency of the amounts.
        /// </summary>
        public String                                            Currency             => Text(PropertyKeys.Currency);

        /// <summary>
        /// The orders of the steps, in their order, and what each took.
        /// </summary>
        public ImmutableArray<(String OrderId, Decimal Amount)>  Orders               { get; }

        /// <summary>
        /// The price of the session.
        /// </summary>
        public Decimal                                           Price                { get; }

        /// <summary>
        /// What the steps took together.
        /// </summary>
        public Decimal                                           Paid                 { get; }

        /// <summary>
        /// What was refunded: the paid amount less the price, not less than nothing.
        /// </summary>
        public Decimal                                           Refunded             { get; }

        /// <summary>
        /// The charging station operator signing it.
        /// </summary>
        public String                                            Signer               => Text(PropertyKeys.Signer);

        /// <summary>
        /// When it was signed.
        /// </summary>
        public DateTimeOffset                                    SignedAt             { get; }

        /// <summary>
        /// The signature algorithm.
        /// </summary>
        public String                                            Algorithm            => Text(PropertyKeys.Algorithm);

        /// <summary>
        /// The @id of the signing key.
        /// </summary>
        public String                                            KeyId                => Text(PropertyKeys.KeyId);

        /// <summary>
        /// The signature, Base64.
        /// </summary>
        public String                                            Value                => Text(PropertyKeys.Value);

        #endregion

        #region (private) Constructor(s)

        private EVTalerSettlement(JObject                                    JSON,
                                  DateTimeOffset                             Start,
                                  DateTimeOffset                             End,
                                  IEnumerable<EVTalerSignedMeterValue>       MeterValues,
                                  Decimal                                    Energy,
                                  IEnumerable<(String OrderId, Decimal Amount)>  Orders,
                                  Decimal                                    Price,
                                  Decimal                                    Paid,
                                  Decimal                                    Refunded,
                                  DateTimeOffset                             SignedAt)
        {

            this.JSON         = JSON;
            this.Start        = Start;
            this.End          = End;
            this.MeterValues  = [.. MeterValues];
            this.Energy       = Energy;
            this.Orders       = [.. Orders];
            this.Price        = Price;
            this.Paid         = Paid;
            this.Refunded     = Refunded;
            this.SignedAt     = SignedAt;

        }

        #endregion


        #region (static) Create(PrivateKey, KeyId, Algorithm, Signer, RoamingNetworkId, EVSEId, SessionId, MerchantPublicKey, ChargingTariffId, PriceStatement, Start, End, StartMeterValue, EndMeterValue, Energy, Currency, Orders, Price, …)

        /// <summary>
        /// Create and sign the settlement statement of a session, as its charging station operator does with a signing key
        /// that may sign payments: what was paid is the sum of the orders, what was refunded the paid amount less the
        /// price, not less than nothing.
        /// </summary>
        /// <param name="PrivateKey">The private signing key.</param>
        /// <param name="KeyId">The @id of the signing key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="Signer">The charging station operator.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="EVSEId">The EVSE of the session.</param>
        /// <param name="SessionId">The session at the offers server.</param>
        /// <param name="MerchantPublicKey">The public key of the operator's merchant instance.</param>
        /// <param name="ChargingTariffId">The EV Taler tariff.</param>
        /// <param name="PriceStatement">The digest of the price statement that priced the session, if any.</param>
        /// <param name="Start">The time of the signed meter value at the start.</param>
        /// <param name="End">The time of the signed meter value at the end.</param>
        /// <param name="StartMeterValue">The signed meter value at the start, as the station sent it.</param>
        /// <param name="EndMeterValue">The signed meter value at the end, as the station sent it.</param>
        /// <param name="Energy">The energy, in kWh.</param>
        /// <param name="Currency">The currency of the amounts.</param>
        /// <param name="Orders">The orders of the steps, in their order, and what each took.</param>
        /// <param name="Price">The price of the session.</param>
        /// <param name="SignedAt">When it is signed, now by default.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static EVTalerSettlement Create(AsymmetricKeyParameter                         PrivateKey,
                                               String                                         KeyId,
                                               COSEAlgorithm                                  Algorithm,
                                               ChargingStationOperator_Id                     Signer,
                                               RoamingNetwork_Id                              RoamingNetworkId,
                                               String                                         EVSEId,
                                               String                                         SessionId,
                                               String                                         MerchantPublicKey,
                                               String                                         ChargingTariffId,
                                               String?                                        PriceStatement,
                                               DateTimeOffset                                 Start,
                                               DateTimeOffset                                 End,
                                               EVTalerSignedMeterValue                        StartMeterValue,
                                               EVTalerSignedMeterValue                        EndMeterValue,
                                               Decimal                                        Energy,
                                               String                                         Currency,
                                               IEnumerable<(String OrderId, Decimal Amount)>  Orders,
                                               Decimal                                        Price,
                                               DateTimeOffset?                                SignedAt        = null,
                                               Boolean                                        Deterministic   = true)
        {

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            var orders  = Orders.ToArray();
            var paid    = orders.Sum(order => order.Amount);

            var json    = JSONObject.Create(
                              new JProperty(PropertyKeys.Context,            JSONLDContext),
                              new JProperty(PropertyKeys.RoamingNetworkId,   RoamingNetworkId.ToString()),
                              new JProperty(PropertyKeys.EVSEId,             EVSEId),
                              new JProperty(PropertyKeys.SessionId,          SessionId),
                              new JProperty(PropertyKeys.MerchantPublicKey,  MerchantPublicKey),
                              new JProperty(PropertyKeys.ChargingTariffId,   ChargingTariffId),
                              PriceStatement is not null
                                  ? new JProperty(PropertyKeys.PriceStatement,  PriceStatement)
                                  : null,
                              new JProperty(PropertyKeys.Start,              Start.ToISO8601()),
                              new JProperty(PropertyKeys.End,                End.  ToISO8601()),
                              new JProperty(PropertyKeys.MeterValues,        new JArray(StartMeterValue.ToJSON(), EndMeterValue.ToJSON())),
                              new JProperty(PropertyKeys.Energy,             EVTalerAmounts.WriteDecimal(Energy)),
                              new JProperty(PropertyKeys.Currency,           Currency),
                              new JProperty(PropertyKeys.Orders,             new JArray(orders.Select(order => new JObject(new JProperty(PropertyKeys.OrderId, order.OrderId),
                                                                                                                           new JProperty(PropertyKeys.Amount,  EVTalerAmounts.Write(order.Amount, Currency)))))),
                              new JProperty(PropertyKeys.Price,              EVTalerAmounts.Write(Price,                         Currency)),
                              new JProperty(PropertyKeys.Paid,               EVTalerAmounts.Write(paid,                          Currency)),
                              new JProperty(PropertyKeys.Refunded,           EVTalerAmounts.Write(Math.Max(0m, paid - Price),    Currency)),
                              new JProperty(PropertyKeys.Role,               RoamingNetworkCommitSignatureRoles.ChargingStationOperator),
                              new JProperty(PropertyKeys.Signer,             Signer.ToString()),
                              new JProperty(PropertyKeys.SignedAt,           (SignedAt ?? Timestamp.Now).ToISO8601()),
                              new JProperty(PropertyKeys.Algorithm,          Algorithm.Name),
                              new JProperty(PropertyKeys.KeyId,              KeyId)
                          );

            json.Add(PropertyKeys.Value, Convert.ToBase64String(Algorithm.Sign(CanonicalJSON.ToUTF8Bytes(json), PrivateKey, Deterministic: Deterministic)));

            return TryParse(json, out var settlement, out var error)
                       ? settlement
                       : throw new ArgumentException(error);

        }

        #endregion

        #region (static) TryParse(JSON, out Settlement, out ErrorResponse)

        /// <summary>
        /// Try to read the given settlement statement: its context and role, nothing it does not know, both signed meter
        /// values, at least one order, its decimals as texts, what was paid the sum of the orders and what was refunded the
        /// paid amount less the price, not less than nothing.
        /// </summary>
        public static Boolean TryParse(JToken?                                      JSON,
                                       [NotNullWhen(true)]  out EVTalerSettlement?  Settlement,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            Settlement = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "A settlement statement must be a JSON object!";
                return false;
            }

            if (json.Properties().FirstOrDefault(property => !Known.Contains(property.Name)) is JProperty unknown)
            {
                ErrorResponse = $"A settlement statement has no '{unknown.Name}'!";
                return false;
            }

            if (json[PropertyKeys.Context]?.Type != JTokenType.String || json[PropertyKeys.Context]!.Value<String>() != JSONLDContext ||
                json[PropertyKeys.Role]?.   Type != JTokenType.String || json[PropertyKeys.Role]!.   Value<String>() != RoamingNetworkCommitSignatureRoles.ChargingStationOperator)
            {
                ErrorResponse = "Unsupported context or role of a settlement statement!";
                return false;
            }

            var notText = Texts.FirstOrDefault(name => json[name]?.Type != JTokenType.String) ??
                          (json[PropertyKeys.PriceStatement] is not null && json[PropertyKeys.PriceStatement]!.Type != JTokenType.String
                               ? PropertyKeys.PriceStatement
                               : null);

            if (notText is not null)
            {
                ErrorResponse = $"A settlement statement names its '{notText}' as a text!";
                return false;
            }

            // No settlement without both signed meter values (5.4).
            if (json[PropertyKeys.MeterValues] is not JArray { Count: 2 } meterValuesJSON)
            {
                ErrorResponse = "A settlement statement names both signed meter values, at the start and at the end!";
                return false;
            }

            var meterValues = new List<EVTalerSignedMeterValue>();

            foreach (var meterValueJSON in meterValuesJSON)
            {

                if (!EVTalerSignedMeterValue.TryParse(meterValueJSON, out var meterValue, out var meterValueError))
                {
                    ErrorResponse = $"A signed meter value of a settlement statement is invalid: {meterValueError}";
                    return false;
                }

                meterValues.Add(meterValue);

            }

            if (!SessionPriceAgreement.TryTimestamp(json, PropertyKeys.Start,    out var start,    out ErrorResponse) ||
                !SessionPriceAgreement.TryTimestamp(json, PropertyKeys.End,      out var end,      out ErrorResponse) ||
                !SessionPriceAgreement.TryTimestamp(json, PropertyKeys.SignedAt, out var signedAt, out ErrorResponse))
            {
                return false;
            }

            if (!EVTalerAmounts.TryParseDecimal(json, PropertyKeys.Energy,   out var energy,   out ErrorResponse) ||
                !EVTalerAmounts.TryParseDecimal(json, PropertyKeys.Price,    out var price,    out ErrorResponse) ||
                !EVTalerAmounts.TryParseDecimal(json, PropertyKeys.Paid,     out var paid,     out ErrorResponse) ||
                !EVTalerAmounts.TryParseDecimal(json, PropertyKeys.Refunded, out var refunded, out ErrorResponse))
            {
                return false;
            }

            if (json[PropertyKeys.Orders] is not JArray { Count: > 0 } ordersJSON)
            {
                ErrorResponse = "A settlement statement names the orders of the steps of its session!";
                return false;
            }

            var orders = new List<(String, Decimal)>();

            foreach (var orderJSON in ordersJSON)
            {

                if (orderJSON is not JObject { Count: 2 } order ||
                    order[PropertyKeys.OrderId]?.Type != JTokenType.String ||
                    order[PropertyKeys.OrderId]!.Value<String>()!.Length == 0 ||
                    !EVTalerAmounts.TryParseDecimal(order, PropertyKeys.Amount, out var amount, out _))
                {
                    ErrorResponse = "An order of a settlement statement is { \"orderId\": …, \"amount\": … }, the amount a decimal as a text!";
                    return false;
                }

                orders.Add((order[PropertyKeys.OrderId]!.Value<String>()!, amount));

            }

            if (paid != orders.Sum(order => order.Item2) || refunded != Math.Max(0m, paid - price))
            {
                ErrorResponse = "What a settlement statement says was paid is the sum of its orders, and what was refunded the paid amount less the price!";
                return false;
            }

            Settlement     = new EVTalerSettlement(json, start, end, meterValues, energy, orders, price, paid, refunded, signedAt);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region GetSigningBytes()

        /// <summary>
        /// The signature input: the canonical JSON of what it names, but its value.
        /// </summary>
        public Byte[] GetSigningBytes()
        {

            var signed = (JObject) JSON.DeepClone();
            signed.Remove(PropertyKeys.Value);

            return CanonicalJSON.ToUTF8Bytes(signed);

        }

        #endregion

        #region TryVerify(Station, Steps, MeterValueVerifier, out Verification, out ErrorResponse)

        /// <summary>
        /// Verify this settlement statement against the verified charging station and the verified steps of its session
        /// (concepts/EVTaler.md, 5.4): signed by the operator with a key that may sign payments, valid when it was signed;
        /// of the merchant, EVSE, session, tariff and price statement of the steps, naming their orders and amounts; both
        /// signed meter values verified with one measurement key the proof gives, the start value the one the steps name,
        /// the end value after and not below it, the times and the energy theirs; and, where the prices are of the simple
        /// profile, the price recalculated.
        /// </summary>
        /// <param name="Station">The verified charging station.</param>
        /// <param name="Steps">The steps of the session, verified one after another.</param>
        /// <param name="MeterValueVerifier">A transparency software verifying signed meter values.</param>
        /// <param name="Verification">What was established.</param>
        /// <param name="ErrorResponse">Why it is refused.</param>
        public Boolean TryVerify(VerifiedChargingStation                                  Station,
                                 IReadOnlyList<EVTalerVerifiedStep>                       Steps,
                                 EVTalerMeterValueVerifier                                MeterValueVerifier,
                                 [NotNullWhen(true)]  out EVTalerSettlementVerification?  Verification,
                                 [NotNullWhen(false)] out String?                         ErrorResponse)
        {

            Verification = null;

            var diagnostics = new List<String>();

            #region Signed by the operator, with a key for payments

            if (RoamingNetworkId != Station.RoamingNetworkId.ToString() || Signer != VerifiedChargingStation.IdOf(Station.Operator))
            {
                ErrorResponse = "The settlement statement is not one of the operator of the station!";
                return false;
            }

            if (Station.OperatorSigningKeys.FirstOrDefault(key => key.Id == KeyId) is not POIPublicKey key ||
                !key.IsValidAt(SignedAt) ||
                !key.MaySign(POIKeyUsages.Payments))
            {
                ErrorResponse = "The settlement statement is not signed with a key of the operator for payments, valid when it was signed!";
                return false;
            }

            try
            {

                if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || algorithm.Name != Algorithm)
                {
                    ErrorResponse = "Unknown or noncanonical signature algorithm of the settlement statement!";
                    return false;
                }

                var value = Convert.FromBase64String(Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Value)
                {
                    ErrorResponse = "The signature of the settlement statement must be nonempty canonical Base64!";
                    return false;
                }

                if (!algorithm.Verify(GetSigningBytes(), value, key.Key.ToPublicKey(), out var signatureError))
                {
                    ErrorResponse = $"The signature of the settlement statement is invalid: {signatureError}";
                    return false;
                }

            }
            catch (Exception e)
            {
                ErrorResponse = $"The signature of the settlement statement is invalid: {e.Message}";
                return false;
            }

            #endregion

            #region The steps of the session

            if (Steps.Count == 0)
            {
                ErrorResponse = "A settlement statement is checked against the steps of its session!";
                return false;
            }

            for (var i = 0; i < Steps.Count; i++)
                if (Steps[i].Step.Step != i + 1 || (i > 0 && Steps[i].Step.PreviousOrderId != Steps[i - 1].Terms.OrderId))
                {
                    ErrorResponse = "The steps of a session are 1, 2, …, each naming the order of the one before!";
                    return false;
                }

            var first = Steps[0];

            if (MerchantPublicKey != first.Merchant.MerchantPublicKey ||
                EVSEId            != first.Step.EVSEId                ||
                SessionId         != first.Step.SessionId             ||
                ChargingTariffId  != first.Step.ChargingTariffId      ||
                PriceStatement    != first.Step.PriceStatement        ||
                Currency          != first.Currency)
            {
                ErrorResponse = "The settlement statement names another merchant, EVSE, session, tariff, price statement or currency than the steps of its session!";
                return false;
            }

            if (Orders.Length != Steps.Count ||
                Orders.Where((order, i) => order.OrderId != Steps[i].Terms.OrderId || order.Amount != Steps[i].Terms.Amount).Any())
            {
                ErrorResponse = "The settlement statement names the orders of the steps of its session, in their order, each with what it took!";
                return false;
            }

            #endregion

            #region Both signed meter values

            if (!EVTalerSignedMeterValue.TryVerify(Station, EVSEId, MeterValues[0], MeterValueVerifier, out var start, out ErrorResponse) ||
                !EVTalerSignedMeterValue.TryVerify(Station, EVSEId, MeterValues[1], MeterValueVerifier, out var end,   out ErrorResponse))
            {
                ErrorResponse = $"No settlement without both signed meter values: {ErrorResponse}";
                return false;
            }

            if (start.KeyId != end.KeyId)
            {
                ErrorResponse = "The signed meter values at the start and at the end are of two meters!";
                return false;
            }

            var named = Steps.Select(step => step.Step.StartMeterValue).FirstOrDefault(value => value is not null);

            if (named is not null && !named.Equals(MeterValues[0]))
            {
                ErrorResponse = "The signed meter value at the start is not the one the contract terms of the steps name!";
                return false;
            }

            if (end.Reading.Timestamp <= start.Reading.Timestamp || end.Reading.Energy < start.Reading.Energy)
            {
                ErrorResponse = "The signed meter value at the end is before or below the one at the start!";
                return false;
            }

            var energy = end.Reading.Energy - start.Reading.Energy;

            if (Start != start.Reading.Timestamp || End != end.Reading.Timestamp || Energy != energy)
            {
                ErrorResponse = "The start, the end and the energy of the settlement statement are not those of its signed meter values!";
                return false;
            }

            if (SignedAt < End)
            {
                ErrorResponse = "A settlement statement is signed after the end of its session!";
                return false;
            }

            #endregion

            #region The price

            Decimal? computed    = null;
            var      priceCheck  = EVTalerAmountChecks.Unsupported;

            if (first.Rates is null)
                diagnostics.Add("The price is not recalculated: the prices of the session are not of the simple profile.");

            else
            {

                computed = first.Rates.SessionPrice(energy, End - Start);

                if (Price != computed)
                {
                    ErrorResponse = $"The price of the session is {EVTalerAmounts.Write(computed.Value, Currency)} {Currency} by its prices, not {EVTalerAmounts.Write(Price, Currency)} {Currency}!";
                    return false;
                }

                priceCheck = EVTalerAmountChecks.Exact;

            }

            if (Price > Paid)
                diagnostics.Add("The price is more than was paid: what the credit did not cover is not collected (5.5).");

            #endregion

            Verification   = new EVTalerSettlementVerification(this, start, end, energy, computed, priceCheck, [.. diagnostics]);
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region (private) Text(Name)

        private String Text(String Name)

            => JSON[Name]!.Value<String>()!;

        #endregion

    }

}
