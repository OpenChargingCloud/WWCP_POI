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
    /// Extension methods for conformity assessment body status typess.
    /// </summary>
    public static class ConformityAssessmentBodyStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this conformity assessment body status type is null or empty.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType">A conformity assessment body status type.</param>
        public static Boolean IsNullOrEmpty(this ConformityAssessmentBodyStatusType? ConformityAssessmentBodyStatusType)
            => !ConformityAssessmentBodyStatusType.HasValue || ConformityAssessmentBodyStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this conformity assessment body status type is null or empty.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType">A conformity assessment body status type.</param>
        public static Boolean IsNotNullOrEmpty(this ConformityAssessmentBodyStatusType? ConformityAssessmentBodyStatusType)
            => ConformityAssessmentBodyStatusType.HasValue && ConformityAssessmentBodyStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status type of a conformity assessment body.
    /// </summary>
    public readonly struct ConformityAssessmentBodyStatusType : IId,
                                                               IEquatable<ConformityAssessmentBodyStatusType>,
                                                               IComparable<ConformityAssessmentBodyStatusType>
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
        /// The length of the conformity assessment body status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new conformity assessment body status types based on the given string.
        /// </summary>
        private ConformityAssessmentBodyStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a conformity assessment body status type.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body status type.</param>
        public static ConformityAssessmentBodyStatusType Parse(String Text)
        {

            if (TryParse(Text, out ConformityAssessmentBodyStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            throw new ArgumentException($"Invalid text representation of a conformity assessment body status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a conformity assessment body status type.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body status type.</param>
        public static ConformityAssessmentBodyStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ConformityAssessmentBodyStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ConformityAssessmentBodyStatusType)

        /// <summary>
        /// Parse the given string as a conformity assessment body status type.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body status type.</param>
        /// <param name="ConformityAssessmentBodyStatusType">The parsed conformity assessment body status type.</param>
        public static Boolean TryParse(String Text, out ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ConformityAssessmentBodyStatusType = new ConformityAssessmentBodyStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ConformityAssessmentBodyStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this conformity assessment body status type.
        /// </summary>
        public ConformityAssessmentBodyStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the conformity assessment body.
        /// </summary>
        public static ConformityAssessmentBodyStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the conformity assessment body.
        /// </summary>
        public static ConformityAssessmentBodyStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// Currently no communication with the conformity assessment body is possible.
        /// </summary>
        public static ConformityAssessmentBodyStatusType  Offline             { get; }
            = new ("offline");

        /// <summary>
        /// The conformity assessment body is being deployed, not fully operational yet.
        /// </summary>
        public static ConformityAssessmentBodyStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The conformity assessment body is available.
        /// </summary>
        public static ConformityAssessmentBodyStatusType  Available           { get; }
            = new ("available");

        /// <summary>
        /// An error occurred within the conformity assessment body.
        /// </summary>
        public static ConformityAssessmentBodyStatusType  Error               { get; }
            = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (ConformityAssessmentBodyStatusType1, ConformityAssessmentBodyStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType1">A conformity assessment body status type.</param>
        /// <param name="ConformityAssessmentBodyStatusType2">Another conformity assessment body status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType1,
                                           ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType2)

            => ConformityAssessmentBodyStatusType1.Equals(ConformityAssessmentBodyStatusType2);

        #endregion

        #region Operator != (ConformityAssessmentBodyStatusType1, ConformityAssessmentBodyStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType1">A conformity assessment body status type.</param>
        /// <param name="ConformityAssessmentBodyStatusType2">Another conformity assessment body status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType1,
                                           ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType2)

            => !ConformityAssessmentBodyStatusType1.Equals(ConformityAssessmentBodyStatusType2);

        #endregion

        #region Operator <  (ConformityAssessmentBodyStatusType1, ConformityAssessmentBodyStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType1">A conformity assessment body status type.</param>
        /// <param name="ConformityAssessmentBodyStatusType2">Another conformity assessment body status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType1,
                                          ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType2)

            => ConformityAssessmentBodyStatusType1.CompareTo(ConformityAssessmentBodyStatusType2) < 0;

        #endregion

        #region Operator <= (ConformityAssessmentBodyStatusType1, ConformityAssessmentBodyStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType1">A conformity assessment body status type.</param>
        /// <param name="ConformityAssessmentBodyStatusType2">Another conformity assessment body status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType1,
                                           ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType2)

            => ConformityAssessmentBodyStatusType1.CompareTo(ConformityAssessmentBodyStatusType2) <= 0;

        #endregion

        #region Operator >  (ConformityAssessmentBodyStatusType1, ConformityAssessmentBodyStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType1">A conformity assessment body status type.</param>
        /// <param name="ConformityAssessmentBodyStatusType2">Another conformity assessment body status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType1,
                                          ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType2)

            => ConformityAssessmentBodyStatusType1.CompareTo(ConformityAssessmentBodyStatusType2) > 0;

        #endregion

        #region Operator >= (ConformityAssessmentBodyStatusType1, ConformityAssessmentBodyStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType1">A conformity assessment body status type.</param>
        /// <param name="ConformityAssessmentBodyStatusType2">Another conformity assessment body status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType1,
                                           ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType2)

            => ConformityAssessmentBodyStatusType1.CompareTo(ConformityAssessmentBodyStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ConformityAssessmentBodyStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two conformity assessment body status types.
        /// </summary>
        /// <param name="Object">A conformity assessment body status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ConformityAssessmentBodyStatusType chargingStationManufacturerStatusTypes
                   ? CompareTo(chargingStationManufacturerStatusTypes)
                   : throw new ArgumentException("The given object is not a conformity assessment body status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ConformityAssessmentBodyStatusType)

        /// <summary>
        /// Compares two conformity assessment body status types.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType">A conformity assessment body status type to compare with.</param>
        public Int32 CompareTo(ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType)

            => String.Compare(InternalId,
                              ConformityAssessmentBodyStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ConformityAssessmentBodyStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two conformity assessment body status types for equality.
        /// </summary>
        /// <param name="Object">A conformity assessment body status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ConformityAssessmentBodyStatusType chargingStationManufacturerStatusTypes &&
                   Equals(chargingStationManufacturerStatusTypes);

        #endregion

        #region Equals(ConformityAssessmentBodyStatusType)

        /// <summary>
        /// Compares two conformity assessment body status types for equality.
        /// </summary>
        /// <param name="ConformityAssessmentBodyStatusType">A conformity assessment body status type to compare with.</param>
        public Boolean Equals(ConformityAssessmentBodyStatusType ConformityAssessmentBodyStatusType)

            => String.Equals(InternalId,
                             ConformityAssessmentBodyStatusType.InternalId,
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
