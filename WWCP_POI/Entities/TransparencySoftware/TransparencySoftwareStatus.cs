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

using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Static software applicability information for a meter, with an optional shared model/version document.
/// Legal status is static POI data, independent of operational runtime schedules.
/// </summary>
public sealed partial class TransparencySoftwareStatus : IEquatable<TransparencySoftwareStatus>,
    IComparable<TransparencySoftwareStatus>, IComparable
{
    /// <summary>
    /// The software release resolved from this network version's registry.
    /// </summary>
    public TransparencySoftware TransparencySoftware { get; }

    /// <summary>
    /// The referenced software release identity.
    /// </summary>
    public TransparencySoftware_Id TransparencySoftwareId => TransparencySoftware.Id;

    /// <summary>
    /// The declared legal status of the software's use with this meter.
    /// </summary>
    public LegalStatus LegalStatus { get; }

    /// <summary>
    /// Optional shared station model/version approval or compatibility document.
    /// </summary>
    public TransparencySoftwareCertificate? Certificate { get; }

    /// <summary>
    /// The referenced document identity, when supplied.
    /// </summary>
    public TransparencySoftwareCertificate_Id? CertificateId => Certificate?.Id;

    /// <summary>
    /// Create an immutable software assignment. A supplied document must cover this release.
    /// </summary>
    public TransparencySoftwareStatus(TransparencySoftware TransparencySoftware, LegalStatus LegalStatus,
                                      TransparencySoftwareCertificate? Certificate = null)
    {
        ArgumentNullException.ThrowIfNull(TransparencySoftware);
        if (LegalStatus.IsNullOrEmpty) throw new ArgumentException("A legal status is required.", nameof(LegalStatus));
        if (Certificate is not null && !Certificate.Covers(TransparencySoftware.Id))
            throw new ArgumentException("The approval document does not cover this software release.", nameof(Certificate));
        this.TransparencySoftware = TransparencySoftware; this.LegalStatus = LegalStatus; this.Certificate = Certificate;
    }

    /// <summary>
    /// Export software and approval document references without duplicating their descriptions.
    /// </summary>
    public JObject ToJSON(CustomJObjectSerializerDelegate<TransparencySoftwareStatus>? CustomTransparencySoftwareStatusSerializer = null)
    {
        var json = new JObject(new JProperty("transparencySoftwareId", TransparencySoftwareId.ToString()),
                               new JProperty("legalStatus", LegalStatus.ToString()));
        if (CertificateId is { } certificate) json["certificateId"] = certificate.ToString();
        return POIRepresentation.AddETags(this, CustomTransparencySoftwareStatusSerializer?.Invoke(this, json) ?? json);
    }

    /// <summary>
    /// Parse an assignment using software and certificate registries from the same network version.
    /// </summary>
    public static TransparencySoftwareStatus Parse(JObject JSON,
        CustomJObjectParserDelegate<TransparencySoftwareStatus>? CustomTransparencySoftwareStatusParser = null,
        RoamingNetwork? Network = null)
    {
        if (TryParse(JSON, out var result, out var error, CustomTransparencySoftwareStatusParser, Network)) return result;
        throw new ArgumentException(error, nameof(JSON));
    }

    /// <summary>
    /// Try to parse an assignment without a custom parser.
    /// </summary>
    public static Boolean TryParse(JObject JSON, [NotNullWhen(true)] out TransparencySoftwareStatus? result,
        [NotNullWhen(false)] out String? error)
        => TryParse(JSON, out result, out error, null);

    /// <summary>
    /// Try to parse an assignment with its registry context.
    /// </summary>
    public static Boolean TryParse(JObject JSON, [NotNullWhen(true)] out TransparencySoftwareStatus? result,
        [NotNullWhen(false)] out String? error,
        CustomJObjectParserDelegate<TransparencySoftwareStatus>? custom = null, RoamingNetwork? Network = null)
        => TryParseDocument(JSON, out result, out error, custom, Network);

    /// <summary>
    /// Copy this assignment, resolving shared descriptions in the target network when supplied.
    /// </summary>
    public TransparencySoftwareStatus Clone(RoamingNetwork? network = null)
    {
        var software = network is null ? TransparencySoftware :
            network.GetTransparencySoftwareById(TransparencySoftwareId) ??
            throw new ArgumentException($"Unresolved software reference '{TransparencySoftwareId}' in the target network.");
        var certificate = network is null ? Certificate : CertificateId is { } id ?
            network.GetTransparencySoftwareCertificateById(id) ??
            throw new ArgumentException($"Unresolved certificate reference '{id}' in the target network.") : null;
        return new(software, LegalStatus, certificate);
    }

    /// <summary>
    /// Compare static assignments by reference identity and legal status.
    /// </summary>
    public Int32 CompareTo(TransparencySoftwareStatus? other)
    {
        ArgumentNullException.ThrowIfNull(other);
        var c = TransparencySoftwareId.CompareTo(other.TransparencySoftwareId);
        if (c == 0) c = LegalStatus.CompareTo(other.LegalStatus);
        if (c == 0) c = Nullable.Compare(CertificateId, other.CertificateId);
        return c;
    }

    /// <summary>
    /// Compare with another assignment.
    /// </summary>
    public Int32 CompareTo(Object? other) => other is TransparencySoftwareStatus status ? CompareTo(status) :
        throw new ArgumentException("Expected a transparency software assignment.", nameof(other));

    /// <summary>
    /// Compare the stored reference identities and legal status.
    /// </summary>
    public Boolean Equals(TransparencySoftwareStatus? other) => other is not null && CompareTo(other) == 0;

    /// <summary>
    /// Compare with another assignment.
    /// </summary>
    public override Boolean Equals(Object? other) => other is TransparencySoftwareStatus status && Equals(status);

    /// <summary>
    /// Hash the same values used for equality.
    /// </summary>
    public override Int32 GetHashCode() => HashCode.Combine(TransparencySoftwareId, LegalStatus, CertificateId);

    /// <summary>
    /// Return a readable software applicability description.
    /// </summary>
    public override String ToString() => $"{TransparencySoftware.Name}: {LegalStatus}";

    public static Boolean operator ==(TransparencySoftwareStatus? left, TransparencySoftwareStatus? right) => Equals(left, right);
    public static Boolean operator !=(TransparencySoftwareStatus? left, TransparencySoftwareStatus? right) => !Equals(left, right);
    public static Boolean operator <(TransparencySoftwareStatus left, TransparencySoftwareStatus right) => left.CompareTo(right) < 0;
    public static Boolean operator >(TransparencySoftwareStatus left, TransparencySoftwareStatus right) => left.CompareTo(right) > 0;
    public static Boolean operator <=(TransparencySoftwareStatus left, TransparencySoftwareStatus right) => left.CompareTo(right) <= 0;
    public static Boolean operator >=(TransparencySoftwareStatus left, TransparencySoftwareStatus right) => left.CompareTo(right) >= 0;
}
