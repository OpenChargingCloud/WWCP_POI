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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Every change set is signed by the owners of what it changes, with keys the roaming network names:
    /// a charging station operator changes only its own data, the roaming network operator adds parties.
    /// </summary>
    [TestFixture]
    public class OwnershipTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-13T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  EXA      = ChargingStationOperator_Id.Parse("DE*EXA");
        private static readonly ParkingOperator_Id          PAR      = ParkingOperator_Id.        Parse("DE*PAR");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly ParkingGarage_Id            Garage   = ParkingGarage_Id.          Parse("garage-1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  DataSource:                "v1",
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                           Created:        Created,
                                                                                           ChargingPools:  [ new ChargingPool(P1,
                                                                                                                              Created:           Created,
                                                                                                                              ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                       Created:  Created,
                                                                                                                                                                       EVSEs:    [ new EVSE(E1, Created: Created) ]) ]) ]),
                                                               new ChargingStationOperator(EXA, Created: Created) ],
                                  ParkingOperators:          [ new ParkingOperator(PAR,
                                                                                   Created:         Created,
                                                                                   ParkingGarages:  [ new ParkingGarage(Garage, Created: Created) ]) ]).WithKeys();

        #endregion

        #region Change(Network, Changes)

        private static RoamingNetworkChangeSet Change(RoamingNetwork                Network,
                                                      params RoamingNetworkChange[] Changes)

            => Network.CreateChangeSet(Changes, CreatedAt: T0);

        #endregion

        #region Apply(Network, ChangeSet)

        private static (Boolean Applied, String? Error) Apply(RoamingNetwork           Network,
                                                              RoamingNetworkChangeSet  ChangeSet)

            => (Network.TryApplyChangeSet(ChangeSet, out _, out var error), error);

        #endregion


        #region Keys_are_part_of_the_content()

        [Test]
        public void Keys_are_part_of_the_content()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();
            var key   = TestParties.Key(TestParties.Party(GEF)).Public;

            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(),                    out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);
            Assert.That(RoamingNetwork.TryParse(JObject.Parse(json.ToString()),  out var fromText, out var errorText),  Is.True, errorText);

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            JObject GEFJSON(JObject Network)
                => (JObject) Network["chargingStationOperators"]!.Single(op => op["@id"]!.Value<String>() == "DE*GEF");

            var wrongId     = Changed(network => GEFJSON(network)["signingKeys"]![0]!["@id"] = new String('0', 64));
            var bothLists   = Changed(network => GEFJSON(network)["identityKeys"] = GEFJSON(network)["signingKeys"]!.DeepClone());
            var notBase64   = Changed(network => GEFJSON(network)["signingKeys"]![0]!["key"] = "no key");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromCBOR!.CalcETags(),                                                    Is.EqualTo(rn1.CalcETags()));
                Assert.That(fromText!.ToJSON().ToString(),                                            Is.EqualTo(json.ToString()));

                var gef = fromCBOR.GetChargingStationOperatorById(GEF)!;
                Assert.That(gef.SigningKeys.Single().Id,                                              Is.EqualTo(key.Id));
                Assert.That(gef.SigningKeys.Single().Key.Curve,                                       Is.EqualTo(COSECurve.P256));
                Assert.That(gef.IdentityKeys.Single().Key.Curve,                                      Is.EqualTo(COSECurve.P521));
                Assert.That(fromCBOR.SigningKeys.Single().Id,                                         Is.EqualTo(TestParties.Key(TestParties.Party(rn1)).Public.Id));
                Assert.That(key.Id,                                                                   Has.Length.EqualTo(64));

                Assert.That(RoamingNetwork.TryParse(wrongId,   out _, out var error1),               Is.False);
                Assert.That(error1,                                                                   Does.Contain("is not its thumbprint"));
                Assert.That(RoamingNetwork.TryParse(bothLists, out _, out var error2),               Is.False);
                Assert.That(error2,                                                                   Does.Contain("must not be both a signing and an identity key"));
                Assert.That(RoamingNetwork.TryParse(notBase64, out _, out var error3),               Is.False);
                Assert.That(error3,                                                                   Does.Contain("signing key"));

                // A private key is published as its public half.
                Assert.That(POIPublicKey.From(TestParties.Key(TestParties.Party(GEF)).Private).Id,  Is.EqualTo(key.Id));
                Assert.That(POIPublicKey.From(TestParties.Key(TestParties.Party(GEF)).Private).Key.IsPrivate,  Is.False);

            }

        }

        #endregion

        #region An_operator_changes_only_its_own_data()

        [Test]
        public void An_operator_changes_only_its_own_data()
        {

            var rn1   = NewNetwork();
            var pool  = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
            var evse  = pool.EVSEs.Single();
            var gef   = rn1.GetChargingStationOperatorById(GEF)!;

            var poolByGEF    = Change(rn1, RoamingNetworkChange.UpdateProperty(pool, "hotline", null, "+49 800 1234")).SignedBy(TestParties.Party(GEF));
            var poolByEXA    = Change(rn1, RoamingNetworkChange.UpdateProperty(pool, "hotline", null, "+49 800 1234")).SignedBy(TestParties.Party(EXA));
            var evseByRNO    = Change(rn1, RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 11000)).       SignedBy(TestParties.Party(rn1));
            var gefByEXA     = Change(rn1, RoamingNetworkChange.UpdateProperty(gef,  "telephone", null, "+49 30 1")). SignedBy(TestParties.Party(EXA));
            var gefByGEF     = Change(rn1, RoamingNetworkChange.UpdateProperty(gef,  "telephone", null, "+49 30 1")). SignedBy(TestParties.Party(GEF));
            var networkByGEF = Change(rn1, RoamingNetworkChange.UpdateProperty(rn1,  "dataSource", null, "v2")).      SignedBy(TestParties.Party(GEF));

            // A change set of two owners needs both.
            var both         = Change(rn1, RoamingNetworkChange.UpdateProperty(rn1,  "dataSource", null, "v2"),
                                           RoamingNetworkChange.UpdateProperty(pool, "hotline",    null, "+49 800 1234"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Apply(rn1, poolByGEF).Applied,       Is.True, Apply(rn1, poolByGEF).Error);
                Assert.That(Apply(rn1, gefByGEF). Applied,       Is.True, Apply(rn1, gefByGEF). Error);

                Assert.That(Apply(rn1, poolByEXA).Error,         Does.Contain("needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));
                Assert.That(Apply(rn1, evseByRNO).Error,         Does.Contain("of the EVSE 'DE*GEF*E1*1' ('maxPower') needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));
                Assert.That(Apply(rn1, gefByEXA). Error,         Does.Contain("needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));
                Assert.That(Apply(rn1, networkByGEF).Error,      Does.Contain("needs a signature by a signing key of the roaming network operator"));

                Assert.That(Apply(rn1, both.SignedBy(TestParties.Party(rn1))).Error,  Does.Contain("Change 1"));
                Assert.That(Apply(rn1, both.SignedBy(TestParties.Party(rn1)).
                                            SignedBy(TestParties.Party(GEF))).Applied,  Is.True);

                // An identity key does not sign data.
                Assert.That(Apply(rn1, Change(rn1, RoamingNetworkChange.UpdateProperty(pool, "hotline", null, "+49 800 1234")).
                                           SignedBy(TestParties.Party(GEF), POIKeyKind.Identity)).Error,
                            Does.Contain("needs a signature by a signing key"));

            }

        }

        #endregion

        #region A_parking_operator_changes_its_parking_data()

        [Test]
        public void A_parking_operator_changes_its_parking_data()
        {

            var rn1     = NewNetwork();
            var garage  = rn1.ParkingGarages.Single();

            var byPAR   = Change(rn1, RoamingNetworkChange.UpdateProperty(garage, "openingTimes", null, OpeningTimes.Open24Hours.ToJSON())).SignedBy(TestParties.Party(PAR));
            var byGEF   = Change(rn1, RoamingNetworkChange.UpdateProperty(garage, "openingTimes", null, OpeningTimes.Open24Hours.ToJSON())).SignedBy(TestParties.Party(GEF));

            // A space added and changed within one change set belongs to the parking operator it is added to.
            var added   = Change(rn1, RoamingNetworkChange.Add(new ParkingSpace(ParkingSpace_Id.Parse("space-9")), PAR),
                                      RoamingNetworkChange.UpdateProperty(nameof(ParkingSpace), "space-9", "parkingGarageId", null, "garage-1"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Apply(rn1, byPAR).Applied,                                       Is.True, Apply(rn1, byPAR).Error);
                Assert.That(Apply(rn1, byGEF).Error,                                         Does.Contain("needs a signature by a signing key of the ParkingOperator 'DE*PAR'"));
                Assert.That(Apply(rn1, added.SignedBy(TestParties.Party(PAR))).Applied,     Is.True, Apply(rn1, added.SignedBy(TestParties.Party(PAR))).Error);
                Assert.That(Apply(rn1, added.SignedBy(TestParties.Party(GEF))).Error,       Does.Contain("ParkingOperator 'DE*PAR'"));
            }

        }

        #endregion

        #region The_roaming_network_operator_adds_parties_with_keys_and_without_children()

        [Test]
        public void The_roaming_network_operator_adds_parties_with_keys_and_without_children()
        {

            var rn1          = NewNetwork();
            var NEW          = ChargingStationOperator_Id.Parse("DE*NEW");

            var withKeys     = new ChargingStationOperator(NEW, Created: Created).WithKeys();
            var withoutKeys  = new ChargingStationOperator(NEW, Created: Created);
            var withPool     = new ChargingStationOperator(NEW, Created: Created,
                                                           ChargingPools: [ new ChargingPool(ChargingPool_Id.Parse("DE*NEW*P1")) ]).WithKeys();

            var byRNO        = Change(rn1, RoamingNetworkChange.Add(withKeys)).SignedBy(TestParties.Party(rn1));
            var byGEF        = Change(rn1, RoamingNetworkChange.Add(withKeys)).SignedBy(TestParties.Party(GEF));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Apply(rn1, byRNO).Applied,  Is.True, Apply(rn1, byRNO).Error);
                Assert.That(Apply(rn1, byGEF).Error,    Does.Contain("needs a signature by a signing key of the roaming network operator"));

                Assert.That(Apply(rn1, Change(rn1, RoamingNetworkChange.Add(withoutKeys)).SignedBy(TestParties.Party(rn1))).Error,
                            Does.Contain("must be added with its signingKeys"));

                Assert.That(Apply(rn1, Change(rn1, RoamingNetworkChange.Add(nameof(ChargingStationOperator), "DE*NEW", withPool.ToEmbeddedJSON())).SignedBy(TestParties.Party(rn1))).Error,
                            Does.Contain("must be added without its chargingPools"));

                // Removed by the roaming network operator only.
                Assert.That(Apply(rn1, Change(rn1, RoamingNetworkChange.Remove(EXA)).SignedBy(TestParties.Party(EXA))).Error,
                            Does.Contain("needs a signature by a signing key of the roaming network operator"));
                Assert.That(Apply(rn1, Change(rn1, RoamingNetworkChange.Remove(EXA)).SignedBy(TestParties.Party(rn1))).Applied,
                            Is.True);

            }

        }

        #endregion

        #region Operators_are_added_without_their_children_which_they_add_themselves()

        /// <summary>
        /// Add(operator) leaves the children out, AddChildren(operator) adds them in a change set of their owner.
        /// </summary>
        [Test]
        public void Operators_are_added_without_their_children_which_they_add_themselves()
        {

            var rn1   = NewNetwork();
            var NEW   = ChargingStationOperator_Id.Parse("DE*NEW");
            var PRK   = ParkingOperator_Id.        Parse("DE*PRK");
            var NE9   = GridOperator_Id.           Parse("DE*NE9");
            var T1    = ChargingTariff_Id.         Parse("DE*NEW*T1");
            var E1    = EVSE_Id.                   Parse("DE*NEW*E1*1");
            var S1    = ParkingSpace_Id.           Parse("space-91");

            var cso   = new ChargingStationOperator(NEW,
                                                    Created:               Created,
                                                    ChargingTariffs:       [ new ChargingTariff(T1,
                                                                                                Currency.EUR,
                                                                                                [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                Created: Created) ],
                                                    ChargingPools:         [ new ChargingPool(ChargingPool_Id.Parse("DE*NEW*P1"),
                                                                                              Created:           Created,
                                                                                              ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*NEW*S1"),
                                                                                                                                       Created:  Created,
                                                                                                                                       EVSEs:    [ new EVSE(E1, Created: Created) ]) ]) ],
                                                    EVSEGroups:            [ new EVSEGroup          (EVSEGroup_Id.          Parse("DE*NEW*EG1"), EVSEIds:           [ E1 ], Created: Created) ],
                                                    ChargingTariffGroups:  [ new ChargingTariffGroup(ChargingTariffGroup_Id.Parse("DE*NEW*TG1"), ChargingTariffIds: [ T1 ], Created: Created) ]).WithKeys();

            var prk   = new ParkingOperator(PRK,
                                            Created:             Created,
                                            ParkingGarages:      [ new ParkingGarage    (ParkingGarage_Id.    Parse("garage-91"), Created: Created) ],
                                            ParkingSpaces:       [ new ParkingSpace     (S1,                                      Created: Created) ],
                                            ParkingSpaceGroups:  [ new ParkingSpaceGroup(ParkingSpaceGroup_Id.Parse("level-91"),  ParkingSpaceIds: [ S1 ], Created: Created) ]).WithKeys();

            var grid  = new GridOperator(NE9,
                                         GridConnectionPoints:  [ new GridConnectionPoint(GridConnectionPoint_Id.Parse("connection-91")) ],
                                         Created:               Created).WithKeys();

            var added = Change(rn1, RoamingNetworkChange.Add(cso),
                                    RoamingNetworkChange.Add(prk),
                                    RoamingNetworkChange.Add(grid)).SignedBy(TestParties.Party(rn1));

            Assert.That(rn1.TryApplyChangeSet(added, out var rn2, out var error), Is.True, error);

            var children = Change(rn2!, [ .. RoamingNetworkChange.AddChildren(cso),
                                          .. RoamingNetworkChange.AddChildren(prk),
                                          .. RoamingNetworkChange.AddChildren(grid) ]);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2!.GetChargingStationOperatorById(NEW)!.ChargingPools.Count,  Is.Zero);
                Assert.That(rn2. ParkingOperators.Single(o => o.Id == PRK).ParkingSpaces.Count,  Is.Zero);
                Assert.That(rn2. GridOperators.   Single(o => o.Id == NE9).GridConnectionPoints.Count,  Is.Zero);

                Assert.That(children.Changes.Select(change => change.EntityType),
                            Is.EqualTo(new[] { "ChargingTariff", "ChargingPool", "EVSEGroup", "ChargingTariffGroup",
                                               "ParkingGarage", "ParkingSpace", "ParkingSpaceGroup", "GridConnectionPoint" }));

                // Children are their owners' to add.
                Assert.That(Apply(rn2, children.SignedBy(TestParties.Party(rn1))).Error,  Does.Contain("needs a signature by a signing key of the ChargingStationOperator 'DE*NEW'"));

            }

            Assert.That(rn2.TryApplyChangeSet(children.SignedBy(TestParties.Party(NEW)).
                                                       SignedBy(TestParties.Party(PRK)).
                                                       SignedBy(TestParties.Party(NE9)), out var rn3, out error),  Is.True, error);

            var cso3  = rn3!.GetChargingStationOperatorById(NEW)!.ToEmbeddedJSON();
            var prk3  = rn3. ParkingOperators.Single(o => o.Id == PRK).ToJSON();
            var grid3 = rn3. GridOperators.   Single(o => o.Id == NE9).ToJSON();

            // The children are as they were, changed last when they were added.
            static JToken? Content(JToken? Children)
            {
                var content = Children?.DeepClone();
                foreach (var lastChange in content?.SelectTokens("$..lastChange").ToArray() ?? [])
                    lastChange.Parent!.Remove();
                return content;
            }

            using (Assert.EnterMultipleScope())
            {

                foreach (var key in new[] { "chargingTariffs", "chargingPools", "EVSEGroups", "chargingTariffGroups" })
                    Assert.That(Content(cso3[key]),  Is.EqualTo(Content(cso.ToEmbeddedJSON()[key])).Using<JToken>(JToken.DeepEquals), key);
                foreach (var key in new[] { "parkingGarages", "parkingSpaces", "parkingSpaceGroups" })
                    Assert.That(Content(prk3[key]),  Is.EqualTo(Content(prk.ToJSON()[key])).       Using<JToken>(JToken.DeepEquals), key);
                Assert.That(Content(grid3["gridConnectionPoints"]),  Is.EqualTo(Content(grid.ToJSON()["gridConnectionPoints"])).Using<JToken>(JToken.DeepEquals));

                Assert.That(cso3["chargingPools"]![0]!["lastChange"]?.Value<String>(),  Is.EqualTo("2026-10-13T12:00:00.000Z"));

            }

        }

        #endregion

        #region Keys_are_changed_by_their_owners()

        /// <summary>
        /// Signing keys are rotated by the signing keys they replace, or by an identity key;
        /// identity keys only by an identity key. The roaming network operator recovers the keys of every party.
        /// </summary>
        [Test]
        public void Keys_are_changed_by_their_owners()
        {

            var rn1       = NewNetwork();
            var gef       = rn1.GetChargingStationOperatorById(GEF)!;
            var rotated   = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF#2"));
            var newKeys   = new JArray(rotated.Public.ToJSON());

            RoamingNetworkChangeSet Rotate()
                => Change(rn1, RoamingNetworkChange.UpdateProperty(gef, "signingKeys", null, newKeys));

            var bySigning   = Rotate().SignedBy(TestParties.Party(GEF), POIKeyKind.Signing);
            var byIdentity  = Rotate().SignedBy(TestParties.Party(GEF), POIKeyKind.Identity);
            var byRNO       = Rotate().SignedBy(TestParties.Party(rn1), POIKeyKind.Identity);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Apply(rn1, bySigning).Applied,   Is.True, "Rotated by the signing key it replaces.");
                Assert.That(Apply(rn1, byRNO).Applied,       Is.True, "Recovery by the roaming network operator.");

                Assert.That(rn1.TryApplyChangeSet(byIdentity, out var rn2, out var error), Is.True, error);

                // Identity keys only by an identity key.
                var rotatedIdentity = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF#3"));
                var identityChange  = Change(rn1, RoamingNetworkChange.UpdateProperty(gef, "identityKeys", null, new JArray(rotatedIdentity.Public.ToJSON())));

                Assert.That(Apply(rn1, identityChange.SignedBy(TestParties.Party(GEF))).Error,
                            Does.Contain("needs a signature by an identity key of the ChargingStationOperator 'DE*GEF' or an identity key of the roaming network operator"));
                Assert.That(Apply(rn1, identityChange.SignedBy(TestParties.Party(GEF), POIKeyKind.Identity)).Applied,
                            Is.True);

                // From now on, the new key signs, the old one no longer.
                var pool = rn2!.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();
                var next = rn2.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(pool, "hotline", null, "+49 800 1234") ], CreatedAt: T0);

                Assert.That(Apply(rn2, next.SignedBy(TestParties.Party(GEF))).Error,
                            Does.Contain("is no key of a party"));
                Assert.That(Apply(rn2, next.Sign(rotated.Private, rotated.Public.Id, COSEAlgorithm.ES256)).Applied,
                            Is.True);

                // A party keeps at least one key of each kind.
                Assert.That(Apply(rn1, Change(rn1, RoamingNetworkChange.UpdateProperty(gef, "signingKeys", null, new JArray())).
                                           SignedBy(TestParties.Party(GEF), POIKeyKind.Identity)).Error,
                            Does.Contain("must keep at least one of its signingKeys"));
                Assert.That(Apply(rn1, Change(rn1, RoamingNetworkChange.RemoveProperty(gef, "identityKeys")).
                                           SignedBy(TestParties.Party(GEF), POIKeyKind.Identity)).Error,
                            Does.Contain("must keep at least one of its identityKeys"));

                // The keys of the roaming network operator itself, without anybody to recover them:
                // its signing keys by a signing or an identity key, its identity keys only by an identity key.
                var rnoSigningKeys   = Change(rn1, RoamingNetworkChange.UpdateProperty(rn1, "signingKeys",  null, newKeys));
                var rnoIdentityKeys  = Change(rn1, RoamingNetworkChange.UpdateProperty(rn1, "identityKeys", null, new JArray(rotatedIdentity.Public.ToJSON())));

                Assert.That(Apply(rn1, rnoSigningKeys. SignedBy(TestParties.Party(rn1))).Applied,                       Is.True);
                Assert.That(Apply(rn1, rnoSigningKeys. SignedBy(TestParties.Party(rn1), POIKeyKind.Identity)).Applied,  Is.True);
                Assert.That(Apply(rn1, rnoIdentityKeys.SignedBy(TestParties.Party(rn1))).Error,
                            Does.Contain("needs a signature by an identity key of the roaming network operator"));
                Assert.That(Apply(rn1, rnoIdentityKeys.SignedBy(TestParties.Party(rn1), POIKeyKind.Identity)).Applied,  Is.True);

            }

        }

        #endregion

        #region Parties_are_named_in_every_spelling_of_their_identification()

        /// <summary>
        /// A change names a party, or the roaming network, in any spelling its identification type considers equal,
        /// and needs the signature of that party, as with the spelling it is kept with.
        /// </summary>
        [Test]
        public void Parties_are_named_in_every_spelling_of_their_identification()
        {

            var rn1       = NewNetwork();

            var operator1 = Change(rn1, RoamingNetworkChange.UpdateProperty("ChargingStationOperator", "degef", "telephone",  null, "+49 30 1234567", "RoamingNetwork", "RN1"));
            var network   = Change(rn1, RoamingNetworkChange.UpdateProperty("RoamingNetwork",          "RN1",   "dataSource", null, "v2"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.TryApplyChangeSet(operator1.SignedBy(TestParties.Party(GEF)), out var rn2, out var error),  Is.True, error);
                Assert.That(rn2!.GetChargingStationOperatorById(GEF)!.Telephone?.ToString(),                                   Is.EqualTo("+49 30 1234567"));

                Assert.That(rn1.TryApplyChangeSet(network.SignedBy(TestParties.Party(rn1)), out var rn3, out error),         Is.True, error);
                Assert.That(rn3!.DataSource,                                                                                   Is.EqualTo("v2"));

                // Still only by its own keys.
                Assert.That(Apply(rn1, operator1.SignedBy(TestParties.Party(EXA))).Error,
                            Does.Contain("needs a signature by a signing key of the ChargingStationOperator 'DE*GEF'"));

            }

        }

        #endregion

        #region Keys_are_valid_for_their_time()

        [Test]
        public void Keys_are_valid_for_their_time()
        {

            var rn1      = NewNetwork();
            var gef      = rn1.GetChargingStationOperatorById(GEF)!;
            var expired  = new POIPublicKey(TestParties.Key(TestParties.Party(GEF)).Public.Key, NotAfter: T0.AddDays(-1));

            var rn2      = rn1.ApplyChangeSet(Change(rn1, RoamingNetworkChange.UpdateProperty(gef, "signingKeys", null, new JArray(expired.ToJSON()))).
                                                  SignedBy(TestParties.Party(GEF), POIKeyKind.Identity));

            var pool     = rn2.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();

            Assert.That(Apply(rn2, rn2.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(pool, "hotline", null, "+49 800 1234") ], CreatedAt: T0).
                                       SignedBy(TestParties.Party(GEF))).Error,
                        Does.Contain("is not valid at 2026-10-13T12:00:00.000Z"));

        }

        #endregion

    }

}
