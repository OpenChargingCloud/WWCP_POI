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
    /// A change set may name the ETags entities must still have: it applies, and is rebased, only while they do.
    /// The ETag of an entity is the one POI_API serves it with.
    /// </summary>
    [TestFixture]
    public class PreconditionTests
    {

        #region Data

        private static readonly DateTimeOffset              Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0       = DateTimeOffset.Parse("2026-10-16T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF      = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingStationOperator_Id  EXA      = ChargingStationOperator_Id.Parse("DE*EXA");
        private static readonly ChargingPool_Id             P1       = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly EVSE_Id                     E1       = EVSE_Id.                   Parse("DE*GEF*E1*1");

        #endregion

        #region NewNetwork()

        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                  Created:                   Created,
                                  ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                                           Created:        Created,
                                                                                           ChargingPools:  [ new ChargingPool(P1,
                                                                                                                              Created:           Created,
                                                                                                                              ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                                                       Created:  Created,
                                                                                                                                                                       EVSEs:    [ new EVSE(E1, Created: Created) ]) ]) ]),
                                                               new ChargingStationOperator(EXA, Created: Created) ]).WithKeys();

        #endregion

        #region Apply(Network, Changes)

        private static RoamingNetwork Apply(RoamingNetwork Network, params RoamingNetworkChange[] Changes)

            => Network.ApplyChangeSet(Network.CreateSignedChangeSet(Changes, CreatedAt: T0));

        #endregion


        #region The_ETag_of_an_entity_is_the_one_it_is_served_with()

        [Test]
        public void The_ETag_of_an_entity_is_the_one_it_is_served_with()
        {

            var rn1   = NewNetwork();
            var gef   = rn1.GetChargingStationOperatorById(GEF)!;
            var pool  = gef.ChargingPools.Single();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn1.TryGetEntityETag(nameof(ChargingStationOperator), GEF.ToString(), out var operatorETag),  Is.True);
                Assert.That(operatorETag,  Is.EqualTo(POIRepresentation.Of(gef.ToEmbeddedJSON(), ETagFormat.JSON).ETag));

                Assert.That(rn1.TryGetEntityETag(nameof(ChargingPool), P1.ToString(), out var poolETag),                  Is.True);
                Assert.That(poolETag,      Is.EqualTo(POIRepresentation.Of(pool.ToJSON(Embedded: true), ETagFormat.JSON).ETag));

                Assert.That(rn1.TryGetEntityETag(nameof(ChargingPool), P1.ToString(), out var poolCBOR, ETagFormat.CBOR), Is.True);
                Assert.That(poolCBOR,      Is.EqualTo(POIRepresentation.Of(pool.ToJSON(Embedded: true), ETagFormat.CBOR).ETag));

                // The roaming network itself has the ETag of its content.
                Assert.That(rn1.TryGetEntityETag(nameof(RoamingNetwork), "rn1", out var networkETag),                     Is.True);
                Assert.That(networkETag,   Is.EqualTo(rn1.CalcETags().First(eTag => eTag.Format == ETagFormat.JSON)));

                // In every spelling its type considers equal; unknown entities have none.
                Assert.That(rn1.TryGetEntityETag(nameof(ChargingPool), "DEGEF*P1", out var respelled),                   Is.True);
                Assert.That(respelled,     Is.EqualTo(poolETag));
                Assert.That(rn1.TryGetEntityETag(nameof(ChargingPool), "DE*GEF*P9", out _),                               Is.False);

            }

        }

        #endregion

        #region A_change_set_is_rebased_only_while_its_preconditions_hold()

        /// <summary>
        /// A change of an EVSE, prepared while its charging pool had a certain ETag: rebased past a change of another
        /// operator, but not past a change of the pool, though that one changed another property.
        /// </summary>
        [Test]
        public void A_change_set_is_rebased_only_while_its_preconditions_hold()
        {

            var rn1        = NewNetwork();
            var evse       = rn1.GetChargingStationOperatorById(GEF)!.EVSEs.Single();
            var pool       = rn1.GetChargingStationOperatorById(GEF)!.ChargingPools.Single();

            var guarded    = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 11000) ],
                                                 CreatedAt:      T0,
                                                 Preconditions:  [ rn1.Precondition(nameof(ChargingPool), P1.ToString()) ]);

            var unguarded  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 11000) ], CreatedAt: T0);

            var other      = Apply(rn1, RoamingNetworkChange.UpdateProperty(rn1.GetChargingStationOperatorById(EXA)!, "telephone", null, "+49 30 1"));
            var poolChange = Apply(rn1, RoamingNetworkChange.UpdateProperty(pool, "hotline", null, "+49 800 1234"));

            // Read again from JSON, as a peer does.
            Assert.That(RoamingNetworkChangeSet.TryParse(guarded.ToJSON(), out var parsed, out var parseError),  Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(parsed!.Preconditions,                                     Is.EqualTo(guarded.Preconditions));
                Assert.That(guarded.ToJSON(IncludeSignatures: false)["preconditions"],  Is.Not.Null, "signed with the change set");
                Assert.That(unguarded.ToJSON()["preconditions"],                        Is.Null,     "written only when there are any");

                Assert.That(other.TryRebase(guarded, out var rebased, out var error1),  Is.True, error1);
                Assert.That(rebased!.Preconditions,                                    Is.EqualTo(guarded.Preconditions));
                Assert.That(other.TryApplyChangeSet(rebased.SignedByAll(other), out _, out var error2),  Is.True, error2);

                Assert.That(poolChange.TryRebase(guarded, out _, out var error3),       Is.False);
                Assert.That(error3,                                                     Does.Contain($"Precondition 0: the ChargingPool '{P1}' changed, it no longer has the ETag"));

                // Without it, the change of another property of the pool does not keep it from being rebased.
                Assert.That(poolChange.TryRebase(unguarded, out _, out var error4),     Is.True, error4);

            }

        }

        #endregion

        #region Preconditions_that_do_not_hold_are_refused()

        [Test]
        public void Preconditions_that_do_not_hold_are_refused()
        {

            var rn1       = NewNetwork();
            var evse      = rn1.GetChargingStationOperatorById(GEF)!.EVSEs.Single();
            var before    = rn1.Precondition(nameof(EVSE), E1.ToString());
            var rn2       = Apply(rn1, RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 22000));

            var changes   = new[] { RoamingNetworkChange.UpdateProperty(nameof(EVSE), E1.ToString(), "maxVoltage", null, 230) };
            var valid     = rn2.CreateChangeSet(changes, CreatedAt: T0);

            // Prepared without it, then given a stale one, and signed: the signature is fine, the precondition is not.
            var stale     = new RoamingNetworkChangeSet(valid.Id, valid.RoamingNetworkId, valid.CreatedAt, valid.Changes, valid.BeforeETags, valid.AfterETags,
                                                        Preconditions: [ before ]).SignedByAll(rn2);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => rn2.CreateChangeSet(changes, CreatedAt: T0, Preconditions: [ before ]),
                            Throws.ArgumentException.With.Message.Contains($"Precondition 0: the EVSE '{E1}' changed"));

                Assert.That(() => rn2.CreateChangeSet(changes, CreatedAt: T0, Preconditions: [ new RoamingNetworkPrecondition(nameof(EVSE), "DE*GEF*E1*9", before.ETag) ]),
                            Throws.ArgumentException.With.Message.Contains("Precondition 0: the EVSE 'DE*GEF*E1*9' does not exist"));

                Assert.That(rn2.TryApplyChangeSet(stale, out _, out var error),  Is.False);
                Assert.That(error,                                                Does.Contain($"Precondition 0: the EVSE '{E1}' changed"));

                Assert.That(() => rn1.Precondition(nameof(EVSE), "DE*GEF*E1*9"),  Throws.ArgumentException);

            }

        }

        #endregion

        #region The_ETags_of_a_change_set_are_written_in_hex()

        /// <summary>
        /// A change set is signed as it is written: its ETags in base64, which it would write in hex again, are refused.
        /// </summary>
        [Test]
        public void The_ETags_of_a_change_set_are_written_in_hex()
        {

            var rn1        = NewNetwork();
            var evse       = rn1.GetChargingStationOperatorById(GEF)!.EVSEs.Single();
            var pool       = rn1.Precondition(nameof(ChargingPool), P1.ToString());

            var changeSet  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 11000) ],
                                                 CreatedAt:      T0,
                                                 Preconditions:  [ pool ]).SignedByAll(rn1);

            JObject With(Action<JObject> Change)
            {
                var json = changeSet.ToJSON();
                Change(json);
                return json;
            }

            var base64Precondition  = With(json => json["preconditions"]![0]!["eTag"] = pool.ETag.ToJSON(ETagDigestEncoding.Base64));
            var base64BeforeETag    = With(json => json["beforeETags"]![0]           = changeSet.BeforeETags[0].ToJSON(ETagDigestEncoding.Base64));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(RoamingNetworkChangeSet.TryParse(changeSet.ToJSON(),  out _, out var hexError),          Is.True, hexError);
                Assert.That(RoamingNetworkChangeSet.TryParse(base64Precondition,  out _, out var preconditionError),  Is.False);
                Assert.That(RoamingNetworkChangeSet.TryParse(base64BeforeETag,    out _, out var beforeError),        Is.False);
                Assert.That(beforeError,                                                                               Does.Contain("must be written in hex"));
            }

        }

        #endregion

        #region Preconditions_are_limited()

        /// <summary>
        /// A change set read names at most 1,000 preconditions; an entity named again is checked with the ETag calculated once.
        /// </summary>
        [Test]
        public void Preconditions_are_limited()
        {

            var rn1        = NewNetwork();
            var evse       = rn1.GetChargingStationOperatorById(GEF)!.EVSEs.Single();
            var pool       = rn1.Precondition(nameof(ChargingPool), P1.ToString());

            // 1,000 times the same entity: calculated once, and fine.
            var manyTimes  = rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(evse, "maxPower", null, 11000) ],
                                                 CreatedAt:      T0,
                                                 Preconditions:  Enumerable.Repeat(pool, 1_000)).SignedByAll(rn1);

            var json       = manyTimes.ToJSON();
            ((JArray) json["preconditions"]!).Add(pool.ToJSON());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn1.TryApplyChangeSet(manyTimes, out _, out var applyError),                         Is.True, applyError);
                Assert.That(RoamingNetworkChangeSet.TryParse(manyTimes.ToJSON(), out _, out var parseError),      Is.True, parseError);
                Assert.That(RoamingNetworkChangeSet.TryParse(json, out _, out var limitError),                   Is.False);
                Assert.That(limitError,                                                                           Does.Contain("exceeds the limit of 1000 preconditions"));
            }

        }

        #endregion

    }

}
