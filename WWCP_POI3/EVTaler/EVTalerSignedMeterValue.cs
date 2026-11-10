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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a transparency software reads from a signed meter value it verified: the meter that signed it, when, and the
    /// energy register, in kWh.
    /// </summary>
    /// <param name="MeterSerialNumber">The serial number of the meter, as the value names it.</param>
    /// <param name="Timestamp">When it was measured.</param>
    /// <param name="Energy">The energy register, in kWh.</param>
    public sealed record EVTalerMeterReading(String          MeterSerialNumber,
                                             DateTimeOffset  Timestamp,
                                             Decimal         Energy);


    /// <summary>
    /// A transparency software verifying a signed meter value with the given public key, as it knows the format of the
    /// value, e.g. OCMF. WWCP_POI does not verify meter values itself: it chooses the key from the station proof and checks
    /// what the reading says (concepts/EVTaler.md, 5.4).
    /// </summary>
    /// <param name="Value">The signed meter value, as the station sent it.</param>
    /// <param name="Key">The measurement public key the station proof gives for it.</param>
    /// <param name="Reading">What the value says, when its signature is valid.</param>
    /// <param name="ErrorResponse">Why it is not valid.</param>
    public delegate Boolean EVTalerMeterValueVerifier(EVTalerSignedMeterValue                        Value,
                                                      MeasurementPublicKey                           Key,
                                                      [NotNullWhen(true)]  out EVTalerMeterReading?  Reading,
                                                      [NotNullWhen(false)] out String?               ErrorResponse);


    /// <summary>
    /// A signed meter value verified with a key a station proof gives for the meter of the EVSE or for the station.
    /// </summary>
    /// <param name="Value">The signed meter value.</param>
    /// <param name="Reading">What it says.</param>
    /// <param name="KeyId">The @id of the measurement public key it was verified with.</param>
    /// <param name="SignedBy">The @id of the energy meter or the charging station whose key it is.</param>
    public sealed record EVTalerVerifiedMeterValue(EVTalerSignedMeterValue  Value,
                                                   EVTalerMeterReading      Reading,
                                                   String                   KeyId,
                                                   String                   SignedBy);


    /// <summary>
    /// A signed meter value as a charging station sends it with OCPP 2.1 (SignedMeterValueType), e.g. at the start of a
    /// transaction (Transaction.Begin): the signed data as the meter gives it, Base64 encoded, how it is encoded and signed,
    /// and maybe the public key. It is kept and compared as sent.
    /// </summary>
    public sealed class EVTalerSignedMeterValue : IEquatable<EVTalerSignedMeterValue>
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// The JSON keys, as OCPP 2.1 names them.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The signed data, Base64 encoded.
            /// </summary>
            public const String  SignedMeterData   = "signedMeterData";

            /// <summary>
            /// How it is signed, e.g. "ECDSA-secp256r1-SHA256".
            /// </summary>
            public const String  SigningMethod     = "signingMethod";

            /// <summary>
            /// How the meter encodes it, e.g. "OCMF".
            /// </summary>
            public const String  EncodingMethod    = "encodingMethod";

            /// <summary>
            /// The public key, Base64 encoded, where the station sends it.
            /// </summary>
            public const String  PublicKey         = "publicKey";

        }

        #endregion

        #region Properties

        /// <summary>
        /// The signed data, Base64 encoded, as the station sent it.
        /// </summary>
        public String   SignedMeterData    { get; }

        /// <summary>
        /// How it is signed, if the station says so apart from the data.
        /// </summary>
        public String?  SigningMethod      { get; }

        /// <summary>
        /// How the meter encodes it, e.g. "OCMF".
        /// </summary>
        public String   EncodingMethod     { get; }

        /// <summary>
        /// The public key, Base64 encoded, if the station sent it: of the DER encoded SubjectPublicKeyInfo, it names which
        /// key of the proof verifies the value.
        /// </summary>
        public String?  PublicKey          { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a signed meter value as a charging station sent it.
        /// </summary>
        /// <param name="SignedMeterData">The signed data, canonical Base64.</param>
        /// <param name="EncodingMethod">How the meter encodes it, e.g. "OCMF".</param>
        /// <param name="SigningMethod">How it is signed, if the station says so.</param>
        /// <param name="PublicKey">The public key, canonical Base64, if the station sent it.</param>
        public EVTalerSignedMeterValue(String   SignedMeterData,
                                       String   EncodingMethod,
                                       String?  SigningMethod   = null,
                                       String?  PublicKey       = null)
        {

            this.SignedMeterData  = SignedMeterData;
            this.EncodingMethod   = EncodingMethod;
            this.SigningMethod    = SigningMethod;
            this.PublicKey        = PublicKey;

            var error = Invalid(this);

            if (error is not null)
                throw new ArgumentException(error);

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of this signed meter value, as OCPP 2.1 writes it.
        /// </summary>
        public JObject ToJSON()

            => JSONObject.Create(
                   new JProperty(PropertyKeys.SignedMeterData,  SignedMeterData),
                   SigningMethod is not null
                       ? new JProperty(PropertyKeys.SigningMethod,  SigningMethod)
                       : null,
                   new JProperty(PropertyKeys.EncodingMethod,   EncodingMethod),
                   PublicKey is not null
                       ? new JProperty(PropertyKeys.PublicKey,      PublicKey)
                       : null
               );

        #endregion

        #region (static) TryParse(JSON, out Value, out ErrorResponse)

        /// <summary>
        /// Try to read the given signed meter value, as OCPP 2.1 writes it: the signed data and the public key canonical
        /// Base64, the methods nonempty texts, nothing else.
        /// </summary>
        public static Boolean TryParse(JToken?                                            JSON,
                                       [NotNullWhen(true)]  out EVTalerSignedMeterValue?  Value,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            Value = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "A signed meter value must be a JSON object!";
                return false;
            }

            if (json.Properties().FirstOrDefault(property => property.Name is not PropertyKeys.SignedMeterData
                                                                          and not PropertyKeys.SigningMethod
                                                                          and not PropertyKeys.EncodingMethod
                                                                          and not PropertyKeys.PublicKey) is JProperty unknown)
            {
                ErrorResponse = $"A signed meter value has no '{unknown.Name}'!";
                return false;
            }

            if (new[] { PropertyKeys.SignedMeterData, PropertyKeys.EncodingMethod }.Any(name => json[name]?.Type != JTokenType.String) ||
                new[] { PropertyKeys.SigningMethod,   PropertyKeys.PublicKey      }.Any(name => json[name] is not null && json[name]!.Type != JTokenType.String))
            {
                ErrorResponse = "A signed meter value names its signed meter data and its encoding method, and maybe its signing method and its public key, as texts!";
                return false;
            }

            var value = new EVTalerSignedMeterValue(json, json[PropertyKeys.SignedMeterData]!.Value<String>()!,
                                                          json[PropertyKeys.EncodingMethod]! .Value<String>()!,
                                                          json[PropertyKeys.SigningMethod]?  .Value<String>(),
                                                          json[PropertyKeys.PublicKey]?      .Value<String>());

            ErrorResponse = Invalid(value);

            if (ErrorResponse is not null)
                return false;

            Value = value;
            return true;

        }

        // Read as written: the checks are those of the public constructor, made by TryParse.
        private EVTalerSignedMeterValue(JObject  _,
                                        String   SignedMeterData,
                                        String   EncodingMethod,
                                        String?  SigningMethod,
                                        String?  PublicKey)
        {

            this.SignedMeterData  = SignedMeterData;
            this.EncodingMethod   = EncodingMethod;
            this.SigningMethod    = SigningMethod;
            this.PublicKey        = PublicKey;

        }

        #endregion

        #region (static) TryVerify(Station, EVSEId, Value, Verifier, out Verified, out ErrorResponse)

        /// <summary>
        /// Verify the given signed meter value of the given EVSE with a measurement public key the station proof gives for
        /// the energy meter of the EVSE or for the charging station: the one the value names by its public key, else the
        /// only one there is; no key is tried after another. The reading must name the serial number of the meter or the
        /// station whose key it is.
        /// </summary>
        public static Boolean TryVerify(VerifiedChargingStation                              Station,
                                        String                                               EVSEId,
                                        EVTalerSignedMeterValue                              Value,
                                        EVTalerMeterValueVerifier                            Verifier,
                                        [NotNullWhen(true)]  out EVTalerVerifiedMeterValue?  Verified,
                                        [NotNullWhen(false)] out String?                     ErrorResponse)
        {

            Verified = null;

            var owners      = new List<JObject>();

            if (Station.EVSEs.FirstOrDefault(evse => VerifiedChargingStation.IdOf(evse) == EVSEId)?[EVSE.PropertyKeys.EnergyMeter] is JObject meter)
                owners.Add(meter);

            owners.Add(Station.ChargingStation);

            var candidates  = new List<(JObject Owner, MeasurementPublicKey Key)>();

            foreach (var owner in owners)
                foreach (var keyJSON in (owner[EnergyMeter.PropertyKeys.PublicKeys] as JArray ?? []).OfType<JObject>())
                {

                    if (!MeasurementPublicKey.TryParse(keyJSON, out var key, out var keyError))
                    {
                        ErrorResponse = $"A measurement public key of '{VerifiedChargingStation.IdOf(owner)}' in the proof is invalid: {keyError}";
                        return false;
                    }

                    candidates.Add((owner, key));

                }

            if (Value.PublicKey is not null)
            {
                var named = Convert.FromBase64String(Value.PublicKey);
                candidates = [.. candidates.Where(candidate => candidate.Key.Key.AsSpan().SequenceEqual(named))];
            }

            if (candidates.Count != 1)
            {
                ErrorResponse = candidates.Count == 0
                                    ? $"The proof gives no measurement public key of the meter of the EVSE '{EVSEId}' or of its station{(Value.PublicKey is not null ? " that is the one of the signed meter value" : "")}!"
                                    : $"The proof gives more than one measurement public key of the meter of the EVSE '{EVSEId}' and its station: the signed meter value has to name its own!";
                return false;
            }

            var (signedBy, measurementKey) = candidates[0];

            if (!Verifier(Value, measurementKey, out var reading, out var verifyError))
            {
                ErrorResponse = $"The signed meter value is not verified with the key '{measurementKey.Id}' of '{VerifiedChargingStation.IdOf(signedBy)}': {verifyError}";
                return false;
            }

            var serialNumber = signedBy[EnergyMeter.PropertyKeys.SerialNumber]?.Type == JTokenType.String
                                   ? signedBy[EnergyMeter.PropertyKeys.SerialNumber]!.Value<String>()!
                                   : null;

            if (serialNumber is null || reading.MeterSerialNumber != serialNumber)
            {
                ErrorResponse = $"The signed meter value names the serial number '{reading.MeterSerialNumber}', not the one '{serialNumber}' of '{VerifiedChargingStation.IdOf(signedBy)}' the proof gives!";
                return false;
            }

            Verified       = new EVTalerVerifiedMeterValue(Value, reading, measurementKey.Id, VerifiedChargingStation.IdOf(signedBy));
            ErrorResponse  = null;
            return true;

        }

        #endregion


        #region Equals / GetHashCode

        /// <summary>
        /// Two signed meter values are equal when the station sent the same, field by field.
        /// </summary>
        public Boolean Equals(EVTalerSignedMeterValue? Other)

            => Other is not null &&
               SignedMeterData == Other.SignedMeterData &&
               SigningMethod   == Other.SigningMethod   &&
               EncodingMethod  == Other.EncodingMethod  &&
               PublicKey       == Other.PublicKey;

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)
            => Equals(Object as EVTalerSignedMeterValue);

        /// <inheritdoc/>
        public override Int32 GetHashCode()
            => HashCode.Combine(SignedMeterData, SigningMethod, EncodingMethod, PublicKey);

        #endregion

        #region (private static) Invalid(Value)

        private static String? Invalid(EVTalerSignedMeterValue Value)
        {

            if (!IsCanonicalBase64(Value.SignedMeterData))
                return "The signed meter data must be nonempty canonical Base64!";

            if (Value.EncodingMethod.Trim().Length == 0 || Value.EncodingMethod.Trim() != Value.EncodingMethod)
                return "The encoding method of a signed meter value must be a nonempty text without surrounding whitespace!";

            if (Value.SigningMethod is not null && (Value.SigningMethod.Trim().Length == 0 || Value.SigningMethod.Trim() != Value.SigningMethod))
                return "The signing method of a signed meter value must be a nonempty text without surrounding whitespace!";

            if (Value.PublicKey is not null && !IsCanonicalBase64(Value.PublicKey))
                return "The public key of a signed meter value must be nonempty canonical Base64!";

            return null;

        }

        #endregion

        #region (private static) IsCanonicalBase64(Text)

        private static Boolean IsCanonicalBase64(String Text)
        {

            try
            {
                var bytes = Convert.FromBase64String(Text);
                return bytes.Length > 0 && Convert.ToBase64String(bytes) == Text;
            }
            catch (FormatException)
            {
                return false;
            }

        }

        #endregion

    }

}
