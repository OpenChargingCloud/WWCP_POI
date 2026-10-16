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
    /// Extension methods for EU vehicle categories.
    /// </summary>
    public static class EUVehicleCategoryExtensions
    {

        /// <summary>
        /// Indicates whether this EU vehicle category is null or empty.
        /// </summary>
        /// <param name="EUVehicleCategory">An EU vehicle category.</param>
        public static Boolean IsNullOrEmpty(this EUVehicleCategory? EUVehicleCategory)
            => !EUVehicleCategory.HasValue || EUVehicleCategory.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this EU vehicle category is NOT null or empty.
        /// </summary>
        /// <param name="EUVehicleCategory">An EU vehicle category.</param>
        public static Boolean IsNotNullOrEmpty(this EUVehicleCategory? EUVehicleCategory)
            => EUVehicleCategory.HasValue && EUVehicleCategory.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// An EU vehicle category of a vehicle type-approval, e.g. M1 for passenger cars; other texts are read as well.
    /// </summary>
    public readonly struct EUVehicleCategory : IId<EUVehicleCategory>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this EU vehicle category is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this EU vehicle category is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the EU vehicle category.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EU vehicle category based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of an EU vehicle category.</param>
        private EUVehicleCategory(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as an EU vehicle category.
        /// </summary>
        /// <param name="Text">A text representation of an EU vehicle category.</param>
        public static EUVehicleCategory Parse(String Text)
        {

            if (TryParse(Text, out var eUVehicleCategory))
                return eUVehicleCategory;

            throw new ArgumentException($"Invalid text representation of an EU vehicle category: '" + Text + "'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as an EU vehicle category.
        /// </summary>
        /// <param name="Text">A text representation of an EU vehicle category.</param>
        public static EUVehicleCategory? TryParse(String Text)
        {

            if (TryParse(Text, out var eUVehicleCategory))
                return eUVehicleCategory;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out EUVehicleCategory)

        /// <summary>
        /// Try to parse the given text as an EU vehicle category.
        /// </summary>
        /// <param name="Text">A text representation of an EU vehicle category.</param>
        /// <param name="EUVehicleCategory">The parsed EU vehicle category.</param>
        public static Boolean TryParse(String Text, out EUVehicleCategory EUVehicleCategory)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    EUVehicleCategory = new EUVehicleCategory(Text);
                    return true;
                }
                catch
                { }
            }

            EUVehicleCategory = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this EU vehicle category.
        /// </summary>
        public EUVehicleCategory Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Static definitions

        /// <summary>
        /// Light motor vehicles with two, three or four wheels, Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L     { get; }
            = new ("L");

        /// <summary>
        /// Category L1e of Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L1    { get; }
            = new ("L1");

        /// <summary>
        /// Category L2e of Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L2    { get; }
            = new ("L2");

        /// <summary>
        /// Category L3e of Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L3    { get; }
            = new ("L3");

        /// <summary>
        /// Category L4e of Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L4    { get; }
            = new ("L4");

        /// <summary>
        /// Category L5e of Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L5    { get; }
            = new ("L5");

        /// <summary>
        /// Category L6e of Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L6    { get; }
            = new ("L6");

        /// <summary>
        /// Category L7e of Regulation (EU) No 168/2013
        /// </summary>
        public static EUVehicleCategory L7    { get; }
            = new ("L7");

        /// <summary>
        /// Motor vehicles carrying passengers, Regulation (EU) 2018/858
        /// </summary>
        public static EUVehicleCategory M     { get; }
            = new ("M");

        /// <summary>
        /// Passenger cars: at most eight seats besides the driver's
        /// </summary>
        public static EUVehicleCategory M1    { get; }
            = new ("M1");

        /// <summary>
        /// Buses: more than eight seats besides the driver's, at most 5 t
        /// </summary>
        public static EUVehicleCategory M2    { get; }
            = new ("M2");

        /// <summary>
        /// Buses: more than eight seats besides the driver's, more than 5 t
        /// </summary>
        public static EUVehicleCategory M3    { get; }
            = new ("M3");

        /// <summary>
        /// Motor vehicles carrying goods, Regulation (EU) 2018/858
        /// </summary>
        public static EUVehicleCategory N     { get; }
            = new ("N");

        /// <summary>
        /// Light goods vehicles: at most 3.5 t
        /// </summary>
        public static EUVehicleCategory N1    { get; }
            = new ("N1");

        /// <summary>
        /// Goods vehicles: more than 3.5 t, at most 12 t
        /// </summary>
        public static EUVehicleCategory N2    { get; }
            = new ("N2");

        /// <summary>
        /// Goods vehicles: more than 12 t
        /// </summary>
        public static EUVehicleCategory N3    { get; }
            = new ("N3");

        /// <summary>
        /// Trailers, Regulation (EU) 2018/858
        /// </summary>
        public static EUVehicleCategory O     { get; }
            = new ("O");

        /// <summary>
        /// Trailers of category O1
        /// </summary>
        public static EUVehicleCategory O1    { get; }
            = new ("O1");

        /// <summary>
        /// Trailers of category O2
        /// </summary>
        public static EUVehicleCategory O2    { get; }
            = new ("O2");

        /// <summary>
        /// Trailers of category O3
        /// </summary>
        public static EUVehicleCategory O3    { get; }
            = new ("O3");

        /// <summary>
        /// Trailers of category O4
        /// </summary>
        public static EUVehicleCategory O4    { get; }
            = new ("O4");

        /// <summary>
        /// Agricultural trailers, Regulation (EU) No 167/2013
        /// </summary>
        public static EUVehicleCategory R     { get; }
            = new ("R");

        /// <summary>
        /// Agricultural trailers of category R1
        /// </summary>
        public static EUVehicleCategory R1    { get; }
            = new ("R1");

        /// <summary>
        /// Agricultural trailers of category R2
        /// </summary>
        public static EUVehicleCategory R2    { get; }
            = new ("R2");

        /// <summary>
        /// Agricultural trailers of category R3
        /// </summary>
        public static EUVehicleCategory R3    { get; }
            = new ("R3");

        /// <summary>
        /// Agricultural trailers of category R4
        /// </summary>
        public static EUVehicleCategory R4    { get; }
            = new ("R4");

        /// <summary>
        /// Agricultural tractors on wheels, Regulation (EU) No 167/2013
        /// </summary>
        public static EUVehicleCategory T     { get; }
            = new ("T");

        /// <summary>
        /// Agricultural tractors of category T1
        /// </summary>
        public static EUVehicleCategory T1    { get; }
            = new ("T1");

        /// <summary>
        /// Agricultural tractors of category T2
        /// </summary>
        public static EUVehicleCategory T2    { get; }
            = new ("T2");

        /// <summary>
        /// Agricultural tractors of category T3
        /// </summary>
        public static EUVehicleCategory T3    { get; }
            = new ("T3");

        /// <summary>
        /// Agricultural tractors of category T4
        /// </summary>
        public static EUVehicleCategory T4    { get; }
            = new ("T4");

        #endregion


        #region Operator overloading

        #region Operator == (EUVehicleCategory1, EUVehicleCategory2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EUVehicleCategory1">An EU vehicle category.</param>
        /// <param name="EUVehicleCategory2">Another EU vehicle category.</param>
        /// <returns>true|false</returns>
        public static Boolean operator == (EUVehicleCategory EUVehicleCategory1,
                                           EUVehicleCategory EUVehicleCategory2)

            => EUVehicleCategory1.Equals(EUVehicleCategory2);

        #endregion

        #region Operator != (EUVehicleCategory1, EUVehicleCategory2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EUVehicleCategory1">An EU vehicle category.</param>
        /// <param name="EUVehicleCategory2">Another EU vehicle category.</param>
        /// <returns>true|false</returns>
        public static Boolean operator != (EUVehicleCategory EUVehicleCategory1,
                                           EUVehicleCategory EUVehicleCategory2)

            => !EUVehicleCategory1.Equals(EUVehicleCategory2);

        #endregion

        #region Operator <  (EUVehicleCategory1, EUVehicleCategory2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EUVehicleCategory1">An EU vehicle category.</param>
        /// <param name="EUVehicleCategory2">Another EU vehicle category.</param>
        /// <returns>true|false</returns>
        public static Boolean operator < (EUVehicleCategory EUVehicleCategory1,
                                          EUVehicleCategory EUVehicleCategory2)

            => EUVehicleCategory1.CompareTo(EUVehicleCategory2) < 0;

        #endregion

        #region Operator <= (EUVehicleCategory1, EUVehicleCategory2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EUVehicleCategory1">An EU vehicle category.</param>
        /// <param name="EUVehicleCategory2">Another EU vehicle category.</param>
        /// <returns>true|false</returns>
        public static Boolean operator <= (EUVehicleCategory EUVehicleCategory1,
                                           EUVehicleCategory EUVehicleCategory2)

            => EUVehicleCategory1.CompareTo(EUVehicleCategory2) <= 0;

        #endregion

        #region Operator >  (EUVehicleCategory1, EUVehicleCategory2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EUVehicleCategory1">An EU vehicle category.</param>
        /// <param name="EUVehicleCategory2">Another EU vehicle category.</param>
        /// <returns>true|false</returns>
        public static Boolean operator > (EUVehicleCategory EUVehicleCategory1,
                                          EUVehicleCategory EUVehicleCategory2)

            => EUVehicleCategory1.CompareTo(EUVehicleCategory2) > 0;

        #endregion

        #region Operator >= (EUVehicleCategory1, EUVehicleCategory2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EUVehicleCategory1">An EU vehicle category.</param>
        /// <param name="EUVehicleCategory2">Another EU vehicle category.</param>
        /// <returns>true|false</returns>
        public static Boolean operator >= (EUVehicleCategory EUVehicleCategory1,
                                           EUVehicleCategory EUVehicleCategory2)

            => EUVehicleCategory1.CompareTo(EUVehicleCategory2) >= 0;

        #endregion

        #endregion

        #region IComparable<EUVehicleCategory> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two EU vehicle categories.
        /// </summary>
        /// <param name="Object">An EU vehicle category to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is EUVehicleCategory eUVehicleCategory
                   ? CompareTo(eUVehicleCategory)
                   : throw new ArgumentException("The given object is not an EU vehicle category!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(EUVehicleCategory)

        /// <summary>
        /// Compares two EU vehicle categories.
        /// </summary>
        /// <param name="EUVehicleCategory">An EU vehicle category to compare with.</param>
        public Int32 CompareTo(EUVehicleCategory EUVehicleCategory)

            => String.Compare(InternalId,
                              EUVehicleCategory.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<EUVehicleCategory> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two EU vehicle categories for equality.
        /// </summary>
        /// <param name="Object">An EU vehicle category to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is EUVehicleCategory eUVehicleCategory &&
                   Equals(eUVehicleCategory);

        #endregion

        #region Equals(EUVehicleCategory)

        /// <summary>
        /// Compares two EU vehicle categories for equality.
        /// </summary>
        /// <param name="EUVehicleCategory">An EU vehicle category to compare with.</param>
        public Boolean Equals(EUVehicleCategory EUVehicleCategory)

            => String.Equals(InternalId,
                             EUVehicleCategory.InternalId,
                             StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => InternalId?.ToUpper().GetHashCode() ?? 0;

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
