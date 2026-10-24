/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

using cloud.charging.open.protocols.WWCP.POI.tests.History;
using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The CBOR representations built directly, map entry by map entry, are byte for byte the CBOR of the JSON
    /// representations they replaced, embedded or not; and what is read back from them writes the JSON it was written from.
    /// </summary>
    [TestFixture]
    public class CBORParityTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-19T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E11      = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E12      = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly ChargingTariff_Id           T1       = ChargingTariff_Id.         Parse("DE*GEF*T1");

        /// <summary>
        /// Custom data with nested objects, decimals, integers, texts, booleans and arrays.
        /// </summary>
        private static CustomDataNew RichCustomData

            => CustomDataNew.ParseJObject(new JObject(
                   new JProperty("vendor", new JObject(
                       new JProperty("tier",     2),
                       new JProperty("rate",     0.39m),
                       new JProperty("factor",   -12.5m),
                       new JProperty("verified", true),
                       new JProperty("tags",     new JArray("fast", "green", 7))
                   ))
               ));

        private static Address RichAddress

            => new ("Hauptstraße", "07743", I18NString.Create(Languages.de, "Jena"), Country.Germany, HouseNumber: "1");

        /// <summary>
        /// A license that is not well-known, with its URLs out of order.
        /// </summary>
        private static readonly DataLicense OwnLicense = new (DataLicense_Id.Parse("own-1.0"),
                                                              I18NString.Create(Languages.en, "Our own license"),
                                                              URL.Parse("https://example.org/z"),
                                                              URL.Parse("https://example.org/a"));

        #endregion


        #region Histories

        /// <summary>
        /// The history of the network of the station proofs, its commits signed by the leader and the roaming network
        /// operator, its checkpoints by the roaming network operator, the price statements of DE*GEF by DE*GEF and its
        /// head attested: a genesis, a commit of a change set with a description and metadata, price statements before and
        /// after a checkpoint, another commit, and a second checkpoint following the first.
        /// </summary>
        private sealed class RichHistory
        {

            public required RoamingNetworkHistory         History            { get; init; }
            public required RoamingNetworkCommit          Genesis            { get; init; }
            public required RoamingNetworkCommit          Commit             { get; init; }
            public required RoamingNetworkCheckpoint      FirstCheckpoint    { get; init; }
            public required RoamingNetworkCheckpoint      SecondCheckpoint   { get; init; }
            public required RoamingNetworkPriceStatement  FirstStatement     { get; init; }
            public required RoamingNetworkPriceStatement  LaterStatement     { get; init; }
            public required RoamingNetworkAttestation     Attestation        { get; init; }
            public required RoamingNetworkProof           Proof              { get; init; }

        }

        private static readonly Lazy<RichHistory> rich = new (() => {

            var since    = DateTimeOffset.FromUnixTimeSeconds(Timestamp.Now.AddHours(-1).ToUnixTimeSeconds());

            var history  = RoamingNetworkHistory.Start(StationProofTests.NewNetwork(),
                                                       [ CommitSignatureTests.LeaderSigner, StationProofTests.RNOSigner ]);

            history.CheckpointSigners       = [ CheckpointTests.RNOSigner, CheckpointTests.WitnessSigner ];
            history.PriceStatementSigners   = [ PriceStatementTests.Signer(GEF) ];
            history.PriceStatementValidity  = TimeSpan.FromHours(6);
            history.AttestationSigners      = [ StationProofTests.RNOSigner ];

            var genesis  = history.Head.Commit;
            var network  = history.Head.RoamingNetwork;

            var result   = history.Commit(network.CreateSignedChangeSet(
                                              [
                                                  RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "v1"),
                                                  RoamingNetworkChange.UpdateProperty(network, "name",       null, new JObject(new JProperty("en", "Example Roaming"), new JProperty("de", "Beispiel-Roaming")))
                                              ],
                                              Description:  ImmutableI18NString.Create(Languages.en, "Named the network").Set(Languages.de, "Das Netz benannt"),
                                              Metadata:     new JObject(new JProperty("ticket", "POI-42"), new JProperty("validFrom", "2026-01-01T00:00:00Z"), new JProperty("weight", 0.75m)),
                                              CreatedAt:    T0
                                          ));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            var first    = SetPrice(history, nameof(EVSE), E11.ToString(), 0.29m, since.AddSeconds(1));

            Assert.That(history.TryMakeCheckpoint(out var firstCheckpoint,  out var firstError,  CreatedAt: since.AddSeconds(2)),  Is.True, firstError);

            SetPrice(history, nameof(EVSE), E11.ToString(), 0.31m, since.AddSeconds(3));

            Assert.That(Commit(history, "v2", 1).IsCommitted);

            var later    = SetPrice(history, nameof(EVSE), E11.ToString(), 0.345m, since.AddSeconds(4));

            Assert.That(history.TryMakeCheckpoint(out var secondCheckpoint, out var secondError, CreatedAt: since.AddSeconds(5)),  Is.True, secondError);

            Assert.That(history.TryAttest(out var attestations, out var attestError, T0),                                          Is.True, attestError);
            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var proofError, At: T0.AddMinutes(1)),           Is.True, proofError);

            return new RichHistory {
                       History           = history,
                       Genesis           = genesis,
                       Commit            = result.Commit!,
                       FirstCheckpoint   = firstCheckpoint!,
                       SecondCheckpoint  = secondCheckpoint!,
                       FirstStatement    = first,
                       LaterStatement    = later,
                       Attestation       = attestations!.Single(),
                       Proof             = proof!
                   };

        });

        private static RoamingNetworkPriceStatement SetPrice(RoamingNetworkHistory  History,
                                                             String                 EntityType,
                                                             String                 EntityId,
                                                             Decimal                Price,
                                                             DateTimeOffset         At)
        {

            var prices = new ChargingPrices([ new ChargingTariffPrices(T1,
                                                                       [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, Price, StepSize: 1),
                                                                                                     new ChargingPriceComponent(ChargingDimensionTypes.TIME,   1.25m, StepSize: 60) ]) ]) ]);

            var result = History.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, EntityType, EntityId, prices.Text, At, out _, out var error);

            Assert.That(result,                                                                       Is.EqualTo(POIStatusUpdateResult.Updated), error);
            Assert.That(History.TryGetPriceStatement(EntityType, EntityId, out var priceStatement),   Is.True, "The price status was not stated.");

            return priceStatement!;

        }

        /// <summary>
        /// A closed first epoch and the second one started from its final checkpoint, of SHA-512 identifications: the final checkpoint naming the next epoch, the genesis of the next epoch naming
        /// its profiles, a commit of the next epoch, and its first checkpoint naming the epoch before.
        /// </summary>
        private sealed class EpochHistory
        {

            public required RoamingNetworkCheckpoint  Final            { get; init; }
            public required RoamingNetworkCommit      NextGenesis      { get; init; }
            public required RoamingNetworkCommit      NextCommit       { get; init; }
            public required RoamingNetworkCheckpoint  NextCheckpoint   { get; init; }

        }

        private static readonly Lazy<EpochHistory> epochs = new (() => {

            var history  = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());

            history.CheckpointSigners = [ CheckpointTests.RNOSigner ];

            Assert.That(Commit(history, "v1", 1).IsCommitted);
            Assert.That(history.TryMakeCheckpoint(out _, out var checkpointError, CreatedAt: T0.AddMinutes(2)),  Is.True, checkpointError);
            Assert.That(Commit(history, "v2", 3).IsCommitted);

            Assert.That(history.TryCloseEpoch(new RoamingNetworkEpochProfiles(ETagHashAlgorithm.SHA512),
                                              out var final, out var closeError, T0.AddMinutes(4)),                Is.True, closeError);

            Assert.That(history.TryStartNextEpoch(out var next, out var startError, Signers: [ CheckpointTests.RNOSigner ]),  Is.True, startError);

            next!.CheckpointSigners = [ CheckpointTests.RNOSigner ];

            var v3       = Commit(next, "v3", 5);

            Assert.That(v3.IsCommitted, Is.True, v3.ErrorResponse);
            Assert.That(next.TryMakeCheckpoint(out var first, out var firstError, CreatedAt: T0.AddMinutes(6)),  Is.True, firstError);

            return new EpochHistory {
                       Final           = final!,
                       NextGenesis     = next.Root,
                       NextCommit      = v3.Commit!,
                       NextCheckpoint  = first!
                   };

        });

        private static RoamingNetworkCommitResult Commit(RoamingNetworkHistory  History,
                                                         String                 Value,
                                                         Int32                  Minutes)

            => History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(
                                  [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, "dataSource", null, Value) ],
                                  CreatedAt: T0.AddMinutes(Minutes)));

        #endregion

        #region Entities

        private static RoamingNetworkAttestation AttestationWithSubject()
        {

            var head = rich.Value.History.Head;
            var eTag = head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256);

            return StationProofTests.RNOSigner.Attest(head.RoamingNetwork.Id,
                                                      head.Commit.Id,
                                                      head.Commit.Revision,
                                                      eTag,
                                                      T0,
                                                      T0.AddMinutes(15),
                                                      new RoamingNetworkAttestationSubject(nameof(ChargingStation),
                                                                                           S1.ToString(),
                                                                                           Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(S1.ToString()))).ToLowerInvariant()));

        }

        private static RoamingNetworkChangeSet SignedChangeSet()
        {

            var network = StationProofTests.NewNetwork();

            return network.CreateSignedChangeSet(
                       [
                           RoamingNetworkChange.UpdateProperty(network, "dataSource",  null,  "v1"),
                           RoamingNetworkChange.UpdateProperty(network, "name",        null,  new JObject(new JProperty("en", "Network 1"))),
                           RoamingNetworkChange.UpdateProperty(network, "description", null,  new JObject(new JProperty("de", "Ein Netz"))),
                           RoamingNetworkChange.UpdateProperty(network, "customData",  null,  RichCustomData.ToJObject())
                       ],
                       Description:  ImmutableI18NString.Create(Languages.en, "Hello world!").Set(Languages.de, "Hallo Welt!"),
                       Metadata:     new JObject(new JProperty("validFrom", "2026-01-01T00:00:00Z"), new JProperty("weight", 0.75m), new JProperty("count", 3)),
                       Id:           ChangeSet_Id.Parse("cs-rich"),
                       CreatedAt:    DateTimeOffset.Parse("2026-10-11T12:34:56.7891234Z")
                   );

        }

        private static ChargingStationOperator RichChargingStationOperator()
        {

            var gef = StationProofTests.NewNetwork().GetChargingStationOperatorById(GEF)!;

            return new ChargingStationOperator(
                       GEF,
                       Name:                   I18NString.Create(Languages.en, "GraphDefined").Set(Languages.de, "GraphDefined GmbH"),
                       Description:            I18NString.Create(Languages.de, "Ein Betreiber"),
                       Logo:                   URL.Parse("https://example.org/logo.png"),
                       Address:                RichAddress,
                       GeoLocation:            GeoCoordinate.Parse(50.9271, 11.5892),
                       Telephone:              PhoneNumber.Parse("+49 3641 123456"),
                       EMailAddress:           EMailAddress.Parse("Example Office <info@example.org>"),
                       Homepage:               URL.Parse("https://example.org"),
                       HotlinePhoneNumber:     PhoneNumber.Parse("+49 800 123456"),
                       TermsAndConditionsURL:  URL.Parse("https://example.org/terms"),
                       DataLicenses:           [ OwnLicense, DataLicense.OpenDatabaseLicense ],
                       ChargingPools:          gef.ChargingPools,
                       ChargingTariffs:        gef.ChargingTariffs,
                       EVSEGroups:             [ new EVSEGroup           (EVSEGroup_Id.           Parse("DE*GEF*EG1"), I18NString.Create(Languages.en, "Fast"), EVSEIds:            [ E12, E11 ], Created: Created) ],
                       ChargingStationGroups:  [ new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG1"),                                          ChargingStationIds: [ S1 ],       Created: Created) ],
                       ChargingPoolGroups:     [ new ChargingPoolGroup   (ChargingPoolGroup_Id.   Parse("DE*GEF*PG1"),                                          ChargingPoolIds:    [ ChargingPool_Id.Parse("DE*GEF*P1") ], Created: Created) ],
                       ChargingTariffGroups:   [ new ChargingTariffGroup (ChargingTariffGroup_Id. Parse("DE*GEF*TG1"),                                          ChargingTariffIds:  [ T1 ],       Created: Created) ],
                       Brands:                 [ new Brand(Brand_Id.Parse("blue-charge"), ImmutableI18NString.Create(Languages.en, "Blue Charge")) ],
                       SigningKeys:            gef.SigningKeys,
                       IdentityKeys:           gef.IdentityKeys,
                       EVSETemplates:          gef.EVSETemplates,
                       DataSource:             "test",
                       Created:                Created,
                       LastChange:             T0,
                       CustomData:             RichCustomData
                   );

        }

        private static EMobilityProvider RichEMobilityProvider()

            => new EMobilityProvider(
                   EMobilityProvider_Id.Parse("DE-GDF"),
                   Name:                   I18NString.Create(Languages.en, "Example Mobility").Set(Languages.de, "Beispiel-Mobilität"),
                   Description:            I18NString.Create(Languages.en, "An e-mobility provider"),
                   Logo:                   URL.Parse("https://mobility.example.org/logo.png"),
                   Address:                RichAddress,
                   GeoLocation:            GeoCoordinate.Parse(52.5200, 13.4050),
                   Telephone:              PhoneNumber.Parse("+49 30 654321"),
                   EMailAddress:           EMailAddress.Parse("Example Mobility <info@mobility.example.org>"),
                   Homepage:               URL.Parse("https://mobility.example.org"),
                   HotlinePhoneNumber:     PhoneNumber.Parse("+49 800 654321"),
                   TermsAndConditionsURL:  URL.Parse("https://mobility.example.org/terms"),
                   DataLicenses:           [ OwnLicense ],
                   EVSEGroups:             [ new EVSEGroup           (EVSEGroup_Id.           Parse("DE-GDF*EGFAV"), I18NString.Create(Languages.en, "Favourites"), EVSEIds:            [ E11 ], Created: Created) ],
                   ChargingStationGroups:  [ new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE-GDF*SGFAV"),                                                ChargingStationIds: [ S1 ],  Created: Created) ],
                   DataSource:             "test",
                   Created:                Created,
                   LastChange:             T0,
                   CustomData:             RichCustomData
               ).WithKeys();

        private static ChargingStationManufacturer RichChargingStationManufacturer()

            => new ChargingStationManufacturer(
                   ChargingStationManufacturer_Id.Parse("ACME"),
                   Name:                I18NString.Create(Languages.en, "ACME Wallboxes"),
                   Description:            I18NString.Create(Languages.de, "Ein Hersteller"),
                   Logo:                   URL.Parse("https://acme.example.org/logo.png"),
                   Address:                RichAddress,
                   GeoLocation:            GeoCoordinate.Parse(48.1351, 11.5820),
                   Telephone:              PhoneNumber.Parse("+49 89 111111"),
                   EMailAddress:           EMailAddress.Parse("ACME Sales <sales@acme.example.org>"),
                   Homepage:               URL.Parse("https://acme.example.org"),
                   HotlinePhoneNumber:     PhoneNumber.Parse("+49 800 111111"),
                   TermsAndConditionsURL:  URL.Parse("https://acme.example.org/terms"),
                   DataLicenses:           [ DataLicense.OpenDatabaseLicense ],
                   Models:                 [ new ChargingStationModel(ChargingStationModel_Id.Parse("DC-300"),
                                                                      I18NString.Create(Languages.en, "DC 300 kW"),
                                                                      I18NString.Create(Languages.de, "Schnelllader"),
                                                                      Versions:        [ "1.0", "1.1" ],
                                                                      ProductURL:      URL.Parse("https://acme.example.org/dc-300"),
                                                                      DatasheetURL:    URL.Parse("https://acme.example.org/dc-300.pdf"),
                                                                      MaxPower:        Watt.FromW(300000),
                                                                      CurrentTypes:    CurrentTypes.DC,
                                                                      ConnectorTypes:  [ ChargingConnectorType.IEC_62196_T2_COMBO, ChargingConnectorType.CHAdeMO ],
                                                                      NumberOfEVSEs:   2,
                                                                      CustomData:      RichCustomData),
                                             new ChargingStationModel(ChargingStationModel_Id.Parse("AC-22")) ],
                   SigningKeys:            [ TestParties.Key(new POIPartyId(nameof(ChargingStationManufacturer), "ACME")).Public ],
                   IdentityKeys:           [ TestParties.Key(new POIPartyId(nameof(ChargingStationManufacturer), "ACME"), POIKeyKind.Identity).Public ],
                   DataSource:          "test",
                   Created:             Created,
                   LastChange:          T0,
                   CustomData:          RichCustomData
               );

        private static EnergyMeterManufacturer RichEnergyMeterManufacturer()

            => new EnergyMeterManufacturer(
                   EnergyMeterManufacturer_Id.Parse("METERS"),
                   Name:                ImmutableI18NString.Create(Languages.en, "ACME Meters"),
                   Description:            ImmutableI18NString.Create(Languages.de, "Ein Zählerhersteller"),
                   Logo:                   URL.Parse("https://meters.example.org/logo.png"),
                   Address:                RichAddress,
                   GeoLocation:            GeoCoordinate.Parse(50.1109, 8.6821),
                   Telephone:              PhoneNumber.Parse("+49 69 222222"),
                   EMailAddress:           EMailAddress.Parse("ACME Meters <meters@example.org>"),
                   Homepage:               URL.Parse("https://meters.example.org"),
                   HotlinePhoneNumber:     PhoneNumber.Parse("+49 800 222222"),
                   TermsAndConditionsURL:  URL.Parse("https://meters.example.org/terms"),
                   DataLicenses:           [ OwnLicense ],
                   Models:                 [ new EnergyMeterModel(EnergyMeterModel_Id.Parse("M-500"),
                                                                  ImmutableI18NString.Create(Languages.en, "DC meter"),
                                                                  Versions:          [ "3.0" ],
                                                                  CurrentTypes:      CurrentTypes.DC | CurrentTypes.AC_ThreePhases,
                                                                  MaxCurrent:        Ampere.FromA(500),
                                                                  AccuracyClass:     "B",
                                                                  Measurands:        [ "1-0:2.8.0", "1-0:1.8.0" ],
                                                                  SignatureMethods:  [ SignatureMethod.OCMF, SignatureMethod.EDL40 ]) ],
                   IdentityKeys:           [ TestParties.Key(new POIPartyId(nameof(EnergyMeterManufacturer), "METERS"), POIKeyKind.Identity).Public ],
                   SigningKeys:            [ TestParties.Key(new POIPartyId(nameof(EnergyMeterManufacturer), "METERS")).Public ],
                   DataSource:          "test",
                   Created:             Created,
                   LastChange:          T0,
                   CustomData:          RichCustomData
               );

        private static GridOperator RichGridOperator()
        {

            var gridMeter = EnergyMeter_Id.Parse("grid-meter-1");

            return new GridOperator(
                       GridOperator_Id.Parse("DE*NET"),
                       Name:                  I18NString.Create(Languages.en, "Example Netz"),
                       Description:           I18NString.Create(Languages.de, "Ein Netzbetreiber"),
                       Address:               RichAddress,
                       Homepage:              URL.Parse("https://netz.example.org"),
                       HotlinePhoneNumber:    PhoneNumber.Parse("+49 800 333333"),
                       GridConnectionPoints:  [ new GridConnectionPoint(GridConnectionPoint_Id.Parse("gcp-1"),
                                                                        VoltageLevel:                   GridVoltageLevel.LowVoltage,
                                                                        ConnectionType:                 GridConnectionTypes.AC_ThreePhases,
                                                                        NominalVoltage:                 Volt.FromV(400),
                                                                        NominalFrequency:               Hertz.FromHz(50),
                                                                        ContractedImportPower:          Watt.FromW(250000),
                                                                        ContractedExportPower:          Watt.FromW(100000),
                                                                        ContractedImportApparentPower:  VoltAmpere.FromVA(300000),
                                                                        MarketLocationIds:              [ "DE0001", "DE0002" ],
                                                                        EnergyMeter:                    new EnergyMeter(gridMeter, Role: EnergyMeterRole.Grid, Created: Created)),
                                                new GridConnectionPoint(GridConnectionPoint_Id.Parse("gcp-2"), VoltageLevel: GridVoltageLevel.MediumVoltage) ],
                       EnergyMeterGroups:     [ new EnergyMeterGroup(EnergyMeterGroup_Id.Parse("all-grid-meters"), EnergyMeterIds: [ gridMeter ], Created: Created) ],
                       DataSource:            "test",
                       Created:               Created,
                       LastChange:            T0,
                       CustomData:            RichCustomData
                   ).WithKeys();

        }

        private static ParkingOperator RichParkingOperator()
        {

            var s1     = ChargingStation_Id.Parse("DE*GEF*S1");
            var g1     = ParkingGarage_Id.  Parse("garage-1");
            var sp1    = ParkingSpace_Id.   Parse("space-1");
            var sp2    = ParkingSpace_Id.   Parse("space-2");
            var sn1    = ParkingSensor_Id.  Parse("sensor-1");

            return new ParkingOperator(
                       ParkingOperator_Id.Parse("DE*PAR"),
                       Name:                   I18NString.Create(Languages.en, "Example Parking"),
                       Description:            I18NString.Create(Languages.de, "Ein Parkhausbetreiber"),
                       Address:                RichAddress,
                       Homepage:               URL.Parse("https://parking.example.org"),
                       HotlinePhoneNumber:     PhoneNumber.Parse("+49 800 444444"),
                       ParkingGarages:         [ new ParkingGarage(g1,
                                                                   I18NString.Create(Languages.en, "Garage 1"),
                                                                   GeoLocation:         GeoCoordinate.Parse(50.92, 11.58),
                                                                   ChargingStationIds:  [ s1 ],
                                                                   Created:             Created) ],
                       ParkingSpaces:          [ new ParkingSpace(sp1,
                                                                  ParkingGarageId:      g1,
                                                                  ParkingSensorIds:     [ sn1 ],
                                                                  ChargingStationIds:   [ s1 ],
                                                                  ParkingRestrictions:  [ ParkingRestrictionGroup.EV_ONLY, ParkingRestrictionGroup.DISABLED ],
                                                                  EUVehicleCategories:  [ EUVehicleCategory.M1 ],
                                                                  TemplateIds:          [ ParkingSpaceTemplate_Id.Parse("level-1") ],
                                                                  Created:              Created),
                                                 new ParkingSpace(sp2,
                                                                  ParkingGarageId:      g1,
                                                                  Created:              Created) ],
                       ParkingSensors:         [ new ParkingSensor(sn1, Created: Created) ],
                       ParkingSpaceGroups:     [ new ParkingSpaceGroup(ParkingSpaceGroup_Id.Parse("level-1"), ParkingSpaceIds: [ sp1, sp2 ], Created: Created) ],
                       ParkingSpaceTemplates:  [ new ParkingSpaceTemplate(ParkingSpaceTemplate_Id.Parse("level-1"), Roofed: true, Created: Created) ],
                       DataSource:             "test",
                       Created:                Created,
                       LastChange:             T0,
                       CustomData:             RichCustomData
                   ).WithKeys();

        }

        private static EVSEGroup RichEVSEGroup()

            => new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EG1"),
                             Name:         I18NString.Create(Languages.en, "Fast").Set(Languages.de, "Schnell"),
                             Description:  I18NString.Create(Languages.en, "The fast EVSEs"),
                             EVSEIds:      [ E12, E11 ],
                             DataSource:   "test",
                             Created:      Created,
                             LastChange:   T0,
                             CustomData:   RichCustomData);

        private static ChargingStationGroup RichChargingStationGroup()

            => new ChargingStationGroup(ChargingStationGroup_Id.Parse("DE*GEF*SG1"),
                                        Name:                I18NString.Create(Languages.en, "Downtown"),
                                        Description:         I18NString.Create(Languages.de, "Die Stationen in der Stadt"),
                                        ChargingStationIds:  [ S1, ChargingStation_Id.Parse("DE*GEF*S2") ],
                                        DataSource:          "test",
                                        Created:             Created,
                                        LastChange:          T0,
                                        CustomData:          RichCustomData);

        #endregion

        #region Cases

        /// <summary>
        /// Every class whose CBOR is built directly, by name, with how to make one as rich as it can be.
        /// </summary>
        private static IEnumerable<(String Name, Func<Object> Create)> Objects()
        {

            yield return ("Commit (genesis, two signatures)",                 () => rich.Value.Genesis);
            yield return ("Commit (change set with metadata)",                () => rich.Value.Commit);
            yield return ("Commit (genesis of epoch 2, profiles)",            () => epochs.Value.NextGenesis);
            yield return ("Commit (epoch 2)",                                 () => epochs.Value.NextCommit);
            yield return ("Checkpoint (first, price statements)",             () => rich.Value.FirstCheckpoint);
            yield return ("Checkpoint (second, previous, two signatures)",    () => rich.Value.SecondCheckpoint);
            yield return ("Checkpoint (final, next epoch)",                   () => epochs.Value.Final);
            yield return ("Checkpoint (epoch 2, previous epoch)",             () => epochs.Value.NextCheckpoint);
            yield return ("Attestation",                                      () => rich.Value.Attestation);
            yield return ("Attestation (subject)",                            AttestationWithSubject);
            yield return ("CommitInfo (genesis)",                             () => new RoamingNetworkCommitInfo(rich.Value.Genesis));
            yield return ("CommitInfo (change set)",                          () => new RoamingNetworkCommitInfo(rich.Value.Commit));
            yield return ("PriceStatement (first)",                           () => rich.Value.FirstStatement);
            yield return ("PriceStatement (after a checkpoint, previous)",    () => rich.Value.LaterStatement);
            yield return ("Proof (station, attestation)",                     () => rich.Value.Proof);
            yield return ("ChangeSet (signed, metadata)",                     SignedChangeSet);
            yield return ("ChargingStationOperator",                          RichChargingStationOperator);
            yield return ("ChargingStationOperator (station proofs network)", () => StationProofTests.NewNetwork().GetChargingStationOperatorById(GEF)!);
            yield return ("EMobilityProvider",                                RichEMobilityProvider);
            yield return ("ChargingStationManufacturer",                      RichChargingStationManufacturer);
            yield return ("EnergyMeterManufacturer",                          RichEnergyMeterManufacturer);
            yield return ("GridOperator",                                     RichGridOperator);
            yield return ("ParkingOperator",                                  RichParkingOperator);
            yield return ("EVSEGroup",                                        RichEVSEGroup);
            yield return ("ChargingStationGroup",                             RichChargingStationGroup);
            yield return ("RoamingNetworkOperator",                           RoamingNetworkOperatorSerializationTests.Full);
            yield return ("RoamingNetwork",                                   RoamingNetworkSerializationTests.Full);

        }

        /// <summary>
        /// Every object, embedded and not, the change set with and without its signatures, the roaming network with and
        /// without its ETags.
        /// </summary>
        private static IEnumerable<TestCaseData> Cases()
        {
            foreach (var (name, create) in Objects())
                foreach (var embedded in new[] { false, true })
                    foreach (var option in name.StartsWith("ChangeSet") || name == "RoamingNetwork" ? new[] { true, false } : new[] { true })
                        yield return new TestCaseData(name, create, embedded, option).
                                         SetName($"{name}, Embedded: {embedded}" + (name.StartsWith("ChangeSet") ? $", IncludeSignatures: {option}" :
                                                                                    name == "RoamingNetwork"     ? $", IncludeETags: {option}"      : ""));
        }

        /// <summary>
        /// The cases whose CBOR is the CBOR of their JSON: all but the roaming network with its ETags, whose CBOR writes
        /// each ETag as its own CBOR representation, [format, algorithm, digest bytes], not as its JSON array of texts.
        /// </summary>
        private static IEnumerable<TestCaseData> ParityCases()

            => Cases().Where(testCase => !((String) testCase.Arguments[0]! == "RoamingNetwork" && (Boolean) testCase.Arguments[3]!));

        /// <summary>
        /// The cases of every class that can be read back from its CBOR: all but the commit info, which has no parser.
        /// The change set and the commit containing it have a change whose old value is null: it reads back as null,
        /// not as "", so that the identification of the commit still fits its content.
        /// </summary>
        private static IEnumerable<TestCaseData> ParsableCases()

            => Cases().Where(testCase => !((String) testCase.Arguments[0]!).StartsWith("CommitInfo"));

        #endregion

        #region (private static) CBOROf(Value, Embedded, Option) / JSONOf(Value, Embedded, Option)

        private static CBORValue CBOROf(Object Value, Boolean Embedded, Boolean Option)

            => Value switch {
                   RoamingNetworkCommit          commit           => commit.         ToCBOR(Embedded: Embedded),
                   RoamingNetworkCheckpoint      checkpoint       => checkpoint.     ToCBOR(Embedded: Embedded),
                   RoamingNetworkAttestation     attestation      => attestation.    ToCBOR(Embedded: Embedded),
                   RoamingNetworkCommitInfo      commitInfo       => commitInfo.     ToCBOR(Embedded: Embedded),
                   RoamingNetworkPriceStatement  priceStatement   => priceStatement. ToCBOR(Embedded: Embedded),
                   RoamingNetworkProof           proof            => proof.          ToCBOR(Embedded: Embedded),
                   RoamingNetworkChangeSet       changeSet        => changeSet.      ToCBOR(Embedded: Embedded, IncludeSignatures: Option),
                   ChargingStationOperator       csOperator       => csOperator.     ToCBOR(Embedded: Embedded),
                   EMobilityProvider             provider         => provider.       ToCBOR(Embedded: Embedded),
                   ChargingStationManufacturer   csManufacturer   => csManufacturer. ToCBOR(Embedded: Embedded),
                   EnergyMeterManufacturer       emManufacturer   => emManufacturer. ToCBOR(Embedded: Embedded),
                   GridOperator                  gridOperator     => gridOperator.   ToCBOR(Embedded: Embedded),
                   ParkingOperator               parkingOperator  => parkingOperator.ToCBOR(Embedded: Embedded),
                   EVSEGroup                     evseGroup        => evseGroup.      ToCBOR(Embedded: Embedded),
                   ChargingStationGroup          stationGroup     => stationGroup.   ToCBOR(Embedded: Embedded),
                   RoamingNetworkOperator        rnOperator       => rnOperator.     ToCBOR(Embedded: Embedded),
                   RoamingNetwork                network          => network.        ToCBOR(Embedded: Embedded, IncludeETags: Option),
                   _                                              => throw new ArgumentException($"No CBOR for '{Value.GetType().Name}'!", nameof(Value))
               };

        private static JObject JSONOf(Object Value, Boolean Embedded, Boolean Option)

            => Value switch {
                   RoamingNetworkCommit          commit           => commit.         ToJSON(Embedded: Embedded),
                   RoamingNetworkCheckpoint      checkpoint       => checkpoint.     ToJSON(Embedded: Embedded),
                   RoamingNetworkAttestation     attestation      => attestation.    ToJSON(Embedded: Embedded),
                   RoamingNetworkCommitInfo      commitInfo       => commitInfo.     ToJSON(Embedded: Embedded),
                   RoamingNetworkPriceStatement  priceStatement   => priceStatement. ToJSON(Embedded: Embedded),
                   RoamingNetworkProof           proof            => proof.          ToJSON(Embedded: Embedded),
                   RoamingNetworkChangeSet       changeSet        => changeSet.      ToJSON(Embedded: Embedded, IncludeSignatures: Option),
                   ChargingStationOperator       csOperator       => csOperator.     ToJSON(Embedded: Embedded),
                   EMobilityProvider             provider         => provider.       ToJSON(Embedded: Embedded),
                   ChargingStationManufacturer   csManufacturer   => csManufacturer. ToJSON(Embedded: Embedded),
                   EnergyMeterManufacturer       emManufacturer   => emManufacturer. ToJSON(Embedded: Embedded),
                   GridOperator                  gridOperator     => gridOperator.   ToJSON(Embedded: Embedded),
                   ParkingOperator               parkingOperator  => parkingOperator.ToJSON(Embedded: Embedded),
                   EVSEGroup                     evseGroup        => evseGroup.      ToJSON(Embedded: Embedded),
                   ChargingStationGroup          stationGroup     => stationGroup.   ToJSON(Embedded: Embedded),
                   RoamingNetworkOperator        rnOperator       => rnOperator.     ToJSON(Embedded: Embedded),
                   RoamingNetwork                network          => network.        ToJSON(Embedded: Embedded, IncludeETags: Option),
                   _                                              => throw new ArgumentException($"No JSON for '{Value.GetType().Name}'!", nameof(Value))
               };

        #endregion

        #region (private static) TryParseAs(Original, CBOR, out Parsed, out ErrorResponse)

        /// <summary>
        /// Read the given CBOR as an object of the class of the given original.
        /// </summary>
        private static Boolean TryParseAs(Object       Original,
                                          CBORValue    CBOR,
                                          out Object?  Parsed,
                                          out String?  ErrorResponse)
        {

            Boolean result;

            switch (Original)
            {
                case RoamingNetworkCommit:          result = RoamingNetworkCommit.        TryParse(CBOR, out var commit,           out ErrorResponse); Parsed = commit;           break;
                case RoamingNetworkCheckpoint:      result = RoamingNetworkCheckpoint.    TryParse(CBOR, out var checkpoint,       out ErrorResponse); Parsed = checkpoint;       break;
                case RoamingNetworkAttestation:     result = RoamingNetworkAttestation.   TryParse(CBOR, out var attestation,      out ErrorResponse); Parsed = attestation;      break;
                case RoamingNetworkPriceStatement:  result = RoamingNetworkPriceStatement.TryParse(CBOR, out var priceStatement,   out ErrorResponse); Parsed = priceStatement;   break;
                case RoamingNetworkProof:           result = RoamingNetworkProof.         TryParse(CBOR, out var proof,            out ErrorResponse); Parsed = proof;            break;
                case RoamingNetworkChangeSet:       result = RoamingNetworkChangeSet.     TryParse(CBOR, out var changeSet,        out ErrorResponse); Parsed = changeSet;        break;
                case ChargingStationOperator:       result = ChargingStationOperator.     TryParse(CBOR, out var csOperator,       out ErrorResponse); Parsed = csOperator;       break;
                case EMobilityProvider:             result = EMobilityProvider.           TryParse(CBOR, out var provider,         out ErrorResponse); Parsed = provider;         break;
                case ChargingStationManufacturer:   result = ChargingStationManufacturer. TryParse(CBOR, out var csManufacturer,   out ErrorResponse); Parsed = csManufacturer;   break;
                case EnergyMeterManufacturer:       result = EnergyMeterManufacturer.     TryParse(CBOR, out var emManufacturer,   out ErrorResponse); Parsed = emManufacturer;   break;
                case GridOperator:                  result = GridOperator.                TryParse(CBOR, out var gridOperator,     out ErrorResponse); Parsed = gridOperator;     break;
                case ParkingOperator:               result = ParkingOperator.             TryParse(CBOR, out var parkingOperator,  out ErrorResponse); Parsed = parkingOperator;  break;
                case EVSEGroup:                     result = EVSEGroup.                   TryParse(CBOR, out var evseGroup,        out ErrorResponse); Parsed = evseGroup;        break;
                case ChargingStationGroup:          result = ChargingStationGroup.        TryParse(CBOR, out var stationGroup,     out ErrorResponse); Parsed = stationGroup;     break;
                case RoamingNetworkOperator:        result = RoamingNetworkOperator.      TryParse(CBOR, out var rnOperator,       out ErrorResponse); Parsed = rnOperator;       break;
                case RoamingNetwork:                result = RoamingNetwork.              TryParse(CBOR, out var network,          out ErrorResponse); Parsed = network;          break;
                default:                            throw new ArgumentException($"No parser for '{Original.GetType().Name}'!", nameof(Original));
            }

            return result;

        }

        #endregion

        #region (private static) Difference(Expected, Actual)

        /// <summary>
        /// Where the given canonical CBOR encodings differ: the first differing byte and the first differing value, by its
        /// path of map keys and array indices, in CBOR diagnostic notation.
        /// </summary>
        private static String Difference(Byte[] Expected, Byte[] Actual)
        {

            var offset = 0;

            while (offset < Expected.Length && offset < Actual.Length && Expected[offset] == Actual[offset])
                offset++;

            var text = new StringBuilder();

            text.AppendLine($"The CBOR of the JSON has {Expected.Length} bytes, the CBOR built directly {Actual.Length}; they differ from byte {offset} on.");

            Compare(CBORValue.Parse(Expected), CBORValue.Parse(Actual), "$", text);

            return text.ToString();

        }

        private static void Compare(CBORValue      Expected,
                                    CBORValue      Actual,
                                    String         Path,
                                    StringBuilder  Text)
        {

            if (Expected.Kind != Actual.Kind)
            {
                Text.AppendLine($"{Path}: of the JSON {Expected.Kind} {Shorten(Expected.ToDiagnosticString())}, built directly {Actual.Kind} {Shorten(Actual.ToDiagnosticString())}");
                return;
            }

            if (Expected.Kind == CBORValueKind.Map)
            {

                var expected = Expected.AsMap().ToDictionary(entry => entry.Key.ToDiagnosticString(), entry => entry.Value);
                var actual   = Actual.  AsMap().ToDictionary(entry => entry.Key.ToDiagnosticString(), entry => entry.Value);

                foreach (var key in expected.Keys.Except(actual.Keys))
                    Text.AppendLine($"{Path}[{key}]: only in the CBOR of the JSON: {Shorten(expected[key].ToDiagnosticString())}");

                foreach (var key in actual.Keys.Except(expected.Keys))
                    Text.AppendLine($"{Path}[{key}]: only in the CBOR built directly: {Shorten(actual[key].ToDiagnosticString())}");

                foreach (var key in expected.Keys.Intersect(actual.Keys))
                    Compare(expected[key], actual[key], $"{Path}[{key}]", Text);

                return;

            }

            if (Expected.Kind == CBORValueKind.Array)
            {

                var expected = Expected.AsArray();
                var actual   = Actual.  AsArray();

                if (expected.Count != actual.Count)
                    Text.AppendLine($"{Path}: {expected.Count} items in the CBOR of the JSON, {actual.Count} built directly");

                for (var index = 0; index < Math.Min(expected.Count, actual.Count); index++)
                    Compare(expected[index], actual[index], $"{Path}[{index}]", Text);

                return;

            }

            var expectedText = Expected.ToDiagnosticString();
            var actualText   = Actual.  ToDiagnosticString();

            if (expectedText != actualText ||
                !Convert.ToHexString(Expected.ToByteArray(CBORWriterOptions.Canonical)).Equals(Convert.ToHexString(Actual.ToByteArray(CBORWriterOptions.Canonical))))
                Text.AppendLine($"{Path}: of the JSON {Shorten(expectedText)} ({Convert.ToHexString(Expected.ToByteArray(CBORWriterOptions.Canonical))}), built directly {Shorten(actualText)} ({Convert.ToHexString(Actual.ToByteArray(CBORWriterOptions.Canonical))})");

        }

        /// <summary>
        /// Where the given JSON differ, by the paths of their properties and items, whatever the order of their properties:
        /// the values of both.
        /// </summary>
        private static String JSONDifference(JToken Expected, JToken Actual)
        {

            var text = new StringBuilder();

            CompareJSON(Expected, Actual, "$", text);

            return text.Length > 0
                       ? text.ToString()
                       : "no difference";

        }

        private static void CompareJSON(JToken         Expected,
                                        JToken         Actual,
                                        String         Path,
                                        StringBuilder  Text)
        {

            if (Expected is JObject expectedObject && Actual is JObject actualObject)
            {

                var expectedNames = expectedObject.Properties().Select(property => property.Name).ToArray();
                var actualNames   = actualObject.  Properties().Select(property => property.Name).ToArray();

                foreach (var name in expectedNames.Except(actualNames))
                    Text.AppendLine($"{Path}.{name}: only expected: {Shorten(expectedObject[name]!.ToString(Newtonsoft.Json.Formatting.None))}");

                foreach (var name in actualNames.Except(expectedNames))
                    Text.AppendLine($"{Path}.{name}: only actual: {Shorten(actualObject[name]!.ToString(Newtonsoft.Json.Formatting.None))}");

                foreach (var name in expectedNames.Intersect(actualNames))
                    CompareJSON(expectedObject[name]!, actualObject[name]!, $"{Path}.{name}", Text);

                return;

            }

            if (Expected is JArray expectedArray && Actual is JArray actualArray)
            {

                if (expectedArray.Count != actualArray.Count)
                    Text.AppendLine($"{Path}: {expectedArray.Count} items expected, {actualArray.Count} actual");

                for (var index = 0; index < Math.Min(expectedArray.Count, actualArray.Count); index++)
                    CompareJSON(expectedArray[index], actualArray[index], $"{Path}[{index}]", Text);

                return;

            }

            if (Expected.Type != Actual.Type || !JToken.DeepEquals(Expected, Actual))
                Text.AppendLine($"{Path}: expected {Expected.Type} {Shorten(Expected.ToString(Newtonsoft.Json.Formatting.None))}, actual {Actual.Type} {Shorten(Actual.ToString(Newtonsoft.Json.Formatting.None))}");

        }

        private static String Shorten(String Text)

            => Text.Length > 200
                   ? Text[..200] + "..."
                   : Text;

        #endregion


        #region TheCBORIsTheCBOROfTheJSON(Name, Create, Embedded, Option)

        /// <summary>
        /// The CBOR built directly is byte for byte the CBOR of the JSON, in canonical encoding.
        /// </summary>
        [TestCaseSource(nameof(ParityCases))]
        public void TheCBORIsTheCBOROfTheJSON(String        Name,
                                              Func<Object>  Create,
                                              Boolean       Embedded,
                                              Boolean       Option)
        {

            var value    = Create();
            var direct   = CBOROf(value, Embedded, Option).ToByteArray(CBORWriterOptions.Canonical);
            var viaJSON  = CBORJSON.ToCBOR(JSONOf(value, Embedded, Option)).ToByteArray(CBORWriterOptions.Canonical);

            var directHex   = Convert.ToHexString(direct);
            var viaJSONHex  = Convert.ToHexString(viaJSON);

            Assert.That(directHex, Is.EqualTo(viaJSONHex), () => $"{Name}: " + Difference(viaJSON, direct));

        }

        #endregion

        #region TheCBORIsReadBack(Name, Create, Embedded, Option)

        /// <summary>
        /// What is read back from the CBOR, embedded or not, writes the JSON it was written from, embedded.
        /// </summary>
        [TestCaseSource(nameof(ParsableCases))]
        public void TheCBORIsReadBack(String        Name,
                                      Func<Object>  Create,
                                      Boolean       Embedded,
                                      Boolean       Option)
        {

            var value   = Create();
            var cbor    = CBORValue.Parse(CBOROf(value, Embedded, Option).ToByteArray(CBORWriterOptions.Canonical));

            Assert.That(TryParseAs(value, cbor, out var parsed, out var error), Is.True, () => $"{Name}: {error}\n" + JSONDifference(JSONOf(value, Embedded, Option), (JObject) CBORJSON.ToJSON(cbor)));

            // A roaming network read back calculates its ETags anew, as they are checked on the way.
            if (parsed is RoamingNetwork network)
                network.CalcETags();

            var expected = JSONOf(value,   true, Option);
            var actual   = JSONOf(parsed!, true, Option);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(parsed!.GetType(),                   Is.EqualTo(value.GetType()));
                // Compared as written: a text token without a text and a null token are both null.
                Assert.That(CanonicalJSON.Serialize(actual),      Is.EqualTo(CanonicalJSON.Serialize(expected)), () => $"{Name}:\n" + JSONDifference(expected, actual));
            }

        }

        #endregion

    }

}
