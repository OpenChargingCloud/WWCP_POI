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
    /// Extension methods for energy meter manufacturer status typess.
    /// </summary>
    public static class EnergyMeterManufacturerStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter manufacturer status type is null or empty.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType">A energy meter manufacturer status type.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterManufacturerStatusType? EnergyMeterManufacturerStatusType)
            => !EnergyMeterManufacturerStatusType.HasValue || EnergyMeterManufacturerStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter manufacturer status type is null or empty.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType">A energy meter manufacturer status type.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterManufacturerStatusType? EnergyMeterManufacturerStatusType)
            => EnergyMeterManufacturerStatusType.HasValue && EnergyMeterManufacturerStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status type of a energy meter manufacturer.
    /// </summary>
    public readonly struct EnergyMeterManufacturerStatusType : IId,
                                                               IEquatable<EnergyMeterManufacturerStatusType>,
                                                               IComparable<EnergyMeterManufacturerStatusType>
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
        /// The length of the energy meter manufacturer status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter manufacturer status types based on the given string.
        /// </summary>
        private EnergyMeterManufacturerStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a energy meter manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer status type.</param>
        public static EnergyMeterManufacturerStatusType Parse(String Text)
        {

            if (TryParse(Text, out EnergyMeterManufacturerStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            throw new ArgumentException($"Invalid text representation of a energy meter manufacturer status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a energy meter manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer status type.</param>
        public static EnergyMeterManufacturerStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EnergyMeterManufacturerStatusType chargingStationManufacturerStatusTypes))
                return chargingStationManufacturerStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterManufacturerStatusType)

        /// <summary>
        /// Parse the given string as a energy meter manufacturer status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer status type.</param>
        /// <param name="EnergyMeterManufacturerStatusType">The parsed energy meter manufacturer status type.</param>
        public static Boolean TryParse(String Text, out EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterManufacturerStatusType = new EnergyMeterManufacturerStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterManufacturerStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter manufacturer status type.
        /// </summary>
        public EnergyMeterManufacturerStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the energy meter manufacturer.
        /// </summary>
        public static EnergyMeterManufacturerStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the energy meter manufacturer.
        /// </summary>
        public static EnergyMeterManufacturerStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// Currently no communication with the energy meter manufacturer is possible.
        /// </summary>
        public static EnergyMeterManufacturerStatusType  Offline             { get; }
            = new ("offline");

        /// <summary>
        /// The energy meter manufacturer is being deployed, not fully operational yet.
        /// </summary>
        public static EnergyMeterManufacturerStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The energy meter manufacturer is available.
        /// </summary>
        public static EnergyMeterManufacturerStatusType  Available           { get; }
            = new ("available");

        /// <summary>
        /// An error occurred within the energy meter manufacturer.
        /// </summary>
        public static EnergyMeterManufacturerStatusType  Error               { get; }
            = new ("error");

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterManufacturerStatusType1, EnergyMeterManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType1">A energy meter manufacturer status type.</param>
        /// <param name="EnergyMeterManufacturerStatusType2">Another energy meter manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType1,
                                           EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType2)

            => EnergyMeterManufacturerStatusType1.Equals(EnergyMeterManufacturerStatusType2);

        #endregion

        #region Operator != (EnergyMeterManufacturerStatusType1, EnergyMeterManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType1">A energy meter manufacturer status type.</param>
        /// <param name="EnergyMeterManufacturerStatusType2">Another energy meter manufacturer status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType1,
                                           EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType2)

            => !EnergyMeterManufacturerStatusType1.Equals(EnergyMeterManufacturerStatusType2);

        #endregion

        #region Operator <  (EnergyMeterManufacturerStatusType1, EnergyMeterManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType1">A energy meter manufacturer status type.</param>
        /// <param name="EnergyMeterManufacturerStatusType2">Another energy meter manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType1,
                                          EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType2)

            => EnergyMeterManufacturerStatusType1.CompareTo(EnergyMeterManufacturerStatusType2) < 0;

        #endregion

        #region Operator <= (EnergyMeterManufacturerStatusType1, EnergyMeterManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType1">A energy meter manufacturer status type.</param>
        /// <param name="EnergyMeterManufacturerStatusType2">Another energy meter manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType1,
                                           EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType2)

            => EnergyMeterManufacturerStatusType1.CompareTo(EnergyMeterManufacturerStatusType2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterManufacturerStatusType1, EnergyMeterManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType1">A energy meter manufacturer status type.</param>
        /// <param name="EnergyMeterManufacturerStatusType2">Another energy meter manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType1,
                                          EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType2)

            => EnergyMeterManufacturerStatusType1.CompareTo(EnergyMeterManufacturerStatusType2) > 0;

        #endregion

        #region Operator >= (EnergyMeterManufacturerStatusType1, EnergyMeterManufacturerStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType1">A energy meter manufacturer status type.</param>
        /// <param name="EnergyMeterManufacturerStatusType2">Another energy meter manufacturer status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType1,
                                           EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType2)

            => EnergyMeterManufacturerStatusType1.CompareTo(EnergyMeterManufacturerStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterManufacturerStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter manufacturer status types.
        /// </summary>
        /// <param name="Object">A energy meter manufacturer status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterManufacturerStatusType chargingStationManufacturerStatusTypes
                   ? CompareTo(chargingStationManufacturerStatusTypes)
                   : throw new ArgumentException("The given object is not a energy meter manufacturer status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterManufacturerStatusType)

        /// <summary>
        /// Compares two energy meter manufacturer status types.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType">A energy meter manufacturer status type to compare with.</param>
        public Int32 CompareTo(EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType)

            => String.Compare(InternalId,
                              EnergyMeterManufacturerStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterManufacturerStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter manufacturer status types for equality.
        /// </summary>
        /// <param name="Object">A energy meter manufacturer status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterManufacturerStatusType chargingStationManufacturerStatusTypes &&
                   Equals(chargingStationManufacturerStatusTypes);

        #endregion

        #region Equals(EnergyMeterManufacturerStatusType)

        /// <summary>
        /// Compares two energy meter manufacturer status types for equality.
        /// </summary>
        /// <param name="EnergyMeterManufacturerStatusType">A energy meter manufacturer status type to compare with.</param>
        public Boolean Equals(EnergyMeterManufacturerStatusType EnergyMeterManufacturerStatusType)

            => String.Equals(InternalId,
                             EnergyMeterManufacturerStatusType.InternalId,
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
