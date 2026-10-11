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
    /// Extension methods for the EVSE energy status.
    /// </summary>
    public static class EVSEEnergyStatusExtensions
    {

        #region ToJSON(this EVSEEnergyStatus, Skip = null, Take = null)

        public static JObject ToJSON(this IEnumerable<EVSEEnergyStatus>  EVSEEnergyStatus,
                                     UInt64?                             Skip   = null,
                                     UInt64?                             Take   = null)
        {

            #region Initial checks

            if (EVSEEnergyStatus is null || !EVSEEnergyStatus.Any())
                return new JObject();

            #endregion

            #region Maybe there are duplicate EVSE identifications in the enumeration... take the newest one!

            var filteredEnergyInfo = new Dictionary<EVSE_Id, EVSEEnergyStatus>();

            foreach (var status in EVSEEnergyStatus)
            {

                if (!filteredEnergyInfo.TryGetValue(status.Id, out var known) ||
                    known.Timestamp < status.Timestamp)
                {
                    filteredEnergyInfo[status.Id] = status;
                }

            }

            #endregion


            return new JObject((Take.HasValue ? filteredEnergyInfo.OrderBy(status => status.Key).Skip(Skip).Take(Take)
                                              : filteredEnergyInfo.OrderBy(status => status.Key).Skip(Skip)).

                                   Select(kvp => new JProperty(kvp.Key.ToString(),
                                                               new JArray(kvp.Value.Timestamp.  ToISO8601(),
                                                                          kvp.Value.EnergyInfo.ToJSON(Embedded: true))
                                                              )));

        }

        #endregion

    }


    /// <summary>
    /// The current energy status of an EVSE.
    /// </summary>
    public readonly struct EVSEEnergyStatus : IEquatable<EVSEEnergyStatus>,
                                              IComparable<EVSEEnergyStatus>,
                                              IComparable
    {

        #region Properties

        /// <summary>
        /// The unique identification of the EVSE.
        /// </summary>
        public EVSE_Id         Id            { get; }

        /// <summary>
        /// The current energy status of the EVSE.
        /// </summary>
        public EnergyInfo      EnergyInfo    { get; }

        /// <summary>
        /// The timestamp of the current energy status of the EVSE.
        /// </summary>
        public DateTimeOffset  Timestamp     { get; }

        /// <summary>
        /// An optional data source or context for this EVSE energy status.
        /// </summary>
        public Context?        Context       { get; }

        /// <summary>
        /// The timestamped energy status of the EVSE.
        /// </summary>
        public Timestamped<EnergyInfo> TimestampedEnergyStatus
            => new (Timestamp, EnergyInfo);

        #endregion

        #region Constructor(s)

        #region EVSEEnergyStatus(Id,            EnergyInfo, Context = null)

        /// <summary>
        /// Create a new EVSE energy status.
        /// </summary>
        /// <param name="Id">The unique identification of the EVSE.</param>
        /// <param name="EnergyInfo">The current timestamped energy information of the EVSE.</param>
        /// <param name="Context">An optional data source or context for the EVSE energy status.</param>
        public EVSEEnergyStatus(EVSE_Id                  Id,
                                Timestamped<EnergyInfo>  EnergyInfo,
                                Context?                 Context   = null)

            : this(Id,
                   EnergyInfo.Value,
                   EnergyInfo.Timestamp,
                   Context)

        { }

        #endregion

        #region EVSEEnergyStatus(Id, EnergyInfo, Timestamp, Context = null)

        /// <summary>
        /// Create a new EVSE energy status.
        /// </summary>
        /// <param name="Id">The unique identification of the EVSE.</param>
        /// <param name="EnergyInfo">The current energy information of the EVSE.</param>
        /// <param name="Timestamp">The timestamp of the energy information of the EVSE.</param>
        /// <param name="Context">An optional data source or context for the EVSE energy status.</param>
        public EVSEEnergyStatus(EVSE_Id         Id,
                                EnergyInfo      EnergyInfo,
                                DateTimeOffset  Timestamp,
                                Context?        Context   = null)
        {

            this.Id          = Id;
            this.EnergyInfo  = EnergyInfo;
            this.Timestamp   = Timestamp;
            this.Context     = Context;

            unchecked
            {

                hashCode = Id.        GetHashCode() * 7 ^
                           EnergyInfo.GetHashCode() * 5 ^
                           Timestamp. ToUnixTimeMilliseconds().GetHashCode() * 3 ^
                          (Context?.  GetHashCode() ?? 0);

            }

        }

        #endregion

        #endregion


        #region Parse/TryParse

        /// <summary>
        /// Parse the given JSON representation of an EVSE energy status.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomEVSEEnergyStatusParser">An optional delegate to parse custom EVSE energy status JSON objects.</param>
        public static EVSEEnergyStatus Parse(JObject                                         JSON,
                                             CustomJObjectParserDelegate<EVSEEnergyStatus>?  CustomEVSEEnergyStatusParser   = null)
        {

            if (TryParse(JSON, out var evseEnergyStatus, out var errorResponse, CustomEVSEEnergyStatusParser))
                return evseEnergyStatus;

            throw new ArgumentException("The given JSON representation of an EVSE energy status is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        /// <summary>
        /// Try to parse the given JSON representation of an EVSE energy status.
        /// The context is internal data and is not part of the JSON representation.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EVSEEnergyStatus">The parsed EVSE energy status.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomEVSEEnergyStatusParser">An optional delegate to parse custom EVSE energy status JSON objects.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       out EVSEEnergyStatus                            EVSEEnergyStatus,
                                       [NotNullWhen(false)] out String?                ErrorResponse,
                                       CustomJObjectParserDelegate<EVSEEnergyStatus>?  CustomEVSEEnergyStatusParser   = null)
        {

            EVSEEnergyStatus  = default;
            ErrorResponse     = null;

            try
            {

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                #region Parse Id            [mandatory]

                if (!JSON.ParseMandatory("id",
                                         "EVSE identification",
                                         EVSE_Id.TryParse,
                                         out EVSE_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse EnergyInfo    [mandatory]

                if (!JSON.ParseMandatoryJSON("energyInfo",
                                             "energy information",
                                             (JObject json, out EnergyInfo value, out String? error) => POI.EnergyInfo.TryParse(json, out value, out error),
                                             out EnergyInfo energyInfo,
                                             out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Parse Timestamp     [mandatory]

                if (!JSON.ParseMandatory("timestamp",
                                         "timestamp",
                                         out DateTimeOffset timestamp,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion


                EVSEEnergyStatus = new EVSEEnergyStatus(
                                       id,
                                       energyInfo,
                                       timestamp
                                   );


                if (CustomEVSEEnergyStatusParser is not null)
                    EVSEEnergyStatus = CustomEVSEEnergyStatusParser(JSON,
                                                                    EVSEEnergyStatus);

                return true;

            }
            catch (Exception e)
            {
                EVSEEnergyStatus  = default;
                ErrorResponse     = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this object.
        /// The timestamp is written in UTC with millisecond precision.
        /// </summary>
        public JObject ToJSON()

            => JSONObject.Create(
                   new JProperty("id",           Id.        ToString()),
                   new JProperty("energyInfo",   EnergyInfo.ToJSON(Embedded: true)),
                   new JProperty("timestamp",    Timestamp. ToISO8601())
               );

        #endregion



        #region Operator overloading

        #region Operator == (EVSEEnergyStatus1, EVSEEnergyStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEEnergyStatus1">An EVSE energy status.</param>
        /// <param name="EVSEEnergyStatus2">Another EVSE energy status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVSEEnergyStatus EVSEEnergyStatus1,
                                           EVSEEnergyStatus EVSEEnergyStatus2)

            => EVSEEnergyStatus1.Equals(EVSEEnergyStatus2);

        #endregion

        #region Operator != (EVSEEnergyStatus1, EVSEEnergyStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEEnergyStatus1">An EVSE energy status.</param>
        /// <param name="EVSEEnergyStatus2">Another EVSE energy status.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVSEEnergyStatus EVSEEnergyStatus1,
                                           EVSEEnergyStatus EVSEEnergyStatus2)

            => !EVSEEnergyStatus1.Equals(EVSEEnergyStatus2);

        #endregion

        #region Operator <  (EVSEEnergyStatus1, EVSEEnergyStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEEnergyStatus1">An EVSE energy status.</param>
        /// <param name="EVSEEnergyStatus2">Another EVSE energy status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EVSEEnergyStatus EVSEEnergyStatus1,
                                          EVSEEnergyStatus EVSEEnergyStatus2)

            => EVSEEnergyStatus1.CompareTo(EVSEEnergyStatus2) < 0;

        #endregion

        #region Operator <= (EVSEEnergyStatus1, EVSEEnergyStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEEnergyStatus1">An EVSE energy status.</param>
        /// <param name="EVSEEnergyStatus2">Another EVSE energy status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EVSEEnergyStatus EVSEEnergyStatus1,
                                           EVSEEnergyStatus EVSEEnergyStatus2)

            => EVSEEnergyStatus1.CompareTo(EVSEEnergyStatus2) <= 0;

        #endregion

        #region Operator >  (EVSEEnergyStatus1, EVSEEnergyStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEEnergyStatus1">An EVSE energy status.</param>
        /// <param name="EVSEEnergyStatus2">Another EVSE energy status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EVSEEnergyStatus EVSEEnergyStatus1,
                                          EVSEEnergyStatus EVSEEnergyStatus2)

            => EVSEEnergyStatus1.CompareTo(EVSEEnergyStatus2) > 0;

        #endregion

        #region Operator >= (EVSEEnergyStatus1, EVSEEnergyStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEEnergyStatus1">An EVSE energy status.</param>
        /// <param name="EVSEEnergyStatus2">Another EVSE energy status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EVSEEnergyStatus EVSEEnergyStatus1,
                                           EVSEEnergyStatus EVSEEnergyStatus2)

            => EVSEEnergyStatus1.CompareTo(EVSEEnergyStatus2) >= 0;

        #endregion

        #endregion

        #region IComparable<EVSEEnergyStatus> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVSE energy status.
        /// </summary>
        /// <param name="Object">An EVSE energy status to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EVSEEnergyStatus evseEnergyStatus
                   ? CompareTo(evseEnergyStatus)
                   : throw new ArgumentException("The given object is not an EVSE energy status!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EVSEEnergyStatus)

        /// <summary>
        /// Compares two EVSE energy status.
        /// </summary>
        /// <param name="EVSEEnergyStatus">An EVSE energy status to compare with.</param>
        public Int32 CompareTo(EVSEEnergyStatus EVSEEnergyStatus)
        {

            var c = Id.        CompareTo(EVSEEnergyStatus.Id);

            if (c == 0)
                c = EnergyInfo.CompareTo(EVSEEnergyStatus.EnergyInfo);

            if (c == 0)
                c = Timestamp. ToUnixTimeMilliseconds().CompareTo(EVSEEnergyStatus.Timestamp.ToUnixTimeMilliseconds());

            if (c == 0 && Context is not null && EVSEEnergyStatus.Context is not null)
                c = Context.   CompareTo(EVSEEnergyStatus.Context);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<EVSEEnergyStatus> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSE energy status for equality.
        /// </summary>
        /// <param name="Object">An EVSE energy status to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSEEnergyStatus evseEnergyStatus &&
                   Equals(evseEnergyStatus);

        #endregion

        #region Equals(EVSEEnergyStatus)

        /// <summary>
        /// Compares two EVSE energy status for equality.
        /// </summary>
        /// <param name="EVSEEnergyStatus">An EVSE energy status to compare with.</param>
        public Boolean Equals(EVSEEnergyStatus EVSEEnergyStatus)

            => Id.        Equals(EVSEEnergyStatus.Id)         &&
               EnergyInfo.Equals(EVSEEnergyStatus.EnergyInfo) &&
               Timestamp. ToUnixTimeMilliseconds().Equals(EVSEEnergyStatus.Timestamp.ToUnixTimeMilliseconds())  &&

             ((Context is null     && EVSEEnergyStatus.Context is null) ||
              (Context is not null && EVSEEnergyStatus.Context is not null && Context.Equals(EVSEEnergyStatus.Context)));

        #endregion

        #endregion

        #region (override) GetHashCode()

        private readonly Int32 hashCode;

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => hashCode;

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Id} -> '{EnergyInfo}' since {Timestamp.ToISO8601()}";

        #endregion

    }

}
