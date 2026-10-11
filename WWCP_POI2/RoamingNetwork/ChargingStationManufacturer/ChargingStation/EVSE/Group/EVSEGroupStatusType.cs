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
    /// Extension methods for EVSE group status types.
    /// </summary>
    public static class EVSEGroupStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this EVSE group status types is null or empty.
        /// </summary>
        /// <param name="EVSEGroupStatusType">A EVSE group status type.</param>
        public static Boolean IsNullOrEmpty(this EVSEGroupStatusType? EVSEGroupStatusType)
            => !EVSEGroupStatusType.HasValue || EVSEGroupStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this EVSE group status types is null or empty.
        /// </summary>
        /// <param name="EVSEGroupStatusType">A EVSE group status type.</param>
        public static Boolean IsNotNullOrEmpty(this EVSEGroupStatusType? EVSEGroupStatusType)
            => EVSEGroupStatusType.HasValue && EVSEGroupStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of an EVSE group.
    /// </summary>
    public readonly struct EVSEGroupStatusType : IId,
                                                               IEquatable<EVSEGroupStatusType>,
                                                               IComparable<EVSEGroupStatusType>
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
        /// The length of the EVSE group status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE group status type based on the given string.
        /// </summary>
        private EVSEGroupStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an EVSE group status type.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE group status type.</param>
        public static EVSEGroupStatusType Parse(String Text)
        {

            if (TryParse(Text, out EVSEGroupStatusType evseGroupStatusType))
                return evseGroupStatusType;

            throw new ArgumentException($"Invalid text representation of an EVSE group status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an EVSE group status type.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE group status type.</param>
        public static EVSEGroupStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EVSEGroupStatusType evseGroupStatusType))
                return evseGroupStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EVSEGroupStatusType)

        /// <summary>
        /// Parse the given string as an EVSE group status type.
        /// </summary>
        /// <param name="Text">A text representation of an EVSE group status type.</param>
        /// <param name="EVSEGroupStatusType">The parsed EVSE group status type.</param>
        public static Boolean TryParse(String Text, out EVSEGroupStatusType EVSEGroupStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EVSEGroupStatusType = new EVSEGroupStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EVSEGroupStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this EVSE group status type.
        /// </summary>
        public EVSEGroupStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the EVSE group.
        /// </summary>
        public static readonly EVSEGroupStatusType Unknown          = new ("unknown");

        /// <summary>
        /// Unclear status of the EVSE group.
        /// </summary>
        public static readonly EVSEGroupStatusType Unspecified      = new ("unspecified");

        /// <summary>
        /// The EVSE group is planned for the future.
        /// </summary>
        public static readonly EVSEGroupStatusType Planned          = new ("planned");

        /// <summary>
        /// The EVSE group is currently in deployment.
        /// </summary>
        public static readonly EVSEGroupStatusType InDeployment     = new ("inDeployment");

        /// <summary>
        /// The EVSE group is out of service.
        /// </summary>
        public static readonly EVSEGroupStatusType OutOfService     = new ("outOfService");

        /// <summary>
        /// The EVSE group is currently offline.
        /// </summary>
        public static readonly EVSEGroupStatusType Offline          = new ("offline");

        /// <summary>
        /// All members of the EVSE group are available.
        /// </summary>
        public static readonly EVSEGroupStatusType Available        = new ("available");

        /// <summary>
        /// Some members of the EVSE group are available.
        /// </summary>
        public static readonly EVSEGroupStatusType PartialAvailable = new ("partialAvailable");

        /// <summary>
        /// The members of the EVSE group are reserved.
        /// </summary>
        public static readonly EVSEGroupStatusType Reserved         = new ("reserved");

        /// <summary>
        /// The members of the EVSE group are charging.
        /// </summary>
        public static readonly EVSEGroupStatusType Charging         = new ("charging");

        /// <summary>
        /// The members of the EVSE group are faulted.
        /// </summary>
        public static readonly EVSEGroupStatusType Faulted          = new ("faulted");

        /// <summary>
        /// Another status of the EVSE group.
        /// </summary>
        public static readonly EVSEGroupStatusType Other            = new ("other");

        #endregion


        #region Operator overloading

        #region Operator == (EVSEGroupStatusType1, EVSEGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupStatusType1">A EVSE group status type.</param>
        /// <param name="EVSEGroupStatusType2">Another EVSE group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EVSEGroupStatusType EVSEGroupStatusType1,
                                           EVSEGroupStatusType EVSEGroupStatusType2)

            => EVSEGroupStatusType1.Equals(EVSEGroupStatusType2);

        #endregion

        #region Operator != (EVSEGroupStatusType1, EVSEGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupStatusType1">A EVSE group status type.</param>
        /// <param name="EVSEGroupStatusType2">Another EVSE group status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EVSEGroupStatusType EVSEGroupStatusType1,
                                           EVSEGroupStatusType EVSEGroupStatusType2)

            => !EVSEGroupStatusType1.Equals(EVSEGroupStatusType2);

        #endregion

        #region Operator <  (EVSEGroupStatusType1, EVSEGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupStatusType1">A EVSE group status type.</param>
        /// <param name="EVSEGroupStatusType2">Another EVSE group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EVSEGroupStatusType EVSEGroupStatusType1,
                                          EVSEGroupStatusType EVSEGroupStatusType2)

            => EVSEGroupStatusType1.CompareTo(EVSEGroupStatusType2) < 0;

        #endregion

        #region Operator <= (EVSEGroupStatusType1, EVSEGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupStatusType1">A EVSE group status type.</param>
        /// <param name="EVSEGroupStatusType2">Another EVSE group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EVSEGroupStatusType EVSEGroupStatusType1,
                                           EVSEGroupStatusType EVSEGroupStatusType2)

            => EVSEGroupStatusType1.CompareTo(EVSEGroupStatusType2) <= 0;

        #endregion

        #region Operator >  (EVSEGroupStatusType1, EVSEGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupStatusType1">A EVSE group status type.</param>
        /// <param name="EVSEGroupStatusType2">Another EVSE group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EVSEGroupStatusType EVSEGroupStatusType1,
                                          EVSEGroupStatusType EVSEGroupStatusType2)

            => EVSEGroupStatusType1.CompareTo(EVSEGroupStatusType2) > 0;

        #endregion

        #region Operator >= (EVSEGroupStatusType1, EVSEGroupStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSEGroupStatusType1">A EVSE group status type.</param>
        /// <param name="EVSEGroupStatusType2">Another EVSE group status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EVSEGroupStatusType EVSEGroupStatusType1,
                                           EVSEGroupStatusType EVSEGroupStatusType2)

            => EVSEGroupStatusType1.CompareTo(EVSEGroupStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EVSEGroupStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EVSE group status types.
        /// </summary>
        /// <param name="Object">A EVSE group status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EVSEGroupStatusType evseGroupStatusType
                   ? CompareTo(evseGroupStatusType)
                   : throw new ArgumentException("The given object is not an EVSE group status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EVSEGroupStatusType)

        /// <summary>
        /// Compares two EVSE group status types.
        /// </summary>
        /// <param name="EVSEGroupStatusType">A EVSE group status type to compare with.</param>
        public Int32 CompareTo(EVSEGroupStatusType EVSEGroupStatusType)

            => String.Compare(InternalId,
                              EVSEGroupStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EVSEGroupStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EVSE group status types for equality.
        /// </summary>
        /// <param name="Object">A EVSE group status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EVSEGroupStatusType evseGroupStatusType &&
                   Equals(evseGroupStatusType);

        #endregion

        #region Equals(EVSEGroupStatusType)

        /// <summary>
        /// Compares two EVSE group status types for equality.
        /// </summary>
        /// <param name="EVSEGroupStatusType">A EVSE group status type to compare with.</param>
        public Boolean Equals(EVSEGroupStatusType EVSEGroupStatusType)

            => String.Equals(InternalId,
                             EVSEGroupStatusType.InternalId,
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
