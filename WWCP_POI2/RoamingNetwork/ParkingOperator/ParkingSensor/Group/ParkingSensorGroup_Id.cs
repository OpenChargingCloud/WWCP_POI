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
    /// Extension methods for parking sensor group identifications.
    /// </summary>
    public static class ParkingSensorGroupIdExtensions
    {

        /// <summary>
        /// Indicates whether this parking sensor group identification is null or empty.
        /// </summary>
        /// <param name="ParkingSensorGroupId">A parking sensor group identification.</param>
        public static Boolean IsNullOrEmpty(this ParkingSensorGroup_Id? ParkingSensorGroupId)
            => !ParkingSensorGroupId.HasValue || ParkingSensorGroupId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking sensor group identification is NOT null or empty.
        /// </summary>
        /// <param name="ParkingSensorGroupId">A parking sensor group identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSensorGroup_Id? ParkingSensorGroupId)
            => ParkingSensorGroupId.HasValue && ParkingSensorGroupId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a parking sensor group.
    /// </summary>
    public readonly struct ParkingSensorGroup_Id : IId<ParkingSensorGroup_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this parking sensor group identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this parking sensor group identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the parking sensor group identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking sensor group identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a parking sensor group identification.</param>
        private ParkingSensorGroup_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique parking sensor group identification (UUIDv7).
        /// </summary>
        public static ParkingSensorGroup_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random parking sensor group identification.
        /// </summary>
        /// <param name="Length">The expected length of the parking sensor group identification.</param>
        public static ParkingSensorGroup_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a parking sensor group identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor group identification.</param>
        public static ParkingSensorGroup_Id Parse(String Text)
        {

            if (TryParse(Text, out var parkingSensorGroupId))
                return parkingSensorGroupId;

            throw new ArgumentException($"Invalid text representation of a parking sensor group identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parking sensor group identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor group identification.</param>
        public static ParkingSensorGroup_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var parkingSensorGroupId))
                return parkingSensorGroupId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSensorGroupId)

        /// <summary>
        /// Try to parse the given text as a parking sensor group identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor group identification.</param>
        /// <param name="ParkingSensorGroupId">The parsed parking sensor group identification.</param>
        public static Boolean TryParse(String Text, out ParkingSensorGroup_Id ParkingSensorGroupId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSensorGroupId = new ParkingSensorGroup_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSensorGroupId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking sensor group identification.
        /// </summary>
        public ParkingSensorGroup_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSensorGroupId1, ParkingSensorGroupId2)

        /// <summary>
        /// Compares two parking sensor group identifications for equality.
        /// </summary>
        /// <param name="ParkingSensorGroupId1">A parking sensor group identification.</param>
        /// <param name="ParkingSensorGroupId2">Another parking sensor group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSensorGroup_Id ParkingSensorGroupId1,
                                           ParkingSensorGroup_Id ParkingSensorGroupId2)

            => ParkingSensorGroupId1.Equals(ParkingSensorGroupId2);

        #endregion

        #region Operator != (ParkingSensorGroupId1, ParkingSensorGroupId2)

        /// <summary>
        /// Compares two parking sensor group identifications for inequality.
        /// </summary>
        /// <param name="ParkingSensorGroupId1">A parking sensor group identification.</param>
        /// <param name="ParkingSensorGroupId2">Another parking sensor group identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSensorGroup_Id ParkingSensorGroupId1,
                                           ParkingSensorGroup_Id ParkingSensorGroupId2)

            => !ParkingSensorGroupId1.Equals(ParkingSensorGroupId2);

        #endregion

        #region Operator <  (ParkingSensorGroupId1, ParkingSensorGroupId2)

        /// <summary>
        /// Compares two parking sensor group identifications.
        /// </summary>
        /// <param name="ParkingSensorGroupId1">A parking sensor group identification.</param>
        /// <param name="ParkingSensorGroupId2">Another parking sensor group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSensorGroup_Id ParkingSensorGroupId1,
                                          ParkingSensorGroup_Id ParkingSensorGroupId2)

            => ParkingSensorGroupId1.CompareTo(ParkingSensorGroupId2) < 0;

        #endregion

        #region Operator <= (ParkingSensorGroupId1, ParkingSensorGroupId2)

        /// <summary>
        /// Compares two parking sensor group identifications.
        /// </summary>
        /// <param name="ParkingSensorGroupId1">A parking sensor group identification.</param>
        /// <param name="ParkingSensorGroupId2">Another parking sensor group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSensorGroup_Id ParkingSensorGroupId1,
                                           ParkingSensorGroup_Id ParkingSensorGroupId2)

            => ParkingSensorGroupId1.CompareTo(ParkingSensorGroupId2) <= 0;

        #endregion

        #region Operator >  (ParkingSensorGroupId1, ParkingSensorGroupId2)

        /// <summary>
        /// Compares two parking sensor group identifications.
        /// </summary>
        /// <param name="ParkingSensorGroupId1">A parking sensor group identification.</param>
        /// <param name="ParkingSensorGroupId2">Another parking sensor group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSensorGroup_Id ParkingSensorGroupId1,
                                          ParkingSensorGroup_Id ParkingSensorGroupId2)

            => ParkingSensorGroupId1.CompareTo(ParkingSensorGroupId2) > 0;

        #endregion

        #region Operator >= (ParkingSensorGroupId1, ParkingSensorGroupId2)

        /// <summary>
        /// Compares two parking sensor group identifications.
        /// </summary>
        /// <param name="ParkingSensorGroupId1">A parking sensor group identification.</param>
        /// <param name="ParkingSensorGroupId2">Another parking sensor group identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSensorGroup_Id ParkingSensorGroupId1,
                                           ParkingSensorGroup_Id ParkingSensorGroupId2)

            => ParkingSensorGroupId1.CompareTo(ParkingSensorGroupId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSensorGroupId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking sensor group identifications.
        /// </summary>
        /// <param name="Object">A parking sensor group identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSensorGroup_Id parkingSensorGroupId
                   ? CompareTo(parkingSensorGroupId)
                   : throw new ArgumentException("The given object is not a parking sensor group identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSensorGroupId)

        /// <summary>
        /// Compares two parking sensor group identifications.
        /// </summary>
        /// <param name="ParkingSensorGroupId">A parking sensor group identification to compare with.</param>
        public Int32 CompareTo(ParkingSensorGroup_Id ParkingSensorGroupId)

            => String.Compare(InternalId,
                              ParkingSensorGroupId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSensorGroupId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking sensor group identifications for equality.
        /// </summary>
        /// <param name="Object">A parking sensor group identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSensorGroup_Id parkingSensorGroupId &&
                   Equals(parkingSensorGroupId);

        #endregion

        #region Equals(ParkingSensorGroupId)

        /// <summary>
        /// Compares two parking sensor group identifications for equality.
        /// </summary>
        /// <param name="ParkingSensorGroupId">A parking sensor group identification to compare with.</param>
        public Boolean Equals(ParkingSensorGroup_Id ParkingSensorGroupId)

            => String.Equals(InternalId,
                             ParkingSensorGroupId.InternalId,
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
