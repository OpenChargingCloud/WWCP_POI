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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The floors of the last changes of the effective members of a charging station operator, as part of its version:
    /// kept by a snapshot, and found again for what a change set moved.
    /// </summary>
    public sealed partial class ChargingStationOperator
    {

        #region (internal) EffectiveFloors()

        /// <summary>
        /// The last changes its effective EVSEs, charging stations and charging pools have at least, beyond what they, their
        /// groups and what they embed tell: what a snapshot of a version keeps.
        /// </summary>
        internal IEnumerable<POIEffectiveLastChange> EffectiveFloors()

            => effectiveFloors.EVSEs.           Select(floor => new POIEffectiveLastChange(nameof(EVSE),            floor.Key.ToString(), floor.Value)).Concat(
               effectiveFloors.ChargingStations.Select(floor => new POIEffectiveLastChange(nameof(ChargingStation), floor.Key.ToString(), floor.Value))).Concat(
               effectiveFloors.ChargingPools.   Select(floor => new POIEffectiveLastChange(nameof(ChargingPool),    floor.Key.ToString(), floor.Value)));

        #endregion

        #region (internal) TakeEffectiveFloors(Floors)

        /// <summary>
        /// Take the given floors of the last changes of effective members, e.g. of a snapshot, and calculate the effective
        /// members again with them. Floors of members it does not have are passed over; of two the later one is kept.
        /// </summary>
        /// <param name="Floors">Floors, also of other operators.</param>
        internal void TakeEffectiveFloors(IEnumerable<POIEffectiveLastChange> Floors)
        {

            var evses             = new Dictionary<EVSE_Id,            DateTimeOffset>(effectiveFloors.EVSEs);
            var chargingStations  = new Dictionary<ChargingStation_Id, DateTimeOffset>(effectiveFloors.ChargingStations);
            var chargingPools     = new Dictionary<ChargingPool_Id,    DateTimeOffset>(effectiveFloors.ChargingPools);
            var found             = false;

            foreach (var floor in Floors)
            {
                switch (floor.EntityType)
                {

                    case nameof(EVSE):
                        if (EVSE_Id.TryParse(floor.EntityId, out var evseId) && TryGetEVSE(evseId, out var evse))
                            found = Raise(evses, evse.Id, floor.LastChange) | found;
                        break;

                    case nameof(ChargingStation):
                        if (ChargingStation_Id.TryParse(floor.EntityId, out var chargingStationId) && TryGetChargingStation(chargingStationId, out var chargingStation))
                            found = Raise(chargingStations, chargingStation.Id, floor.LastChange) | found;
                        break;

                    case nameof(ChargingPool):
                        if (ChargingPool_Id.TryParse(floor.EntityId, out var chargingPoolId) && TryGetChargingPool(chargingPoolId, out var chargingPool))
                            found = Raise(chargingPools, chargingPool.Id, floor.LastChange) | found;
                        break;

                }
            }

            if (!found)
                return;

            effectiveFloors = new (evses, chargingStations, chargingPools);

            (effectiveEVSEs, effectiveChargingStations, effectiveChargingPoolIndex) = CalculateEffective(effectiveFloors);
            effectiveChargingPools = new (CalculateEffectiveChargingPools);

            ShareRuntimeOfEffective();

        }

        #endregion

        #region (internal) TakeMovedFrom(Before, Added)

        /// <summary>
        /// After a change set: of the charging pools, charging stations and EVSEs it added that the given version before it
        /// had already, i.e. it moved them to another charging pool or charging station, or removed and added them again,
        /// take the runtime status from that version, with the one of their energy meters, and compare them with it as they
        /// are effective. Between their removal and their addition they did not exist, so neither was done change by
        /// change; but an EVSE moved under another charging station or charging pool may take other tariffs of their
        /// groups, though neither it nor a group changed.
        /// </summary>
        /// <param name="Before">This operator in the version before the change set.</param>
        /// <param name="Added">The entities the change set added, also of other operators: their types and identifications.</param>
        internal void TakeMovedFrom(ChargingStationOperator                          Before,
                                    IEnumerable<(String EntityType, String EntityId)>  Added)
        {

            var chargingPools     = new List<ChargingPool>();
            var chargingStations  = new List<ChargingStation>();
            var evses             = new List<EVSE>();

            foreach (var (entityType, entityId) in Added)
            {
                switch (entityType)
                {

                    case nameof(ChargingPool):
                        if (ChargingPool_Id.TryParse(entityId, out var chargingPoolId) && TryGetChargingPool(chargingPoolId, out var chargingPool))
                        {
                            chargingPools.   Add     (chargingPool);
                            chargingStations.AddRange(chargingPool.ChargingStations);
                            evses.           AddRange(chargingPool.EVSEs);
                        }
                        break;

                    case nameof(ChargingStation):
                        if (ChargingStation_Id.TryParse(entityId, out var chargingStationId) && TryGetChargingStation(chargingStationId, out var chargingStation))
                        {
                            chargingStations.Add     (chargingStation);
                            evses.           AddRange(chargingStation.EVSEs);
                        }
                        break;

                    case nameof(EVSE):
                        if (EVSE_Id.TryParse(entityId, out var evseId) && TryGetEVSE(evseId, out var evse))
                            evses.Add(evse);
                        break;

                }
            }

            #region Only those it had before: their runtime status, and the one of their energy meters

            var movedEVSEs             = new HashSet<EVSE_Id>();
            var movedChargingStations  = new HashSet<ChargingStation_Id>();

            void ShareRuntimeOfEnergyMeters(IEnumerable<EnergyMeter> EnergyMeters)
            {
                foreach (var energyMeter in EnergyMeters)
                    if (Before.TryGetEnergyMeter(energyMeter.Id, out var previousEnergyMeter))
                        energyMeter.ShareRuntimeOf(previousEnergyMeter);
            }

            foreach (var chargingPool in chargingPools)
                if (Before.TryGetChargingPool(chargingPool.Id, out var previousChargingPool))
                {
                    chargingPool.ShareRuntimeOf(previousChargingPool);
                    ShareRuntimeOfEnergyMeters(chargingPool.EnergyMeters);
                }

            foreach (var chargingStation in chargingStations)
                if (Before.TryGetChargingStation(chargingStation.Id, out var previousChargingStation))
                {
                    chargingStation.ShareRuntimeOf(previousChargingStation);
                    ShareRuntimeOfEnergyMeters(chargingStation.EnergyMeters);
                    movedChargingStations.Add(chargingStation.Id);
                }

            foreach (var evse in evses)
                if (Before.TryGetEVSE(evse.Id, out var previousEVSE))
                {
                    evse.ShareRuntimeOf(previousEVSE);
                    if (evse.EnergyMeter is EnergyMeter energyMeter)
                        ShareRuntimeOfEnergyMeters([ energyMeter ]);
                    movedEVSEs.Add(evse.Id);
                }

            #endregion

            if (movedEVSEs.Count == 0 && movedChargingStations.Count == 0)
                return;

            StampChangedEffective(Before, movedEVSEs, movedChargingStations);
            ShareRuntimeOfEffective();

        }

        #endregion

    }

}
