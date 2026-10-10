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
    /// Extension methods for grid connection point identifications.
    /// </summary>
    public static class GridConnectionPointIdExtensions
    {

        /// <summary>
        /// Indicates whether this grid connection point identification is null or empty.
        /// </summary>
        /// <param name="GridConnectionPointId">A grid connection point identification.</param>
        public static Boolean IsNullOrEmpty(this GridConnectionPoint_Id? GridConnectionPointId)
            => !GridConnectionPointId.HasValue || GridConnectionPointId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this grid connection point identification is NOT null or empty.
        /// </summary>
        /// <param name="GridConnectionPointId">A grid connection point identification.</param>
        public static Boolean IsNotNullOrEmpty(this GridConnectionPoint_Id? GridConnectionPointId)
            => GridConnectionPointId.HasValue && GridConnectionPointId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a grid connection point.
    /// </summary>
    public readonly struct GridConnectionPoint_Id : IId<GridConnectionPoint_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this grid connection point identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this grid connection point identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the grid connection point identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new grid connection point identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a grid connection point identification.</param>
        private GridConnectionPoint_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random grid connection point identification.
        /// </summary>
        /// <param name="Length">The expected length of the grid connection point identification.</param>
        public static GridConnectionPoint_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a grid connection point identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid connection point identification.</param>
        public static GridConnectionPoint_Id Parse(String Text)
        {

            if (TryParse(Text, out var gridConnectionPointId))
                return gridConnectionPointId;

            throw new ArgumentException($"Invalid text representation of a grid connection point identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a grid connection point identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid connection point identification.</param>
        public static GridConnectionPoint_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var gridConnectionPointId))
                return gridConnectionPointId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out GridConnectionPointId)

        /// <summary>
        /// Try to parse the given text as a grid connection point identification.
        /// </summary>
        /// <param name="Text">A text representation of a grid connection point identification.</param>
        /// <param name="GridConnectionPointId">The parsed grid connection point identification.</param>
        public static Boolean TryParse(String Text, out GridConnectionPoint_Id GridConnectionPointId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    GridConnectionPointId = new GridConnectionPoint_Id(Text);
                    return true;
                }
                catch
                { }
            }

            GridConnectionPointId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this grid connection point identification.
        /// </summary>
        public GridConnectionPoint_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (GridConnectionPointId1, GridConnectionPointId2)

        /// <summary>
        /// Compares two grid connection point identifications for equality.
        /// </summary>
        /// <param name="GridConnectionPointId1">A grid connection point identification.</param>
        /// <param name="GridConnectionPointId2">Another grid connection point identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (GridConnectionPoint_Id GridConnectionPointId1,
                                           GridConnectionPoint_Id GridConnectionPointId2)

            => GridConnectionPointId1.Equals(GridConnectionPointId2);

        #endregion

        #region Operator != (GridConnectionPointId1, GridConnectionPointId2)

        /// <summary>
        /// Compares two grid connection point identifications for inequality.
        /// </summary>
        /// <param name="GridConnectionPointId1">A grid connection point identification.</param>
        /// <param name="GridConnectionPointId2">Another grid connection point identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (GridConnectionPoint_Id GridConnectionPointId1,
                                           GridConnectionPoint_Id GridConnectionPointId2)

            => !GridConnectionPointId1.Equals(GridConnectionPointId2);

        #endregion

        #region Operator <  (GridConnectionPointId1, GridConnectionPointId2)

        /// <summary>
        /// Compares two grid connection point identifications.
        /// </summary>
        /// <param name="GridConnectionPointId1">A grid connection point identification.</param>
        /// <param name="GridConnectionPointId2">Another grid connection point identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (GridConnectionPoint_Id GridConnectionPointId1,
                                          GridConnectionPoint_Id GridConnectionPointId2)

            => GridConnectionPointId1.CompareTo(GridConnectionPointId2) < 0;

        #endregion

        #region Operator <= (GridConnectionPointId1, GridConnectionPointId2)

        /// <summary>
        /// Compares two grid connection point identifications.
        /// </summary>
        /// <param name="GridConnectionPointId1">A grid connection point identification.</param>
        /// <param name="GridConnectionPointId2">Another grid connection point identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (GridConnectionPoint_Id GridConnectionPointId1,
                                           GridConnectionPoint_Id GridConnectionPointId2)

            => GridConnectionPointId1.CompareTo(GridConnectionPointId2) <= 0;

        #endregion

        #region Operator >  (GridConnectionPointId1, GridConnectionPointId2)

        /// <summary>
        /// Compares two grid connection point identifications.
        /// </summary>
        /// <param name="GridConnectionPointId1">A grid connection point identification.</param>
        /// <param name="GridConnectionPointId2">Another grid connection point identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (GridConnectionPoint_Id GridConnectionPointId1,
                                          GridConnectionPoint_Id GridConnectionPointId2)

            => GridConnectionPointId1.CompareTo(GridConnectionPointId2) > 0;

        #endregion

        #region Operator >= (GridConnectionPointId1, GridConnectionPointId2)

        /// <summary>
        /// Compares two grid connection point identifications.
        /// </summary>
        /// <param name="GridConnectionPointId1">A grid connection point identification.</param>
        /// <param name="GridConnectionPointId2">Another grid connection point identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (GridConnectionPoint_Id GridConnectionPointId1,
                                           GridConnectionPoint_Id GridConnectionPointId2)

            => GridConnectionPointId1.CompareTo(GridConnectionPointId2) >= 0;

        #endregion

        #endregion

        #region IComparable<GridConnectionPointId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two grid connection point identifications.
        /// </summary>
        /// <param name="Object">A grid connection point identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is GridConnectionPoint_Id gridConnectionPointId
                   ? CompareTo(gridConnectionPointId)
                   : throw new ArgumentException("The given object is not a grid connection point identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(GridConnectionPointId)

        /// <summary>
        /// Compares two grid connection point identifications.
        /// </summary>
        /// <param name="GridConnectionPointId">A grid connection point identification to compare with.</param>
        public Int32 CompareTo(GridConnectionPoint_Id GridConnectionPointId)

            => String.Compare(InternalId,
                              GridConnectionPointId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<GridConnectionPointId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two grid connection point identifications for equality.
        /// </summary>
        /// <param name="Object">A grid connection point identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is GridConnectionPoint_Id gridConnectionPointId &&
                   Equals(gridConnectionPointId);

        #endregion

        #region Equals(GridConnectionPointId)

        /// <summary>
        /// Compares two grid connection point identifications for equality.
        /// </summary>
        /// <param name="GridConnectionPointId">A grid connection point identification to compare with.</param>
        public Boolean Equals(GridConnectionPoint_Id GridConnectionPointId)

            => String.Equals(InternalId,
                             GridConnectionPointId.InternalId,
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
