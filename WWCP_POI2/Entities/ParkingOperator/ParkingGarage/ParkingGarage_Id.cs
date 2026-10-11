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
    /// Extension methods for parking garage identifications.
    /// </summary>
    public static class ParkingGarageIdExtensions
    {

        /// <summary>
        /// Indicates whether this parking garage identification is null or empty.
        /// </summary>
        /// <param name="ParkingGarageId">A parking garage identification.</param>
        public static Boolean IsNullOrEmpty(this ParkingGarage_Id? ParkingGarageId)
            => !ParkingGarageId.HasValue || ParkingGarageId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking garage identification is NOT null or empty.
        /// </summary>
        /// <param name="ParkingGarageId">A parking garage identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingGarage_Id? ParkingGarageId)
            => ParkingGarageId.HasValue && ParkingGarageId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a parking garage.
    /// </summary>
    public readonly struct ParkingGarage_Id : IId<ParkingGarage_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this parking garage identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this parking garage identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the parking garage identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking garage identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a parking garage identification.</param>
        private ParkingGarage_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique parking garage identification (UUIDv7).
        /// </summary>
        public static ParkingGarage_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random parking garage identification.
        /// </summary>
        /// <param name="Length">The expected length of the parking garage identification.</param>
        public static ParkingGarage_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a parking garage identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage identification.</param>
        public static ParkingGarage_Id Parse(String Text)
        {

            if (TryParse(Text, out var parkingGarageId))
                return parkingGarageId;

            throw new ArgumentException($"Invalid text representation of a parking garage identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parking garage identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage identification.</param>
        public static ParkingGarage_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var parkingGarageId))
                return parkingGarageId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingGarageId)

        /// <summary>
        /// Try to parse the given text as a parking garage identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking garage identification.</param>
        /// <param name="ParkingGarageId">The parsed parking garage identification.</param>
        public static Boolean TryParse(String Text, out ParkingGarage_Id ParkingGarageId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingGarageId = new ParkingGarage_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingGarageId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking garage identification.
        /// </summary>
        public ParkingGarage_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ParkingGarageId1, ParkingGarageId2)

        /// <summary>
        /// Compares two parking garage identifications for equality.
        /// </summary>
        /// <param name="ParkingGarageId1">A parking garage identification.</param>
        /// <param name="ParkingGarageId2">Another parking garage identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingGarage_Id ParkingGarageId1,
                                           ParkingGarage_Id ParkingGarageId2)

            => ParkingGarageId1.Equals(ParkingGarageId2);

        #endregion

        #region Operator != (ParkingGarageId1, ParkingGarageId2)

        /// <summary>
        /// Compares two parking garage identifications for inequality.
        /// </summary>
        /// <param name="ParkingGarageId1">A parking garage identification.</param>
        /// <param name="ParkingGarageId2">Another parking garage identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingGarage_Id ParkingGarageId1,
                                           ParkingGarage_Id ParkingGarageId2)

            => !ParkingGarageId1.Equals(ParkingGarageId2);

        #endregion

        #region Operator <  (ParkingGarageId1, ParkingGarageId2)

        /// <summary>
        /// Compares two parking garage identifications.
        /// </summary>
        /// <param name="ParkingGarageId1">A parking garage identification.</param>
        /// <param name="ParkingGarageId2">Another parking garage identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingGarage_Id ParkingGarageId1,
                                          ParkingGarage_Id ParkingGarageId2)

            => ParkingGarageId1.CompareTo(ParkingGarageId2) < 0;

        #endregion

        #region Operator <= (ParkingGarageId1, ParkingGarageId2)

        /// <summary>
        /// Compares two parking garage identifications.
        /// </summary>
        /// <param name="ParkingGarageId1">A parking garage identification.</param>
        /// <param name="ParkingGarageId2">Another parking garage identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingGarage_Id ParkingGarageId1,
                                           ParkingGarage_Id ParkingGarageId2)

            => ParkingGarageId1.CompareTo(ParkingGarageId2) <= 0;

        #endregion

        #region Operator >  (ParkingGarageId1, ParkingGarageId2)

        /// <summary>
        /// Compares two parking garage identifications.
        /// </summary>
        /// <param name="ParkingGarageId1">A parking garage identification.</param>
        /// <param name="ParkingGarageId2">Another parking garage identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingGarage_Id ParkingGarageId1,
                                          ParkingGarage_Id ParkingGarageId2)

            => ParkingGarageId1.CompareTo(ParkingGarageId2) > 0;

        #endregion

        #region Operator >= (ParkingGarageId1, ParkingGarageId2)

        /// <summary>
        /// Compares two parking garage identifications.
        /// </summary>
        /// <param name="ParkingGarageId1">A parking garage identification.</param>
        /// <param name="ParkingGarageId2">Another parking garage identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingGarage_Id ParkingGarageId1,
                                           ParkingGarage_Id ParkingGarageId2)

            => ParkingGarageId1.CompareTo(ParkingGarageId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingGarageId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking garage identifications.
        /// </summary>
        /// <param name="Object">A parking garage identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingGarage_Id parkingGarageId
                   ? CompareTo(parkingGarageId)
                   : throw new ArgumentException("The given object is not a parking garage identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingGarageId)

        /// <summary>
        /// Compares two parking garage identifications.
        /// </summary>
        /// <param name="ParkingGarageId">A parking garage identification to compare with.</param>
        public Int32 CompareTo(ParkingGarage_Id ParkingGarageId)

            => String.Compare(InternalId,
                              ParkingGarageId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingGarageId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking garage identifications for equality.
        /// </summary>
        /// <param name="Object">A parking garage identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingGarage_Id parkingGarageId &&
                   Equals(parkingGarageId);

        #endregion

        #region Equals(ParkingGarageId)

        /// <summary>
        /// Compares two parking garage identifications for equality.
        /// </summary>
        /// <param name="ParkingGarageId">A parking garage identification to compare with.</param>
        public Boolean Equals(ParkingGarage_Id ParkingGarageId)

            => String.Equals(InternalId,
                             ParkingGarageId.InternalId,
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
