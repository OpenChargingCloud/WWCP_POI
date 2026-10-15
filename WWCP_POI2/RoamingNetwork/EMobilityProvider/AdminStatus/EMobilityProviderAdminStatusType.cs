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
    /// Extension methods for e-mobility provider admin status types.
    /// </summary>
    public static class EMobilityProviderAdminStatusTypeExtensions
    {

        /// <summary>
        /// Indicates whether this e-mobility provider admin status types is null or empty.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType">A e-mobility provider admin status type.</param>
        public static Boolean IsNullOrEmpty(this EMobilityProviderAdminStatusType? EMobilityProviderAdminStatusType)
            => !EMobilityProviderAdminStatusType.HasValue || EMobilityProviderAdminStatusType.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this e-mobility provider admin status types is null or empty.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType">A e-mobility provider admin status type.</param>
        public static Boolean IsNotNullOrEmpty(this EMobilityProviderAdminStatusType? EMobilityProviderAdminStatusType)
            => EMobilityProviderAdminStatusType.HasValue && EMobilityProviderAdminStatusType.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The admin status of an e-mobility provider.
    /// </summary>
    public readonly struct EMobilityProviderAdminStatusType : IId,
                                                                    IEquatable<EMobilityProviderAdminStatusType>,
                                                                    IComparable<EMobilityProviderAdminStatusType>
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
        /// The length of the e-mobility provider admin status.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new e-mobility provider admin status type based on the given string.
        /// </summary>
        private EMobilityProviderAdminStatusType(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as an e-mobility provider admin status type.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility provider admin status type.</param>
        public static EMobilityProviderAdminStatusType Parse(String Text)
        {

            if (TryParse(Text, out EMobilityProviderAdminStatusType eMobilityProviderAdminStatusType))
                return eMobilityProviderAdminStatusType;

            throw new ArgumentException($"Invalid text representation of an e-mobility provider admin status type: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as an e-mobility provider admin status type.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility provider admin status type.</param>
        public static EMobilityProviderAdminStatusType? TryParse(String Text)
        {

            if (TryParse(Text, out EMobilityProviderAdminStatusType eMobilityProviderAdminStatusType))
                return eMobilityProviderAdminStatusType;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EMobilityProviderAdminStatusType)

        /// <summary>
        /// Parse the given string as an e-mobility provider admin status type.
        /// </summary>
        /// <param name="Text">A text representation of an e-mobility provider admin status type.</param>
        /// <param name="EMobilityProviderAdminStatusType">The parsed e-mobility provider admin status type.</param>
        public static Boolean TryParse(String Text, out EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EMobilityProviderAdminStatusType = new EMobilityProviderAdminStatusType(Text);
                    return true;
                }
                catch
                { }
            }

            EMobilityProviderAdminStatusType = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this e-mobility provider admin status type.
        /// </summary>
        public EMobilityProviderAdminStatusType Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static members

        /// <summary>
        /// Unknown admin status of the e-mobility provider.
        /// </summary>
        public static readonly EMobilityProviderAdminStatusType Unknown      = new ("unknown");

        /// <summary>
        /// Unclear admin status of the e-mobility provider.
        /// </summary>
        public static readonly EMobilityProviderAdminStatusType Unspecified  = new ("unspecified");

        /// <summary>
        /// The e-mobility provider is planned for the future.
        /// </summary>
        public static readonly EMobilityProviderAdminStatusType Planned      = new ("planned");

        /// <summary>
        /// The e-mobility provider is being deployed, not fully operational yet.
        /// </summary>
        public static readonly EMobilityProviderAdminStatusType InDeployment = new ("inDeployment");

        /// <summary>
        /// The e-mobility provider is operational.
        /// </summary>
        public static readonly EMobilityProviderAdminStatusType Operational  = new ("operational");

        /// <summary>
        /// Private or internal use.
        /// </summary>
        public static readonly EMobilityProviderAdminStatusType InternalUse  = new ("internalUse");

        /// <summary>
        /// The e-mobility provider is under maintenance.
        /// </summary>
        public static readonly EMobilityProviderAdminStatusType OutOfService = new ("outOfService");

        #endregion


        #region Operator overloading

        #region Operator == (EMobilityProviderAdminStatusType1, EMobilityProviderAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType1">A e-mobility provider admin status type.</param>
        /// <param name="EMobilityProviderAdminStatusType2">Another e-mobility provider admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType1,
                                           EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType2)

            => EMobilityProviderAdminStatusType1.Equals(EMobilityProviderAdminStatusType2);

        #endregion

        #region Operator != (EMobilityProviderAdminStatusType1, EMobilityProviderAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType1">A e-mobility provider admin status type.</param>
        /// <param name="EMobilityProviderAdminStatusType2">Another e-mobility provider admin status type.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType1,
                                           EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType2)

            => !EMobilityProviderAdminStatusType1.Equals(EMobilityProviderAdminStatusType2);

        #endregion

        #region Operator <  (EMobilityProviderAdminStatusType1, EMobilityProviderAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType1">A e-mobility provider admin status type.</param>
        /// <param name="EMobilityProviderAdminStatusType2">Another e-mobility provider admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType1,
                                          EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType2)

            => EMobilityProviderAdminStatusType1.CompareTo(EMobilityProviderAdminStatusType2) < 0;

        #endregion

        #region Operator <= (EMobilityProviderAdminStatusType1, EMobilityProviderAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType1">A e-mobility provider admin status type.</param>
        /// <param name="EMobilityProviderAdminStatusType2">Another e-mobility provider admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType1,
                                           EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType2)

            => EMobilityProviderAdminStatusType1.CompareTo(EMobilityProviderAdminStatusType2) <= 0;

        #endregion

        #region Operator >  (EMobilityProviderAdminStatusType1, EMobilityProviderAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType1">A e-mobility provider admin status type.</param>
        /// <param name="EMobilityProviderAdminStatusType2">Another e-mobility provider admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType1,
                                          EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType2)

            => EMobilityProviderAdminStatusType1.CompareTo(EMobilityProviderAdminStatusType2) > 0;

        #endregion

        #region Operator >= (EMobilityProviderAdminStatusType1, EMobilityProviderAdminStatusType2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType1">A e-mobility provider admin status type.</param>
        /// <param name="EMobilityProviderAdminStatusType2">Another e-mobility provider admin status type.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType1,
                                           EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType2)

            => EMobilityProviderAdminStatusType1.CompareTo(EMobilityProviderAdminStatusType2) >= 0;

        #endregion

        #endregion

        #region IComparable<EMobilityProviderAdminStatusType> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two e-mobility provider admin status types.
        /// </summary>
        /// <param name="Object">A e-mobility provider admin status type to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EMobilityProviderAdminStatusType eMobilityProviderAdminStatusType
                   ? CompareTo(eMobilityProviderAdminStatusType)
                   : throw new ArgumentException("The given object is not an e-mobility provider admin status type!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EMobilityProviderAdminStatusType)

        /// <summary>
        /// Compares two e-mobility provider admin status types.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType">A e-mobility provider admin status type to compare with.</param>
        public Int32 CompareTo(EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType)

            => String.Compare(InternalId,
                              EMobilityProviderAdminStatusType.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EMobilityProviderAdminStatusType> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two e-mobility provider admin status types for equality.
        /// </summary>
        /// <param name="Object">A e-mobility provider admin status type to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EMobilityProviderAdminStatusType eMobilityProviderAdminStatusType &&
                   Equals(eMobilityProviderAdminStatusType);

        #endregion

        #region Equals(EMobilityProviderAdminStatusType)

        /// <summary>
        /// Compares two e-mobility provider admin status types for equality.
        /// </summary>
        /// <param name="EMobilityProviderAdminStatusType">A e-mobility provider admin status type to compare with.</param>
        public Boolean Equals(EMobilityProviderAdminStatusType EMobilityProviderAdminStatusType)

            => String.Equals(InternalId,
                             EMobilityProviderAdminStatusType.InternalId,
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
