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

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The format a measurement public key is given in, named explicitly: a 64 digit @id never tells how it was derived.
    /// </summary>
    public enum MeasurementKeyFormat
    {

        /// <summary>
        /// A DER encoded SubjectPublicKeyInfo, identified by its SHA-256, as a proof gives the keys of an energy meter.
        /// </summary>
        SPKIDER,

        /// <summary>
        /// A deterministically encoded COSE key, identified by its SHA-256 thumbprint (RFC 9679).
        /// </summary>
        COSEKey

    }


    /// <summary>
    /// Why a measurement public key is not bound to an EVSE (WWCP_POI.TS MeasurementKeyError).
    /// </summary>
    public enum MeasurementKeyProblem
    {

        /// <summary>
        /// The EVSE names no energy meter of its own.
        /// </summary>
        MissingMeter,

        /// <summary>
        /// The energy meter of the EVSE is not the one asked for.
        /// </summary>
        IdentityMismatch,

        /// <summary>
        /// Not exactly one key of the meter is selected.
        /// </summary>
        AmbiguousKey,

        /// <summary>
        /// A key of an algorithm or curve not supported yet: only ECDSA P-256 is.
        /// </summary>
        UnsupportedKey,

        /// <summary>
        /// A key not valid as given, e.g. not matching its @id.
        /// </summary>
        InvalidKey

    }


    /// <summary>
    /// A public key an energy meter signs its measurements with, as a verifier uses it (WWCP_POI.TS MeasurementPublicKey):
    /// an ECDSA P-256 key, as the first pilot supports, with its @id and how it is derived, and as a canonical COSE key with
    /// its thumbprint, the same of either format.
    /// </summary>
    public sealed class VerifiedMeasurementPublicKey
    {

        #region Data

        /// <summary>
        /// The scheme of an @id that is the SHA-256 of a DER encoded SubjectPublicKeyInfo.
        /// </summary>
        public const String SHA256OfSPKI           = "sha256-spki";

        /// <summary>
        /// The scheme of an @id that is the SHA-256 thumbprint of a COSE key (RFC 9679).
        /// </summary>
        public const String SHA256COSEThumbprint   = "sha256-cose-key-thumbprint";

        /// <summary>
        /// The algorithm of every key the first pilot supports.
        /// </summary>
        public const String ECDSAsecp256r1         = "ECDSA-secp256r1";

        private static readonly X9ECParameters      p256        = ECNamedCurveTable.GetByName("P-256");
        private static readonly ECDomainParameters  p256Domain  = new (p256);

        #endregion

        #region Properties

        /// <summary>
        /// The @id as given.
        /// </summary>
        public String                  Id              { get; }

        /// <summary>
        /// How the @id as given is derived: SHA256OfSPKI or SHA256COSEThumbprint.
        /// </summary>
        public String                  IdScheme        { get; }

        /// <summary>
        /// The SHA-256 thumbprint of the canonical COSE key, as lowercase hex: the same of either format.
        /// </summary>
        public String                  CanonicalId     { get; }

        /// <summary>
        /// The format it was given in.
        /// </summary>
        public MeasurementKeyFormat    SourceFormat    { get; }

        /// <summary>
        /// The canonical COSE key: the curve and its coordinates only.
        /// </summary>
        public COSEKey                 Key             { get; }

        /// <summary>
        /// The canonical COSE key, as Base64 of its CBOR.
        /// </summary>
        public String                  Value           { get; }

        /// <summary>
        /// The encodings of Value: COSE_Key, CBOR, base64.
        /// </summary>
        public ImmutableArray<String>  Encodings       { get; } = [ "COSE_Key", "CBOR", "base64" ];

        /// <summary>
        /// The algorithm: ECDSA-secp256r1.
        /// </summary>
        public String                  Algorithm       { get; } = ECDSAsecp256r1;

        #endregion

        #region (private) Constructor(s)

        private VerifiedMeasurementPublicKey(String                Id,
                                             String                IdScheme,
                                             MeasurementKeyFormat  SourceFormat,
                                             COSEKey               Key)
        {

            this.Id            = Id;
            this.IdScheme      = IdScheme;
            this.SourceFormat  = SourceFormat;
            this.Key           = Key;
            this.Value         = Convert.ToBase64String(Key.ToByteArray());
            this.CanonicalId   = Convert.ToHexStringLower(Key.Thumbprint());

        }

        #endregion


        #region (static) TryParse(JSON, Format, out Key, out Problem, out ErrorResponse)

        /// <summary>
        /// Try to read the given public key of an energy meter, { "@id", "key" }, in the given format: of a DER encoded
        /// SubjectPublicKeyInfo its @id must be its SHA-256, of a COSE key its thumbprint and its encoding deterministic; its
        /// algorithm ECDSA, its curve P-256 and its point uncompressed and on the curve. No other is guessed at.
        /// </summary>
        /// <param name="JSON">The public key, as an energy meter of a proof names it.</param>
        /// <param name="Format">The format it is given in.</param>
        /// <param name="Key">The key.</param>
        /// <param name="Problem">Why it is not.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken?                                                 JSON,
                                       MeasurementKeyFormat                                    Format,
                                       [NotNullWhen(true)]  out VerifiedMeasurementPublicKey?  Key,
                                       [NotNullWhen(false)] out MeasurementKeyProblem?         Problem,
                                       [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            Key = null;

            if (JSON is not JObject json                                                  ||
                json[MeasurementPublicKey.PropertyKeys.Id]?.Type  != JTokenType.String    ||
                json[MeasurementPublicKey.PropertyKeys.Key]?.Type != JTokenType.String)
            {
                Problem        = MeasurementKeyProblem.InvalidKey;
                ErrorResponse  = "A measurement public key is a JSON object with an @id and a key!";
                return false;
            }

            var id = json[MeasurementPublicKey.PropertyKeys.Id]!.Value<String>()!;

            Byte[] bytes;

            try
            {
                bytes = Convert.FromBase64String(json[MeasurementPublicKey.PropertyKeys.Key]!.Value<String>()!);
            }
            catch (FormatException)
            {
                Problem        = MeasurementKeyProblem.InvalidKey;
                ErrorResponse  = "The key of a measurement public key must be Base64!";
                return false;
            }

            try
            {

                if (Format == MeasurementKeyFormat.SPKIDER)
                {

                    var asn1 = Asn1Object.FromByteArray(bytes);

                    if (!asn1.GetDerEncoded().AsSpan().SequenceEqual(bytes))
                    {
                        Problem        = MeasurementKeyProblem.InvalidKey;
                        ErrorResponse  = "A measurement SubjectPublicKeyInfo must be DER encoded, and nothing after it!";
                        return false;
                    }

                    var spki        = SubjectPublicKeyInfo.GetInstance(asn1);
                    var algorithm   = spki.Algorithm;

                    if (algorithm.Algorithm.Id != X9ObjectIdentifiers.IdECPublicKey.Id ||
                        algorithm.Parameters is not DerObjectIdentifier curve          ||
                        curve.Id               != X9ObjectIdentifiers.Prime256v1.Id)
                    {
                        Problem        = MeasurementKeyProblem.UnsupportedKey;
                        ErrorResponse  = "The measurement-key pilot supports ECDSA P-256 only!";
                        return false;
                    }

                    var point = spki.PublicKey.GetOctets();

                    if (!TryGetUncompressed(point, out var x, out var y))
                    {
                        Problem        = MeasurementKeyProblem.InvalidKey;
                        ErrorResponse  = "A measurement key needs a valid uncompressed P-256 point!";
                        return false;
                    }

                    if (id != Convert.ToHexStringLower(SHA256.HashData(bytes)))
                    {
                        Problem        = MeasurementKeyProblem.InvalidKey;
                        ErrorResponse  = "The SHA-256 of the measurement SubjectPublicKeyInfo does not match its @id!";
                        return false;
                    }

                    Key = new VerifiedMeasurementPublicKey(id, SHA256OfSPKI, Format, CanonicalOf(x, y));

                }

                else
                {

                    if (!COSEKey.TryParse(bytes, out var cose, out var coseError))
                    {
                        Problem        = MeasurementKeyProblem.InvalidKey;
                        ErrorResponse  = $"The measurement COSE key is invalid: {coseError}";
                        return false;
                    }

                    if (cose.IsPrivate || cose.Curve != COSECurve.P256 || cose.X?.Length != 32 || cose.Y?.Length != 32)
                    {
                        Problem        = MeasurementKeyProblem.UnsupportedKey;
                        ErrorResponse  = "A public P-256 COSE measurement key is required!";
                        return false;
                    }

                    if (!TryGetUncompressed([ 0x04, .. cose.X, .. cose.Y ], out var x, out var y))
                    {
                        Problem        = MeasurementKeyProblem.InvalidKey;
                        ErrorResponse  = "The measurement point is not on P-256!";
                        return false;
                    }

                    if (!cose.ToByteArray().AsSpan().SequenceEqual(bytes) || id != Convert.ToHexStringLower(cose.Thumbprint()))
                    {
                        Problem        = MeasurementKeyProblem.InvalidKey;
                        ErrorResponse  = "The COSE key must be deterministic and match its thumbprint!";
                        return false;
                    }

                    Key = new VerifiedMeasurementPublicKey(id, SHA256COSEThumbprint, Format, CanonicalOf(x, y));

                }

            }
            catch (Exception e)
            {
                Problem        = MeasurementKeyProblem.InvalidKey;
                ErrorResponse  = $"Invalid measurement key: {e.Message}";
                return false;
            }

            Problem        = null;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (private static) TryGetUncompressed(Point, out X, out Y)

        /// <summary>
        /// The coordinates of the given uncompressed P-256 point, where it is one on the curve.
        /// </summary>
        private static Boolean TryGetUncompressed(Byte[]                            Point,
                                                  [NotNullWhen(true)] out Byte[]?   X,
                                                  [NotNullWhen(true)] out Byte[]?   Y)
        {

            X = Y = null;

            if (Point.Length != 65 || Point[0] != 0x04)
                return false;

            try
            {
                if (!p256.Curve.DecodePoint(Point).IsValid())
                    return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            X = Point[1..33];
            Y = Point[33..];
            return true;

        }

        #endregion

        #region (private static) CanonicalOf(X, Y)

        /// <summary>
        /// The canonical COSE key of the given coordinates: the curve and the coordinates only.
        /// </summary>
        private static COSEKey CanonicalOf(Byte[]  X,
                                           Byte[]  Y)

            => COSEKey.From(new ECPublicKeyParameters(p256Domain.Curve.DecodePoint([ 0x04, .. X, .. Y ]), p256Domain));

        #endregion

    }


    /// <summary>
    /// The evidence of a binding of a measurement public key to an EVSE (WWCP_POI.TS VerifiedMeterKeyBinding.evidence).
    /// </summary>
    /// <param name="RoamingNetworkId">The roaming network.</param>
    /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
    /// <param name="CommitId">The commit of the version of the proof.</param>
    /// <param name="Revision">The revision of the version of the proof.</param>
    /// <param name="TreeRoot">The root the proof leads to.</param>
    /// <param name="KeyStatementDigest">The digest of the key statement the trust context of the verifier was evaluated of.</param>
    /// <param name="VerifiedAt">When it was verified.</param>
    /// <param name="ValidUntil">Until when the observation holds at most, no lease.</param>
    /// <param name="Association">How the key is associated: VerifiedMeterKeyBinding.CurrentSnapshot or .HistoricalAttestation.</param>
    /// <param name="MeasurementTimes">Of a historical association, the time of the measurement it covers.</param>
    /// <param name="HistoricalStatementDigest">Of a historical association, the digest of the key statement of that time.</param>
    /// <param name="AttestationValidUntil">Of a historical association, until when the attestations of that time held.</param>
    public sealed record MeterKeyEvidence(RoamingNetwork_Id                 RoamingNetworkId,
                                          RoamingNetworkOperator_Id         RoamingNetworkOperatorId,
                                          RoamingNetworkCommit_Id?          CommitId,
                                          UInt64?                           Revision,
                                          ETag                              TreeRoot,
                                          String                            KeyStatementDigest,
                                          DateTimeOffset                    VerifiedAt,
                                          DateTimeOffset                    ValidUntil,
                                          String                            Association,
                                          ImmutableArray<DateTimeOffset>    MeasurementTimes            = default,
                                          String?                           HistoricalStatementDigest   = null,
                                          DateTimeOffset?                   AttestationValidUntil       = null);


    /// <summary>
    /// A measurement public key bound to an EVSE by a verified proof (WWCP_POI.TS verifyMeterKey): the energy meter of that
    /// EVSE, of the serial number asked for, and the one key of it asked for. A current snapshot is no evidence of when the
    /// meter was installed or its key rotated, nor a lease on local trust: a verifier verifies again for every use.
    /// </summary>
    /// <param name="EVSEId">The EVSE.</param>
    /// <param name="ChargingStationId">The charging station of the EVSE.</param>
    /// <param name="MeterId">The energy meter of the EVSE.</param>
    /// <param name="MeterSerialNumber">Its serial number.</param>
    /// <param name="Key">The key.</param>
    /// <param name="Evidence">The evidence of the binding.</param>
    public sealed record VerifiedMeterKeyBinding(String                        EVSEId,
                                                 String                        ChargingStationId,
                                                 String                        MeterId,
                                                 String                        MeterSerialNumber,
                                                 VerifiedMeasurementPublicKey  Key,
                                                 MeterKeyEvidence              Evidence)
    {

        /// <summary>
        /// The association of a key a current snapshot gives.
        /// </summary>
        public const String CurrentSnapshot        = "current-snapshot";

        /// <summary>
        /// The association of a key attested at the time of a measurement (HistoricalMeterEvidence).
        /// </summary>
        public const String HistoricalAttestation  = "historical-attestation";


        #region (static) TryVerify(TrustContext, Proof, EVSEId, MeterSerialNumber, Format, out Binding, out Problem, out ErrorResponse, MeterId = null, KeyId = null)

        /// <summary>
        /// Verify the given proof against the given trust context, evaluated of the decisions of the app, and bind the key of
        /// the energy meter of the given EVSE: its meter of the given serial number, and of the given @id where one is given,
        /// and exactly one key of it, the given one where several are named. No other EVSE or meter is looked at, nor a key
        /// tried until one verifies, nor a signing key of the CPO or the RNO taken instead.
        /// </summary>
        /// <param name="TrustContext">The trust context.</param>
        /// <param name="Proof">The proof of the charging station of the EVSE.</param>
        /// <param name="EVSEId">The EVSE.</param>
        /// <param name="MeterSerialNumber">The serial number of its meter, e.g. as the measurements name it.</param>
        /// <param name="Format">The format the key is given in.</param>
        /// <param name="Binding">The key bound to the EVSE.</param>
        /// <param name="Problem">Why it is not, where the key is.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="MeterId">The @id of the meter, if known.</param>
        /// <param name="KeyId">The @id of the key, where the meter names several.</param>
        public static Boolean TryVerify(RoamingNetworkTrustContext                         TrustContext,
                                        RoamingNetworkProof                                Proof,
                                        String                                             EVSEId,
                                        String                                             MeterSerialNumber,
                                        MeasurementKeyFormat                               Format,
                                        [NotNullWhen(true)]  out VerifiedMeterKeyBinding?  Binding,
                                        out MeasurementKeyProblem?                         Problem,
                                        [NotNullWhen(false)] out String?                   ErrorResponse,
                                        String?                                            MeterId   = null,
                                        String?                                            KeyId     = null)
        {

            Binding  = null;
            Problem  = null;

            if (!VerifiedChargingStation.TryVerify(TrustContext, Proof, out var station, out ErrorResponse, EVSEId: EVSEId))
                return false;

            if (station.Verification.ValidUntil < TrustContext.EvaluatedAt)
            {
                ErrorResponse = "The certified-key observation is no longer current!";
                return false;
            }

            if (station.EVSEs.FirstOrDefault(evse => VerifiedChargingStation.IdOf(evse) == EVSEId)?[EVSE.PropertyKeys.EnergyMeter] is not JObject meter)
            {
                Problem        = MeasurementKeyProblem.MissingMeter;
                ErrorResponse  = "The selected EVSE supplies no directly associated energy meter!";
                return false;
            }

            var meterId  = VerifiedChargingStation.IdOf(meter);
            var serial   = meter[EnergyMeter.PropertyKeys.SerialNumber]?.Type == JTokenType.String ? meter[EnergyMeter.PropertyKeys.SerialNumber]!.Value<String>()! : "";

            if (meterId.Length == 0 || serial.Length == 0 || MeterSerialNumber.Trim().Length == 0 || serial != MeterSerialNumber || (MeterId is not null && meterId != MeterId))
            {
                Problem        = MeasurementKeyProblem.IdentityMismatch;
                ErrorResponse  = "The signed meter identity does not match the selected EVSE meter!";
                return false;
            }

            var keys = (meter[EnergyMeter.PropertyKeys.PublicKeys] as JArray ?? []).
                           Where(key => KeyId is null || (key is JObject json && json[MeasurementPublicKey.PropertyKeys.Id]?.Type == JTokenType.String && json[MeasurementPublicKey.PropertyKeys.Id]!.Value<String>() == KeyId)).
                           ToArray();

            if (keys.Length != 1)
            {
                Problem        = MeasurementKeyProblem.AmbiguousKey;
                ErrorResponse  = "Select exactly one explicitly identified meter key; no trial verification across keys!";
                return false;
            }

            if (!VerifiedMeasurementPublicKey.TryParse(keys[0], Format, out var key, out Problem, out ErrorResponse))
                return false;

            Binding = new VerifiedMeterKeyBinding(EVSEId,
                                                  VerifiedChargingStation.IdOf(station.ChargingStation),
                                                  meterId,
                                                  serial,
                                                  key,
                                                  new MeterKeyEvidence(TrustContext.RoamingNetworkId,
                                                                       TrustContext.RoamingNetworkOperatorId,
                                                                       station.CommitId,
                                                                       station.Revision,
                                                                       station.Verification.ETag,
                                                                       TrustContext.KeyStatementDigest,
                                                                       station.Verification.VerifiedAt,
                                                                       station.Verification.ValidUntil,
                                                                       CurrentSnapshot));

            return true;

        }

        #endregion

    }

}
