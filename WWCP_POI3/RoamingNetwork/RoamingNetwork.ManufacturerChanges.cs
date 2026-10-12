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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Changes of the charging station manufacturers of a roaming network.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The properties of a charging station manufacturer a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableManufacturerProperties = [
            "name",
            "description",
            "dataSource",
            "address",
            "homepage",
            "hotline",
            "customData"
        ];

        #endregion


        #region (private) TryApplyManufacturerChange(Change, Index, CreatedAt, Manufacturers, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a charging station manufacturer to the given manufacturers.
        /// </summary>
        private Boolean TryApplyManufacturerChange(RoamingNetworkChange                                                     Change,
                                                   Int32                                                                    Index,
                                                   DateTimeOffset                                                           CreatedAt,
                                                   Dictionary<ChargingStationManufacturer_Id, ChargingStationManufacturer>  Manufacturers,
                                                   out JToken?                                                              CurrentValue,
                                                   [NotNullWhen(false)] out String?                                         ErrorResponse)
        {

            CurrentValue = null;

            if (Change.ParentEntityType is not null &&
               (Change.ParentEntityType != nameof(POI.RoamingNetwork) || Change.ParentEntityId != Id.ToString()))
            {
                ErrorResponse = $"Change {Index}: a charging station manufacturer of this roaming network belongs to RoamingNetwork '{Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                return false;
            }

            if (!ChargingStationManufacturer_Id.TryParse(Change.EntityId, out var manufacturerId))
            {
                ErrorResponse = $"Change {Index}: invalid charging station manufacturer identification '{Change.EntityId}'!";
                return false;
            }

            Manufacturers.TryGetValue(manufacturerId, out var currentManufacturer);

            var currentJSON  = currentManufacturer?.ToJSON();
            var isProperty   = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            if (Change.Kind != RoamingNetworkChangeKind.Add && currentJSON is null)
            {
                ErrorResponse = $"Change {Index}: the charging station manufacturer '{manufacturerId}' does not exist!";
                return false;
            }

            // A property change compares the property, every other change the complete manufacturer.
            CurrentValue = isProperty
                               ? currentJSON![Change.PropertyName!]?.DeepClone()
                               : currentJSON;

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, CurrentValue))
            {
                ErrorResponse = isProperty
                                    ? $"Change {Index}: the property '{Change.PropertyName}' of the charging station manufacturer '{manufacturerId}' is no longer the expected old value, but '{CurrentValue?.ToString(Newtonsoft.Json.Formatting.None) ?? "absent"}'!"
                                    : $"Change {Index}: the charging station manufacturer '{manufacturerId}' is no longer the expected old value{(CurrentValue is null ? ": it does not exist" : "")}!";
                return false;
            }

            switch (Change.Kind)
            {

                case RoamingNetworkChangeKind.UpdateProperty:
                case RoamingNetworkChangeKind.RemoveProperty:

                    var propertyName = Change.PropertyName!;

                    if (!ChangeableManufacturerProperties.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of a charging station manufacturer can not be changed!";
                        return false;
                    }

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"Change {Index}: the property '{propertyName}' of the charging station manufacturer '{manufacturerId}' does not exist!";
                            return false;
                        }

                        currentJSON!.Remove(propertyName);

                    }
                    else
                        currentJSON![propertyName] = Change.NewValue;

                    currentJSON["lastChange"] = CreatedAt.ToISO8601();

                    if (!ChargingStationManufacturer.TryParse(currentJSON, out var changedManufacturer, out var changedError))
                    {
                        ErrorResponse = $"Change {Index}: {changedError}";
                        return false;
                    }

                    // Admin status, status and internal data are not content: they go along.
                    changedManufacturer.ShareRuntimeOf(currentManufacturer!);

                    Manufacturers[manufacturerId] = changedManufacturer;
                    break;

                case RoamingNetworkChangeKind.Add:

                    if (currentManufacturer is not null)
                    {
                        ErrorResponse = $"Change {Index}: the charging station manufacturer '{manufacturerId}' already exists!";
                        return false;
                    }

                    if (Change.NewValue is not JObject newJSON)
                    {
                        ErrorResponse = $"Change {Index}: the charging station manufacturer to add must be a JSON object!";
                        return false;
                    }

                    var added = (JObject) newJSON.DeepClone();
                    added["created"]    ??= CreatedAt.ToISO8601();
                    added["lastChange"]   = CreatedAt.ToISO8601();

                    if (!ChargingStationManufacturer.TryParse(added, out var newManufacturer, out var newError))
                    {
                        ErrorResponse = $"Change {Index}: {newError}";
                        return false;
                    }

                    if (newManufacturer.Id != manufacturerId)
                    {
                        ErrorResponse = $"Change {Index}: the charging station manufacturer '{newManufacturer.Id}' is not the one to add, '{manufacturerId}'!";
                        return false;
                    }

                    Manufacturers.Add(manufacturerId, newManufacturer);
                    break;

                case RoamingNetworkChangeKind.Remove:
                    Manufacturers.Remove(manufacturerId);
                    break;

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a charging station manufacturer is not supported yet!";
                    return false;

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
