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
    /// Extension methods for conformity assessment body admin status types.
    /// </summary>
    public static class ConformityAssessmentBodyAdminStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this conformity assessment body admin status type is null or empty.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType">A conformity assessment body admin status type.</param>
        public static Boolean IsNullOrEmpty(this ConformityAssessmentBodyAdminStatusType? ConformityAssessmentBodyAdminStatusType)
            => !ConformityAssessmentBodyAdminStatusType.HasValue || ConformityAssessmentBodyAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this conformity assessment body admin status type is null or empty.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType">A conformity assessment body admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this ConformityAssessmentBodyAdminStatusType? ConformityAssessmentBodyAdminStatusType)
            => ConformityAssessmentBodyAdminStatusType.HasValue && ConformityAssessmentBodyAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a conformity assessment body.
    /// </summary>
    public readonly struct ConformityAssessmentBodyAdminStatusType : IId,
                                                                        IEquatable<ConformityAssessmentBodyAdminStatusType>,
                                                                        IComparable<ConformityAssessmentBodyAdminStatusType>
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
        /// The length of the conformity assessment body admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new conformity assessment body admin status type based on the given string.
        /// </summary>
        private ConformityAssessmentBodyAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a conformity assessment body admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body admin status type.</param>
        public static ConformityAssessmentBodyAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out ConformityAssessmentBodyAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            throw new ArgumentException($"Invalid text representation of a conformity assessment body admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a conformity assessment body admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body admin status type.</param>
        public static ConformityAssessmentBodyAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out ConformityAssessmentBodyAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ConformityAssessmentBodyAdminStatusType)

        /// <summary>
        /// Parse the given string as a conformity assessment body admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a conformity assessment body admin status type.</param>
        /// <param name="ConformityAssessmentBodyAdminStatusType">The parsed conformity assessment body admin status type.</param>
        public static Boolean TryParse(String Text, out ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ConformityAssessmentBodyAdminStatusType = new ConformityAssessmentBodyAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            ConformityAssessmentBodyAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this conformity assessment body admin status type.
        /// </summary>
        public ConformityAssessmentBodyAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the conformity assessment body.
        /// </summary>
        public static ConformityAssessmentBodyAdminStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the conformity assessment body.
        /// </summary>
        public static ConformityAssessmentBodyAdminStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// The conformity assessment body is planned for the future.
        /// </summary>
        public static ConformityAssessmentBodyAdminStatusType  Planned             { get; }
            = new ("planned");

        /// <summary>
        /// The conformity assessment body is being deployed, not fully operational yet.
        /// </summary>
        public static ConformityAssessmentBodyAdminStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The conformity assessment body is operational.
        /// </summary>
        public static ConformityAssessmentBodyAdminStatusType  Operational         { get; }
            = new ("operational");

        /// <summary>
        /// The conformity assessment body is for internal use only, e.g. for tests.
        /// </summary>
        public static ConformityAssessmentBodyAdminStatusType  InternalUse         { get; }
            = new ("internalUse");

        /// <summary>
        /// The conformity assessment body is under maintenance.
        /// </summary>
        public static ConformityAssessmentBodyAdminStatusType  OutOfService        { get; }
            = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (ConformityAssessmentBodyAdminStatusType1, ConformityAssessmentBodyAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType1">A conformity assessment body admin status type.</param>
        /// <param name="ConformityAssessmentBodyAdminStatusType2">Another conformity assessment body admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType1,
                                           ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType2)

            => ConformityAssessmentBodyAdminStatusType1.Equals(ConformityAssessmentBodyAdminStatusType2);

        #endregion

        #region Operator != (ConformityAssessmentBodyAdminStatusType1, ConformityAssessmentBodyAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType1">A conformity assessment body admin status type.</param>
        /// <param name="ConformityAssessmentBodyAdminStatusType2">Another conformity assessment body admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType1,
                                           ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType2)

            => !ConformityAssessmentBodyAdminStatusType1.Equals(ConformityAssessmentBodyAdminStatusType2);

        #endregion

        #region Operator <  (ConformityAssessmentBodyAdminStatusType1, ConformityAssessmentBodyAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType1">A conformity assessment body admin status type.</param>
        /// <param name="ConformityAssessmentBodyAdminStatusType2">Another conformity assessment body admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType1,
                                          ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType2)

            => ConformityAssessmentBodyAdminStatusType1.CompareTo(ConformityAssessmentBodyAdminStatusType2) < 0;

        #endregion

        #region Operator <= (ConformityAssessmentBodyAdminStatusType1, ConformityAssessmentBodyAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType1">A conformity assessment body admin status type.</param>
        /// <param name="ConformityAssessmentBodyAdminStatusType2">Another conformity assessment body admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType1,
                                           ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType2)

            => ConformityAssessmentBodyAdminStatusType1.CompareTo(ConformityAssessmentBodyAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (ConformityAssessmentBodyAdminStatusType1, ConformityAssessmentBodyAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType1">A conformity assessment body admin status type.</param>
        /// <param name="ConformityAssessmentBodyAdminStatusType2">Another conformity assessment body admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType1,
                                          ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType2)

            => ConformityAssessmentBodyAdminStatusType1.CompareTo(ConformityAssessmentBodyAdminStatusType2) > 0;

        #endregion

        #region Operator >= (ConformityAssessmentBodyAdminStatusType1, ConformityAssessmentBodyAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType1">A conformity assessment body admin status type.</param>
        /// <param name="ConformityAssessmentBodyAdminStatusType2">Another conformity assessment body admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType1,
                                           ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType2)

            => ConformityAssessmentBodyAdminStatusType1.CompareTo(ConformityAssessmentBodyAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<ConformityAssessmentBodyAdminStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two conformity assessment body admin status types.
        /// </summary>
        /// <param name="Object">A conformity assessment body admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ConformityAssessmentBodyAdminStatusType chargingStationManufacturerAdminStatusTypes
                   ? CompareTo(chargingStationManufacturerAdminStatusTypes)
                   : throw new ArgumentException("The given object is not a conformity assessment body admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ConformityAssessmentBodyAdminStatusType)

        /// <summary>
        /// Compares two conformity assessment body admin status types.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType">A conformity assessment body admin status type to compare with.</param>
        public Int32 CompareTo(ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType)

            => String.Compare(InternalId,
                              ConformityAssessmentBodyAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ConformityAssessmentBodyAdminStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two conformity assessment body admin status types for equality.
        /// </summary>
        /// <param name="Object">A conformity assessment body admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ConformityAssessmentBodyAdminStatusType chargingStationManufacturerAdminStatusTypes &&
                   Equals(chargingStationManufacturerAdminStatusTypes);

        #endregion

        #region Equals(ConformityAssessmentBodyAdminStatusType)

        /// <summary>
        /// Compares two conformity assessment body admin status types for equality.
        /// </summary>
        /// <param name="ConformityAssessmentBodyAdminStatusType">A conformity assessment body admin status type to compare with.</param>
        public Boolean Equals(ConformityAssessmentBodyAdminStatusType ConformityAssessmentBodyAdminStatusType)

            => String.Equals(InternalId,
                             ConformityAssessmentBodyAdminStatusType.InternalId,
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
