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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The charging tariff offers of the charging station operators and e-mobility providers of a roaming network: their
    /// identifications are unique in it; an offer of a provider covers EVSEs of any operator, by weak references.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region ChargingTariffOffers

        /// <summary>
        /// The charging tariff offers of all charging station operators and e-mobility providers.
        /// </summary>
        public IEnumerable<ChargingTariffOffer> ChargingTariffOffers

            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingTariffOffers).
                   Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.ChargingTariffOffers));

        #endregion

        #region TryGetChargingTariffOfferById(ChargingTariffOfferId, out ChargingTariffOffer)

        /// <summary>
        /// Try to get the charging tariff offer having the given identification, of any charging station operator or
        /// e-mobility provider.
        /// </summary>
        public Boolean TryGetChargingTariffOfferById(ChargingTariffOffer_Id                        ChargingTariffOfferId,
                                                     [NotNullWhen(true)] out ChargingTariffOffer?  ChargingTariffOffer)
        {

            foreach (var chargingStationOperator in chargingStationOperators)
                if (chargingStationOperator.TryGetChargingTariffOffer(ChargingTariffOfferId, out ChargingTariffOffer))
                    return true;

            foreach (var eMobilityProvider in eMobilityProviders)
                if (eMobilityProvider.TryGetChargingTariffOffer(ChargingTariffOfferId, out ChargingTariffOffer))
                    return true;

            ChargingTariffOffer = null;
            return false;

        }

        #endregion

        #region OffersCovering(EVSEId)

        /// <summary>
        /// The charging tariff offers covering the given EVSE: of its operator, then of e-mobility providers, each in the
        /// order of their identifications.
        /// </summary>
        public IEnumerable<ChargingTariffOffer> OffersCovering(EVSE_Id EVSEId)

            => (chargingStationOperators.TryGet(EVSEId.OperatorId, out var chargingStationOperator)
                    ? chargingStationOperator.OffersCovering(EVSEId)
                    : []).
               Concat(eMobilityProviders.SelectMany(eMobilityProvider => eMobilityProvider.ChargingTariffOffers).
                                         Where     (offer             => CoveredEVSEs(offer, chargingStationOperators.Index, eMobilityProviders).Contains(EVSEId)));

        #endregion


        #region (internal static) DuplicateChargingTariffOffer(ChargingStationOperators, EMobilityProviders)

        /// <summary>
        /// The error, when two charging tariff offers have the same identification: they are unique in the roaming network.
        /// </summary>
        internal static String? DuplicateChargingTariffOffer(IEnumerable<ChargingStationOperator>  ChargingStationOperators,
                                                             IEnumerable<EMobilityProvider>        EMobilityProviders)
        {

            var seen = new HashSet<ChargingTariffOffer_Id>();

            foreach (var offer in ChargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.ChargingTariffOffers).
                           Concat(EMobilityProviders.      SelectMany(eMobilityProvider       => eMobilityProvider.      ChargingTariffOffers)))
            {
                if (!seen.Add(offer.Id))
                    return $"The charging tariff offer '{offer.Id}' must not be given twice in the roaming network!";
            }

            return null;

        }

        #endregion

        #region (internal static) CoveredEVSEs(Offer, ChargingStationOperators, EMobilityProviders)

        /// <summary>
        /// The EVSEs of the given operators the given offer covers: all of the operators it names, and those that, or whose
        /// charging station or charging pool, are members of one of the groups it names, of an operator or of a provider.
        /// What does not exist (any more) is left out.
        /// </summary>
        internal static HashSet<EVSE_Id> CoveredEVSEs(ChargingTariffOffer                                                       Offer,
                                                     IReadOnlyDictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                     IEnumerable<EMobilityProvider>                                            EMobilityProviders)
        {

            var covered    = new HashSet<EVSE_Id>();
            var providers  = EMobilityProviders.ToArray();

            ChargingStationOperator? OperatorOf(ChargingStationOperator_Id OperatorId)
                => ChargingStationOperators.TryGetValue(OperatorId, out var chargingStationOperator) ? chargingStationOperator : null;

            foreach (var operatorId in Offer.ChargingStationOperatorIds)
                if (OperatorOf(operatorId) is ChargingStationOperator chargingStationOperator)
                    covered.UnionWith(chargingStationOperator.EVSEs.Select(evse => evse.Id));

            foreach (var groupId in Offer.EVSEGroupIds)
            {

                var evseGroup = OperatorOf(groupId.OperatorId) is ChargingStationOperator groupOperator && groupOperator.TryGetEVSEGroup(groupId, out var operatorGroup)
                                    ? operatorGroup
                                    : providers.Select(provider => provider.TryGetEVSEGroup(groupId, out var providerGroup) ? providerGroup : null).FirstOrDefault(group => group is not null);

                foreach (var evseId in evseGroup?.EVSEIds ?? [])
                    if (OperatorOf(evseId.OperatorId)?.TryGetEVSE(evseId, out _) == true)
                        covered.Add(evseId);

            }

            foreach (var groupId in Offer.ChargingStationGroupIds)
            {

                var chargingStationGroup = OperatorOf(groupId.OperatorId) is ChargingStationOperator groupOperator && groupOperator.TryGetChargingStationGroup(groupId, out var operatorGroup)
                                               ? operatorGroup
                                               : providers.Select(provider => provider.TryGetChargingStationGroup(groupId, out var providerGroup) ? providerGroup : null).FirstOrDefault(group => group is not null);

                foreach (var chargingStationId in chargingStationGroup?.ChargingStationIds ?? [])
                    if (OperatorOf(chargingStationId.OperatorId) is ChargingStationOperator chargingStationOperator &&
                        chargingStationOperator.TryGetChargingStation(chargingStationId, out var chargingStation))
                    {
                        covered.UnionWith(chargingStation.EVSEs.Select(evse => evse.Id));
                    }

            }

            foreach (var groupId in Offer.ChargingPoolGroupIds)
            {

                var chargingPoolGroup = OperatorOf(groupId.OperatorId) is ChargingStationOperator groupOperator && groupOperator.TryGetChargingPoolGroup(groupId, out var operatorGroup)
                                            ? operatorGroup
                                            : providers.Select(provider => provider.TryGetChargingPoolGroup(groupId, out var providerGroup) ? providerGroup : null).FirstOrDefault(group => group is not null);

                foreach (var chargingPoolId in chargingPoolGroup?.ChargingPoolIds ?? [])
                    if (OperatorOf(chargingPoolId.OperatorId) is ChargingStationOperator chargingStationOperator &&
                        chargingStationOperator.TryGetChargingPool(chargingPoolId, out var chargingPool))
                    {
                        covered.UnionWith(chargingPool.ChargingStations.SelectMany(chargingStation => chargingStation.EVSEs).Select(evse => evse.Id));
                    }

            }

            return covered;

        }

        #endregion

        #region (internal static) OverlappingOffers(EMobilityProvider, ChargingStationOperators, EMobilityProviders)

        /// <summary>
        /// The pairs of charging tariff offers of the given e-mobility provider that overlap: they cover the same EVSE, for
        /// some of the same sessions, at some of the same times; with the first such EVSE, in the ordinal order.
        /// </summary>
        internal static Dictionary<(ChargingTariffOffer_Id, ChargingTariffOffer_Id), EVSE_Id> OverlappingOffers(EMobilityProvider                                                         EMobilityProvider,
                                                                                                                IReadOnlyDictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                                                                                IEnumerable<EMobilityProvider>                                            EMobilityProviders)
        {

            var overlaps  = new Dictionary<(ChargingTariffOffer_Id, ChargingTariffOffer_Id), EVSE_Id>();
            var offers    = EMobilityProvider.ChargingTariffOffers.ToArray();
            var covered   = new Dictionary<ChargingTariffOffer_Id, HashSet<EVSE_Id>>();
            var providers = EMobilityProviders.ToArray();

            HashSet<EVSE_Id> CoveredBy(ChargingTariffOffer Offer)
            {
                if (!covered.TryGetValue(Offer.Id, out var evseIds))
                    covered[Offer.Id] = evseIds = CoveredEVSEs(Offer, ChargingStationOperators, providers);
                return evseIds;
            }

            for (var i = 0; i < offers.Length; i++)
                for (var j = i + 1; j < offers.Length; j++)
                    if (ChargingTariffOffer.Overlap(offers[i], offers[j]) &&
                        CoveredBy(offers[i]).Intersect(CoveredBy(offers[j])).
                                             OrderBy(evseId => evseId.ToString(), StringComparer.Ordinal).
                                             Cast<EVSE_Id?>().
                                             FirstOrDefault() is EVSE_Id evseId)
                    {
                        overlaps[(offers[i].Id, offers[j].Id)] = evseId;
                    }

            return overlaps;

        }

        #endregion

        #region (private) NewProviderOfferOverlap(ChargingStationOperators, EMobilityProviders)

        /// <summary>
        /// The error, when a change set makes two charging tariff offers of an e-mobility provider it changed overlap,
        /// which did not overlap in this version: they may come to overlap by the changes of others only.
        /// </summary>
        private String? NewProviderOfferOverlap(IReadOnlyDictionary<ChargingStationOperator_Id, ChargingStationOperator>  ChargingStationOperators,
                                                IReadOnlyDictionary<EMobilityProvider_Id,       EMobilityProvider>        EMobilityProviders)
        {

            foreach (var eMobilityProvider in EMobilityProviders.Values)
            {

                // Only a provider the change set changed: its own changes must not make its offers overlap.
                if (eMobilityProviders.TryGet(eMobilityProvider.Id, out var before) && ReferenceEquals(before, eMobilityProvider))
                    continue;

                if (eMobilityProvider.ChargingTariffOffers.Count < 2)
                    continue;

                var overlapsBefore = before is not null
                                         ? OverlappingOffers(before, chargingStationOperators.Index, eMobilityProviders)
                                         : [];

                foreach (var ((offer1, offer2), evseId) in OverlappingOffers(eMobilityProvider, ChargingStationOperators, EMobilityProviders.Values))
                    if (!overlapsBefore.ContainsKey((offer1, offer2)))
                        return $"The charging tariff offers '{offer1}' and '{offer2}' of the e-mobility provider '{eMobilityProvider.Id}' must not overlap: they cover the EVSE '{evseId}' for the same sessions at the same times!";

            }

            return null;

        }

        #endregion

    }

}
