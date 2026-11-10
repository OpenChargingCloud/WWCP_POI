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

using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An EV Taler session at the offers server (concepts/EVTaler.md, section 6): its events, numbered, kept until the
    /// settlement and a while after it, and read again from any number; the secret that reads them, kept as its hash only;
    /// and the checks of every offer of the backend before it is relayed.
    /// </summary>
    public sealed class EVTalerOfferSession : IEVTalerSessionEvents
    {

        #region Data

        private readonly EVTalerOffersAPI                offersAPI;
        private readonly Byte[]                          secretHash;
        private readonly Lock                            sessionLock    = new ();
        private readonly List<JObject>                   events         = [];
        private          TaskCompletionSource            nextEvent      = new (TaskCreationOptions.RunContinuationsAsynchronously);

        private          EVTalerVerifiedStep?            lastStep;
        private          EVTalerSignedMeterValue?        startValue;
        private          Decimal                         offeredTotal;

        #endregion

        #region Properties

        /// <summary>
        /// The session: 128 random bits, base64url.
        /// </summary>
        public String                                       SessionId          { get; }

        /// <summary>
        /// What the driver asked for.
        /// </summary>
        public EVTalerSessionRequest                        Request            { get; }

        /// <summary>
        /// The charging station of the EVSE, as the server's version proved it when the session was asked for.
        /// </summary>
        public VerifiedChargingStation                      Station            { get; }

        /// <summary>
        /// The current price statements of the EVSE, its station and its pool, when the session was asked for.
        /// </summary>
        public IReadOnlyList<RoamingNetworkPriceStatement>  PriceStatements    { get; }

        /// <summary>
        /// The client that asked for it, by its address, for the limits.
        /// </summary>
        public String                                       Client             { get; }

        /// <summary>
        /// When it was asked for.
        /// </summary>
        public DateTimeOffset                               Created            { get; }

        /// <summary>
        /// When charging ended, if it did.
        /// </summary>
        public DateTimeOffset?                              StoppedAt          { get; private set; }

        /// <summary>
        /// When it was settled, if it was: its events are kept a while after it.
        /// </summary>
        public DateTimeOffset?                              SettledAt          { get; private set; }

        /// <summary>
        /// The number of its last event; 0 before the first.
        /// </summary>
        public UInt64 LastSequenceNumber
        {
            get
            {
                lock (sessionLock)
                {
                    return (UInt64) events.Count;
                }
            }
        }

        #endregion

        #region Constructor(s)

        internal EVTalerOfferSession(EVTalerOffersAPI                             OffersAPI,
                                     String                                       SessionId,
                                     String                                       Secret,
                                     EVTalerSessionRequest                        Request,
                                     VerifiedChargingStation                      Station,
                                     IReadOnlyList<RoamingNetworkPriceStatement>  PriceStatements,
                                     String                                       Client,
                                     DateTimeOffset                               Created)
        {

            this.offersAPI        = OffersAPI;
            this.SessionId        = SessionId;
            this.secretHash       = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Secret));
            this.Request          = Request;
            this.Station          = Station;
            this.PriceStatements  = PriceStatements;
            this.Client           = Client;
            this.Created          = Created;

        }

        #endregion


        #region IsSecret(Secret)

        /// <summary>
        /// Whether the given secret is the one of this session, compared in constant time with its hash.
        /// </summary>
        public Boolean IsSecret(String? Secret)

            => Secret is not null &&
               CryptographicOperations.FixedTimeEquals(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(Secret)), secretHash);

        #endregion

        #region Events(After)

        /// <summary>
        /// Its events after the given number, and a task that completes with the next one.
        /// </summary>
        public (IReadOnlyList<JObject> Events, Task Next) EventsAfter(UInt64 After)
        {
            lock (sessionLock)
            {
                return ((UInt64) events.Count > After
                            ? [.. events.Skip((Int32) Math.Min(After, Int32.MaxValue)).Select(json => (JObject) json.DeepClone())]
                            : [],
                        nextEvent.Task);
            }
        }

        #endregion

        #region (private) Add(Type, Properties)

        /// <summary>
        /// Add an event of the given type, numbered after the last one, and wake every reader waiting for it.
        /// </summary>
        private JObject Add(String              Type,
                            params JProperty[]  Properties)
        {

            TaskCompletionSource woken;
            JObject              json;

            lock (sessionLock)
            {

                json = new JObject(new JProperty("seq",  events.Count + 1),
                                   new JProperty("type", Type));

                foreach (var property in Properties)
                    json.Add(property);

                events.Add(json);

                woken      = nextEvent;
                nextEvent  = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            }

            woken.TrySetResult();

            return json;

        }

        #endregion


        #region IEVTalerSessionEvents

        #region Offer     (Offer)

        /// <summary>
        /// Check the offer against the version the server holds and relay it (concepts/EVTaler.md, section 6): the merchant
        /// and the exchanges of the operator's evTaler in the tariff's currency, among them the driver's; extra.wwcp naming
        /// this EVSE, this session, the step after the last one, the EV Taler tariff and the price statement the session
        /// began with, and from step 2 on the signed start value the station sent; the amount, the step at that price; and
        /// all offers together within the driver's cap. An offer failing a check is not relayed, and the session ends.
        /// </summary>
        public async Task<EVTalerOfferResult> Offer(EVTalerOffer Offer)
        {

            String? error;
            JObject? relayed = null;

            lock (sessionLock)
            {

                error = Check(Offer, out var verified);

                if (error is null)
                {

                    lastStep       = verified!;
                    offeredTotal  += verified!.Terms.Amount;

                    relayed        = OfferJSON(Offer, verified);

                }

            }

            if (error is null)
            {
                Add("offer", [.. relayed!.Properties()]);
                return new EVTalerOfferResult(true, null);
            }

            Add("refused",
                new JProperty("step",         Offer.Step),
                new JProperty("description",  error));

            await offersAPI.EndRefused(this, Offer.Step);

            return new EVTalerOfferResult(false, error);

        }

        #endregion

        #region Paid      (Step, OrderId)

        public Task Paid(UInt32  Step,
                         String  OrderId)
        {

            Add("paid",
                new JProperty("step",     Step),
                new JProperty("orderId",  OrderId));

            return Task.CompletedTask;

        }

        #endregion

        #region Unpaid    (Step, OrderId)

        public Task Unpaid(UInt32  Step,
                           String  OrderId)
        {

            Add("unpaid",
                new JProperty("step",     Step),
                new JProperty("orderId",  OrderId));

            return Task.CompletedTask;

        }

        #endregion

        #region MeterValue(Value, At)

        public Task MeterValue(EVTalerSignedMeterValue  Value,
                               String                   At)
        {

            if (At == EVTalerMeterValueAt.Start)
                lock (sessionLock)
                {
                    startValue ??= Value;
                }

            Add("meterValue",
                new JProperty("at",                At),
                new JProperty("signedMeterValue",  Value.ToJSON()));

            return Task.CompletedTask;

        }

        #endregion

        #region Stopped   (Reason, Description = null)

        public Task Stopped(String   Reason,
                            String?  Description   = null)
        {

            StoppedAt ??= offersAPI.TimeProvider.GetUtcNow();

            var properties = new List<JProperty> { new ("reason", Reason) };

            if (Description is not null)
                properties.Add(new JProperty("description", Description));

            Add("stopped", [.. properties]);

            offersAPI.Release(this);

            return Task.CompletedTask;

        }

        #endregion

        #region Settlement(Statement, Refunds)

        public Task Settlement(EVTalerSettlement?          Statement,
                               IEnumerable<EVTalerRefund>  Refunds)
        {

            StoppedAt ??= offersAPI.TimeProvider.GetUtcNow();
            SettledAt ??= offersAPI.TimeProvider.GetUtcNow();

            var properties = new List<JProperty>();

            if (Statement is not null)
                properties.Add(new JProperty("statement", Statement.JSON.DeepClone()));

            properties.Add(new JProperty("refunds", new JArray(Refunds.Select(refund => refund.ToJSON()))));

            Add("settlement", [.. properties]);

            offersAPI.Release(this);

            return Task.CompletedTask;

        }

        #endregion

        #endregion


        #region (private) Check(Offer, out Verified)

        private String? Check(EVTalerOffer              Offer,
                              out EVTalerVerifiedStep?  Verified)
        {

            Verified = null;

            if (StoppedAt.HasValue)
                return "The session has ended: no step is offered after it!";

            var expected = (lastStep?.Step.Step ?? 0) + 1;

            if (Offer.Step != expected)
                return $"Step {Offer.Step} is offered, not step {expected}, the one after the last!";

            if (!EVTalerContractTerms.TryParse(Offer.ContractTerms, out var terms, out var error))
                return $"The contract terms of the offer are invalid: {error}";

            if (terms.OrderId != Offer.OrderId)
                return $"The contract terms are of the order '{terms.OrderId}', not of '{Offer.OrderId}'!";

            if (!EVTalerPayURI.TryParse(Offer.TalerPayURI, out var payURI, out error))
                return $"The taler://pay URI of the offer is invalid: {error}";

            if (terms.Step.Step != Offer.Step)
                return $"The contract terms are of step {terms.Step.Step}, not of step {Offer.Step}!";

            if (terms.Step.SessionId != SessionId)
                return $"The contract terms are of the session '{terms.Step.SessionId}', not of this one!";

            if (terms.Step.EVSEId != Request.EVSEId.ToString())
                return $"The contract terms are of the EVSE '{terms.Step.EVSEId}', not of '{Request.EVSEId}'!";

            if (!terms.Exchanges.Any(exchange => exchange.MasterPublicKey == Request.Exchange))
                return $"The contract terms do not name the exchange '{Request.Exchange}' of the driver's coins!";

            if (Offer.Step > 1)
            {

                if (startValue is null)
                    return $"Step {Offer.Step} is offered before the station gave the signed meter value at the start!";

                if (!startValue.Equals(terms.Step.StartMeterValue))
                    return $"Step {Offer.Step} names another signed meter value at the start than the station gave!";

            }

            if (!terms.TryVerify(Station,
                                 PriceStatements,
                                 lastStep,
                                 offersAPI.MeterValueVerifier,
                                 out Verified,
                                 out error,
                                 payURI))
                return error;

            if (Request.Cap.HasValue && offeredTotal + terms.Amount > Request.Cap.Value)
                return $"The offers of the session would cost {EVTalerAmounts.Write(offeredTotal + terms.Amount, terms.Currency)} {terms.Currency}, more than the cap of {EVTalerAmounts.Write(Request.Cap.Value, terms.Currency)} {terms.Currency}!";

            return null;

        }

        #endregion

        #region (private static) OfferJSON(Offer, Verified)

        private static JObject OfferJSON(EVTalerOffer         Offer,
                                         EVTalerVerifiedStep  Verified)
        {

            var terms = Verified.Terms;

            return new JObject(new JProperty("step",           Offer.Step),
                               new JProperty("orderId",        Offer.OrderId),
                               new JProperty("talerPayURI",    Offer.TalerPayURI),
                               new JProperty("amount",         $"{terms.Currency}:{EVTalerAmounts.Write(terms.Amount, terms.Currency)}"),
                               new JProperty("covers",         Verified.Step.Covers.ToJSON()),
                               new JProperty("payDeadline",    terms.PayDeadline.ToISO8601()),
                               new JProperty("contractTerms",  Offer.ContractTerms.DeepClone()));

        }

        #endregion


        public override String ToString()
            => $"{SessionId} at {Request.EVSEId}";

    }

}
