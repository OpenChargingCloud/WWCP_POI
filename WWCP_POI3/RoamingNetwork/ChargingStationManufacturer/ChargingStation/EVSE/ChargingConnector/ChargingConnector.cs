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

        #region Properties

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

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this charging connector.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id",   Id.  ToString()),
                           new JProperty("type",  Type.ToString())
                       );

            if (Lockable.HasValue)
                json.Add("lockable",               Lockable.Value);

            if (Format.HasValue)
                json.Add("format",                 Format.Value.ToString());

            if (CableLength.HasValue)
                json.Add("cableLength",            POIJSON.Number(CableLength.Value.m));

            if (CableWeight.HasValue)
                json.Add("cableWeight",            POIJSON.Number(CableWeight.Value.Value));

            if (CableManagementSystem.HasValue)
                json.Add("cableManagementSystem",  CableManagementSystem.Value);

            if (Standards.Length > 0)
                json.Add("standards",              POIJSON.IdsToJSON(Standards));

            if (Images.Length > 0)
                json.Add("images",                 new JArray(Images.Select(image => image.ToJSON())));

            if (CurrentTypes.HasValue)
                json.Add("currentTypes",           POIJSON.FlagsToJSON(CurrentTypes.Value));

            if (MaxVoltage.HasValue)
                json.Add("maxVoltage",             POIJSON.Number(MaxVoltage.Value.Value));

            if (MaxCurrent.HasValue)
                json.Add("maxCurrent",             POIJSON.Number(MaxCurrent.Value.Value));

            if (MaxPower.HasValue)
                json.Add("maxPower",               POIJSON.Number(MaxPower.Value.Value));

            if (ChargingTariffIds.Length > 0)
                json.Add("chargingTariffIds",      POIJSON.IdsToJSON(ChargingTariffIds));

            return json;

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

                if (!JSON.ParseMandatory("@id",
                                         "charging connector identification",
                                         ChargingConnector_Id.TryParse,
                                         out ChargingConnector_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!JSON.ParseMandatory("type",
                                         "charging connector type",
                                         ChargingConnectorType.TryParse,
                                         out ChargingConnectorType type,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("lockable",
                                       "lockable",
                                       out Boolean? lockable,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                if (JSON.ParseOptional("cableManagementSystem",
                                       "cable management system",
                                       out Boolean? cableManagementSystem,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                if (!POIJSON.TryParseEnum       (JSON, "format",       "charging connector format",  out ChargingConnectorFormat? format,  out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, "cableLength",  "cable length",               out var cableLength,                out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, "cableWeight",  "cable weight",               out var cableWeight,                out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "standards",    "standard", Standard.TryParse, out ImmutableArray<Standard> standards, out ErrorResponse) ||
                    !POIJSON.TryParseObjects<Image>(JSON, "images",    "images",   Image.TryParse,    out var images,                    out ErrorResponse) ||
                    !POIJSON.TryParseFlags      (JSON, "currentTypes", "current type",               out CurrentTypes currentTypes,      out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "chargingTariffIds", "charging tariff identification", ChargingTariff_Id.TryParse, out ImmutableArray<ChargingTariff_Id> chargingTariffIds, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("maxVoltage", "maximum voltage", out Volt?   maxVoltage, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("maxCurrent", "maximum current", out Ampere? maxCurrent, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("maxPower",   "maximum power",   out Watt?   maxPower,   out ErrorResponse))
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
               POIJSON.SameValue(ToJSON(), ChargingConnector.ToJSON());

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => ToJSON().ToString(Newtonsoft.Json.Formatting.None).GetHashCode();

        #endregion

    }

}
