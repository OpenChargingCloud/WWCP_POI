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

using System.Net;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Proofs
{

    /// <summary>
    /// Proofs of charging stations by the hash tree along the entities of the effective content, checked against
    /// attestations of the roaming network operator (SPECIFICATION.md, section 8).
    /// </summary>
    [TestFixture]
    public class StationProofTests
    {

        #region Data

        private static readonly Ed25519PrivateKeyParameters         RNOKey      = new (Convert.FromHexString("606162636465666768696a6b6c6d6e6f707172737475767778797a7b7c7d7e7f"), 0);
        private static readonly Ed25519PrivateKeyParameters         OtherKey    = new (Convert.FromHexString("808182838485868788898a8b8c8d8e8f909192939495969798999a9b9c9d9e9f"), 0);

        private static readonly DateTimeOffset                      Created     = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset                      T0          = DateTimeOffset.Parse("2026-10-19T12:00:00Z");

        private static readonly RoamingNetwork_Id                   RN          = RoamingNetwork_Id.                 Parse("rn1");
        private static readonly ChargingStationOperator_Id          GEF         = ChargingStationOperator_Id.        Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id          XYZ         = ChargingStationOperator_Id.        Parse("DE*XYZ");
        private static readonly ChargingStation_Id                  S1          = ChargingStation_Id.                Parse("DE*GEF*S1");
        private static readonly EVSE_Id                             E11         = EVSE_Id.                           Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                             E12         = EVSE_Id.                           Parse("DE*GEF*E1*2");
        private static readonly ChargingTariff_Id                   T1          = ChargingTariff_Id.                 Parse("DE*GEF*T1");
        private static readonly ChargingTariff_Id                   T2          = ChargingTariff_Id.                 Parse("DE*GEF*T2");
        private static readonly ChargingTariff_Id                   T3          = ChargingTariff_Id.                 Parse("DE*GEF*T3");
        private static readonly EVSETemplate_Id                     ET1         = EVSETemplate_Id.                   Parse("DE*GEF*ET1");
        private static readonly ChargingStationManufacturer_Id      ACME        = ChargingStationManufacturer_Id.    Parse("ACME");
        private static readonly EnergyMeterManufacturer_Id          Meters      = EnergyMeterManufacturer_Id.        Parse("METERS");
        private static readonly EnergyMeterManufacturer_Id          Others      = EnergyMeterManufacturer_Id.        Parse("OTHERS");
        private static readonly TransparencySoftware_Id             Chargy10    = TransparencySoftware_Id.           Parse("chargy-1.0");
        private static readonly TransparencySoftware_Id             Chargy11    = TransparencySoftware_Id.           Parse("chargy-1.1");
        private static readonly TransparencySoftwareCertificate_Id  Cert1       = TransparencySoftwareCertificate_Id.Parse("cert-1");
        private static readonly GridOperator_Id                     NET         = GridOperator_Id.                   Parse("DE*NET");
        private static readonly GridConnectionPoint_Id              GCP1        = GridConnectionPoint_Id.            Parse("gcp-1");
        private static readonly ParkingOperator_Id                  PAR         = ParkingOperator_Id.                Parse("DE*PAR");
        private static readonly ParkingSpace_Id                     Space1      = ParkingSpace_Id.                   Parse("space-1");

        internal static RoamingNetworkCommitSigner RNOSigner
            => new (RNOKey,   "rno",   COSEAlgorithm.Ed25519, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, "DE*RNO");

        internal static RoamingNetworkCommitSigner OtherSigner
            => new (OtherKey, "rno",   COSEAlgorithm.Ed25519, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator, "DE*RNO");

        /// <summary>
        /// The key of the roaming network operator, trusted independently of the network, e.g. pinned in the app.
        /// </summary>
        internal static readonly VerifyAttestationDelegate Trusted = attestation =>
            attestation.VerifySignature(RNOKey.GeneratePublicKey(), "rno", out _);

        #endregion


        #region NewNetwork()

        /// <summary>
        /// Two operators; DE*GEF with three charging tariffs, an EVSE template giving a maximum power and a charging tariff,
        /// two pools; its station S1 made by ACME, naming a parking space of DE*PAR, in a pool at the grid connection point
        /// gcp-1, with the EVSE E1*1 of the template, whose energy meter is made by METERS and names Chargy 1.0 with its
        /// certificate, and the EVSE E1*2 with the tariff T1 of its own.
        /// </summary>
        internal static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RN,
                   Created:                           Created,
                   ChargingStationManufacturers:      [ new ChargingStationManufacturer(ACME, Created: Created) ],
                   EnergyMeterManufacturers:          [ new EnergyMeterManufacturer(Meters, Created: Created),
                                                        new EnergyMeterManufacturer(Others, Created: Created) ],
                   TransparencySoftware:              [ NewRelease(Chargy10, "1.0"), NewRelease(Chargy11, "1.1") ],
                   TransparencySoftwareCertificates:  [ new TransparencySoftwareCertificate(Cert1,
                                                                                            "PTB",
                                                                                            ChargingStationModel_Id.Parse("Wallbox-22"),
                                                                                            "1.0",
                                                                                            [ Chargy10 ],
                                                                                            [ Chargy11 ],
                                                                                            ChargingStationManufacturerId:  ACME,
                                                                                            DocumentNumber:                 "DE-123",
                                                                                            NotBefore:                      Created) ],
                   GridOperators:                     [ new GridOperator(NET,
                                                                         GridConnectionPoints:  [ new GridConnectionPoint(GCP1, VoltageLevel: GridVoltageLevel.LowVoltage),
                                                                                                  new GridConnectionPoint(GridConnectionPoint_Id.Parse("gcp-2"), VoltageLevel: GridVoltageLevel.MediumVoltage) ],
                                                                         Created:               Created) ],
                   ParkingOperators:                  [ new ParkingOperator(PAR,
                                                                            Created:                Created,
                                                                            ParkingSpaces:          [ new ParkingSpace(Space1, TemplateIds: [ ParkingSpaceTemplate_Id.Parse("level-1") ], Created: Created),
                                                                                                      new ParkingSpace(ParkingSpace_Id.Parse("space-2"), Created: Created) ],
                                                                            ParkingSpaceTemplates:  [ new ParkingSpaceTemplate(ParkingSpaceTemplate_Id.Parse("level-1"),
                                                                                                                               Roofed:   true,
                                                                                                                               Created:  Created) ]) ],
                   ChargingStationOperators:          [ new ChargingStationOperator(
                                                            GEF,
                                                            Created:          Created,
                                                            ChargingTariffs:  [ NewTariff(T1, 0.39m), NewTariff(T2, 0.49m), NewTariff(T3, 0.59m) ],
                                                            EVSETemplates:    [ new EVSETemplate(ET1,
                                                                                                 MaxPower:          Watt.FromW(22000),
                                                                                                 ChargingTariffId:  T2,
                                                                                                 Created:           Created) ],
                                                            ChargingPools:    [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                 GridConnectionPointId:  GCP1,
                                                                                                 Created:                Created,
                                                                                                 ChargingStations:       [ new ChargingStation(S1,
                                                                                                                                               ManufacturerId:   ACME,
                                                                                                                                               ParkingSpaceIds:  [ Space1 ],
                                                                                                                                               Created:          Created,
                                                                                                                                               EVSEs:            [ new EVSE(E11,
                                                                                                                                                                            TemplateIds:  [ ET1 ],
                                                                                                                                                                            EnergyMeter:  new EnergyMeter(EnergyMeter_Id.Parse("meter-1"),
                                                                                                                                                                                                          ManufacturerId:              Meters,
                                                                                                                                                                                                          SerialNumber:                "M-1",
                                                                                                                                                                                                          TransparencySoftwareStatus:  [ new TransparencySoftwareAssignment(Chargy10, LegalStatus.GermanCalibrationLaw, Cert1) ],
                                                                                                                                                                                                          Created:                     Created),
                                                                                                                                                                            Created:      Created),
                                                                                                                                                                   new EVSE(E12,
                                                                                                                                                                            ChargingTariffIds:  [ T1 ],
                                                                                                                                                                            Created:            Created) ]),
                                                                                                                           new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S2"),
                                                                                                                                               Created:  Created,
                                                                                                                                               EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E2*1"), ChargingTariffIds: [ T3 ], Created: Created) ]) ]),
                                                                                new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P2"),
                                                                                                 Created:           Created,
                                                                                                 ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S3"), Created: Created) ]) ]),
                                                        new ChargingStationOperator(XYZ, Created: Created) ]).WithKeys();

        #endregion

        #region NewRelease(Id, Version) / NewTariff(Id, Price)

        private static TransparencySoftware NewRelease(TransparencySoftware_Id Id, String Version)

            => new (Id,
                    new ImmutableI18NString(I18NString.Create(Languages.en, "Chargy")),
                    TransparencySoftwareVersion.Parse(Version),
                    [ OpenSourceLicense.AGPL3 ],
                    "GraphDefined GmbH",
                    SourceCodeRepository: URL.Parse("https://github.com/OpenChargingCloud/ChargyDesktopApp"));

        private static ChargingTariff NewTariff(ChargingTariff_Id Id, Decimal Price)

            => new (Id,
                    Currency.EUR,
                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1) ]) ],
                    Created: Created);

        #endregion

        #region (static) Ids(Entities)

        private static String[] Ids(IEnumerable<JObject> Entities)

            => [ .. Entities.Select(entity => entity["@id"]!.Value<String>()!) ];

        #endregion


        #region Versions_and_commits_carry_the_effectiveTree_ETag()

        [Test]
        public void Versions_and_commits_carry_the_effectiveTree_ETag()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var version   = history.Head.RoamingNetwork;
            var genesis   = history.Head.Commit;
            var tree      = version.EffectiveTree(ETagHashAlgorithm.SHA256);

            var effective = version.ToEffectiveContentJSON();
            var stored    = version.ToJSON(IncludeETags: false);

            Assert.That(POIEffectiveTree.TryDigest((JObject) effective.DeepClone(), POIEffectiveTree.RoamingNetworkKind, ETagHashAlgorithm.SHA256, out var digest, out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(version.ETags.Select(eTag => eTag.Format),       Is.EqualTo(new[] { ETagFormat.JSON, ETagFormat.CBOR, ETagFormat.EffectiveTree }));
                Assert.That(version.ETags[2],                                Is.EqualTo(new ETag(ETagFormat.EffectiveTree, ETagHashAlgorithm.SHA256, digest)));
                Assert.That(version.ETags[2],                                Is.EqualTo(tree.ETag));
                Assert.That(genesis.ETags,                                   Is.EqualTo(version.ETags));
                Assert.That(genesis.Profile,                                 Is.EqualTo(RoamingNetworkCommit.TreeIdentityProfile));
                Assert.That(version.ETags[2].ToString(),                     Does.StartWith("effectiveTree:sha256:hex:"));

                // The effective content has the EVSE as it is at runtime, with the maximum power and tariff of its template.
                var effectiveEVSE  = effective["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;
                var storedEVSE     = stored   ["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;

                Assert.That(effectiveEVSE["maxPower"]?.Value<Decimal>(),     Is.EqualTo(22000m));
                Assert.That(effectiveEVSE["chargingTariffIds"]?[0]?.Value<String>(), Is.EqualTo(T2.ToString()));
                Assert.That(storedEVSE   ["maxPower"],                       Is.Null);

                // A reader recalculates the tree: a commit with another effectiveTree ETag is refused.
                Assert.That(version.MatchesCommitETags(genesis.ETags),                                                                         Is.True);
                Assert.That(version.MatchesCommitETags([ .. genesis.ETags.Take(2), new ETag(ETagFormat.EffectiveTree, ETagHashAlgorithm.SHA256, new Byte[32]) ]),  Is.False);
                Assert.That(version.MatchesCommitETags([ .. genesis.ETags.Take(2) ]),                                                          Is.True, "a commit made before effective trees");

            }

        }

        #endregion

        #region A_template_change_changes_the_tree()

        [Test]
        public void A_template_change_changes_the_tree()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());
            var before   = history.Head.RoamingNetwork;

            var template = before.ChargingStationOperators[0].EVSETemplates[0];

            var result   = history.Commit(before.CreateSignedChangeSet(
                                              [ RoamingNetworkChange.UpdateProperty(nameof(EVSETemplate), template.Id.ToString(), "maxPower", 22000, 11000) ],
                                              CreatedAt: T0));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            var after    = history.Head.RoamingNetwork;

            Assert.That(after.TryGetChargingStationOperatorById(GEF, out var chargingStationOperator), Is.True);
            Assert.That(chargingStationOperator!.TryGetEffectiveEVSE(E11, out var evse),                Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.EffectiveTreeETag(ETagHashAlgorithm.SHA256),  Is.Not.EqualTo(before.EffectiveTreeETag(ETagHashAlgorithm.SHA256)));
                Assert.That(result.Commit!.ETags,                               Does.Contain(after.EffectiveTreeETag(ETagHashAlgorithm.SHA256)));
                Assert.That(evse!.MaxPower?.Value,                                  Is.EqualTo(11000m));
            }

        }

        #endregion

        #region A_station_proof_gives_the_station_and_what_it_names()

        [Test]
        public void A_station_proof_gives_the_station_and_what_it_names()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork());

            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var error), Is.True, error);
            Assert.That(proof!.TryCalculateETag(out var eTag, out error),                    Is.True, error);

            var json     = proof.ToJSON();

            Assert.That(RoamingNetworkProof.TryParse(json.ToString(Newtonsoft.Json.Formatting.Indented), out var fromText, out error),  Is.True, error);
            Assert.That(RoamingNetworkProof.TryParse(proof.ToCBOR(),                                       out var fromCBOR, out error),  Is.True, error);

            var stations = proof.Given("chargingStationOperators", "chargingPools", "chargingStations").ToArray();
            var station  = stations.Single();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(eTag,                                                                     Is.EqualTo(history.Head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256)));
                Assert.That(proof.CommitId,                                                           Is.EqualTo(history.Head.Commit.Id));
                Assert.That(proof.Revision,                                                           Is.EqualTo(history.Head.Commit.Revision));
                Assert.That(json["@context"]!.Value<String>(),                                        Is.EqualTo(RoamingNetworkProof.JSONLDContext));
                Assert.That(json["hashAlgorithm"]!.Value<String>(),                                   Is.EqualTo("sha256"));

                // The roaming network with its own properties and its operator; one of two charging station operators.
                Assert.That(proof.RoamingNetwork["roamingNetworkOperator"],                           Is.Not.Null);
                Assert.That(Ids(proof.Given("chargingStationOperators")),                             Is.EqualTo(new[] { "DE*GEF" }));
                Assert.That(proof.RoamingNetwork["chargingStationOperators"]!["@size"]!.Value<Int32>(), Is.EqualTo(2));

                // The station completely, with its effective EVSEs, and nothing else of its pool and its operator.
                Assert.That(station["@id"]!.Value<String>(),                                          Is.EqualTo(S1.ToString()));
                Assert.That(station["EVSEs"] is JArray,                                               Is.True, "its EVSEs completely");
                Assert.That(Ids(station["EVSEs"]!.OfType<JObject>()),                                 Is.EqualTo(new[] { E11.ToString(), E12.ToString() }));
                Assert.That(station["EVSEs"]![0]!["maxPower"]!.Value<Decimal>(),                      Is.EqualTo(22000m));
                Assert.That(station["EVSEs"]![0]!["energyMeter"]!["serialNumber"]!.Value<String>(),   Is.EqualTo("M-1"));
                Assert.That(Ids(proof.Given("chargingStationOperators", "chargingPools")),            Is.EqualTo(new[] { "DE*GEF*P1" }));
                Assert.That(proof.Given("chargingStationOperators", "chargingPools").Single()["chargingStations"]!["@size"]!.Value<Int32>(), Is.EqualTo(2));

                // The tariffs its EVSEs take, its own and the one of the template; not the one of the other station.
                Assert.That(Ids(proof.Given("chargingStationOperators", "chargingTariffs")),          Is.EqualTo(new[] { T1.ToString(), T2.ToString() }));
                Assert.That(proof.Given("chargingStationOperators").Single()["EVSETemplates"]!["@merkleRoot"], Is.Not.Null, "templates are pruned");

                // What the station and its energy meters name.
                Assert.That(Ids(proof.Given("chargingStationManufacturers")),                         Is.EqualTo(new[] { "ACME" }));
                Assert.That(Ids(proof.Given("energyMeterManufacturers")),                             Is.EqualTo(new[] { "METERS" }));
                Assert.That(Ids(proof.Given("transparencySoftware")),                                 Is.EqualTo(new[] { "chargy-1.0" }));
                Assert.That(Ids(proof.Given("transparencySoftwareCertificates")),                     Is.EqualTo(new[] { "cert-1" }));
                Assert.That(Ids(proof.Given("parkingOperators", "parkingSpaces")),                    Is.EqualTo(new[] { "space-1" }));
                Assert.That(proof.Given("parkingOperators", "parkingSpaces").Single()["roofed"]?.Value<Boolean>(), Is.True, "the effective parking space");
                Assert.That(Ids(proof.Given("gridOperators", "gridConnectionPoints")),                Is.EqualTo(new[] { "gcp-1" }));

                // Read again, from text and from CBOR, it leads to the same root.
                Assert.That(fromText!.Verify(eTag, out error),                                        Is.True, error);
                Assert.That(fromCBOR!.Verify(eTag, out error),                                        Is.True, error);

            }

        }

        #endregion

        #region The_proof_of_an_EVSE_is_the_proof_of_its_station()

        [Test]
        public void The_proof_of_an_EVSE_is_the_proof_of_its_station()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());

            Assert.That(history.TryGetChargingStationProof(S1,  out var stationProof, out var error),  Is.True, error);
            Assert.That(history.TryGetChargingStationProof(E12, out var evseProof,    out error),      Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(JToken.DeepEquals(evseProof!.ToJSON(), stationProof!.ToJSON()),                                         Is.True);
                Assert.That(history.TryGetChargingStationProof(ChargingStation_Id.Parse("DE*GEF*S9"), out _, out error),             Is.False);
                Assert.That(error,                                                                                                    Does.Contain("unknown"));
                Assert.That(history.TryGetChargingStationProof(S1, out _, out error, ETagHashAlgorithm.SHA384),                      Is.False, "a hash algorithm the network does not name");
            }

        }

        #endregion

        #region A_proof_changed_leads_elsewhere()

        [Test]
        public void A_proof_changed_leads_elsewhere()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            var root    = history.Head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256);

            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var error), Is.True, error);

            RoamingNetworkProof Changed(Action<JObject> Change)
            {
                var json = (JObject) proof!.ToJSON().DeepClone();
                Change((JObject) json["roamingNetwork"]!);
                return RoamingNetworkProof.TryParse(json, out var changed, out var parseError) ? changed : throw new AssertionException(parseError);
            }

            static JObject Station(JObject Network)
                => (JObject) Network["chargingStationOperators"]!["@leaves"]![0]!["@entity"]!["chargingPools"]!["@leaves"]![0]!["@entity"]!["chargingStations"]!["@leaves"]![0]!["@entity"]!;

            var morePower     = Changed(network => Station(network)["EVSEs"]![0]!["maxPower"] = 50000);
            var lessEVSEs     = Changed(network => ((JArray) Station(network)["EVSEs"]!).RemoveAt(1));
            var otherIndex    = Changed(network => network["chargingStationOperators"]!["@leaves"]![0]!["@index"] = 1);
            var shortPath     = Changed(network => ((JArray) network["chargingStationOperators"]!["@leaves"]![0]!["@auditPath"]!).RemoveAt(0));
            var badHash       = Changed(network => network["eMobilityProviders"] = new JObject(new JProperty("@merkleRoot", "00"), new JProperty("@size", 1)));
            var otherTariff   = Changed(network => network["chargingStationOperators"]!["@leaves"]![0]!["@entity"]!["chargingTariffs"]!["@leaves"]![0]!["@entity"]!["elements"]![0]!["priceComponents"]![0]!["price"] = 0.01m);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(proof!.Verify(root, out error),       Is.True, error);

                Assert.That(morePower.  Verify(root, out error),  Is.False, "an EVSE given other");
                Assert.That(lessEVSEs.  Verify(root, out error),  Is.False, "an EVSE left out");
                Assert.That(otherIndex. Verify(root, out error),  Is.False, "the operator at another index");
                Assert.That(shortPath.  Verify(root, out error),  Is.False, "an audit path too short");
                Assert.That(badHash.    Verify(root, out error),  Is.False, "a pruned array of a digest too short");
                Assert.That(error,                                Does.Contain("eMobilityProviders"));
                Assert.That(otherTariff.Verify(root, out error),  Is.False, "a tariff given other");

            }

        }

        #endregion

        #region Attestations_of_the_head_verify_its_proofs()

        [Test]
        public void Attestations_of_the_head_verify_its_proofs()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());

            history.AttestationSigners = [ RNOSigner ];

            Assert.That(history.TryAttest(out var attestations, out var error, T0), Is.True, error);

            var attestation  = attestations!.Single();

            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out error, At: T0.AddMinutes(1)),  Is.True, error);
            Assert.That(RoamingNetworkAttestation.TryParse(attestation.ToJSON().ToString(), out var parsed, out error), Is.True, error);

            var forged = new RoamingNetworkAttestation(attestation.RoamingNetworkId, attestation.CommitId, attestation.Revision, attestation.ETag,
                                                       attestation.Role, attestation.Signer, attestation.SignedAt, attestation.ValidUntil.Value.AddDays(1),
                                                       attestation.Subject, attestation.Algorithm, attestation.KeyId, attestation.Value);

            var byOther = OtherSigner.Attest(RN, attestation.CommitId, attestation.Revision, attestation.ETag, T0, T0.AddMinutes(15));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(attestation.ETag,                                                             Is.EqualTo(history.Head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256)));
                Assert.That(attestation.CommitId,                                                         Is.EqualTo(history.Head.Commit.Id));
                Assert.That(attestation.SignedAt,                                                         Is.EqualTo(T0));
                Assert.That(attestation.ValidUntil,                                                       Is.EqualTo(T0.AddMinutes(15)));
                Assert.That(attestation.ToJSON()["profile"]!.Value<String>(),                             Is.EqualTo("wwcp-poi-attestation-v1"));

                Assert.That(proof!.Attestations,                                                          Has.Length.EqualTo(1));
                Assert.That(proof.Verify(Trusted, out var verifiedBy, out error, T0.AddMinutes(1)),       Is.True, error);
                Assert.That(verifiedBy!.Value,                                                            Is.EqualTo(attestation.Value));

                // Expired, or by a key nobody trusts, or with its validity stretched: the proof is not attested.
                Assert.That(proof.Verify(Trusted, out _, out error, T0.AddMinutes(16)),                   Is.False, "after its validUntil");
                Assert.That(proof.Verify(_ => false, out _, out error, T0.AddMinutes(1)),                 Is.False, "no trusted key");
                Assert.That(new RoamingNetworkProof(proof.HashAlgorithm, proof.RoamingNetwork, proof.CommitId, proof.Revision, [ byOther ]).Verify(Trusted, out _, out error, T0.AddMinutes(1)), Is.False, "another key");
                Assert.That(forged.VerifySignature(RNOKey.GeneratePublicKey(), "rno", out _),             Is.False, "another validity");

                Assert.That(parsed!.VerifySignature(RNOKey.GeneratePublicKey(), "rno", out error),        Is.True, error);
                Assert.That(JToken.DeepEquals(parsed.ToJSON(), attestation.ToJSON()),                     Is.True);

            }

        }

        #endregion

        #region The_head_is_attested_when_it_changes_and_again_in_time()

        [Test]
        public void The_head_is_attested_when_it_changes_and_again_in_time()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            var taken   = new List<RoamingNetworkAttestation>();

            history.AttestationSigners  = [ RNOSigner ];
            history.OnAttestation      += taken.Add;

            var result  = history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet(
                                             [ RoamingNetworkChange.UpdateProperty(history.Head.RoamingNetwork, "dataSource", null, "v1") ],
                                             CreatedAt: T0));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            var signedAt      = taken.Single().SignedAt;
            var soon          = history.RefreshAttestations(signedAt.AddMinutes(1));
            var later         = history.RefreshAttestations(signedAt.AddMinutes(6));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(taken[0].CommitId,                                        Is.EqualTo(result.Commit!.Id));
                Assert.That(soon,                                                     Is.Empty, "the newest one is fresh");
                Assert.That(later,                                                    Has.Count.EqualTo(1));
                Assert.That(taken,                                                    Has.Count.EqualTo(2));
                Assert.That(history.CurrentAttestations(signedAt.AddMinutes(7)),      Has.Count.EqualTo(2));
                Assert.That(history.CurrentAttestations(signedAt.AddMinutes(20)),     Has.Count.EqualTo(1), "only the later one is still valid");
            }

        }

        #endregion

        #region A_history_takes_attestations_of_its_commits_only()

        [Test]
        public void A_history_takes_attestations_of_its_commits_only()
        {

            var leader   = RoamingNetworkHistory.Start(NewNetwork());
            var mirror   = RoamingNetworkHistory.Start(NewNetwork());
            var head     = leader.Head;
            var eTag     = head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256);

            var good     = RNOSigner.Attest(RN, head.Commit.Id, head.Commit.Revision, eTag, Timestamp.Now, Timestamp.Now.AddMinutes(15));
            var wrongTag = RNOSigner.Attest(RN, head.Commit.Id, head.Commit.Revision, new ETag(ETagFormat.EffectiveTree, ETagHashAlgorithm.SHA256, new Byte[32]), Timestamp.Now, Timestamp.Now.AddMinutes(15));
            var expired  = RNOSigner.Attest(RN, head.Commit.Id, head.Commit.Revision, eTag, Timestamp.Now.AddHours(-1), Timestamp.Now.AddMinutes(-45));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(mirror.TryAddAttestation(good,     Trusted, out var error),  Is.True, error);
                Assert.That(mirror.TryAddAttestation(good,     Trusted, out error),      Is.True, "once more, as known");
                Assert.That(mirror.TryAddAttestation(wrongTag, Trusted, out error),      Is.False);
                Assert.That(mirror.TryAddAttestation(expired,  Trusted, out error),      Is.False);
                Assert.That(mirror.TryAddAttestation(good,     _ => false, out error),   Is.False);
                Assert.That(mirror.CurrentAttestations(),                                Has.Count.EqualTo(1));
            }

        }

        #endregion

        #region A_second_tree_hash_algorithm()

        [Test]
        public void A_second_tree_hash_algorithm()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());

            var result  = history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet(
                                             [ RoamingNetworkChange.UpdateProperty(history.Head.RoamingNetwork, POITreeHashAlgorithms.JSONProperty, null, new JArray("sha256", "sha512")) ],
                                             CreatedAt: T0));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            var version = history.Head.RoamingNetwork;

            Assert.That(history.TryGetChargingStationProof(S1, out var proof512, out var error, ETagHashAlgorithm.SHA512), Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(version.TreeHashAlgorithms,                                  Is.EqualTo(new[] { ETagHashAlgorithm.SHA256, ETagHashAlgorithm.SHA512 }));
                Assert.That(version.ToJSON()[POITreeHashAlgorithms.JSONProperty]!.Select(algorithm => algorithm.Value<String>()),  Is.EqualTo(new[] { "sha256", "sha512" }));
                Assert.That(result.Commit!.ETags.Where(eTag => eTag.Format == ETagFormat.EffectiveTree).Select(eTag => eTag.HashAlgorithm),
                            Is.EqualTo(new[] { ETagHashAlgorithm.SHA256, ETagHashAlgorithm.SHA512 }));
                Assert.That(proof512!.Verify(version.EffectiveTreeETag(ETagHashAlgorithm.SHA512), out error),  Is.True, error);
                Assert.That(proof512.ToJSON()["hashAlgorithm"]!.Value<String>(),         Is.EqualTo("sha512"));
                Assert.That(() => version.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(version, POITreeHashAlgorithms.JSONProperty, null, new JArray("sha256", "sha256")) ]),
                            Throws.ArgumentException.With.Message.Contains("twice"));

                // Their order is content: the first one is the one of proofs by default.
                Assert.That(() => version.CreateChangeSet([ RoamingNetworkChange.AddElement(nameof(RoamingNetwork), RN.ToString(), [ new POIElementPathSegment(POITreeHashAlgorithms.JSONProperty, "sha384") ], "sha384") ]),
                            Throws.ArgumentException.With.Message.Contains("as a whole"));

                // Read back with its tree hash algorithms.
                Assert.That(RoamingNetwork.TryParse(version.ToJSON(), out var parsed, out error),                Is.True, error);
                Assert.That(parsed!.TreeHashAlgorithms,                                  Is.EqualTo(version.TreeHashAlgorithms));
                Assert.That(parsed.CalcETags(),                                          Is.EqualTo(version.ETags));

            }

        }

        #endregion

        #region A_station_proof_is_witnessed_by_a_checkpoint()

        [Test]
        public void A_station_proof_is_witnessed_by_a_checkpoint()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());

            history.CheckpointSigners = [ RNOSigner ];

            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var error, CreatedAt: T0),                    Is.True, error);
            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out error),                               Is.True, error);
            Assert.That(history.TryGetCommitProof(proof!.CommitId!.Value, out var commitProof, out error),              Is.True, error);

            var treeETag = commitProof!.ETags.Single(eTag => eTag.Format == ETagFormat.EffectiveTree);

            using (Assert.EnterMultipleScope())
            {
                // Months later: the station proof leads to the effectiveTree ETag of a commit the checkpoint proves.
                Assert.That(commitProof.Verify(checkpoint!, out error),        Is.True, error);
                Assert.That(proof.Verify(treeETag, out error),                 Is.True, error);
            }

        }

        #endregion

        #region Proofs_and_attestations_over_HTTP()

        [Test]
        public async Task Proofs_and_attestations_over_HTTP()
        {

            var history     = RoamingNetworkHistory.Start(NewNetwork());

            history.AttestationSigners = [ RNOSigner ];
            Assert.That(history.TryAttest(out _, out var attestError), Is.True, attestError);

            await using var httpServer = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI     = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

            using var httpClient = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/") };

            var full          = await httpClient.GetAsync("CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/proof");
            var byStation     = await httpClient.GetStringAsync("CSs/DE*GEF*S1/proof");
            var byEVSE        = await httpClient.GetStringAsync("EVSEs/DE*GEF*E1*2/proof");
            var byFullEVSE    = await httpClient.GetStringAsync("CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*1/proof");
            var attestations  = JObject.Parse(await httpClient.GetStringAsync("attestations"));
            var otherHash     = await httpClient.GetAsync("CSs/DE*GEF*S1/proof?hashAlgorithm=sha384");
            var unknown       = await httpClient.GetAsync("CSs/DE*GEF*S9/proof");

            Assert.That(full.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(RoamingNetworkProof.TryParse(await full.Content.ReadAsStringAsync(), out var proof, out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(proof!.Verify(Trusted, out _, out error),                                     Is.True, error);
                Assert.That(JToken.DeepEquals(JObject.Parse(byStation), JObject.Parse(byEVSE)),           Is.True);
                Assert.That(JToken.DeepEquals(JObject.Parse(byStation), JObject.Parse(byFullEVSE)),       Is.True);
                Assert.That(attestations["attestations"]!.Count(),                                        Is.EqualTo(1));
                Assert.That(otherHash.StatusCode,                                                         Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(unknown.StatusCode,                                                           Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(String.Join(", ", full.Headers.GetValues("ETag")),                                                       Does.StartWith("\"json:sha256:hex:"));
            }

        }

        #endregion

    }

}
