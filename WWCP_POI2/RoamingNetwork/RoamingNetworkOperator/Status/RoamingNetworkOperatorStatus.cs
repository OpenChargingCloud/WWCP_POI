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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Extension methods for the roaming network operator status.
    /// </summary>
    public static class RoamingNetworkOperatorStatusExtensions
    {

        #region ToJSON(this RoamingNetworkOperatorStatus, Skip = null, Take = null)

        public static JObject ToJSON(this IEnumerable<RoamingNetworkOperatorStatus>  RoamingNetworkOperatorStatus,
                                     UInt64?                                          Skip   = null,
                                     UInt64?                                          Take   = null)
        {

            #region Initial checks

            if (RoamingNetworkOperatorStatus is null || !RoamingNetworkOperatorStatus.Any())
                return new JObject();

            #endregion

            #region Maybe there are duplicate roaming network operator identifications in the enumeration... take the newest one!

            var filteredStatus = new Dictionary<RoamingNetworkOperator_Id, RoamingNetworkOperatorStatus>();

            foreach (var status in RoamingNetworkOperatorStatus)
            {

                if (!filteredStatus.ContainsKey(status.Id))
                    filteredStatus.Add(status.Id, status);

                else if (filteredStatus[status.Id].Timestamp >= status.Timestamp)
                    filteredStatus[status.Id] = status;

            }

            #endregion


            return new JObject((Take.HasValue ? filteredStatus.OrderBy(status => status.Key).Skip(Skip).Take(Take)
                                              : filteredStatus.OrderBy(status => status.Key).Skip(Skip)).

                                   Select(kvp => new JProperty(kvp.Key.ToString(),
                                                               new JArray(kvp.Value.Timestamp.  ToISO8601(),
                                                                          kvp.Value.Status.ToString())
                                                              )));

        }

        #endregion

        #region Contains(this RoamingNetworkOperatorStatus, Id, Status)

        /// <summary>
        /// Check if the given enumeration of roaming network operators and their current status
        /// contains the given pair of roaming network operator identification and status.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus">An enumeration of roaming network operators and their current status.</param>
        /// <param name="Id">A roaming network operator identification.</param>
        /// <param name="Status">A roaming network operator status.</param>
        public static Boolean Contains(this IEnumerable<RoamingNetworkOperatorStatus>  RoamingNetworkOperatorStatus,
                                       RoamingNetworkOperator_Id                       Id,
                                       RoamingNetworkOperatorStatusType                Status)
        {

            foreach (var status in RoamingNetworkOperatorStatus)
            {
                if (status.Id     == Id &&
                    status.Status == Status)
                {
                    return true;
                }
            }

            return false;

        }

        #endregion

    }


    /// <summary>
    /// The current status of a roaming network operator.
    /// </summary>
    public class RoamingNetworkOperatorStatus : AInternalData,
                                                 IEquatable <RoamingNetworkOperatorStatus>,
                                                 IComparable<RoamingNetworkOperatorStatus>
    {

        #region Properties

        /// <summary>
        /// The unique identification of the roaming network operator.
        /// </summary>
        public RoamingNetworkOperator_Id          Id           { get; }

        /// <summary>
        /// The current status of the roaming network operator.
        /// </summary>
        public RoamingNetworkOperatorStatusType   Status       { get; }

        /// <summary>
        /// The timestamp of the current status of the roaming network operator.
        /// </summary>
        public DateTimeOffset           Timestamp    { get; }

        /// <summary>
        /// The timestamped status of the roaming network operator.
        /// </summary>
        public Timestamped<RoamingNetworkOperatorStatusType> TimestampedStatus
            => new (Timestamp, Status);

        #endregion

        #region Constructor(s)

        #region RoamingNetworkOperatorStatus(Id, Status,            CustomData = null, InternalData = null)

        /// <summary>
        /// Create a new roaming network operator status.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network operator.</param>
        /// <param name="Status">The current timestamped status of the roaming network operator.</param>
        /// <param name="CustomData">An optional dictionary of customer-specific data.</param>
        public RoamingNetworkOperatorStatus(RoamingNetworkOperator_Id                       Id,
                                  Timestamped<RoamingNetworkOperatorStatusType>   Status,
                                  CustomDataNew?                        CustomData     = null,
                                  UserDefinedDictionary?                InternalData   = null)

            : base(CustomData,
                   InternalData,
                   org.GraphDefined.Vanaheimr.Illias.Timestamp.Now)

        {

            this.Id         = Id;
            this.Status     = Status.Value;
            this.Timestamp  = Status.Timestamp;

        }

        #endregion

        #region RoamingNetworkOperatorStatus(Id, Status, Timestamp, CustomData = null, InternalData = null)

        /// <summary>
        /// Create a new roaming network operator status.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network operator.</param>
        /// <param name="Status">The current status of the roaming network operator.</param>
        /// <param name="Timestamp">The timestamp of the status change of the roaming network operator.</param>
        /// <param name="CustomData">An optional dictionary of customer-specific data.</param>
        public RoamingNetworkOperatorStatus(RoamingNetworkOperator_Id          Id,
                                             RoamingNetworkOperatorStatusType   Status,
                                             DateTimeOffset                      Timestamp,
                                             CustomDataNew?                      CustomData     = null,
                                             UserDefinedDictionary?              InternalData   = null)

            : base(CustomData,
                   InternalData,
                   org.GraphDefined.Vanaheimr.Illias.Timestamp.Now)

        {

            this.Id         = Id;
            this.Status     = Status;
            this.Timestamp  = Timestamp;

        }

        #endregion

        #endregion


        #region (static) Snapshot(RoamingNetworkOperator)

        ///// <summary>
        ///// Take a snapshot of the current roaming network operator status.
        ///// </summary>
        ///// <param name="RoamingNetworkOperator">A roaming network operator.</param>
        //public static RoamingNetworkOperatorStatus Snapshot(RoamingNetworkOperator RoamingNetworkOperator)

        //    => new (RoamingNetworkOperator.Id,
        //            RoamingNetworkOperator.Status);

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkOperatorStatus1, RoamingNetworkOperatorStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus1">A roaming network operator status.</param>
        /// <param name="RoamingNetworkOperatorStatus2">Another roaming network operator status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus1,
                                           RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(RoamingNetworkOperatorStatus1, RoamingNetworkOperatorStatus2))
                return true;

            // If one is null, but not both, return false.
            if (RoamingNetworkOperatorStatus1 is null || RoamingNetworkOperatorStatus2 is null)
                return false;

            return RoamingNetworkOperatorStatus1.Equals(RoamingNetworkOperatorStatus2);

        }

        #endregion

        #region Operator != (RoamingNetworkOperatorStatus1, RoamingNetworkOperatorStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus1">A roaming network operator status.</param>
        /// <param name="RoamingNetworkOperatorStatus2">Another roaming network operator status.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus1,
                                           RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus2)

            => !(RoamingNetworkOperatorStatus1 == RoamingNetworkOperatorStatus2);

        #endregion

        #region Operator <  (RoamingNetworkOperatorStatus1, RoamingNetworkOperatorStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus1">A roaming network operator status.</param>
        /// <param name="RoamingNetworkOperatorStatus2">Another roaming network operator status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus1,
                                          RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus2)
        {

            if (RoamingNetworkOperatorStatus1 is null)
                throw new ArgumentNullException(nameof(RoamingNetworkOperatorStatus1), "The given RoamingNetworkOperatorStatus1 must not be null!");

            return RoamingNetworkOperatorStatus1.CompareTo(RoamingNetworkOperatorStatus2) < 0;

        }

        #endregion

        #region Operator <= (RoamingNetworkOperatorStatus1, RoamingNetworkOperatorStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus1">A roaming network operator status.</param>
        /// <param name="RoamingNetworkOperatorStatus2">Another roaming network operator status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus1,
                                           RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus2)

            => !(RoamingNetworkOperatorStatus1 > RoamingNetworkOperatorStatus2);

        #endregion

        #region Operator >  (RoamingNetworkOperatorStatus1, RoamingNetworkOperatorStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus1">A roaming network operator status.</param>
        /// <param name="RoamingNetworkOperatorStatus2">Another roaming network operator status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus1,
                                          RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus2)
        {

            if (RoamingNetworkOperatorStatus1 is null)
                throw new ArgumentNullException(nameof(RoamingNetworkOperatorStatus1), "The given RoamingNetworkOperatorStatus1 must not be null!");

            return RoamingNetworkOperatorStatus1.CompareTo(RoamingNetworkOperatorStatus2) > 0;

        }

        #endregion

        #region Operator >= (RoamingNetworkOperatorStatus1, RoamingNetworkOperatorStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus1">A roaming network operator status.</param>
        /// <param name="RoamingNetworkOperatorStatus2">Another roaming network operator status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus1,
                                           RoamingNetworkOperatorStatus RoamingNetworkOperatorStatus2)

            => !(RoamingNetworkOperatorStatus1 < RoamingNetworkOperatorStatus2);

        #endregion

        #endregion

        #region IComparable<RoamingNetworkOperatorStatus> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperatorStatus chargingStationOperatorStatus
                   ? CompareTo(chargingStationOperatorStatus)
                   : throw new ArgumentException("The given object is not a roaming network operator status!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetworkOperatorStatus)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus">An object to compare with.</param>
        public Int32 CompareTo(RoamingNetworkOperatorStatus? RoamingNetworkOperatorStatus)
        {

            if (RoamingNetworkOperatorStatus is null)
                throw new ArgumentNullException(nameof(RoamingNetworkOperatorStatus), "The given roaming network operator status must not be null!");

            var c = Id.       CompareTo(RoamingNetworkOperatorStatus.Id);

            if (c == 0)
                c = Status.   CompareTo(RoamingNetworkOperatorStatus.Status);

            if (c == 0)
                c = Timestamp.ToUnixTimeMilliseconds().CompareTo(RoamingNetworkOperatorStatus.Timestamp.ToUnixTimeMilliseconds());

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<RoamingNetworkOperatorStatus> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperatorStatus chargingStationOperatorStatus &&
                   Equals(chargingStationOperatorStatus);

        #endregion

        #region Equals(RoamingNetworkOperatorStatus)

        /// <summary>
        /// Compares two RoamingNetworkOperator identifications for equality.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatus">A roaming network operator identification to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(RoamingNetworkOperatorStatus? RoamingNetworkOperatorStatus)

            => RoamingNetworkOperatorStatus is not null              &&
               Id.       Equals(RoamingNetworkOperatorStatus.Id)     &&
               Status.   Equals(RoamingNetworkOperatorStatus.Status) &&
               Timestamp.ToUnixTimeMilliseconds().Equals(RoamingNetworkOperatorStatus.Timestamp.ToUnixTimeMilliseconds());

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

                return Id.       GetHashCode() * 5 ^
                       Status.   GetHashCode() * 3 ^
                       Timestamp.ToUnixTimeMilliseconds().GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(Id, " -> ",
                             Status,
                             " since ",
                             Timestamp.ToISO8601());

        #endregion

    }

}
