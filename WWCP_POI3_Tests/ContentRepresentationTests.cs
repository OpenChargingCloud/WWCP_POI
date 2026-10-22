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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The JSON and CBOR ETags of the entities having child arrays are made of the contents their children keep
    /// (SPECIFICATION.md, section 8.7): they must be those of their contents written as a whole, before and after a change.
    /// In Debug, every test checks this (WWCP.POI.VerifyContentRepresentations); these tests check it in Release as well.
    /// </summary>
    [TestFixture]
    public class ContentRepresentationTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-21T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  BDO      = ChargingStationOperator_Id.Parse("DE*BDO");

        #endregion

        #region NewNetwork()

        /// <summary>
        /// A network of two operators, one with templates, of two charging pools of two charging stations of two EVSEs each,
        /// with an e-mobility provider, a grid operator and a manufacturer of energy meters.
        /// </summary>
        private static RoamingNetwork NewNetwork()
        {

            ChargingStationOperator Operator(ChargingStationOperator_Id Id, Boolean WithTemplates)

                => new (Id,
                        I18NString.Create(Languages.en, $"Operator {Id}"),
                        Created:                   Created,
                        ChargingPools:             [ .. Enumerable.Range(1, 2).Select(pool => new ChargingPool(
                                                            ChargingPool_Id.Parse($"{Id}*P{pool}"),
                                                            Created:           Created,
                                                            TemplateIds:       WithTemplates ? [ ChargingPoolTemplate_Id.Parse($"{Id}*PT1") ] : null,
                                                            Facilities:        WithTemplates ? null : [ Facility.CAFE ],
                                                            ChargingStations:  [ .. Enumerable.Range(1, 2).Select(station => new ChargingStation(
                                                                                        ChargingStation_Id.Parse($"{Id}*S{pool}{station}"),
                                                                                        Created:  Created,
                                                                                        EVSEs:    [ .. Enumerable.Range(1, 2).Select(evse => new EVSE(
                                                                                                           EVSE_Id.Parse($"{Id}*E{pool}{station}*{evse}"),
                                                                                                           Created:   Created,
                                                                                                           MaxPower:  Watt.FromW(22000))) ])) ])) ],
                        ChargingPoolTemplates:     WithTemplates ? [ new ChargingPoolTemplate(ChargingPoolTemplate_Id.Parse($"{Id}*PT1"), Facilities: [ Facility.CAFE ], Created: Created) ] : null);

            return new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                      Created:                   Created,
                                      ChargingStationOperators:  [ Operator(GEF, WithTemplates: true),
                                                                   Operator(BDO, WithTemplates: false) ],
                                      EMobilityProviders:        [ new EMobilityProvider(EMobilityProvider_Id.Parse("DE*EMP"),
                                                                                         I18NString.Create(Languages.en, "Example Mobility"),
                                                                                         Created: Created) ],
                                      GridOperators:             [ new GridOperator(GridOperator_Id.Parse("DE*NET"),
                                                                                    I18NString.Create(Languages.en, "Example Netz"),
                                                                                    Created: Created) ],
                                      EnergyMeterManufacturers:  [ new EnergyMeterManufacturer(EnergyMeterManufacturer_Id.Parse("ACME"),
                                                                                               ImmutableI18NString.Create(Languages.en, "ACME Meters"),
                                                                                               Homepage:  URL.Parse("https://meters.example.org"),
                                                                                               Created:   Created) ]).WithKeys();

        }

        #endregion

        #region (static) AssertWrittenAsAWhole(Network)

        /// <summary>
        /// The JSON and CBOR ETags of the network, its operators, charging pools and charging stations must be those of
        /// their contents written as a whole, with SHA-256 and SHA-512.
        /// </summary>
        private static void AssertWrittenAsAWhole(RoamingNetwork Network)
        {

            using (Assert.EnterMultipleScope())
            {

                foreach (var hashAlgorithm in new[] { ETagHashAlgorithm.SHA256, ETagHashAlgorithm.SHA512 })
                {

                    Assert.That(Network.ContentETag(ETagFormat.JSON, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.JSON, hashAlgorithm, CanonicalJSON.ToUTF8Bytes(Network.ToJSON(IncludeETags: false)))));
                    Assert.That(Network.ContentETag(ETagFormat.CBOR, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.CBOR, hashAlgorithm, Network.ToCBOR(IncludeETags: false).ToByteArray(CBORWriterOptions.Canonical))));

                    foreach (var chargingStationOperator in Network.ChargingStationOperators)
                    {

                        Assert.That(chargingStationOperator.ContentETag(ETagFormat.JSON, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.JSON, hashAlgorithm, CanonicalJSON.ToUTF8Bytes(chargingStationOperator.ToJSON(Embedded: true)))));
                        Assert.That(chargingStationOperator.ContentETag(ETagFormat.CBOR, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.CBOR, hashAlgorithm, CBORJSON.ToCBOR(chargingStationOperator.ToJSON(Embedded: true)).ToByteArray(CBORWriterOptions.Canonical))));

                        foreach (var chargingPool in chargingStationOperator.ChargingPools)
                        {

                            Assert.That(chargingPool.ContentETag(ETagFormat.JSON, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.JSON, hashAlgorithm, CanonicalJSON.ToUTF8Bytes(chargingPool.ToJSON(Embedded: true)))));
                            Assert.That(chargingPool.ContentETag(ETagFormat.CBOR, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.CBOR, hashAlgorithm, CBORJSON.ToCBOR(chargingPool.ToJSON(Embedded: true)).ToByteArray(CBORWriterOptions.Canonical))));

                            foreach (var chargingStation in chargingPool.ChargingStations)
                                Assert.That(chargingStation.ContentETag(ETagFormat.JSON, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.JSON, hashAlgorithm, CanonicalJSON.ToUTF8Bytes(chargingStation.ToJSON(Embedded: true)))));

                        }

                    }

                    foreach (var eMobilityProvider in Network.EMobilityProviders)
                        Assert.That(eMobilityProvider.ContentETag(ETagFormat.CBOR, hashAlgorithm),  Is.EqualTo(ETag.Compute(ETagFormat.CBOR, hashAlgorithm, CBORJSON.ToCBOR(eMobilityProvider.ToJSON(Embedded: true)).ToByteArray(CBORWriterOptions.Canonical))));

                    foreach (var gridOperator in Network.GridOperators)
                        Assert.That(gridOperator.ContentETag(ETagFormat.JSON, hashAlgorithm),       Is.EqualTo(ETag.Compute(ETagFormat.JSON, hashAlgorithm, CanonicalJSON.ToUTF8Bytes(gridOperator.ToJSON(Embedded: true)))));

                }

            }

        }

        #endregion


        #region The_content_ETags_are_those_of_the_contents_written_as_a_whole()

        [Test]
        public void The_content_ETags_are_those_of_the_contents_written_as_a_whole()
        {
            AssertWrittenAsAWhole(NewNetwork());
        }

        #endregion

        #region After_a_change_the_content_ETags_are_still_those_of_the_contents_written_as_a_whole()

        /// <summary>
        /// A change of an EVSE writes the content of its charging station, charging pool, operator and roaming network
        /// anew; the other operator is the same entity, with the content it keeps.
        /// </summary>
        [Test]
        public void After_a_change_the_content_ETags_are_still_those_of_the_contents_written_as_a_whole()
        {

            var before  = NewNetwork();
            before.CalcETags();

            var evse    = before.ChargingStationOperators[0].ChargingPools.First().EVSEs.First();
            var after   = before.ApplyChangeSet(before.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "maxVoltage", null, 400) ],
                                                                             CreatedAt: T0));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(after.ChargingStationOperators[1],  Is.SameAs(before.ChargingStationOperators[1]));
                Assert.That(after.CalcChangeSetETags(),         Is.Not.EqualTo(before.CalcChangeSetETags()));
            }

            AssertWrittenAsAWhole(after);

        }

        #endregion

    }

}
