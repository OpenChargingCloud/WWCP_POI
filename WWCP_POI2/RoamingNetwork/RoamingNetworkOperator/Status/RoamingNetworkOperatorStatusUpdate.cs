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
    /// A roaming network operator status update.
    /// </summary>
    public readonly struct RoamingNetworkOperatorStatusUpdate : IEquatable<RoamingNetworkOperatorStatusUpdate>,
                                                                 IComparable<RoamingNetworkOperatorStatusUpdate>,
                                                                 IComparable
    {

        #region Properties

        /// <summary>
        /// The unique identification of the roaming network operator.
        /// </summary>
        public RoamingNetworkOperator_Id                        Id            { get; }

        /// <summary>
        /// The new timestamped status of the roaming network operator.
        /// </summary>
        public Timestamped<RoamingNetworkOperatorStatusType>    NewStatus     { get; }

        /// <summary>
        /// The old timestamped status of the roaming network operator.
        /// </summary>
        public Timestamped<RoamingNetworkOperatorStatusType>?   OldStatus     { get; }

        /// <summary>
        /// An optional data source or context for this roaming network operator status update.
        /// </summary>
        public String?                                           DataSource    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network operator status update.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network operator.</param>
        /// <param name="NewStatus">The new timestamped status of the roaming network operator.</param>
        /// <param name="OldStatus">The optional old timestamped status of the roaming network operator.</param>
        /// <param name="DataSource">An optional data source or context for the roaming network operator status update.</param>
        public RoamingNetworkOperatorStatusUpdate(RoamingNetworkOperator_Id                        Id,
                                                   Timestamped<RoamingNetworkOperatorStatusType>    NewStatus,
                                                   Timestamped<RoamingNetworkOperatorStatusType>?   OldStatus    = null,
                                                   String?                                           DataSource   = null)

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
        ///// Take a snapshot of the current roaming network operator status.
        ///// </summary>
        ///// <param name="RoamingNetworkOperator">A roaming network operator.</param>
        ///// <param name="DataSource">An optional data source or context for the roaming network operator status update.</param>
        //public static RoamingNetworkOperatorStatusUpdate Snapshot(RoamingNetworkOperator  RoamingNetworkOperator,
        //                                                           String?                   DataSource   = null)

        //    => new (RoamingNetworkOperator.Id,
        //            RoamingNetworkOperator.Status,
        //            RoamingNetworkOperator.StatusSchedule().Skip(1).FirstOrDefault(),
        //            DataSource);

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkOperatorStatusUpdate1, RoamingNetworkOperatorStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate1">A roaming network operator status update.</param>
        /// <param name="RoamingNetworkOperatorStatusUpdate2">Another roaming network operator status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate1,
                                           RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate2)

            => RoamingNetworkOperatorStatusUpdate1.Equals(RoamingNetworkOperatorStatusUpdate2);

        #endregion

        #region Operator != (RoamingNetworkOperatorStatusUpdate1, RoamingNetworkOperatorStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate1">A roaming network operator status update.</param>
        /// <param name="RoamingNetworkOperatorStatusUpdate2">Another roaming network operator status update.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate1,
                                           RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate2)

            => !RoamingNetworkOperatorStatusUpdate1.Equals(RoamingNetworkOperatorStatusUpdate2);

        #endregion

        #region Operator <  (RoamingNetworkOperatorStatusUpdate1, RoamingNetworkOperatorStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate1">A roaming network operator status update.</param>
        /// <param name="RoamingNetworkOperatorStatusUpdate2">Another roaming network operator status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate1,
                                          RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate2)

            => RoamingNetworkOperatorStatusUpdate1.CompareTo(RoamingNetworkOperatorStatusUpdate2) < 0;

        #endregion

        #region Operator <= (RoamingNetworkOperatorStatusUpdate1, RoamingNetworkOperatorStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate1">A roaming network operator status update.</param>
        /// <param name="RoamingNetworkOperatorStatusUpdate2">Another roaming network operator status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate1,
                                           RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate2)

            => RoamingNetworkOperatorStatusUpdate1.CompareTo(RoamingNetworkOperatorStatusUpdate2) <= 0;

        #endregion

        #region Operator >  (RoamingNetworkOperatorStatusUpdate1, RoamingNetworkOperatorStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate1">A roaming network operator status update.</param>
        /// <param name="RoamingNetworkOperatorStatusUpdate2">Another roaming network operator status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate1,
                                          RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate2)

            => RoamingNetworkOperatorStatusUpdate1.CompareTo(RoamingNetworkOperatorStatusUpdate2) > 0;

        #endregion

        #region Operator >= (RoamingNetworkOperatorStatusUpdate1, RoamingNetworkOperatorStatusUpdate2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate1">A roaming network operator status update.</param>
        /// <param name="RoamingNetworkOperatorStatusUpdate2">Another roaming network operator status update.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate1,
                                           RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate2)

            => RoamingNetworkOperatorStatusUpdate1.CompareTo(RoamingNetworkOperatorStatusUpdate2) >= 0;

        #endregion

        #endregion

        #region IComparable<RoamingNetworkOperatorStatusUpdate> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two roaming network operator status updates.
        /// </summary>
        /// <param name="Object">A roaming network operator status update to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperatorStatusUpdate chargingStationOperatorStatusUpdate
                   ? CompareTo(chargingStationOperatorStatusUpdate)
                   : throw new ArgumentException("The given object is not a roaming network operator status update!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetworkOperatorStatusUpdate)

        /// <summary>
        /// Compares two roaming network operator status updates.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate">A roaming network operator status update to compare with.</param>
        public Int32 CompareTo(RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate)
        {

            var c = Id.             CompareTo(RoamingNetworkOperatorStatusUpdate.Id);

            if (c == 0)
                c = NewStatus.      CompareTo(RoamingNetworkOperatorStatusUpdate.NewStatus);

            if (c == 0 && OldStatus.HasValue && RoamingNetworkOperatorStatusUpdate.OldStatus.HasValue)
                c = OldStatus.Value.CompareTo(RoamingNetworkOperatorStatusUpdate.OldStatus.Value);

            if (c == 0 && DataSource is not null && RoamingNetworkOperatorStatusUpdate.DataSource is not null)
                c = DataSource.     CompareTo(RoamingNetworkOperatorStatusUpdate.DataSource);

            return c;

        }

        #endregion

        #endregion

        #region IEquatable<RoamingNetworkOperatorStatusUpdate> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two roaming network operator status updates for equality.
        /// </summary>
        /// <param name="Object">A roaming network operator status update to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperatorStatusUpdate chargingStationOperatorStatusUpdate &&
                   Equals(chargingStationOperatorStatusUpdate);

        #endregion

        #region Equals(RoamingNetworkOperatorStatusUpdate)

        /// <summary>
        /// Compares two roaming network operator status updates for equality.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusUpdate">A roaming network operator status update to compare with.</param>
        public Boolean Equals(RoamingNetworkOperatorStatusUpdate RoamingNetworkOperatorStatusUpdate)

            => Id.       Equals(RoamingNetworkOperatorStatusUpdate.Id)        &&
               NewStatus.Equals(RoamingNetworkOperatorStatusUpdate.NewStatus) &&

            ((!OldStatus.HasValue     && !RoamingNetworkOperatorStatusUpdate.OldStatus.HasValue) ||
              (OldStatus.HasValue     &&  RoamingNetworkOperatorStatusUpdate.OldStatus.HasValue     && OldStatus.Value.Equals(RoamingNetworkOperatorStatusUpdate.OldStatus.Value))) &&

            (( DataSource is null     &&  RoamingNetworkOperatorStatusUpdate.DataSource is null) ||
              (DataSource is not null &&  RoamingNetworkOperatorStatusUpdate.DataSource is not null && DataSource.     Equals(RoamingNetworkOperatorStatusUpdate.DataSource)));

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
