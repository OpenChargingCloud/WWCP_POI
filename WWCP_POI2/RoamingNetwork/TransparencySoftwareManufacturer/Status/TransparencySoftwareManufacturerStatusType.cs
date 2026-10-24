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
    /// Extension methods for transparency software manufacturer status typess.
    /// </summary>
    public static class TransparencySoftwareManufacturerStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this transparency software manufacturer status type is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType">A transparency software manufacturer status type.</param>
        public static Boolean IsNullOrEmpty(this TransparencySoftwareManufacturerStatusType? TransparencySoftwareManufacturerStatusType)
            => !TransparencySoftwareManufacturerStatusType.HasValue || TransparencySoftwareManufacturerStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this transparency software manufacturer status type is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType">A transparency software manufacturer status type.</param>
        public static Boolean IsNotNullOrEmpty(this TransparencySoftwareManufacturerStatusType? TransparencySoftwareManufacturerStatusType)
            => TransparencySoftwareManufacturerStatusType.HasValue && TransparencySoftwareManufacturerStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status type of a transparency software manufacturer.
    /// </summary>
    public readonly struct TransparencySoftwareManufacturerStatusType : IId,
                                                               IEquatable<TransparencySoftwareManufacturerStatusType>,
                                                               IComparable<TransparencySoftwareManufacturerStatusType>
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
        /// The length of the transparency software manufacturer status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transparency software manufacturer status types based on the given string.
        /// </summary>
        private TransparencySoftwareManufacturerStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a transparency software manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer status type.</param>
        public static TransparencySoftwareManufacturerStatusType Parse(String Text)
        {

            if (TryParse(Text, out TransparencySoftwareManufacturerStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            throw new ArgumentException($"Invalid text representation of a transparency software manufacturer status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a transparency software manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer status type.</param>
        public static TransparencySoftwareManufacturerStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out TransparencySoftwareManufacturerStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out TransparencySoftwareManufacturerStatusType)

        /// <summary>
        /// Parse the given string as a transparency software manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer status type.</param>
        /// <param name="TransparencySoftwareManufacturerStatusType">The parsed transparency software manufacturer status type.</param>
        public static Boolean TryParse(String Text, out TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    TransparencySoftwareManufacturerStatusType = new TransparencySoftwareManufacturerStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            TransparencySoftwareManufacturerStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transparency software manufacturer status type.
        /// </summary>
        public TransparencySoftwareManufacturerStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the transparency software manufacturer.
        /// </summary>
        public static TransparencySoftwareManufacturerStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the transparency software manufacturer.
        /// </summary>
        public static TransparencySoftwareManufacturerStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// Currently no communication with the transparency software manufacturer is possible.
        /// </summary>
        public static TransparencySoftwareManufacturerStatusType  Offline             { get; }
            = new ("offline");

        /// <summary>
        /// The transparency software manufacturer is being deployed, not fully operational yet.
        /// </summary>
        public static TransparencySoftwareManufacturerStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The transparency software manufacturer is available.
        /// </summary>
        public static TransparencySoftwareManufacturerStatusType  Available           { get; }
            = new ("available");

        /// <summary>
        /// An error occurred within the transparency software manufacturer.
        /// </summary>
        public static TransparencySoftwareManufacturerStatusType  Error               { get; }
            = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (TransparencySoftwareManufacturerStatusType1, TransparencySoftwareManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType1">A transparency software manufacturer status type.</param>
        /// <param name="TransparencySoftwareManufacturerStatusType2">Another transparency software manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType1,
                                           TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType2)

            => TransparencySoftwareManufacturerStatusType1.Equals(TransparencySoftwareManufacturerStatusType2);

        #endregion

        #region Operator != (TransparencySoftwareManufacturerStatusType1, TransparencySoftwareManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType1">A transparency software manufacturer status type.</param>
        /// <param name="TransparencySoftwareManufacturerStatusType2">Another transparency software manufacturer status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType1,
                                           TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType2)

            => !TransparencySoftwareManufacturerStatusType1.Equals(TransparencySoftwareManufacturerStatusType2);

        #endregion

        #region Operator <  (TransparencySoftwareManufacturerStatusType1, TransparencySoftwareManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType1">A transparency software manufacturer status type.</param>
        /// <param name="TransparencySoftwareManufacturerStatusType2">Another transparency software manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType1,
                                          TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType2)

            => TransparencySoftwareManufacturerStatusType1.CompareTo(TransparencySoftwareManufacturerStatusType2) < 0;

        #endregion

        #region Operator <= (TransparencySoftwareManufacturerStatusType1, TransparencySoftwareManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType1">A transparency software manufacturer status type.</param>
        /// <param name="TransparencySoftwareManufacturerStatusType2">Another transparency software manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType1,
                                           TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType2)

            => TransparencySoftwareManufacturerStatusType1.CompareTo(TransparencySoftwareManufacturerStatusType2) <= 0;

        #endregion

        #region Operator >  (TransparencySoftwareManufacturerStatusType1, TransparencySoftwareManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType1">A transparency software manufacturer status type.</param>
        /// <param name="TransparencySoftwareManufacturerStatusType2">Another transparency software manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType1,
                                          TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType2)

            => TransparencySoftwareManufacturerStatusType1.CompareTo(TransparencySoftwareManufacturerStatusType2) > 0;

        #endregion

        #region Operator >= (TransparencySoftwareManufacturerStatusType1, TransparencySoftwareManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType1">A transparency software manufacturer status type.</param>
        /// <param name="TransparencySoftwareManufacturerStatusType2">Another transparency software manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType1,
                                           TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType2)

            => TransparencySoftwareManufacturerStatusType1.CompareTo(TransparencySoftwareManufacturerStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<TransparencySoftwareManufacturerStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two transparency software manufacturer status types.
        /// </summary>
        /// <param name="Object">A transparency software manufacturer status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftwareManufacturerStatusType chargingStationManufacturerStatusTypes
                   ? CompareTo(chargingStationManufacturerStatusTypes)
                   : throw new ArgumentException("The given object is not a transparency software manufacturer status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TransparencySoftwareManufacturerStatusType)

        /// <summary>
        /// Compares two transparency software manufacturer status types.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType">A transparency software manufacturer status type to compare with.</param>
        public Int32 CompareTo(TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType)

            => String.Compare(InternalId,
                              TransparencySoftwareManufacturerStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<TransparencySoftwareManufacturerStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two transparency software manufacturer status types for equality.
        /// </summary>
        /// <param name="Object">A transparency software manufacturer status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftwareManufacturerStatusType chargingStationManufacturerStatusTypes &&
                   Equals(chargingStationManufacturerStatusTypes);

        #endregion

        #region Equals(TransparencySoftwareManufacturerStatusType)

        /// <summary>
        /// Compares two transparency software manufacturer status types for equality.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerStatusType">A transparency software manufacturer status type to compare with.</param>
        public Boolean Equals(TransparencySoftwareManufacturerStatusType TransparencySoftwareManufacturerStatusType)

            => String.Equals(InternalId,
                             TransparencySoftwareManufacturerStatusType.InternalId,
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
