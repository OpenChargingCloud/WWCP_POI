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
    /// Changes of the models of charging stations and energy meters embedded in their manufacturers.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region (private) ModelKind<TManufacturerId, TManufacturer, TModelId>

        /// <summary>
        /// What a change of a model needs to know about its kind.
        /// </summary>
        /// <param name="Description">The model in words, e.g. "charging station model".</param>
        /// <param name="ManufacturerType">The entity type of its manufacturers.</param>
        /// <param name="ModelsProperty">The property of its manufacturers embedding their models.</param>
        /// <param name="ChangeableProperties">The properties a change set can update or remove.</param>
        /// <param name="TryParseManufacturerId">Parses the identification of a manufacturer.</param>
        /// <param name="TryParseModelId">Parses the identification of a model.</param>
        /// <param name="TryParseManufacturer">Parses the JSON representation of a manufacturer.</param>
        /// <param name="StoredModelId">The identification a manufacturer keeps the given model with, or null.</param>
        /// <param name="ToJSON">The JSON representation of a manufacturer, with its models.</param>
        /// <param name="GetId">The identification of a manufacturer.</param>
        /// <param name="ShareRuntimeOf">Lets a changed manufacturer take the runtime status and internal data of its previous version.</param>
        private sealed record ModelKind<TManufacturerId, TManufacturer, TModelId>(String                                Description,
                                                                                 String                                ManufacturerType,
                                                                                 String                                ModelsProperty,
                                                                                 ImmutableHashSet<String>              ChangeableProperties,
                                                                                 POITextParser<TManufacturerId>        TryParseManufacturerId,
                                                                                 POITextParser<TModelId>               TryParseModelId,
                                                                                 POIObjectParser<TManufacturer>        TryParseManufacturer,
                                                                                 Func<TManufacturer, TModelId, String?>  StoredModelId,
                                                                                 Func<TManufacturer, JObject>          ToJSON,
                                                                                 Func<TManufacturer, TManufacturerId>  GetId,
                                                                                 Action<TManufacturer, TManufacturer>  ShareRuntimeOf)

            where TManufacturerId : notnull
            where TManufacturer   : class
            where TModelId        : notnull;

        #endregion

        #region Data

        /// <summary>
        /// The properties of a charging station model a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableChargingStationModelProperties = [
            ChargingStationModel.PropertyKeys.Name,
            ChargingStationModel.PropertyKeys.Description,
            ChargingStationModel.PropertyKeys.Versions,
            ChargingStationModel.PropertyKeys.ProductURL,
            ChargingStationModel.PropertyKeys.DatasheetURL,
            ChargingStationModel.PropertyKeys.Images,
            ChargingStationModel.PropertyKeys.MaxPower,
            ChargingStationModel.PropertyKeys.CurrentTypes,
            ChargingStationModel.PropertyKeys.ConnectorTypes,
            ChargingStationModel.PropertyKeys.NumberOfEVSEs,
            ChargingStationModel.PropertyKeys.CustomData
        ];

        /// <summary>
        /// The properties of an energy meter model a change set can update or remove.
        /// </summary>
        private static readonly ImmutableHashSet<String> ChangeableEnergyMeterModelProperties = [
            EnergyMeterModel.PropertyKeys.Name,
            EnergyMeterModel.PropertyKeys.Description,
            EnergyMeterModel.PropertyKeys.Versions,
            EnergyMeterModel.PropertyKeys.ProductURL,
            EnergyMeterModel.PropertyKeys.DatasheetURL,
            EnergyMeterModel.PropertyKeys.Images,
            EnergyMeterModel.PropertyKeys.CurrentTypes,
            EnergyMeterModel.PropertyKeys.MaxCurrent,
            EnergyMeterModel.PropertyKeys.AccuracyClass,
            EnergyMeterModel.PropertyKeys.Measurands,
            EnergyMeterModel.PropertyKeys.SignatureMethods,
            EnergyMeterModel.PropertyKeys.CustomData
        ];

        private static readonly ModelKind<ChargingStationManufacturer_Id, ChargingStationManufacturer, ChargingStationModel_Id> ChargingStationModels = new (
            "charging station model",
            nameof(ChargingStationManufacturer),
            ChargingStationManufacturer.PropertyKeys.Models,
            ChangeableChargingStationModelProperties,
            ChargingStationManufacturer_Id.TryParse,
            ChargingStationModel_Id.       TryParse,
            ChargingStationManufacturer.   TryParse,
            (manufacturer, modelId) => manufacturer.TryGetModel(modelId, out var model) ? model.Id.ToString() : null,
            manufacturer            => manufacturer.ToJSON(Embedded: true),
            manufacturer            => manufacturer.Id,
            // Its models have no runtime status.
            (changed, previous)     => changed.ShareRuntimeOf(previous)
        );

        private static readonly ModelKind<EnergyMeterManufacturer_Id, EnergyMeterManufacturer, EnergyMeterModel_Id> EnergyMeterModels = new (
            "energy meter model",
            nameof(EnergyMeterManufacturer),
            EnergyMeterManufacturer.PropertyKeys.Models,
            ChangeableEnergyMeterModelProperties,
            EnergyMeterManufacturer_Id.TryParse,
            EnergyMeterModel_Id.       TryParse,
            EnergyMeterManufacturer.   TryParse,
            (manufacturer, modelId) => manufacturer.TryGetModel(modelId, out var model) ? model.Id.ToString() : null,
            manufacturer            => manufacturer.ToJSON(Embedded: true),
            manufacturer            => manufacturer.Id,
            (changed, previous)     => changed.ShareRuntimeOf(previous)
        );

        #endregion


        #region (private static) TryApplyModelChange<...>(Kind, Change, Index, CreatedAt, Manufacturers, out CurrentValue, out ErrorResponse)

        /// <summary>
        /// Apply a change of a model to the manufacturer its parent names: its identification is unique within its
        /// manufacturer only.
        /// </summary>
        private static Boolean TryApplyModelChange<TManufacturerId, TManufacturer, TModelId>(ModelKind<TManufacturerId, TManufacturer, TModelId>  Kind,
                                                                                            RoamingNetworkChange                                 Change,
                                                                                            Int32                                                Index,
                                                                                            DateTimeOffset                                       CreatedAt,
                                                                                            Dictionary<TManufacturerId, TManufacturer>           Manufacturers,
                                                                                            out JToken?                                          CurrentValue,
                                                                                            [NotNullWhen(false)] out String?                     ErrorResponse)

            where TManufacturerId : notnull
            where TManufacturer   : class
            where TModelId        : notnull

        {

            CurrentValue = null;

            var description = Kind.Description;

            #region The manufacturer its parent names, and the model in it

            if (!Kind.TryParseModelId(Change.EntityId, out var modelId))
            {
                ErrorResponse = $"Change {Index}: invalid {description} identification '{Change.EntityId}'!";
                return false;
            }

            if (Change.ParentEntityType != Kind.ManufacturerType                              ||
                !Kind.TryParseManufacturerId(Change.ParentEntityId ?? "", out var manufacturerId) ||
                !Manufacturers.TryGetValue(manufacturerId, out var manufacturer))
            {
                ErrorResponse = $"Change {Index}: the {description} '{modelId}' needs its {Kind.ManufacturerType}, an existing one!";
                return false;
            }

            var manufacturerJSON  = Kind.ToJSON(manufacturer);
            var models            = manufacturerJSON[Kind.ModelsProperty] as JArray;
            // Found under the identification it is kept with: the change may name it in another spelling.
            var storedId          = Kind.StoredModelId(manufacturer, modelId);
            var modelJSON         = storedId is null
                                        ? null
                                        : models?.OfType<JObject>().FirstOrDefault(model => model[ChargingStationModel.PropertyKeys.Id]?.Value<String>() == storedId);

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {
                if (modelJSON is not null)
                {
                    ErrorResponse = $"Change {Index}: the {description} '{modelId}' already exists, at the manufacturer '{Kind.GetId(manufacturer)}'!";
                    return false;
                }
            }

            else if (modelJSON is null)
            {
                ErrorResponse = $"Change {Index}: the {description} '{modelId}' of the manufacturer '{Kind.GetId(manufacturer)}' does not exist!";
                return false;
            }

            #endregion

            // An element change is applied as the change of the property it amounts to.
            var isElementChange = Change.IsElementChange;

            if (!TryResolveElementChange(ref Change, modelJSON, description, out var elementValue, out ErrorResponse))
            {
                ErrorResponse = $"Change {Index}: {ErrorResponse}";
                return false;
            }

            var isProperty = Change.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty;

            // A property change compares the property, every other change the complete model.
            CurrentValue = isProperty
                               ? modelJSON![Change.PropertyName!]?.DeepClone()
                               : modelJSON?.DeepClone();

            if (Change.OldValue is JToken expected &&
                !OldValueMatches(expected, CurrentValue))
            {
                ErrorResponse = isProperty
                                    ? $"Change {Index}: the property '{Change.PropertyName}' of the {description} '{modelId}' is no longer the expected old value, but '{POIJSON.ContentText(CurrentValue)}'!"
                                    : $"Change {Index}: the {description} '{modelId}' is no longer the expected old value{(CurrentValue is null ? ": it does not exist" : "")}!";
                return false;
            }

            switch (Change.Kind)
            {

                #region UpdateProperty, RemoveProperty

                case RoamingNetworkChangeKind.UpdateProperty:
                case RoamingNetworkChangeKind.RemoveProperty:

                    var propertyName = Change.PropertyName!;

                    if (!Kind.ChangeableProperties.Contains(propertyName))
                    {
                        ErrorResponse = $"Change {Index}: the property '{propertyName}' of a {description} can not be changed!";
                        return false;
                    }

                    if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty)
                    {

                        if (CurrentValue is null)
                        {
                            ErrorResponse = $"Change {Index}: the property '{propertyName}' of the {description} '{modelId}' does not exist!";
                            return false;
                        }

                        modelJSON!.Remove(propertyName);

                    }
                    else
                        modelJSON![propertyName] = Change.NewValue;

                    break;

                #endregion

                #region Add

                case RoamingNetworkChangeKind.Add:

                    if (Change.NewValue is not JObject newModel ||
                        newModel[ChargingStationModel.PropertyKeys.Id]?.Type != JTokenType.String ||
                        !Kind.TryParseModelId(newModel[ChargingStationModel.PropertyKeys.Id]!.Value<String>()!, out var newId) ||
                        !newId.Equals(modelId))
                    {
                        ErrorResponse = $"Change {Index}: the {description} to add must be a JSON object with the identification '{modelId}'!";
                        return false;
                    }

                    if (models is null)
                    {
                        models = [];
                        manufacturerJSON[Kind.ModelsProperty] = models;
                    }

                    models.Add(newModel.DeepClone());
                    break;

                #endregion

                #region Remove

                case RoamingNetworkChangeKind.Remove:

                    models!.Remove(modelJSON!);

                    if (models.Count == 0)
                        manufacturerJSON.Remove(Kind.ModelsProperty);

                    break;

                #endregion

                default:
                    ErrorResponse = $"Change {Index}: {Change.Kind} of a {description} is not supported yet!";
                    return false;

            }

            // The manufacturer embedding it changed with it, at the time of the change set.
            manufacturerJSON[BasePropertyKeys.LastChange] = CreatedAt.ToISO8601();

            #region The changed manufacturer must still be a valid one

            if (!Kind.TryParseManufacturer(manufacturerJSON, out var changedManufacturer, out var changedManufacturerError))
            {
                ErrorResponse = $"Change {Index}: {changedManufacturerError}";
                return false;
            }

            // Admin status, status and internal data are not content: they go along.
            Kind.ShareRuntimeOf(changedManufacturer, manufacturer);

            Manufacturers[Kind.GetId(manufacturer)] = changedManufacturer;

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
