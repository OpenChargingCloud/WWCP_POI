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
    /// Extension methods for the roaming network operator admin status.
    /// </summary>
    public static class RoamingNetworkOperatorAdminStatusExtensions
    {

        #region ToJSON(this RoamingNetworkOperatorAdminStatus, Skip = null, Take = null)

        public static JObject ToJSON(this IEnumerable<RoamingNetworkOperatorAdminStatus>  RoamingNetworkOperatorAdminStatus,
                                     UInt64?                                               Skip   = null,
                                     UInt64?                                               Take   = null)
        {

            #region Initial checks

            if (RoamingNetworkOperatorAdminStatus is null || !RoamingNetworkOperatorAdminStatus.Any())
                return new JObject();

            #endregion

            #region Maybe there are duplicate roaming network operator identifications in the enumeration... take the newest one!

            var filteredStatus = new Dictionary<RoamingNetworkOperator_Id, RoamingNetworkOperatorAdminStatus>();

            foreach (var status in RoamingNetworkOperatorAdminStatus)
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
                                                                          kvp.Value.AdminStatus.ToString())
                                                              )));

        }

        #endregion

        #region Contains(this RoamingNetworkOperatorAdminStatus, Id, Status)

        /// <summary>
        /// Check if the given enumeration of roaming network operators and their current admin status
        /// contains the given pair of roaming network operator identification and admin status.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus">An enumeration of roaming network operators and their current admin status.</param>
        /// <param name="Id">A roaming network operator identification.</param>
        /// <param name="AdminStatus">A roaming network operator admin status.</param>
        public static Boolean Contains(this IEnumerable<RoamingNetworkOperatorAdminStatus>  RoamingNetworkOperatorAdminStatus,
                                       RoamingNetworkOperator_Id                            Id,
                                       RoamingNetworkOperatorAdminStatusType                AdminStatus)
        {

            foreach (var status in RoamingNetworkOperatorAdminStatus)
            {
                if (status.Id          == Id &&
                    status.AdminStatus == AdminStatus)
                {
                    return true;
                }
            }

            return false;

        }

        #endregion

    }


    /// <summary>
    /// The current admin status of a roaming network operator.
    /// </summary>
    public class RoamingNetworkOperatorAdminStatus : AInternalData,
                                                      IEquatable<RoamingNetworkOperatorAdminStatus>,
                                                      IComparable<RoamingNetworkOperatorAdminStatus>
    {

        #region Properties

        /// <summary>
        /// The unique identification of the roaming network operator.
        /// </summary>
        public RoamingNetworkOperator_Id               Id             { get; }

        /// <summary>
        /// The current admin status of the roaming network operator.
        /// </summary>
        public RoamingNetworkOperatorAdminStatusType   AdminStatus    { get; }

        /// <summary>
        /// The timestamp of the current admin status of the roaming network operator.
        /// </summary>
        public DateTimeOffset                Timestamp      { get; }

        /// <summary>
        /// The timestamped admin status of the roaming network operator.
        /// </summary>
        public Timestamped<RoamingNetworkOperatorAdminStatusType> TimestampedAdminStatus
            => new (Timestamp, AdminStatus);

        #endregion

        #region Constructor(s)

        #region RoamingNetworkOperatorAdminStatus(Id, AdminStatus,            CustomData = null, InternalData = null)

        /// <summary>
        /// Create a new roaming network operator admin status.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network operator.</param>
        /// <param name="AdminStatus">The current timestamped adminstatus of the roaming network operator.</param>
        /// <param name="CustomData">An optional dictionary of customer-specific data.</param>
        public RoamingNetworkOperatorAdminStatus(RoamingNetworkOperator_Id                            Id,
                                       Timestamped<RoamingNetworkOperatorAdminStatusType>   AdminStatus,
                                       CustomDataNew?                             CustomData     = null,
                                       UserDefinedDictionary?                     InternalData   = null)

            : base(CustomData,
                   InternalData,
                   org.GraphDefined.Vanaheimr.Illias.Timestamp.Now)

        {

            this.Id           = Id;
            this.AdminStatus  = AdminStatus.Value;
            this.Timestamp    = AdminStatus.Timestamp;

        }

        #endregion

        #region RoamingNetworkOperatorAdminStatus(Id, AdminStatus, Timestamp, CustomData = null, InternalData = null)

        /// <summary>
        /// Create a new roaming network operator admin status.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network operator.</param>
        /// <param name="Status">The current admin status of the roaming network operator.</param>
        /// <param name="Timestamp">The timestamp of the status change of the roaming network operator.</param>
        /// <param name="CustomData">An optional dictionary of customer-specific data.</param>
        public RoamingNetworkOperatorAdminStatus(RoamingNetworkOperator_Id               Id,
                                       RoamingNetworkOperatorAdminStatusType   AdminStatus,
                                       DateTimeOffset                Timestamp,
                                       CustomDataNew?                CustomData     = null,
                                       UserDefinedDictionary?        InternalData   = null)

            : base(CustomData,
                   InternalData,
                   org.GraphDefined.Vanaheimr.Illias.Timestamp.Now)

        {

            this.Id           = Id;
            this.AdminStatus  = AdminStatus;
            this.Timestamp    = Timestamp;

        }

        #endregion

        #endregion


        #region (static) Snapshot(RoamingNetworkOperator)

        ///// <summary>
        ///// Take a snapshot of the current roaming network operator admin status.
        ///// </summary>
        ///// <param name="RoamingNetworkOperator">A roaming network operator.</param>
        //public static RoamingNetworkOperatorAdminStatus Snapshot(RoamingNetworkOperator RoamingNetworkOperator)

        //    => new (RoamingNetworkOperator.Id,
        //            RoamingNetworkOperator.AdminStatus);

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkOperatorAdminStatus1, RoamingNetworkOperatorAdminStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus1">A roaming network operator admin status.</param>
        /// <param name="RoamingNetworkOperatorAdminStatus2">Another roaming network operator admin status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus1,
                                           RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(RoamingNetworkOperatorAdminStatus1, RoamingNetworkOperatorAdminStatus2))
                return true;

            // If one is null, but not both, return false.
            if (RoamingNetworkOperatorAdminStatus1 is null || RoamingNetworkOperatorAdminStatus2 is null)
                return false;

            return RoamingNetworkOperatorAdminStatus1.Equals(RoamingNetworkOperatorAdminStatus2);

        }

        #endregion

        #region Operator != (RoamingNetworkOperatorAdminStatus1, RoamingNetworkOperatorAdminStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus1">A roaming network operator admin status.</param>
        /// <param name="RoamingNetworkOperatorAdminStatus2">Another roaming network operator admin status.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus1,
                                           RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus2)

            => !(RoamingNetworkOperatorAdminStatus1 == RoamingNetworkOperatorAdminStatus2);

        #endregion

        #region Operator <  (RoamingNetworkOperatorAdminStatus1, RoamingNetworkOperatorAdminStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus1">A roaming network operator admin status.</param>
        /// <param name="RoamingNetworkOperatorAdminStatus2">Another roaming network operator admin status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus1,
                                          RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus2)
        {

            if (RoamingNetworkOperatorAdminStatus1 is null)
                throw new ArgumentNullException(nameof(RoamingNetworkOperatorAdminStatus1), "The given RoamingNetworkOperatorAdminStatus1 must not be null!");

            return RoamingNetworkOperatorAdminStatus1.CompareTo(RoamingNetworkOperatorAdminStatus2) < 0;

        }

        #endregion

        #region Operator <= (RoamingNetworkOperatorAdminStatus1, RoamingNetworkOperatorAdminStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus1">A roaming network operator admin status.</param>
        /// <param name="RoamingNetworkOperatorAdminStatus2">Another roaming network operator admin status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus1,
                                           RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus2)

            => !(RoamingNetworkOperatorAdminStatus1 > RoamingNetworkOperatorAdminStatus2);

        #endregion

        #region Operator >  (RoamingNetworkOperatorAdminStatus1, RoamingNetworkOperatorAdminStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus1">A roaming network operator admin status.</param>
        /// <param name="RoamingNetworkOperatorAdminStatus2">Another roaming network operator admin status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus1,
                                          RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus2)
        {

            if (RoamingNetworkOperatorAdminStatus1 is null)
                throw new ArgumentNullException(nameof(RoamingNetworkOperatorAdminStatus1), "The given RoamingNetworkOperatorAdminStatus1 must not be null!");

            return RoamingNetworkOperatorAdminStatus1.CompareTo(RoamingNetworkOperatorAdminStatus2) > 0;

        }

        #endregion

        #region Operator >= (RoamingNetworkOperatorAdminStatus1, RoamingNetworkOperatorAdminStatus2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus1">A roaming network operator admin status.</param>
        /// <param name="RoamingNetworkOperatorAdminStatus2">Another roaming network operator admin status.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus1,
                                           RoamingNetworkOperatorAdminStatus RoamingNetworkOperatorAdminStatus2)

            => !(RoamingNetworkOperatorAdminStatus1 < RoamingNetworkOperatorAdminStatus2);

        #endregion

        #endregion

        #region IComparable<RoamingNetworkOperatorAdminStatus> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperatorAdminStatus chargingStationOperatorAdminStatus
                   ? CompareTo(chargingStationOperatorAdminStatus)
                   : throw new ArgumentException("The given object is not a roaming network operator admin status!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetworkOperatorAdminStatus)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus">An object to compare with.</param>
        public Int32 CompareTo(RoamingNetworkOperatorAdminStatus? RoamingNetworkOperatorAdminStatus)
        {

            if (RoamingNetworkOperatorAdminStatus is null)
                throw new ArgumentNullException(nameof(RoamingNetworkOperatorAdminStatus), "The given roaming network operator admin status must not be null!");

            var c = Id.         CompareTo(RoamingNetworkOperatorAdminStatus.Id);

            if (c == 0)
                c = AdminStatus.CompareTo(RoamingNetworkOperatorAdminStatus.AdminStatus);

            if (c == 0)
                c = Timestamp.  ToUnixTimeMilliseconds().CompareTo(RoamingNetworkOperatorAdminStatus.Timestamp.ToUnixTimeMilliseconds());

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<RoamingNetworkOperatorAdminStatus> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperatorAdminStatus chargingStationOperatorAdminStatus &&
                   Equals(chargingStationOperatorAdminStatus);

        #endregion

        #region Equals(RoamingNetworkOperatorAdminStatus)

        /// <summary>
        /// Compares two RoamingNetworkOperator identifications for equality.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatus">A roaming network operator identification to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(RoamingNetworkOperatorAdminStatus? RoamingNetworkOperatorAdminStatus)

            => RoamingNetworkOperatorAdminStatus is not null                     &&
               Id.         Equals(RoamingNetworkOperatorAdminStatus.Id)          &&
               AdminStatus.Equals(RoamingNetworkOperatorAdminStatus.AdminStatus) &&
               Timestamp.  ToUnixTimeMilliseconds().Equals(RoamingNetworkOperatorAdminStatus.Timestamp.ToUnixTimeMilliseconds());

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

                return Id.         GetHashCode() * 5 ^
                       AdminStatus.GetHashCode() * 3 ^
                       Timestamp.  ToUnixTimeMilliseconds().GetHashCode();

            }
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => String.Concat(Id, " -> ",
                             AdminStatus,
                             " since ",
                             Timestamp.ToISO8601());

        #endregion

    }

}
