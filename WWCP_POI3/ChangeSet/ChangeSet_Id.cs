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
    /// Extension methods for change set identifications.
    /// </summary>
    public static class ChangeSetIdExtensions
    {

        /// <summary>
        /// Indicates whether this change set identification is null or empty.
        /// </summary>
        /// <param name="ChangeSetId">A change set identification.</param>
        public static Boolean IsNullOrEmpty(this ChangeSet_Id? ChangeSetId)
            => !ChangeSetId.HasValue || ChangeSetId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this change set identification is null or empty.
        /// </summary>
        /// <param name="ChangeSetId">A change set identification.</param>
        public static Boolean IsNotNullOrEmpty(this ChangeSet_Id? ChangeSetId)
            => ChangeSetId.HasValue && ChangeSetId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a change set.
    /// </summary>
    public readonly struct ChangeSet_Id : IId<ChangeSet_Id>
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
        /// The length of the change set identifier.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new change set identification.
        /// based on the given string.
        /// </summary>
        private ChangeSet_Id(String Text)
        {
            InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given string as a change set identification.
        /// </summary>
        /// <param name="Text">A text representation of a change set identification.</param>
        public static ChangeSet_Id Parse(String Text)
        {

            if (TryParse(Text, out ChangeSet_Id changeSetId))
                return changeSetId;

            throw new ArgumentException($"Invalid text representation of a change set identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given string as a change set identification.
        /// </summary>
        /// <param name="Text">A text representation of a change set identification.</param>
        public static ChangeSet_Id? TryParse(String Text)
        {

            if (TryParse(Text, out ChangeSet_Id changeSetId))
                return changeSetId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out ChangeSetId)

        /// <summary>
        /// Parse the given string as a change set identification.
        /// </summary>
        /// <param name="Text">A text representation of a change set identification.</param>
        /// <param name="ChangeSetId">The parsed change set identification.</param>
        public static Boolean TryParse(String Text, out ChangeSet_Id ChangeSetId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    ChangeSetId = new ChangeSet_Id(Text);
                    return true;
                }
                catch
                { }
            }

            ChangeSetId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this change set identification.
        /// </summary>
        public ChangeSet_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (ChangeSetId1, ChangeSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChangeSetId1">A change set identification.</param>
        /// <param name="ChangeSetId2">Another change set identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChangeSet_Id ChangeSetId1,
                                           ChangeSet_Id ChangeSetId2)

            => ChangeSetId1.Equals(ChangeSetId2);

        #endregion

        #region Operator != (ChangeSetId1, ChangeSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChangeSetId1">A change set identification.</param>
        /// <param name="ChangeSetId2">Another change set identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChangeSet_Id ChangeSetId1,
                                           ChangeSet_Id ChangeSetId2)

            => !ChangeSetId1.Equals(ChangeSetId2);

        #endregion

        #region Operator <  (ChangeSetId1, ChangeSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChangeSetId1">A change set identification.</param>
        /// <param name="ChangeSetId2">Another change set identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChangeSet_Id ChangeSetId1,
                                          ChangeSet_Id ChangeSetId2)

            => ChangeSetId1.CompareTo(ChangeSetId2) < 0;

        #endregion

        #region Operator <= (ChangeSetId1, ChangeSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChangeSetId1">A change set identification.</param>
        /// <param name="ChangeSetId2">Another change set identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChangeSet_Id ChangeSetId1,
                                           ChangeSet_Id ChangeSetId2)

            => ChangeSetId1.CompareTo(ChangeSetId2) <= 0;

        #endregion

        #region Operator >  (ChangeSetId1, ChangeSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChangeSetId1">A change set identification.</param>
        /// <param name="ChangeSetId2">Another change set identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChangeSet_Id ChangeSetId1,
                                          ChangeSet_Id ChangeSetId2)

            => ChangeSetId1.CompareTo(ChangeSetId2) > 0;

        #endregion

        #region Operator >= (ChangeSetId1, ChangeSetId2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChangeSetId1">A change set identification.</param>
        /// <param name="ChangeSetId2">Another change set identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChangeSet_Id ChangeSetId1,
                                           ChangeSet_Id ChangeSetId2)

            => ChangeSetId1.CompareTo(ChangeSetId2) >= 0;

        #endregion

        #endregion

        #region IComparable<ChangeSet_Id> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is ChangeSet_Id changeSetId
                   ? CompareTo(changeSetId)
                   : throw new ArgumentException("The given object is not a change set identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(ChangeSetId)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChangeSetId">An object to compare with.</param>
        public Int32 CompareTo(ChangeSet_Id ChangeSetId)

            => String.Compare(InternalId,
                              ChangeSetId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<ChangeSet_Id> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object? Object)

            => Object is ChangeSet_Id changeSetId &&
                   Equals(changeSetId);

        #endregion

        #region Equals(ChangeSetId)

        /// <summary>
        /// Compares two change set identifications for equality.
        /// </summary>
        /// <param name="ChangeSetId">A change set identification to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ChangeSet_Id ChangeSetId)

            => String.Equals(InternalId,
                             ChangeSetId.InternalId,
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
