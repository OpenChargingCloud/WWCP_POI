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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A model of charging stations: an immutable description, embedded completely in its charging station manufacturer.
    /// Charging stations, charging station templates and transparency software certificates name it by its manufacturer
    /// and its identification, which is unique within its manufacturer.
    /// </summary>
    /// <remarks>
    /// Its manufacturer states it and signs its changes. Its hardware revisions are those it was built in, its firmware
    /// versions those it ran: a charging station of it names one of each, a certificate those it covers. Its technical
    /// data are the ratings of the model in their base units: maxPower in W. A charging station of it may have less, its
    /// EVSEs name what they have.
    /// </remarks>
    public sealed class ChargingStationModel
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingStationModel/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id                = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context           = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name              = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description       = "description";

            /// <summary>
            /// The property of its hardware revisions.
            /// </summary>
            public const String  HardwareRevisions = "hardwareRevisions";

            /// <summary>
            /// The property of its firmware versions.
            /// </summary>
            public const String  FirmwareVersions  = "firmwareVersions";

            /// <summary>
            /// The property of the URL of its product page.
            /// </summary>
            public const String  ProductURL        = "productURL";

            /// <summary>
            /// The property of the URL of its datasheet.
            /// </summary>
            public const String  DatasheetURL      = "datasheetURL";

            /// <summary>
            /// The property of its images.
            /// </summary>
            public const String  Images            = "images";

            /// <summary>
            /// The property of its maximum power.
            /// </summary>
            public const String  MaxPower          = "maxPower";

            /// <summary>
            /// The property of its current types.
            /// </summary>
            public const String  CurrentTypes      = "currentTypes";

            /// <summary>
            /// The property of the types of its charging connectors.
            /// </summary>
            public const String  ConnectorTypes    = "connectorTypes";

            /// <summary>
            /// The property of its number of EVSEs.
            /// </summary>
            public const String  NumberOfEVSEs     = "numberOfEVSEs";

            /// <summary>
            /// The property of its custom data.
            /// </summary>
            public const String  CustomData        = "customData";

            /// <summary>
            /// The property of the identification of its manufacturer, refused: it is embedded in its manufacturer.
            /// </summary>
            public const String  ManufacturerId    = "manufacturerId";

        }

        #endregion


        /// <summary>
        /// The identification of the model, assigned by its manufacturer, e.g. "GD-DC300".
        /// </summary>
        public ChargingStationModel_Id               Id                { get; }

        /// <summary>
        /// The multi-language name.
        /// </summary>
        public ImmutableI18NString?                  Name              { get; }

        /// <summary>
        /// The multi-language description.
        /// </summary>
        public ImmutableI18NString?                  Description       { get; }

        /// <summary>
        /// The hardware revisions it was built in, each once, in their given order.
        /// </summary>
        public ImmutableArray<HardwareRevision>      HardwareRevisions { get; }

        /// <summary>
        /// The firmware versions it ran, each once, in their given order.
        /// </summary>
        public ImmutableArray<FirmwareVersion>       FirmwareVersions  { get; }

        /// <summary>
        /// The URL of its product page.
        /// </summary>
        public URL?                                  ProductURL        { get; }

        /// <summary>
        /// The URL of its datasheet.
        /// </summary>
        public URL?                                  DatasheetURL      { get; }

        /// <summary>
        /// Images of it, e.g. photos, each URL once, in their given order.
        /// </summary>
        public ImmutableArray<Image>                 Images            { get; }

        /// <summary>
        /// The maximum power of a charging station of it.
        /// </summary>
        public Watt?                                 MaxPower          { get; }

        /// <summary>
        /// Its current types.
        /// </summary>
        public CurrentTypes?                         CurrentTypes      { get; }

        /// <summary>
        /// The types of its charging connectors, ordered by their identifications.
        /// </summary>
        public ImmutableArray<ChargingConnectorType> ConnectorTypes    { get; }

        /// <summary>
        /// The number of EVSEs of a charging station of it.
        /// </summary>
        public Int32?                                NumberOfEVSEs     { get; }

        /// <summary>
        /// Custom data.
        /// </summary>
        public CustomDataNew?                        CustomData        { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station model.
        /// </summary>
        /// <param name="Id">The identification of the model, unique within its manufacturer.</param>
        /// <param name="Name">The multi-language name.</param>
        /// <param name="Description">The multi-language description.</param>
        /// <param name="HardwareRevisions">The hardware revisions it was built in.</param>
        /// <param name="FirmwareVersions">The firmware versions it ran.</param>
        /// <param name="ProductURL">The URL of its product page.</param>
        /// <param name="DatasheetURL">The URL of its datasheet.</param>
        /// <param name="Images">Images of it, e.g. photos.</param>
        /// <param name="MaxPower">The maximum power of a charging station of it.</param>
        /// <param name="CurrentTypes">Its current types.</param>
        /// <param name="ConnectorTypes">The types of its charging connectors.</param>
        /// <param name="NumberOfEVSEs">The number of EVSEs of a charging station of it.</param>
        /// <param name="CustomData">Custom data.</param>
        public ChargingStationModel(ChargingStationModel_Id              Id,
                                    ImmutableI18NString?                 Name              = null,
                                    ImmutableI18NString?                 Description       = null,
                                    IEnumerable<HardwareRevision>?       HardwareRevisions = null,
                                    IEnumerable<FirmwareVersion>?        FirmwareVersions  = null,
                                    URL?                                 ProductURL        = null,
                                    URL?                                 DatasheetURL      = null,
                                    IEnumerable<Image>?                  Images            = null,
                                    Watt?                                MaxPower          = null,
                                    CurrentTypes?                        CurrentTypes      = null,
                                    IEnumerable<ChargingConnectorType>?  ConnectorTypes    = null,
                                    Int32?                               NumberOfEVSEs     = null,
                                    CustomDataNew?                       CustomData        = null)
        {

            if (MaxPower?.Value <= 0)
                throw new ArgumentException($"The maximum power of the charging station model '{Id}' must be positive!", nameof(MaxPower));

            if (NumberOfEVSEs < 1)
                throw new ArgumentException($"The number of EVSEs of the charging station model '{Id}' must be at least one!", nameof(NumberOfEVSEs));

            this.Id                = Id;
            this.Name              = Name;
            this.Description       = Description;
            this.HardwareRevisions = ModelVersions.Of(HardwareRevisions, "hardware revision", $"the charging station model '{Id}'");
            this.FirmwareVersions  = ModelVersions.Of(FirmwareVersions,  "firmware version",  $"the charging station model '{Id}'");
            this.ProductURL        = ProductURL;
            this.DatasheetURL      = DatasheetURL;
            this.Images            = POIJSON.Distinct  (Images, image => image.URL.ToString(), $"image of the charging station model '{Id}'");
            this.MaxPower          = MaxPower;
            this.CurrentTypes      = CurrentTypes == POI.CurrentTypes.Unspecified ? null : CurrentTypes;
            this.ConnectorTypes    = POIJSON.OrderedIds(ConnectorTypes, $"connector type of the charging station model '{Id}'");
            this.NumberOfEVSEs     = NumberOfEVSEs;
            this.CustomData        = CustomData;

        }

        #endregion


        #region HasHardwareRevision(HardwareRevision)

        /// <summary>
        /// Whether the given hardware revision is one of its hardware revisions.
        /// </summary>
        public Boolean HasHardwareRevision(HardwareRevision HardwareRevision)

            => HardwareRevisions.Contains(HardwareRevision);

        #endregion

        #region HasFirmwareVersion(FirmwareVersion)

        /// <summary>
        /// Whether the given firmware version is one of its firmware versions.
        /// </summary>
        public Boolean HasFirmwareVersion(FirmwareVersion FirmwareVersion)

            => FirmwareVersions.Contains(FirmwareVersion);

        #endregion


        #region ToJSON(Embedded = false, CustomChargingStationModelSerializer = null)

        /// <summary>
        /// Return the JSON representation of this charging station model, as its manufacturer embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingStationModelSerializer">A delegate to serialize custom charging station model JSON objects.</param>
        public JObject ToJSON(Boolean                                                 Embedded                               = false,
                              CustomJObjectSerializerDelegate<ChargingStationModel>?  CustomChargingStationModelSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,         JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,            Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,     Description.ToJSON());

            if (HardwareRevisions.Length > 0)
                json.Add(PropertyKeys.HardwareRevisions, POIJSON.IdsToJSON(HardwareRevisions));

            if (FirmwareVersions.Length > 0)
                json.Add(PropertyKeys.FirmwareVersions,  POIJSON.IdsToJSON(FirmwareVersions));

            if (ProductURL.HasValue)
                json.Add(PropertyKeys.ProductURL,      ProductURL.Value.ToString());

            if (DatasheetURL.HasValue)
                json.Add(PropertyKeys.DatasheetURL,    DatasheetURL.Value.ToString());

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,          new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (MaxPower.HasValue)
                json.Add(PropertyKeys.MaxPower,        POIJSON.Number(MaxPower.Value.Value));

            if (CurrentTypes.HasValue)
                json.Add(PropertyKeys.CurrentTypes,    POIJSON.FlagsToJSON(CurrentTypes.Value));

            if (ConnectorTypes.Length > 0)
                json.Add(PropertyKeys.ConnectorTypes,  POIJSON.IdsToJSON(ConnectorTypes));

            if (NumberOfEVSEs.HasValue)
                json.Add(PropertyKeys.NumberOfEVSEs,   NumberOfEVSEs.Value);

            if (CustomData is not null && CustomData.HasValues)
                json.Add(PropertyKeys.CustomData,      CustomData.ToJObject());

            return CustomChargingStationModelSerializer is not null
                       ? CustomChargingStationModelSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingStationModel, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station model.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationModel">The parsed charging station model.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out ChargingStationModel?  ChargingStationModel,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            ChargingStationModel = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "charging station model identification",
                                         ChargingStationModel_Id.TryParse,
                                         out ChargingStationModel_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station model '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (JSON[PropertyKeys.ManufacturerId] is not null)
                {
                    ErrorResponse = $"The charging station model '{id}' is embedded in its manufacturer: it names none!";
                    return false;
                }

                if (!POIJSON.TryParseI18N                     (JSON, PropertyKeys.Name,            "charging station model name",         out var name,                      out ErrorResponse) ||
                    !POIJSON.TryParseI18N                     (JSON, PropertyKeys.Description,     "charging station model description",  out var description,               out ErrorResponse) ||
                    !POIJSON.TryParseIds<HardwareRevision>    (JSON, PropertyKeys.HardwareRevisions, "hardware revision", HardwareRevision.TryParse, out var hardwareRevisions, out ErrorResponse) ||
                    !POIJSON.TryParseIds<FirmwareVersion>     (JSON, PropertyKeys.FirmwareVersions,  "firmware version",  FirmwareVersion. TryParse, out var firmwareVersions,  out ErrorResponse) ||
                    !POIJSON.TryParseText                     (JSON, PropertyKeys.ProductURL,      "product URL",    URL.TryParse,        out URL? productURL,               out ErrorResponse) ||
                    !POIJSON.TryParseText                     (JSON, PropertyKeys.DatasheetURL,    "datasheet URL",  URL.TryParse,        out URL? datasheetURL,             out ErrorResponse) ||
                    !POIJSON.TryParseObjects<Image>           (JSON, PropertyKeys.Images,          "images",         Image.TryParse,      out var images,                    out ErrorResponse) ||
                    !POIJSON.TryParseFlags                    (JSON, PropertyKeys.CurrentTypes,    "current type",                        out CurrentTypes currentTypes,     out ErrorResponse) ||
                    !POIJSON.TryParseIds<ChargingConnectorType>(JSON, PropertyKeys.ConnectorTypes, "connector type", ChargingConnectorType.TryParse, out var connectorTypes, out ErrorResponse) ||
                    !POIJSON.TryParseInteger                  (JSON, PropertyKeys.NumberOfEVSEs,   "number of EVSEs",                     out var numberOfEVSEs,             out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.MaxPower, "maximum power", out Watt? maxPower, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingStationModel = new ChargingStationModel(id,
                                                                name,
                                                                description,
                                                                hardwareRevisions,
                                                                firmwareVersions,
                                                                productURL,
                                                                datasheetURL,
                                                                images,
                                                                maxPower,
                                                                currentTypes,
                                                                connectorTypes,
                                                                numberOfEVSEs,
                                                                customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationModel  = null;
                ErrorResponse         = "The given JSON representation of a charging station model is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

    }

}
