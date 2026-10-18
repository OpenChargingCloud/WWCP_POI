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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Groups of e-mobility providers: of EVSEs, charging stations and charging pools of any operator, by weak references,
    /// owned by the provider embedding them, their identifications unique in the roaming network.
    /// </summary>
    [TestFixture]
    public class ProviderGroupTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  XYZ      = ChargingStationOperator_Id.Parse("DE*XYZ");
        private static readonly EMobilityProvider_Id        GDF      = EMobilityProvider_Id.      Parse("DE-GDF");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2       = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly EVSE_Id                     X1       = EVSE_Id.                   Parse("DE*XYZ*E1*1");
        private static readonly EVSEGroup_Id                Motorway = EVSEGroup_Id.              Parse("DE-GDF*EGMOTORWAY");
        private static readonly ChargingPoolGroup_Id        Partner  = ChargingPoolGroup_Id.      Parse("DE-GDF*PGPARTNER");

        private static readonly POIPartyId                  GDFParty = new (nameof(EMobilityProvider), GDF.ToString());

        #endregion

        #region NewNetwork(ProviderGroups = null)

        /// <summary>
        /// Two charging station operators with an EVSE or two each, and an e-mobility provider with a group of EVSEs of both
        /// along a motorway and a group of partner charging pools.
        /// </summary>
        private static RoamingNetwork NewNetwork(IEnumerable<EVSEGroup>? ProviderGroups = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                            Created:        Created,
                                                                            ChargingPools:  [ new ChargingPool(P1,
                                                                                                               Created:           Created,
                                                                                                               ChargingStations:  [ new ChargingStation(S1, Created: Created, EVSEs: [ new EVSE(E1, Created: Created),
                                                                                                                                                                                        new EVSE(E2, Created: Created) ]) ]) ]),
                                                new ChargingStationOperator(XYZ,
                                                                            Created:        Created,
                                                                            ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*XYZ*P1"),
                                                                                                               Created:           Created,
                                                                                                               ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*XYZ*S1"), Created: Created, EVSEs: [ new EVSE(X1, Created: Created) ]) ]) ]) ],
                   EMobilityProviders:        [ new EMobilityProvider(GDF,
                                                                      Created:             Created,
                                                                      EVSEGroups:          ProviderGroups ?? [ new EVSEGroup(Motorway, I18NString.Create(Languages.en, "Along the A4"), EVSEIds: [ E1, X1 ], Created: Created) ],
                                                                      ChargingPoolGroups:  [ new ChargingPoolGroup(Partner, ChargingPoolIds: [ P1 ], Created: Created) ]) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork                  Network,
                                            params RoamingNetworkChange[]   Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region Provider_groups_survive_JSON_and_CBOR()

        [Test]
        public void Provider_groups_survive_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var gdf = json["eMobilityProviders"]![0]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                     Is.EqualTo(json.ToString()));
                Assert.That(fromJSON.CalcETags(),                                              Is.EqualTo(rn1.CalcETags()));

                // Embedded in the provider, naming EVSEs of two operators.
                Assert.That(gdf["EVSEGroups"]![0]!["EVSEIds"]!.Values<String>(),               Is.EqualTo(new[] { "DE*GEF*E1*1", "DE*XYZ*E1*1" }));
                Assert.That(gdf["chargingPoolGroups"]![0]!["chargingPoolIds"]!.Values<String>(),  Is.EqualTo(new[] { "DE*GEF*P1" }));

                // Found in the roaming network, also in another spelling of the provider.
                Assert.That(fromJSON.TryGetEVSEGroupById(EVSEGroup_Id.Parse("DE*GDF*EGmotorway"), out var motorway),  Is.True);
                Assert.That(motorway!.EVSEIds,                                                  Is.EqualTo(new[] { E1, X1 }));
                Assert.That(fromJSON.EVSEGroups.Select(group => group.Id),                     Is.EqualTo(new[] { Motorway }));
                Assert.That(fromJSON.TryGetChargingPoolGroupById(Partner, out _),              Is.True);

                // The groups an EVSE is in: of its operator and of providers.
                Assert.That(fromJSON.EVSEGroupsOf(X1).Select(group => group.Id),               Is.EqualTo(new[] { Motorway }));
                Assert.That(fromJSON.EVSEGroupsOf(E2),                                         Is.Empty);

            }

        }

        #endregion

        #region Invalid_provider_groups_are_refused()

        [Test]
        public void Invalid_provider_groups_are_refused()
        {

            using (Assert.EnterMultipleScope())
            {

                // A provider names its groups by its own identification.
                Assert.That(() => NewNetwork([ new EVSEGroup(EVSEGroup_Id.Parse("DE*XYZ*EG1"), EVSEIds: [ E1 ]) ]),
                            Throws.ArgumentException.With.Message.Contains("The group 'DE*XYZ*EG1' does not belong to the e-mobility provider 'DE-GDF'"));

                // Unique in the roaming network: an operator of the same prefix must not have a group of the same identification.
                var json = NewNetwork().ToJSON(IncludeETags: false);
                ((JArray) json["chargingStationOperators"]!).Add(new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GDF"),
                                                                                             Created:     Created,
                                                                                             EVSEGroups:  [ new EVSEGroup(EVSEGroup_Id.Parse("DE*GDF*EGMOTORWAY"), Created: Created) ]).ToEmbeddedJSON());

                Assert.That(RoamingNetwork.TryParse(json, out _, out var duplicateError),      Is.False);
                Assert.That(duplicateError,                                                    Does.Contain("The EVSE group 'DE*GDF*EGMOTORWAY' must not be given twice in the roaming network"));

                // References across owners are weak: an EVSE no operator has is left as it is.
                Assert.That(() => NewNetwork([ new EVSEGroup(Motorway, EVSEIds: [ EVSE_Id.Parse("DE*ABC*E9*1") ]) ]),
                            Throws.Nothing);

            }

        }

        #endregion

        #region Provider_groups_are_changed_by_change_sets_of_their_provider()

        [Test]
        public void Provider_groups_are_changed_by_change_sets_of_their_provider()
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var rn1       = history.Head.RoamingNetwork;
            rn1.TryGetEVSEGroupById(Motorway, out var motorway);

            var updated   = history.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(EVSEGroup), Motorway.ToString(), "charging", T0, out _, out var updateError);
            Assert.That(updated, Is.EqualTo(POIStatusUpdateResult.Updated), updateError);

            var stations  = ChargingStationGroup_Id.Parse("DE-GDF*SGFAST");

            var commit    = history.Commit(rn1.CreateSignedChangeSet([
                                RoamingNetworkChange.UpdateProperty(motorway!, "EVSEIds", null, new JArray(E1.ToString(), E2.ToString(), X1.ToString())),
                                RoamingNetworkChange.Add(new ChargingStationGroup(stations, ChargingStationIds: [ S1 ]), GDF)
                            ], CreatedAt: T0));

            Assert.That(commit.IsCommitted, commit.ErrorResponse);

            var rn2       = history.Head.RoamingNetwork;
            rn2.TryGetEMobilityProviderById(GDF, out var gdf2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(gdf2!.EVSEGroups.Single().EVSEIds,                                 Is.EqualTo(new[] { E1, E2, X1 }));
                Assert.That(gdf2.ChargingStationGroups.Single().Id,                            Is.EqualTo(stations));
                Assert.That(gdf2.LastChangeDate,                                               Is.EqualTo(T0));
                Assert.That(rn2.GetChargingStationOperatorById(GEF)!.LastChangeDate,           Is.EqualTo(Created));

                // The status of the group survived its change.
                Assert.That(history.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(EVSEGroup), Motorway.ToString(), out var status),  Is.True);
                Assert.That(status!.Value,                                                                                                     Is.EqualTo("charging"));

                // An operator removes a member of a provider's group without the provider: the reference is weak.
                rn2.GetChargingStationOperatorById(XYZ)!.TryGetChargingPool(ChargingPool_Id.Parse("DE*XYZ*P1"), out var xyzPool);
                var rn3 = Apply(rn2, RoamingNetworkChange.Remove(nameof(ChargingStation), "DE*XYZ*S1"));
                Assert.That(rn3.GetChargingStationOperatorById(XYZ)!.EVSEs,                    Is.Empty);
                Assert.That(rn3.EVSEGroupsOf(X1).Single().EVSEIds,                             Does.Contain(X1));

                // A group of a provider is added with its provider; the identification of another one is refused.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add(new EVSEGroup(Motorway), GDF)),
                            Throws.ArgumentException.With.Message.Contains("already exists"));
                Assert.That(() => Apply(rn2, RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE-GDF*EGOTHER")))),
                            Throws.ArgumentException);

                // A group gives nothing.
                Assert.That(() => Apply(rn2, RoamingNetworkChange.UpdateProperty(gdf2.EVSEGroups.Single(), "maxPower", null, 22000)),
                            Throws.ArgumentException.With.Message.Contains("the property 'maxPower' of a EVSE group can not be changed"));

                // Only the provider signs a change of its groups, not the operator of its members.
                var change = rn2.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gdf2.EVSEGroups.Single(), "EVSEIds", null, new JArray(E1.ToString())) ], CreatedAt: T0.AddMinutes(1));

                Assert.That(rn2.TryCheckOwnership(change.SignedBy(TestParties.Party(GEF)), out var operatorError),  Is.False);
                Assert.That(operatorError,                                                                          Does.Contain("needs a signature by"));
                Assert.That(rn2.TryCheckOwnership(change.SignedBy(GDFParty), out var providerOK),                  Is.True, providerOK);

                // Adding one to a provider needs the provider's signature as well.
                var added  = rn2.CreateChangeSet([ RoamingNetworkChange.Add(new EVSEGroup(EVSEGroup_Id.Parse("DE-GDF*EGCITY"), EVSEIds: [ E2 ]), GDF) ], CreatedAt: T0.AddMinutes(1));
                Assert.That(rn2.TryCheckOwnership(added.SignedBy(TestParties.Party(GEF)), out _),                  Is.False);
                Assert.That(rn2.TryCheckOwnership(added.SignedBy(GDFParty), out var addedOK),                      Is.True, addedOK);

            }

        }

        #endregion

        #region A_provider_is_added_without_its_groups()

        [Test]
        public void A_provider_is_added_without_its_groups()
        {

            var rn1       = NewNetwork();
            var provider  = new EMobilityProvider(EMobilityProvider_Id.Parse("DE-NEW"),
                                                  Created:     Created,
                                                  EVSEGroups:  [ new EVSEGroup(EVSEGroup_Id.Parse("DE-NEW*EG1"), EVSEIds: [ E1 ], Created: Created) ]).WithKeys();

            var add       = RoamingNetworkChange.Add(provider);
            var children  = RoamingNetworkChange.AddChildren(provider);

            var rn2       = rn1.ApplyChangeSet(rn1.CreateChangeSet([ add ], CreatedAt: T0).SignedBy(TestParties.Party(rn1)));
            var rn3       = Apply(rn2, [ .. children ]);

            rn3.TryGetEMobilityProviderById(provider.Id, out var added);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(add.NewValue!["EVSEGroups"],                                       Is.Null);
                Assert.That(children.Single().ParentEntityType,                                Is.EqualTo(nameof(EMobilityProvider)));
                Assert.That(added!.EVSEGroups.Single().EVSEIds,                                Is.EqualTo(new[] { E1 }));
            }

        }

        #endregion

        #region Changes_of_provider_groups_on_two_branches_merge()

        [Test]
        public void Changes_of_provider_groups_on_two_branches_merge()
        {

            var rn1       = NewNetwork();
            rn1.TryGetEVSEGroupById(Motorway, out var motorway);

            // Each branch adds an EVSE to the group: they merge element by element.
            var left      = Apply(rn1, RoamingNetworkChange.AddElement(nameof(EVSEGroup), Motorway.ToString(), [ new POIElementPathSegment("EVSEIds", E2.ToString()) ], E2.ToString()));
            var right     = Apply(rn1, RoamingNetworkChange.UpdateProperty(motorway!, "name", null, I18NString.Create(Languages.en, "A4 and A9").ToJSON()));

            var merged    = RoamingNetwork.TryMerge(rn1, left, right, ChangeSet_Id.Parse("merge-1"), out var merge, out var changeSet, out var conflicts);

            merge?.TryGetEVSEGroupById(Motorway, out motorway);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(merged,                                                            Is.True, String.Join(" ", conflicts.Select(conflict => conflict.Description)));
                Assert.That(motorway!.EVSEIds,                                                 Is.EqualTo(new[] { E1, E2, X1 }));
                Assert.That(motorway.Name.FirstText(),                                         Is.EqualTo("A4 and A9"));
            }

        }

        #endregion

    }

    /// <summary>
    /// Groups of e-mobility providers over HTTP.
    /// </summary>
    [TestFixture]
    public class ProviderGroupAPITests
    {

        #region Data

        private const String GDF  = "RNs/rn1/EMPs/DE-GDF";
        private const String CS   = "RNs/rn1/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1";

        private RoamingNetworkHistory?  history;
        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private HttpClient?             httpClient;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            var created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

            history      = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                               Created:                   created,
                               ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                                        Created:     created,
                                                                                        ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                           Created:           created,
                                                                                                                           ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"), Created: created, EVSEs: [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"), Created: created) ]) ]) ],
                                                                                        EVSEGroups:  [ new EVSEGroup(EVSEGroup_Id.Parse("DE*GEF*EGALL"), EVSEIds: [ EVSE_Id.Parse("DE*GEF*E1*1") ], Created: created) ]) ],
                               EMobilityProviders:        [ new EMobilityProvider(EMobilityProvider_Id.Parse("DE-GDF"),
                                                                                  Created:     created,
                                                                                  EVSEGroups:  [ new EVSEGroup(EVSEGroup_Id.Parse("DE-GDF*EGMOTORWAY"), EVSEIds: [ EVSE_Id.Parse("DE*GEF*E1*1") ], Created: created) ]) ]).WithKeys());

            httpServer   = new HTTPServer(
                               IPAddress:  IPv4Address.Localhost,
                               TCPPort:    IPPort.Zero,
                               AutoStart:  true
                           );

            poiAPI       = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

            httpClient   = new HttpClient {
                               BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/")
                           };

        }

        [TearDown]
        public async Task TearDown()
        {

            httpClient?.Dispose();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

        }

        #endregion

        #region Get(Path)

        private async Task<(HttpResponseMessage Response, JToken? JSON)> Get(String Path)
        {

            var response  = await httpClient!.GetAsync(Path);
            var content   = await response.Content.ReadAsStringAsync();

            return (response,
                    response.IsSuccessStatusCode
                        ? JToken.ReadFrom(new JsonTextReader(new StringReader(content)) { DateParseHandling = DateParseHandling.None })
                        : null);

        }

        #endregion


        #region Provider_groups_are_served_by_their_provider_and_below_their_members()

        [Test]
        public async Task Provider_groups_are_served_by_their_provider_and_below_their_members()
        {

            var (_,        provider)  = await Get(GDF);
            var (_,        without)   = await Get(GDF + "?without=EVSEGroups");
            var (_,        groups)    = await Get(GDF + "/EVSEGs");
            var (group,    groupJSON) = await Get(GDF + "/EVSEGs/DE*GDF*EGmotorway");
            var (other,    _)         = await Get(GDF + "/EVSEGs/DE*GEF*EGALL");
            var (_,        ofEVSE)    = await Get(CS  + "/EVSEs/DE*GEF*E1*1/EVSEGs");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(provider!["EVSEGroups"],                                           Is.Not.Null);
                Assert.That(without!["EVSEGroups"],                                            Is.Null);
                Assert.That(groups!.Select(entry => entry["@id"]!.Value<String>()),            Is.EqualTo(new[] { "DE*GDF*EGMOTORWAY" }));
                Assert.That(group.StatusCode,                                                  Is.EqualTo(HttpStatusCode.OK));
                Assert.That(groupJSON!["EVSEIds"]!.Values<String>(),                           Is.EqualTo(new[] { "DE*GEF*E1*1" }));

                // A group of the operator is none of the provider.
                Assert.That(other.StatusCode,                                                  Is.EqualTo(HttpStatusCode.NotFound));

                // Below the EVSE: the groups of its operator and of providers.
                Assert.That(ofEVSE!.Select(entry => entry["@id"]!.Value<String>()),            Is.EquivalentTo(new[] { "DE*GEF*EGALL", "DE*GDF*EGMOTORWAY" }));

            }

        }

        #endregion

    }

}
