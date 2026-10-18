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
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A template of energy meters, embedded completely in its owner; its identification is unique within the roaming
    /// network. A charging station operator has templates for the energy meters of its charging pools, charging stations
    /// and EVSEs, a grid operator for those of its grid connection points. It gives the energy meters naming it in their
    /// templateIds what a series of meters shares, which they take at runtime: a value of their own wins, else the one of
    /// the first of their templates giving one; arrays are united. What belongs to a single device, its serial number and
    /// its public keys, is no template's to give.
    /// </summary>
    public sealed class EnergyMeterTemplate : APOITemplate<EnergyMeterTemplate_Id>,
                                              IEquatable<EnergyMeterTemplate>,
                                              IComparable<EnergyMeterTemplate>
    {

        #region Properties

        /// <summary>
        /// What its members measure, unless they tell it themselves.
        /// </summary>
        public EnergyMeterRole?                                Role                        { get; }

        /// <summary>
        /// The manufacturer of its members, one of its roaming network, unless they name their own.
        /// </summary>
        public EnergyMeterManufacturer_Id?                     ManufacturerId              { get; }

        /// <summary>
        /// The model of its members, as its manufacturer names it, unless they name their own.
        /// </summary>
        public EnergyMeterModel_Id?                            ModelId                     { get; }

        /// <summary>
        /// The homepage of the model of its members, unless they have their own.
        /// </summary>
        public URL?                                            ModelURL                    { get; }

        /// <summary>
        /// The hardware version of its members, unless they have their own.
        /// </summary>
        public String?                                         HardwareVersion             { get; }

        /// <summary>
        /// The firmware version of its members, unless they have their own.
        /// </summary>
        public String?                                         FirmwareVersion             { get; }

        /// <summary>
        /// Transparency software releases for the measurements of its members, with their legal status, besides their
        /// own; at most one per release, in the order of their identifications.
        /// </summary>
        public ImmutableArray<TransparencySoftwareAssignment>  TransparencySoftwareStatus  { get; }

        /// <summary>
        /// How its members sign their measurements, besides their own; in the order of their names, each once.
        /// </summary>
        public ImmutableArray<SignatureMethod>                 SignatureMethods            { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter template.
        /// </summary>
        public EnergyMeterTemplate(EnergyMeterTemplate_Id                              Id,
                                I18NString?                                      Name                         = null,
                                I18NString?                                      Description                  = null,

                                EnergyMeterRole?                                 Role                         = null,
                                EnergyMeterManufacturer_Id?                      ManufacturerId               = null,
                                EnergyMeterModel_Id?                             ModelId                      = null,
                                URL?                                             ModelURL                     = null,
                                String?                                          HardwareVersion              = null,
                                String?                                          FirmwareVersion              = null,
                                IEnumerable<TransparencySoftwareAssignment>?     TransparencySoftwareStatus   = null,
                                IEnumerable<SignatureMethod>?                    SignatureMethods             = null,

                                String?                                          DataSource                   = null,
                                DateTimeOffset?                                  Created                      = null,
                                DateTimeOffset?                                  LastChange                   = null,

                                CustomDataNew?                                   CustomData                   = null,
                                UserDefinedDictionary?                           InternalData                 = null)

            : base(Id,
                   Name,
                   Description,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            this.Role                        = Role;
            this.ManufacturerId              = ManufacturerId;
            this.ModelId                     = ModelId;
            this.ModelURL                    = ModelURL;
            this.HardwareVersion             = HardwareVersion;
            this.FirmwareVersion             = FirmwareVersion;
            this.TransparencySoftwareStatus  = TransparencySoftwareAssignment.Checked(TransparencySoftwareStatus, $"the energy meter template '{Id}'");
            this.SignatureMethods            = POIJSON.OrderedIds(SignatureMethods,  $"signature method of the energy meter template '{Id}'");

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this energy meter template, as its owner embeds it.
        /// </summary>
        public override JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",            Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",     Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",      DataSource);

            // The properties of an energy meter, as an energy meter writes them.
            if (Role.HasValue)
                json.Add("role",             Role.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add("manufacturerId",   ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add("modelId",          ModelId.Value.ToString());

            if (ModelURL.HasValue)
                json.Add("modelURL",         ModelURL.Value.ToString());

            if (HardwareVersion is not null)
                json.Add("hardwareVersion",  HardwareVersion);

            if (FirmwareVersion is not null)
                json.Add("firmwareVersion",  FirmwareVersion);

            if (TransparencySoftwareStatus.Length > 0)
                json.Add("transparencySoftwareStatus", new JArray(TransparencySoftwareStatus.Select(assignment => assignment.ToJSON())));

            if (SignatureMethods.Length > 0)
                json.Add("signatureMethods", POIJSON.IdsToJSON(SignatureMethods));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out EnergyMeterTemplate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an energy meter template.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMeterTemplate">The parsed energy meter template.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out EnergyMeterTemplate?   EnergyMeterTemplate,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            EnergyMeterTemplate = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "energy meter template identification",
                                         EnergyMeterTemplate_Id.TryParse,
                                         out EnergyMeterTemplate_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, "name",            "energy meter template name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, "description",     "energy meter template description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, "dataSource",      "data source",                     out var dataSource,   out ErrorResponse))
                {
                    return false;
                }

                // The properties of an energy meter, as an energy meter reads them.
                if (!POIJSON.TryParseText  (JSON, "role",             "energy meter role",           EnergyMeterRole.           TryParse, out EnergyMeterRole?            role,           out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "manufacturerId",   "manufacturer identification", EnergyMeterManufacturer_Id.TryParse, out EnergyMeterManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "modelId",          "model identification",        EnergyMeterModel_Id.       TryParse, out EnergyMeterModel_Id?        modelId,        out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "modelURL",         "model URL",                   URL.                       TryParse, out URL?                        modelURL,       out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "hardwareVersion",  "hardware version",            out var hardwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "firmwareVersion",  "firmware version",            out var firmwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseObjects<TransparencySoftwareAssignment>(JSON, "transparencySoftwareStatus", "transparency software status", TransparencySoftwareAssignment.TryParse, out var transparencySoftwareStatus, out ErrorResponse) ||
                    !POIJSON.TryParseIds<SignatureMethod>                   (JSON, "signatureMethods",           "signature method",             SignatureMethod.               TryParse, out var signatureMethods,           out ErrorResponse))
                {
                    return false;
                }

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                EnergyMeterTemplate = new EnergyMeterTemplate(id,
                                                        name,
                                                        description,
                                                        role,
                                                        manufacturerId,
                                                        modelId,
                                                        modelURL,
                                                        hardwareVersion,
                                                        firmwareVersion,
                                                        transparencySoftwareStatus,
                                                        signatureMethods,
                                                        DataSource:  dataSource,
                                                        Created:     created,
                                                        LastChange:  lastChange,
                                                        CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EnergyMeterTemplate  = null;
                ErrorResponse     = "The given JSON representation of an energy meter template is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (internal static) Effective(EnergyMeter, Templates)

        /// <summary>
        /// The given energy meter as it is at runtime, with the properties of the given templates, in the order of its
        /// templateIds: a single value is its own, else the one of the first of its templates giving one; an array is its
        /// own united with those of all its templates, of transparency software releases its own status, else the one of
        /// the first template giving one. Its serial number and public keys are its own. It has the runtime status of the
        /// energy meter; its last change is the latest of it and its templates.
        /// </summary>
        /// <param name="EnergyMeter">An energy meter.</param>
        /// <param name="Templates">The templates of the energy meter, in the order of its templateIds.</param>
        internal static EnergyMeter Effective(EnergyMeter                             EnergyMeter,
                                              POITemplateValues<EnergyMeterTemplate>  Templates)
        {

            try
            {

                var effective = new EnergyMeter(

                                    EnergyMeter.Id,
                                    EnergyMeter.Name,
                                    EnergyMeter.Description,

                                    Role:                        Templates.Single(EnergyMeter.Role,             template => template.Role),
                                    ManufacturerId:              Templates.Single(EnergyMeter.ManufacturerId,   template => template.ManufacturerId),
                                    ModelId:                     Templates.Single(EnergyMeter.ModelId,          template => template.ModelId),
                                    ModelURL:                    Templates.Single(EnergyMeter.ModelURL,         template => template.ModelURL),
                                    HardwareVersion:             Templates.Single(EnergyMeter.HardwareVersion,  template => template.HardwareVersion),
                                    FirmwareVersion:             Templates.Single(EnergyMeter.FirmwareVersion,  template => template.FirmwareVersion),

                                    TransparencySoftwareStatus:  Templates.Union (EnergyMeter.TransparencySoftwareStatus, template => template.TransparencySoftwareStatus,
                                                                                  assignment => assignment.TransparencySoftwareId),
                                    SignatureMethods:            Templates.Union (EnergyMeter.SignatureMethods, template => template.SignatureMethods),

                                    // What belongs to the single device, and the templates it names.
                                    SerialNumber:                EnergyMeter.SerialNumber,
                                    PublicKeys:                  EnergyMeter.PublicKeys,
                                    TemplateIds:                 EnergyMeter.TemplateIds,

                                    DataSource:                  EnergyMeter.DataSource,
                                    Created:                     EnergyMeter.Created,
                                    LastChange:                  Templates.LastChange(EnergyMeter.LastChangeDate),
                                    CustomData:                  EnergyMeter.CustomData

                                );

                effective.ShareRuntimeOf(EnergyMeter);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The energy meter '{EnergyMeter.Id}' {Templates.With} is invalid: {e.Message}", e);
            }

        }

        #endregion

        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two energy meter templates for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EnergyMeterTemplate? EnergyMeterTemplate1, EnergyMeterTemplate? EnergyMeterTemplate2)

            => ReferenceEquals(EnergyMeterTemplate1, EnergyMeterTemplate2) ||
               (EnergyMeterTemplate1 is not null && EnergyMeterTemplate1.Equals(EnergyMeterTemplate2));

        /// <summary>
        /// Compares two energy meter templates for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EnergyMeterTemplate? EnergyMeterTemplate1, EnergyMeterTemplate? EnergyMeterTemplate2)

            => !(EnergyMeterTemplate1 == EnergyMeterTemplate2);

        /// <inheritdoc/>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterTemplate energyMeterTemplate
                   ? CompareTo(energyMeterTemplate)
                   : throw new ArgumentException("The given object is not an energy meter template!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EnergyMeterTemplate? EnergyMeterTemplate)

            => EnergyMeterTemplate is null
                   ? throw new ArgumentNullException(nameof(EnergyMeterTemplate), "The given energy meter template must not be null!")
                   : Id.CompareTo(EnergyMeterTemplate.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterTemplate energyMeterTemplate &&
               Equals(energyMeterTemplate);

        /// <inheritdoc/>
        public Boolean Equals(EnergyMeterTemplate? EnergyMeterTemplate)

            => EnergyMeterTemplate is not null &&
               Id.Equals(EnergyMeterTemplate.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
