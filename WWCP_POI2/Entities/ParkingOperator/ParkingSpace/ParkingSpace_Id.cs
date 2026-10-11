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
    /// Extension methods for parking space identifications.
    /// </summary>
    public static class ParkingSpaceIdExtensions
    {

        /// <summary>
        /// Indicates whether this parking space identification is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceId">A parking space identification.</param>
        public static Boolean IsNullOrEmpty(this ParkingSpace_Id? ParkingSpaceId)
            => !ParkingSpaceId.HasValue || ParkingSpaceId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking space identification is NOT null or empty.
        /// </summary>
        /// <param name="ParkingSpaceId">A parking space identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSpace_Id? ParkingSpaceId)
            => ParkingSpaceId.HasValue && ParkingSpaceId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a parking space.
    /// </summary>
    public readonly struct ParkingSpace_Id : IId<ParkingSpace_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this parking space identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this parking space identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the parking space identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a parking space identification.</param>
        private ParkingSpace_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique parking space identification (UUIDv7).
        /// </summary>
        public static ParkingSpace_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random parking space identification.
        /// </summary>
        /// <param name="Length">The expected length of the parking space identification.</param>
        public static ParkingSpace_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a parking space identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space identification.</param>
        public static ParkingSpace_Id Parse(String Text)
        {

            if (TryParse(Text, out var parkingSpaceId))
                return parkingSpaceId;

            throw new ArgumentException($"Invalid text representation of a parking space identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parking space identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space identification.</param>
        public static ParkingSpace_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var parkingSpaceId))
                return parkingSpaceId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSpaceId)

        /// <summary>
        /// Try to parse the given text as a parking space identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space identification.</param>
        /// <param name="ParkingSpaceId">The parsed parking space identification.</param>
        public static Boolean TryParse(String Text, out ParkingSpace_Id ParkingSpaceId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSpaceId = new ParkingSpace_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSpaceId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking space identification.
        /// </summary>
        public ParkingSpace_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSpaceId1, ParkingSpaceId2)

        /// <summary>
        /// Compares two parking space identifications for equality.
        /// </summary>
        /// <param name="ParkingSpaceId1">A parking space identification.</param>
        /// <param name="ParkingSpaceId2">Another parking space identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSpace_Id ParkingSpaceId1,
                                           ParkingSpace_Id ParkingSpaceId2)

            => ParkingSpaceId1.Equals(ParkingSpaceId2);

        #endregion

        #region Operator != (ParkingSpaceId1, ParkingSpaceId2)

        /// <summary>
        /// Compares two parking space identifications for inequality.
        /// </summary>
        /// <param name="ParkingSpaceId1">A parking space identification.</param>
        /// <param name="ParkingSpaceId2">Another parking space identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSpace_Id ParkingSpaceId1,
                                           ParkingSpace_Id ParkingSpaceId2)

            => !ParkingSpaceId1.Equals(ParkingSpaceId2);

        #endregion

        #region Operator <  (ParkingSpaceId1, ParkingSpaceId2)

        /// <summary>
        /// Compares two parking space identifications.
        /// </summary>
        /// <param name="ParkingSpaceId1">A parking space identification.</param>
        /// <param name="ParkingSpaceId2">Another parking space identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSpace_Id ParkingSpaceId1,
                                          ParkingSpace_Id ParkingSpaceId2)

            => ParkingSpaceId1.CompareTo(ParkingSpaceId2) < 0;

        #endregion

        #region Operator <= (ParkingSpaceId1, ParkingSpaceId2)

        /// <summary>
        /// Compares two parking space identifications.
        /// </summary>
        /// <param name="ParkingSpaceId1">A parking space identification.</param>
        /// <param name="ParkingSpaceId2">Another parking space identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSpace_Id ParkingSpaceId1,
                                           ParkingSpace_Id ParkingSpaceId2)

            => ParkingSpaceId1.CompareTo(ParkingSpaceId2) <= 0;

        #endregion

        #region Operator >  (ParkingSpaceId1, ParkingSpaceId2)

        /// <summary>
        /// Compares two parking space identifications.
        /// </summary>
        /// <param name="ParkingSpaceId1">A parking space identification.</param>
        /// <param name="ParkingSpaceId2">Another parking space identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSpace_Id ParkingSpaceId1,
                                          ParkingSpace_Id ParkingSpaceId2)

            => ParkingSpaceId1.CompareTo(ParkingSpaceId2) > 0;

        #endregion

        #region Operator >= (ParkingSpaceId1, ParkingSpaceId2)

        /// <summary>
        /// Compares two parking space identifications.
        /// </summary>
        /// <param name="ParkingSpaceId1">A parking space identification.</param>
        /// <param name="ParkingSpaceId2">Another parking space identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSpace_Id ParkingSpaceId1,
                                           ParkingSpace_Id ParkingSpaceId2)

            => ParkingSpaceId1.CompareTo(ParkingSpaceId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSpaceId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking space identifications.
        /// </summary>
        /// <param name="Object">A parking space identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpace_Id parkingSpaceId
                   ? CompareTo(parkingSpaceId)
                   : throw new ArgumentException("The given object is not a parking space identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSpaceId)

        /// <summary>
        /// Compares two parking space identifications.
        /// </summary>
        /// <param name="ParkingSpaceId">A parking space identification to compare with.</param>
        public Int32 CompareTo(ParkingSpace_Id ParkingSpaceId)

            => String.Compare(InternalId,
                              ParkingSpaceId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSpaceId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking space identifications for equality.
        /// </summary>
        /// <param name="Object">A parking space identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpace_Id parkingSpaceId &&
                   Equals(parkingSpaceId);

        #endregion

        #region Equals(ParkingSpaceId)

        /// <summary>
        /// Compares two parking space identifications for equality.
        /// </summary>
        /// <param name="ParkingSpaceId">A parking space identification to compare with.</param>
        public Boolean Equals(ParkingSpace_Id ParkingSpaceId)

            => String.Equals(InternalId,
                             ParkingSpaceId.InternalId,
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
