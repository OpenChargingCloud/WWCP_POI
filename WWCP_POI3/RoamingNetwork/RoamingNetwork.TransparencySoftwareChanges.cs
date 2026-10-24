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
    /// Changes of the transparency software releases embedded in their manufacturers.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The properties of a transparency software release a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableTransparencySoftwareProperties = [
            POI.TransparencySoftware.PropertyKeys.Name,
            POI.TransparencySoftware.PropertyKeys.Version,
            POI.TransparencySoftware.PropertyKeys.OpenSourceLicenses,
            POI.TransparencySoftware.PropertyKeys.Logo,
            POI.TransparencySoftware.PropertyKeys.HowToUse,
            POI.TransparencySoftware.PropertyKeys.MoreInformation,
            POI.TransparencySoftware.PropertyKeys.SourceCodeRepository,
            POI.TransparencySoftware.PropertyKeys.SupportUntil,
            POI.TransparencySoftware.PropertyKeys.DeclarationOfConformity,
            POI.TransparencySoftware.PropertyKeys.SoftwareBillOfMaterials
        ];

        #endregion


        #region (private static) TryApplyTransparencySoftwareChange(Change, Index, CreatedAt, Manufacturers, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a transparency software release to the given manufacturers: an added one to the manufacturer
        /// its parent names, every other to the one having it.
        /// </summary>
        private static Boolean TryApplyTransparencySoftwareChange(RoamingNetworkChange                                                               Change,
                                                                  Int32                                                                              Index,
                                                                  DateTimeOffset                                                                     CreatedAt,
                                                                  Dictionary<TransparencySoftwareManufacturer_Id, TransparencySoftwareManufacturer>  Manufacturers,
                                                                  out JToken?                                                                        CurrentValue,
                                                                  [NotNullWhen(false)] out String?                                                   ErrorResponse)
        {

            CurrentValue = null;

            #region The manufacturer: the one having it, or the parent of a new one

            if (!TransparencySoftware_Id.TryParse(Change.EntityId, out var releaseId))
            {
                ErrorResponse = $"Change {Index}: invalid transparency software release identification '{Change.EntityId}'!";
                return false;
            }

            var manufacturer = TransparencySoftwareManufacturerOf(Manufacturers.Values, releaseId);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (manufacturer is not null)
                {
                    ErrorResponse = $"Change {Index}: the transparency software release '{releaseId}' already exists, at the manufacturer '{manufacturer.Id}'!";
                    return false;
                }

                if (Change.ParentEntityType != nameof(TransparencySoftwareManufacturer) ||
                    !TransparencySoftwareManufacturer_Id.TryParse(Change.ParentEntityId ?? "", out var parentId) ||
                    !Manufacturers.TryGetValue(parentId, out manufacturer))
                {
                    ErrorResponse = $"Change {Index}: the transparency software release '{releaseId}' to add needs its TransparencySoftwareManufacturer, an existing one!";
                    return false;
                }

            }

            else
            {

                if (manufacturer is null)
                {
                    ErrorResponse = $"Change {Index}: the transparency software release '{releaseId}' does not exist!";
                    return false;
                }

                if (Change.ParentEntityType is not null &&
                   (Change.ParentEntityType != nameof(TransparencySoftwareManufacturer)                              ||
                    !TransparencySoftwareManufacturer_Id.TryParse(Change.ParentEntityId ?? "", out var namedParentId) ||
                    namedParentId != manufacturer.Id))
                {
                    ErrorResponse = $"Change {Index}: the transparency software release '{releaseId}' belongs to TransparencySoftwareManufacturer '{manufacturer.Id}', not to {Change.ParentEntityType} '{Change.ParentEntityId}'!";
                    return false;
                }

            }

            var manufacturerJSON  = manufacturer.ToJSON(Embedded: true);
            var releases          = manufacturerJSON[TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware] as JArray;
            // Found under the identification it is kept with: the change may name it in another spelling.
            var storedId          = manufacturer.TryGetTransparencySoftware(releaseId, out var stored) ? stored.Id.ToString() : null;
            var releaseJSON       = Change.Kind == RoamingNetworkChangeKind.Add
                                        ? null
                                        : releases?.OfType<JObject>().FirstOrDefault(release => release[POI.TransparencySoftware.PropertyKeys.Id]?.Value<String>() == storedId);

            #endregion

            // An element change is applied as the change of the property it amounts to.
            var isElementChange = Change.IsElementChange;

            if (!TryResolveElementChange(ref Change, releaseJSON, "transparency software release", out var elementValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            // A property change compares the property, every other change the complete release.
            CurrentValue = isProperty
                               ? releaseJSON![Change.PropertyName!]?.DeepClone()
                               : releaseJSON?.DeepClone();

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, CurrentValue))
            {
                ErrorResponse = isProperty
                                    ? $"Change {Index}: the property '{Change.PropertyName}' of the transparency software release '{releaseId}' is no longer the expected old value, but '{POIJSON.ContentText(CurrentValue)}'!"
                                    : $"Change {Index}: the transparency software release '{releaseId}' is no longer the expected old value{(CurrentValue is null ? ": it does not exist" : "")}!";
                return false;
            }

            switch (Change.Kind)
            {

                #region UpdateProperty, RemoveProperty

                case RoamingNetworkChangeKind.UpdateProperty:
                case RoamingNetworkChangeKind.RemoveProperty:

                    var propertyName = Change.PropertyName!;

                    if (!ChangeableTransparencySoftwareProperties.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of a transparency software release can not be changed!";
                        return false;
                    }

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"Change {Index}: the property '{propertyName}' of the transparency software release '{releaseId}' does not exist!";
                            return false;
                        }

                        releaseJSON!.Remove(propertyName);

                    }
                    else
                        releaseJSON![propertyName] = Change.NewValue;

                    break;

                #endregion

                #region Add

                case RoamingNetworkChangeKind.Add:

                    if (Change.NewValue is not JObject newRelease ||
                        newRelease[POI.TransparencySoftware.PropertyKeys.Id]?.Type != JTokenType.String ||
                        !TransparencySoftware_Id.TryParse(newRelease[POI.TransparencySoftware.PropertyKeys.Id]!.Value<String>()!, out var newId) ||
                        newId != releaseId)
                    {
                        ErrorResponse = $"Change {Index}: the transparency software release to add must be a JSON object with the identification '{releaseId}'!";
                        return false;
                    }

                    if (releases is null)
                    {
                        releases = [];
                        manufacturerJSON[TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware] = releases;
                    }

                    releases.Add(newRelease.DeepClone());
                    break;

                #endregion

                #region Remove

                case RoamingNetworkChangeKind.Remove:

                    releases!.Remove(releaseJSON!);

                    if (releases.Count == 0)
                        manufacturerJSON.Remove(TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware);

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a transparency software release is not supported yet!";
                    return false;

            }

            // The manufacturer embedding it changed with it, at the time of the change set.
            manufacturerJSON[TransparencySoftwareManufacturer.BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

            #region The changed manufacturer must still be a valid one

            if (!TransparencySoftwareManufacturer.TryParse(manufacturerJSON, out var changedManufacturer, out var changedManufacturerError))
            {
                ErrorResponse = $"Change {Index}: {changedManufacturerError}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along.
            changedManufacturer.ShareRuntimeOf(manufacturer);

            Manufacturers[manufacturer.Id] = changedManufacturer;

            #endregion

            // The old value of an element change is the value of what it addresses.
            if (isElementChange)
                CurrentValue = elementValue;

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
