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
    /// Extension methods for parking space group identifications.
    /// </summary>
    public static class ParkingSpaceGroupIdExtensions
    {

        /// <summary>
        /// Indicates whether this parking space group identification is null or empty.
        /// </summary>
        /// <param name="ParkingSpaceGroupId">A parking space group identification.</param>
        public static Boolean IsNullOrEmpty(this ParkingSpaceGroup_Id? ParkingSpaceGroupId)
            => !ParkingSpaceGroupId.HasValue || ParkingSpaceGroupId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking space group identification is NOT null or empty.
        /// </summary>
        /// <param name="ParkingSpaceGroupId">A parking space group identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSpaceGroup_Id? ParkingSpaceGroupId)
            => ParkingSpaceGroupId.HasValue && ParkingSpaceGroupId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a parking space group.
    /// </summary>
    public readonly struct ParkingSpaceGroup_Id : IId<ParkingSpaceGroup_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this parking space group identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this parking space group identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the parking space group identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space group identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a parking space group identification.</param>
        private ParkingSpaceGroup_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique parking space group identification (UUIDv7).
        /// </summary>
        public static ParkingSpaceGroup_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random parking space group identification.
        /// </summary>
        /// <param name="Length">The expected length of the parking space group identification.</param>
        public static ParkingSpaceGroup_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a parking space group identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group identification.</param>
        public static ParkingSpaceGroup_Id Parse(String Text)
        {

            if (TryParse(Text, out var parkingSpaceGroupId))
                return parkingSpaceGroupId;

            throw new ArgumentException($"Invalid text representation of a parking space group identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parking space group identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group identification.</param>
        public static ParkingSpaceGroup_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var parkingSpaceGroupId))
                return parkingSpaceGroupId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSpaceGroupId)

        /// <summary>
        /// Try to parse the given text as a parking space group identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking space group identification.</param>
        /// <param name="ParkingSpaceGroupId">The parsed parking space group identification.</param>
        public static Boolean TryParse(String Text, out ParkingSpaceGroup_Id ParkingSpaceGroupId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSpaceGroupId = new ParkingSpaceGroup_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSpaceGroupId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking space group identification.
        /// </summary>
        public ParkingSpaceGroup_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSpaceGroupId1, ParkingSpaceGroupId2)

        /// <summary>
        /// Compares two parking space group identifications for equality.
        /// </summary>
        /// <param name="ParkingSpaceGroupId1">A parking space group identification.</param>
        /// <param name="ParkingSpaceGroupId2">Another parking space group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSpaceGroup_Id ParkingSpaceGroupId1,
                                           ParkingSpaceGroup_Id ParkingSpaceGroupId2)

            => ParkingSpaceGroupId1.Equals(ParkingSpaceGroupId2);

        #endregion

        #region Operator != (ParkingSpaceGroupId1, ParkingSpaceGroupId2)

        /// <summary>
        /// Compares two parking space group identifications for inequality.
        /// </summary>
        /// <param name="ParkingSpaceGroupId1">A parking space group identification.</param>
        /// <param name="ParkingSpaceGroupId2">Another parking space group identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSpaceGroup_Id ParkingSpaceGroupId1,
                                           ParkingSpaceGroup_Id ParkingSpaceGroupId2)

            => !ParkingSpaceGroupId1.Equals(ParkingSpaceGroupId2);

        #endregion

        #region Operator <  (ParkingSpaceGroupId1, ParkingSpaceGroupId2)

        /// <summary>
        /// Compares two parking space group identifications.
        /// </summary>
        /// <param name="ParkingSpaceGroupId1">A parking space group identification.</param>
        /// <param name="ParkingSpaceGroupId2">Another parking space group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSpaceGroup_Id ParkingSpaceGroupId1,
                                          ParkingSpaceGroup_Id ParkingSpaceGroupId2)

            => ParkingSpaceGroupId1.CompareTo(ParkingSpaceGroupId2) < 0;

        #endregion

        #region Operator <= (ParkingSpaceGroupId1, ParkingSpaceGroupId2)

        /// <summary>
        /// Compares two parking space group identifications.
        /// </summary>
        /// <param name="ParkingSpaceGroupId1">A parking space group identification.</param>
        /// <param name="ParkingSpaceGroupId2">Another parking space group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSpaceGroup_Id ParkingSpaceGroupId1,
                                           ParkingSpaceGroup_Id ParkingSpaceGroupId2)

            => ParkingSpaceGroupId1.CompareTo(ParkingSpaceGroupId2) <= 0;

        #endregion

        #region Operator >  (ParkingSpaceGroupId1, ParkingSpaceGroupId2)

        /// <summary>
        /// Compares two parking space group identifications.
        /// </summary>
        /// <param name="ParkingSpaceGroupId1">A parking space group identification.</param>
        /// <param name="ParkingSpaceGroupId2">Another parking space group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSpaceGroup_Id ParkingSpaceGroupId1,
                                          ParkingSpaceGroup_Id ParkingSpaceGroupId2)

            => ParkingSpaceGroupId1.CompareTo(ParkingSpaceGroupId2) > 0;

        #endregion

        #region Operator >= (ParkingSpaceGroupId1, ParkingSpaceGroupId2)

        /// <summary>
        /// Compares two parking space group identifications.
        /// </summary>
        /// <param name="ParkingSpaceGroupId1">A parking space group identification.</param>
        /// <param name="ParkingSpaceGroupId2">Another parking space group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSpaceGroup_Id ParkingSpaceGroupId1,
                                           ParkingSpaceGroup_Id ParkingSpaceGroupId2)

            => ParkingSpaceGroupId1.CompareTo(ParkingSpaceGroupId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSpaceGroupId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking space group identifications.
        /// </summary>
        /// <param name="Object">A parking space group identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceGroup_Id parkingSpaceGroupId
                   ? CompareTo(parkingSpaceGroupId)
                   : throw new ArgumentException("The given object is not a parking space group identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSpaceGroupId)

        /// <summary>
        /// Compares two parking space group identifications.
        /// </summary>
        /// <param name="ParkingSpaceGroupId">A parking space group identification to compare with.</param>
        public Int32 CompareTo(ParkingSpaceGroup_Id ParkingSpaceGroupId)

            => String.Compare(InternalId,
                              ParkingSpaceGroupId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSpaceGroupId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking space group identifications for equality.
        /// </summary>
        /// <param name="Object">A parking space group identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceGroup_Id parkingSpaceGroupId &&
                   Equals(parkingSpaceGroupId);

        #endregion

        #region Equals(ParkingSpaceGroupId)

        /// <summary>
        /// Compares two parking space group identifications for equality.
        /// </summary>
        /// <param name="ParkingSpaceGroupId">A parking space group identification to compare with.</param>
        public Boolean Equals(ParkingSpaceGroup_Id ParkingSpaceGroupId)

            => String.Equals(InternalId,
                             ParkingSpaceGroupId.InternalId,
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
