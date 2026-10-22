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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A manufacturer of energy meters, embedded completely in its roaming network.
    /// Energy meters name it by its identification. It has no runtime status.
    /// </summary>
    public sealed class EnergyMeterManufacturer : IEquatable<EnergyMeterManufacturer>,
                                                  IComparable<EnergyMeterManufacturer>,
                                                  IComparable
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/energyMeterManufacturer/v1";

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
            public const String  Id                   = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context              = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name                 = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description          = "description";

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource           = "dataSource";

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address              = "address";

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage             = "homepage";

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber   = "hotline";

            /// <summary>
            /// The property of its creation timestamp, as of every entity.
            /// </summary>
            public const String  Created              = "created";

            /// <summary>
            /// The property of its last change timestamp, as of every entity.
            /// </summary>
            public const String  LastChange           = "lastChange";

            /// <summary>
            /// The property of its custom data, as of every entity.
            /// </summary>
            public const String  CustomData           = "customData";

        }

        #endregion


        /// <summary>
        /// The identification of this manufacturer.
        /// </summary>
        public EnergyMeterManufacturer_Id  Id                    { get; }

        /// <summary>
        /// The multi-language name.
        /// </summary>
        public ImmutableI18NString         Name                  { get; }

        /// <summary>
        /// The multi-language description.
        /// </summary>
        public ImmutableI18NString         Description           { get; }

        /// <summary>
        /// The source of this information.
        /// </summary>
        public String?                     DataSource            { get; }

        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?                    Address               { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?                        Homepage              { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?                HotlinePhoneNumber    { get; }

        /// <summary>
        /// When it was added to its roaming network.
        /// </summary>
        public DateTimeOffset              Created               { get; }

        /// <summary>
        /// When it was changed last.
        /// </summary>
        public DateTimeOffset              LastChangeDate        { get; }

        /// <summary>
        /// Optional custom data.
        /// </summary>
        public CustomDataNew               CustomData            { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter manufacturer.
        /// </summary>
        public EnergyMeterManufacturer(EnergyMeterManufacturer_Id  Id,
                                       ImmutableI18NString?        Name                 = null,
                                       ImmutableI18NString?        Description          = null,
                                       Address?                    Address              = null,
                                       URL?                        Homepage             = null,
                                       PhoneNumber?                HotlinePhoneNumber   = null,
                                       String?                     DataSource           = null,
                                       DateTimeOffset?             Created              = null,
                                       DateTimeOffset?             LastChange           = null,
                                       CustomDataNew?              CustomData           = null)
        {

            this.Id                  = Id;
            this.Name                = Name        ?? ImmutableI18NString.Empty;
            this.Description         = Description ?? ImmutableI18NString.Empty;
            this.DataSource          = DataSource;
            this.Address             = Address;
            this.Homepage            = Homepage;
            this.HotlinePhoneNumber  = HotlinePhoneNumber;
            this.Created             = POIEntityMetadata.Timestamp(Created    ?? Timestamp.Now);
            this.LastChangeDate      = POIEntityMetadata.Timestamp(LastChange ?? this.Created);
            this.CustomData          = (CustomData ?? CustomDataNew.Empty).Clone();

        }

        #endregion


        #region ToJSON(Embedded = false, CustomEnergyMeterManufacturerSerializer = null)

        /// <summary>
        /// Return the JSON representation of this energy meter manufacturer, as its roaming network embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEnergyMeterManufacturerSerializer">A delegate to serialize custom energy meter manufacturer JSON objects.</param>
        public JObject ToJSON(Boolean                                                    Embedded                                  = false,
                              CustomJObjectSerializerDelegate<EnergyMeterManufacturer>?  CustomEnergyMeterManufacturerSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,             JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Name,                Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description,         Description.ToJSON());

            if (DataSource is not null)
                json.Add(PropertyKeys.DataSource,          DataSource);

            if (Address is not null)
                json.Add(PropertyKeys.Address,             Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,            Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,  HotlinePhoneNumber.Value.ToString());

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomEnergyMeterManufacturerSerializer is not null
                       ? CustomEnergyMeterManufacturerSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomEnergyMeterManufacturerSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this energy meter manufacturer: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEnergyMeterManufacturerSerializer">A delegate to serialize custom energy meter manufacturer CBOR values.</param>
        public CBORValue ToCBOR(Boolean                                                  Embedded                                  = false,
                                CustomCBORSerializerDelegate<EnergyMeterManufacturer>?   CustomEnergyMeterManufacturerSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(PropertyKeys.Id),                  CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),             CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Name),                Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(PropertyKeys.Description),         Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.DataSource),          CBORValue.FromText(DataSource));

            // The address has no CBOR representation of its own: it is the keys and values of its JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),             CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),            CBORValue.FromText(Homepage.Value.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),  CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(PropertyKeys.Created),                 CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(PropertyKeys.LastChange),              CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(PropertyKeys.CustomData),          CBORJSON.ToCBOR(CustomData.ToJObject()));

            return CustomEnergyMeterManufacturerSerializer is not null
                       ? CustomEnergyMeterManufacturerSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (static) TryParse(JSON, out EnergyMeterManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an energy meter manufacturer.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMeterManufacturer">The parsed energy meter manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out EnergyMeterManufacturer?  EnergyMeterManufacturer,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            EnergyMeterManufacturer = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "energy meter manufacturer identification",
                                         EnergyMeterManufacturer_Id.TryParse,
                                         out EnergyMeterManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the energy meter manufacturer '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, PropertyKeys.Name,                "manufacturer name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, PropertyKeys.Description,         "manufacturer description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, PropertyKeys.DataSource,          "data source",               out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress(JSON, PropertyKeys.Address,             "address",                   out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, PropertyKeys.Homepage,            "homepage",                  URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",      PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
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

                EnergyMeterManufacturer = new EnergyMeterManufacturer(id,
                                                                      name,
                                                                      description,
                                                                      address,
                                                                      homepage,
                                                                      hotline,
                                                                      dataSource,
                                                                      created,
                                                                      lastChange,
                                                                      customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EnergyMeterManufacturer  = null;
                ErrorResponse            = "The given JSON representation of an energy meter manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out EnergyMeterManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of an energy meter manufacturer: the keys and values of its JSON
        /// representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="EnergyMeterManufacturer">The parsed energy meter manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                          CBOR,
                                       [NotNullWhen(true)]  out EnergyMeterManufacturer?  EnergyMeterManufacturer,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            EnergyMeterManufacturer = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of an energy meter manufacturer must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(PropertyKeys.Id,
                                         "energy meter manufacturer identification",
                                         EnergyMeterManufacturer_Id.TryParse,
                                         out EnergyMeterManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(PropertyKeys.Context), out var context) &&
                    !(context.TryGetText(out var contextText) && contextText == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the energy meter manufacturer '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptionalText(PropertyKeys.DataSource, "data source", out var dataSource, out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR parser of its own is read as its JSON, with the parsers of the JSON representation:
                // the multilingual texts, the address, the homepage, the hotline and the metadata.
                var json = new JObject(
                               new JProperty(PropertyKeys.Id, id.ToString())
                           );

                foreach (var key in new[] { PropertyKeys.Name,
                                            PropertyKeys.Description,
                                            PropertyKeys.Address,
                                            PropertyKeys.Homepage,
                                            PropertyKeys.HotlinePhoneNumber,
                                            PropertyKeys.Created,
                                            PropertyKeys.LastChange,
                                            PropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!POIJSON.TryParseI18N   (json, PropertyKeys.Name,                "manufacturer name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (json, PropertyKeys.Description,         "manufacturer description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseAddress(json, PropertyKeys.Address,             "address",                   out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (json, PropertyKeys.Homepage,            "homepage",                  URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (json, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",      PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIEntityMetadata.TryParse(json,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                EnergyMeterManufacturer = new EnergyMeterManufacturer(id,
                                                                      name,
                                                                      description,
                                                                      address,
                                                                      homepage,
                                                                      hotline,
                                                                      dataSource,
                                                                      created,
                                                                      lastChange,
                                                                      customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EnergyMeterManufacturer  = null;
                ErrorResponse            = "The given CBOR representation of an energy meter manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two energy meter manufacturers for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EnergyMeterManufacturer? Manufacturer1, EnergyMeterManufacturer? Manufacturer2)

            => ReferenceEquals(Manufacturer1, Manufacturer2) ||
               (Manufacturer1 is not null && Manufacturer1.Equals(Manufacturer2));

        /// <summary>
        /// Compares two energy meter manufacturers for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EnergyMeterManufacturer? Manufacturer1, EnergyMeterManufacturer? Manufacturer2)

            => !(Manufacturer1 == Manufacturer2);

        /// <inheritdoc/>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterManufacturer manufacturer
                   ? CompareTo(manufacturer)
                   : throw new ArgumentException("The given object is not an energy meter manufacturer!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EnergyMeterManufacturer? EnergyMeterManufacturer)

            => EnergyMeterManufacturer is null
                   ? throw new ArgumentNullException(nameof(EnergyMeterManufacturer), "The given energy meter manufacturer must not be null!")
                   : Id.CompareTo(EnergyMeterManufacturer.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterManufacturer manufacturer &&
               Equals(manufacturer);

        /// <inheritdoc/>
        public Boolean Equals(EnergyMeterManufacturer? EnergyMeterManufacturer)

            => EnergyMeterManufacturer is not null &&
               Id.Equals(EnergyMeterManufacturer.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
