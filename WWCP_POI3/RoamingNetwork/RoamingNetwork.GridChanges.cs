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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Changes of the grid connection points of a roaming network.
    /// </summary>
    /// <remarks>
    /// They are embedded completely in their grid operator: a change edits the embedded JSON of the
    /// grid operator and reads it again, so that the changed grid operator is valid as a whole.
    /// Their identifications are unique within the network, so an existing one is found without its parent;
    /// adding one names its grid operator. A given parent of any other change must be the actual one.
    /// Grid connection points have no timestamps. Who may sign a change is decided by their maintainer:
    /// see the ownership of the roaming network.
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The properties of a grid connection point a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableGridConnectionPointProperties = [
            "name",
            "description",
            "address",
            "geoLocation",
            "voltageLevel",
            "connectionType",
            "nominalVoltage",
            "nominalFrequency",
            "contractedImportPower",
            "contractedExportPower",
            "contractedImportApparentPower",
            "contractedExportApparentPower",
            "connectionAgreementId",
            "networkLocationId",
            "marketLocationIds",
            "meteringLocationIds",
            "energyMeter",
            MaintainedByProperty,
            "customData"
        ];

        /// <summary>
        /// The property naming the maintainer of a grid connection point.
        /// </summary>
        public const String MaintainedByProperty = "maintainedBy";

        #endregion


        #region (private static) GridOperatorOf(GridOperators, GridConnectionPointId)

        /// <summary>
        /// The grid operator having the grid connection point with the given identification, or null.
        /// </summary>
        private static GridOperator? GridOperatorOf(IEnumerable<GridOperator>  GridOperators,
                                                    GridConnectionPoint_Id     GridConnectionPointId)

            => GridOperators.FirstOrDefault(gridOperator => gridOperator.TryGetGridConnectionPoint(GridConnectionPointId, out _));

        #endregion

        #region (private static) TryApplyGridConnectionPointChange(Change, Index, GridOperators, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a grid connection point to the given grid operators.
        /// </summary>
        private static Boolean TryApplyGridConnectionPointChange(RoamingNetworkChange                       Change,
                                                                 Int32                                      Index,
                                                                 Dictionary<GridOperator_Id, GridOperator>  GridOperators,
                                                                 out JToken?                                CurrentValue,
                                                                 [NotNullWhen(false)] out String?           ErrorResponse)
        {

            CurrentValue = null;

            #region The grid operator: the one having it, or the parent of a new one

            if (!GridConnectionPoint_Id.TryParse(Change.EntityId, out var gridConnectionPointId))
            {
                ErrorResponse = $"Change {Index}: invalid grid connection point identification '{Change.EntityId}'!";
                return false;
            }

            var gridOperator = GridOperatorOf(GridOperators.Values, gridConnectionPointId);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (gridOperator is not null)
                {
                    ErrorResponse = $"Change {Index}: the grid connection point '{gridConnectionPointId}' already exists, at the grid operator '{gridOperator.Id}'!";
                    return false;
                }

                if (Change.ParentEntityType != nameof(GridOperator) ||
                    !GridOperator_Id.TryParse(Change.ParentEntityId ?? "", out var parentId) ||
                    !GridOperators.TryGetValue(parentId, out gridOperator))
                {
                    ErrorResponse = $"Change {Index}: the grid connection point '{gridConnectionPointId}' to add needs its GridOperator, an existing one!";
                    return false;
                }

            }

            else
            {

                if (gridOperator is null)
                {
                    ErrorResponse = $"Change {Index}: the grid connection point '{gridConnectionPointId}' does not exist!";
                    return false;
                }

                if (Change.ParentEntityType is not null &&
                   (Change.ParentEntityType != nameof(GridOperator)                              ||
                    !GridOperator_Id.TryParse(Change.ParentEntityId ?? "", out var namedParentId) ||
                    namedParentId != gridOperator.Id))
                {
                    ErrorResponse = $"Change {Index}: the grid connection point '{gridConnectionPointId}' belongs to GridOperator '{gridOperator.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                    return false;
                }

            }

            var operatorJSON  = gridOperator.ToJSON();
            var children      = operatorJSON["gridConnectionPoints"] as JArray;
            // Found under the identification it is kept with: the change may name it in another spelling.
            var storedId      = gridOperator.TryGetGridConnectionPoint(gridConnectionPointId, out var stored) ? stored.Id.ToString() : null;
            var pointJSON     = Change.Kind == RoamingNetworkChangeKind.Add
                                    ? null
                                    : children?.OfType<JObject>().FirstOrDefault(child => child["@id"]?.Value<String>() == storedId);

            #endregion

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            // A property change compares the property, every other change the complete grid connection point.
            CurrentValue = isProperty
                               ? pointJSON![Change.PropertyName!]?.DeepClone()
                               : pointJSON?.DeepClone();

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, CurrentValue))
            {
                ErrorResponse = isProperty
                                    ? $"Change {Index}: the property '{Change.PropertyName}' of the grid connection point '{gridConnectionPointId}' is no longer the expected old value, but '{CurrentValue?.ToString(Newtonsoft.Json.Formatting.None) ?? "absent"}'!"
                                    : $"Change {Index}: the grid connection point '{gridConnectionPointId}' is no longer the expected old value{(CurrentValue is null ? ": it does not exist" : "")}!";
                return false;
            }

            switch (Change.Kind)
            {

                #region UpdateProperty, RemoveProperty

                case RoamingNetworkChangeKind.UpdateProperty:
                case RoamingNetworkChangeKind.RemoveProperty:

                    var propertyName = Change.PropertyName!;

                    if (!ChangeableGridConnectionPointProperties.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of a grid connection point can not be changed!";
                        return false;
                    }

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"Change {Index}: the property '{propertyName}' of the grid connection point '{gridConnectionPointId}' does not exist!";
                            return false;
                        }

                        pointJSON!.Remove(propertyName);

                    }
                    else
                        pointJSON![propertyName] = Change.NewValue;

                    break;

                #endregion

                #region Add

                case RoamingNetworkChangeKind.Add:

                    if (Change.NewValue is not JObject newPoint ||
                        newPoint["@id"]?.Type != JTokenType.String ||
                        !GridConnectionPoint_Id.TryParse(newPoint["@id"]!.Value<String>()!, out var newId) ||
                        newId != gridConnectionPointId)
                    {
                        ErrorResponse = $"Change {Index}: the grid connection point to add must be a JSON object with the identification '{gridConnectionPointId}'!";
                        return false;
                    }

                    if (children is null)
                    {
                        children = [];
                        operatorJSON["gridConnectionPoints"] = children;
                    }

                    children.Add(newPoint.DeepClone());
                    break;

                #endregion

                #region Remove

                case RoamingNetworkChangeKind.Remove:

                    children!.Remove(pointJSON!);

                    if (children.Count == 0)
                        operatorJSON.Remove("gridConnectionPoints");

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a grid connection point is not supported yet!";
                    return false;

            }

            #region The changed grid operator must still be a valid one

            if (!GridOperator.TryParse(operatorJSON, out var changedOperator, out var changedOperatorError))
            {
                ErrorResponse = $"Change {Index}: {changedOperatorError}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along, those of its meters too.
            changedOperator.ShareRuntimeOfAll(gridOperator);

            GridOperators[gridOperator.Id] = changedOperator;

            #endregion

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
