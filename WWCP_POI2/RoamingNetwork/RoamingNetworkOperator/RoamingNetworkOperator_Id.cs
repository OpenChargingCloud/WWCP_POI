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
    /// A delegate for filtering roaming network operator identifications.
    /// </summary>
    /// <param name="RoamingNetworkOperatorId">A roaming network operator identification to include.</param>
    public delegate Boolean IncludeRoamingNetworkOperatorIdDelegate(RoamingNetworkOperator_Id RoamingNetworkOperatorId);


    /// <summary>
    /// Extension methods for roaming network operator identifications.
    /// </summary>
    public static class RoamingNetworkOperatorIdExtensions
    {

        /// <summary>
        /// Indicates whether this roaming network operator identification is null or empty.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId">A roaming network operator identification.</param>
        public static Boolean IsNullOrEmpty(this RoamingNetworkOperator_Id? RoamingNetworkOperatorId)
            => !RoamingNetworkOperatorId.HasValue || RoamingNetworkOperatorId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this roaming network operator identification is NOT null or empty.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId">A roaming network operator identification.</param>
        public static Boolean IsNotNullOrEmpty(this RoamingNetworkOperator_Id? RoamingNetworkOperatorId)
            => RoamingNetworkOperatorId.HasValue && RoamingNetworkOperatorId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a roaming network operator.
    /// </summary>
    public readonly struct RoamingNetworkOperator_Id : IId<RoamingNetworkOperator_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this roaming network operator identification is null or empty.
        /// </summary>
        public Boolean  IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this roaming network operator identification is NOT null or empty.
        /// </summary>
        public Boolean  IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the roaming network operator identification.
        /// </summary>
        public UInt64   Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network operator identification based on the given string.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator identification.</param>
        private RoamingNetworkOperator_Id(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a roaming network operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator identification.</param>
        public static RoamingNetworkOperator_Id Parse(String Text)
        {

            if (TryParse(Text, out var roamingNetworkOperatorId))
                return roamingNetworkOperatorId;

            throw new ArgumentException($"Invalid text representation of a roaming network operator identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a roaming network operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator identification.</param>
        public static RoamingNetworkOperator_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var roamingNetworkOperatorId))
                return roamingNetworkOperatorId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out RoamingNetworkOperatorId)

        /// <summary>
        /// Parse the given text as a roaming network operator identification.
        /// </summary>
        /// <param name="Text">A text representation of a roaming network operator identification.</param>
        /// <param name="RoamingNetworkOperatorId">The parsed roaming network operator identification.</param>
        public static Boolean TryParse(String Text, out RoamingNetworkOperator_Id RoamingNetworkOperatorId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                RoamingNetworkOperatorId = new RoamingNetworkOperator_Id(Text);
                return true;
            }

            RoamingNetworkOperatorId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this roaming network operator identification.
        /// </summary>
        public RoamingNetworkOperator_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (RoamingNetworkOperatorId1, RoamingNetworkOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId1">A roaming network operator identification.</param>
        /// <param name="RoamingNetworkOperatorId2">Another roaming network operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (RoamingNetworkOperator_Id RoamingNetworkOperatorId1,
                                           RoamingNetworkOperator_Id RoamingNetworkOperatorId2)

            => RoamingNetworkOperatorId1.Equals(RoamingNetworkOperatorId2);

        #endregion

        #region Operator != (RoamingNetworkOperatorId1, RoamingNetworkOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId1">A roaming network operator identification.</param>
        /// <param name="RoamingNetworkOperatorId2">Another roaming network operator identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (RoamingNetworkOperator_Id RoamingNetworkOperatorId1,
                                           RoamingNetworkOperator_Id RoamingNetworkOperatorId2)

            => !RoamingNetworkOperatorId1.Equals(RoamingNetworkOperatorId2);

        #endregion

        #region Operator <  (RoamingNetworkOperatorId1, RoamingNetworkOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId1">A roaming network operator identification.</param>
        /// <param name="RoamingNetworkOperatorId2">Another roaming network operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (RoamingNetworkOperator_Id RoamingNetworkOperatorId1,
                                          RoamingNetworkOperator_Id RoamingNetworkOperatorId2)

            => RoamingNetworkOperatorId1.CompareTo(RoamingNetworkOperatorId2) < 0;

        #endregion

        #region Operator <= (RoamingNetworkOperatorId1, RoamingNetworkOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId1">A roaming network operator identification.</param>
        /// <param name="RoamingNetworkOperatorId2">Another roaming network operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (RoamingNetworkOperator_Id RoamingNetworkOperatorId1,
                                           RoamingNetworkOperator_Id RoamingNetworkOperatorId2)

            => RoamingNetworkOperatorId1.CompareTo(RoamingNetworkOperatorId2) <= 0;

        #endregion

        #region Operator >  (RoamingNetworkOperatorId1, RoamingNetworkOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId1">A roaming network operator identification.</param>
        /// <param name="RoamingNetworkOperatorId2">Another roaming network operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (RoamingNetworkOperator_Id RoamingNetworkOperatorId1,
                                          RoamingNetworkOperator_Id RoamingNetworkOperatorId2)

            => RoamingNetworkOperatorId1.CompareTo(RoamingNetworkOperatorId2) > 0;

        #endregion

        #region Operator >= (RoamingNetworkOperatorId1, RoamingNetworkOperatorId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId1">A roaming network operator identification.</param>
        /// <param name="RoamingNetworkOperatorId2">Another roaming network operator identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (RoamingNetworkOperator_Id RoamingNetworkOperatorId1,
                                           RoamingNetworkOperator_Id RoamingNetworkOperatorId2)

            => RoamingNetworkOperatorId1.CompareTo(RoamingNetworkOperatorId2) >= 0;

        #endregion

        #endregion

        #region IComparable<RoamingNetworkOperator_Id> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two roaming network operator identifications.
        /// </summary>
        /// <param name="Object">A roaming network operator identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperator_Id roamingNetworkOperatorId
                   ? CompareTo(roamingNetworkOperatorId)
                   : throw new ArgumentException("The given object is not a roaming network operator identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(RoamingNetworkOperatorId)

        /// <summary>
        /// Compares two roaming network operator identifications.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId">A roaming network operator identification to compare with.</param>
        public Int32 CompareTo(RoamingNetworkOperator_Id RoamingNetworkOperatorId)

            => String.Compare(InternalId,
                              RoamingNetworkOperatorId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<RoamingNetworkOperator_Id> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two roaming network operator identifications for equality.
        /// </summary>
        /// <param name="Object">A roaming network operator identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperator_Id roamingNetworkOperatorId &&
                   Equals(roamingNetworkOperatorId);

        #endregion

        #region Equals(RoamingNetworkOperatorId)

        /// <summary>
        /// Compares two roaming network operator identifications for equality.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId">A roaming network operator identification to compare with.</param>
        public Boolean Equals(RoamingNetworkOperator_Id RoamingNetworkOperatorId)

            => String.Equals(InternalId,
                             RoamingNetworkOperatorId.InternalId,
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
