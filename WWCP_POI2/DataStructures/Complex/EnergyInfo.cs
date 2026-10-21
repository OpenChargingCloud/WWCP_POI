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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An energy information: whether a newly arriving vehicle can still charge sensibly,
    /// or whether too many vehicles are already fast charging.
    /// </summary>
    /// <param name="Used">The power currently delivered.</param>
    /// <param name="Available">The power a newly arriving vehicle would still get.</param>
    /// <param name="StoredEnergy">The optional energy stored in a battery buffer.</param>
    /// <param name="CurrentVoltage">The optional voltage an EVSE currently charges with.</param>
    /// <param name="RemainingChargingTime">The optional time the vehicle at an EVSE will probably still charge.</param>
    public readonly struct EnergyInfo(Watt       Used,
                                      Watt       Available,
                                      WattHour?  StoredEnergy            = null,
                                      Volt?      CurrentVoltage          = null,
                                      TimeSpan?  RemainingChargingTime   = null)

        : IEquatable<EnergyInfo>,
          IComparable<EnergyInfo>,
          IComparable

    {

        #region Data

        /// <summary>
        /// The JSON-LD context of this object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/energyInfo";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context                 = "@context";

            /// <summary>
            /// The property of its power currently delivered.
            /// </summary>
            public const String  Used                    = "used";

            /// <summary>
            /// The property of its power a newly arriving vehicle would still get.
            /// </summary>
            public const String  Available               = "available";

            /// <summary>
            /// The property of its energy stored in a battery buffer.
            /// </summary>
            public const String  StoredEnergy            = "storedEnergy";

            /// <summary>
            /// The property of its current voltage.
            /// </summary>
            public const String  CurrentVoltage          = "currentVoltage";

            /// <summary>
            /// The property of its remaining charging time.
            /// </summary>
            public const String  RemainingChargingTime   = "remainingChargingTime";

        }

        #endregion

        /// <summary>
        /// The power currently delivered.
        /// </summary>
        public Watt       Used                     { get; } = Used;

        /// <summary>
        /// The power a newly arriving vehicle would still get.
        /// </summary>
        public Watt       Available                { get; } = Available;

        /// <summary>
        /// The optional energy stored in a battery buffer,
        /// e.g. to estimate how long fast charging beyond the grid connection lasts.
        /// </summary>
        public WattHour?  StoredEnergy             { get; } = StoredEnergy;

        /// <summary>
        /// The optional voltage an EVSE currently charges with.
        /// </summary>
        public Volt?      CurrentVoltage           { get; } = CurrentVoltage;

        /// <summary>
        /// The optional time the vehicle at an EVSE will probably still charge.
        /// </summary>
        public TimeSpan?  RemainingChargingTime    { get; } = RemainingChargingTime;

        /// <summary>
        /// Whether it has values only an EVSE has: the current voltage or the remaining charging time.
        /// </summary>
        public Boolean    HasEVSEValues
            => CurrentVoltage.HasValue || RemainingChargingTime.HasValue;

        #endregion


        #region Parse

        /// <summary>
        /// Parse the given JSON representation of an energy information.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomEnergyInfoParser">An optional delegate to parse custom energy information JSON objects.</param>
        public static EnergyInfo Parse(JObject                                   JSON,
                                       CustomJObjectParserDelegate<EnergyInfo>?  CustomEnergyInfoParser   = null)
        {

            if (TryParse(JSON, out var energyInfo, out var errorResponse, CustomEnergyInfoParser))
                return energyInfo;

            throw new ArgumentException("The given JSON representation of an energy information is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region TryParse

        /// <summary>
        /// Try to parse the given JSON representation of an energy information.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyInfo">The parsed energy information.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEnergyInfoParser">An optional delegate to parse custom energy information JSON objects.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       out EnergyInfo                            EnergyInfo,
                                       [NotNullWhen(false)] out String?          ErrorResponse,
                                       CustomJObjectParserDelegate<EnergyInfo>?  CustomEnergyInfoParser   = null)
        {

            EnergyInfo     = default;
            ErrorResponse  = null;

            try
            {

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                #region Parse Used            [mandatory]

                if (!JSON.ParseMandatory(PropertyKeys.Used,
                                         "used",
                                         Watt.TryParse,
                                         out Watt used,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Available       [mandatory]

                if (!JSON.ParseMandatory(PropertyKeys.Available,
                                         "available",
                                         Watt.TryParse,
                                         out Watt available,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse StoredEnergy    [optional]

                WattHour? storedEnergy = null;

                if (JSON.ParseOptionalStruct(PropertyKeys.StoredEnergy,
                                             "stored energy",
                                             WattHour.TryParse,
                                             out WattHour storedEnergyValue,
                                             out ErrorResponse))
                {

                    if (ErrorResponse is not null)
                        return false;

                    storedEnergy = storedEnergyValue;

                }

                #endregion

                #region Parse CurrentVoltage  [optional]

                Volt? currentVoltage = null;

                if (JSON.ParseOptionalStruct(PropertyKeys.CurrentVoltage,
                                             "current voltage",
                                             Volt.TryParse,
                                             out Volt currentVoltageValue,
                                             out ErrorResponse))
                {

                    if (ErrorResponse is not null)
                        return false;

                    if (currentVoltageValue.Value < 0)
                    {
                        ErrorResponse = "The current voltage must not be negative!";
                        return false;
                    }

                    currentVoltage = currentVoltageValue;

                }

                #endregion

                #region Parse RemainingChargingTime [optional]

                TimeSpan? remainingChargingTime = null;

                if (JSON[PropertyKeys.RemainingChargingTime] is JToken remainingChargingTimeToken)
                {

                    if (remainingChargingTimeToken.Type is not (JTokenType.Integer or JTokenType.Float) ||
                        remainingChargingTimeToken.Value<Decimal>() < 0)
                    {
                        ErrorResponse = "The remaining charging time must be a number of seconds, not negative!";
                        return false;
                    }

                    remainingChargingTime = FromSeconds(remainingChargingTimeToken.Value<Decimal>());

                }

                #endregion


                EnergyInfo = new EnergyInfo(
                                 used,
                                 available,
                                 storedEnergy,
                                 currentVoltage,
                                 remainingChargingTime
                             );


                if (CustomEnergyInfoParser is not null)
                    EnergyInfo = CustomEnergyInfoParser(JSON,
                                                        EnergyInfo);

                return true;

            }
            catch (Exception e)
            {
                EnergyInfo     = default;
                ErrorResponse  = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false)

        /// <summary>
        /// Return a JSON representation of this object.
        /// Powers, energy and voltage are written culture-invariant with their unit, e.g. "11000 W";
        /// the remaining charging time as a number of seconds.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomEnergyInfoSerializer">A delegate to serialize custom energy information JSON objects.</param>
        public JObject ToJSON(Boolean                                       Embedded                     = false,
                              CustomJObjectSerializerDelegate<EnergyInfo>?  CustomEnergyInfoSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,                 JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Used,                    Used.     ToString()),
                                 new JProperty(PropertyKeys.Available,               Available.ToString()),

                           StoredEnergy.HasValue
                               ? new JProperty(PropertyKeys.StoredEnergy,            StoredEnergy.Value.ToString())
                               : null,

                           CurrentVoltage.HasValue
                               ? new JProperty(PropertyKeys.CurrentVoltage,          CurrentVoltage.Value.ToString())
                               : null,

                           RemainingChargingTime.HasValue
                               ? new JProperty(PropertyKeys.RemainingChargingTime,   Seconds(RemainingChargingTime.Value))
                               : null

                       );

            return CustomEnergyInfoSerializer is not null
                       ? CustomEnergyInfoSerializer(this, json)
                       : json;

        }

        #endregion


        #region (private static) Seconds    (Duration)

        /// <summary>
        /// The given duration in seconds, as a decimal.
        /// </summary>
        private static Decimal Seconds(TimeSpan Duration)

            => (Decimal) Duration.Ticks / TimeSpan.TicksPerSecond;

        #endregion

        #region (private static) FromSeconds(Seconds)

        /// <summary>
        /// The duration of the given number of seconds.
        /// </summary>
        private static TimeSpan FromSeconds(Decimal Seconds)

            => TimeSpan.FromTicks((Int64) (Seconds * TimeSpan.TicksPerSecond));

        #endregion


        #region Operator overloading

        #region Operator == (EnergyInfo1, EnergyInfo2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyInfo1">An energy information.</param>
        /// <param name="EnergyInfo2">Another energy information.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyInfo EnergyInfo1,
                                           EnergyInfo EnergyInfo2)

            => EnergyInfo1.Equals(EnergyInfo2);

        #endregion

        #region Operator != (EnergyInfo1, EnergyInfo2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyInfo1">An energy information.</param>
        /// <param name="EnergyInfo2">Another energy information.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyInfo EnergyInfo1,
                                           EnergyInfo EnergyInfo2)

            => !EnergyInfo1.Equals(EnergyInfo2);

        #endregion

        #region Operator <  (EnergyInfo1, EnergyInfo2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyInfo1">An energy information.</param>
        /// <param name="EnergyInfo2">Another energy information.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyInfo EnergyInfo1,
                                          EnergyInfo EnergyInfo2)

            => EnergyInfo1.CompareTo(EnergyInfo2) < 0;

        #endregion

        #region Operator <= (EnergyInfo1, EnergyInfo2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyInfo1">An energy information.</param>
        /// <param name="EnergyInfo2">Another energy information.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyInfo EnergyInfo1,
                                           EnergyInfo EnergyInfo2)

            => EnergyInfo1.CompareTo(EnergyInfo2) <= 0;

        #endregion

        #region Operator >  (EnergyInfo1, EnergyInfo2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyInfo1">An energy information.</param>
        /// <param name="EnergyInfo2">Another energy information.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyInfo EnergyInfo1,
                                          EnergyInfo EnergyInfo2)

            => EnergyInfo1.CompareTo(EnergyInfo2) > 0;

        #endregion

        #region Operator >= (EnergyInfo1, EnergyInfo2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyInfo1">An energy information.</param>
        /// <param name="EnergyInfo2">Another energy information.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyInfo EnergyInfo1,
                                           EnergyInfo EnergyInfo2)

            => EnergyInfo1.CompareTo(EnergyInfo2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyInfo> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy information.
        /// </summary>
        /// <param name="Object">An energy information to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyInfo energyInfo
                   ? CompareTo(energyInfo)
                   : throw new ArgumentException("The given object is not an energy information!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyInfo)

        /// <summary>
        /// Compares two energy information.
        /// </summary>
        /// <param name="EnergyInfo">An energy information to compare with.</param>
        public Int32 CompareTo(EnergyInfo EnergyInfo)
        {

            var c = Used.     CompareTo(EnergyInfo.Used);

            if (c == 0)
                c = Available.CompareTo(EnergyInfo.Available);

            if (c == 0)
                c = Nullable.Compare(StoredEnergy,           EnergyInfo.StoredEnergy);

            if (c == 0)
                c = Nullable.Compare(CurrentVoltage,         EnergyInfo.CurrentVoltage);

            if (c == 0)
                c = Nullable.Compare(RemainingChargingTime,  EnergyInfo.RemainingChargingTime);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<EnergyInfo> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy information for equality.
        /// </summary>
        /// <param name="Object">An energy information to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyInfo energyInfo &&
                   Equals(energyInfo);

        #endregion

        #region Equals(EnergyInfo)

        /// <summary>
        /// Compares two energy information for equality.
        /// </summary>
        /// <param name="EnergyInfo">An energy information to compare with.</param>
        public Boolean Equals(EnergyInfo EnergyInfo)

            => Used.                 Equals(EnergyInfo.Used)            &&
               Available.            Equals(EnergyInfo.Available)       &&
               StoredEnergy.         Equals(EnergyInfo.StoredEnergy)    &&
               CurrentVoltage.       Equals(EnergyInfo.CurrentVoltage)  &&
               RemainingChargingTime.Equals(EnergyInfo.RemainingChargingTime);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the HashCode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            unchecked
            {

                return Used.                  GetHashCode()       * 11 ^
                       Available.             GetHashCode()       *  7 ^
                      (StoredEnergy?.         GetHashCode() ?? 0) *  5 ^
                      (CurrentVoltage?.       GetHashCode() ?? 0) *  3 ^
                      (RemainingChargingTime?.GetHashCode() ?? 0);

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Used} used, {Available} available" +
               (StoredEnergy.         HasValue ? $", {StoredEnergy.Value} stored"                                   : "") +
               (CurrentVoltage.       HasValue ? $", at {CurrentVoltage.Value}"                                     : "") +
               (RemainingChargingTime.HasValue ? $", {Seconds(RemainingChargingTime.Value)} s remaining charging time" : "");

        #endregion

    }

}
