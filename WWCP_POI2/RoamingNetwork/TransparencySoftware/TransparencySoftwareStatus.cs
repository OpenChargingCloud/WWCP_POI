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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Looks up a transparency software release by its identification.
    /// </summary>
    public delegate TransparencySoftware?             TransparencySoftwareLookupDelegate           (TransparencySoftware_Id             TransparencySoftwareId);

    /// <summary>
    /// Looks up an approval or compatibility document by its identification.
    /// </summary>
    public delegate TransparencySoftwareCertificate?  TransparencySoftwareCertificateLookupDelegate(TransparencySoftwareCertificate_Id  CertificateId);


    /// <summary>
    /// The legal status of a transparency software release for an energy meter,
    /// with an optional approval or compatibility document covering the release.
    /// </summary>
    public sealed partial class TransparencySoftwareStatus : IEquatable<TransparencySoftwareStatus>,
                                                             IComparable<TransparencySoftwareStatus>,
                                                             IComparable
    {

        #region Properties

        /// <summary>
        /// The transparency software release.
        /// </summary>
        [Mandatory]
        public TransparencySoftware                 TransparencySoftware      { get; }

        /// <summary>
        /// The legal status of the use of the software release with the energy meter.
        /// </summary>
        [Mandatory]
        public LegalStatus                          LegalStatus               { get; }

        /// <summary>
        /// The optional approval or compatibility document covering the software release.
        /// </summary>
        [Optional]
        public TransparencySoftwareCertificate?     Certificate               { get; }

        /// <summary>
        /// The identification of the software release.
        /// </summary>
        public TransparencySoftware_Id              TransparencySoftwareId
            => TransparencySoftware.Id;

        /// <summary>
        /// The identification of the optional document.
        /// </summary>
        public TransparencySoftwareCertificate_Id?  CertificateId
            => Certificate?.Id;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new legal status of a transparency software release.
        /// </summary>
        /// <param name="TransparencySoftware">The transparency software release.</param>
        /// <param name="LegalStatus">The legal status of the use of the software release.</param>
        /// <param name="Certificate">An optional document, which must cover the software release.</param>
        public TransparencySoftwareStatus(TransparencySoftware              TransparencySoftware,
                                          LegalStatus                       LegalStatus,
                                          TransparencySoftwareCertificate?  Certificate   = null)
        {

            ArgumentNullException.ThrowIfNull(TransparencySoftware);

            if (LegalStatus.IsNullOrEmpty)
                throw new ArgumentException("A legal status is required.", nameof(LegalStatus));

            if (Certificate is not null && !Certificate.Covers(TransparencySoftware.Id))
                throw new ArgumentException("The approval document does not cover this software release.", nameof(Certificate));

            this.TransparencySoftware  = TransparencySoftware;
            this.LegalStatus           = LegalStatus;
            this.Certificate           = Certificate;

        }

        #endregion


        #region ToJSON(CustomTransparencySoftwareStatusSerializer = null)

        /// <summary>
        /// Return a JSON representation of this object. The software release and
        /// its document are referenced by their identifications.
        /// </summary>
        /// <param name="CustomTransparencySoftwareStatusSerializer">A delegate to serialize custom transparency software status JSON objects.</param>
        public JObject ToJSON(CustomJObjectSerializerDelegate<TransparencySoftwareStatus>? CustomTransparencySoftwareStatusSerializer = null)
        {

            var json = JSONObject.Create(

                                 new JProperty("transparencySoftwareId",  TransparencySoftwareId.ToString()),
                                 new JProperty("legalStatus",             LegalStatus.           ToString()),

                           CertificateId.HasValue
                               ? new JProperty("certificateId",           CertificateId.Value.   ToString())
                               : null

                       );

            return CustomTransparencySoftwareStatusSerializer is not null
                       ? CustomTransparencySoftwareStatusSerializer(this, json)
                       : json;

        }

        #endregion

        #region Clone(TransparencySoftwareLookup, CertificateLookup)

        /// <summary>
        /// Copy this status with the software release and the document of another registry.
        /// </summary>
        /// <param name="TransparencySoftwareLookup">Looks up the software release.</param>
        /// <param name="CertificateLookup">Looks up the document.</param>
        public TransparencySoftwareStatus Clone(TransparencySoftwareLookupDelegate             TransparencySoftwareLookup,
                                                TransparencySoftwareCertificateLookupDelegate  CertificateLookup)

            => new (TransparencySoftwareLookup(TransparencySoftwareId) ??
                        throw new ArgumentException($"Unresolved software reference '{TransparencySoftwareId}' in the target network."),
                    LegalStatus,
                    CertificateId is { } certificateId
                        ? CertificateLookup(certificateId) ??
                              throw new ArgumentException($"Unresolved certificate reference '{certificateId}' in the target network.")
                        : null);

        #endregion


        #region Operator overloading

        /// <summary>
        /// Compares two legal statuses for equality.
        /// </summary>
        public static Boolean operator == (TransparencySoftwareStatus? TransparencySoftwareStatus1,
                                           TransparencySoftwareStatus? TransparencySoftwareStatus2)

            => Equals(TransparencySoftwareStatus1, TransparencySoftwareStatus2);

        /// <summary>
        /// Compares two legal statuses for inequality.
        /// </summary>
        public static Boolean operator != (TransparencySoftwareStatus? TransparencySoftwareStatus1,
                                           TransparencySoftwareStatus? TransparencySoftwareStatus2)

            => !Equals(TransparencySoftwareStatus1, TransparencySoftwareStatus2);

        /// <summary>
        /// Compares two legal statuses.
        /// </summary>
        public static Boolean operator <  (TransparencySoftwareStatus TransparencySoftwareStatus1,
                                           TransparencySoftwareStatus TransparencySoftwareStatus2)

            => TransparencySoftwareStatus1.CompareTo(TransparencySoftwareStatus2) < 0;

        /// <summary>
        /// Compares two legal statuses.
        /// </summary>
        public static Boolean operator <= (TransparencySoftwareStatus TransparencySoftwareStatus1,
                                           TransparencySoftwareStatus TransparencySoftwareStatus2)

            => TransparencySoftwareStatus1.CompareTo(TransparencySoftwareStatus2) <= 0;

        /// <summary>
        /// Compares two legal statuses.
        /// </summary>
        public static Boolean operator >  (TransparencySoftwareStatus TransparencySoftwareStatus1,
                                           TransparencySoftwareStatus TransparencySoftwareStatus2)

            => TransparencySoftwareStatus1.CompareTo(TransparencySoftwareStatus2) > 0;

        /// <summary>
        /// Compares two legal statuses.
        /// </summary>
        public static Boolean operator >= (TransparencySoftwareStatus TransparencySoftwareStatus1,
                                           TransparencySoftwareStatus TransparencySoftwareStatus2)

            => TransparencySoftwareStatus1.CompareTo(TransparencySoftwareStatus2) >= 0;

        #endregion

        #region IComparable<TransparencySoftwareStatus> Members

        /// <summary>
        /// Compares two legal statuses.
        /// </summary>
        /// <param name="Object">A legal status to compare with.</param>
        public Int32 CompareTo(Object? Object)

            => Object is TransparencySoftwareStatus transparencySoftwareStatus
                   ? CompareTo(transparencySoftwareStatus)
                   : throw new ArgumentException("The given object is not a transparency software status!", nameof(Object));

        /// <summary>
        /// Compares two legal statuses by their software release, legal status and document.
        /// </summary>
        /// <param name="TransparencySoftwareStatus">A legal status to compare with.</param>
        public Int32 CompareTo(TransparencySoftwareStatus? TransparencySoftwareStatus)
        {

            ArgumentNullException.ThrowIfNull(TransparencySoftwareStatus);

            var c = TransparencySoftwareId.CompareTo(TransparencySoftwareStatus.TransparencySoftwareId);

            if (c == 0)
                c = LegalStatus.           CompareTo(TransparencySoftwareStatus.LegalStatus);

            if (c == 0)
                c = Nullable.Compare(CertificateId, TransparencySoftwareStatus.CertificateId);

            return c;

        }

        #endregion

        #region IEquatable<TransparencySoftwareStatus> Members

        /// <summary>
        /// Compares two legal statuses for equality.
        /// </summary>
        /// <param name="Object">A legal status to compare with.</param>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftwareStatus transparencySoftwareStatus &&
                   Equals(transparencySoftwareStatus);

        /// <summary>
        /// Compares two legal statuses for equality by their software release, legal status and document.
        /// </summary>
        /// <param name="TransparencySoftwareStatus">A legal status to compare with.</param>
        public Boolean Equals(TransparencySoftwareStatus? TransparencySoftwareStatus)

            => TransparencySoftwareStatus is not null &&
               CompareTo(TransparencySoftwareStatus) == 0;

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Return the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()

            => HashCode.Combine(TransparencySoftwareId,
                                LegalStatus,
                                CertificateId);

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{TransparencySoftware.Name}: {LegalStatus}";

        #endregion

    }

}
