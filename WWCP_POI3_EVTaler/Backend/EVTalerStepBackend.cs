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

using System.Collections.Concurrent;
using System.Threading.Channels;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The backend of an operator for EV Taler sessions (concepts/EVTaler.md, sections 5 and 6), as its charging station
    /// management system runs it (decision D7): it makes the steps of a session as orders at the operator's merchant
    /// instance, waits for their payment, starts charging limited to the credit and raises the limit with every step paid,
    /// offers the next step before the credit runs out, and settles the session by the signed meter values at its start and
    /// its end: it refunds what was paid beyond the price, from the last step back, and signs a settlement statement with a
    /// key for payments. Without both signed meter values every step is refunded in full.
    /// </summary>
    /// <remarks>
    /// It makes steps of sessions priced as the simple profile of EVTalerRates prices them: one element of ENERGY, TIME and
    /// FLAT components, no restrictions, no minimum or maximum price. A session of other prices is not accepted (422).
    /// Sessions are kept in memory: a restart of the backend loses them, and their orders are settled by hand.
    /// </remarks>
    public sealed class EVTalerStepBackend : IEVTalerBackend,
                                             IEVTalerChargingEvents
    {

        #region Data

        /// <summary>
        /// The step sizes accepted by default.
        /// </summary>
        public static readonly IReadOnlyList<EVTalerStepSize> DefaultStepSizes = [
                                                                                     new (1,  EVTalerCovers.KWh),
                                                                                     new (2,  EVTalerCovers.KWh),
                                                                                     new (5,  EVTalerCovers.KWh),
                                                                                     new (10, EVTalerCovers.KWh),
                                                                                     new (20, EVTalerCovers.KWh),
                                                                                     new (50, EVTalerCovers.KWh),
                                                                                     new (5,  EVTalerCovers.Minutes),
                                                                                     new (15, EVTalerCovers.Minutes),
                                                                                     new (30, EVTalerCovers.Minutes),
                                                                                     new (60, EVTalerCovers.Minutes)
                                                                                 ];

        private readonly ConcurrentDictionary<String, BackendSession>  sessions = new (StringComparer.Ordinal);
        private readonly AsymmetricKeyParameter                        settlementKey;

        #endregion

        #region Properties

        /// <summary>
        /// The roaming network it makes offers in.
        /// </summary>
        public RoamingNetwork_Id               RoamingNetworkId       { get; }

        /// <summary>
        /// The operator it makes offers for.
        /// </summary>
        public ChargingStationOperator_Id      OperatorId             { get; }

        /// <summary>
        /// The client of the operator's merchant instance.
        /// </summary>
        public TalerMerchantClient             Merchant               { get; }

        /// <summary>
        /// Charging at the operator's charging station management system.
        /// </summary>
        public IEVTalerChargingControl         ChargingControl        { get; }

        /// <summary>
        /// A transparency software reading the signed meter values the settlement prices a session by.
        /// </summary>
        public EVTalerMeterValueVerifier       MeterValueVerifier     { get; }

        /// <summary>
        /// The @id of the signing key of the operator that signs settlement statements: one that may sign "payments".
        /// </summary>
        public String                          SettlementKeyId        { get; }

        /// <summary>
        /// The algorithm settlement statements are signed with.
        /// </summary>
        public COSEAlgorithm                   SettlementAlgorithm    { get; }

        /// <summary>
        /// The step sizes it accepts; of those, the ones of the dimension the tariff of an EVSE prices.
        /// </summary>
        public IReadOnlyList<EVTalerStepSize>  StepSizes              { get; set; } = DefaultStepSizes;

        /// <summary>
        /// The reserve step 1 carries, for what may come after the last step (concepts/EVTaler.md, 5.5); refunded at the
        /// settlement as far as it is not used.
        /// </summary>
        public Decimal                         Reserve                { get; set; }

        /// <summary>
        /// How long step 1 waits for its payment: the driver stands at the charger.
        /// </summary>
        public TimeSpan                        FirstPayDeadline       { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// How long a later step waits for its payment.
        /// </summary>
        public TimeSpan                        NextPayDeadline        { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// The next step is offered when this share of the last step paid is delivered.
        /// </summary>
        public Decimal                         NextStepAt             { get; set; } = 0.8m;

        /// <summary>
        /// How long after its payment deadline a step may be refunded: after the latest end of a session plus the time its
        /// settlement takes.
        /// </summary>
        public TimeSpan                        RefundDelay            { get; set; } = TimeSpan.FromDays(2);

        /// <summary>
        /// How long the wallet looks for a refund by itself.
        /// </summary>
        public TimeSpan                        AutoRefund             { get; set; } = TimeSpan.FromDays(2);

        /// <summary>
        /// How long after the merchant backend failed to make a step it is asked again.
        /// </summary>
        public TimeSpan                        RetryStep              { get; set; } = TimeSpan.FromSeconds(10);

        /// <summary>
        /// How long after the deadline of a step the merchant backend is still asked whether it was paid, when it does not
        /// say: a step is unpaid when the merchant backend says so after its deadline; one it still does not say anything
        /// of after this time is taken as unpaid, and reported.
        /// </summary>
        public TimeSpan                        UnknownPaymentWait     { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>
        /// The time provider.
        /// </summary>
        public TimeProvider                    TimeProvider           { get; }

        #endregion

        #region Events

        /// <summary>
        /// What went wrong in a session, for the operator's log: a merchant backend not answering, a refund not granted, …;
        /// the session, or the EVSE where none is made yet, e.g. one whose web payments are not known.
        /// </summary>
        public event Action<String, String>?   OnError;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create the backend of the given operator for EV Taler sessions.
        /// </summary>
        /// <param name="RoamingNetworkId">The roaming network it makes offers in.</param>
        /// <param name="OperatorId">The operator it makes offers for.</param>
        /// <param name="Merchant">The client of the operator's merchant instance.</param>
        /// <param name="ChargingControl">Charging at the operator's charging station management system.</param>
        /// <param name="MeterValueVerifier">A transparency software reading signed meter values.</param>
        /// <param name="SettlementKey">The private signing key of the operator that signs settlement statements.</param>
        /// <param name="SettlementKeyId">Its @id: of a key that may sign "payments".</param>
        /// <param name="SettlementAlgorithm">The algorithm settlement statements are signed with.</param>
        /// <param name="TimeProvider">The time provider.</param>
        public EVTalerStepBackend(RoamingNetwork_Id           RoamingNetworkId,
                                  ChargingStationOperator_Id  OperatorId,
                                  TalerMerchantClient         Merchant,
                                  IEVTalerChargingControl     ChargingControl,
                                  EVTalerMeterValueVerifier   MeterValueVerifier,
                                  AsymmetricKeyParameter      SettlementKey,
                                  String                      SettlementKeyId,
                                  COSEAlgorithm               SettlementAlgorithm,
                                  TimeProvider?               TimeProvider   = null)
        {

            if (!SettlementKey.IsPrivate)
                throw new ArgumentException("Settlement statements are signed with a private key!", nameof(SettlementKey));

            this.RoamingNetworkId     = RoamingNetworkId;
            this.OperatorId           = OperatorId;
            this.Merchant             = Merchant;
            this.ChargingControl      = ChargingControl;
            this.MeterValueVerifier   = MeterValueVerifier;
            this.settlementKey        = SettlementKey;
            this.SettlementKeyId      = SettlementKeyId;
            this.SettlementAlgorithm  = SettlementAlgorithm;
            this.TimeProvider         = TimeProvider ?? TimeProvider.System;

        }

        #endregion


        #region IEVTalerBackend

        #region Serves      (RoamingNetworkId, OperatorId)

        public Boolean Serves(RoamingNetwork_Id           RoamingNetworkId,
                              ChargingStationOperator_Id  OperatorId)

            => RoamingNetworkId == this.RoamingNetworkId &&
               OperatorId       == this.OperatorId;

        #endregion

        #region IsCurrentTOTP(EVSEId, TOTP, CancellationToken)

        public async Task<Boolean> IsCurrentTOTP(EVSE_Id            EVSEId,
                                                 String             TOTP,
                                                 CancellationToken  CancellationToken)
        {

            if (EVSEId.OperatorId != OperatorId)
                return false;

            var webPayments = await ChargingControl.WebPaymentsOf(EVSEId, CancellationToken);

            if (webPayments is null)
            {

                try
                {
                    OnError?.Invoke(EVSEId.ToString(), $"The EVSE '{EVSEId}' gives no one-time password: no web payments of it are known, and no EV Taler session is made there!");
                }
                catch
                { }

                return false;

            }

            return webPayments.IsCurrent(TOTP, TimeProvider.GetUtcNow());

        }

        #endregion

        #region StartSession(Session, CancellationToken)

        public async Task<EVTalerStartResult> StartSession(EVTalerSessionContext  Session,
                                                           CancellationToken      CancellationToken)
        {

            var request = Session.Request;
            var evseId  = request.EVSEId.ToString();

            if (!Serves(request.RoamingNetworkId, request.EVSEId.OperatorId))
                return EVTalerStartResult.Failed($"No sessions are made here for the operator '{request.EVSEId.OperatorId}'!");

            #region The merchant, the tariff and the prices of the session

            if (Session.Station.Operator[ChargingStationOperator.PropertyKeys.EVTaler] is not JObject merchantJSON ||
                !EVTalerMerchant.TryParse(merchantJSON, out var merchant, out _))
                return EVTalerStartResult.Failed($"The proof gives no EV Taler merchant of the operator '{OperatorId}'!");

            if (!EVTalerContractTerms.TryGetSessionPrices(Session.Station, evseId, Session.PriceStatements, Session.StartedAt, out var tariff, out var priceStatement, out var elements, out var pricesError))
                return EVTalerStartResult.NotAccepted(pricesError);

            var currency = tariff[ChargingTariff.PropertyKeys.Currency]!.Value<String>()!;

            if (!EVTalerRates.TryOf(currency, tariff, elements, out var rates, out var reason))
                return EVTalerStartResult.NotAccepted($"The session is not priced as the steps of this backend are: {reason}!");

            #endregion

            #region The step and the exchange

            var accepted = StepSizes.Where(step => (step.Unit == EVTalerCovers.KWh     && rates.EnergyPerKWh.HasValue) ||
                                                   (step.Unit == EVTalerCovers.Minutes && rates.TimePerHour. HasValue)).
                                     ToArray();

            if (!accepted.Contains(request.Step))
                return EVTalerStartResult.NotAccepted($"Steps of {request.Step} are not accepted at the EVSE '{evseId}'!", accepted);

            if (!merchant.Exchanges.Any(exchange => exchange.MasterPublicKey == request.Exchange && exchange.Currency.ISOCode == currency))
                return EVTalerStartResult.NotAccepted($"The exchange '{request.Exchange}' is not accepted in '{currency}'!");

            #endregion

            #region The EVSE takes a session now

            String? busy;

            try
            {
                busy = await ChargingControl.WhyNotStart(request.EVSEId, CancellationToken);
            }
            catch (Exception e)
            {
                return EVTalerStartResult.Failed($"The charging station management system failed: {e.Message}");
            }

            if (busy is not null)
                return EVTalerStartResult.Busy(busy);

            #endregion

            #region Step 1

            var session = new BackendSession(this,
                                              Session,
                                              merchant,
                                              tariff[RoamingNetwork.BasePropertyKeys.Id]?.Value<String>() ?? "",
                                              priceStatement?.Digest,
                                              currency,
                                              rates);

            if (!sessions.TryAdd(Session.SessionId, session))
                return EVTalerStartResult.Failed($"The session '{Session.SessionId}' is known already!");

            session.Run();

            var result = await session.Enqueue(() => MakeStep(session));

            if (result is not null)
            {
                sessions.TryRemove(Session.SessionId, out _);
                session.Complete();
                return result;
            }

            return EVTalerStartResult.Success();

            #endregion

        }

        #endregion

        #region StopSession (SessionId, CancellationToken)

        public Task StopSession(String             SessionId,
                                CancellationToken  CancellationToken)
        {

            if (sessions.TryGetValue(SessionId, out var session))
                session.Post(async () => {

                    if (session.Ended || session.Stopping)
                        return;

                    session.Stopping = true;

                    if (session.ChargingStarted)
                        await ChargingControl.Stop(SessionId, CancellationToken.None);

                    else
                    {

                        await session.Events.Stopped(EVTalerStopReasons.Driver);

                        // Step 1 is still waiting for its payment: the session is settled once it is paid, in full, or
                        // its deadline passed.
                        if (session.Steps.All(step => step.Paid || step.Unpaid))
                            await Settle(session);

                    }

                });

            return Task.CompletedTask;

        }

        #endregion

        #endregion

        #region IEVTalerChargingEvents

        #region Started  (SessionId, StartValue)

        public Task Started(String                    SessionId,
                            EVTalerSignedMeterValue?  StartValue)
        {

            if (sessions.TryGetValue(SessionId, out var session))
                session.Post(async () => {

                    if (StartValue is not null && session.StartValue is null && !session.Ended)
                    {
                        session.StartValue = StartValue;
                        await session.Events.MeterValue(StartValue, EVTalerMeterValueAt.Start);
                        await OfferNextStepIfDue(session);
                    }

                });

            return Task.CompletedTask;

        }

        #endregion

        #region Delivered(SessionId, Quantity)

        public Task Delivered(String   SessionId,
                              Decimal  Quantity)
        {

            if (sessions.TryGetValue(SessionId, out var session))
                session.Post(async () => {

                    if (Quantity > session.Delivered)
                        session.Delivered = Quantity;

                    await OfferNextStepIfDue(session);

                });

            return Task.CompletedTask;

        }

        #endregion

        #region Ended    (SessionId, EndValue, Reason)

        public Task Ended(String                    SessionId,
                          EVTalerSignedMeterValue?  EndValue,
                          String                    Reason)
        {

            if (sessions.TryGetValue(SessionId, out var session))
                session.Post(async () => {

                    if (session.ChargingEnded || session.Ended)
                        return;

                    session.ChargingEnded = true;

                    if (EndValue is not null)
                    {
                        session.EndValue = EndValue;
                        await session.Events.MeterValue(EndValue, EVTalerMeterValueAt.End);
                    }

                    await session.Events.Stopped(Reason == EVTalerStopReasons.Limit
                                                     ? session.Capped
                                                           ? EVTalerStopReasons.Cap
                                                           : session.Steps.Any(step => step.Unpaid)
                                                                 ? EVTalerStopReasons.Unpaid
                                                                 : EVTalerStopReasons.Limit
                                                     : Reason);

                    // A step still waiting for its payment is settled with the others once it is paid or its deadline
                    // passed: no money taken stays without its refund.
                    if (session.Steps.All(step => step.Paid || step.Unpaid))
                        await Settle(session);

                });

            return Task.CompletedTask;

        }

        #endregion

        #endregion


        #region (private) MakeStep(Session)

        /// <summary>
        /// Make the next step of the given session as an order, and offer it. Null when it was offered; else why not.
        /// </summary>
        private async Task<EVTalerStartResult?> MakeStep(BackendSession Session)
        {

            var number    = (UInt32) Session.Steps.Count + 1;
            var request   = Session.Context.Request;
            var quantity  = request.Step.Quantity;
            var covers    = new EVTalerCovers(Session.OfferedTo, Session.OfferedTo + quantity, request.Step.Unit);

            if (!Session.Rates.TryGetStepPrice(covers, out var stepPrice, out var priceError))
                return EVTalerStartResult.NotAccepted(priceError);

            var flatFee   = number == 1 ? Session.Rates.FlatFee ?? 0m : 0m;
            var reserve   = number == 1 ? Reserve                     : 0m;
            var amount    = stepPrice + flatFee + reserve;

            if (request.Cap.HasValue && Session.OfferedAmount + amount > request.Cap.Value)
            {

                if (number == 1)
                    return EVTalerStartResult.NotAccepted($"Step 1 costs {Write(amount, Session.Currency)}, more than the cap of {Write(request.Cap.Value, Session.Currency)}!");

                Session.Capped = true;
                return EVTalerStartResult.NotAccepted("The cap is reached.");

            }

            var step      = new EVTalerStep(RoamingNetworkId,
                                            request.EVSEId.ToString(),
                                            Session.Context.SessionId,
                                            number,
                                            Session.Steps.LastOrDefault()?.OrderId,
                                            Session.TariffId,
                                            Session.PriceStatement,
                                            covers,
                                            number > 1 ? Session.StartValue : null);

            var orderId   = $"{Session.Context.SessionId}-{number}";
            var deadline  = TimeProvider.GetUtcNow() + (number == 1 ? FirstPayDeadline : NextPayDeadline);

            var products  = new JArray(Product($"Charging at {request.EVSEId}, step {number}: {covers}", stepPrice, Session.Currency));

            if (flatFee > 0)
                products.Add(Product("Flat fee", flatFee, Session.Currency));

            if (reserve > 0)
                products.Add(Product("Reserve, refunded as far as it is not used", reserve, Session.Currency));

            var order     = new JObject(new JProperty("order_id",      orderId),
                                        new JProperty("summary",       $"Charging at {request.EVSEId}, step {number}: {covers}"),
                                        new JProperty("amount",        $"{Session.Currency}:{Write(amount, Session.Currency)}"),
                                        new JProperty("products",      products),
                                        new JProperty("pay_deadline",  TalerTime.Timestamp(deadline)),
                                        new JProperty("auto_refund",   TalerTime.RelativeTime(AutoRefund)),
                                        new JProperty("extra",         new JObject(new JProperty("wwcp", step.ToJSON()))));

            var posted    = await Merchant.PostOrder(order, RefundDelay);

            if (!posted.IsSuccess)
            {
                Session.RetryStepAt = TimeProvider.GetUtcNow() + RetryStep;
                return Failed(Session, $"The merchant backend made no order of step {number}: {posted}");
            }

            var status    = await Merchant.GetOrder(posted.Result.OrderId);

            if (!status.IsSuccess || status.Result.TalerPayURI is null || status.Result.ContractTerms is null)
            {
                Session.RetryStepAt = TimeProvider.GetUtcNow() + RetryStep;
                return Failed(Session, $"The merchant backend gave no offer of step {number}: {status}");
            }

            var state     = new StepState(number, posted.Result.OrderId, amount, covers, deadline);

            Session.Steps.Add(state);
            Session.OfferedTo      = covers.To;
            Session.OfferedAmount += amount;
            Session.PendingStep    = true;

            var offered   = await Session.Events.Offer(new EVTalerOffer(number,
                                                                        posted.Result.OrderId,
                                                                        status.Result.TalerPayURI,
                                                                        status.Result.ContractTerms));

            if (!offered.Relayed)
            {
                // Not relayed: no driver pays it, and the session ends (the server stops it from step 2 on).
                state.Unpaid         = true;
                Session.PendingStep  = false;
                return EVTalerStartResult.Failed($"The offer of step {number} was refused: {offered.ErrorResponse}");
            }

            _ = WatchPayment(Session, state);

            return null;

        }

        #endregion

        #region (private) OfferNextStepIfDue(Session)

        /// <summary>
        /// Offer the next step when the credit of the steps paid is about to run out: every step offered is paid, and the
        /// share NextStepAt of the last one is delivered. Not before the station gave the signed meter value at the start.
        /// </summary>
        private async Task OfferNextStepIfDue(BackendSession Session)
        {

            if (!Session.ChargingStarted || Session.ChargingEnded || Session.Ended || Session.Stopping ||
                Session.PendingStep      || Session.Capped        || Session.StartValue is null       ||
                Session.OfferedTo != Session.Credit || TimeProvider.GetUtcNow() < Session.RetryStepAt)
                return;

            var step = Session.Context.Request.Step.Quantity;

            if (Session.Credit - Session.Delivered > (1m - NextStepAt) * step)
                return;

            await MakeStep(Session);

        }

        #endregion

        #region (private) WatchPayment(Session, Step)

        /// <summary>
        /// Wait for the payment of the given step by long polling, until its deadline, and end the step: paid, or unpaid
        /// once the merchant backend says so after the deadline. Where it does not say, failing or not reached, it is asked
        /// again, UnknownPaymentWait after the deadline at most; then the step is taken as unpaid, and reported. The watch
        /// always ends the step, so that the session and its EVSE are never held without one.
        /// </summary>
        private async Task WatchPayment(BackendSession  Session,
                                        StepState       Step)
        {

            try
            {

                while (true)
                {

                    var asked = TimeProvider.GetUtcNow();
                    TalerMerchantResponse<TalerOrderStatus>? status;

                    try
                    {
                        var remaining = Step.PayDeadline - asked;
                        status = await Merchant.GetOrder(Step.OrderId, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
                    }
                    catch (Exception e)
                    {
                        status = null;
                        Report(Session, $"Waiting for the payment of step {Step.Step} failed: {e.Message}");
                    }

                    if (status is { IsSuccess: true } && status.Result.IsPaid)
                    {
                        Session.Post(() => OnPaid(Session, Step));
                        return;
                    }

                    var now = TimeProvider.GetUtcNow();

                    if (now >= Step.PayDeadline + TimeSpan.FromSeconds(1))
                    {

                        // The merchant backend says it is not paid, after its deadline: no wallet pays it any more.
                        if (status is { IsSuccess: true })
                        {
                            Session.Post(() => OnUnpaid(Session, Step));
                            return;
                        }

                        if (now >= Step.PayDeadline + UnknownPaymentWait)
                        {
                            Report(Session, $"The payment of step {Step.Step} is still not known {UnknownPaymentWait} after its deadline: it is taken as unpaid; the order '{Step.OrderId}' at the merchant backend says whether it was.");
                            Session.Post(() => OnUnpaid(Session, Step));
                            return;
                        }

                    }

                    if (status is { IsSuccess: false })
                        Report(Session, $"The payment of step {Step.Step} is not known: {status}");

                    // A backend answering at once, or not at all, is asked again a moment later.
                    if (TimeProvider.GetUtcNow() - asked < TimeSpan.FromSeconds(1))
                        await Task.Delay(TimeSpan.FromSeconds(1), TimeProvider);

                }

            }
            catch (Exception e)
            {
                // Not to come: the step ends unpaid, rather than holding the session and its EVSE.
                Report(Session, $"Waiting for the payment of step {Step.Step} failed, it is taken as unpaid: {e.Message}");
                Session.Post(() => OnUnpaid(Session, Step));
            }

        }

        #endregion

        #region (private) OnPaid  (Session, Step)

        private async Task OnPaid(BackendSession  Session,
                                  StepState       Step)
        {

            if (Step.Paid)
                return;

            Step.Paid            = true;
            Session.PendingStep  = false;
            Session.Credit       = Math.Max(Session.Credit, Step.Covers.To);

            await Session.Events.Paid(Step.Step, Step.OrderId);

            var limit = new EVTalerChargingLimit(Session.Credit, Session.Context.Request.Step.Unit);

            if (Session.Stopping || Session.ChargingEnded)
            {
                // Paid after the driver stopped, or after charging ended: settled with the others, refunded in full when
                // nothing was delivered by it.
                if (Session.Steps.All(step => step.Paid || step.Unpaid))
                    await Settle(Session);
                return;
            }

            if (!Session.ChargingStarted)
            {

                String? why;

                try
                {
                    why = await ChargingControl.Start(Session.Context.Request.EVSEId, Session.Context.SessionId, limit, this, CancellationToken.None);
                }
                catch (Exception e)
                {
                    Report(Session, $"The charging station management system failed to start charging: {e.Message}");
                    why = "The charging station management system failed to start charging.";
                }

                if (why is not null)
                {
                    Session.ChargingEnded = true;
                    await Session.Events.Stopped(EVTalerStopReasons.Operator, why);
                    await Settle(Session);
                    return;
                }

                Session.ChargingStarted = true;

            }

            else
                await ChargingControl.RaiseLimit(Session.Context.SessionId, limit, CancellationToken.None);

            await OfferNextStepIfDue(Session);

        }

        #endregion

        #region (private) OnUnpaid(Session, Step)

        private async Task OnUnpaid(BackendSession  Session,
                                    StepState       Step)
        {

            if (Step.Paid || Step.Unpaid)
                return;

            Step.Unpaid          = true;
            Session.PendingStep  = false;

            await Session.Events.Unpaid(Step.Step, Step.OrderId);

            if (!Session.ChargingStarted && !Session.ChargingEnded)
            {
                // Step 1 not paid: no charging, nothing to refund.
                Session.ChargingEnded = true;

                if (!Session.Stopping)
                    await Session.Events.Stopped(EVTalerStopReasons.Unpaid);

                await Settle(Session);
                return;
            }

            if (Session.ChargingEnded && Session.Steps.All(step => step.Paid || step.Unpaid))
                await Settle(Session);

        }

        #endregion

        #region (private) Settle  (Session)

        /// <summary>
        /// Settle the session (concepts/EVTaler.md, 5.4): its price by the tariff from the signed meter values at its start
        /// and its end, both verified with a key of the station proof; what was paid beyond it refunded from the last step
        /// back; the settlement statement signed with the key for payments. Without both values every step is refunded in
        /// full, and there is no statement.
        /// </summary>
        private async Task Settle(BackendSession Session)
        {

            if (Session.Ended)
                return;

            Session.Ended = true;

            var paid       = Session.Steps.Where(step => step.Paid).ToArray();
            var paidTotal  = paid.Sum(step => step.Amount);
            var evseId     = Session.Context.Request.EVSEId.ToString();
            var now        = TimeProvider.GetUtcNow();

            EVTalerSettlement?  statement  = null;
            Decimal?            price      = null;
            String?             noPrice    = null;

            if (paid.Length == 0)
                noPrice = "nothing was paid";

            else if (Session.StartValue is null || Session.EndValue is null)
                noPrice = "the station gave no signed meter value at the start or at the end";

            else if (!EVTalerSignedMeterValue.TryVerify(Session.Context.Station, evseId, Session.StartValue, MeterValueVerifier, out var start, out var startError))
                noPrice = $"the signed meter value at the start is not verified: {startError}";

            else if (!EVTalerSignedMeterValue.TryVerify(Session.Context.Station, evseId, Session.EndValue,   MeterValueVerifier, out var end,   out var endError))
                noPrice = $"the signed meter value at the end is not verified: {endError}";

            else if (end.Reading.Energy < start.Reading.Energy || end.Reading.Timestamp < start.Reading.Timestamp || end.SignedBy != start.SignedBy)
                noPrice = "the signed meter values at the start and the end are not of one meter, one after the other";

            else
            {

                var energy = end.Reading.Energy - start.Reading.Energy;

                price      = Session.Rates.SessionPrice(energy, end.Reading.Timestamp - start.Reading.Timestamp);

                try
                {
                    statement = EVTalerSettlement.Create(settlementKey,
                                                         SettlementKeyId,
                                                         SettlementAlgorithm,
                                                         OperatorId,
                                                         RoamingNetworkId,
                                                         evseId,
                                                         Session.Context.SessionId,
                                                         Session.Merchant.MerchantPublicKey,
                                                         Session.TariffId,
                                                         Session.PriceStatement,
                                                         start.Reading.Timestamp,
                                                         end.  Reading.Timestamp,
                                                         Session.StartValue,
                                                         Session.EndValue,
                                                         energy,
                                                         Session.Currency,
                                                         paid.Select(step => (step.OrderId, step.Amount)),
                                                         price.Value,
                                                         SignedAt: now);
                }
                catch (ArgumentException e)
                {
                    Report(Session, $"The settlement statement is not made: {e.Message}");
                    price    = null;
                    noPrice  = "the settlement statement is not made";
                }

            }

            if (noPrice is not null && paid.Length > 0)
                Report(Session, $"The session has no price by the tariff, every step is refunded in full: {noPrice}.");

            // What is refunded: beyond the price, from the last step back; everything without a price.
            var toRefund = statement is not null && price.HasValue
                               ? Math.Max(0m, paidTotal - price.Value)
                               : paidTotal;

            var refunds  = new List<EVTalerRefund>();

            foreach (var step in paid.Reverse())
            {

                if (toRefund <= 0)
                    break;

                var refund  = Math.Min(toRefund, step.Amount);
                var granted = await Merchant.Refund(step.OrderId,
                                                    $"{Session.Currency}:{Write(refund, Session.Currency)}",
                                                    statement is not null && price.HasValue
                                                        ? $"EV Taler session {Session.Context.SessionId}: settled at {Write(price.Value, Session.Currency)} {Session.Currency}"
                                                        : $"EV Taler session {Session.Context.SessionId}: refunded in full, {noPrice}");

                if (granted.IsSuccess)
                    refunds.Add(new EVTalerRefund(step.OrderId, refund, Session.Currency, granted.Result.TalerRefundURI));
                else
                    Report(Session, $"The refund of {Write(refund, Session.Currency)} {Session.Currency} of step {step.Step} was not granted: {granted}");

                toRefund -= refund;

            }

            await Session.Events.Settlement(statement, refunds);

            sessions.TryRemove(Session.Context.SessionId, out _);
            Session.Complete();

        }

        #endregion


        #region (private) Failed(Session, Description)

        private EVTalerStartResult Failed(BackendSession  Session,
                                          String          Description)
        {
            Report(Session, Description);
            return EVTalerStartResult.Failed(Description);
        }

        #endregion

        #region (private) Report(Session, Description)

        private void Report(BackendSession  Session,
                            String          Description)
        {
            try
            {
                OnError?.Invoke(Session.Context.SessionId, Description);
            }
            catch
            { }
        }

        #endregion

        #region (private static) Product(Description, Price, Currency)

        private static JObject Product(String   Description,
                                       Decimal  Price,
                                       String   Currency)

            => new (new JProperty("description",  Description),
                    new JProperty("quantity",     1),
                    new JProperty("price",        $"{Currency}:{Write(Price, Currency)}"));

        #endregion

        #region (private static) Write(Amount, Currency)

        private static String Write(Decimal  Amount,
                                    String   Currency)

            => EVTalerAmounts.Write(Amount, Currency);

        #endregion


        #region (private) class StepState

        private sealed class StepState(UInt32          Step,
                                       String          OrderId,
                                       Decimal         Amount,
                                       EVTalerCovers   Covers,
                                       DateTimeOffset  PayDeadline)
        {
            public UInt32          Step          { get; } = Step;
            public String          OrderId       { get; } = OrderId;
            public Decimal         Amount        { get; } = Amount;
            public EVTalerCovers   Covers        { get; } = Covers;
            public DateTimeOffset  PayDeadline   { get; } = PayDeadline;
            public Boolean         Paid          { get; set; }
            public Boolean         Unpaid        { get; set; }
        }

        #endregion

        #region (private) class BackendSession

        /// <summary>
        /// A session of the backend: what it offered, what was paid and delivered. Everything that changes it runs one
        /// after the other on its own queue, so that a charging station management system telling of the start while it is
        /// asked to start waits for nothing.
        /// </summary>
        private sealed class BackendSession(EVTalerStepBackend     Backend,
                                            EVTalerSessionContext  Context,
                                            EVTalerMerchant        Merchant,
                                            String                 TariffId,
                                            String?                PriceStatement,
                                            String                 Currency,
                                            EVTalerRates           Rates)
        {

            private readonly Channel<Func<Task>> work = Channel.CreateUnbounded<Func<Task>>(new UnboundedChannelOptions { SingleReader = true });

            public EVTalerSessionContext     Context            { get; } = Context;
            public EVTalerMerchant           Merchant           { get; } = Merchant;
            public String                    TariffId           { get; } = TariffId;
            public String?                   PriceStatement     { get; } = PriceStatement;
            public String                    Currency           { get; } = Currency;
            public EVTalerRates              Rates              { get; } = Rates;
            public IEVTalerSessionEvents     Events             => Context.Events;

            public List<StepState>           Steps              { get; } = [];
            public Decimal                   OfferedTo          { get; set; }
            public Decimal                   OfferedAmount      { get; set; }
            public Decimal                   Credit             { get; set; }
            public Decimal                   Delivered          { get; set; }
            public EVTalerSignedMeterValue?  StartValue         { get; set; }
            public EVTalerSignedMeterValue?  EndValue           { get; set; }
            public Boolean                   PendingStep        { get; set; }
            public Boolean                   Capped             { get; set; }
            public Boolean                   Stopping           { get; set; }
            public Boolean                   ChargingStarted    { get; set; }
            public Boolean                   ChargingEnded      { get; set; }
            public Boolean                   Ended              { get; set; }
            public DateTimeOffset            RetryStepAt        { get; set; }

            /// <summary>
            /// Run its queue.
            /// </summary>
            public void Run()

                => _ = Task.Run(async () => {
                           await foreach (var item in work.Reader.ReadAllAsync())
                           {
                               try
                               {
                                   await item();
                               }
                               catch (Exception e)
                               {
                                   Backend.Report(this, $"The session failed: {e.Message}");
                               }
                           }
                       });

            /// <summary>
            /// Queue the given work, without waiting for it.
            /// </summary>
            public void Post(Func<Task> Work)
                => work.Writer.TryWrite(Work);

            /// <summary>
            /// Queue the given work and wait for its result.
            /// </summary>
            public Task<T> Enqueue<T>(Func<Task<T>> Work)
            {

                var done = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);

                if (!work.Writer.TryWrite(async () => {
                        try
                        {
                            done.TrySetResult(await Work());
                        }
                        catch (Exception e)
                        {
                            done.TrySetException(e);
                        }
                    }))
                    done.TrySetException(new InvalidOperationException("The session has ended."));

                return done.Task;

            }

            /// <summary>
            /// Close its queue.
            /// </summary>
            public void Complete()
                => work.Writer.TryComplete();

        }

        #endregion

    }

}
