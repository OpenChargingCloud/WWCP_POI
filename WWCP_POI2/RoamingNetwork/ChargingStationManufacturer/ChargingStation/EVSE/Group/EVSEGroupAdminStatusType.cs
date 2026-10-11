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
    /// Extension methods for EVSE group admin status types.
    /// </summary>
    public static class EVSEGroupAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this EVSE group admin status types is null or empty.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType">A EVSE group admin status type.</param>
        public static Boolean IsNullOrEmpty(this EVSEGroupAdminStatusType? EVSEGroupAdminStatusType)
            => !EVSEGroupAdminStatusType.HasValue || EVSEGroupAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this EVSE group admin status types is null or empty.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType">A EVSE group admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this EVSEGroupAdminStatusType? EVSEGroupAdminStatusType)
            => EVSEGroupAdminStatusType.HasValue && EVSEGroupAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of an EVSE group.
    /// </summary>
    public readonly struct EVSEGroupAdminStatusType : IId,
                                                                    IEquatable<EVSEGroupAdminStatusType>,
                                                                    IComparable<EVSEGroupAdminStatusType>
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
        /// The length of the EVSE group admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE group admin status type based on the given string.
        /// </summary>
        private EVSEGroupAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an EVSE group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE group admin status type.</param>
        public static EVSEGroupAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out EVSEGroupAdminStatusType evseGroupAdminStatusType))
                return evseGroupAdminStatusType;

            throw new ArgumentException($"Invalid text representation of an EVSE group admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an EVSE group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE group admin status type.</param>
        public static EVSEGroupAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EVSEGroupAdminStatusType evseGroupAdminStatusType))
                return evseGroupAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EVSEGroupAdminStatusType)

        /// <summary>
        /// Parse the given string as an EVSE group admin status type.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE group admin status type.</param>
        /// <param name="EVSEGroupAdminStatusType">The parsed EVSE group admin status type.</param>
        public static Boolean TryParse(String Text, out EVSEGroupAdminStatusType EVSEGroupAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EVSEGroupAdminStatusType = new EVSEGroupAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EVSEGroupAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this EVSE group admin status type.
        /// </summary>
        public EVSEGroupAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the EVSE group.
        /// </summary>
        public static readonly EVSEGroupAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the EVSE group.
        /// </summary>
        public static readonly EVSEGroupAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The EVSE group is under maintenance.
        /// </summary>
        public static readonly EVSEGroupAdminStatusType OutOfService = new ("outOfService");

        /// <summary>
        /// The EVSE group is operational.
        /// </summary>
        public static readonly EVSEGroupAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly EVSEGroupAdminStatusType InternalUse  = new ("internalUse");

        #endregion


        #region Operator overloading

        #region Operator == (EVSEGroupAdminStatusType1, EVSEGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType1">A EVSE group admin status type.</param>
        /// <param name="EVSEGroupAdminStatusType2">Another EVSE group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVSEGroupAdminStatusType EVSEGroupAdminStatusType1,
                                           EVSEGroupAdminStatusType EVSEGroupAdminStatusType2)

            => EVSEGroupAdminStatusType1.Equals(EVSEGroupAdminStatusType2);

        #endregion

        #region Operator != (EVSEGroupAdminStatusType1, EVSEGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType1">A EVSE group admin status type.</param>
        /// <param name="EVSEGroupAdminStatusType2">Another EVSE group admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVSEGroupAdminStatusType EVSEGroupAdminStatusType1,
                                           EVSEGroupAdminStatusType EVSEGroupAdminStatusType2)

            => !EVSEGroupAdminStatusType1.Equals(EVSEGroupAdminStatusType2);

        #endregion

        #region Operator <  (EVSEGroupAdminStatusType1, EVSEGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType1">A EVSE group admin status type.</param>
        /// <param name="EVSEGroupAdminStatusType2">Another EVSE group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EVSEGroupAdminStatusType EVSEGroupAdminStatusType1,
                                          EVSEGroupAdminStatusType EVSEGroupAdminStatusType2)

            => EVSEGroupAdminStatusType1.CompareTo(EVSEGroupAdminStatusType2) < 0;

        #endregion

        #region Operator <= (EVSEGroupAdminStatusType1, EVSEGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType1">A EVSE group admin status type.</param>
        /// <param name="EVSEGroupAdminStatusType2">Another EVSE group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EVSEGroupAdminStatusType EVSEGroupAdminStatusType1,
                                           EVSEGroupAdminStatusType EVSEGroupAdminStatusType2)

            => EVSEGroupAdminStatusType1.CompareTo(EVSEGroupAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (EVSEGroupAdminStatusType1, EVSEGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType1">A EVSE group admin status type.</param>
        /// <param name="EVSEGroupAdminStatusType2">Another EVSE group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EVSEGroupAdminStatusType EVSEGroupAdminStatusType1,
                                          EVSEGroupAdminStatusType EVSEGroupAdminStatusType2)

            => EVSEGroupAdminStatusType1.CompareTo(EVSEGroupAdminStatusType2) > 0;

        #endregion

        #region Operator >= (EVSEGroupAdminStatusType1, EVSEGroupAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType1">A EVSE group admin status type.</param>
        /// <param name="EVSEGroupAdminStatusType2">Another EVSE group admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EVSEGroupAdminStatusType EVSEGroupAdminStatusType1,
                                           EVSEGroupAdminStatusType EVSEGroupAdminStatusType2)

            => EVSEGroupAdminStatusType1.CompareTo(EVSEGroupAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EVSEGroupAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVSE group admin status types.
        /// </summary>
        /// <param name="Object">A EVSE group admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EVSEGroupAdminStatusType evseGroupAdminStatusType
                   ? CompareTo(evseGroupAdminStatusType)
                   : throw new ArgumentException("The given object is not an EVSE group admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EVSEGroupAdminStatusType)

        /// <summary>
        /// Compares two EVSE group admin status types.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType">A EVSE group admin status type to compare with.</param>
        public Int32 CompareTo(EVSEGroupAdminStatusType EVSEGroupAdminStatusType)

            => String.Compare(InternalId,
                              EVSEGroupAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EVSEGroupAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSE group admin status types for equality.
        /// </summary>
        /// <param name="Object">A EVSE group admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSEGroupAdminStatusType evseGroupAdminStatusType &&
                   Equals(evseGroupAdminStatusType);

        #endregion

        #region Equals(EVSEGroupAdminStatusType)

        /// <summary>
        /// Compares two EVSE group admin status types for equality.
        /// </summary>
        /// <param name="EVSEGroupAdminStatusType">A EVSE group admin status type to compare with.</param>
        public Boolean Equals(EVSEGroupAdminStatusType EVSEGroupAdminStatusType)

            => String.Equals(InternalId,
                             EVSEGroupAdminStatusType.InternalId,
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
