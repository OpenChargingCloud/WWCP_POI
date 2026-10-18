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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The EVSEs the charging tariff offers of a charging station operator cover, and the rule that two of them must not
    /// overlap at one: the offers, their groups and the EVSEs are all the operator's own data.
    /// </summary>
    public sealed partial class ChargingStationOperator
    {

        #region TimeZoneOf(EVSEId)

        /// <summary>
        /// The time zone the regular openings of charging tariff offers are local times of at the given EVSE of this
        /// operator: the one of its charging pool as it is at runtime, UTC when it has none, or one unknown here.
        /// </summary>
        /// <param name="EVSEId">An EVSE of this operator.</param>
        public TimeZoneInfo TimeZoneOf(EVSE_Id EVSEId)

            => evseTimeZones.Value.TryGetValue(EVSEId, out var timeZone)
                   ? timeZone
                   : TimeZoneInfo.Utc;

        /// <summary>
        /// The time zones of the EVSEs whose charging pool, as it is at runtime, has a known one.
        /// </summary>
        private ImmutableDictionary<EVSE_Id, TimeZoneInfo> CalculateEVSETimeZones()
        {

            var timeZones = ImmutableDictionary.CreateBuilder<EVSE_Id, TimeZoneInfo>();

            foreach (var chargingPool in ChargingPools)
                if (EffectiveChargingPool(chargingPool).TimeZone is Time_Zone timeZone &&
                    TimeZoneInfo.TryFindSystemTimeZoneById(timeZone.ToString(), out var timeZoneInfo))
                {
                    foreach (var evse in chargingPool.ChargingStations.SelectMany(chargingStation => chargingStation.EVSEs))
                        timeZones[evse.Id] = timeZoneInfo;
                }

            return timeZones.ToImmutable();

        }

        #endregion

        #region CoveredEVSEs(Offer)

        /// <summary>
        /// The EVSEs of this operator the given offer covers: those of the operators it names, if it is one of them, and
        /// those that, or whose charging station or charging pool, are members of one of the groups of this operator it
        /// names. A group or member this operator does not have is left out.
        /// </summary>
        /// <param name="Offer">A charging tariff offer, of this operator or of an e-mobility provider.</param>
        public IEnumerable<EVSE_Id> CoveredEVSEs(ChargingTariffOffer Offer)
        {

            if (Offer.ChargingStationOperatorIds.Contains(Id))
                return EVSEs.Select(evse => evse.Id);

            var covered = new HashSet<EVSE_Id>();

            foreach (var groupId in Offer.EVSEGroupIds)
                if (TryGetEVSEGroup(groupId, out var evseGroup))
                    covered.UnionWith(evseGroup.EVSEIds.Where(evseIndex.ContainsKey));

            foreach (var groupId in Offer.ChargingStationGroupIds)
                if (TryGetChargingStationGroup(groupId, out var chargingStationGroup))
                    foreach (var chargingStationId in chargingStationGroup.ChargingStationIds)
                        if (TryGetChargingStation(chargingStationId, out var chargingStation))
                            covered.UnionWith(chargingStation.EVSEs.Select(evse => evse.Id));

            foreach (var groupId in Offer.ChargingPoolGroupIds)
                if (TryGetChargingPoolGroup(groupId, out var chargingPoolGroup))
                    foreach (var chargingPoolId in chargingPoolGroup.ChargingPoolIds)
                        if (TryGetChargingPool(chargingPoolId, out var chargingPool))
                            covered.UnionWith(chargingPool.ChargingStations.SelectMany(chargingStation => chargingStation.EVSEs).Select(evse => evse.Id));

            return covered;

        }

        #endregion

        #region (private) OverlappingOffers()

        /// <summary>
        /// The error, when two charging tariff offers of this operator overlap: they cover the same EVSE, for some of the
        /// same sessions, at some of the same times in the time zone of the EVSE (see ChargingTariffOffer.Overlap).
        /// </summary>
        private String? OverlappingOffers()
        {

            var offers   = ChargingTariffOffers.ToArray();
            var covered  = new Dictionary<ChargingTariffOffer_Id, HashSet<EVSE_Id>>();

            HashSet<EVSE_Id> CoveredBy(ChargingTariffOffer Offer)
            {
                if (!covered.TryGetValue(Offer.Id, out var evseIds))
                    covered[Offer.Id] = evseIds = [ .. CoveredEVSEs(Offer) ];
                return evseIds;
            }

            for (var i = 0; i < offers.Length; i++)
                for (var j = i + 1; j < offers.Length; j++)
                    if (ChargingTariffOffer.SessionsOverlap(offers[i], offers[j]) &&
                        FirstOverlap(offers[i], offers[j], CoveredBy(offers[i]).Intersect(CoveredBy(offers[j])), TimeZoneOf) is EVSE_Id evseId)
                    {
                        return $"The charging tariff offers '{offers[i].Id}' and '{offers[j].Id}' of the operator '{Id}' overlap at the EVSE '{evseId}': they must not cover the same EVSE for the same sessions at the same times!";
                    }

            return null;

        }

        #endregion

        #region (internal static) FirstOverlap(Offer1, Offer2, EVSEIds, TimeZoneOf)

        /// <summary>
        /// The first of the given EVSEs, both offers cover, in the ordinal order, in whose time zone the offers apply at
        /// some of the same times; null, when there is none. The times are compared once per time zone.
        /// </summary>
        internal static EVSE_Id? FirstOverlap(ChargingTariffOffer           Offer1,
                                              ChargingTariffOffer           Offer2,
                                              IEnumerable<EVSE_Id>          EVSEIds,
                                              Func<EVSE_Id, TimeZoneInfo>   TimeZoneOf)
        {

            var overlapIn = new Dictionary<TimeZoneInfo, Boolean>();

            foreach (var evseId in EVSEIds.OrderBy(evseId => evseId.ToString(), StringComparer.Ordinal))
            {

                var timeZone = TimeZoneOf(evseId);

                if (!overlapIn.TryGetValue(timeZone, out var overlap))
                    overlapIn[timeZone] = overlap = ChargingTariffOffer.TimesOverlap(Offer1, Offer2, timeZone);

                if (overlap)
                    return evseId;

            }

            return null;

        }

        #endregion

        #region OffersCovering(EVSEId)

        /// <summary>
        /// The charging tariff offers of this operator covering the given EVSE of it, in the order of their identifications.
        /// </summary>
        public IEnumerable<ChargingTariffOffer> OffersCovering(EVSE_Id EVSEId)

            => ChargingTariffOffers.Where(offer => CoveredEVSEs(offer).Contains(EVSEId));

        #endregion

        #region ApplicableOffers(EVSEId, Timestamp)

        /// <summary>
        /// The charging tariff offers of this operator that apply at the given EVSE of it at the given time: covering it,
        /// active then and open by their opening times in its time zone. They are for different sessions, as they do not
        /// overlap.
        /// </summary>
        public IEnumerable<ChargingTariffOffer> ApplicableOffers(EVSE_Id         EVSEId,
                                                                 DateTimeOffset  Timestamp)

            => OffersCovering(EVSEId).Where(offer => offer.AppliesAt(Timestamp, TimeZoneOf(EVSEId)));

        #endregion

    }

}
