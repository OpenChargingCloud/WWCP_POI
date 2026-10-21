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
    /// Whether a charging connector is a socket to plug a cable into, or a cable attached to the charging station.
    /// </summary>
    public enum ChargingConnectorFormat
    {

        /// <summary>
        /// A socket: the driver brings the cable.
        /// </summary>
        SOCKET,

        /// <summary>
        /// A cable attached to the charging station.
        /// </summary>
        CABLE

    }


    /// <summary>
    /// A charging connector of an EVSE: a plug or a socket. Content of its EVSE, without a status of its own.
    /// Its current types, maximum voltage, current and power and its charging tariffs are those of its EVSE, unless it
    /// has its own, e.g. a CHAdeMO connector of 50 kW beside a CCS one of 150 kW.
    /// </summary>
    public sealed record ChargingConnector
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/chargingConnector";

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
            public const String  Id                      = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                 = "@context";

            /// <summary>
            /// The property of its type.
            /// </summary>
            public const String  Type                    = "type";

            /// <summary>
            /// The property of whether it can be locked.
            /// </summary>
            public const String  Lockable                = "lockable";

            /// <summary>
            /// The property of its format: a socket or an attached cable.
            /// </summary>
            public const String  Format                  = "format";

            /// <summary>
            /// The property of the length of its attached cable.
            /// </summary>
            public const String  CableLength             = "cableLength";

            /// <summary>
            /// The property of the weight of its attached cable.
            /// </summary>
            public const String  CableWeight             = "cableWeight";

            /// <summary>
            /// The property of whether a system helps to handle its cable.
            /// </summary>
            public const String  CableManagementSystem   = "cableManagementSystem";

            /// <summary>
            /// The property of its standards.
            /// </summary>
            public const String  Standards               = "standards";

            /// <summary>
            /// The property of its images.
            /// </summary>
            public const String  Images                  = "images";

            /// <summary>
            /// The property of its current types.
            /// </summary>
            public const String  CurrentTypes            = "currentTypes";

            /// <summary>
            /// The property of its maximum voltage.
            /// </summary>
            public const String  MaxVoltage              = "maxVoltage";

            /// <summary>
            /// The property of its maximum current.
            /// </summary>
            public const String  MaxCurrent              = "maxCurrent";

            /// <summary>
            /// The property of its maximum power.
            /// </summary>
            public const String  MaxPower                = "maxPower";

            /// <summary>
            /// The property of the identifications of its charging tariffs.
            /// </summary>
            public const String  ChargingTariffIds       = "chargingTariffIds";

        }

        #endregion


        /// <summary>
        /// The identification of the connector, unique within its EVSE.
        /// </summary>
        public ChargingConnector_Id      Id                      { get; }

        /// <summary>
        /// The type of the connector, e.g. IEC_62196_T2.
        /// </summary>
        public ChargingConnectorType     Type                    { get; }

        /// <summary>
        /// Whether the connector can be locked, if known.
        /// </summary>
        public Boolean?                  Lockable                { get; }

        /// <summary>
        /// Whether it is a socket or an attached cable, if known.
        /// </summary>
        public ChargingConnectorFormat?  Format                  { get; }

        /// <summary>
        /// The length of its attached cable.
        /// </summary>
        public Meter?                    CableLength             { get; }

        /// <summary>
        /// The weight of its attached cable, as a driver handles it.
        /// </summary>
        public Kilogram?                 CableWeight             { get; }

        /// <summary>
        /// Whether a system helps to store, lift or guide its cable, if known.
        /// </summary>
        public Boolean?                  CableManagementSystem   { get; }

        /// <summary>
        /// The standards it conforms to, e.g. accessibility ones, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>  Standards               { get; }

        /// <summary>
        /// Images of it, e.g. photos, each URL once, in their given order.
        /// </summary>
        public ImmutableArray<Image>     Images                  { get; }

        /// <summary>
        /// Its current types, unless those of its EVSE.
        /// </summary>
        public CurrentTypes?             CurrentTypes            { get; }

        /// <summary>
        /// Its maximum voltage, unless that of its EVSE.
        /// </summary>
        public Volt?                     MaxVoltage              { get; }

        /// <summary>
        /// Its maximum current, unless that of its EVSE.
        /// </summary>
        public Ampere?                   MaxCurrent              { get; }

        /// <summary>
        /// Its maximum power, unless that of its EVSE.
        /// </summary>
        public Watt?                     MaxPower                { get; }

        /// <summary>
        /// Its charging tariffs, of the operator of its EVSE, in the order of their identifications; none: those of its EVSE.
        /// </summary>
        public ImmutableArray<ChargingTariff_Id>  ChargingTariffIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging connector.
        /// </summary>
        /// <param name="Id">The identification of the connector, unique within its EVSE.</param>
        /// <param name="Type">The type of the connector, e.g. IEC_62196_T2.</param>
        /// <param name="Lockable">Whether the connector can be locked, if known.</param>
        /// <param name="Format">Whether it is a socket or an attached cable, if known.</param>
        /// <param name="CableLength">The length of its attached cable.</param>
        /// <param name="CableWeight">The weight of its attached cable, as a driver handles it.</param>
        /// <param name="CableManagementSystem">Whether a system helps to store, lift or guide its cable, if known.</param>
        /// <param name="Standards">The standards it conforms to, e.g. accessibility ones.</param>
        /// <param name="Images">Images of it, e.g. photos.</param>
        /// <param name="CurrentTypes">Its current types, unless those of its EVSE.</param>
        /// <param name="MaxVoltage">Its maximum voltage, unless that of its EVSE.</param>
        /// <param name="MaxCurrent">Its maximum current, unless that of its EVSE.</param>
        /// <param name="MaxPower">Its maximum power, unless that of its EVSE.</param>
        /// <param name="ChargingTariffIds">Its charging tariffs, unless those of its EVSE.</param>
        public ChargingConnector(ChargingConnector_Id             Id,
                                 ChargingConnectorType            Type,
                                 Boolean?                         Lockable                = null,
                                 ChargingConnectorFormat?         Format                  = null,
                                 Meter?                           CableLength             = null,
                                 Kilogram?                        CableWeight             = null,
                                 Boolean?                         CableManagementSystem   = null,
                                 IEnumerable<Standard>?           Standards               = null,
                                 IEnumerable<Image>?              Images                  = null,
                                 CurrentTypes?                    CurrentTypes            = null,
                                 Volt?                            MaxVoltage              = null,
                                 Ampere?                          MaxCurrent              = null,
                                 Watt?                            MaxPower                = null,
                                 IEnumerable<ChargingTariff_Id>?  ChargingTariffIds       = null)
        {

            this.Id                     = Id;
            this.Type                   = Type;
            this.Lockable               = Lockable;
            this.Format                 = Format;
            this.CableLength            = CableLength;
            this.CableWeight            = CableWeight;
            this.CableManagementSystem  = CableManagementSystem;
            this.Standards              = POIJSON.OrderedIds(Standards, $"standard of the charging connector '{Id}'");
            this.Images                 = POIJSON.Distinct  (Images, image => image.URL.ToString(), $"image of the charging connector '{Id}'");
            this.CurrentTypes           = CurrentTypes == POI.CurrentTypes.Unspecified ? null : CurrentTypes;
            this.MaxVoltage             = MaxVoltage;
            this.MaxCurrent             = MaxCurrent;
            this.MaxPower               = MaxPower;
            this.ChargingTariffIds      = POIJSON.OrderedIds(ChargingTariffIds, $"charging tariff of the charging connector '{Id}'");

        }

        #endregion

        #region CurrentTypesAt(EVSE) / MaxVoltageAt(EVSE) / MaxCurrentAt(EVSE) / MaxPowerAt(EVSE) / ChargingTariffIdsAt(EVSE)

        /// <summary>
        /// Its current types: its own, else those of the given EVSE, its EVSE.
        /// </summary>
        public CurrentTypes CurrentTypesAt(EVSE EVSE)
            => CurrentTypes ?? EVSE.CurrentTypes;

        /// <summary>
        /// Its maximum voltage: its own, else that of the given EVSE, its EVSE.
        /// </summary>
        public Volt? MaxVoltageAt(EVSE EVSE)
            => MaxVoltage ?? EVSE.MaxVoltage;

        /// <summary>
        /// Its maximum current: its own, else that of the given EVSE, its EVSE.
        /// </summary>
        public Ampere? MaxCurrentAt(EVSE EVSE)
            => MaxCurrent ?? EVSE.MaxCurrent;

        /// <summary>
        /// Its maximum power: its own, else that of the given EVSE, its EVSE.
        /// </summary>
        public Watt? MaxPowerAt(EVSE EVSE)
            => MaxPower ?? EVSE.MaxPower;

        /// <summary>
        /// Its charging tariffs: its own, else those of the given EVSE, its EVSE, e.g. its effective one.
        /// </summary>
        public ImmutableArray<ChargingTariff_Id> ChargingTariffIdsAt(EVSE EVSE)
            => ChargingTariffIds.Length > 0 ? ChargingTariffIds : EVSE.ChargingTariffIds;

        #endregion

        #region Validate()

        /// <summary>
        /// The error, when this charging connector is invalid.
        /// </summary>
        internal String? Validate()
        {

            if (CableLength?.m <= 0)
                return $"The cable length of the charging connector '{Id}' must be positive!";

            if (CableWeight?.Value <= 0)
                return $"The cable weight of the charging connector '{Id}' must be positive!";

            if (MaxVoltage?.Value < 0 || MaxCurrent?.Value < 0 || MaxPower?.Value < 0)
                return $"The electrical limits of the charging connector '{Id}' must not be negative!";

            if (Format == ChargingConnectorFormat.SOCKET &&
               (CableLength.HasValue || CableWeight.HasValue || CableManagementSystem.HasValue))
            {
                return $"The charging connector '{Id}' is a socket: it has no cable of its own!";
            }

            return null;

        }

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return a JSON representation of this charging connector; embedded, as its EVSE embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingConnectorSerializer">A delegate to serialize custom charging connector JSON objects.</param>
        public JObject ToJSON(Boolean                                              Embedded                            = false,
                              CustomJObjectSerializerDelegate<ChargingConnector>?  CustomChargingConnectorSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,                JSONLDContext);

            json.Add(PropertyKeys.Type,                       Type.ToString());

            if (Lockable.HasValue)
                json.Add(PropertyKeys.Lockable,               Lockable.Value);

            if (Format.HasValue)
                json.Add(PropertyKeys.Format,                 Format.Value.ToString());

            if (CableLength.HasValue)
                json.Add(PropertyKeys.CableLength,            POIJSON.Number(CableLength.Value.m));

            if (CableWeight.HasValue)
                json.Add(PropertyKeys.CableWeight,            POIJSON.Number(CableWeight.Value.Value));

            if (CableManagementSystem.HasValue)
                json.Add(PropertyKeys.CableManagementSystem,  CableManagementSystem.Value);

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,              POIJSON.IdsToJSON(Standards));

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,                 new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (CurrentTypes.HasValue)
                json.Add(PropertyKeys.CurrentTypes,           POIJSON.FlagsToJSON(CurrentTypes.Value));

            if (MaxVoltage.HasValue)
                json.Add(PropertyKeys.MaxVoltage,             POIJSON.Number(MaxVoltage.Value.Value));

            if (MaxCurrent.HasValue)
                json.Add(PropertyKeys.MaxCurrent,             POIJSON.Number(MaxCurrent.Value.Value));

            if (MaxPower.HasValue)
                json.Add(PropertyKeys.MaxPower,               POIJSON.Number(MaxPower.Value.Value));

            if (ChargingTariffIds.Length > 0)
                json.Add(PropertyKeys.ChargingTariffIds,      POIJSON.IdsToJSON(ChargingTariffIds));

            return CustomChargingConnectorSerializer is not null
                       ? CustomChargingConnectorSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingConnector, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging connector.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingConnector">The parsed charging connector.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out ChargingConnector?  ChargingConnector,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            ChargingConnector = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "charging connector identification",
                                         ChargingConnector_Id.TryParse,
                                         out ChargingConnector_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging connector '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory(PropertyKeys.Type,
                                         "charging connector type",
                                         ChargingConnectorType.TryParse,
                                         out ChargingConnectorType type,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.Lockable,
                                       "lockable",
                                       out Boolean? lockable,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                if (JSON.ParseOptional(PropertyKeys.CableManagementSystem,
                                       "cable management system",
                                       out Boolean? cableManagementSystem,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                if (!POIJSON.TryParseEnum       (JSON, PropertyKeys.Format,       "charging connector format",  out ChargingConnectorFormat? format,  out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, PropertyKeys.CableLength,  "cable length",               out var cableLength,                out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, PropertyKeys.CableWeight,  "cable weight",               out var cableWeight,                out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Standards,    "standard", Standard.TryParse, out ImmutableArray<Standard> standards, out ErrorResponse) ||
                    !POIJSON.TryParseObjects<Image>(JSON, PropertyKeys.Images,    "images",   Image.TryParse,    out var images,                    out ErrorResponse) ||
                    !POIJSON.TryParseFlags      (JSON, PropertyKeys.CurrentTypes, "current type",               out CurrentTypes currentTypes,      out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.ChargingTariffIds, "charging tariff identification", ChargingTariff_Id.TryParse, out ImmutableArray<ChargingTariff_Id> chargingTariffIds, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.MaxVoltage, "maximum voltage", out Volt?   maxVoltage, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.MaxCurrent, "maximum current", out Ampere? maxCurrent, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.MaxPower,   "maximum power",   out Watt?   maxPower,   out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                var chargingConnector = new ChargingConnector(id,
                                                              type,
                                                              lockable,
                                                              format,
                                                              cableLength.HasValue ? Meter.   From_m (cableLength.Value) : null,
                                                              cableWeight.HasValue ? Kilogram.FromKG(cableWeight.Value) : null,
                                                              cableManagementSystem,
                                                              standards,
                                                              images,
                                                              currentTypes,
                                                              maxVoltage,
                                                              maxCurrent,
                                                              maxPower,
                                                              chargingTariffIds);

                if (chargingConnector.Validate() is String error)
                {
                    ErrorResponse = error;
                    return false;
                }

                ChargingConnector  = chargingConnector;
                ErrorResponse      = null;
                return true;

            }
            catch (Exception e)
            {
                ErrorResponse = "The given JSON representation of a charging connector is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Equals(ChargingConnector) / GetHashCode()

        /// <summary>
        /// Whether both charging connectors say the same: as their arrays are compared by their contents,
        /// by their JSON representations.
        /// </summary>
        public Boolean Equals(ChargingConnector? ChargingConnector)

            => ChargingConnector is not null &&
               POIJSON.SameValue(ToJSON(Embedded: true), ChargingConnector.ToJSON(Embedded: true));

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => ToJSON(Embedded: true).ToString(Newtonsoft.Json.Formatting.None).GetHashCode();

        #endregion

    }

}
