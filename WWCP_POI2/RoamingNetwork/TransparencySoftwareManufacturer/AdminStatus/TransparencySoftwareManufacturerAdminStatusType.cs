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
    /// Extension methods for transparency software manufacturer admin status types.
    /// </summary>
    public static class TransparencySoftwareManufacturerAdminStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this transparency software manufacturer admin status type is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType">A transparency software manufacturer admin status type.</param>
        public static Boolean IsNullOrEmpty(this TransparencySoftwareManufacturerAdminStatusType? TransparencySoftwareManufacturerAdminStatusType)
            => !TransparencySoftwareManufacturerAdminStatusType.HasValue || TransparencySoftwareManufacturerAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this transparency software manufacturer admin status type is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType">A transparency software manufacturer admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this TransparencySoftwareManufacturerAdminStatusType? TransparencySoftwareManufacturerAdminStatusType)
            => TransparencySoftwareManufacturerAdminStatusType.HasValue && TransparencySoftwareManufacturerAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a transparency software manufacturer.
    /// </summary>
    public readonly struct TransparencySoftwareManufacturerAdminStatusType : IId,
                                                                        IEquatable<TransparencySoftwareManufacturerAdminStatusType>,
                                                                        IComparable<TransparencySoftwareManufacturerAdminStatusType>
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
        /// The length of the transparency software manufacturer admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transparency software manufacturer admin status type based on the given string.
        /// </summary>
        private TransparencySoftwareManufacturerAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a transparency software manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer admin status type.</param>
        public static TransparencySoftwareManufacturerAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out TransparencySoftwareManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            throw new ArgumentException($"Invalid text representation of a transparency software manufacturer admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a transparency software manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer admin status type.</param>
        public static TransparencySoftwareManufacturerAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out TransparencySoftwareManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out TransparencySoftwareManufacturerAdminStatusType)

        /// <summary>
        /// Parse the given string as a transparency software manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer admin status type.</param>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType">The parsed transparency software manufacturer admin status type.</param>
        public static Boolean TryParse(String Text, out TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    TransparencySoftwareManufacturerAdminStatusType = new TransparencySoftwareManufacturerAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            TransparencySoftwareManufacturerAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transparency software manufacturer admin status type.
        /// </summary>
        public TransparencySoftwareManufacturerAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the transparency software manufacturer.
        /// </summary>
        public static TransparencySoftwareManufacturerAdminStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the transparency software manufacturer.
        /// </summary>
        public static TransparencySoftwareManufacturerAdminStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// The transparency software manufacturer is planned for the future.
        /// </summary>
        public static TransparencySoftwareManufacturerAdminStatusType  Planned             { get; }
            = new ("planned");

        /// <summary>
        /// The transparency software manufacturer is being deployed, not fully operational yet.
        /// </summary>
        public static TransparencySoftwareManufacturerAdminStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The transparency software manufacturer is operational.
        /// </summary>
        public static TransparencySoftwareManufacturerAdminStatusType  Operational         { get; }
            = new ("operational");

        /// <summary>
        /// The transparency software manufacturer is for internal use only, e.g. for tests.
        /// </summary>
        public static TransparencySoftwareManufacturerAdminStatusType  InternalUse         { get; }
            = new ("internalUse");

        /// <summary>
        /// The transparency software manufacturer is under maintenance.
        /// </summary>
        public static TransparencySoftwareManufacturerAdminStatusType  OutOfService        { get; }
            = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (TransparencySoftwareManufacturerAdminStatusType1, TransparencySoftwareManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType1">A transparency software manufacturer admin status type.</param>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType2">Another transparency software manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType1,
                                           TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType2)

            => TransparencySoftwareManufacturerAdminStatusType1.Equals(TransparencySoftwareManufacturerAdminStatusType2);

        #endregion

        #region Operator != (TransparencySoftwareManufacturerAdminStatusType1, TransparencySoftwareManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType1">A transparency software manufacturer admin status type.</param>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType2">Another transparency software manufacturer admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType1,
                                           TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType2)

            => !TransparencySoftwareManufacturerAdminStatusType1.Equals(TransparencySoftwareManufacturerAdminStatusType2);

        #endregion

        #region Operator <  (TransparencySoftwareManufacturerAdminStatusType1, TransparencySoftwareManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType1">A transparency software manufacturer admin status type.</param>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType2">Another transparency software manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType1,
                                          TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType2)

            => TransparencySoftwareManufacturerAdminStatusType1.CompareTo(TransparencySoftwareManufacturerAdminStatusType2) < 0;

        #endregion

        #region Operator <= (TransparencySoftwareManufacturerAdminStatusType1, TransparencySoftwareManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType1">A transparency software manufacturer admin status type.</param>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType2">Another transparency software manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType1,
                                           TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType2)

            => TransparencySoftwareManufacturerAdminStatusType1.CompareTo(TransparencySoftwareManufacturerAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (TransparencySoftwareManufacturerAdminStatusType1, TransparencySoftwareManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType1">A transparency software manufacturer admin status type.</param>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType2">Another transparency software manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType1,
                                          TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType2)

            => TransparencySoftwareManufacturerAdminStatusType1.CompareTo(TransparencySoftwareManufacturerAdminStatusType2) > 0;

        #endregion

        #region Operator >= (TransparencySoftwareManufacturerAdminStatusType1, TransparencySoftwareManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType1">A transparency software manufacturer admin status type.</param>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType2">Another transparency software manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType1,
                                           TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType2)

            => TransparencySoftwareManufacturerAdminStatusType1.CompareTo(TransparencySoftwareManufacturerAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<TransparencySoftwareManufacturerAdminStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two transparency software manufacturer admin status types.
        /// </summary>
        /// <param name="Object">A transparency software manufacturer admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftwareManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes
                   ? CompareTo(chargingStationManufacturerAdminStatusTypes)
                   : throw new ArgumentException("The given object is not a transparency software manufacturer admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TransparencySoftwareManufacturerAdminStatusType)

        /// <summary>
        /// Compares two transparency software manufacturer admin status types.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType">A transparency software manufacturer admin status type to compare with.</param>
        public Int32 CompareTo(TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType)

            => String.Compare(InternalId,
                              TransparencySoftwareManufacturerAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<TransparencySoftwareManufacturerAdminStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two transparency software manufacturer admin status types for equality.
        /// </summary>
        /// <param name="Object">A transparency software manufacturer admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftwareManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes &&
                   Equals(chargingStationManufacturerAdminStatusTypes);

        #endregion

        #region Equals(TransparencySoftwareManufacturerAdminStatusType)

        /// <summary>
        /// Compares two transparency software manufacturer admin status types for equality.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerAdminStatusType">A transparency software manufacturer admin status type to compare with.</param>
        public Boolean Equals(TransparencySoftwareManufacturerAdminStatusType TransparencySoftwareManufacturerAdminStatusType)

            => String.Equals(InternalId,
                             TransparencySoftwareManufacturerAdminStatusType.InternalId,
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
