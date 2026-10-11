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
    /// Extension methods for charging tariff admin status types.
    /// </summary>
    public static class ChargingTariffAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this charging tariff admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType">A charging tariff admin status type.</param>
        public static Boolean IsNullOrEmpty(this ChargingTariffAdminStatusType? ChargingTariffAdminStatusType)
            => !ChargingTariffAdminStatusType.HasValue || ChargingTariffAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this charging tariff admin status types is null or empty.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType">A charging tariff admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ChargingTariffAdminStatusType? ChargingTariffAdminStatusType)
            => ChargingTariffAdminStatusType.HasValue && ChargingTariffAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a charging tariff.
    /// </summary>
    public readonly struct ChargingTariffAdminStatusType : IId,
                                                                    IEquatable<ChargingTariffAdminStatusType>,
                                                                    IComparable<ChargingTariffAdminStatusType>
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
        /// The length of the charging tariff admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff admin status type based on the given string.
        /// </summary>
        private ChargingTariffAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a charging tariff admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff admin status type.</param>
        public static ChargingTariffAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ChargingTariffAdminStatusType chargingTariffAdminStatusType))
                return chargingTariffAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a charging tariff admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a charging tariff admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff admin status type.</param>
        public static ChargingTariffAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ChargingTariffAdminStatusType chargingTariffAdminStatusType))
                return chargingTariffAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChargingTariffAdminStatusType)

        /// <summary>
        /// Parse the given string as a charging tariff admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a charging tariff admin status type.</param>
        /// <param name="ChargingTariffAdminStatusType">The parsed charging tariff admin status type.</param>
        public static Boolean TryParse(String Text, out ChargingTariffAdminStatusType ChargingTariffAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChargingTariffAdminStatusType = new ChargingTariffAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ChargingTariffAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this charging tariff admin status type.
        /// </summary>
        public ChargingTariffAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the charging tariff.
        /// </summary>
        public static readonly ChargingTariffAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the charging tariff.
        /// </summary>
        public static readonly ChargingTariffAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The charging tariff is planned for the future.
        /// </summary>
        public static readonly ChargingTariffAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The charging tariff is under maintenance.
        /// </summary>
        public static readonly ChargingTariffAdminStatusType OutOfService = new ("outOfService");

        /// <summary>
        /// The charging tariff is operational.
        /// </summary>
        public static readonly ChargingTariffAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly ChargingTariffAdminStatusType InternalUse  = new ("internalUse");

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffAdminStatusType1, ChargingTariffAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType1">A charging tariff admin status type.</param>
        /// <param name="ChargingTariffAdminStatusType2">Another charging tariff admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffAdminStatusType ChargingTariffAdminStatusType1,
                                           ChargingTariffAdminStatusType ChargingTariffAdminStatusType2)

            => ChargingTariffAdminStatusType1.Equals(ChargingTariffAdminStatusType2);

        #endregion

        #region Operator != (ChargingTariffAdminStatusType1, ChargingTariffAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType1">A charging tariff admin status type.</param>
        /// <param name="ChargingTariffAdminStatusType2">Another charging tariff admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffAdminStatusType ChargingTariffAdminStatusType1,
                                           ChargingTariffAdminStatusType ChargingTariffAdminStatusType2)

            => !ChargingTariffAdminStatusType1.Equals(ChargingTariffAdminStatusType2);

        #endregion

        #region Operator <  (ChargingTariffAdminStatusType1, ChargingTariffAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType1">A charging tariff admin status type.</param>
        /// <param name="ChargingTariffAdminStatusType2">Another charging tariff admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffAdminStatusType ChargingTariffAdminStatusType1,
                                          ChargingTariffAdminStatusType ChargingTariffAdminStatusType2)

            => ChargingTariffAdminStatusType1.CompareTo(ChargingTariffAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ChargingTariffAdminStatusType1, ChargingTariffAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType1">A charging tariff admin status type.</param>
        /// <param name="ChargingTariffAdminStatusType2">Another charging tariff admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffAdminStatusType ChargingTariffAdminStatusType1,
                                           ChargingTariffAdminStatusType ChargingTariffAdminStatusType2)

            => ChargingTariffAdminStatusType1.CompareTo(ChargingTariffAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ChargingTariffAdminStatusType1, ChargingTariffAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType1">A charging tariff admin status type.</param>
        /// <param name="ChargingTariffAdminStatusType2">Another charging tariff admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffAdminStatusType ChargingTariffAdminStatusType1,
                                          ChargingTariffAdminStatusType ChargingTariffAdminStatusType2)

            => ChargingTariffAdminStatusType1.CompareTo(ChargingTariffAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ChargingTariffAdminStatusType1, ChargingTariffAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType1">A charging tariff admin status type.</param>
        /// <param name="ChargingTariffAdminStatusType2">Another charging tariff admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffAdminStatusType ChargingTariffAdminStatusType1,
                                           ChargingTariffAdminStatusType ChargingTariffAdminStatusType2)

            => ChargingTariffAdminStatusType1.CompareTo(ChargingTariffAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChargingTariffAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two charging tariff admin status types.
        /// </summary>
        /// <param name="Object">A charging tariff admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChargingTariffAdminStatusType chargingTariffAdminStatusType
                   ? CompareTo(chargingTariffAdminStatusType)
                   : throw new ArgumentException("The given object is not a charging tariff admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChargingTariffAdminStatusType)

        /// <summary>
        /// Compares two charging tariff admin status types.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType">A charging tariff admin status type to compare with.</param>
        public Int32 CompareTo(ChargingTariffAdminStatusType ChargingTariffAdminStatusType)

            => String.Compare(InternalId,
                              ChargingTariffAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChargingTariffAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two charging tariff admin status types for equality.
        /// </summary>
        /// <param name="Object">A charging tariff admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffAdminStatusType chargingTariffAdminStatusType &&
                   Equals(chargingTariffAdminStatusType);

        #endregion

        #region Equals(ChargingTariffAdminStatusType)

        /// <summary>
        /// Compares two charging tariff admin status types for equality.
        /// </summary>
        /// <param name="ChargingTariffAdminStatusType">A charging tariff admin status type to compare with.</param>
        public Boolean Equals(ChargingTariffAdminStatusType ChargingTariffAdminStatusType)

            => String.Equals(InternalId,
                             ChargingTariffAdminStatusType.InternalId,
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
