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

using System.Collections;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a charging tariff offer changed.
    /// </summary>
    public delegate Task OnChargingTariffOfferAdminStatusChangedDelegate(DateTimeOffset                                    Timestamp,
                                                                         EventTracking_Id                                  EventTrackingId,
                                                                         ChargingTariffOffer                               ChargingTariffOffer,
                                                                         Timestamped<ChargingTariffOfferAdminStatusType>   OldStatus,
                                                                         Timestamped<ChargingTariffOfferAdminStatusType>   NewStatus,
                                                                         Context?                                          DataSource);

    /// <summary>
    /// The status of a charging tariff offer changed.
    /// </summary>
    public delegate Task OnChargingTariffOfferStatusChangedDelegate(DateTimeOffset                               Timestamp,
                                                                    EventTracking_Id                             EventTrackingId,
                                                                    ChargingTariffOffer                          ChargingTariffOffer,
                                                                    Timestamped<ChargingTariffOfferStatusType>   OldStatus,
                                                                    Timestamped<ChargingTariffOfferStatusType>   NewStatus,
                                                                    Context?                                     DataSource);


    /// <summary>
    /// The sessions a charging tariff offer of a charging station operator is for.
    /// </summary>
    public enum ChargingTariffOfferSessions
    {

        /// <summary>
        /// Sessions paid without a contract.
        /// </summary>
        adHoc,

        /// <summary>
        /// Sessions with a contract of an e-mobility provider.
        /// </summary>
        contract

    }


    /// <summary>
    /// A charging tariff offer: another price for a time at the EVSEs of groups or of operators, of a charging station
    /// operator at its own EVSEs, or of an e-mobility provider for its customers at the EVSEs of any operator. It changes
    /// neither the groups nor the EVSEs; no effective member takes it.
    /// </summary>
    /// <remarks>
    /// - What: exactly one of a price of its own and a discount.
    /// - Where: at least one of EVSE groups, charging station groups, charging pool groups and operators: an EVSE is covered
    ///   where it, its charging station or its charging pool is a member of one of the groups, or where its operator is named.
    /// - When: from notBefore until notAfter, both inclusive, as of keys, and within its opening times.
    /// - For which sessions: how often one contract takes it; of an operator's offer also ad hoc or with a contract, and of
    ///   the contracts of which providers.
    /// Its status is calculated: planned before its notBefore, active, expired after its notAfter, unless its owner sets it
    /// exhausted before.
    /// </remarks>
    public sealed class ChargingTariffOffer : AImmutableEMobilityEntity<ChargingTariffOffer_Id,
                                                                        ChargingTariffOfferAdminStatusType,
                                                                        ChargingTariffOfferStatusType>,
                                              IEquatable<ChargingTariffOffer>,
                                              IComparable<ChargingTariffOffer>
    {

        #region Data

        /// <summary>
        /// The minutes of a week.
        /// </summary>
        private const Int32 MinutesPerWeek = 7 * 24 * 60;

        #endregion

        #region Properties

        /// <summary>
        /// Its price of its own, unless it gives a discount.
        /// </summary>
        public ChargingTariffOfferPrice?                  ChargingTariff                { get; }

        /// <summary>
        /// Its discount, unless it has a price of its own.
        /// </summary>
        public ChargingTariffDiscount?                    Discount                      { get; }

        /// <summary>
        /// The EVSE groups whose EVSEs it covers, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EVSEGroup_Id>               EVSEGroupIds                  { get; }

        /// <summary>
        /// The charging station groups whose charging stations' EVSEs it covers, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStationGroup_Id>    ChargingStationGroupIds       { get; }

        /// <summary>
        /// The charging pool groups whose charging pools' EVSEs it covers, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingPoolGroup_Id>       ChargingPoolGroupIds          { get; }

        /// <summary>
        /// The charging station operators all of whose EVSEs it covers, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStationOperator_Id> ChargingStationOperatorIds    { get; }

        /// <summary>
        /// From when it applies, inclusive; null: always since.
        /// </summary>
        public DateTimeOffset?                            NotBefore                     { get; }

        /// <summary>
        /// Until when it applies, inclusive; null: for good.
        /// </summary>
        public DateTimeOffset?                            NotAfter                      { get; }

        /// <summary>
        /// When it applies within its validity, as a charging pool writes its opening times; null: at any time.
        /// </summary>
        public ImmutableOpeningTimes?                     OpeningTimes                  { get; }

        /// <summary>
        /// Of an offer of a charging station operator: the sessions it is for; null: ad hoc and with a contract.
        /// </summary>
        public ChargingTariffOfferSessions?               Sessions                      { get; }

        /// <summary>
        /// Of an offer of a charging station operator: the e-mobility providers with whose contracts it is for, in the
        /// order of their identifications; none: the contracts of every provider.
        /// </summary>
        public ImmutableArray<EMobilityProvider_Id>       EMobilityProviderIds          { get; }

        /// <summary>
        /// How often one contract, or ad hoc one means of payment, takes it, at least once; null: every time.
        /// </summary>
        public UInt32?                                    MaxSessionsPerContract        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff offer.
        /// </summary>
        /// <exception cref="ArgumentException">Neither or both of a price and a discount, nothing it covers, a notAfter before
        /// its notBefore, a maximum of sessions of 0, or an identification given twice.</exception>
        public ChargingTariffOffer(ChargingTariffOffer_Id                             Id,
                                   I18NString?                                        Name                         = null,
                                   I18NString?                                        Description                  = null,
                                   ChargingTariffOfferPrice?                          ChargingTariff               = null,
                                   ChargingTariffDiscount?                            Discount                     = null,
                                   IEnumerable<EVSEGroup_Id>?                         EVSEGroupIds                 = null,
                                   IEnumerable<ChargingStationGroup_Id>?              ChargingStationGroupIds      = null,
                                   IEnumerable<ChargingPoolGroup_Id>?                 ChargingPoolGroupIds         = null,
                                   IEnumerable<ChargingStationOperator_Id>?           ChargingStationOperatorIds   = null,
                                   DateTimeOffset?                                    NotBefore                    = null,
                                   DateTimeOffset?                                    NotAfter                     = null,
                                   ImmutableOpeningTimes?                             OpeningTimes                 = null,
                                   ChargingTariffOfferSessions?                       Sessions                     = null,
                                   IEnumerable<EMobilityProvider_Id>?                 EMobilityProviderIds         = null,
                                   UInt32?                                            MaxSessionsPerContract       = null,

                                   Timestamped<ChargingTariffOfferAdminStatusType>?   InitialAdminStatus           = null,
                                   Timestamped<ChargingTariffOfferStatusType>?        InitialStatus                = null,
                                   UInt16?                                            MaxAdminStatusScheduleSize   = null,
                                   UInt16?                                            MaxStatusScheduleSize        = null,

                                   String?                                            DataSource                   = null,
                                   DateTimeOffset?                                    Created                      = null,
                                   DateTimeOffset?                                    LastChange                   = null,

                                   CustomDataNew?                                     CustomData                   = null,
                                   UserDefinedDictionary?                             InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise: active, whenever its times tell so (StatusAt).
                   InitialAdminStatus         ?? new Timestamped<ChargingTariffOfferAdminStatusType>(Created ?? Timestamp.Now, ChargingTariffOfferAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingTariffOfferStatusType>     (Created ?? Timestamp.Now, ChargingTariffOfferStatusType.Active),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            if ((ChargingTariff is null) == (Discount is null))
                throw new ArgumentException($"The charging tariff offer '{Id}' must have exactly one of a price of its own and a discount!", nameof(ChargingTariff));

            this.ChargingTariff              = ChargingTariff;
            this.Discount                    = Discount;
            this.EVSEGroupIds                = POIJSON.OrderedIds(EVSEGroupIds,               $"EVSE group of the charging tariff offer '{Id}'");
            this.ChargingStationGroupIds     = POIJSON.OrderedIds(ChargingStationGroupIds,    $"charging station group of the charging tariff offer '{Id}'");
            this.ChargingPoolGroupIds        = POIJSON.OrderedIds(ChargingPoolGroupIds,       $"charging pool group of the charging tariff offer '{Id}'");
            this.ChargingStationOperatorIds  = POIJSON.OrderedIds(ChargingStationOperatorIds, $"charging station operator of the charging tariff offer '{Id}'");
            this.NotBefore                   = NotBefore;
            this.NotAfter                    = NotAfter;
            this.OpeningTimes                = OpeningTimes;
            this.Sessions                    = Sessions;
            this.EMobilityProviderIds        = POIJSON.OrderedIds(EMobilityProviderIds,       $"e-mobility provider of the charging tariff offer '{Id}'");
            this.MaxSessionsPerContract      = MaxSessionsPerContract;

            if (this.EVSEGroupIds.IsEmpty && this.ChargingStationGroupIds.IsEmpty && this.ChargingPoolGroupIds.IsEmpty && this.ChargingStationOperatorIds.IsEmpty)
                throw new ArgumentException($"The charging tariff offer '{Id}' must name at least one EVSE group, charging station group, charging pool group or charging station operator!", nameof(EVSEGroupIds));

            if (NotBefore.HasValue && NotAfter.HasValue && NotAfter.Value < NotBefore.Value)
                throw new ArgumentException($"The notAfter of the charging tariff offer '{Id}' must not be before its notBefore!", nameof(NotAfter));

            if (MaxSessionsPerContract == 0)
                throw new ArgumentException($"The maximum of sessions per contract of the charging tariff offer '{Id}' must be at least 1!", nameof(MaxSessionsPerContract));

        }

        #endregion


        #region Status events

        private readonly Lock                                              statusEventsLock  = new ();
        private          OnChargingTariffOfferAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingTariffOfferStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingTariffOfferAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status set for it changed.
        /// </summary>
        public event OnChargingTariffOfferStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                    Timestamp,
                                                                   EventTracking_Id                                  EventTrackingId,
                                                                   Timestamped<ChargingTariffOfferAdminStatusType>   NewStatus,
                                                                   Timestamped<ChargingTariffOfferAdminStatusType>   OldStatus,
                                                                   Context?                                          DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                               Timestamp,
                                                              EventTracking_Id                             EventTrackingId,
                                                              Timestamped<ChargingTariffOfferStatusType>   NewStatus,
                                                              Timestamped<ChargingTariffOfferStatusType>   OldStatus,
                                                              Context?                                     DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region IsValidAt(Timestamp)

        /// <summary>
        /// Whether the given time is within its notBefore and notAfter, both inclusive.
        /// </summary>
        public Boolean IsValidAt(DateTimeOffset Timestamp)

            => (!NotBefore.HasValue || Timestamp >= NotBefore.Value) &&
               (!NotAfter. HasValue || Timestamp <= NotAfter. Value);

        #endregion

        #region StatusAt(Timestamp)

        /// <summary>
        /// Its status at the given time: exhausted, when its owner set it so; else planned before its notBefore, expired
        /// after its notAfter, and active in between.
        /// </summary>
        public ChargingTariffOfferStatusType StatusAt(DateTimeOffset Timestamp)
        {

            if (CurrentStatus is Timestamped<ChargingTariffOfferStatusType> set &&
                set.Value     == ChargingTariffOfferStatusType.Exhausted &&
                set.Timestamp <= Timestamp)
            {
                return ChargingTariffOfferStatusType.Exhausted;
            }

            return NotBefore.HasValue && Timestamp < NotBefore.Value ? ChargingTariffOfferStatusType.Planned
                 : NotAfter. HasValue && Timestamp > NotAfter. Value ? ChargingTariffOfferStatusType.Expired
                 : ChargingTariffOfferStatusType.Active;

        }

        #endregion

        #region StatusSince(Timestamp)

        /// <summary>
        /// Its status at the given time, see StatusAt(…), with the time it has it since: the time its owner set it
        /// exhausted, its creation while planned, its notBefore (or creation) while active, its notAfter once expired.
        /// </summary>
        public Timestamped<ChargingTariffOfferStatusType> StatusSince(DateTimeOffset Timestamp)
        {

            var status = StatusAt(Timestamp);

            var since  = status == ChargingTariffOfferStatusType.Exhausted ? CurrentStatus!.Value.Timestamp
                       : status == ChargingTariffOfferStatusType.Planned   ? Created
                       : status == ChargingTariffOfferStatusType.Expired   ? NotAfter!.Value
                       : NotBefore ?? Created;

            return new Timestamped<ChargingTariffOfferStatusType>(since, status);

        }

        #endregion

        #region IsActive

        /// <summary>
        /// Whether it is active now, see StatusAt(…).
        /// </summary>
        public Boolean IsActive
            => StatusAt(Timestamp.Now) == ChargingTariffOfferStatusType.Active;

        #endregion

        #region Covers(EVSE, ChargingStation, ChargingPool)

        /// <summary>
        /// Whether it covers the given EVSE of the given charging station and charging pool: where it, its charging station
        /// or its charging pool is a member of one of the groups it names, or where its operator is named. The groups are
        /// looked up by the given function: a group that does not exist (any more) is left out.
        /// </summary>
        public Boolean Covers(EVSE_Id                                     EVSEId,
                              ChargingStation_Id                          ChargingStationId,
                              ChargingPool_Id                             ChargingPoolId,
                              Func<EVSEGroup_Id,            EVSEGroup?>             EVSEGroupOf,
                              Func<ChargingStationGroup_Id, ChargingStationGroup?>  ChargingStationGroupOf,
                              Func<ChargingPoolGroup_Id,    ChargingPoolGroup?>     ChargingPoolGroupOf)

            => ChargingStationOperatorIds.Contains(EVSEId.OperatorId) ||
               EVSEGroupIds.           Any(groupId => EVSEGroupOf           (groupId)?.EVSEIds.           Contains(EVSEId)            == true) ||
               ChargingStationGroupIds.Any(groupId => ChargingStationGroupOf(groupId)?.ChargingStationIds.Contains(ChargingStationId) == true) ||
               ChargingPoolGroupIds.   Any(groupId => ChargingPoolGroupOf   (groupId)?.ChargingPoolIds.   Contains(ChargingPoolId)    == true);

        #endregion

        #region (static) SessionsOverlap(Offer1, Offer2)

        /// <summary>
        /// Whether the given offers apply to some of the same sessions: both to ad hoc sessions, or both to sessions with
        /// a contract of a provider both name, or one of them names none.
        /// </summary>
        public static Boolean SessionsOverlap(ChargingTariffOffer  Offer1,
                                              ChargingTariffOffer  Offer2)
        {

            var adHoc1     = Offer1.Sessions is null or ChargingTariffOfferSessions.adHoc;
            var adHoc2     = Offer2.Sessions is null or ChargingTariffOfferSessions.adHoc;
            var contract1  = Offer1.Sessions is null or ChargingTariffOfferSessions.contract;
            var contract2  = Offer2.Sessions is null or ChargingTariffOfferSessions.contract;

            if (adHoc1 && adHoc2)
                return true;

            return contract1 && contract2 &&
                   (Offer1.EMobilityProviderIds.IsEmpty ||
                    Offer2.EMobilityProviderIds.IsEmpty ||
                    Offer1.EMobilityProviderIds.Intersect(Offer2.EMobilityProviderIds).Any());

        }

        #endregion

        #region (static) TimesOverlap(Offer1, Offer2)

        /// <summary>
        /// Whether the given offers apply at some of the same times: their notBefore and notAfter leave a common time,
        /// and their opening times, compared week by week by their regular openings, too. Opening times with exceptional
        /// openings count as open at any time, exceptional closings as open as well: the comparison errs on the side of
        /// an overlap.
        /// </summary>
        public static Boolean TimesOverlap(ChargingTariffOffer  Offer1,
                                           ChargingTariffOffer  Offer2)
        {

            var begin  = Max(Offer1.NotBefore, Offer2.NotBefore);
            var end    = Min(Offer1.NotAfter,  Offer2.NotAfter);

            if (begin.HasValue && end.HasValue && end.Value < begin.Value)
                return false;

            var week1  = WeekOf(Offer1.OpeningTimes);
            var week2  = WeekOf(Offer2.OpeningTimes);

            if (week1 is null || week2 is null)
                return true;

            return new BitArray(week1).And(week2).Cast<Boolean>().Any(open => open);

        }

        private static DateTimeOffset? Max(DateTimeOffset? A, DateTimeOffset? B)
            => !A.HasValue ? B : !B.HasValue ? A : A.Value > B.Value ? A : B;

        private static DateTimeOffset? Min(DateTimeOffset? A, DateTimeOffset? B)
            => !A.HasValue ? B : !B.HasValue ? A : A.Value < B.Value ? A : B;

        /// <summary>
        /// The minutes of a week, from Monday 00:00, the given opening times are regularly open at; null for at any time.
        /// A period ending before it begins ends the next day.
        /// </summary>
        private static BitArray? WeekOf(ImmutableOpeningTimes? OpeningTimes)
        {

            if (OpeningTimes is null || OpeningTimes.IsOpen24Hours || !OpeningTimes.ExceptionalOpenings.IsDefaultOrEmpty)
                return null;

            var week = new BitArray(MinutesPerWeek);

            foreach (var (dayOfWeek, periods) in OpeningTimes.RegularOpenings)
            {

                var day = ((Int32) dayOfWeek + 6) % 7;

                foreach (var period in periods)
                {

                    var begin  = day * 1440 + period.PeriodBegin.Hour * 60 + period.PeriodBegin.Minute;
                    var end    = day * 1440 + period.PeriodEnd.  Hour * 60 + period.PeriodEnd.  Minute;

                    if (end <= begin)
                        end += 1440;

                    for (var minute = begin; minute < end; minute++)
                        week[minute % MinutesPerWeek] = true;

                }

            }

            return week;

        }

        #endregion

        #region (static) Overlap(Offer1, Offer2)

        /// <summary>
        /// Whether the given offers, both covering an EVSE, overlap there: for some of the same sessions, at some of the
        /// same times.
        /// </summary>
        public static Boolean Overlap(ChargingTariffOffer  Offer1,
                                      ChargingTariffOffer  Offer2)

            => SessionsOverlap(Offer1, Offer2) &&
               TimesOverlap   (Offer1, Offer2);

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging tariff offer, as its owner embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",                        Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",                 Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",                  DataSource);

            if (ChargingTariff is not null)
                json.Add("chargingTariff",              ChargingTariff.ToJSON());

            if (Discount is not null)
                json.Add("discount",                    Discount.ToJSON());

            if (EVSEGroupIds.Length > 0)
                json.Add("EVSEGroupIds",                POIJSON.IdsToJSON(EVSEGroupIds));

            if (ChargingStationGroupIds.Length > 0)
                json.Add("chargingStationGroupIds",     POIJSON.IdsToJSON(ChargingStationGroupIds));

            if (ChargingPoolGroupIds.Length > 0)
                json.Add("chargingPoolGroupIds",        POIJSON.IdsToJSON(ChargingPoolGroupIds));

            if (ChargingStationOperatorIds.Length > 0)
                json.Add("chargingStationOperatorIds",  POIJSON.IdsToJSON(ChargingStationOperatorIds));

            if (NotBefore.HasValue)
                json.Add("notBefore",                   NotBefore.Value.ToISO8601());

            if (NotAfter.HasValue)
                json.Add("notAfter",                    NotAfter. Value.ToISO8601());

            if (OpeningTimes is not null)
                json.Add("openingTimes",                OpeningTimes.ToJSON());

            if (Sessions.HasValue)
                json.Add("sessions",                    Sessions.Value.ToString());

            if (EMobilityProviderIds.Length > 0)
                json.Add("eMobilityProviderIds",        POIJSON.IdsToJSON(EMobilityProviderIds));

            if (MaxSessionsPerContract.HasValue)
                json.Add("maxSessionsPerContract",      MaxSessionsPerContract.Value);

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingTariffOffer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging tariff offer.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingTariffOffer">The parsed charging tariff offer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                        JSON,
                                       [NotNullWhen(true)]  out ChargingTariffOffer?  ChargingTariffOffer,
                                       [NotNullWhen(false)] out String?               ErrorResponse)
        {

            ChargingTariffOffer = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "charging tariff offer identification",
                                         ChargingTariffOffer_Id.TryParse,
                                         out ChargingTariffOffer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                var owner = $"the charging tariff offer '{id}'";

                if (!POIJSON.TryParseI18N  (JSON, "name",         "charging tariff offer name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",  "charging tariff offer description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",   "data source",                        out var dataSource,   out ErrorResponse))
                {
                    return false;
                }

                #region What: a price of its own or a discount

                ChargingTariffOfferPrice?  chargingTariff  = null;
                ChargingTariffDiscount?    discount        = null;

                if (JSON["chargingTariff"] is JToken chargingTariffJSON &&
                    (chargingTariffJSON is not JObject chargingTariffObject ||
                     !ChargingTariffOfferPrice.TryParse(chargingTariffObject, owner, out chargingTariff, out ErrorResponse)))
                {
                    ErrorResponse ??= $"The price of {owner} must be a JSON object!";
                    return false;
                }

                if (JSON["discount"] is JToken discountJSON &&
                    (discountJSON is not JObject discountObject ||
                     !ChargingTariffDiscount.TryParse(discountObject, out discount, out ErrorResponse)))
                {
                    ErrorResponse ??= $"The discount of {owner} must be a JSON object!";
                    return false;
                }

                #endregion

                #region Where, when, for which sessions

                if (!POIJSON.TryParseIds(JSON, "EVSEGroupIds",                "EVSE group identification",                EVSEGroup_Id.              TryParse, out ImmutableArray<EVSEGroup_Id>               evseGroupIds,                out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, "chargingStationGroupIds",     "charging station group identification",    ChargingStationGroup_Id.   TryParse, out ImmutableArray<ChargingStationGroup_Id>    chargingStationGroupIds,     out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, "chargingPoolGroupIds",        "charging pool group identification",       ChargingPoolGroup_Id.      TryParse, out ImmutableArray<ChargingPoolGroup_Id>       chargingPoolGroupIds,        out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, "chargingStationOperatorIds",  "charging station operator identification", ChargingStationOperator_Id.TryParse, out ImmutableArray<ChargingStationOperator_Id> chargingStationOperatorIds,  out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, "eMobilityProviderIds",        "e-mobility provider identification",       EMobilityProvider_Id.      TryParse, out ImmutableArray<EMobilityProvider_Id>       eMobilityProviderIds,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIEntityMetadata.TryParseTimestamp(JSON["notBefore"], "notBefore of the charging tariff offer", out var notBefore, out ErrorResponse) ||
                    !POIEntityMetadata.TryParseTimestamp(JSON["notAfter"],  "notAfter of the charging tariff offer",  out var notAfter,  out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseObject<ImmutableOpeningTimes>(JSON, "openingTimes", "opening times", ImmutableOpeningTimes.TryParse, out var openingTimes, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseEnum(JSON, "sessions", "sessions of the charging tariff offer", out ChargingTariffOfferSessions? sessions, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseInteger(JSON, "maxSessionsPerContract", "maximum of sessions per contract", out var maxSessionsPerContract, out ErrorResponse))
                    return false;

                if (maxSessionsPerContract < 1)
                {
                    ErrorResponse = $"The maximum of sessions per contract of {owner} must be at least 1!";
                    return false;
                }

                #endregion

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingTariffOffer = new ChargingTariffOffer(id,
                                                              name,
                                                              description,
                                                              chargingTariff,
                                                              discount,
                                                              evseGroupIds,
                                                              chargingStationGroupIds,
                                                              chargingPoolGroupIds,
                                                              chargingStationOperatorIds,
                                                              notBefore,
                                                              notAfter,
                                                              openingTimes,
                                                              sessions,
                                                              eMobilityProviderIds,
                                                              maxSessionsPerContract.HasValue ? (UInt32) maxSessionsPerContract.Value : null,
                                                              DataSource:  dataSource,
                                                              Created:     created,
                                                              LastChange:  lastChange,
                                                              CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingTariffOffer  = null;
                ErrorResponse        = "The given JSON representation of a charging tariff offer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging tariff offers for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingTariffOffer? ChargingTariffOffer1, ChargingTariffOffer? ChargingTariffOffer2)

            => ReferenceEquals(ChargingTariffOffer1, ChargingTariffOffer2) ||
               (ChargingTariffOffer1 is not null && ChargingTariffOffer1.Equals(ChargingTariffOffer2));

        /// <summary>
        /// Compares two charging tariff offers for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingTariffOffer? ChargingTariffOffer1, ChargingTariffOffer? ChargingTariffOffer2)

            => !(ChargingTariffOffer1 == ChargingTariffOffer2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingTariffOffer chargingTariffOffer
                   ? CompareTo(chargingTariffOffer)
                   : throw new ArgumentException("The given object is not a charging tariff offer!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingTariffOffer? ChargingTariffOffer)

            => ChargingTariffOffer is null
                   ? throw new ArgumentNullException(nameof(ChargingTariffOffer), "The given charging tariff offer must not be null!")
                   : Id.CompareTo(ChargingTariffOffer.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffOffer chargingTariffOffer &&
               Equals(chargingTariffOffer);

        /// <inheritdoc/>
        public Boolean Equals(ChargingTariffOffer? ChargingTariffOffer)

            => ChargingTariffOffer is not null &&
               Id.Equals(ChargingTariffOffer.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
