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
    /// The single charging station or energy meter a digital calibration certificate is for (the item of a PTB DCC): by
    /// its identification in the roaming network, and as it was calibrated, its manufacturer, model, serial number,
    /// hardware revision and firmware version.
    /// </summary>
    public sealed class QIDigitalItem
    {

        #region Data

        /// <summary>
        /// The type of a charging station.
        /// </summary>
        public const String ChargingStationType  = nameof(ChargingStation);

        /// <summary>
        /// The type of an energy meter.
        /// </summary>
        public const String EnergyMeterType      = nameof(EnergyMeter);

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
            public const String  Type              = "type";

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                = "id";

            /// <summary>
            /// The property of its manufacturer.
            /// </summary>
            public const String  ManufacturerId    = "manufacturerId";

            /// <summary>
            /// The property of its model.
            /// </summary>
            public const String  ModelId           = "modelId";

            /// <summary>
            /// The property of its serial number.
            /// </summary>
            public const String  SerialNumber      = "serialNumber";

            /// <summary>
            /// The property of its hardware revision.
            /// </summary>
            public const String  HardwareRevision  = "hardwareRevision";

            /// <summary>
            /// The property of its firmware version.
            /// </summary>
            public const String  FirmwareVersion   = "firmwareVersion";

        }

        #endregion


        /// <summary>
        /// ChargingStation or EnergyMeter.
        /// </summary>
        public String             Type                { get; }

        /// <summary>
        /// Its identification in the roaming network.
        /// </summary>
        public String             Id                  { get; }

        /// <summary>
        /// Its manufacturer, a charging station or an energy meter manufacturer by its type.
        /// </summary>
        public String?            ManufacturerId      { get; }

        /// <summary>
        /// Its model, one of its manufacturer.
        /// </summary>
        public String?            ModelId             { get; }

        /// <summary>
        /// Its serial number, as its manufacturer marked it.
        /// </summary>
        public SerialNumber?      SerialNumber        { get; }

        /// <summary>
        /// The hardware revision it was calibrated in.
        /// </summary>
        public HardwareRevision?  HardwareRevision    { get; }

        /// <summary>
        /// The firmware version it was calibrated with.
        /// </summary>
        public FirmwareVersion?   FirmwareVersion     { get; }

        /// <summary>
        /// Itself in words, e.g. "charging station 'DE*GEF*S1'".
        /// </summary>
        public String             Text
            => $"{(Type == ChargingStationType ? "charging station" : "energy meter")} '{Id}'";

        #endregion

        #region Constructor(s)

        private QIDigitalItem(String             Type,
                              String             Id,
                              String?            ManufacturerId,
                              String?            ModelId,
                              SerialNumber?      SerialNumber,
                              HardwareRevision?  HardwareRevision,
                              FirmwareVersion?   FirmwareVersion)
        {

            if (ModelId is not null && ManufacturerId is null)
                throw new ArgumentException($"The {(Type == ChargingStationType ? "charging station" : "energy meter")} '{Id}' names its model with its manufacturer!", nameof(ManufacturerId));

            if ((HardwareRevision.HasValue || FirmwareVersion.HasValue) && ModelId is null)
                throw new ArgumentException($"The {(Type == ChargingStationType ? "charging station" : "energy meter")} '{Id}' names a hardware revision or a firmware version with its model!", nameof(ModelId));

            this.Type              = Type;
            this.Id                = Id;
            this.ManufacturerId    = ManufacturerId;
            this.ModelId           = ModelId;
            this.SerialNumber      = SerialNumber;
            this.HardwareRevision  = HardwareRevision;
            this.FirmwareVersion   = FirmwareVersion;

        }

        #endregion


        #region (static) Of(ChargingStation)

        /// <summary>
        /// The given charging station as it is, e.g. as it is at runtime with its templates, with its serial number: no
        /// template gives it, it belongs to the single station.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        public static QIDigitalItem Of(ChargingStation ChargingStation)

            => new (ChargingStationType,
                    ChargingStation.Id.ToString(),
                    ChargingStation.ManufacturerId?.ToString(),
                    ChargingStation.ModelId?.       ToString(),
                    ChargingStation.SerialNumber,
                    ChargingStation.HardwareRevision,
                    ChargingStation.FirmwareVersion);

        #endregion

        #region (static) Of(EnergyMeter)

        /// <summary>
        /// The given energy meter as it is, with its serial number.
        /// </summary>
        /// <param name="EnergyMeter">An energy meter.</param>
        public static QIDigitalItem Of(EnergyMeter EnergyMeter)

            => new (EnergyMeterType,
                    EnergyMeter.Id.ToString(),
                    EnergyMeter.ManufacturerId?.ToString(),
                    EnergyMeter.ModelId?.       ToString(),
                    EnergyMeter.SerialNumber,
                    EnergyMeter.HardwareRevision,
                    EnergyMeter.FirmwareVersion);

        #endregion


        #region TryVerify(RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// Whether it is a charging station or an energy meter of the given roaming network, each as it is at runtime, with
        /// what its templates give it, of the manufacturer, the model, the serial number, the hardware revision and the
        /// firmware version it names: a charging station or an energy meter changed since does not match its certificate
        /// any more.
        /// </summary>
        public Boolean TryVerify(RoamingNetwork                    RoamingNetwork,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            QIDigitalItem actual;

            if (Type == ChargingStationType)
            {

                if (!ChargingStation_Id.TryParse(Id, out var chargingStationId)                                                           ||
                    !RoamingNetwork.TryGetChargingStationOperatorById(chargingStationId.OperatorId, out var chargingStationOperator) ||
                    !chargingStationOperator.TryGetEffectiveChargingStation(chargingStationId, out var chargingStation))
                {
                    ErrorResponse = $"The {Text} is none of the roaming network '{RoamingNetwork.Id}'!";
                    return false;
                }

                // Its own serial number, as the roaming network names it: a station replaced by another one of the same
                // identification, model and versions is not the one calibrated (csharp-review 23).
                actual = Of(chargingStation);

            }
            else
            {

                // As it is at runtime: its manufacturer, model, hardware revision and firmware version may be those of the
                // energy meter templates of its owner.
                if (!EnergyMeter_Id.TryParse(Id, out var energyMeterId) ||
                    RoamingNetwork.AllEffectiveEnergyMeters(RoamingNetwork.ChargingStationOperators, RoamingNetwork.GridOperators).
                                   FirstOrDefault(effectiveEnergyMeter => effectiveEnergyMeter.Id == energyMeterId) is not EnergyMeter energyMeter)
                {
                    ErrorResponse = $"The {Text} is none of the roaming network '{RoamingNetwork.Id}'!";
                    return false;
                }

                actual = Of(energyMeter);

            }

            if (!SameText(ManufacturerId, actual.ManufacturerId, StringComparison.OrdinalIgnoreCase))
                ErrorResponse = $"The {Text} is one of the manufacturer '{actual.ManufacturerId}', not of '{ManufacturerId}'!";

            else if (!SameText(ModelId, actual.ModelId, StringComparison.OrdinalIgnoreCase))
                ErrorResponse = $"The {Text} is one of the model '{actual.ModelId}', not of '{ModelId}'!";

            else if (SerialNumber != actual.SerialNumber)
                ErrorResponse = $"The {Text} has the serial number '{actual.SerialNumber}', not '{SerialNumber}'!";

            else if (HardwareRevision != actual.HardwareRevision)
                ErrorResponse = $"The {Text} is of the hardware revision '{actual.HardwareRevision}', not '{HardwareRevision}'!";

            else if (FirmwareVersion != actual.FirmwareVersion)
                ErrorResponse = $"The {Text} runs the firmware version '{actual.FirmwareVersion}', not '{FirmwareVersion}'!";

            else
                ErrorResponse = null;

            return ErrorResponse is null;

        }

        #endregion


        #region (static) TryParse(JSON, out Item, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the item of a digital calibration certificate.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Item">The parsed item.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out QIDigitalItem?  Item,
                                       [NotNullWhen(false)] out String?         ErrorResponse)
        {

            Item = null;

            try
            {

                if (!QIDigitalJSON.TryOnly(JSON, "an item", out ErrorResponse,
                                           PropertyKeys.Type, PropertyKeys.Id, PropertyKeys.ManufacturerId, PropertyKeys.ModelId,
                                           PropertyKeys.SerialNumber, PropertyKeys.HardwareRevision, PropertyKeys.FirmwareVersion))
                {
                    return false;
                }

                if (!POIJSON.TryParseText  (JSON, PropertyKeys.SerialNumber,     "serial number",     POI.SerialNumber.    TryParse, out SerialNumber?     serialNumber,     out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.HardwareRevision, "hardware revision", POI.HardwareRevision.TryParse, out HardwareRevision? hardwareRevision, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.FirmwareVersion,  "firmware version",  POI.FirmwareVersion. TryParse, out FirmwareVersion?  firmwareVersion,  out ErrorResponse))
                {
                    return false;
                }

                switch (QIDigitalJSON.Text(JSON, PropertyKeys.Type))
                {

                    case ChargingStationType:

                        if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.Id,             "charging station identification",               ChargingStation_Id.            TryParse, out ChargingStation_Id               chargingStationId, out ErrorResponse) ||
                            !POIJSON.      TryParseText        (JSON, PropertyKeys.ManufacturerId, "charging station manufacturer identification",  ChargingStationManufacturer_Id.TryParse, out ChargingStationManufacturer_Id?  csManufacturerId,  out ErrorResponse) ||
                            !POIJSON.      TryParseText        (JSON, PropertyKeys.ModelId,        "charging station model identification",         ChargingStationModel_Id.       TryParse, out ChargingStationModel_Id?         csModelId,         out ErrorResponse))
                        {
                            return false;
                        }

                        Item = new QIDigitalItem(ChargingStationType, chargingStationId.ToString(), csManufacturerId?.ToString(), csModelId?.ToString(), serialNumber, hardwareRevision, firmwareVersion);
                        break;

                    case EnergyMeterType:

                        if (!QIDigitalJSON.TryParseRequiredText(JSON, PropertyKeys.Id,             "energy meter identification",                   EnergyMeter_Id.                TryParse, out EnergyMeter_Id                   energyMeterId,     out ErrorResponse) ||
                            !POIJSON.      TryParseText        (JSON, PropertyKeys.ManufacturerId, "energy meter manufacturer identification",      EnergyMeterManufacturer_Id.    TryParse, out EnergyMeterManufacturer_Id?      emManufacturerId,  out ErrorResponse) ||
                            !POIJSON.      TryParseText        (JSON, PropertyKeys.ModelId,        "energy meter model identification",             EnergyMeterModel_Id.           TryParse, out EnergyMeterModel_Id?             emModelId,         out ErrorResponse))
                        {
                            return false;
                        }

                        Item = new QIDigitalItem(EnergyMeterType, energyMeterId.ToString(), emManufacturerId?.ToString(), emModelId?.ToString(), serialNumber, hardwareRevision, firmwareVersion);
                        break;

                    default:
                        ErrorResponse = $"The type of an item must be '{ChargingStationType}' or '{EnergyMeterType}'!";
                        return false;

                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Item           = null;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this item.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Type,  Type),
                           new JProperty(PropertyKeys.Id,    Id)
                       );

            if (ManufacturerId is not null)
                json.Add(PropertyKeys.ManufacturerId,    ManufacturerId);

            if (ModelId is not null)
                json.Add(PropertyKeys.ModelId,           ModelId);

            if (SerialNumber.HasValue)
                json.Add(PropertyKeys.SerialNumber,      SerialNumber.Value.ToString());

            if (HardwareRevision.HasValue)
                json.Add(PropertyKeys.HardwareRevision,  HardwareRevision.Value.ToString());

            if (FirmwareVersion.HasValue)
                json.Add(PropertyKeys.FirmwareVersion,   FirmwareVersion.Value.ToString());

            return json;

        }

        #endregion


        #region (private static) SameText(Text1, Text2, Comparison)

        private static Boolean SameText(String? Text1, String? Text2, StringComparison Comparison)

            => Text1 is null
                   ? Text2 is null
                   : Text2 is not null && String.Equals(Text1, Text2, Comparison);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => Text;

        #endregion

    }

}
