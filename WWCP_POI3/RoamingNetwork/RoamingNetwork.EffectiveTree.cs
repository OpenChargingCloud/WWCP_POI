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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The effective content of a version and its hash tree (SPECIFICATION.md, sections 8.1 to 8.3): its content with every
    /// charging pool, charging station, EVSE, charging tariff, parking space and energy meter as it is at runtime, with the
    /// properties its templates give it, and their last changes without the floors kept at runtime, so that every server
    /// calculates the same tree from the version alone.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The effectiveTree ETags of this version, by their hash algorithms, calculated when asked for: a version never changes.
        /// </summary>
        private readonly ConcurrentDictionary<ETagHashAlgorithm, ETag> effectiveTreeETags = new ();

        #endregion


        #region EffectiveTreeETag (HashAlgorithm)

        /// <summary>
        /// The effectiveTree ETag of this version with the given hash algorithm: the root of the hash tree along the entities
        /// of its effective content, calculated once, from the digests its entities keep (POIEffectiveDigests): those the
        /// versions before did not change are not hashed again.
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm of the tree.</param>
        public ETag EffectiveTreeETag(ETagHashAlgorithm HashAlgorithm)

            => effectiveTreeETags.GetOrAdd(HashAlgorithm,
                                           hashAlgorithm => new ETag(ETagFormat.EffectiveTree,
                                                                     hashAlgorithm,
                                                                     POIEffectiveDigests.Digest(this, hashAlgorithm)));

        #endregion

        #region EffectiveTreeETags

        /// <summary>
        /// The effectiveTree ETags of this version, one per tree hash algorithm, in their order.
        /// </summary>
        public IEnumerable<ETag> EffectiveTreeETags

            => TreeHashAlgorithms.Select(EffectiveTreeETag);

        #endregion

        #region EffectiveTree     (HashAlgorithm)

        /// <summary>
        /// The hash tree of this version with the given hash algorithm, with what proofs of its entities need, from the
        /// digests its entities keep.
        /// </summary>
        /// <param name="HashAlgorithm">The hash algorithm of the tree.</param>
        public RoamingNetworkEffectiveTree EffectiveTree(ETagHashAlgorithm HashAlgorithm)

            => RoamingNetworkEffectiveTree.Of(this, HashAlgorithm);

        #endregion

        #region ToEffectiveContentJSON()

        /// <summary>
        /// The effective content of this version (SPECIFICATION.md, section 8.1): its content, as ToJSON(IncludeETags: false)
        /// writes it, with every charging pool, charging station, EVSE, charging tariff, parking space and energy meter
        /// replaced by its effective one, without the floors of their last changes kept at runtime.
        /// </summary>
        public JObject ToEffectiveContentJSON()

            => EffectiveContentJSON(out _);

        #endregion

        #region (internal) EffectiveContentJSON(out Members)

        /// <summary>
        /// The effective content of this version, and the effective members it was made of, for proofs.
        /// </summary>
        /// <param name="Members">The effective members differing from those of the content.</param>
        internal JObject EffectiveContentJSON(out RoamingNetworkEffectiveMembers Members)
        {

            var json     = ToJSON(IncludeETags: false);
            var members  = EffectiveMembersOfTheContent();

            #region Charging station operators: their effective charging pools and charging tariffs

            if (json["chargingStationOperators"] is JArray operatorsJSON)
            {
                for (var operatorIndex = 0; operatorIndex < chargingStationOperators.Count; operatorIndex++)
                {

                    var chargingStationOperator  = chargingStationOperators[operatorIndex];
                    var operatorJSON             = (JObject) operatorsJSON[operatorIndex];
                    var effectivePools           = members.ChargingPools  [chargingStationOperator.Id];
                    var effectiveTariffs         = members.ChargingTariffs[chargingStationOperator.Id];

                    // Only those differing are written again; the others are as the operator embeds them.
                    if (effectivePools.Count > 0 && operatorJSON["chargingPools"] is JArray poolsJSON)
                        for (var poolIndex = 0; poolIndex < chargingStationOperator.ChargingPools.Count; poolIndex++)
                            if (effectivePools.TryGetValue(chargingStationOperator.ChargingPools[poolIndex].Id, out var effectivePool))
                                poolsJSON[poolIndex] = effectivePool.ToJSON();

                    if (effectiveTariffs.Count > 0 && operatorJSON["chargingTariffs"] is JArray tariffsJSON)
                        for (var tariffIndex = 0; tariffIndex < chargingStationOperator.ChargingTariffs.Count; tariffIndex++)
                            if (effectiveTariffs.TryGetValue(chargingStationOperator.ChargingTariffs[tariffIndex].Id, out var effectiveTariff))
                                tariffsJSON[tariffIndex] = effectiveTariff.ToJSON();

                }
            }

            #endregion

            #region Parking operators: their effective parking spaces

            if (json["parkingOperators"] is JArray parkingOperatorsJSON)
            {
                for (var operatorIndex = 0; operatorIndex < parkingOperators.Count; operatorIndex++)
                {

                    var parkingOperator  = parkingOperators[operatorIndex];
                    var effectiveSpaces  = members.ParkingSpaces[parkingOperator.Id];

                    if (effectiveSpaces.Count > 0 && parkingOperatorsJSON[operatorIndex]["parkingSpaces"] is JArray spacesJSON)
                        for (var spaceIndex = 0; spaceIndex < parkingOperator.ParkingSpaces.Count; spaceIndex++)
                            if (effectiveSpaces.TryGetValue(parkingOperator.ParkingSpaces[spaceIndex].Id, out var effectiveSpace))
                                spacesJSON[spaceIndex] = effectiveSpace.ToJSON();

                }
            }

            #endregion

            #region Grid operators: their effective grid connection points

            if (json["gridOperators"] is JArray gridOperatorsJSON)
            {
                for (var operatorIndex = 0; operatorIndex < gridOperators.Count; operatorIndex++)
                {

                    var gridOperator = gridOperators[operatorIndex];

                    if (gridOperatorsJSON[operatorIndex]["gridConnectionPoints"] is JArray pointsJSON)
                        for (var pointIndex = 0; pointIndex < gridOperator.GridConnectionPoints.Count; pointIndex++)
                        {

                            var gridConnectionPoint  = gridOperator.GridConnectionPoints[pointIndex];
                            var effectivePoint       = gridOperator.EffectiveGridConnectionPoint(gridConnectionPoint);

                            if (!ReferenceEquals(effectivePoint, gridConnectionPoint))
                                pointsJSON[pointIndex] = effectivePoint.ToJSON();

                        }

                }
            }

            #endregion

            Members = members;
            return json;

        }

        #endregion

        #region (internal) EffectiveMembersOfTheContent()

        /// <summary>
        /// The effective members of the effective content of this version differing from those of its content, for proofs.
        /// </summary>
        internal RoamingNetworkEffectiveMembers EffectiveMembersOfTheContent()
        {

            var members = new RoamingNetworkEffectiveMembers();

            foreach (var chargingStationOperator in chargingStationOperators)
            {
                members.ChargingPools.  Add(chargingStationOperator.Id, chargingStationOperator.EffectiveChargingPoolsOfTheContent());
                members.ChargingTariffs.Add(chargingStationOperator.Id, chargingStationOperator.EffectiveChargingTariffsOfTheContent());
            }

            foreach (var parkingOperator in parkingOperators)
                members.ParkingSpaces.Add(parkingOperator.Id, parkingOperator.EffectiveParkingSpacesOfTheContent());

            return members;

        }

        #endregion

    }


    /// <summary>
    /// The effective members of the effective content of a version differing from those of its content, by their owners.
    /// </summary>
    internal sealed class RoamingNetworkEffectiveMembers
    {

        /// <summary>
        /// The effective charging pools, with their effective charging stations and EVSEs, by their charging station operators.
        /// </summary>
        public Dictionary<ChargingStationOperator_Id, IReadOnlyDictionary<ChargingPool_Id,   ChargingPool>>    ChargingPools    { get; } = [];

        /// <summary>
        /// The effective charging tariffs, by their charging station operators.
        /// </summary>
        public Dictionary<ChargingStationOperator_Id, IReadOnlyDictionary<ChargingTariff_Id, ChargingTariff>>  ChargingTariffs  { get; } = [];

        /// <summary>
        /// The effective parking spaces, by their parking operators.
        /// </summary>
        public Dictionary<ParkingOperator_Id,         IReadOnlyDictionary<ParkingSpace_Id,   ParkingSpace>>    ParkingSpaces    { get; } = [];

    }

}
