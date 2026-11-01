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

using System.Security.Cryptography;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Measurement keys bound to an EVSE by a proof verified against a trust context, as WWCP_POI.TS 0.1.2-dev.0 binds them
    /// (tests/measurementKeys.test.ts), of its vector meter-session.json: the same keys, @ids and freshness.
    /// </summary>
    [TestFixture]
    public class MeterKeyTests
    {

        #region Data

        private const String EVSEId  = "DE*POI*E1*1";
        private const String Serial  = "CHARGY-POI-METER-001";

        private static readonly DateTimeOffset GeneratedAt = DateTimeOffset.Parse("2026-11-01T00:16:21.000Z");

        private JObject                     raw        = null!;
        private RoamingNetworkTrustAnchor   anchor     = null!;
        private RoamingNetworkKeyStatement  statement  = null!;
        private RoamingNetworkTrustContext  context    = null!;
        private RoamingNetworkProof         proof      = null!;

        #endregion

        [SetUp]
        public void SetUp()
        {

            raw        = TSVectors.Read("meter-session");

            var evidence   = (JObject) raw["poiEvidence"]!;
            var bootstrap  = (JObject) evidence["bootstrap"]!;

            anchor     = TSVectors.TrustAnchorOf(bootstrap);
            statement  = TSVectors.KeyStatementOf(bootstrap);
            context    = TSVectors.ContextOf(anchor, statement, GeneratedAt);
            proof      = TSVectors.ProofOf(evidence["chargingStationProof"]!);

        }


        #region Binds_the_meter_key_as_WWCP_POI_TS_does()

        /// <summary>
        /// The meter of the EVSE and its one key, with the @ids, the canonical COSE key, the commit and the conservative
        /// freshness WWCP_POI.TS gives of the same vector.
        /// </summary>
        [Test]
        public void Binds_the_meter_key_as_WWCP_POI_TS_does()
        {

            Assert.That(VerifiedMeterKeyBinding.TryVerify(context, proof, EVSEId, Serial, MeasurementKeyFormat.SPKIDER, out var binding, out var problem, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(problem,                                     Is.Null);
                Assert.That(binding!.ChargingStationId,                  Is.EqualTo("DE*POI*S1"));
                Assert.That(binding.MeterId,                             Is.EqualTo("DE*POI*M1"));
                Assert.That(binding.MeterSerialNumber,                   Is.EqualTo(Serial));

                Assert.That(binding.Key.Id,                              Is.EqualTo("0cfefc81f7537db1d2b85ae423bfa45835e664d6f4e77021f6adbc5b83c7e89c"));
                Assert.That(binding.Key.IdScheme,                        Is.EqualTo(VerifiedMeasurementPublicKey.SHA256OfSPKI));
                Assert.That(binding.Key.CanonicalId,                     Is.EqualTo("6a6bd5873a23a0eeb02d986504b6705df8584533e0135da9df52f790276efee7"));
                Assert.That(binding.Key.Value,                           Is.EqualTo("pAECIAEhWCDeP/XCjxLXmwJz5gARNJkoWgvLHbsxwy/xYYdgtY+LAiJYIH9FfvNaN7xI4M2W7YBnYNDOQoGOBNbaM/RMymcFXoEd"));
                Assert.That(binding.Key.Algorithm,                       Is.EqualTo("ECDSA-secp256r1"));

                Assert.That(binding.Evidence.Association,                Is.EqualTo(VerifiedMeterKeyBinding.CurrentSnapshot));
                Assert.That(binding.Evidence.CommitId.ToString(),        Is.EqualTo("7d3186d7ab4e17b44761f20cabe455873a59984350d6a41d8002d4ce7bfa2ad1"));
                Assert.That(binding.Evidence.Revision,                   Is.EqualTo(1));
                Assert.That(binding.Evidence.TreeRoot.ToString(),        Is.EqualTo("effectiveTree:sha256:hex:02474f54f5e5c322aae9b5af7c63a94c89dd4b35d055aea89d1fe69d5603855d"));
                Assert.That(binding.Evidence.KeyStatementDigest,         Is.EqualTo("f6ca6f926bf309ccb0a97982f28b788237f1cd669dc702805c3776365ff775d8"));
                Assert.That(binding.Evidence.VerifiedAt,                 Is.EqualTo(GeneratedAt));
                Assert.That(binding.Evidence.ValidUntil,                 Is.EqualTo(DateTimeOffset.Parse("2026-11-01T23:16:21.000Z")), "of the attestations, before the key statement");
                Assert.That(binding.Evidence.ValidUntil,                 Is.LessThan(context.ValidUntil));

            }

        }

        #endregion

        #region A_short_lived_certified_signing_key_caps_the_observation()

        /// <summary>
        /// A certified signing key ending a second later caps the observation, though its root attestation lasts longer.
        /// </summary>
        [Test]
        public void A_short_lived_certified_signing_key_caps_the_observation()
        {

            var changed      = (JObject) raw["poiEvidence"]!["chargingStationProof"]!.DeepClone();
            var certificate  = (JObject) changed["roamingNetwork"]!["operator"]!["signingKeyCertificates"]![0]!;

            certificate["signingKey"]!["notAfter"] = GeneratedAt.AddSeconds(1).ToISO8601();

            Assert.That(RoamingNetworkSigningKeyCertificate.TryParse(certificate, out var parsedCertificate, out var error),  Is.True, error);
            certificate["value"] = TSVectors.Sign(41, parsedCertificate!.GetSigningBytes());

            Assert.That(TSVectors.ProofOf(changed).TryCalculateETag(out var root, out error),  Is.True, error);

            var commitId = Convert.ToHexStringLower(SHA256.HashData(root.Digest.AsSpan()));
            changed["commitId"]  = commitId;
            changed["revision"]  = 2;

            var index = 0;
            foreach (var attestation in changed["attestations"]!.Cast<JObject>())
            {
                attestation["eTag"]      = root.ToJSON();
                attestation["commitId"]  = commitId;
                attestation["revision"]  = 2;
                Assert.That(RoamingNetworkAttestation.TryParse(attestation, out var parsed, out error),  Is.True, error);
                attestation["value"]     = TSVectors.Sign(index++ == 0 ? (Byte) 42 : (Byte) 44, parsed!.GetSigningBytes());
            }

            Assert.That(VerifiedMeterKeyBinding.TryVerify(context, TSVectors.ProofOf(changed), EVSEId, Serial, MeasurementKeyFormat.SPKIDER, out var binding, out _, out error),  Is.True, error);
            Assert.That(binding!.Evidence.ValidUntil,  Is.EqualTo(GeneratedAt.AddSeconds(1)));

        }

        #endregion

        #region Never_looks_for_another_EVSE_meter_or_key()

        /// <summary>
        /// Another EVSE, serial number, meter or key is refused, never searched for until a signature happens to work.
        /// </summary>
        [Test]
        public void Never_looks_for_another_EVSE_meter_or_key()
        {

            using (Assert.EnterMultipleScope())
            {

                Assert.That(VerifiedMeterKeyBinding.TryVerify(context, proof, "FOREIGN", Serial,    MeasurementKeyFormat.SPKIDER, out _, out _,           out _),                    Is.False);

                Assert.That(VerifiedMeterKeyBinding.TryVerify(context, proof, EVSEId,    "FOREIGN", MeasurementKeyFormat.SPKIDER, out _, out var problem, out var error),          Is.False);
                Assert.That(problem,  Is.EqualTo(MeasurementKeyProblem.IdentityMismatch));
                Assert.That(error,    Does.Contain("identity"));

                Assert.That(VerifiedMeterKeyBinding.TryVerify(context, proof, EVSEId,    Serial,    MeasurementKeyFormat.SPKIDER, out _, out problem,     out error, MeterId: "FOREIGN"),  Is.False);
                Assert.That(problem,  Is.EqualTo(MeasurementKeyProblem.IdentityMismatch));

                Assert.That(VerifiedMeterKeyBinding.TryVerify(context, proof, EVSEId,    Serial,    MeasurementKeyFormat.SPKIDER, out _, out problem,     out error, KeyId:   "FOREIGN"),  Is.False);
                Assert.That(problem,  Is.EqualTo(MeasurementKeyProblem.AmbiguousKey));
                Assert.That(error,    Does.Contain("exactly one"));

            }

        }

        #endregion

        #region SPKI_and_COSE_agree_on_the_canonical_identity()

        /// <summary>
        /// The canonical identity of a key is the same given as a SubjectPublicKeyInfo or as a COSE key, the @id schemes not.
        /// </summary>
        [Test]
        public void SPKI_and_COSE_agree_on_the_canonical_identity()
        {

            Assert.That(VerifiedMeterKeyBinding.TryVerify(context, proof, EVSEId, Serial, MeasurementKeyFormat.SPKIDER, out var binding, out _, out var error),  Is.True, error);

            var cose = new JObject(new JProperty("@id", binding!.Key.CanonicalId),
                                   new JProperty("key", binding.Key.Value));

            Assert.That(VerifiedMeasurementPublicKey.TryParse(cose, MeasurementKeyFormat.COSEKey, out var other, out _, out error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(other!.CanonicalId,  Is.EqualTo(binding.Key.CanonicalId));
                Assert.That(other.IdScheme,      Is.EqualTo(VerifiedMeasurementPublicKey.SHA256COSEThumbprint));
                Assert.That(other.Value,         Is.EqualTo(binding.Key.Value));
                Assert.That(other.Id,            Is.Not.EqualTo(binding.Key.Id));
            }

        }

        #endregion

        #region Refuses_substituted_material()

        /// <summary>
        /// Another @id, the material in the other format, and a SubjectPublicKeyInfo with a byte after it are refused.
        /// </summary>
        [Test]
        public void Refuses_substituted_material()
        {

            Assert.That(VerifiedMeterKeyBinding.TryVerify(context, proof, EVSEId, Serial, MeasurementKeyFormat.SPKIDER, out var binding, out _, out var error),  Is.True, error);

            var spki = raw["publicKey"]!.Value<String>()!;

            JObject Key(String Id, String Key) => new (new JProperty("@id", Id), new JProperty("key", Key));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(VerifiedMeasurementPublicKey.TryParse(Key(String.Concat(Enumerable.Repeat("ff", 32)), spki), MeasurementKeyFormat.SPKIDER, out _, out var problem, out error),  Is.False);
                Assert.That(problem,  Is.EqualTo(MeasurementKeyProblem.InvalidKey));
                Assert.That(error,    Does.Contain("SHA-256"));

                Assert.That(VerifiedMeasurementPublicKey.TryParse(Key(binding!.Key.Id, binding.Key.Value), MeasurementKeyFormat.SPKIDER, out _, out _, out _),  Is.False);
                Assert.That(VerifiedMeasurementPublicKey.TryParse(Key(binding.Key.Id,  spki),              MeasurementKeyFormat.COSEKey, out _, out _, out _),  Is.False);
                Assert.That(VerifiedMeasurementPublicKey.TryParse(Key(binding.Key.Id,  Convert.ToBase64String([ .. Convert.FromBase64String(spki), 0 ])), MeasurementKeyFormat.SPKIDER, out _, out _, out _),  Is.False);

            }

        }

        #endregion

        #region Refuses_changed_proofs_expired_attestations_and_too_little_trust()

        /// <summary>
        /// A changed proof, attestations ended at the time of a later context, and trust in one holder only are refused.
        /// </summary>
        [Test]
        public void Refuses_changed_proofs_expired_attestations_and_too_little_trust()
        {

            var forged = (JObject) POIJSON.Read(raw.ToString(Newtonsoft.Json.Formatting.None).Replace("Chargy POI demo station", "forged station"));
            var later  = TSVectors.ContextOf(anchor, statement, GeneratedAt.AddHours(25));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(forged.ToString(),  Does.Contain("forged station"));
                Assert.That(VerifiedMeterKeyBinding.TryVerify(context, TSVectors.ProofOf(forged["poiEvidence"]!["chargingStationProof"]!), EVSEId, Serial, MeasurementKeyFormat.SPKIDER, out _, out _, out _),  Is.False);
                Assert.That(VerifiedMeterKeyBinding.TryVerify(later,   proof,                                                                EVSEId, Serial, MeasurementKeyFormat.SPKIDER, out _, out _, out _),  Is.False);
                Assert.That(RoamingNetworkTrustContext.TryCreate(anchor, statement, anchor.Keys.Take(1), out _, out _, At: GeneratedAt),  Is.False);
            }

        }

        #endregion

    }

}
