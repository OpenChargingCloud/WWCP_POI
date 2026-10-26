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
    /// Extension methods for firmware versions.
    /// </summary>
    public static class FirmwareVersionExtensions
    {

        /// <summary>
        /// Indicates whether this firmware version is null or empty.
        /// </summary>
        /// <param name="FirmwareVersion">A firmware version.</param>
        public static Boolean IsNullOrEmpty(this FirmwareVersion? FirmwareVersion)
            => !FirmwareVersion.HasValue || FirmwareVersion.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this firmware version is NOT null or empty.
        /// </summary>
        /// <param name="FirmwareVersion">A firmware version.</param>
        public static Boolean IsNotNullOrEmpty(this FirmwareVersion? FirmwareVersion)
            => FirmwareVersion.HasValue && FirmwareVersion.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// A firmware version of a model of charging stations or energy meters, e.g. "2.4.1", as its manufacturer names it.
    /// </summary>
    /// <remarks>
    /// Unlike an identification it is taken exactly as it is given: not trimmed, and compared ordinally, case included, as
    /// its model, a charging station or an energy meter of it, and a certificate covering it name it and sign it.
    /// </remarks>
    public readonly struct FirmwareVersion : IId<FirmwareVersion>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this firmware version is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this firmware version is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the firmware version.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new firmware version based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a firmware version.</param>
        private FirmwareVersion(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a firmware version.
        /// </summary>
        /// <param name="Text">A text representation of a firmware version.</param>
        public static FirmwareVersion Parse(String Text)
        {

            if (TryParse(Text, out var firmwareVersion))
                return firmwareVersion;

            throw new ArgumentException($"Invalid text representation of a firmware version: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a firmware version.
        /// </summary>
        /// <param name="Text">A text representation of a firmware version.</param>
        public static FirmwareVersion? TryParse(String Text)
        {

            if (TryParse(Text, out var firmwareVersion))
                return firmwareVersion;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out FirmwareVersion)

        /// <summary>
        /// Try to parse the given text as a firmware version: not empty, and neither beginning nor ending with whitespace.
        /// </summary>
        /// <param name="Text">A text representation of a firmware version.</param>
        /// <param name="FirmwareVersion">The parsed firmware version.</param>
        public static Boolean TryParse(String Text, out FirmwareVersion FirmwareVersion)
        {

            if (!String.IsNullOrWhiteSpace(Text) &&
                Text == Text.Trim())
            {
                FirmwareVersion = new FirmwareVersion(Text);
                return true;
            }

            FirmwareVersion = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this firmware version.
        /// </summary>
        public FirmwareVersion Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (FirmwareVersion1, FirmwareVersion2)

        /// <summary>
        /// Compares two firmware versions for equality.
        /// </summary>
        /// <param name="FirmwareVersion1">A firmware version.</param>
        /// <param name="FirmwareVersion2">Another firmware version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (FirmwareVersion FirmwareVersion1,
                                           FirmwareVersion FirmwareVersion2)

            => FirmwareVersion1.Equals(FirmwareVersion2);

        #endregion

        #region Operator != (FirmwareVersion1, FirmwareVersion2)

        /// <summary>
        /// Compares two firmware versions for inequality.
        /// </summary>
        /// <param name="FirmwareVersion1">A firmware version.</param>
        /// <param name="FirmwareVersion2">Another firmware version.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (FirmwareVersion FirmwareVersion1,
                                           FirmwareVersion FirmwareVersion2)

            => !FirmwareVersion1.Equals(FirmwareVersion2);

        #endregion

        #region Operator <  (FirmwareVersion1, FirmwareVersion2)

        /// <summary>
        /// Compares two firmware versions.
        /// </summary>
        /// <param name="FirmwareVersion1">A firmware version.</param>
        /// <param name="FirmwareVersion2">Another firmware version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (FirmwareVersion FirmwareVersion1,
                                          FirmwareVersion FirmwareVersion2)

            => FirmwareVersion1.CompareTo(FirmwareVersion2) < 0;

        #endregion

        #region Operator <= (FirmwareVersion1, FirmwareVersion2)

        /// <summary>
        /// Compares two firmware versions.
        /// </summary>
        /// <param name="FirmwareVersion1">A firmware version.</param>
        /// <param name="FirmwareVersion2">Another firmware version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (FirmwareVersion FirmwareVersion1,
                                           FirmwareVersion FirmwareVersion2)

            => FirmwareVersion1.CompareTo(FirmwareVersion2) <= 0;

        #endregion

        #region Operator >  (FirmwareVersion1, FirmwareVersion2)

        /// <summary>
        /// Compares two firmware versions.
        /// </summary>
        /// <param name="FirmwareVersion1">A firmware version.</param>
        /// <param name="FirmwareVersion2">Another firmware version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (FirmwareVersion FirmwareVersion1,
                                          FirmwareVersion FirmwareVersion2)

            => FirmwareVersion1.CompareTo(FirmwareVersion2) > 0;

        #endregion

        #region Operator >= (FirmwareVersion1, FirmwareVersion2)

        /// <summary>
        /// Compares two firmware versions.
        /// </summary>
        /// <param name="FirmwareVersion1">A firmware version.</param>
        /// <param name="FirmwareVersion2">Another firmware version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (FirmwareVersion FirmwareVersion1,
                                           FirmwareVersion FirmwareVersion2)

            => FirmwareVersion1.CompareTo(FirmwareVersion2) >= 0;

        #endregion

        #endregion

        #region IComparable<FirmwareVersion> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two firmware versions.
        /// </summary>
        /// <param name="Object">A firmware version to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is FirmwareVersion firmwareVersion
                   ? CompareTo(firmwareVersion)
                   : throw new ArgumentException("The given object is not a firmware version!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(FirmwareVersion)

        /// <summary>
        /// Compares two firmware versions.
        /// </summary>
        /// <param name="FirmwareVersion">A firmware version to compare with.</param>
        public Int32 CompareTo(FirmwareVersion FirmwareVersion)

            => String.Compare(InternalId,
                              FirmwareVersion.InternalId,
                              StringComparison.Ordinal);

        #endregion

        #endregion

        #region IEquatable<FirmwareVersion> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two firmware versions for equality.
        /// </summary>
        /// <param name="Object">A firmware version to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is FirmwareVersion firmwareVersion &&
                   Equals(firmwareVersion);

        #endregion

        #region Equals(FirmwareVersion)

        /// <summary>
        /// Compares two firmware versions for equality.
        /// </summary>
        /// <param name="FirmwareVersion">A firmware version to compare with.</param>
        public Boolean Equals(FirmwareVersion FirmwareVersion)

            => String.Equals(InternalId,
                             FirmwareVersion.InternalId,
                             StringComparison.Ordinal);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => InternalId is null ? 0 : StringComparer.Ordinal.GetHashCode(InternalId);

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
