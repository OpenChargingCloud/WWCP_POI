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
    /// Extension methods for charging tariff group admin status types.
    /// </summary>
    public static class ChargingTariffGroupAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging tariff group admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType">A charging tariff group admin status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingTariffGroupAdminStatusType? ChargingTariffGroupAdminStatusType)
            => !ChargingTariffGroupAdminStatusType.HasValue || ChargingTariffGroupAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging tariff group admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType">A charging tariff group admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingTariffGroupAdminStatusType? ChargingTariffGroupAdminStatusType)
            => ChargingTariffGroupAdminStatusType.HasValue && ChargingTariffGroupAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a charging tariff group.
    /// </summary>
    public readonly struct ChargingTariffGroupAdminStatusType : IId,
                                                                    IEquatable<ChargingTariffGroupAdminStatusType>,
                                                                    IComparable<ChargingTariffGroupAdminStatusType>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the charging tariff group admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff group admin status type based on the given string.
        /// </summary>
        private ChargingTariffGroupAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging tariff group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff group admin status type.</param>
        public static ChargingTariffGroupAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingTariffGroupAdminStatusType chargingTariffGroupAdminStatusType))
                return chargingTariffGroupAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a charging tariff group admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging tariff group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff group admin status type.</param>
        public static ChargingTariffGroupAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingTariffGroupAdminStatusType chargingTariffGroupAdminStatusType))
                return chargingTariffGroupAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingTariffGroupAdminStatusType)

        /// <summary>
        /// Parse the given string as a charging tariff group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff group admin status type.</param>
        /// <param name="ChargingTariffGroupAdminStatusType">The parsed charging tariff group admin status type.</param>
        public static Boolean TryParse(String Text, out ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingTariffGroupAdminStatusType = new ChargingTariffGroupAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingTariffGroupAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff group admin status type.
        /// </summary>
        public ChargingTariffGroupAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the charging tariff group.
        /// </summary>
        public static readonly ChargingTariffGroupAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the charging tariff group.
        /// </summary>
        public static readonly ChargingTariffGroupAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The charging tariff group is planned for the future.
        /// </summary>
        public static readonly ChargingTariffGroupAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The charging tariff group is being deployed, not fully operational yet.
        /// </summary>
        public static readonly ChargingTariffGroupAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The charging tariff group is operational.
        /// </summary>
        public static readonly ChargingTariffGroupAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ChargingTariffGroupAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The charging tariff group is under maintenance.
        /// </summary>
        public static readonly ChargingTariffGroupAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffGroupAdminStatusType1, ChargingTariffGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType1">A charging tariff group admin status type.</param>
        /// <param name="ChargingTariffGroupAdminStatusType2">Another charging tariff group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType1,
                                           ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType2)

            => ChargingTariffGroupAdminStatusType1.Equals(ChargingTariffGroupAdminStatusType2);

        #endregion

        #region Operator != (ChargingTariffGroupAdminStatusType1, ChargingTariffGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType1">A charging tariff group admin status type.</param>
        /// <param name="ChargingTariffGroupAdminStatusType2">Another charging tariff group admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType1,
                                           ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType2)

            => !ChargingTariffGroupAdminStatusType1.Equals(ChargingTariffGroupAdminStatusType2);

        #endregion

        #region Operator <  (ChargingTariffGroupAdminStatusType1, ChargingTariffGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType1">A charging tariff group admin status type.</param>
        /// <param name="ChargingTariffGroupAdminStatusType2">Another charging tariff group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType1,
                                          ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType2)

            => ChargingTariffGroupAdminStatusType1.CompareTo(ChargingTariffGroupAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingTariffGroupAdminStatusType1, ChargingTariffGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType1">A charging tariff group admin status type.</param>
        /// <param name="ChargingTariffGroupAdminStatusType2">Another charging tariff group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType1,
                                           ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType2)

            => ChargingTariffGroupAdminStatusType1.CompareTo(ChargingTariffGroupAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffGroupAdminStatusType1, ChargingTariffGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType1">A charging tariff group admin status type.</param>
        /// <param name="ChargingTariffGroupAdminStatusType2">Another charging tariff group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType1,
                                          ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType2)

            => ChargingTariffGroupAdminStatusType1.CompareTo(ChargingTariffGroupAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingTariffGroupAdminStatusType1, ChargingTariffGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType1">A charging tariff group admin status type.</param>
        /// <param name="ChargingTariffGroupAdminStatusType2">Another charging tariff group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType1,
                                           ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType2)

            => ChargingTariffGroupAdminStatusType1.CompareTo(ChargingTariffGroupAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffGroupAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff group admin status types.
        /// </summary>
        /// <param name="Object">A charging tariff group admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffGroupAdminStatusType chargingTariffGroupAdminStatusType
                   ? CompareTo(chargingTariffGroupAdminStatusType)
                   : throw new ArgumentException("The given object is not a charging tariff group admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffGroupAdminStatusType)

        /// <summary>
        /// Compares two charging tariff group admin status types.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType">A charging tariff group admin status type to compare with.</param>
        public Int32 CompareTo(ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType)

            => String.Compare(InternalId,
                              ChargingTariffGroupAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingTariffGroupAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff group admin status types for equality.
        /// </summary>
        /// <param name="Object">A charging tariff group admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffGroupAdminStatusType chargingTariffGroupAdminStatusType &&
                   Equals(chargingTariffGroupAdminStatusType);

        #endregion

        #region Equals(ChargingTariffGroupAdminStatusType)

        /// <summary>
        /// Compares two charging tariff group admin status types for equality.
        /// </summary>
        /// <param name="ChargingTariffGroupAdminStatusType">A charging tariff group admin status type to compare with.</param>
        public Boolean Equals(ChargingTariffGroupAdminStatusType ChargingTariffGroupAdminStatusType)

            => String.Equals(InternalId,
                             ChargingTariffGroupAdminStatusType.InternalId,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => InternalId is null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(InternalId);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => InternalId ?? "";

        #endregion

    }

}
