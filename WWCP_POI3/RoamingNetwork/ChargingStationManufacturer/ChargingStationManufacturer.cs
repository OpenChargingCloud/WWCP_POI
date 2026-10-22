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
    /// The admin status of a charging station manufacturer changed.
    /// </summary>
    public delegate Task OnChargingStationManufacturerAdminStatusChangedDelegate(DateTimeOffset                                            Timestamp,
                                                                                 EventTracking_Id                                          EventTrackingId,
                                                                                 ChargingStationManufacturer                               ChargingStationManufacturer,
                                                                                 Timestamped<ChargingStationManufacturerAdminStatusType>   OldStatus,
                                                                                 Timestamped<ChargingStationManufacturerAdminStatusType>   NewStatus,
                                                                                 Context?                                                  DataSource);

    /// <summary>
    /// The status of a charging station manufacturer changed.
    /// </summary>
    public delegate Task OnChargingStationManufacturerStatusChangedDelegate(DateTimeOffset                                       Timestamp,
                                                                            EventTracking_Id                                     EventTrackingId,
                                                                            ChargingStationManufacturer                          ChargingStationManufacturer,
                                                                            Timestamped<ChargingStationManufacturerStatusType>   OldStatus,
                                                                            Timestamped<ChargingStationManufacturerStatusType>   NewStatus,
                                                                            Context?                                             DataSource);


    /// <summary>
    /// A manufacturer of charging stations, embedded completely in its roaming network.
    /// Charging stations name it by its identification.
    /// </summary>
    public sealed class ChargingStationManufacturer : AImmutableEMobilityEntity<ChargingStationManufacturer_Id,
                                                                                ChargingStationManufacturerAdminStatusType,
                                                                                ChargingStationManufacturerStatusType>,
                                                      IEquatable<ChargingStationManufacturer>,
                                                      IComparable<ChargingStationManufacturer>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/chargingStationManufacturer/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

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

        }

        #endregion


        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?      Address               { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?          Homepage              { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?  HotlinePhoneNumber    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station manufacturer.
        /// </summary>
        public ChargingStationManufacturer(ChargingStationManufacturer_Id                              Id,
                                           I18NString?                                                 Name                         = null,
                                           I18NString?                                                 Description                  = null,
                                           Address?                                                    Address                      = null,
                                           URL?                                                        Homepage                     = null,
                                           PhoneNumber?                                                HotlinePhoneNumber           = null,

                                           Timestamped<ChargingStationManufacturerAdminStatusType>?    InitialAdminStatus           = null,
                                           Timestamped<ChargingStationManufacturerStatusType>?         InitialStatus                = null,
                                           UInt16?                                                     MaxAdminStatusScheduleSize   = null,
                                           UInt16?                                                     MaxStatusScheduleSize        = null,

                                           String?                                                     DataSource                   = null,
                                           DateTimeOffset?                                             Created                      = null,
                                           DateTimeOffset?                                             LastChange                   = null,

                                           CustomDataNew?                                              CustomData                   = null,
                                           UserDefinedDictionary?                                      InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingStationManufacturerAdminStatusType>(Created ?? Timestamp.Now, ChargingStationManufacturerAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingStationManufacturerStatusType>     (Created ?? Timestamp.Now, ChargingStationManufacturerStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Address             = Address;
            this.Homepage            = Homepage;
            this.HotlinePhoneNumber  = HotlinePhoneNumber;

        }

        #endregion


        #region Status events

        private readonly Lock                                                      statusEventsLock  = new ();
        private          OnChargingStationManufacturerAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingStationManufacturerStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationManufacturerAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingStationManufacturerStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                           Timestamp,
                                                                   EventTracking_Id                                         EventTrackingId,
                                                                   Timestamped<ChargingStationManufacturerAdminStatusType>  NewStatus,
                                                                   Timestamped<ChargingStationManufacturerAdminStatusType>  OldStatus,
                                                                   Context?                                                 DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                                      Timestamp,
                                                              EventTracking_Id                                    EventTrackingId,
                                                              Timestamped<ChargingStationManufacturerStatusType>  NewStatus,
                                                              Timestamped<ChargingStationManufacturerStatusType>  OldStatus,
                                                              Context?                                            DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region (protected override) ContentJSON()

        /// <inheritdoc/>
        protected override JObject ContentJSON()

            => ToJSON(Embedded: true);

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this charging station manufacturer; embedded, as its roaming network embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomChargingStationManufacturerSerializer">A delegate to serialize custom charging station manufacturer JSON objects.</param>
        public JObject ToJSON(Boolean                                                        Embedded                                      = false,
                              Boolean                                                        IncludeETags                                  = false,
                              ETagDigestEncoding?                                            ETagDigestEncoding                            = null,
                              CustomJObjectSerializerDelegate<ChargingStationManufacturer>?  CustomChargingStationManufacturerSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,          JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,             Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,      Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,       DataSource);

            if (Address is not null)
                json.Add(PropertyKeys.Address,              Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,             Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,   HotlinePhoneNumber.Value.ToString());

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomChargingStationManufacturerSerializer is not null
                       ? CustomChargingStationManufacturerSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this charging station manufacturer: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomChargingStationManufacturerSerializer">A delegate to serialize custom charging station manufacturer CBOR maps.</param>
        public CBORValue ToCBOR(Boolean                                                      Embedded                                      = false,
                                Boolean                                                      IncludeETags                                  = false,
                                CustomCBORSerializerDelegate<ChargingStationManufacturer>?   CustomChargingStationManufacturerSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),           CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),          CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),             Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),      Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),       CBORValue.FromText(DataSource));

            // The address has no CBOR representation of its own: it is written as the keys and values of its JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),              CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),             CBORValue.FromText(Homepage.Value.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),   CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),              CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),           CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),       CBORJSON.ToCBOR(CustomData.ToJObject()));

            AddETagsTo(cbor, IncludeETags);

            return CustomChargingStationManufacturerSerializer is not null
                       ? CustomChargingStationManufacturerSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingStationManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station manufacturer.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationManufacturer">The parsed charging station manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out ChargingStationManufacturer?  ChargingStationManufacturer,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            ChargingStationManufacturer = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "charging station manufacturer identification",
                                         ChargingStationManufacturer_Id.TryParse,
                                         out ChargingStationManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station manufacturer '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Name,            "manufacturer name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Description,     "manufacturer description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, BasePropertyKeys.DataSource,      "data source",               out var dataSource,   out ErrorResponse) ||
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

                ChargingStationManufacturer = new ChargingStationManufacturer(id,
                                                                              name,
                                                                              description,
                                                                              address,
                                                                              homepage,
                                                                              hotline,
                                                                              DataSource:  dataSource,
                                                                              Created:     created,
                                                                              LastChange:  lastChange,
                                                                              CustomData:  customData);

                if (!TryCheckETagsOf(JSON, ChargingStationManufacturer, out ErrorResponse))
                {
                    ChargingStationManufacturer = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationManufacturer  = null;
                ErrorResponse                = "The given JSON representation of a charging station manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out ChargingStationManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a charging station manufacturer: the keys and values of its JSON
        /// representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="ChargingStationManufacturer">The parsed charging station manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                              CBOR,
                                       [NotNullWhen(true)]  out ChargingStationManufacturer?  ChargingStationManufacturer,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            ChargingStationManufacturer = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a charging station manufacturer must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "charging station manufacturer identification",
                                         ChargingStationManufacturer_Id.TryParse,
                                         out ChargingStationManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station manufacturer '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptional    (BasePropertyKeys.Name,         "manufacturer name",         I18NString.TryParse, out I18NString? name,         out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptional    (BasePropertyKeys.Description,  "manufacturer description",  I18NString.TryParse, out I18NString? description,  out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource,   "data source",                                    out var         dataSource,   out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR representation of its own is read as its JSON, with the parsers of the JSON
                // representation: the address, the homepage, the hotline and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.Address,
                                            PropertyKeys.Homepage,
                                            PropertyKeys.HotlinePhoneNumber,
                                            BasePropertyKeys.Created,
                                            BasePropertyKeys.LastChange,
                                            BasePropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!POIJSON.TryParseAddress(json, PropertyKeys.Address,             "address",               out var address,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (json, PropertyKeys.Homepage,            "homepage",              URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (json, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",  PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
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

                ChargingStationManufacturer = new ChargingStationManufacturer(id,
                                                                              name,
                                                                              description,
                                                                              address,
                                                                              homepage,
                                                                              hotline,
                                                                              DataSource:  dataSource,
                                                                              Created:     created,
                                                                              LastChange:  lastChange,
                                                                              CustomData:  customData);

                if (!TryCheckETagsOf(CBOR, ChargingStationManufacturer, out ErrorResponse))
                {
                    ChargingStationManufacturer = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationManufacturer  = null;
                ErrorResponse                = "The given CBOR representation of a charging station manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging station manufacturers for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingStationManufacturer? Manufacturer1, ChargingStationManufacturer? Manufacturer2)

            => ReferenceEquals(Manufacturer1, Manufacturer2) ||
               (Manufacturer1 is not null && Manufacturer1.Equals(Manufacturer2));

        /// <summary>
        /// Compares two charging station manufacturers for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingStationManufacturer? Manufacturer1, ChargingStationManufacturer? Manufacturer2)

            => !(Manufacturer1 == Manufacturer2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingStationManufacturer manufacturer
                   ? CompareTo(manufacturer)
                   : throw new ArgumentException("The given object is not a charging station manufacturer!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingStationManufacturer? ChargingStationManufacturer)

            => ChargingStationManufacturer is null
                   ? throw new ArgumentNullException(nameof(ChargingStationManufacturer), "The given charging station manufacturer must not be null!")
                   : Id.CompareTo(ChargingStationManufacturer.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationManufacturer manufacturer &&
               Equals(manufacturer);

        /// <inheritdoc/>
        public Boolean Equals(ChargingStationManufacturer? ChargingStationManufacturer)

            => ChargingStationManufacturer is not null &&
               Id.Equals(ChargingStationManufacturer.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
