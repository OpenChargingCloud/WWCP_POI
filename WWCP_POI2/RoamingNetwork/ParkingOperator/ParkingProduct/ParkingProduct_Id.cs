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
    /// Extension methods for parking product identifications.
    /// </summary>
    public static class ParkingProductIdExtensions
    {

        /// <summary>
        /// Indicates whether this parking product identification is null or empty.
        /// </summary>
        /// <param name="ParkingProductId">An parking product identification.</param>
        public static Boolean IsNullOrEmpty(this ParkingProduct_Id? ParkingProductId)
            => !ParkingProductId.HasValue || ParkingProductId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking product identification is null or empty.
        /// </summary>
        /// <param name="ParkingProductId">An parking product identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingProduct_Id? ParkingProductId)
            => ParkingProductId.HasValue && ParkingProductId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of an parking product.
    /// </summary>
    public readonly struct ParkingProduct_Id : IId<ParkingProduct_Id>
    {

        #region Data

        /// <summary>
        /// The internal parking product identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this parking product identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this parking product identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the parking product identifier.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking product identification based on the given string.
        /// </summary>
        private ParkingProduct_Id(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) NewRandom  (Length = 15)

        /// <summary>
        /// Create a new random parking product identification.
        /// </summary>
        /// <param name="Length">The expected length of the random parking product identification.</param>
        public static ParkingProduct_Id NewRandom(Byte Length = 15)

            => new (RandomExtensions.RandomString(Length).ToUpper());

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an parking product identification.
        /// </summary>
        /// <param name="Text">A text representation of an parking product identification.</param>
        public static ParkingProduct_Id Parse(String Text)
        {

            if (TryParse(Text, out ParkingProduct_Id parkingProductId))
                return parkingProductId;

            throw new ArgumentException($"Invalid text representation of an parking product identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an parking product identification.
        /// </summary>
        /// <param name="Text">A text representation of an parking product identification.</param>
        public static ParkingProduct_Id? TryParse(String? Text)
        {

            if (Text is not null && TryParse(Text, out ParkingProduct_Id parkingProductId))
                return parkingProductId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingProductId)

        /// <summary>
        /// Try to parse the given string as an parking product identification.
        /// </summary>
        /// <param name="Text">A text representation of an parking product identification.</param>
        /// <param name="ParkingProductId">The parsed parking product identification.</param>
        public static Boolean TryParse(String Text, out ParkingProduct_Id ParkingProductId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingProductId = new ParkingProduct_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingProductId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking product identification.
        /// </summary>
        public ParkingProduct_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ParkingProductId1, ParkingProductId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingProductId1">An parking product identification.</param>
        /// <param name="ParkingProductId2">Another parking product identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingProduct_Id ParkingProductId1,
                                           ParkingProduct_Id ParkingProductId2)

            => ParkingProductId1.Equals(ParkingProductId2);

        #endregion

        #region Operator != (ParkingProductId1, ParkingProductId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingProductId1">An parking product identification.</param>
        /// <param name="ParkingProductId2">Another parking product identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingProduct_Id ParkingProductId1,
                                           ParkingProduct_Id ParkingProductId2)

            => !ParkingProductId1.Equals(ParkingProductId2);

        #endregion

        #region Operator <  (ParkingProductId1, ParkingProductId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingProductId1">An parking product identification.</param>
        /// <param name="ParkingProductId2">Another parking product identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingProduct_Id ParkingProductId1,
                                          ParkingProduct_Id ParkingProductId2)

            => ParkingProductId1.CompareTo(ParkingProductId2) < 0;

        #endregion

        #region Operator <= (ParkingProductId1, ParkingProductId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingProductId1">An parking product identification.</param>
        /// <param name="ParkingProductId2">Another parking product identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingProduct_Id ParkingProductId1,
                                           ParkingProduct_Id ParkingProductId2)

            => ParkingProductId1.CompareTo(ParkingProductId2) <= 0;

        #endregion

        #region Operator >  (ParkingProductId1, ParkingProductId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingProductId1">An parking product identification.</param>
        /// <param name="ParkingProductId2">Another parking product identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingProduct_Id ParkingProductId1,
                                          ParkingProduct_Id ParkingProductId2)

            => ParkingProductId1.CompareTo(ParkingProductId2) > 0;

        #endregion

        #region Operator >= (ParkingProductId1, ParkingProductId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingProductId1">An parking product identification.</param>
        /// <param name="ParkingProductId2">Another parking product identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingProduct_Id ParkingProductId1,
                                           ParkingProduct_Id ParkingProductId2)

            => ParkingProductId1.CompareTo(ParkingProductId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingProduct_Id> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingProduct_Id parkingProductId
                   ? CompareTo(parkingProductId)
                   : throw new ArgumentException("The given object is not an parking product identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingProductId)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingProductId">An object to compare with.</param>
        public Int32 CompareTo(ParkingProduct_Id ParkingProductId)

            => String.Compare(InternalId,
                              ParkingProductId.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<ParkingProduct_Id> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)

            => Object is ParkingProduct_Id parkingProductId &&
                   Equals(parkingProductId);

        #endregion

        #region Equals(ParkingProductId)

        /// <summary>
        /// Compares two parking product identifications for equality.
        /// </summary>
        /// <param name="ParkingProductId">An parking product identification to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ParkingProduct_Id ParkingProductId)

            => String.Equals(InternalId,
                             ParkingProductId.InternalId,
                             StringComparison.Ordinal);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => InternalId is null ? 0 : StringComparer.Ordinal.GetHashCode(InternalId);

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
