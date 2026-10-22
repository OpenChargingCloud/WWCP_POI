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
    /// Extension methods for energy meter manufacturer admin status types.
    /// </summary>
    public static class EnergyMeterManufacturerAdminStatusTypesExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter manufacturer admin status type is null or empty.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType">A energy meter manufacturer admin status type.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterManufacturerAdminStatusType? EnergyMeterManufacturerAdminStatusType)
            => !EnergyMeterManufacturerAdminStatusType.HasValue || EnergyMeterManufacturerAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter manufacturer admin status type is null or empty.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType">A energy meter manufacturer admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterManufacturerAdminStatusType? EnergyMeterManufacturerAdminStatusType)
            => EnergyMeterManufacturerAdminStatusType.HasValue && EnergyMeterManufacturerAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a energy meter manufacturer.
    /// </summary>
    public readonly struct EnergyMeterManufacturerAdminStatusType : IId,
                                                                        IEquatable<EnergyMeterManufacturerAdminStatusType>,
                                                                        IComparable<EnergyMeterManufacturerAdminStatusType>
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
        /// The length of the energy meter manufacturer admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter manufacturer admin status type based on the given string.
        /// </summary>
        private EnergyMeterManufacturerAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a energy meter manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer admin status type.</param>
        public static EnergyMeterManufacturerAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out EnergyMeterManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            throw new ArgumentException($"Invalid text representation of a energy meter manufacturer admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a energy meter manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer admin status type.</param>
        public static EnergyMeterManufacturerAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EnergyMeterManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes))
                return chargingStationManufacturerAdminStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterManufacturerAdminStatusType)

        /// <summary>
        /// Parse the given string as a energy meter manufacturer admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter manufacturer admin status type.</param>
        /// <param name="EnergyMeterManufacturerAdminStatusType">The parsed energy meter manufacturer admin status type.</param>
        public static Boolean TryParse(String Text, out EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterManufacturerAdminStatusType = new EnergyMeterManufacturerAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterManufacturerAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter manufacturer admin status type.
        /// </summary>
        public EnergyMeterManufacturerAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the energy meter manufacturer.
        /// </summary>
        public static EnergyMeterManufacturerAdminStatusType  Unknown             { get; }
            = new ("unknown");

        /// <summary>
        /// Unclear status of the energy meter manufacturer.
        /// </summary>
        public static EnergyMeterManufacturerAdminStatusType  Unspecified         { get; }
            = new ("unspecified");

        /// <summary>
        /// The energy meter manufacturer is planned for the future.
        /// </summary>
        public static EnergyMeterManufacturerAdminStatusType  Planned             { get; }
            = new ("planned");

        /// <summary>
        /// The energy meter manufacturer is being deployed, not fully operational yet.
        /// </summary>
        public static EnergyMeterManufacturerAdminStatusType  InDeployment        { get; }
            = new ("inDeployment");

        /// <summary>
        /// The energy meter manufacturer is operational.
        /// </summary>
        public static EnergyMeterManufacturerAdminStatusType  Operational         { get; }
            = new ("operational");

        /// <summary>
        /// The energy meter manufacturer is for internal use only, e.g. for tests.
        /// </summary>
        public static EnergyMeterManufacturerAdminStatusType  InternalUse         { get; }
            = new ("internalUse");

        /// <summary>
        /// The energy meter manufacturer is under maintenance.
        /// </summary>
        public static EnergyMeterManufacturerAdminStatusType  OutOfService        { get; }
            = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterManufacturerAdminStatusType1, EnergyMeterManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType1">A energy meter manufacturer admin status type.</param>
        /// <param name="EnergyMeterManufacturerAdminStatusType2">Another energy meter manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType1,
                                           EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType2)

            => EnergyMeterManufacturerAdminStatusType1.Equals(EnergyMeterManufacturerAdminStatusType2);

        #endregion

        #region Operator != (EnergyMeterManufacturerAdminStatusType1, EnergyMeterManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType1">A energy meter manufacturer admin status type.</param>
        /// <param name="EnergyMeterManufacturerAdminStatusType2">Another energy meter manufacturer admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType1,
                                           EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType2)

            => !EnergyMeterManufacturerAdminStatusType1.Equals(EnergyMeterManufacturerAdminStatusType2);

        #endregion

        #region Operator <  (EnergyMeterManufacturerAdminStatusType1, EnergyMeterManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType1">A energy meter manufacturer admin status type.</param>
        /// <param name="EnergyMeterManufacturerAdminStatusType2">Another energy meter manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType1,
                                          EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType2)

            => EnergyMeterManufacturerAdminStatusType1.CompareTo(EnergyMeterManufacturerAdminStatusType2) < 0;

        #endregion

        #region Operator <= (EnergyMeterManufacturerAdminStatusType1, EnergyMeterManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType1">A energy meter manufacturer admin status type.</param>
        /// <param name="EnergyMeterManufacturerAdminStatusType2">Another energy meter manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType1,
                                           EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType2)

            => EnergyMeterManufacturerAdminStatusType1.CompareTo(EnergyMeterManufacturerAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterManufacturerAdminStatusType1, EnergyMeterManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType1">A energy meter manufacturer admin status type.</param>
        /// <param name="EnergyMeterManufacturerAdminStatusType2">Another energy meter manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType1,
                                          EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType2)

            => EnergyMeterManufacturerAdminStatusType1.CompareTo(EnergyMeterManufacturerAdminStatusType2) > 0;

        #endregion

        #region Operator >= (EnergyMeterManufacturerAdminStatusType1, EnergyMeterManufacturerAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType1">A energy meter manufacturer admin status type.</param>
        /// <param name="EnergyMeterManufacturerAdminStatusType2">Another energy meter manufacturer admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType1,
                                           EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType2)

            => EnergyMeterManufacturerAdminStatusType1.CompareTo(EnergyMeterManufacturerAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterManufacturerAdminStatusTypes> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter manufacturer admin status types.
        /// </summary>
        /// <param name="Object">A energy meter manufacturer admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes
                   ? CompareTo(chargingStationManufacturerAdminStatusTypes)
                   : throw new ArgumentException("The given object is not a energy meter manufacturer admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterManufacturerAdminStatusType)

        /// <summary>
        /// Compares two energy meter manufacturer admin status types.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType">A energy meter manufacturer admin status type to compare with.</param>
        public Int32 CompareTo(EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType)

            => String.Compare(InternalId,
                              EnergyMeterManufacturerAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterManufacturerAdminStatusTypes> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter manufacturer admin status types for equality.
        /// </summary>
        /// <param name="Object">A energy meter manufacturer admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterManufacturerAdminStatusType chargingStationManufacturerAdminStatusTypes &&
                   Equals(chargingStationManufacturerAdminStatusTypes);

        #endregion

        #region Equals(EnergyMeterManufacturerAdminStatusType)

        /// <summary>
        /// Compares two energy meter manufacturer admin status types for equality.
        /// </summary>
        /// <param name="EnergyMeterManufacturerAdminStatusType">A energy meter manufacturer admin status type to compare with.</param>
        public Boolean Equals(EnergyMeterManufacturerAdminStatusType EnergyMeterManufacturerAdminStatusType)

            => String.Equals(InternalId,
                             EnergyMeterManufacturerAdminStatusType.InternalId,
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
