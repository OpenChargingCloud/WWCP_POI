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
    /// A model of energy meters: an immutable description, embedded completely in its energy meter manufacturer.
    /// Energy meters and energy meter templates name it by their manufacturer and its identification, which is unique
    /// within its manufacturer.
    /// </summary>
    /// <remarks>
    /// Its manufacturer states it and signs its changes. Its versions are the hardware or firmware versions it was built
    /// in. Its technical data are the ratings of the model in their base units: maxCurrent in A. Its signature methods are
    /// how its meters sign their measurements, as a transparency software verifies them; its accuracy class is the one of
    /// its type examination, e.g. "A", "B" or "C" of the Measuring Instruments Directive (2014/32/EU, Annex V).
    /// </remarks>
    public sealed class EnergyMeterModel
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/energyMeterModel/v1";

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
            public const String  Id                 = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context            = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name               = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description        = "description";

            /// <summary>
            /// The property of its versions.
            /// </summary>
            public const String  Versions           = "versions";

            /// <summary>
            /// The property of the URL of its product page.
            /// </summary>
            public const String  ProductURL         = "productURL";

            /// <summary>
            /// The property of the URL of its datasheet.
            /// </summary>
            public const String  DatasheetURL       = "datasheetURL";

            /// <summary>
            /// The property of its images.
            /// </summary>
            public const String  Images             = "images";

            /// <summary>
            /// The property of its current types.
            /// </summary>
            public const String  CurrentTypes       = "currentTypes";

            /// <summary>
            /// The property of its maximum current.
            /// </summary>
            public const String  MaxCurrent         = "maxCurrent";

            /// <summary>
            /// The property of its accuracy class.
            /// </summary>
            public const String  AccuracyClass      = "accuracyClass";

            /// <summary>
            /// The property of what it measures.
            /// </summary>
            public const String  Measurands         = "measurands";

            /// <summary>
            /// The property of its signature methods.
            /// </summary>
            public const String  SignatureMethods   = "signatureMethods";

            /// <summary>
            /// The property of its custom data.
            /// </summary>
            public const String  CustomData         = "customData";

            /// <summary>
            /// The property of the identification of its manufacturer, refused: it is embedded in its manufacturer.
            /// </summary>
            public const String  ManufacturerId     = "manufacturerId";

        }

        #endregion


        /// <summary>
        /// The identification of the model, assigned by its manufacturer, e.g. "KDM-500".
        /// </summary>
        public EnergyMeterModel_Id              Id                 { get; }

        /// <summary>
        /// The multi-language name.
        /// </summary>
        public ImmutableI18NString?             Name               { get; }

        /// <summary>
        /// The multi-language description.
        /// </summary>
        public ImmutableI18NString?             Description        { get; }

        /// <summary>
        /// Its versions, e.g. hardware or firmware versions, each once, in their given order.
        /// </summary>
        public ImmutableArray<String>           Versions           { get; }

        /// <summary>
        /// The URL of its product page.
        /// </summary>
        public URL?                             ProductURL         { get; }

        /// <summary>
        /// The URL of its datasheet.
        /// </summary>
        public URL?                             DatasheetURL       { get; }

        /// <summary>
        /// Images of it, e.g. photos, each URL once, in their given order.
        /// </summary>
        public ImmutableArray<Image>            Images             { get; }

        /// <summary>
        /// Its current types.
        /// </summary>
        public CurrentTypes?                    CurrentTypes       { get; }

        /// <summary>
        /// The maximum current it measures.
        /// </summary>
        public Ampere?                          MaxCurrent         { get; }

        /// <summary>
        /// Its accuracy class, e.g. "B".
        /// </summary>
        public String?                          AccuracyClass      { get; }

        /// <summary>
        /// What it measures, e.g. OBIS codes as "1-0:1.8.0", each once, ordered ordinally.
        /// </summary>
        public ImmutableArray<String>           Measurands         { get; }

        /// <summary>
        /// How its meters sign their measurements, ordered by their identifications.
        /// </summary>
        public ImmutableArray<SignatureMethod>  SignatureMethods   { get; }

        /// <summary>
        /// Custom data.
        /// </summary>
        public CustomDataNew?                   CustomData         { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter model.
        /// </summary>
        /// <param name="Id">The identification of the model, unique within its manufacturer.</param>
        /// <param name="Name">The multi-language name.</param>
        /// <param name="Description">The multi-language description.</param>
        /// <param name="Versions">Its versions, e.g. hardware or firmware versions.</param>
        /// <param name="ProductURL">The URL of its product page.</param>
        /// <param name="DatasheetURL">The URL of its datasheet.</param>
        /// <param name="Images">Images of it, e.g. photos.</param>
        /// <param name="CurrentTypes">Its current types.</param>
        /// <param name="MaxCurrent">The maximum current it measures.</param>
        /// <param name="AccuracyClass">Its accuracy class, e.g. "B".</param>
        /// <param name="Measurands">What it measures.</param>
        /// <param name="SignatureMethods">How its meters sign their measurements.</param>
        /// <param name="CustomData">Custom data.</param>
        public EnergyMeterModel(EnergyMeterModel_Id            Id,
                                ImmutableI18NString?           Name               = null,
                                ImmutableI18NString?           Description        = null,
                                IEnumerable<String>?           Versions           = null,
                                URL?                           ProductURL         = null,
                                URL?                           DatasheetURL       = null,
                                IEnumerable<Image>?            Images             = null,
                                CurrentTypes?                  CurrentTypes       = null,
                                Ampere?                        MaxCurrent         = null,
                                String?                        AccuracyClass      = null,
                                IEnumerable<String>?           Measurands         = null,
                                IEnumerable<SignatureMethod>?  SignatureMethods   = null,
                                CustomDataNew?                 CustomData         = null)
        {

            if (MaxCurrent?.Value <= 0)
                throw new ArgumentException($"The maximum current of the energy meter model '{Id}' must be positive!", nameof(MaxCurrent));

            if (AccuracyClass is not null && (AccuracyClass.Length == 0 || AccuracyClass != AccuracyClass.Trim()))
                throw new ArgumentException($"The accuracy class of the energy meter model '{Id}' must not be empty nor begin or end with whitespace!", nameof(AccuracyClass));

            var measurands = (Measurands ?? []).ToArray();

            if (measurands.Any(measurand => String.IsNullOrWhiteSpace(measurand) || measurand != measurand.Trim()))
                throw new ArgumentException($"A measurand of the energy meter model '{Id}' must not be empty nor begin or end with whitespace!", nameof(Measurands));

            if (measurands.Distinct(StringComparer.Ordinal).Count() != measurands.Length)
                throw new ArgumentException($"A measurand of the energy meter model '{Id}' must not be given twice!", nameof(Measurands));

            this.Id                = Id;
            this.Name              = Name;
            this.Description       = Description;
            this.Versions          = ModelVersions.Of(Versions, $"the energy meter model '{Id}'");
            this.ProductURL        = ProductURL;
            this.DatasheetURL      = DatasheetURL;
            this.Images            = POIJSON.Distinct  (Images, image => image.URL.ToString(), $"image of the energy meter model '{Id}'");
            this.CurrentTypes      = CurrentTypes == POI.CurrentTypes.Unspecified ? null : CurrentTypes;
            this.MaxCurrent        = MaxCurrent;
            this.AccuracyClass     = AccuracyClass;
            this.Measurands        = [ .. measurands.Order(StringComparer.Ordinal) ];
            this.SignatureMethods  = POIJSON.OrderedIds(SignatureMethods, $"signature method of the energy meter model '{Id}'");
            this.CustomData        = CustomData;

        }

        #endregion


        #region HasVersion(Version)

        /// <summary>
        /// Whether the given version is one of its versions.
        /// </summary>
        public Boolean HasVersion(String Version)

            => Versions.Contains(Version, StringComparer.Ordinal);

        #endregion


        #region ToJSON(Embedded = false, CustomEnergyMeterModelSerializer = null)

        /// <summary>
        /// Return the JSON representation of this energy meter model, as its manufacturer embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEnergyMeterModelSerializer">A delegate to serialize custom energy meter model JSON objects.</param>
        public JObject ToJSON(Boolean                                             Embedded                           = false,
                              CustomJObjectSerializerDelegate<EnergyMeterModel>?  CustomEnergyMeterModelSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,           JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,              Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,       Description.ToJSON());

            if (Versions.Length > 0)
                json.Add(PropertyKeys.Versions,          new JArray(Versions));

            if (ProductURL.HasValue)
                json.Add(PropertyKeys.ProductURL,        ProductURL.Value.ToString());

            if (DatasheetURL.HasValue)
                json.Add(PropertyKeys.DatasheetURL,      DatasheetURL.Value.ToString());

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,            new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (CurrentTypes.HasValue)
                json.Add(PropertyKeys.CurrentTypes,      POIJSON.FlagsToJSON(CurrentTypes.Value));

            if (MaxCurrent.HasValue)
                json.Add(PropertyKeys.MaxCurrent,        POIJSON.Number(MaxCurrent.Value.Value));

            if (AccuracyClass is not null)
                json.Add(PropertyKeys.AccuracyClass,     AccuracyClass);

            if (Measurands.Length > 0)
                json.Add(PropertyKeys.Measurands,        new JArray(Measurands));

            if (SignatureMethods.Length > 0)
                json.Add(PropertyKeys.SignatureMethods,  POIJSON.IdsToJSON(SignatureMethods));

            if (CustomData is not null && CustomData.HasValues)
                json.Add(PropertyKeys.CustomData,        CustomData.ToJObject());

            return CustomEnergyMeterModelSerializer is not null
                       ? CustomEnergyMeterModelSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out EnergyMeterModel, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an energy meter model.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMeterModel">The parsed energy meter model.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                     JSON,
                                       [NotNullWhen(true)]  out EnergyMeterModel?  EnergyMeterModel,
                                       [NotNullWhen(false)] out String?            ErrorResponse)
        {

            EnergyMeterModel = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "energy meter model identification",
                                         EnergyMeterModel_Id.TryParse,
                                         out EnergyMeterModel_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the energy meter model '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (JSON[PropertyKeys.ManufacturerId] is not null)
                {
                    ErrorResponse = $"The energy meter model '{id}' is embedded in its manufacturer: it names none!";
                    return false;
                }

                if (!POIJSON.TryParseI18N                (JSON, PropertyKeys.Name,              "energy meter model name",         out var name,                   out ErrorResponse) ||
                    !POIJSON.TryParseI18N                (JSON, PropertyKeys.Description,       "energy meter model description",  out var description,            out ErrorResponse) ||
                    !POIJSON.TryParseStrings             (JSON, PropertyKeys.Versions,          "versions",                        out var versions,               out ErrorResponse) ||
                    !POIJSON.TryParseText                (JSON, PropertyKeys.ProductURL,        "product URL",    URL.TryParse,    out URL? productURL,            out ErrorResponse) ||
                    !POIJSON.TryParseText                (JSON, PropertyKeys.DatasheetURL,      "datasheet URL",  URL.TryParse,    out URL? datasheetURL,          out ErrorResponse) ||
                    !POIJSON.TryParseObjects<Image>      (JSON, PropertyKeys.Images,            "images",         Image.TryParse,  out var images,                 out ErrorResponse) ||
                    !POIJSON.TryParseFlags               (JSON, PropertyKeys.CurrentTypes,      "current type",                    out CurrentTypes currentTypes,  out ErrorResponse) ||
                    !POIJSON.TryParseString              (JSON, PropertyKeys.AccuracyClass,     "accuracy class",                  out var accuracyClass,          out ErrorResponse) ||
                    !POIJSON.TryParseStrings             (JSON, PropertyKeys.Measurands,        "measurands",                      out var measurands,             out ErrorResponse) ||
                    !POIJSON.TryParseIds<SignatureMethod>(JSON, PropertyKeys.SignatureMethods,  "signature method", SignatureMethod.TryParse, out var signatureMethods, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.MaxCurrent, "maximum current", out Ampere? maxCurrent, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                EnergyMeterModel = new EnergyMeterModel(id,
                                                        name,
                                                        description,
                                                        versions,
                                                        productURL,
                                                        datasheetURL,
                                                        images,
                                                        currentTypes,
                                                        maxCurrent,
                                                        accuracyClass,
                                                        measurands,
                                                        signatureMethods,
                                                        customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EnergyMeterModel  = null;
                ErrorResponse     = "The given JSON representation of an energy meter model is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

    }

}
