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
    /// Extension methods for roaming network operator admin status types.
    /// </summary>
    public static class RoamingNetworkOperatorAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this roaming network operator admin status types is null or empty.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType">A roaming network operator admin status type.</param>
        public static Boolean IsNullOrEmpty(this RoamingNetworkOperatorAdminStatusType? RoamingNetworkOperatorAdminStatusType)
            => !RoamingNetworkOperatorAdminStatusType.HasValue || RoamingNetworkOperatorAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this roaming network operator admin status types is null or empty.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType">A roaming network operator admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this RoamingNetworkOperatorAdminStatusType? RoamingNetworkOperatorAdminStatusType)
            => RoamingNetworkOperatorAdminStatusType.HasValue && RoamingNetworkOperatorAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a roaming network operator.
    /// </summary>
    public readonly struct RoamingNetworkOperatorAdminStatusType : IId,
                                                                    IEquatable<RoamingNetworkOperatorAdminStatusType>,
                                                                    IComparable<RoamingNetworkOperatorAdminStatusType>
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
        /// The length of the roaming network operator admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network operator admin status type based on the given string.
        /// </summary>
        private RoamingNetworkOperatorAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a roaming network operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator admin status type.</param>
        public static RoamingNetworkOperatorAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out RoamingNetworkOperatorAdminStatusType roamingNetworkOperatorAdminStatusType))
                return roamingNetworkOperatorAdminStatusType;

            throw new ArgumentException($"Invalid text representation of a roaming network operator admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a roaming network operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator admin status type.</param>
        public static RoamingNetworkOperatorAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out RoamingNetworkOperatorAdminStatusType roamingNetworkOperatorAdminStatusType))
                return roamingNetworkOperatorAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out RoamingNetworkOperatorAdminStatusType)

        /// <summary>
        /// Parse the given string as a roaming network operator admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator admin status type.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusType">The parsed roaming network operator admin status type.</param>
        public static Boolean TryParse(String Text, out RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    RoamingNetworkOperatorAdminStatusType = new RoamingNetworkOperatorAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            RoamingNetworkOperatorAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this roaming network operator admin status type.
        /// </summary>
        public RoamingNetworkOperatorAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the roaming network operator.
        /// </summary>
        public static readonly RoamingNetworkOperatorAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the roaming network operator.
        /// </summary>
        public static readonly RoamingNetworkOperatorAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The roaming network operator is planned for the future.
        /// </summary>
        public static readonly RoamingNetworkOperatorAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The roaming network operator is being deployed, not fully operational yet.
        /// </summary>
        public static readonly RoamingNetworkOperatorAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The roaming network operator is operational.
        /// </summary>
        public static readonly RoamingNetworkOperatorAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly RoamingNetworkOperatorAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The roaming network operator is under maintenance.
        /// </summary>
        public static readonly RoamingNetworkOperatorAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkOperatorAdminStatusType1, RoamingNetworkOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType1">A roaming network operator admin status type.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusType2">Another roaming network operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType1,
                                           RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType2)

            => RoamingNetworkOperatorAdminStatusType1.Equals(RoamingNetworkOperatorAdminStatusType2);

        #endregion

        #region Operator != (RoamingNetworkOperatorAdminStatusType1, RoamingNetworkOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType1">A roaming network operator admin status type.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusType2">Another roaming network operator admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType1,
                                           RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType2)

            => !RoamingNetworkOperatorAdminStatusType1.Equals(RoamingNetworkOperatorAdminStatusType2);

        #endregion

        #region Operator <  (RoamingNetworkOperatorAdminStatusType1, RoamingNetworkOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType1">A roaming network operator admin status type.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusType2">Another roaming network operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType1,
                                          RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType2)

            => RoamingNetworkOperatorAdminStatusType1.CompareTo(RoamingNetworkOperatorAdminStatusType2) < 0;

        #endregion

        #region Operator <= (RoamingNetworkOperatorAdminStatusType1, RoamingNetworkOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType1">A roaming network operator admin status type.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusType2">Another roaming network operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType1,
                                           RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType2)

            => RoamingNetworkOperatorAdminStatusType1.CompareTo(RoamingNetworkOperatorAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (RoamingNetworkOperatorAdminStatusType1, RoamingNetworkOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType1">A roaming network operator admin status type.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusType2">Another roaming network operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType1,
                                          RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType2)

            => RoamingNetworkOperatorAdminStatusType1.CompareTo(RoamingNetworkOperatorAdminStatusType2) > 0;

        #endregion

        #region Operator >= (RoamingNetworkOperatorAdminStatusType1, RoamingNetworkOperatorAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType1">A roaming network operator admin status type.</param>
        /// <param name="RoamingNetworkOperatorAdminStatusType2">Another roaming network operator admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType1,
                                           RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType2)

            => RoamingNetworkOperatorAdminStatusType1.CompareTo(RoamingNetworkOperatorAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<RoamingNetworkOperatorAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two roaming network operator admin status types.
        /// </summary>
        /// <param name="Object">A roaming network operator admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperatorAdminStatusType roamingNetworkOperatorAdminStatusType
                   ? CompareTo(roamingNetworkOperatorAdminStatusType)
                   : throw new ArgumentException("The given object is not a roaming network operator admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetworkOperatorAdminStatusType)

        /// <summary>
        /// Compares two roaming network operator admin status types.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType">A roaming network operator admin status type to compare with.</param>
        public Int32 CompareTo(RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType)

            => String.Compare(InternalId,
                              RoamingNetworkOperatorAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<RoamingNetworkOperatorAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two roaming network operator admin status types for equality.
        /// </summary>
        /// <param name="Object">A roaming network operator admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperatorAdminStatusType roamingNetworkOperatorAdminStatusType &&
                   Equals(roamingNetworkOperatorAdminStatusType);

        #endregion

        #region Equals(RoamingNetworkOperatorAdminStatusType)

        /// <summary>
        /// Compares two roaming network operator admin status types for equality.
        /// </summary>
        /// <param name="RoamingNetworkOperatorAdminStatusType">A roaming network operator admin status type to compare with.</param>
        public Boolean Equals(RoamingNetworkOperatorAdminStatusType RoamingNetworkOperatorAdminStatusType)

            => String.Equals(InternalId,
                             RoamingNetworkOperatorAdminStatusType.InternalId,
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
