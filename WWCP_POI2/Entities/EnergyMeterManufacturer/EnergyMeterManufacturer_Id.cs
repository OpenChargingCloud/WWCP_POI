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
    /// Extension methods for energy meter manufacturer identifications.
    /// </summary>
    public static class EnergyMeterManufacturerIdExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter manufacturer identification is null or empty.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId">A energy meter manufacturer identification.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterManufacturer_Id? EnergyMeterManufacturerId)
            => !EnergyMeterManufacturerId.HasValue || EnergyMeterManufacturerId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter manufacturer identification is NOT null or empty.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId">A energy meter manufacturer identification.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterManufacturer_Id? EnergyMeterManufacturerId)
            => EnergyMeterManufacturerId.HasValue && EnergyMeterManufacturerId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a energy meter manufacturer.
    /// </summary>
    public readonly struct EnergyMeterManufacturer_Id : IId<EnergyMeterManufacturer_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this energy meter manufacturer identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this energy meter manufacturer identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the energy meter manufacturer identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter manufacturer identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a energy meter manufacturer identification.</param>
        private EnergyMeterManufacturer_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random energy meter manufacturer identification.
        /// </summary>
        /// <param name="Length">The expected length of the energy meter manufacturer identification.</param>
        public static EnergyMeterManufacturer_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a energy meter manufacturer identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer identification.</param>
        public static EnergyMeterManufacturer_Id Parse(String Text)
        {

            if (TryParse(Text, out var energyMeterManufacturerId))
                return energyMeterManufacturerId;

            throw new ArgumentException($"Invalid text representation of a energy meter manufacturer identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a energy meter manufacturer identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer identification.</param>
        public static EnergyMeterManufacturer_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var energyMeterManufacturerId))
                return energyMeterManufacturerId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterManufacturerId)

        /// <summary>
        /// Try to parse the given text as a energy meter manufacturer identification.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer identification.</param>
        /// <param name="EnergyMeterManufacturerId">The parsed energy meter manufacturer identification.</param>
        public static Boolean TryParse(String Text, out EnergyMeterManufacturer_Id EnergyMeterManufacturerId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterManufacturerId = new EnergyMeterManufacturer_Id(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterManufacturerId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter manufacturer identification.
        /// </summary>
        public EnergyMeterManufacturer_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterManufacturerId1, EnergyMeterManufacturerId2)

        /// <summary>
        /// Compares two energy meter manufacturer identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId1">A energy meter manufacturer identification.</param>
        /// <param name="EnergyMeterManufacturerId2">Another energy meter manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterManufacturer_Id EnergyMeterManufacturerId1,
                                           EnergyMeterManufacturer_Id EnergyMeterManufacturerId2)

            => EnergyMeterManufacturerId1.Equals(EnergyMeterManufacturerId2);

        #endregion

        #region Operator != (EnergyMeterManufacturerId1, EnergyMeterManufacturerId2)

        /// <summary>
        /// Compares two energy meter manufacturer identifications for inequality.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId1">A energy meter manufacturer identification.</param>
        /// <param name="EnergyMeterManufacturerId2">Another energy meter manufacturer identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterManufacturer_Id EnergyMeterManufacturerId1,
                                           EnergyMeterManufacturer_Id EnergyMeterManufacturerId2)

            => !EnergyMeterManufacturerId1.Equals(EnergyMeterManufacturerId2);

        #endregion

        #region Operator <  (EnergyMeterManufacturerId1, EnergyMeterManufacturerId2)

        /// <summary>
        /// Compares two energy meter manufacturer identifications.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId1">A energy meter manufacturer identification.</param>
        /// <param name="EnergyMeterManufacturerId2">Another energy meter manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterManufacturer_Id EnergyMeterManufacturerId1,
                                          EnergyMeterManufacturer_Id EnergyMeterManufacturerId2)

            => EnergyMeterManufacturerId1.CompareTo(EnergyMeterManufacturerId2) < 0;

        #endregion

        #region Operator <= (EnergyMeterManufacturerId1, EnergyMeterManufacturerId2)

        /// <summary>
        /// Compares two energy meter manufacturer identifications.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId1">A energy meter manufacturer identification.</param>
        /// <param name="EnergyMeterManufacturerId2">Another energy meter manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterManufacturer_Id EnergyMeterManufacturerId1,
                                           EnergyMeterManufacturer_Id EnergyMeterManufacturerId2)

            => EnergyMeterManufacturerId1.CompareTo(EnergyMeterManufacturerId2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterManufacturerId1, EnergyMeterManufacturerId2)

        /// <summary>
        /// Compares two energy meter manufacturer identifications.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId1">A energy meter manufacturer identification.</param>
        /// <param name="EnergyMeterManufacturerId2">Another energy meter manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterManufacturer_Id EnergyMeterManufacturerId1,
                                          EnergyMeterManufacturer_Id EnergyMeterManufacturerId2)

            => EnergyMeterManufacturerId1.CompareTo(EnergyMeterManufacturerId2) > 0;

        #endregion

        #region Operator >= (EnergyMeterManufacturerId1, EnergyMeterManufacturerId2)

        /// <summary>
        /// Compares two energy meter manufacturer identifications.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId1">A energy meter manufacturer identification.</param>
        /// <param name="EnergyMeterManufacturerId2">Another energy meter manufacturer identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterManufacturer_Id EnergyMeterManufacturerId1,
                                           EnergyMeterManufacturer_Id EnergyMeterManufacturerId2)

            => EnergyMeterManufacturerId1.CompareTo(EnergyMeterManufacturerId2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterManufacturerId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter manufacturer identifications.
        /// </summary>
        /// <param name="Object">A energy meter manufacturer identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterManufacturer_Id energyMeterManufacturerId
                   ? CompareTo(energyMeterManufacturerId)
                   : throw new ArgumentException("The given object is not a energy meter manufacturer identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterManufacturerId)

        /// <summary>
        /// Compares two energy meter manufacturer identifications.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId">A energy meter manufacturer identification to compare with.</param>
        public Int32 CompareTo(EnergyMeterManufacturer_Id EnergyMeterManufacturerId)

            => String.Compare(InternalId,
                              EnergyMeterManufacturerId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterManufacturerId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter manufacturer identifications for equality.
        /// </summary>
        /// <param name="Object">A energy meter manufacturer identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterManufacturer_Id energyMeterManufacturerId &&
                   Equals(energyMeterManufacturerId);

        #endregion

        #region Equals(EnergyMeterManufacturerId)

        /// <summary>
        /// Compares two energy meter manufacturer identifications for equality.
        /// </summary>
        /// <param name="EnergyMeterManufacturerId">A energy meter manufacturer identification to compare with.</param>
        public Boolean Equals(EnergyMeterManufacturer_Id EnergyMeterManufacturerId)

            => String.Equals(InternalId,
                             EnergyMeterManufacturerId.InternalId,
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
