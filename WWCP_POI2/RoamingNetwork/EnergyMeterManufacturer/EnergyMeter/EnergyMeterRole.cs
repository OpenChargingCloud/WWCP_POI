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
    /// Extension methods for energy meter roles.
    /// </summary>
    public static class EnergyMeterRoleExtensions
    {

        /// <summary>
        /// Indicates whether this energy meter role is null or empty.
        /// </summary>
        /// <param name="EnergyMeterRole">An energy meter role.</param>
        public static Boolean IsNullOrEmpty(this EnergyMeterRole? EnergyMeterRole)
            => !EnergyMeterRole.HasValue || EnergyMeterRole.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this energy meter role is NOT null or empty.
        /// </summary>
        /// <param name="EnergyMeterRole">An energy meter role.</param>
        public static Boolean IsNotNullOrEmpty(this EnergyMeterRole? EnergyMeterRole)
            => EnergyMeterRole.HasValue && EnergyMeterRole.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// What an energy meter measures, e.g. the grid, a PV system or an EVSE.
    /// </summary>
    public readonly struct EnergyMeterRole : IId<EnergyMeterRole>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this energy meter role is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this energy meter role is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the energy meter role.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter role based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of an energy meter role.</param>
        private EnergyMeterRole(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as an energy meter role.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter role.</param>
        public static EnergyMeterRole Parse(String Text)
        {

            if (TryParse(Text, out var energyMeterRole))
                return energyMeterRole;

            throw new ArgumentException($"Invalid text representation of an energy meter role: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an energy meter role.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter role.</param>
        public static EnergyMeterRole? TryParse(String Text)
        {

            if (TryParse(Text, out var energyMeterRole))
                return energyMeterRole;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EnergyMeterRole)

        /// <summary>
        /// Try to parse the given text as an energy meter role.
        /// </summary>
        /// <param name="Text">A text representation of an energy meter role.</param>
        /// <param name="EnergyMeterRole">The parsed energy meter role.</param>
        public static Boolean TryParse(String Text, out EnergyMeterRole EnergyMeterRole)
        {

            Text = Text?.Trim() ?? "";

            // A predefined role in any case is the predefined one, so that the content,
            // and its ETags, do not depend on how it was written.
            foreach (var predefined in Predefined)
                if (String.Equals(predefined.InternalId, Text, StringComparison.OrdinalIgnoreCase))
                {
                    EnergyMeterRole = predefined;
                    return true;
                }

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EnergyMeterRole = new EnergyMeterRole(Text);
                    return true;
                }
                catch
                { }
            }

            EnergyMeterRole = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this energy meter role.
        /// </summary>
        public EnergyMeterRole Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static Definitions

        /// <summary>
        /// The energy exchanged with the public grid, e.g. the meter of the utility.
        /// </summary>
        public static EnergyMeterRole Grid
            => new ("grid");

        /// <summary>
        /// The energy of a grid connection point.
        /// </summary>
        public static EnergyMeterRole GridConnectionPoint
            => new ("gridConnectionPoint");

        /// <summary>
        /// The energy of a whole charging pool.
        /// </summary>
        public static EnergyMeterRole Pool
            => new ("pool");

        /// <summary>
        /// The energy of a charging station.
        /// </summary>
        public static EnergyMeterRole Station
            => new ("station");

        /// <summary>
        /// The energy of a single EVSE, e.g. its calibration-law meter.
        /// </summary>
        public static EnergyMeterRole EVSE
            => new ("evse");

        /// <summary>
        /// The energy generated by a photovoltaic system.
        /// </summary>
        public static EnergyMeterRole PV
            => new ("pv");

        /// <summary>
        /// The energy generated by a wind turbine.
        /// </summary>
        public static EnergyMeterRole Wind
            => new ("wind");

        /// <summary>
        /// The energy charged into and discharged from a stationary battery.
        /// </summary>
        public static EnergyMeterRole Battery
            => new ("battery");

        /// <summary>
        /// All predefined energy meter roles.
        /// </summary>
        private static readonly EnergyMeterRole[] Predefined = [ Grid, GridConnectionPoint, Pool, Station, EVSE, PV, Wind, Battery ];

        #endregion


        #region Operator overloading

        #region Operator == (EnergyMeterRole1, EnergyMeterRole2)

        /// <summary>
        /// Compares two energy meter roles for equality.
        /// </summary>
        /// <param name="EnergyMeterRole1">An energy meter role.</param>
        /// <param name="EnergyMeterRole2">Another energy meter role.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EnergyMeterRole EnergyMeterRole1,
                                           EnergyMeterRole EnergyMeterRole2)

            => EnergyMeterRole1.Equals(EnergyMeterRole2);

        #endregion

        #region Operator != (EnergyMeterRole1, EnergyMeterRole2)

        /// <summary>
        /// Compares two energy meter roles for inequality.
        /// </summary>
        /// <param name="EnergyMeterRole1">An energy meter role.</param>
        /// <param name="EnergyMeterRole2">Another energy meter role.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EnergyMeterRole EnergyMeterRole1,
                                           EnergyMeterRole EnergyMeterRole2)

            => !EnergyMeterRole1.Equals(EnergyMeterRole2);

        #endregion

        #region Operator <  (EnergyMeterRole1, EnergyMeterRole2)

        /// <summary>
        /// Compares two energy meter roles.
        /// </summary>
        /// <param name="EnergyMeterRole1">An energy meter role.</param>
        /// <param name="EnergyMeterRole2">Another energy meter role.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EnergyMeterRole EnergyMeterRole1,
                                          EnergyMeterRole EnergyMeterRole2)

            => EnergyMeterRole1.CompareTo(EnergyMeterRole2) < 0;

        #endregion

        #region Operator <= (EnergyMeterRole1, EnergyMeterRole2)

        /// <summary>
        /// Compares two energy meter roles.
        /// </summary>
        /// <param name="EnergyMeterRole1">An energy meter role.</param>
        /// <param name="EnergyMeterRole2">Another energy meter role.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EnergyMeterRole EnergyMeterRole1,
                                           EnergyMeterRole EnergyMeterRole2)

            => EnergyMeterRole1.CompareTo(EnergyMeterRole2) <= 0;

        #endregion

        #region Operator >  (EnergyMeterRole1, EnergyMeterRole2)

        /// <summary>
        /// Compares two energy meter roles.
        /// </summary>
        /// <param name="EnergyMeterRole1">An energy meter role.</param>
        /// <param name="EnergyMeterRole2">Another energy meter role.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EnergyMeterRole EnergyMeterRole1,
                                          EnergyMeterRole EnergyMeterRole2)

            => EnergyMeterRole1.CompareTo(EnergyMeterRole2) > 0;

        #endregion

        #region Operator >= (EnergyMeterRole1, EnergyMeterRole2)

        /// <summary>
        /// Compares two energy meter roles.
        /// </summary>
        /// <param name="EnergyMeterRole1">An energy meter role.</param>
        /// <param name="EnergyMeterRole2">Another energy meter role.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EnergyMeterRole EnergyMeterRole1,
                                           EnergyMeterRole EnergyMeterRole2)

            => EnergyMeterRole1.CompareTo(EnergyMeterRole2) >= 0;

        #endregion

        #endregion

        #region IComparable<EnergyMeterRole> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two energy meter roles.
        /// </summary>
        /// <param name="Object">An energy meter role to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EnergyMeterRole energyMeterRole
                   ? CompareTo(energyMeterRole)
                   : throw new ArgumentException("The given object is not an energy meter role!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EnergyMeterRole)

        /// <summary>
        /// Compares two energy meter roles.
        /// </summary>
        /// <param name="EnergyMeterRole">An energy meter role to compare with.</param>
        public Int32 CompareTo(EnergyMeterRole EnergyMeterRole)

            => String.Compare(InternalId,
                              EnergyMeterRole.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EnergyMeterRole> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two energy meter roles for equality.
        /// </summary>
        /// <param name="Object">An energy meter role to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterRole energyMeterRole &&
                   Equals(energyMeterRole);

        #endregion

        #region Equals(EnergyMeterRole)

        /// <summary>
        /// Compares two energy meter roles for equality.
        /// </summary>
        /// <param name="EnergyMeterRole">An energy meter role to compare with.</param>
        public Boolean Equals(EnergyMeterRole EnergyMeterRole)

            => String.Equals(InternalId,
                             EnergyMeterRole.InternalId,
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
