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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An energy meter a certificate of a charging station model was issued with, a component of the station as the
    /// transparency software is: a model of an energy meter manufacturer, in the hardware revisions and firmware versions
    /// the certificate names. Every energy meter of a charging station naming the certificate is one of them.
    /// </summary>
    public sealed class CertifiedEnergyMeter
    {

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the identification of its manufacturer.
            /// </summary>
            public const String  ManufacturerId     = "manufacturerId";

            /// <summary>
            /// The property of the identification of its model.
            /// </summary>
            public const String  ModelId            = "modelId";

            /// <summary>
            /// The property of the hardware revisions of its model.
            /// </summary>
            public const String  HardwareRevisions  = "hardwareRevisions";

            /// <summary>
            /// The property of the firmware versions of its model.
            /// </summary>
            public const String  FirmwareVersions   = "firmwareVersions";

        }

        #endregion

        /// <summary>
        /// The manufacturer of the energy meter model, as energy meters name it by their manufacturerId.
        /// </summary>
        public EnergyMeterManufacturer_Id  ManufacturerId     { get; }

        /// <summary>
        /// The energy meter model, one of its manufacturer, as energy meters name it by their modelId.
        /// </summary>
        public EnergyMeterModel_Id         ModelId            { get; }

        /// <summary>
        /// The hardware revisions of the model, each one of its model, in their given order.
        /// </summary>
        public ImmutableArray<String>      HardwareRevisions  { get; }

        /// <summary>
        /// The firmware versions of the model, each one of its model, in their given order.
        /// </summary>
        public ImmutableArray<String>      FirmwareVersions   { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter a certificate of a charging station model was issued with.
        /// </summary>
        /// <param name="ManufacturerId">The manufacturer of the energy meter model.</param>
        /// <param name="ModelId">The energy meter model.</param>
        /// <param name="HardwareRevisions">The hardware revisions of the model, at least one.</param>
        /// <param name="FirmwareVersions">The firmware versions of the model, at least one.</param>
        public CertifiedEnergyMeter(EnergyMeterManufacturer_Id  ManufacturerId,
                                    EnergyMeterModel_Id         ModelId,
                                    IEnumerable<String>         HardwareRevisions,
                                    IEnumerable<String>         FirmwareVersions)
        {

            if (ManufacturerId.IsNullOrEmpty)
                throw new ArgumentException("The manufacturer of a certified energy meter is required.", nameof(ManufacturerId));

            if (ModelId.IsNullOrEmpty)
                throw new ArgumentException("The model of a certified energy meter is required.", nameof(ModelId));

            this.ManufacturerId     = ManufacturerId;
            this.ModelId            = ModelId;
            this.HardwareRevisions  = ModelVersions.Of(HardwareRevisions, "hardware revision", $"the certified energy meter model '{ModelId}' of '{ManufacturerId}'");
            this.FirmwareVersions   = ModelVersions.Of(FirmwareVersions,  "firmware version",  $"the certified energy meter model '{ModelId}' of '{ManufacturerId}'");

            if (this.HardwareRevisions.IsEmpty)
                throw new ArgumentException($"The certified energy meter model '{ModelId}' of '{ManufacturerId}' must name at least one hardware revision!", nameof(HardwareRevisions));

            if (this.FirmwareVersions.IsEmpty)
                throw new ArgumentException($"The certified energy meter model '{ModelId}' of '{ManufacturerId}' must name at least one firmware version!", nameof(FirmwareVersions));

        }

        #endregion


        #region (static) Parse(JSON)

        /// <summary>
        /// Parse the given JSON representation of an energy meter a certificate was issued with, strictly.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        public static CertifiedEnergyMeter Parse(JToken JSON)
        {

            if (JSON is not JObject json)
                throw new ArgumentException("Expected an object.");

            StrictJSON.OnlyProperties(json, PropertyKeys.ManufacturerId, PropertyKeys.ModelId, PropertyKeys.HardwareRevisions, PropertyKeys.FirmwareVersions);

            return new CertifiedEnergyMeter(EnergyMeterManufacturer_Id.Parse(StrictJSON.RequiredText(json, PropertyKeys.ManufacturerId)),
                                            EnergyMeterModel_Id.       Parse(StrictJSON.RequiredText(json, PropertyKeys.ModelId)),
                                            StrictJSON.Array(json, PropertyKeys.HardwareRevisions, StrictJSON.Identifier),
                                            StrictJSON.Array(json, PropertyKeys.FirmwareVersions,  StrictJSON.Identifier));

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this energy meter, as its certificate embeds it.
        /// </summary>
        public JObject ToJSON()

            => new (new JProperty(PropertyKeys.ManufacturerId,     ManufacturerId.ToString()),
                    new JProperty(PropertyKeys.ModelId,            ModelId.       ToString()),
                    new JProperty(PropertyKeys.HardwareRevisions,  new JArray(HardwareRevisions)),
                    new JProperty(PropertyKeys.FirmwareVersions,   new JArray(FirmwareVersions)));

        #endregion


        #region Names(ManufacturerId, ModelId, HardwareRevision, FirmwareVersion)

        /// <summary>
        /// Whether this is the given model of the given manufacturer, in the given hardware revision and firmware version.
        /// </summary>
        public Boolean Names(EnergyMeterManufacturer_Id?  ManufacturerId,
                             EnergyMeterModel_Id?         ModelId,
                             String?                      HardwareRevision,
                             String?                      FirmwareVersion)

            => ManufacturerId == this.ManufacturerId &&
               ModelId        == this.ModelId        &&
               HardwareRevision is not null && HardwareRevisions.Contains(HardwareRevision, StringComparer.Ordinal) &&
               FirmwareVersion  is not null && FirmwareVersions. Contains(FirmwareVersion,  StringComparer.Ordinal);

        #endregion

        #region (override) ToString()

        /// <inheritdoc/>
        public override String ToString()

            => $"{ModelId} of {ManufacturerId}, hardware {String.Join("/", HardwareRevisions)}, firmware {String.Join("/", FirmwareVersions)}";

        #endregion

    }

}
