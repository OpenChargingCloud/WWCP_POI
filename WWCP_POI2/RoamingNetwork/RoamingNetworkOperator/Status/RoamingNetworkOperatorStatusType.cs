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
    /// Extension methods for roaming network operator status types.
    /// </summary>
    public static class RoamingNetworkOperatorStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this roaming network operator status types is null or empty.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType">A roaming network operator status type.</param>
        public static Boolean IsNullOrEmpty(this RoamingNetworkOperatorStatusType? RoamingNetworkOperatorStatusType)
            => !RoamingNetworkOperatorStatusType.HasValue || RoamingNetworkOperatorStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this roaming network operator status types is null or empty.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType">A roaming network operator status type.</param>
        public static Boolean IsNotNullOrEmpty(this RoamingNetworkOperatorStatusType? RoamingNetworkOperatorStatusType)
            => RoamingNetworkOperatorStatusType.HasValue && RoamingNetworkOperatorStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of a roaming network operator.
    /// </summary>
    public readonly struct RoamingNetworkOperatorStatusType : IId,
                                                               IEquatable<RoamingNetworkOperatorStatusType>,
                                                               IComparable<RoamingNetworkOperatorStatusType>
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
        /// The length of the roaming network operator status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network operator status type based on the given string.
        /// </summary>
        private RoamingNetworkOperatorStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a roaming network operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator status type.</param>
        public static RoamingNetworkOperatorStatusType Parse(String Text)
        {

            if (TryParse(Text, out RoamingNetworkOperatorStatusType roamingNetworkOperatorStatusType))
                return roamingNetworkOperatorStatusType;

            throw new ArgumentException($"Invalid text representation of a roaming network operator status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a roaming network operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator status type.</param>
        public static RoamingNetworkOperatorStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out RoamingNetworkOperatorStatusType roamingNetworkOperatorStatusType))
                return roamingNetworkOperatorStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out RoamingNetworkOperatorStatusType)

        /// <summary>
        /// Parse the given string as a roaming network operator status type.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator status type.</param>
        /// <param name="RoamingNetworkOperatorStatusType">The parsed roaming network operator status type.</param>
        public static Boolean TryParse(String Text, out RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    RoamingNetworkOperatorStatusType = new RoamingNetworkOperatorStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            RoamingNetworkOperatorStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this roaming network operator status type.
        /// </summary>
        public RoamingNetworkOperatorStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the roaming network operator.
        /// </summary>
        public static readonly RoamingNetworkOperatorStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear status of the roaming network operator.
        /// </summary>
        public static readonly RoamingNetworkOperatorStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The roaming network operator is currently offline.
        /// </summary>
        public static readonly RoamingNetworkOperatorStatusType Offline      = new ("offline");

        /// <summary>
        /// The roaming network operator is not fully operational yet.
        /// </summary>
        public static readonly RoamingNetworkOperatorStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The roaming network operator is available.
        /// </summary>
        public static readonly RoamingNetworkOperatorStatusType Available    = new ("available");

        /// <summary>
        /// A fatal error has occurred within the roaming network operator.
        /// </summary>
        public static readonly RoamingNetworkOperatorStatusType Error        = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkOperatorStatusType1, RoamingNetworkOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType1">A roaming network operator status type.</param>
        /// <param name="RoamingNetworkOperatorStatusType2">Another roaming network operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType1,
                                           RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType2)

            => RoamingNetworkOperatorStatusType1.Equals(RoamingNetworkOperatorStatusType2);

        #endregion

        #region Operator != (RoamingNetworkOperatorStatusType1, RoamingNetworkOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType1">A roaming network operator status type.</param>
        /// <param name="RoamingNetworkOperatorStatusType2">Another roaming network operator status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType1,
                                           RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType2)

            => !RoamingNetworkOperatorStatusType1.Equals(RoamingNetworkOperatorStatusType2);

        #endregion

        #region Operator <  (RoamingNetworkOperatorStatusType1, RoamingNetworkOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType1">A roaming network operator status type.</param>
        /// <param name="RoamingNetworkOperatorStatusType2">Another roaming network operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType1,
                                          RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType2)

            => RoamingNetworkOperatorStatusType1.CompareTo(RoamingNetworkOperatorStatusType2) < 0;

        #endregion

        #region Operator <= (RoamingNetworkOperatorStatusType1, RoamingNetworkOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType1">A roaming network operator status type.</param>
        /// <param name="RoamingNetworkOperatorStatusType2">Another roaming network operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType1,
                                           RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType2)

            => RoamingNetworkOperatorStatusType1.CompareTo(RoamingNetworkOperatorStatusType2) <= 0;

        #endregion

        #region Operator >  (RoamingNetworkOperatorStatusType1, RoamingNetworkOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType1">A roaming network operator status type.</param>
        /// <param name="RoamingNetworkOperatorStatusType2">Another roaming network operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType1,
                                          RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType2)

            => RoamingNetworkOperatorStatusType1.CompareTo(RoamingNetworkOperatorStatusType2) > 0;

        #endregion

        #region Operator >= (RoamingNetworkOperatorStatusType1, RoamingNetworkOperatorStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType1">A roaming network operator status type.</param>
        /// <param name="RoamingNetworkOperatorStatusType2">Another roaming network operator status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType1,
                                           RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType2)

            => RoamingNetworkOperatorStatusType1.CompareTo(RoamingNetworkOperatorStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<RoamingNetworkOperatorStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two roaming network operator status types.
        /// </summary>
        /// <param name="Object">A roaming network operator status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperatorStatusType roamingNetworkOperatorStatusType
                   ? CompareTo(roamingNetworkOperatorStatusType)
                   : throw new ArgumentException("The given object is not a roaming network operator status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetworkOperatorStatusType)

        /// <summary>
        /// Compares two roaming network operator status types.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType">A roaming network operator status type to compare with.</param>
        public Int32 CompareTo(RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType)

            => String.Compare(InternalId,
                              RoamingNetworkOperatorStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<RoamingNetworkOperatorStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two roaming network operator status types for equality.
        /// </summary>
        /// <param name="Object">A roaming network operator status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperatorStatusType roamingNetworkOperatorStatusType &&
                   Equals(roamingNetworkOperatorStatusType);

        #endregion

        #region Equals(RoamingNetworkOperatorStatusType)

        /// <summary>
        /// Compares two roaming network operator status types for equality.
        /// </summary>
        /// <param name="RoamingNetworkOperatorStatusType">A roaming network operator status type to compare with.</param>
        public Boolean Equals(RoamingNetworkOperatorStatusType RoamingNetworkOperatorStatusType)

            => String.Equals(InternalId,
                             RoamingNetworkOperatorStatusType.InternalId,
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
