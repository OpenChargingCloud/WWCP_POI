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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A roaming network operator admin status update.
    /// </summary>
    public readonly struct RoamingNetworkOperatorAdminStatusUpdate : IEquatable<RoamingNetworkOperatorAdminStatusUpdate>,
                                                                      IComparable<RoamingNetworkOperatorAdminStatusUpdate>,
                                                                      IComparable
    {

        #region Properties

        /// <summary>
        /// The unique identification of the roaming network operator.
        /// </summary>
        public RoamingNetworkOperator_Id                             Id            { get; }

        /// <summary>
        /// The new timestamped admin status of the roaming network operator.
        /// </summary>
        public Timestamped<RoamingNetworkOperatorAdminStatusType>    NewStatus     { get; }

        /// <summary>
        /// The optional old timestamped admin status of the roaming network operator.
        /// </summary>
        public Timestamped<RoamingNetworkOperatorAdminStatusType>?   OldStatus     { get; }

        /// <summary>
        /// An optional data source or context for this roaming network operator admin status update.
        /// </summary>
        public String?                                                DataSource    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network operator admin status update.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network operator.</param>
        /// <param name="NewStatus">The new timestamped admin status of the roaming network operator.</param>
        /// <param name="OldStatus">The optional old timestamped admin status of the roaming network operator.</param>
        /// <param name="DataSource">An optional data source or context for the roaming network operator admin status update.</param>
        public RoamingNetworkOperatorAdminStatusUpdate(RoamingNetworkOperator_Id                             Id,
                                                        Timestamped<RoamingNetworkOperatorAdminStatusType>    NewStatus,
                                                        Timestamped<RoamingNetworkOperatorAdminStatusType>?   OldStatus    = null,
                                                        String?                                                DataSource   = null)

        {

            this.Id          = Id;
            this.NewStatus   = NewStatus;
            this.OldStatus   = OldStatus;
            this.DataSource  = DataSource;

            unchecked
            {

                hashCode = Id.         GetHashCode()       * 7 ^
                           NewStatus.  GetHashCode()       * 5 ^
                          (OldStatus?. GetHashCode() ?? 0) * 3 ^
                          (DataSource?.GetHashCode() ?? 0);

            }

        }

        #endregion


        #region (static) Snapshot(RoamingNetworkOperator, DataSource = null)

        ///// <summary>
        ///// Take a snapshot of the current roaming network operator admin status.
        ///// </summary>
        ///// <param name="RoamingNetworkOperator">A roaming network operator.</param>
        ///// <param name="DataSource">An optional data source or context for the roaming network operator admin status update.</param>
        //public static RoamingNetworkOperatorAdminStatusUpdate Snapshot(RoamingNetworkOperator  RoamingNetworkOperator,
        //                                                                String?                   DataSource   = null)

        //    => new (RoamingNetworkOperator.Id,
        //            RoamingNetworkOperator.AdminStatus,
        //            RoamingNetworkOperator.AdminStatusSchedule().Skip(1).FirstOrDefault(),
        //            DataSource);

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkOperatorAdminStatusUpdate1, RoamingNetworkOperatorAdminStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate1">A roaming network operator admin status update.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate2">Another roaming network operator admin status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate1,
                                           RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate2)

            => RoamingNetworkOperatorAdminStatusUpdate1.Equals(RoamingNetworkOperatorAdminStatusUpdate2);

        #endregion

        #region Operator != (RoamingNetworkOperatorAdminStatusUpdate1, RoamingNetworkOperatorAdminStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate1">A roaming network operator admin status update.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate2">Another roaming network operator admin status update.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate1,
                                           RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate2)

            => !RoamingNetworkOperatorAdminStatusUpdate1.Equals(RoamingNetworkOperatorAdminStatusUpdate2);

        #endregion

        #region Operator <  (RoamingNetworkOperatorAdminStatusUpdate1, RoamingNetworkOperatorAdminStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate1">A roaming network operator admin status update.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate2">Another roaming network operator admin status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate1,
                                          RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate2)

            => RoamingNetworkOperatorAdminStatusUpdate1.CompareTo(RoamingNetworkOperatorAdminStatusUpdate2) < 0;

        #endregion

        #region Operator <= (RoamingNetworkOperatorAdminStatusUpdate1, RoamingNetworkOperatorAdminStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate1">A roaming network operator admin status update.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate2">Another roaming network operator admin status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate1,
                                           RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate2)

            => RoamingNetworkOperatorAdminStatusUpdate1.CompareTo(RoamingNetworkOperatorAdminStatusUpdate2) <= 0;

        #endregion

        #region Operator >  (RoamingNetworkOperatorAdminStatusUpdate1, RoamingNetworkOperatorAdminStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate1">A roaming network operator admin status update.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate2">Another roaming network operator admin status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate1,
                                          RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate2)

            => RoamingNetworkOperatorAdminStatusUpdate1.CompareTo(RoamingNetworkOperatorAdminStatusUpdate2) > 0;

        #endregion

        #region Operator >= (RoamingNetworkOperatorAdminStatusUpdate1, RoamingNetworkOperatorAdminStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate1">A roaming network operator admin status update.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate2">Another roaming network operator admin status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate1,
                                           RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate2)

            => RoamingNetworkOperatorAdminStatusUpdate1.CompareTo(RoamingNetworkOperatorAdminStatusUpdate2) >= 0;

        #endregion

        #endregion

        #region IComparable<RoamingNetworkOperatorAdminStatusUpdate> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two roaming network operator admin status updates.
        /// </summary>
        /// <param name="Object">A roaming network operator admin status update to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperatorAdminStatusUpdate chargingStationOperatorAdminStatusUpdate
                   ? CompareTo(chargingStationOperatorAdminStatusUpdate)
                   : throw new ArgumentException("The given object is not a roaming network operator admin status update!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetworkOperatorAdminStatusUpdate)

        /// <summary>
        /// Compares two roaming network operator admin status updates.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate">A roaming network operator admin status update to compare with.</param>
        public Int32 CompareTo(RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate)
        {

            var c = Id.             CompareTo(RoamingNetworkOperatorAdminStatusUpdate.Id);

            if (c == 0)
                c = NewStatus.      CompareTo(RoamingNetworkOperatorAdminStatusUpdate.NewStatus);

            if (c == 0 && OldStatus.HasValue && RoamingNetworkOperatorAdminStatusUpdate.OldStatus.HasValue)
                c = OldStatus.Value.CompareTo(RoamingNetworkOperatorAdminStatusUpdate.OldStatus.Value);

            if (c == 0 && DataSource is not null && RoamingNetworkOperatorAdminStatusUpdate.DataSource is not null)
                c = DataSource.     CompareTo(RoamingNetworkOperatorAdminStatusUpdate.DataSource);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<RoamingNetworkOperatorAdminStatusUpdate> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two roaming network operator admin status updates for equality.
        /// </summary>
        /// <param name="Object">A roaming network operator admin status update to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperatorAdminStatusUpdate chargingStationOperatorAdminStatusUpdate &&
                   Equals(chargingStationOperatorAdminStatusUpdate);

        #endregion

        #region Equals(RoamingNetworkOperatorAdminStatusUpdate)

        /// <summary>
        /// Compares two roaming network operator admin status updates for equality.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusUpdate">A roaming network operator admin status update to compare with.</param>
        public Boolean Equals(RoamingNetworkOperatorAdminStatusUpdate RoamingNetworkOperatorAdminStatusUpdate)

            => Id.       Equals(RoamingNetworkOperatorAdminStatusUpdate.Id)        &&
               NewStatus.Equals(RoamingNetworkOperatorAdminStatusUpdate.NewStatus) &&

            ((!OldStatus.HasValue     && !RoamingNetworkOperatorAdminStatusUpdate.OldStatus.HasValue) ||
              (OldStatus.HasValue     &&  RoamingNetworkOperatorAdminStatusUpdate.OldStatus.HasValue     && OldStatus.Value.Equals(RoamingNetworkOperatorAdminStatusUpdate.OldStatus.Value))) &&

            (( DataSource is null     &&  RoamingNetworkOperatorAdminStatusUpdate.DataSource is null) ||
              (DataSource is not null &&  RoamingNetworkOperatorAdminStatusUpdate.DataSource is not null && DataSource.     Equals(RoamingNetworkOperatorAdminStatusUpdate.DataSource)));

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

            => $"{Id}: {(OldStatus.HasValue ? $"'{OldStatus.Value}' -> " : "")}'{NewStatus}'{(DataSource is not null ? $" ({DataSource})" : "")}";

        #endregion

    }

}
