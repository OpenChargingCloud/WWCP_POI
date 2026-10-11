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
    /// Extension methods for e-mobility provider status types.
    /// </summary>
    public static class EMobilityProviderStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this e-mobility provider status types is null or empty.
        /// </summary>
        /// <param name="EMobilityProviderStatusType">A e-mobility provider status type.</param>
        public static Boolean IsNullOrEmpty(this EMobilityProviderStatusType? EMobilityProviderStatusType)
            => !EMobilityProviderStatusType.HasValue || EMobilityProviderStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this e-mobility provider status types is null or empty.
        /// </summary>
        /// <param name="EMobilityProviderStatusType">A e-mobility provider status type.</param>
        public static Boolean IsNotNullOrEmpty(this EMobilityProviderStatusType? EMobilityProviderStatusType)
            => EMobilityProviderStatusType.HasValue && EMobilityProviderStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The status of an e-mobility provider.
    /// </summary>
    public readonly struct EMobilityProviderStatusType : IId,
                                                               IEquatable<EMobilityProviderStatusType>,
                                                               IComparable<EMobilityProviderStatusType>
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
        /// The length of the e-mobility provider status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new e-mobility provider status type based on the given string.
        /// </summary>
        private EMobilityProviderStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an e-mobility provider status type.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility provider status type.</param>
        public static EMobilityProviderStatusType Parse(String Text)
        {

            if (TryParse(Text, out EMobilityProviderStatusType eMobilityProviderStatusType))
                return eMobilityProviderStatusType;

            throw new ArgumentException($"Invalid text representation of an e-mobility provider status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an e-mobility provider status type.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility provider status type.</param>
        public static EMobilityProviderStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EMobilityProviderStatusType eMobilityProviderStatusType))
                return eMobilityProviderStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EMobilityProviderStatusType)

        /// <summary>
        /// Parse the given string as an e-mobility provider status type.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility provider status type.</param>
        /// <param name="EMobilityProviderStatusType">The parsed e-mobility provider status type.</param>
        public static Boolean TryParse(String Text, out EMobilityProviderStatusType EMobilityProviderStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EMobilityProviderStatusType = new EMobilityProviderStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EMobilityProviderStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this e-mobility provider status type.
        /// </summary>
        public EMobilityProviderStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown status of the e-mobility provider.
        /// </summary>
        public static readonly EMobilityProviderStatusType Unknown       = new("unknown");

        /// <summary>
        /// Unclear status of the e-mobility provider.
        /// </summary>
        public static readonly EMobilityProviderStatusType Unspecified   = new("unspecified");

        /// <summary>
        /// The e-mobility provider is currently offline.
        /// </summary>
        public static readonly EMobilityProviderStatusType Offline       = new("offline");

        /// <summary>
        /// The e-mobility provider is not fully operational yet.
        /// </summary>
        public static readonly EMobilityProviderStatusType InDeployment  = new("inDeployment");

        /// <summary>
        /// The e-mobility provider is available.
        /// </summary>
        public static readonly EMobilityProviderStatusType Available     = new("available");

        /// <summary>
        /// A fatal error has occurred within the e-mobility provider.
        /// </summary>
        public static readonly EMobilityProviderStatusType Error         = new("error");

        #endregion


        #region Operator overloading

        #region Operator == (EMobilityProviderStatusType1, EMobilityProviderStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderStatusType1">A e-mobility provider status type.</param>
        /// <param name="EMobilityProviderStatusType2">Another e-mobility provider status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EMobilityProviderStatusType EMobilityProviderStatusType1,
                                           EMobilityProviderStatusType EMobilityProviderStatusType2)

            => EMobilityProviderStatusType1.Equals(EMobilityProviderStatusType2);

        #endregion

        #region Operator != (EMobilityProviderStatusType1, EMobilityProviderStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderStatusType1">A e-mobility provider status type.</param>
        /// <param name="EMobilityProviderStatusType2">Another e-mobility provider status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EMobilityProviderStatusType EMobilityProviderStatusType1,
                                           EMobilityProviderStatusType EMobilityProviderStatusType2)

            => !EMobilityProviderStatusType1.Equals(EMobilityProviderStatusType2);

        #endregion

        #region Operator <  (EMobilityProviderStatusType1, EMobilityProviderStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderStatusType1">A e-mobility provider status type.</param>
        /// <param name="EMobilityProviderStatusType2">Another e-mobility provider status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EMobilityProviderStatusType EMobilityProviderStatusType1,
                                          EMobilityProviderStatusType EMobilityProviderStatusType2)

            => EMobilityProviderStatusType1.CompareTo(EMobilityProviderStatusType2) < 0;

        #endregion

        #region Operator <= (EMobilityProviderStatusType1, EMobilityProviderStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderStatusType1">A e-mobility provider status type.</param>
        /// <param name="EMobilityProviderStatusType2">Another e-mobility provider status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EMobilityProviderStatusType EMobilityProviderStatusType1,
                                           EMobilityProviderStatusType EMobilityProviderStatusType2)

            => EMobilityProviderStatusType1.CompareTo(EMobilityProviderStatusType2) <= 0;

        #endregion

        #region Operator >  (EMobilityProviderStatusType1, EMobilityProviderStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderStatusType1">A e-mobility provider status type.</param>
        /// <param name="EMobilityProviderStatusType2">Another e-mobility provider status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EMobilityProviderStatusType EMobilityProviderStatusType1,
                                          EMobilityProviderStatusType EMobilityProviderStatusType2)

            => EMobilityProviderStatusType1.CompareTo(EMobilityProviderStatusType2) > 0;

        #endregion

        #region Operator >= (EMobilityProviderStatusType1, EMobilityProviderStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderStatusType1">A e-mobility provider status type.</param>
        /// <param name="EMobilityProviderStatusType2">Another e-mobility provider status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EMobilityProviderStatusType EMobilityProviderStatusType1,
                                           EMobilityProviderStatusType EMobilityProviderStatusType2)

            => EMobilityProviderStatusType1.CompareTo(EMobilityProviderStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EMobilityProviderStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two e-mobility provider status types.
        /// </summary>
        /// <param name="Object">A e-mobility provider status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EMobilityProviderStatusType eMobilityProviderStatusType
                   ? CompareTo(eMobilityProviderStatusType)
                   : throw new ArgumentException("The given object is not an e-mobility provider status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EMobilityProviderStatusType)

        /// <summary>
        /// Compares two e-mobility provider status types.
        /// </summary>
        /// <param name="EMobilityProviderStatusType">A e-mobility provider status type to compare with.</param>
        public Int32 CompareTo(EMobilityProviderStatusType EMobilityProviderStatusType)

            => String.Compare(InternalId,
                              EMobilityProviderStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EMobilityProviderStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two e-mobility provider status types for equality.
        /// </summary>
        /// <param name="Object">A e-mobility provider status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EMobilityProviderStatusType eMobilityProviderStatusType &&
                   Equals(eMobilityProviderStatusType);

        #endregion

        #region Equals(EMobilityProviderStatusType)

        /// <summary>
        /// Compares two e-mobility provider status types for equality.
        /// </summary>
        /// <param name="EMobilityProviderStatusType">A e-mobility provider status type to compare with.</param>
        public Boolean Equals(EMobilityProviderStatusType EMobilityProviderStatusType)

            => String.Equals(InternalId,
                             EMobilityProviderStatusType.InternalId,
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
