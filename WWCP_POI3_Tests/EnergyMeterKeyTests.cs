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
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Asn1.TeleTrust;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The signature methods and public keys of energy meters: how a meter signs its measurements, and with which
    /// keys, of curves COSE does not know as well, with their certificate chains.
    /// </summary>
    [TestFixture]
    public class EnergyMeterKeyTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-17T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EnergyMeter_Id              Meter1   = EnergyMeter_Id.            Parse("meter-1");

        #endregion

        #region (static) KeyOf(Curve) / CertifiedKey()

        /// <summary>
        /// The DER encoded SubjectPublicKeyInfo of a new key on the given named curve.
        /// </summary>
        private static Byte[] KeyOf(DerObjectIdentifier Curve)
        {

            var generator = new ECKeyPairGenerator();
            generator.Init(new ECKeyGenerationParameters(Curve, new SecureRandom()));

            return SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(generator.GenerateKeyPair().Public).GetDerEncoded();

        }

        /// <summary>
        /// The key of a meter, secp256r1, with its certificate and the one of the certification authority that issued it.
        /// </summary>
        private static (Byte[] Key, Byte[] Certificate, Byte[] CA) CertifiedKey()
        {

            var notBefore  = DateTimeOffset.UtcNow.AddDays(-1);
            var notAfter   = DateTimeOffset.UtcNow.AddYears(1);

            using var caKey     = ECDsa.Create(ECCurve.NamedCurves.nistP384);
            var caRequest       = new CertificateRequest("CN=Meter Manufacturer CA", caKey, HashAlgorithmName.SHA384);
            caRequest.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            using var ca        = caRequest.CreateSelfSigned(notBefore, notAfter);

            using var meterKey  = ECDsa.Create(ECCurve.NamedCurves.nistP256);
            var meterRequest    = new CertificateRequest("CN=meter-1", meterKey, HashAlgorithmName.SHA256);
            using var meter     = meterRequest.Create(ca, notBefore, notAfter, [ 1, 2, 3, 4 ]);

            return (meter.PublicKey.ExportSubjectPublicKeyInfo(), meter.RawData, ca.RawData);

        }

        #endregion

        #region (static) NewNetwork(Meter)

        private static RoamingNetwork NewNetwork(EnergyMeter Meter)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                Created:           Created,
                                                                                                                ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                         Created:  Created,
                                                                                                                                                         EVSEs:    [ new EVSE(E1,
                                                                                                                                                                              EnergyMeter:  Meter,
                                                                                                                                                                              Created:      Created) ]) ]) ]) ]).WithKeys();

        private static EnergyMeter MeterOf(RoamingNetwork Network)

            => Network.GetChargingStationOperatorById(GEF)!.EVSEs.Single().EnergyMeter!;

        private static ImmutableArray<POIElementPathSegment> Path(params (String PropertyName, String? ElementId)[] Segments)

            => [ .. Segments.Select(segment => new POIElementPathSegment(segment.PropertyName, segment.ElementId)) ];

        #endregion


        #region A_meter_names_its_signature_methods_and_keys_and_survives_JSON_JSON_text_and_CBOR()

        /// <summary>
        /// Keys of curves COSE does not know, secp192r1 and secp224k1, and a certified one; in the order of their
        /// identifications, as are the signature methods in the order of their names.
        /// </summary>
        [Test]
        public void A_meter_names_its_signature_methods_and_keys_and_survives_JSON_JSON_text_and_CBOR()
        {

            var (key, certificate, ca) = CertifiedKey();

            var keys   = new[] {
                             new MeasurementPublicKey(key, [ certificate, ca ]),
                             new MeasurementPublicKey(KeyOf(SecObjectIdentifiers.SecP192r1)),
                             new MeasurementPublicKey(KeyOf(SecObjectIdentifiers.SecP224k1)),
                             new MeasurementPublicKey(KeyOf(TeleTrusTObjectIdentifiers.BrainpoolP256R1))
                         };

            var rn1    = NewNetwork(new EnergyMeter(Meter1,
                                                    SignatureMethods:  [ SignatureMethod.Parse("ocmf"), SignatureMethod.Parse("FooCrypt01"), SignatureMethod.EDL40 ],
                                                    PublicKeys:        keys,
                                                    Created:           Created));

            var json   = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,                            out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var meter  = json.SelectToken("$.chargingStationOperators[0].chargingPools[0].chargingStations[0].EVSEs[0].energyMeter")!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromText!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                   Is.EqualTo(json.ToString()));

                Assert.That(meter["signatureMethods"]!.Values<String>(),                     Is.EqualTo(new[] { "EDL40", "FooCrypt01", "OCMF" }));
                Assert.That(meter["publicKeys"]!.Select(publicKey => publicKey["@id"]!.Value<String>()),
                            Is.EqualTo(keys.Select(publicKey => publicKey.Id).Order(StringComparer.Ordinal)));

                Assert.That(MeterOf(fromJSON).PublicKeys.Select(publicKey => publicKey.Algorithm),
                            Is.EquivalentTo(new[] { "secp256r1", "secp192r1", "secp224k1", "brainpoolP256r1" }));
                Assert.That(MeterOf(fromJSON).PublicKeys.Single(publicKey => publicKey.Algorithm == "secp256r1").CertificateChain.Length,
                            Is.EqualTo(2));

                Assert.That(keys[0].Id,                                                      Is.EqualTo(Convert.ToHexStringLower(SHA256.HashData(key))));
                Assert.That(MeterOf(fromJSON).SignatureMethods,                              Does.Contain(SignatureMethod.OCMF));

            }

        }

        #endregion

        #region Signature_methods_are_read_in_any_case_and_written_as_predefined()

        [Test]
        public void Signature_methods_are_read_in_any_case_and_written_as_predefined()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(SignatureMethod.Parse("ocmf").            ToString(),  Is.EqualTo("OCMF"));
                Assert.That(SignatureMethod.Parse(" alfencrypt03 ").  ToString(),  Is.EqualTo("AlfenCrypt03"));
                Assert.That(SignatureMethod.Parse("EMHCRYPT01").      ToString(),  Is.EqualTo("EMHCrypt01"));
                Assert.That(SignatureMethod.Parse("FooCrypt01").      ToString(),  Is.EqualTo("FooCrypt01"));
                Assert.That(SignatureMethod.Parse("foocrypt01"),                   Is.EqualTo(SignatureMethod.Parse("FooCrypt01")));
                Assert.That(SignatureMethod.TryParse("  "),                        Is.Null);

                // A signature method given twice, in another case as well, is refused.
                Assert.That(EnergyMeter.TryParse(new JObject(new JProperty("@id",               "meter-1"),
                                                             new JProperty("created",           "2026-01-01T00:00:00.000Z"),
                                                             new JProperty("signatureMethods",  new JArray("OCMF", "ocmf"))),
                                                 out _, out var twice),
                            Is.False);
                Assert.That(twice, Does.Contain("twice"));

            }

        }

        #endregion

        #region Invalid_public_keys_are_refused()

        [Test]
        public void Invalid_public_keys_are_refused()
        {

            var (key, certificate, ca) = CertifiedKey();

            var keyId    = Convert.ToHexStringLower(SHA256.HashData(key));
            var other    = KeyOf(SecObjectIdentifiers.SecP256r1);

            var generator = new ECKeyPairGenerator();
            generator.Init(new ECKeyGenerationParameters(SecObjectIdentifiers.SecP256r1, new SecureRandom()));
            var privateKey = PrivateKeyInfoFactory.CreatePrivateKeyInfo(generator.GenerateKeyPair().Private).GetDerEncoded();

            String? Refused(JObject JSON)
            {
                Assert.That(MeasurementPublicKey.TryParse(JSON, out _, out var error), Is.False, JSON.ToString());
                return error;
            }

            JObject KeyJSON(String Id, Byte[] Key, params Byte[][] Chain)
            {
                var json = new JObject(new JProperty("@id", Id), new JProperty("key", Convert.ToBase64String(Key)));
                if (Chain.Length > 0)
                    json.Add("certificateChain", new JArray(Chain.Select(Convert.ToBase64String)));
                return json;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(MeasurementPublicKey.TryParse(KeyJSON(keyId, key, certificate, ca), out var valid, out var error), Is.True, error);
                Assert.That(valid!.ToJSON(Embedded: true).ToString(),                                                Is.EqualTo(KeyJSON(keyId, key, certificate, ca).ToString()));

                Assert.That(Refused(KeyJSON(new String('0', 64), key)),                               Does.Contain("is not the SHA-256"));
                Assert.That(Refused(new JObject(new JProperty("@id", keyId),
                                                new JProperty("key", Convert.ToBase64String(key).Insert(4, "\n")))),
                                                                                                       Does.Contain("canonical Base64"));
                Assert.That(Refused(KeyJSON(Convert.ToHexStringLower(SHA256.HashData(privateKey)), privateKey)),
                                                                                                       Does.Contain("SubjectPublicKeyInfo"));
                Assert.That(Refused(KeyJSON(Convert.ToHexStringLower(SHA256.HashData(other)), other, certificate, ca)),
                                                                                                       Does.Contain("must be the one of the key"));
                Assert.That(Refused(KeyJSON(keyId, key, certificate, certificate)),                    Does.Contain("is not issued by"));
                Assert.That(Refused(KeyJSON(keyId, key, [ 1, 2, 3 ])),                                 Does.Contain("certificate 1"));

            }

        }

        #endregion

        #region Element_changes_add_a_key_and_a_signature_method_to_the_meter_of_an_EVSE()

        /// <summary>
        /// The owner of the EVSE adds them element by element, below its energy meter; a signature method that is
        /// there already in another case is there.
        /// </summary>
        [Test]
        public void Element_changes_add_a_key_and_a_signature_method_to_the_meter_of_an_EVSE()
        {

            var key   = new MeasurementPublicKey(KeyOf(SecObjectIdentifiers.SecP192r1));

            var rn1   = NewNetwork(new EnergyMeter(Meter1, SignatureMethods: [ SignatureMethod.OCMF ], Created: Created));
            var rn2   = rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([
                            RoamingNetworkChange.AddElement(nameof(EVSE), E1.ToString(), Path(("energyMeter", Meter1.ToString()), ("publicKeys",       key.Id)),  key.ToJSON()),
                            RoamingNetworkChange.AddElement(nameof(EVSE), E1.ToString(), Path(("energyMeter", Meter1.ToString()), ("signatureMethods", "EDL40")), "EDL40")
                        ], CreatedAt: T0));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(MeterOf(rn2).PublicKeys.Single(),                  Is.EqualTo(key));
                Assert.That(MeterOf(rn2).SignatureMethods,                     Is.EqualTo(new[] { SignatureMethod.EDL40, SignatureMethod.OCMF }));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.AddElement(nameof(EVSE), E1.ToString(), Path(("energyMeter", Meter1.ToString()), ("signatureMethods", "ocmf")), "ocmf") ],
                                                      CreatedAt: T0),
                            Throws.ArgumentException.With.Message.Contains("already exists"));

            }

        }

        #endregion

    }

}
