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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The floors of the last changes of effective members, as part of a version: the same for every server having the
    /// same history, also after a restart or when started from a snapshot.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region (internal) EffectiveFloors()

        /// <summary>
        /// The last changes the effective members of its operators have at least, beyond what they, their groups and what
        /// they embed tell, ordered by entity type and identification (ordinal): what a snapshot of this version keeps.
        /// </summary>
        internal IEnumerable<POIEffectiveLastChange> EffectiveFloors()

            => chargingStationOperators.SelectMany(chargingStationOperator => chargingStationOperator.EffectiveFloors()).
                   Concat(parkingOperators.SelectMany(parkingOperator => parkingOperator.EffectiveFloors())).
                   OrderBy (floor => floor.EntityType, StringComparer.Ordinal).
                   ThenBy  (floor => floor.EntityId,   StringComparer.Ordinal);

        #endregion

        #region (internal) EffectiveFloorsJSON()

        /// <summary>
        /// The floors of the last changes of effective members as a JSON array, or null when there are none.
        /// </summary>
        internal JArray? EffectiveFloorsJSON()
        {

            var floors = EffectiveFloors().ToArray();

            return floors.Length > 0
                       ? new JArray(floors.Select(floor => floor.ToJSON()))
                       : null;

        }

        #endregion

        #region (internal) TryTakeEffectiveFloors(JSON, out ErrorResponse)

        /// <summary>
        /// Take the floors of the last changes of effective members of a snapshot, as EffectiveFloorsJSON() writes them;
        /// absent, there are none.
        /// </summary>
        /// <param name="JSON">The floors, or null.</param>
        /// <param name="ErrorResponse">Why they could not be read.</param>
        internal Boolean TryTakeEffectiveFloors(JToken?                           JSON,
                                                [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            if (JSON is null || JSON.Type == JTokenType.Null)
                return true;

            if (JSON is not JArray array)
            {
                ErrorResponse = "The effective last changes of a snapshot must be a JSON array!";
                return false;
            }

            var floors = new List<POIEffectiveLastChange>(array.Count);

            foreach (var item in array)
            {

                if (item is not JObject itemJSON ||
                    !POIEffectiveLastChange.TryParse(itemJSON, out var floor, out var error))
                {
                    ErrorResponse = $"Invalid effective last change '{item.ToString(Newtonsoft.Json.Formatting.None)}' of a snapshot!";
                    return false;
                }

                floors.Add(floor);

            }

            if (floors.Count == 0)
                return true;

            foreach (var chargingStationOperator in chargingStationOperators)
                chargingStationOperator.TakeEffectiveFloors(floors);

            foreach (var parkingOperator in parkingOperators)
                parkingOperator.TakeEffectiveFloors(floors);

            return true;

        }

        #endregion


        #region (private) TakeMovedMembers(Before, ChangeSet)

        /// <summary>
        /// After the given change set was applied to the given version before it: the charging pools, charging stations
        /// and EVSEs it moved, removed and added again, take their runtime status from that version, and their last
        /// changes as they are effective are compared with it. See ChargingStationOperator.TakeMovedFrom(…).
        /// </summary>
        /// <param name="Before">The version the change set was applied to.</param>
        /// <param name="ChangeSet">The change set.</param>
        private void TakeMovedMembers(RoamingNetwork           Before,
                                      RoamingNetworkChangeSet  ChangeSet)
        {

            var added = ChangeSet.Changes.Where (change => change.Kind == RoamingNetworkChangeKind.Add &&
                                                           change.EntityType is nameof(ChargingPool) or nameof(ChargingStation) or nameof(EVSE)).
                                          Select(change => (change.EntityType, change.EntityId)).
                                          ToArray();

            if (added.Length == 0)
                return;

            foreach (var chargingStationOperator in chargingStationOperators)
                if (Before.chargingStationOperators.TryGet(chargingStationOperator.Id, out var before) &&
                    !ReferenceEquals(chargingStationOperator, before))
                {
                    chargingStationOperator.TakeMovedFrom(before, added);
                }

        }

        #endregion

        #region (internal) ShareRuntimeOfMergedParents(Left, Right)

        /// <summary>
        /// Take the runtime status of the parents of a merge, and the floors of its effective members compared with both,
        /// as the merge of a history did: also when the version of a merge commit is calculated again from its first parent.
        /// </summary>
        /// <param name="Left">The version of the first parent.</param>
        /// <param name="Right">The version of the second parent.</param>
        internal void ShareRuntimeOfMergedParents(RoamingNetwork  Left,
                                                  RoamingNetwork  Right)

            => ShareRuntimeOfMerged(Left, Right);

        #endregion

    }

}
