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
    /// Extension methods for energy meter admin status types.
    /// </summary>
    public static class EnergyMeterAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter admin status types is null or empty.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType">A energy meter admin status type.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterAdminStatusType? EnergyMeterAdminStatusType)
            => !EnergyMeterAdminStatusType.HasValue || EnergyMeterAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter admin status types is null or empty.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType">A energy meter admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterAdminStatusType? EnergyMeterAdminStatusType)
            => EnergyMeterAdminStatusType.HasValue && EnergyMeterAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of a energy meter.
    /// </summary>
    public readonly struct EnergyMeterAdminStatusType : IId,
                                                        IEquatable<EnergyMeterAdminStatusType>,
                                                        IComparable<EnergyMeterAdminStatusType>
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
        /// The length of the energy meter admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter admin status type based on the given string.
        /// </summary>
        private EnergyMeterAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a energy meter admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter admin status type.</param>
        public static EnergyMeterAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out EnergyMeterAdminStatusType energyMeterAdminStatusTypes))
                return energyMeterAdminStatusTypes;

            throw new ArgumentException($"Invalid text representation of a energy meter admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a energy meter admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter admin status type.</param>
        public static EnergyMeterAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EnergyMeterAdminStatusType energyMeterAdminStatusTypes))
                return energyMeterAdminStatusTypes;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterAdminStatusType)

        /// <summary>
        /// Parse the given string as a energy meter admin status type.
        /// </summary>
        /// <param name="Text">A text representation of a energy meter admin status type.</param>
        /// <param name="EnergyMeterAdminStatusType">The parsed energy meter admin status type.</param>
        public static Boolean TryParse(String Text, out EnergyMeterAdminStatusType EnergyMeterAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterAdminStatusType = new EnergyMeterAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter admin status type.
        /// </summary>
        public EnergyMeterAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unclear admin status of the energy meter.
        /// </summary>
        public static readonly EnergyMeterAdminStatusType Unspecified   = new("unspecified");

        /// <summary>
        /// Unknown admin status of the energy meter.
        /// </summary>
        public static readonly EnergyMeterAdminStatusType Unknown        = new("unknown");

        /// <summary>
        /// The energy meter is planned for the future.
        /// </summary>
        public static readonly EnergyMeterAdminStatusType Planned       = new("planned");

        /// <summary>
        /// The energy meter is currently in deployment, but not fully operational yet.
        /// </summary>
        public static readonly EnergyMeterAdminStatusType InDeployment  = new("inDeployment");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly EnergyMeterAdminStatusType InternalUse   = new("internalUse");

        /// <summary>
        /// The energy meter is under maintenance.
        /// </summary>
        public static readonly EnergyMeterAdminStatusType OutOfService  = new("outOfService");

        /// <summary>
        /// The energy meter is operational.
        /// </summary>
        public static readonly EnergyMeterAdminStatusType Operational   = new("operational");

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterAdminStatusType1, EnergyMeterAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType1">A energy meter admin status type.</param>
        /// <param name="EnergyMeterAdminStatusType2">Another energy meter admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterAdminStatusType EnergyMeterAdminStatusType1,
                                           EnergyMeterAdminStatusType EnergyMeterAdminStatusType2)

            => EnergyMeterAdminStatusType1.Equals(EnergyMeterAdminStatusType2);

        #endregion

        #region Operator != (EnergyMeterAdminStatusType1, EnergyMeterAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType1">A energy meter admin status type.</param>
        /// <param name="EnergyMeterAdminStatusType2">Another energy meter admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterAdminStatusType EnergyMeterAdminStatusType1,
                                           EnergyMeterAdminStatusType EnergyMeterAdminStatusType2)

            => !EnergyMeterAdminStatusType1.Equals(EnergyMeterAdminStatusType2);

        #endregion

        #region Operator <  (EnergyMeterAdminStatusType1, EnergyMeterAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType1">A energy meter admin status type.</param>
        /// <param name="EnergyMeterAdminStatusType2">Another energy meter admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterAdminStatusType EnergyMeterAdminStatusType1,
                                          EnergyMeterAdminStatusType EnergyMeterAdminStatusType2)

            => EnergyMeterAdminStatusType1.CompareTo(EnergyMeterAdminStatusType2) < 0;

        #endregion

        #region Operator <= (EnergyMeterAdminStatusType1, EnergyMeterAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType1">A energy meter admin status type.</param>
        /// <param name="EnergyMeterAdminStatusType2">Another energy meter admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterAdminStatusType EnergyMeterAdminStatusType1,
                                           EnergyMeterAdminStatusType EnergyMeterAdminStatusType2)

            => EnergyMeterAdminStatusType1.CompareTo(EnergyMeterAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterAdminStatusType1, EnergyMeterAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType1">A energy meter admin status type.</param>
        /// <param name="EnergyMeterAdminStatusType2">Another energy meter admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterAdminStatusType EnergyMeterAdminStatusType1,
                                          EnergyMeterAdminStatusType EnergyMeterAdminStatusType2)

            => EnergyMeterAdminStatusType1.CompareTo(EnergyMeterAdminStatusType2) > 0;

        #endregion

        #region Operator >= (EnergyMeterAdminStatusType1, EnergyMeterAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType1">A energy meter admin status type.</param>
        /// <param name="EnergyMeterAdminStatusType2">Another energy meter admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterAdminStatusType EnergyMeterAdminStatusType1,
                                           EnergyMeterAdminStatusType EnergyMeterAdminStatusType2)

            => EnergyMeterAdminStatusType1.CompareTo(EnergyMeterAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter admin status types.
        /// </summary>
        /// <param name="Object">A energy meter admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterAdminStatusType energyMeterAdminStatusTypes
                   ? CompareTo(energyMeterAdminStatusTypes)
                   : throw new ArgumentException("The given object is not a energy meter admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterAdminStatusType)

        /// <summary>
        /// Compares two energy meter admin status types.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType">A energy meter admin status type to compare with.</param>
        public Int32 CompareTo(EnergyMeterAdminStatusType EnergyMeterAdminStatusType)

            => String.Compare(InternalId,
                              EnergyMeterAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter admin status types for equality.
        /// </summary>
        /// <param name="Object">A energy meter admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterAdminStatusType energyMeterAdminStatusTypes &&
                   Equals(energyMeterAdminStatusTypes);

        #endregion

        #region Equals(EnergyMeterAdminStatusType)

        /// <summary>
        /// Compares two energy meter admin status types for equality.
        /// </summary>
        /// <param name="EnergyMeterAdminStatusType">A energy meter admin status type to compare with.</param>
        public Boolean Equals(EnergyMeterAdminStatusType EnergyMeterAdminStatusType)

            => String.Equals(InternalId,
                             EnergyMeterAdminStatusType.InternalId,
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
