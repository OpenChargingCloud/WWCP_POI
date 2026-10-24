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
    /// Extension methods for transparency software manufacturer identifications.
    /// </summary>
    public static class TransparencySoftwareManufacturerIdExtensions
    {

        /// <summary>
        /// Indicates whether this transparency software manufacturer identification is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId">A transparency software manufacturer identification.</param>
        public static Boolean IsNullOrEmpty(this TransparencySoftwareManufacturer_Id? TransparencySoftwareManufacturerId)
            => !TransparencySoftwareManufacturerId.HasValue || TransparencySoftwareManufacturerId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this transparency software manufacturer identification is NOT null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId">A transparency software manufacturer identification.</param>
        public static Boolean IsNotNullOrEmpty(this TransparencySoftwareManufacturer_Id? TransparencySoftwareManufacturerId)
            => TransparencySoftwareManufacturerId.HasValue && TransparencySoftwareManufacturerId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a transparency software manufacturer.
    /// </summary>
    public readonly struct TransparencySoftwareManufacturer_Id : IId<TransparencySoftwareManufacturer_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this transparency software manufacturer identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this transparency software manufacturer identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the transparency software manufacturer identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transparency software manufacturer identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a transparency software manufacturer identification.</param>
        private TransparencySoftwareManufacturer_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random transparency software manufacturer identification.
        /// </summary>
        /// <param name="Length">The expected length of the transparency software manufacturer identification.</param>
        public static TransparencySoftwareManufacturer_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a transparency software manufacturer identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer identification.</param>
        public static TransparencySoftwareManufacturer_Id Parse(String Text)
        {

            if (TryParse(Text, out var transparencySoftwareManufacturerId))
                return transparencySoftwareManufacturerId;

            throw new ArgumentException($"Invalid text representation of a transparency software manufacturer identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a transparency software manufacturer identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer identification.</param>
        public static TransparencySoftwareManufacturer_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var transparencySoftwareManufacturerId))
                return transparencySoftwareManufacturerId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out TransparencySoftwareManufacturerId)

        /// <summary>
        /// Try to parse the given text as a transparency software manufacturer identification.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software manufacturer identification.</param>
        /// <param name="TransparencySoftwareManufacturerId">The parsed transparency software manufacturer identification.</param>
        public static Boolean TryParse(String Text, out TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    TransparencySoftwareManufacturerId = new TransparencySoftwareManufacturer_Id(Text);
                    return true;
                }
                catch
                { }
            }

            TransparencySoftwareManufacturerId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transparency software manufacturer identification.
        /// </summary>
        public TransparencySoftwareManufacturer_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (TransparencySoftwareManufacturerId1, TransparencySoftwareManufacturerId2)

        /// <summary>
        /// Compares two transparency software manufacturer identifications for equality.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId1">A transparency software manufacturer identification.</param>
        /// <param name="TransparencySoftwareManufacturerId2">Another transparency software manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId1,
                                           TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId2)

            => TransparencySoftwareManufacturerId1.Equals(TransparencySoftwareManufacturerId2);

        #endregion

        #region Operator != (TransparencySoftwareManufacturerId1, TransparencySoftwareManufacturerId2)

        /// <summary>
        /// Compares two transparency software manufacturer identifications for inequality.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId1">A transparency software manufacturer identification.</param>
        /// <param name="TransparencySoftwareManufacturerId2">Another transparency software manufacturer identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId1,
                                           TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId2)

            => !TransparencySoftwareManufacturerId1.Equals(TransparencySoftwareManufacturerId2);

        #endregion

        #region Operator <  (TransparencySoftwareManufacturerId1, TransparencySoftwareManufacturerId2)

        /// <summary>
        /// Compares two transparency software manufacturer identifications.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId1">A transparency software manufacturer identification.</param>
        /// <param name="TransparencySoftwareManufacturerId2">Another transparency software manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId1,
                                          TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId2)

            => TransparencySoftwareManufacturerId1.CompareTo(TransparencySoftwareManufacturerId2) < 0;

        #endregion

        #region Operator <= (TransparencySoftwareManufacturerId1, TransparencySoftwareManufacturerId2)

        /// <summary>
        /// Compares two transparency software manufacturer identifications.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId1">A transparency software manufacturer identification.</param>
        /// <param name="TransparencySoftwareManufacturerId2">Another transparency software manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId1,
                                           TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId2)

            => TransparencySoftwareManufacturerId1.CompareTo(TransparencySoftwareManufacturerId2) <= 0;

        #endregion

        #region Operator >  (TransparencySoftwareManufacturerId1, TransparencySoftwareManufacturerId2)

        /// <summary>
        /// Compares two transparency software manufacturer identifications.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId1">A transparency software manufacturer identification.</param>
        /// <param name="TransparencySoftwareManufacturerId2">Another transparency software manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId1,
                                          TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId2)

            => TransparencySoftwareManufacturerId1.CompareTo(TransparencySoftwareManufacturerId2) > 0;

        #endregion

        #region Operator >= (TransparencySoftwareManufacturerId1, TransparencySoftwareManufacturerId2)

        /// <summary>
        /// Compares two transparency software manufacturer identifications.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId1">A transparency software manufacturer identification.</param>
        /// <param name="TransparencySoftwareManufacturerId2">Another transparency software manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId1,
                                           TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId2)

            => TransparencySoftwareManufacturerId1.CompareTo(TransparencySoftwareManufacturerId2) >= 0;

        #endregion

        #endregion

        #region IComparable<TransparencySoftwareManufacturerId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two transparency software manufacturer identifications.
        /// </summary>
        /// <param name="Object">A transparency software manufacturer identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftwareManufacturer_Id transparencySoftwareManufacturerId
                   ? CompareTo(transparencySoftwareManufacturerId)
                   : throw new ArgumentException("The given object is not a transparency software manufacturer identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TransparencySoftwareManufacturerId)

        /// <summary>
        /// Compares two transparency software manufacturer identifications.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId">A transparency software manufacturer identification to compare with.</param>
        public Int32 CompareTo(TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId)

            => String.Compare(InternalId,
                              TransparencySoftwareManufacturerId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<TransparencySoftwareManufacturerId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two transparency software manufacturer identifications for equality.
        /// </summary>
        /// <param name="Object">A transparency software manufacturer identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftwareManufacturer_Id transparencySoftwareManufacturerId &&
                   Equals(transparencySoftwareManufacturerId);

        #endregion

        #region Equals(TransparencySoftwareManufacturerId)

        /// <summary>
        /// Compares two transparency software manufacturer identifications for equality.
        /// </summary>
        /// <param name="TransparencySoftwareManufacturerId">A transparency software manufacturer identification to compare with.</param>
        public Boolean Equals(TransparencySoftwareManufacturer_Id TransparencySoftwareManufacturerId)

            => String.Equals(InternalId,
                             TransparencySoftwareManufacturerId.InternalId,
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
