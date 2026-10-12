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

        #region Properties

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


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this energy meter manufacturer, as its roaming network embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",  Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",   DataSource);

            if (Address is not null)
                json.Add("address",      Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add("homepage",     Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",      HotlinePhoneNumber.Value.ToString());

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR representation of this energy meter manufacturer: the keys and values of its JSON representation.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

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

                if (!JSON.ParseMandatory("@id",
                                         "energy meter manufacturer identification",
                                         EnergyMeterManufacturer_Id.TryParse,
                                         out EnergyMeterManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, "name",         "manufacturer name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, "description",  "manufacturer description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, "dataSource",   "data source",               out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress(JSON,                                              out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "homepage",     "homepage",                  URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "hotline",      "hotline phone number",      PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
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
