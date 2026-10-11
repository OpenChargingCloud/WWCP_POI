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
    /// Extension methods for parking sensor identifications.
    /// </summary>
    public static class ParkingSensorIdExtensions
    {

        /// <summary>
        /// Indicates whether this parking sensor identification is null or empty.
        /// </summary>
        /// <param name="ParkingSensorId">A parking sensor identification.</param>
        public static Boolean IsNullOrEmpty(this ParkingSensor_Id? ParkingSensorId)
            => !ParkingSensorId.HasValue || ParkingSensorId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this parking sensor identification is NOT null or empty.
        /// </summary>
        /// <param name="ParkingSensorId">A parking sensor identification.</param>
        public static Boolean IsNotNullOrEmpty(this ParkingSensor_Id? ParkingSensorId)
            => ParkingSensorId.HasValue && ParkingSensorId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a parking sensor.
    /// </summary>
    public readonly struct ParkingSensor_Id : IId<ParkingSensor_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this parking sensor identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this parking sensor identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the parking sensor identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking sensor identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a parking sensor identification.</param>
        private ParkingSensor_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) New

        /// <summary>
        /// Create a new unique parking sensor identification (UUIDv7).
        /// </summary>
        public static ParkingSensor_Id New

            => new (UUIDv7.Generate().ToString());

        #endregion

        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random parking sensor identification.
        /// </summary>
        /// <param name="Length">The expected length of the parking sensor identification.</param>
        public static ParkingSensor_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a parking sensor identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor identification.</param>
        public static ParkingSensor_Id Parse(String Text)
        {

            if (TryParse(Text, out var parkingSensorId))
                return parkingSensorId;

            throw new ArgumentException($"Invalid text representation of a parking sensor identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a parking sensor identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor identification.</param>
        public static ParkingSensor_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var parkingSensorId))
                return parkingSensorId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ParkingSensorId)

        /// <summary>
        /// Try to parse the given text as a parking sensor identification.
        /// </summary>
        /// <param name="Text">A text representation of a parking sensor identification.</param>
        /// <param name="ParkingSensorId">The parsed parking sensor identification.</param>
        public static Boolean TryParse(String Text, out ParkingSensor_Id ParkingSensorId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ParkingSensorId = new ParkingSensor_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ParkingSensorId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this parking sensor identification.
        /// </summary>
        public ParkingSensor_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ParkingSensorId1, ParkingSensorId2)

        /// <summary>
        /// Compares two parking sensor identifications for equality.
        /// </summary>
        /// <param name="ParkingSensorId1">A parking sensor identification.</param>
        /// <param name="ParkingSensorId2">Another parking sensor identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ParkingSensor_Id ParkingSensorId1,
                                           ParkingSensor_Id ParkingSensorId2)

            => ParkingSensorId1.Equals(ParkingSensorId2);

        #endregion

        #region Operator != (ParkingSensorId1, ParkingSensorId2)

        /// <summary>
        /// Compares two parking sensor identifications for inequality.
        /// </summary>
        /// <param name="ParkingSensorId1">A parking sensor identification.</param>
        /// <param name="ParkingSensorId2">Another parking sensor identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ParkingSensor_Id ParkingSensorId1,
                                           ParkingSensor_Id ParkingSensorId2)

            => !ParkingSensorId1.Equals(ParkingSensorId2);

        #endregion

        #region Operator <  (ParkingSensorId1, ParkingSensorId2)

        /// <summary>
        /// Compares two parking sensor identifications.
        /// </summary>
        /// <param name="ParkingSensorId1">A parking sensor identification.</param>
        /// <param name="ParkingSensorId2">Another parking sensor identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ParkingSensor_Id ParkingSensorId1,
                                          ParkingSensor_Id ParkingSensorId2)

            => ParkingSensorId1.CompareTo(ParkingSensorId2) < 0;

        #endregion

        #region Operator <= (ParkingSensorId1, ParkingSensorId2)

        /// <summary>
        /// Compares two parking sensor identifications.
        /// </summary>
        /// <param name="ParkingSensorId1">A parking sensor identification.</param>
        /// <param name="ParkingSensorId2">Another parking sensor identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ParkingSensor_Id ParkingSensorId1,
                                           ParkingSensor_Id ParkingSensorId2)

            => ParkingSensorId1.CompareTo(ParkingSensorId2) <= 0;

        #endregion

        #region Operator >  (ParkingSensorId1, ParkingSensorId2)

        /// <summary>
        /// Compares two parking sensor identifications.
        /// </summary>
        /// <param name="ParkingSensorId1">A parking sensor identification.</param>
        /// <param name="ParkingSensorId2">Another parking sensor identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ParkingSensor_Id ParkingSensorId1,
                                          ParkingSensor_Id ParkingSensorId2)

            => ParkingSensorId1.CompareTo(ParkingSensorId2) > 0;

        #endregion

        #region Operator >= (ParkingSensorId1, ParkingSensorId2)

        /// <summary>
        /// Compares two parking sensor identifications.
        /// </summary>
        /// <param name="ParkingSensorId1">A parking sensor identification.</param>
        /// <param name="ParkingSensorId2">Another parking sensor identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ParkingSensor_Id ParkingSensorId1,
                                           ParkingSensor_Id ParkingSensorId2)

            => ParkingSensorId1.CompareTo(ParkingSensorId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ParkingSensorId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two parking sensor identifications.
        /// </summary>
        /// <param name="Object">A parking sensor identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ParkingSensor_Id parkingSensorId
                   ? CompareTo(parkingSensorId)
                   : throw new ArgumentException("The given object is not a parking sensor identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ParkingSensorId)

        /// <summary>
        /// Compares two parking sensor identifications.
        /// </summary>
        /// <param name="ParkingSensorId">A parking sensor identification to compare with.</param>
        public Int32 CompareTo(ParkingSensor_Id ParkingSensorId)

            => String.Compare(InternalId,
                              ParkingSensorId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ParkingSensorId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two parking sensor identifications for equality.
        /// </summary>
        /// <param name="Object">A parking sensor identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSensor_Id parkingSensorId &&
                   Equals(parkingSensorId);

        #endregion

        #region Equals(ParkingSensorId)

        /// <summary>
        /// Compares two parking sensor identifications for equality.
        /// </summary>
        /// <param name="ParkingSensorId">A parking sensor identification to compare with.</param>
        public Boolean Equals(ParkingSensor_Id ParkingSensorId)

            => String.Equals(InternalId,
                             ParkingSensorId.InternalId,
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
