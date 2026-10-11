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
    /// Extension methods for transparency software versions.
    /// </summary>
    public static class TransparencySoftwareVersionExtensions
    {

        /// <summary>
        /// Indicates whether this transparency software version is null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareVersion">A transparency software version.</param>
        public static Boolean IsNullOrEmpty(this TransparencySoftwareVersion? TransparencySoftwareVersion)
            => !TransparencySoftwareVersion.HasValue || TransparencySoftwareVersion.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this transparency software version is NOT null or empty.
        /// </summary>
        /// <param name="TransparencySoftwareVersion">A transparency software version.</param>
        public static Boolean IsNotNullOrEmpty(this TransparencySoftwareVersion? TransparencySoftwareVersion)
            => TransparencySoftwareVersion.HasValue && TransparencySoftwareVersion.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique version of a transparency software.
    /// </summary>
    public readonly struct TransparencySoftwareVersion : IId<TransparencySoftwareVersion>
    {

        #region Data

        /// <summary>
        /// The internal version.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this transparency software version is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this transparency software version is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the transparency software version.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transparency software version based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a transparency software version.</param>
        private TransparencySoftwareVersion(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a transparency software version.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software version.</param>
        public static TransparencySoftwareVersion Parse(String Text)
        {

            if (TryParse(Text, out var transparencySoftwareVersion))
                return transparencySoftwareVersion;

            throw new ArgumentException($"Invalid text representation of a transparency software version: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a transparency software version.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software version.</param>
        public static TransparencySoftwareVersion? TryParse(String Text)
        {

            if (TryParse(Text, out var transparencySoftware))
                return transparencySoftware;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out TransparencySoftwareVersion)

        /// <summary>
        /// Try to parse the given text as a transparency software version.
        /// </summary>
        /// <param name="Text">A text representation of a transparency software version.</param>
        /// <param name="TransparencySoftwareVersion">The parsed transparency software version.</param>
        public static Boolean TryParse(String Text, out TransparencySoftwareVersion TransparencySoftwareVersion)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    TransparencySoftwareVersion = new TransparencySoftwareVersion(Text);
                    return true;
                }
                catch
                { }
            }

            TransparencySoftwareVersion = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this transparency software version.
        /// </summary>
        public TransparencySoftwareVersion Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (TransparencySoftwareVersion1, TransparencySoftwareVersion2)

        /// <summary>
        /// Compares two transparency software versions for equality.
        /// </summary>
        /// <param name="TransparencySoftwareVersion1">A transparency software version.</param>
        /// <param name="TransparencySoftwareVersion2">Another transparency software version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (TransparencySoftwareVersion TransparencySoftwareVersion1,
                                           TransparencySoftwareVersion TransparencySoftwareVersion2)

            => TransparencySoftwareVersion1.Equals(TransparencySoftwareVersion2);

        #endregion

        #region Operator != (TransparencySoftwareVersion1, TransparencySoftwareVersion2)

        /// <summary>
        /// Compares two transparency software versions for inequality.
        /// </summary>
        /// <param name="TransparencySoftwareVersion1">A transparency software version.</param>
        /// <param name="TransparencySoftwareVersion2">Another transparency software version.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (TransparencySoftwareVersion TransparencySoftwareVersion1,
                                           TransparencySoftwareVersion TransparencySoftwareVersion2)

            => !TransparencySoftwareVersion1.Equals(TransparencySoftwareVersion2);

        #endregion

        #region Operator <  (TransparencySoftwareVersion1, TransparencySoftwareVersion2)

        /// <summary>
        /// Compares two transparency software versions.
        /// </summary>
        /// <param name="TransparencySoftwareVersion1">A transparency software version.</param>
        /// <param name="TransparencySoftwareVersion2">Another transparency software version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (TransparencySoftwareVersion TransparencySoftwareVersion1,
                                          TransparencySoftwareVersion TransparencySoftwareVersion2)

            => TransparencySoftwareVersion1.CompareTo(TransparencySoftwareVersion2) < 0;

        #endregion

        #region Operator <= (TransparencySoftwareVersion1, TransparencySoftwareVersion2)

        /// <summary>
        /// Compares two transparency software versions.
        /// </summary>
        /// <param name="TransparencySoftwareVersion1">A transparency software version.</param>
        /// <param name="TransparencySoftwareVersion2">Another transparency software version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (TransparencySoftwareVersion TransparencySoftwareVersion1,
                                           TransparencySoftwareVersion TransparencySoftwareVersion2)

            => TransparencySoftwareVersion1.CompareTo(TransparencySoftwareVersion2) <= 0;

        #endregion

        #region Operator >  (TransparencySoftwareVersion1, TransparencySoftwareVersion2)

        /// <summary>
        /// Compares two transparency software versions.
        /// </summary>
        /// <param name="TransparencySoftwareVersion1">A transparency software version.</param>
        /// <param name="TransparencySoftwareVersion2">Another transparency software version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (TransparencySoftwareVersion TransparencySoftwareVersion1,
                                          TransparencySoftwareVersion TransparencySoftwareVersion2)

            => TransparencySoftwareVersion1.CompareTo(TransparencySoftwareVersion2) > 0;

        #endregion

        #region Operator >= (TransparencySoftwareVersion1, TransparencySoftwareVersion2)

        /// <summary>
        /// Compares two transparency software versions.
        /// </summary>
        /// <param name="TransparencySoftwareVersion1">A transparency software version.</param>
        /// <param name="TransparencySoftwareVersion2">Another transparency software version.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (TransparencySoftwareVersion TransparencySoftwareVersion1,
                                           TransparencySoftwareVersion TransparencySoftwareVersion2)

            => TransparencySoftwareVersion1.CompareTo(TransparencySoftwareVersion2) >= 0;

        #endregion

        #endregion

        #region IComparable<TransparencySoftwareVersion> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two transparency software versions.
        /// </summary>
        /// <param name="Object">A transparency software version to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftwareVersion transparencySoftwareVersion
                   ? CompareTo(transparencySoftwareVersion)
                   : throw new ArgumentException("The given object is not a transparency software version!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(TransparencySoftwareVersion)

        /// <summary>
        /// Compares two transparency software versions.
        /// </summary>
        /// <param name="TransparencySoftwareVersion">A transparency software version to compare with.</param>
        public Int32 CompareTo(TransparencySoftwareVersion TransparencySoftwareVersion)

            => String.Compare(InternalId,
                              TransparencySoftwareVersion.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<TransparencySoftwareVersion> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two transparency software versions for equality.
        /// </summary>
        /// <param name="Object">A transparency software version to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftwareVersion transparencySoftwareVersion &&
                   Equals(transparencySoftwareVersion);

        #endregion

        #region Equals(TransparencySoftwareVersion)

        /// <summary>
        /// Compares two transparency software versions for equality.
        /// </summary>
        /// <param name="TransparencySoftwareVersion">A transparency software version to compare with.</param>
        public Boolean Equals(TransparencySoftwareVersion TransparencySoftwareVersion)

            => String.Equals(InternalId,
                             TransparencySoftwareVersion.InternalId,
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
