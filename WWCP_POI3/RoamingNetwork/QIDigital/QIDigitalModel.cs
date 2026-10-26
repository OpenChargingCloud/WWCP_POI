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
    /// The model a declaration or a certificate of conformity is for: a charging station model or an energy meter model,
    /// by its manufacturer and its identification, in the hardware revisions and firmware versions it covers, each one of
    /// the model. The object of an EU declaration of conformity (Decision 768/2008/EC, Annex III, point 4), the approved
    /// type of an EU-type examination certificate (Module B).
    /// </summary>
    public sealed class QIDigitalModel
    {

        #region Data

        /// <summary>
        /// The type of a charging station model.
        /// </summary>
        public const String ChargingStationModelType  = nameof(ChargingStationModel);

        /// <summary>
        /// The type of an energy meter model.
        /// </summary>
        public const String EnergyMeterModelType      = nameof(EnergyMeterModel);

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its type.
            /// </summary>
            public const String  Type               = "type";

            /// <summary>
            /// The property of its manufacturer.
            /// </summary>
            public const String  ManufacturerId     = "manufacturerId";

            /// <summary>
            /// The property of its model.
            /// </summary>
            public const String  ModelId            = "modelId";

            /// <summary>
            /// The property of the hardware revisions it covers.
            /// </summary>
            public const String  HardwareRevisions  = "hardwareRevisions";

            /// <summary>
            /// The property of the firmware versions it covers.
            /// </summary>
            public const String  FirmwareVersions   = "firmwareVersions";

        }

        #endregion


        /// <summary>
        /// ChargingStationModel or EnergyMeterModel.
        /// </summary>
        public String                            Type                 { get; }

        /// <summary>
        /// The manufacturer of the model, a charging station or an energy meter manufacturer by its type: the issuer of a
        /// declaration of conformity, the holder of a certificate of conformity.
        /// </summary>
        public String                            ManufacturerId       { get; }

        /// <summary>
        /// The model, one of its manufacturer.
        /// </summary>
        public String                            ModelId              { get; }

        /// <summary>
        /// The hardware revisions of the model it covers, at least one, each once, in their given order.
        /// </summary>
        public ImmutableArray<HardwareRevision>  HardwareRevisions    { get; }

        /// <summary>
        /// The firmware versions of the model it covers, at least one, each once, in their given order.
        /// </summary>
        public ImmutableArray<FirmwareVersion>   FirmwareVersions     { get; }

        /// <summary>
        /// Its manufacturer as a charging station manufacturer, when it is a charging station model.
        /// </summary>
        public ChargingStationManufacturer_Id?   ChargingStationManufacturerId
            => Type == ChargingStationModelType ? ChargingStationManufacturer_Id.Parse(ManufacturerId) : null;

        /// <summary>
        /// Its model as a charging station model, when it is one.
        /// </summary>
        public ChargingStationModel_Id?          ChargingStationModelId
            => Type == ChargingStationModelType ? ChargingStationModel_Id.Parse(ModelId) : null;

        /// <summary>
        /// Its manufacturer as an energy meter manufacturer, when it is an energy meter model.
        /// </summary>
        public EnergyMeterManufacturer_Id?       EnergyMeterManufacturerId
            => Type == EnergyMeterModelType ? EnergyMeterManufacturer_Id.Parse(ManufacturerId) : null;

        /// <summary>
        /// Its model as an energy meter model, when it is one.
        /// </summary>
        public EnergyMeterModel_Id?              EnergyMeterModelId
            => Type == EnergyMeterModelType ? EnergyMeterModel_Id.Parse(ModelId) : null;

        #endregion

        #region Constructor(s)

        private QIDigitalModel(String                          Type,
                               String                          ManufacturerId,
                               String                          ModelId,
                               IEnumerable<HardwareRevision>?  HardwareRevisions,
                               IEnumerable<FirmwareVersion>?   FirmwareVersions)
        {

            var model               = $"the {(Type == ChargingStationModelType ? "charging station" : "energy meter")} model '{ModelId}'";
            var hardwareRevisions   = ModelVersions.Of(HardwareRevisions, "hardware revision", model);
            var firmwareVersions    = ModelVersions.Of(FirmwareVersions,  "firmware version",  model);

            if (hardwareRevisions.IsEmpty)
                throw new ArgumentException($"A document of conformity of {model} names at least one hardware revision of it!", nameof(HardwareRevisions));

            if (firmwareVersions.IsEmpty)
                throw new ArgumentException($"A document of conformity of {model} names at least one firmware version of it!", nameof(FirmwareVersions));

            this.Type               = Type;
            this.ManufacturerId     = ManufacturerId;
            this.ModelId            = ModelId;
            this.HardwareRevisions  = hardwareRevisions;
            this.FirmwareVersions   = firmwareVersions;

        }

        #endregion


        #region (static) Of(ManufacturerId, ModelId, HardwareRevisions, FirmwareVersions)

        /// <summary>
        /// The given charging station model, in the given hardware revisions and firmware versions.
        /// </summary>
        public static QIDigitalModel Of(ChargingStationManufacturer_Id  ManufacturerId,
                                        ChargingStationModel_Id         ModelId,
                                        IEnumerable<HardwareRevision>   HardwareRevisions,
                                        IEnumerable<FirmwareVersion>    FirmwareVersions)

            => new (ChargingStationModelType, ManufacturerId.ToString(), ModelId.ToString(), HardwareRevisions, FirmwareVersions);


        /// <summary>
        /// The given energy meter model, in the given hardware revisions and firmware versions.
        /// </summary>
        public static QIDigitalModel Of(EnergyMeterManufacturer_Id     ManufacturerId,
                                        EnergyMeterModel_Id            ModelId,
                                        IEnumerable<HardwareRevision>  HardwareRevisions,
                                        IEnumerable<FirmwareVersion>   FirmwareVersions)

            => new (EnergyMeterModelType, ManufacturerId.ToString(), ModelId.ToString(), HardwareRevisions, FirmwareVersions);

        #endregion


        #region Covers(HardwareRevision, FirmwareVersion)

        /// <summary>
        /// Whether it covers the given hardware revision and firmware version of its model.
        /// </summary>
        public Boolean Covers(HardwareRevision  HardwareRevision,
                              FirmwareVersion   FirmwareVersion)

            => HardwareRevisions.Contains(HardwareRevision) &&
               FirmwareVersions. Contains(FirmwareVersion);

        #endregion

        #region TryCover(Item, out ErrorResponse)

        /// <summary>
        /// Whether it covers the given charging station or energy meter, e.g. the item of a digital calibration
        /// certificate: one of its model, in one of its hardware revisions and one of its firmware versions.
        /// </summary>
        /// <param name="Item">A charging station or an energy meter.</param>
        /// <param name="ErrorResponse">Why not.</param>
        public Boolean TryCover(QIDigitalItem                     Item,
                                [NotNullWhen(false)] out String?  ErrorResponse)
        {

            var itemType = Type == ChargingStationModelType ? QIDigitalItem.ChargingStationType : QIDigitalItem.EnergyMeterType;

            if (Item.Type != itemType)
                ErrorResponse = $"The {Item.Text} is no {(itemType == QIDigitalItem.ChargingStationType ? "charging station" : "energy meter")}!";

            else if (!SameId(Item.ManufacturerId, ManufacturerId) || !SameId(Item.ModelId, ModelId))
                ErrorResponse = $"The {Item.Text} is no '{ModelId}' of '{ManufacturerId}'!";

            else if (!Item.HardwareRevision.HasValue || !HardwareRevisions.Contains(Item.HardwareRevision.Value))
                ErrorResponse = $"The hardware revision '{Item.HardwareRevision}' of the {Item.Text} is none the model '{ModelId}' is covered in!";

            else if (!Item.FirmwareVersion.HasValue || !FirmwareVersions.Contains(Item.FirmwareVersion.Value))
                ErrorResponse = $"The firmware version '{Item.FirmwareVersion}' of the {Item.Text} is none the model '{ModelId}' is covered in!";

            else
                ErrorResponse = null;

            return ErrorResponse is null;

        }

        #endregion

        #region TryVerify(RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// Whether its manufacturer is one of the given roaming network, its model one of its manufacturer, and each of its
        /// hardware revisions and firmware versions one of its model.
        /// </summary>
        public Boolean TryVerify(RoamingNetwork                    RoamingNetwork,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Func<HardwareRevision, Boolean> hasHardwareRevision;
            Func<FirmwareVersion,  Boolean> hasFirmwareVersion;

            if (ChargingStationManufacturerId is ChargingStationManufacturer_Id chargingStationManufacturerId)
            {

                if (!RoamingNetwork.TryGetChargingStationManufacturerById(chargingStationManufacturerId, out var manufacturer))
                {
                    ErrorResponse = $"The charging station manufacturer '{ManufacturerId}' is none of the roaming network '{RoamingNetwork.Id}'!";
                    return false;
                }

                if (!manufacturer.Models.Index.TryGetValue(ChargingStationModelId!.Value, out var model))
                {
                    ErrorResponse = $"The charging station model '{ModelId}' is none of '{ManufacturerId}'!";
                    return false;
                }

                hasHardwareRevision  = model.HasHardwareRevision;
                hasFirmwareVersion   = model.HasFirmwareVersion;

            }
            else
            {

                if (!RoamingNetwork.TryGetEnergyMeterManufacturerById(EnergyMeterManufacturerId!.Value, out var manufacturer))
                {
                    ErrorResponse = $"The energy meter manufacturer '{ManufacturerId}' is none of the roaming network '{RoamingNetwork.Id}'!";
                    return false;
                }

                if (!manufacturer.Models.Index.TryGetValue(EnergyMeterModelId!.Value, out var model))
                {
                    ErrorResponse = $"The energy meter model '{ModelId}' is none of '{ManufacturerId}'!";
                    return false;
                }

                hasHardwareRevision  = model.HasHardwareRevision;
                hasFirmwareVersion   = model.HasFirmwareVersion;

            }

            foreach (var hardwareRevision in HardwareRevisions)
                if (!hasHardwareRevision(hardwareRevision))
                {
                    ErrorResponse = $"The hardware revision '{hardwareRevision}' is none of the model '{ModelId}' of '{ManufacturerId}'!";
                    return false;
                }

            foreach (var firmwareVersion in FirmwareVersions)
                if (!hasFirmwareVersion(firmwareVersion))
                {
                    ErrorResponse = $"The firmware version '{firmwareVersion}' is none of the model '{ModelId}' of '{ManufacturerId}'!";
                    return false;
                }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (static) TryParse(JSON, out Model, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the model of a document of conformity.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Model">The parsed model.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out QIDigitalModel?  Model,
                                       [NotNullWhen(false)] out String?          ErrorResponse)
        {

            Model = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "a model", out ErrorResponse,
                                           PropertyKeys.Type, PropertyKeys.ManufacturerId, PropertyKeys.ModelId,
                                           PropertyKeys.HardwareRevisions, PropertyKeys.FirmwareVersions))
                {
                    return false;
                }

                if (!POIJSON.TryParseIds<HardwareRevision>(JSON, PropertyKeys.HardwareRevisions, "hardware revision", HardwareRevision.TryParse, out var hardwareRevisions, out ErrorResponse) ||
                    !POIJSON.TryParseIds<FirmwareVersion> (JSON, PropertyKeys.FirmwareVersions,  "firmware version",  FirmwareVersion. TryParse, out var firmwareVersions,  out ErrorResponse))
                {
                    return false;
                }

                switch (QIDigitalJSON.Text(JSON, PropertyKeys.Type))
                {

                    case ChargingStationModelType:

                        if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ManufacturerId, "charging station manufacturer identification", ChargingStationManufacturer_Id.TryParse, out ChargingStationManufacturer_Id chargingStationManufacturerId, out ErrorResponse) ||
                            !QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ModelId,        "charging station model identification",        ChargingStationModel_Id.       TryParse, out ChargingStationModel_Id        chargingStationModelId,        out ErrorResponse))
                        {
                            return false;
                        }

                        Model = Of(chargingStationManufacturerId, chargingStationModelId, hardwareRevisions, firmwareVersions);
                        break;

                    case EnergyMeterModelType:

                        if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ManufacturerId, "energy meter manufacturer identification",     EnergyMeterManufacturer_Id.    TryParse, out EnergyMeterManufacturer_Id     energyMeterManufacturerId,     out ErrorResponse) ||
                            !QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.ModelId,        "energy meter model identification",            EnergyMeterModel_Id.           TryParse, out EnergyMeterModel_Id            energyMeterModelId,            out ErrorResponse))
                        {
                            return false;
                        }

                        Model = Of(energyMeterManufacturerId, energyMeterModelId, hardwareRevisions, firmwareVersions);
                        break;

                    default:
                        ErrorResponse = $"The type of a model must be '{ChargingStationModelType}' or '{EnergyMeterModelType}'!";
                        return false;

                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Model          = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this model.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty(PropertyKeys.Type,               Type),
                   new JProperty(PropertyKeys.ManufacturerId,     ManufacturerId),
                   new JProperty(PropertyKeys.ModelId,            ModelId),
                   new JProperty(PropertyKeys.HardwareRevisions,  POIJSON.IdsToJSON(HardwareRevisions)),
                   new JProperty(PropertyKeys.FirmwareVersions,   POIJSON.IdsToJSON(FirmwareVersions))
               );

        #endregion


        #region (internal static) SameId(Id1, Id2)

        /// <summary>
        /// Whether both are the same identification: as identifications compare, case-insensitively.
        /// </summary>
        internal static Boolean SameId(String? Id1, String? Id2)

            => Id1 is not null && Id2 is not null &&
               String.Equals(Id1, Id2, StringComparison.OrdinalIgnoreCase);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{ModelId} of {ManufacturerId} (hardware {String.Join("/", HardwareRevisions)}, firmware {String.Join("/", FirmwareVersions)})";

        #endregion

    }

}
