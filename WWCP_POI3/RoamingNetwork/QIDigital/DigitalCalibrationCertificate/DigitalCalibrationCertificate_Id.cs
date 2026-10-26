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
    /// Extension methods for digital calibration certificate identifications.
    /// </summary>
    public static class DigitalCalibrationCertificateIdExtensions
    {

        /// <summary>
        /// Indicates whether this digital calibration certificate identification is null or empty.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId">A digital calibration certificate identification.</param>
        public static Boolean IsNullOrEmpty(this DigitalCalibrationCertificate_Id? DigitalCalibrationCertificateId)
            => !DigitalCalibrationCertificateId.HasValue || DigitalCalibrationCertificateId.Value.IsNullOrEmpty;

        /// <summary>
        /// Indicates whether this digital calibration certificate identification is NOT null or empty.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId">A digital calibration certificate identification.</param>
        public static Boolean IsNotNullOrEmpty(this DigitalCalibrationCertificate_Id? DigitalCalibrationCertificateId)
            => DigitalCalibrationCertificateId.HasValue && DigitalCalibrationCertificateId.Value.IsNotNullOrEmpty;

    }


    /// <summary>
    /// The unique identification of a digital calibration certificate, assigned by its issuer.
    /// </summary>
    public readonly struct DigitalCalibrationCertificate_Id : IId<DigitalCalibrationCertificate_Id>
    {

        #region Data

        /// <summary>
        /// The internal identification.
        /// </summary>
        private readonly String InternalId;

        #endregion

        #region Properties

        /// <summary>
        /// Indicates whether this digital calibration certificate identification is null or empty.
        /// </summary>
        public Boolean IsNullOrEmpty
            => InternalId.IsNullOrEmpty();

        /// <summary>
        /// Indicates whether this digital calibration certificate identification is NOT null or empty.
        /// </summary>
        public Boolean IsNotNullOrEmpty
            => InternalId.IsNotNullOrEmpty();

        /// <summary>
        /// The length of the digital calibration certificate identification.
        /// </summary>
        public UInt64 Length
            => (UInt64) (InternalId?.Length ?? 0);

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new digital calibration certificate identification based on the given text.
        /// </summary>
        /// <param name="Text">The text representation of a digital calibration certificate identification.</param>
        private DigitalCalibrationCertificate_Id(String Text)
        {
            this.InternalId = Text;
        }

        #endregion


        #region (static) NewRandom(Length = 50)

        /// <summary>
        /// Create a new random digital calibration certificate identification.
        /// </summary>
        /// <param name="Length">The expected length of the digital calibration certificate identification.</param>
        public static DigitalCalibrationCertificate_Id NewRandom(Byte Length = 50)

            => new(RandomExtensions.RandomString(Length));

        #endregion

        #region (static) Parse   (Text)

        /// <summary>
        /// Parse the given text as a digital calibration certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital calibration certificate identification.</param>
        public static DigitalCalibrationCertificate_Id Parse(String Text)
        {

            if (TryParse(Text, out var digitalCalibrationCertificateId))
                return digitalCalibrationCertificateId;

            throw new ArgumentException($"Invalid text representation of a digital calibration certificate identification: '{Text}'!",
                                        nameof(Text));

        }

        #endregion

        #region (static) TryParse(Text)

        /// <summary>
        /// Try to parse the given text as a digital calibration certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital calibration certificate identification.</param>
        public static DigitalCalibrationCertificate_Id? TryParse(String Text)
        {

            if (TryParse(Text, out var digitalCalibrationCertificateId))
                return digitalCalibrationCertificateId;

            return null;

        }

        #endregion

        #region (static) TryParse(Text, out DigitalCalibrationCertificateId)

        /// <summary>
        /// Try to parse the given text as a digital calibration certificate identification.
        /// </summary>
        /// <param name="Text">A text representation of a digital calibration certificate identification.</param>
        /// <param name="DigitalCalibrationCertificateId">The parsed digital calibration certificate identification.</param>
        public static Boolean TryParse(String Text, out DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId)
        {

            Text = Text.Trim();

            if (Text.IsNotNullOrEmpty())
            {
                try
                {
                    DigitalCalibrationCertificateId = new DigitalCalibrationCertificate_Id(Text);
                    return true;
                }
                catch
                { }
            }

            DigitalCalibrationCertificateId = default;
            return false;

        }

        #endregion

        #region Clone()

        /// <summary>
        /// Clone this digital calibration certificate identification.
        /// </summary>
        public DigitalCalibrationCertificate_Id Clone()

            => new (
                   InternalId.CloneString()
               );

        #endregion


        #region Operator overloading

        #region Operator == (DigitalCalibrationCertificateId1, DigitalCalibrationCertificateId2)

        /// <summary>
        /// Compares two digital calibration certificate identifications for equality.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId1">A digital calibration certificate identification.</param>
        /// <param name="DigitalCalibrationCertificateId2">Another digital calibration certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId1,
                                           DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId2)

            => DigitalCalibrationCertificateId1.Equals(DigitalCalibrationCertificateId2);

        #endregion

        #region Operator != (DigitalCalibrationCertificateId1, DigitalCalibrationCertificateId2)

        /// <summary>
        /// Compares two digital calibration certificate identifications for inequality.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId1">A digital calibration certificate identification.</param>
        /// <param name="DigitalCalibrationCertificateId2">Another digital calibration certificate identification.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId1,
                                           DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId2)

            => !DigitalCalibrationCertificateId1.Equals(DigitalCalibrationCertificateId2);

        #endregion

        #region Operator <  (DigitalCalibrationCertificateId1, DigitalCalibrationCertificateId2)

        /// <summary>
        /// Compares two digital calibration certificate identifications.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId1">A digital calibration certificate identification.</param>
        /// <param name="DigitalCalibrationCertificateId2">Another digital calibration certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId1,
                                          DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId2)

            => DigitalCalibrationCertificateId1.CompareTo(DigitalCalibrationCertificateId2) < 0;

        #endregion

        #region Operator <= (DigitalCalibrationCertificateId1, DigitalCalibrationCertificateId2)

        /// <summary>
        /// Compares two digital calibration certificate identifications.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId1">A digital calibration certificate identification.</param>
        /// <param name="DigitalCalibrationCertificateId2">Another digital calibration certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId1,
                                           DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId2)

            => DigitalCalibrationCertificateId1.CompareTo(DigitalCalibrationCertificateId2) <= 0;

        #endregion

        #region Operator >  (DigitalCalibrationCertificateId1, DigitalCalibrationCertificateId2)

        /// <summary>
        /// Compares two digital calibration certificate identifications.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId1">A digital calibration certificate identification.</param>
        /// <param name="DigitalCalibrationCertificateId2">Another digital calibration certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId1,
                                          DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId2)

            => DigitalCalibrationCertificateId1.CompareTo(DigitalCalibrationCertificateId2) > 0;

        #endregion

        #region Operator >= (DigitalCalibrationCertificateId1, DigitalCalibrationCertificateId2)

        /// <summary>
        /// Compares two digital calibration certificate identifications.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId1">A digital calibration certificate identification.</param>
        /// <param name="DigitalCalibrationCertificateId2">Another digital calibration certificate identification.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId1,
                                           DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId2)

            => DigitalCalibrationCertificateId1.CompareTo(DigitalCalibrationCertificateId2) >= 0;

        #endregion

        #endregion

        #region IComparable<DigitalCalibrationCertificateId> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two digital calibration certificate identifications.
        /// </summary>
        /// <param name="Object">A digital calibration certificate identification to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is DigitalCalibrationCertificate_Id digitalCalibrationCertificateId
                   ? CompareTo(digitalCalibrationCertificateId)
                   : throw new ArgumentException("The given object is not a digital calibration certificate identification!",
                                                 nameof(Object));

        #endregion

        #region CompareTo(DigitalCalibrationCertificateId)

        /// <summary>
        /// Compares two digital calibration certificate identifications.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId">A digital calibration certificate identification to compare with.</param>
        public Int32 CompareTo(DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId)

            => String.Compare(InternalId,
                              DigitalCalibrationCertificateId.InternalId,
                              StringComparison.OrdinalIgnoreCase);

        #endregion

        #endregion

        #region IEquatable<DigitalCalibrationCertificateId> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two digital calibration certificate identifications for equality.
        /// </summary>
        /// <param name="Object">A digital calibration certificate identification to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is DigitalCalibrationCertificate_Id digitalCalibrationCertificateId &&
                   Equals(digitalCalibrationCertificateId);

        #endregion

        #region Equals(DigitalCalibrationCertificateId)

        /// <summary>
        /// Compares two digital calibration certificate identifications for equality.
        /// </summary>
        /// <param name="DigitalCalibrationCertificateId">A digital calibration certificate identification to compare with.</param>
        public Boolean Equals(DigitalCalibrationCertificate_Id DigitalCalibrationCertificateId)

            => String.Equals(InternalId,
                             DigitalCalibrationCertificateId.InternalId,
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
