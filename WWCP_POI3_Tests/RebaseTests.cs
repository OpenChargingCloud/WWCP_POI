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

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.ChangeSets
{

    /// <summary>
    /// Rebasing change sets prepared concurrently against an older version of a roaming network.
    /// </summary>
    [TestFixture]
    public class RebaseTests
    {

        #region Data

        private static readonly Ed25519PrivateKeyParameters Alice = new (Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);

        private static RoamingNetwork NewNetwork(Boolean WithDescription = true)

            => new RoamingNetwork(
                   Id:                        RoamingNetwork_Id.Parse("rn1"),
                   Name:                      I18NString.Create(Languages.en, "Roaming Network 1"),
                   Description:               WithDescription
                                                  ? I18NString.Create(Languages.en, "My first roaming network!")
                                                  : null,
                   DataSource:                "test",
                   ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"), I18NString.Create(Languages.en, "GraphDefined")) ]
               ).WithKeys();

        #endregion


        #region A_fitting_change_set_stays_as_it_is()

        [Test]
        public void A_fitting_change_set_stays_as_it_is()
        {

            var rn1  = NewNetwork();
            var cs1  = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]);

            Assert.That(rn1.TryRebase(cs1, out var rebased, out var error),  Is.True, error);
            Assert.That(rebased,                                             Is.SameAs(cs1));

        }

        #endregion

        #region Changes_of_other_values_are_rebased()

        [Test]
        public void Changes_of_other_values_are_rebased()
        {

            var rn1   = NewNetwork();
            var csA   = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]);
            var csB   = rn1.CreateSignedChangeSet([
                            RoamingNetworkChange.RemoveProperty(rn1, "description"),
                            RoamingNetworkChange.Add(new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*NEW")).WithKeys())
                        ],
                        Description: I18NString.Create(Languages.en, "B"),
                        Metadata:    new JObject(new JProperty("author", "bob")));

            var csC   = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "name", null, new JObject(new JProperty("en", "C"))) ]);

            var rn2   = rn1.ApplyChangeSet(csA);
            var csB2  = rn2.Rebase(csB);
            var rn3   = rn2.ApplyChangeSet(csB2.SignedByAll(rn2));
            var csC3  = rn3.Rebase(rn2.Rebase(csC));
            var rn4   = rn3.ApplyChangeSet(csC3.SignedByAll(rn3));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(csB2.Id,                                      Is.Not.EqualTo(csB.Id));
                Assert.That(csB2.BeforeETags,                             Is.EqualTo(rn2.CalcChangeSetETags()));
                Assert.That(csB2.Changes,                                 Is.EqualTo(csB.Changes));
                Assert.That(csB2.Description,                             Is.EqualTo(csB.Description));
                Assert.That(csB2.CreatedAt,                               Is.EqualTo(csB.CreatedAt));
                Assert.That(csB2.Metadata["author"]!.Value<String>(),     Is.EqualTo("bob"));
                Assert.That(csB2.Metadata["rebasedFrom"]![0]!["@id"]!.Value<String>(),  Is.EqualTo(csB.Id.ToString()));
                Assert.That(csB2.Metadata["rebasedFrom"]![0]!["afterETags"]!.Select(ETag.ParseHex),  Is.EqualTo(csB.AfterETags));

                // Identified by their content, naming what they were rebased from as it was: once and twice rebased.
                Assert.That(csB2.Id,                                      Is.EqualTo(csB2.ContentId));
                Assert.That(csB2.IdentificationError(),                   Is.Null);
                Assert.That(csC3.IdentificationError(),                   Is.Null);

                Assert.That(rn3.DataSource,                               Is.EqualTo("A"));
                Assert.That(rn3.Description.Count,                        Is.Zero);
                Assert.That(rn3.ChargingStationOperators.Count(),         Is.EqualTo(2));

                // Rebased twice: both originals are named.
                Assert.That(csC3.Metadata["rebasedFrom"]!.Count(),        Is.EqualTo(2));
                Assert.That(rn4.Name[Languages.en],                       Is.EqualTo("C"));
                Assert.That(rn4.Revision,                                 Is.EqualTo(3));

            }

        }

        #endregion

        #region A_rebased_change_set_names_only_what_it_was_rebased_from()

        /// <summary>
        /// A rebased change set names those it was rebased from by their identification and ETags, so that everyone can
        /// calculate their identifications again: an entry naming another change set, with other ETags or without them, or
        /// no entry at all, is refused, also as JSON, and a change set committed rebased is committed already as it was.
        /// </summary>
        [Test]
        public void A_rebased_change_set_names_only_what_it_was_rebased_from()
        {

            var rn1        = NewNetwork();
            var history    = RoamingNetworkHistory.Start(rn1);
            var csA        = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]);
            var csB        = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "name", null, new JObject(new JProperty("en", "B"))) ]);

            Assert.That(history.Commit(csA).IsCommitted, Is.True);

            var rn2        = history.Head.RoamingNetwork;
            var csB2       = rn2.Rebase(csB).SignedByAll(rn2);

            RoamingNetworkChangeSet WithRebasedFrom(JToken RebasedFrom)
            {
                var metadata = csB2.Metadata;
                metadata[RoamingNetworkChangeSet.PropertyKeys.RebasedFrom] = RebasedFrom;
                return new RoamingNetworkChangeSet(csB2.Id, csB2.RoamingNetworkId, csB2.CreatedAt, csB2.Changes, csB2.BeforeETags, csB2.AfterETags, csB2.Description, metadata,
                                                   Preconditions: csB2.Preconditions).WithContentId();
            }

            var entry      = (JObject) csB2.Metadata[RoamingNetworkChangeSet.PropertyKeys.RebasedFrom]![0]!;

            var otherId    = (JObject) entry.DeepClone();
            otherId[RoamingNetworkChangeSet.PropertyKeys.Id] = csA.Id.ToString();

            var otherETags = (JObject) entry.DeepClone();
            otherETags[RoamingNetworkChangeSet.PropertyKeys.AfterETags] = entry[RoamingNetworkChangeSet.PropertyKeys.BeforeETags]!.DeepClone();

            var noETags    = (JObject) entry.DeepClone();
            noETags.Remove(RoamingNetworkChangeSet.PropertyKeys.AfterETags);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(WithRebasedFrom(new JArray(otherId)).   IdentificationError(),  Does.Contain("not the change set it was rebased from"));
                Assert.That(WithRebasedFrom(new JArray(otherETags)).IdentificationError(),  Does.Contain("not the change set it was rebased from"));
                Assert.That(WithRebasedFrom(new JArray(noETags)).   IdentificationError(),  Does.Contain("must name the @id, the beforeETags and the afterETags"));
                Assert.That(WithRebasedFrom(new JArray()).          IdentificationError(),  Does.Contain("must be an array"));

                Assert.That(RoamingNetworkChangeSet.TryParse(WithRebasedFrom(new JArray(otherId)).ToJSON().ToString(), out _, out var parseError),  Is.False);
                Assert.That(parseError,                                                     Does.Contain("not the change set it was rebased from"));

                // Committed rebased, the change set as it was is committed already.
                Assert.That(history.Commit(csB2).Status,                                    Is.EqualTo(RoamingNetworkCommitStatus.Committed));
                Assert.That(history.Commit(csB). Status,                                    Is.EqualTo(RoamingNetworkCommitStatus.AlreadyCommitted));

            }

        }

        #endregion

        #region Changes_of_the_same_value_conflict()

        [Test]
        public void Changes_of_the_same_value_conflict()
        {

            var rn1  = NewNetwork();
            var csA  = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]);
            var csB  = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "B") ]);

            var rn2  = rn1.ApplyChangeSet(csA);

            Assert.That(rn2.TryRebase(csB, out _, out var error),  Is.False);
            Assert.That(error,                                     Does.Contain("can not be rebased").And.Contain("no longer the expected old value"));

        }

        #endregion

        #region A_value_expected_absent_conflicts()

        [Test]
        public void A_value_expected_absent_conflicts()
        {

            var rn1  = NewNetwork(WithDescription: false);
            var csA  = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "description", null, new JObject(new JProperty("en", "A"))) ]);
            var csB  = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "description", null, new JObject(new JProperty("en", "B"))) ]);

            // The old value "absent" survives the JSON text.
            var csBviaJSON  = RoamingNetworkChangeSet.Parse(csB.ToJSON().ToString());

            var rn2  = rn1.ApplyChangeSet(csA);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(csB.       Changes[0].OldValue!.Type,          Is.EqualTo(JTokenType.Null));
                Assert.That(csBviaJSON.Changes[0].OldValue!.Type,          Is.EqualTo(JTokenType.Null));

                Assert.That(rn2.TryRebase(csBviaJSON, out _, out var error),  Is.False);
                Assert.That(error,                                            Does.Contain("'description' is no longer the expected old value"));

            }

        }

        #endregion

        #region A_change_without_old_value_is_not_rebased()

        [Test]
        public void A_change_without_old_value_is_not_rebased()
        {

            var rn1       = NewNetwork();
            var rn2       = rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]));

            // Made by hand, not by CreateChangeSet: no old value.
            var handMade  = new RoamingNetworkChangeSet(
                                ChangeSet_Id.NewRandom(),
                                rn1.Id,
                                Timestamp.Now,
                                [ RoamingNetworkChange.UpdateProperty(rn1, "name", null, new JObject(new JProperty("en", "X"))) ],
                                rn1.CalcETags(),
                                rn1.CalcETags()
                            );

            Assert.That(rn2.TryRebase(handMade, out _, out var error),  Is.False);
            Assert.That(error,                                          Does.Contain("Change 0 has no old value"));

        }

        #endregion

        #region A_rebased_change_set_without_old_value_is_refused()

        /// <summary>
        /// A change set naming the change sets it was rebased from is refused where a change of it, but an Add or AddElement,
        /// names no old value: only old values tell that a change set committed meanwhile changed what it changes, which the
        /// rebase would have overwritten. So a rebase made elsewhere is held to what TryRebase does (csharp-review 12).
        /// </summary>
        [Test]
        public void A_rebased_change_set_without_old_value_is_refused()
        {

            var rn1        = NewNetwork();
            var rn2        = rn1.ApplyChangeSet(rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]));
            var rename     = RoamingNetworkChange.UpdateProperty(rn1, "name", null, new JObject(new JProperty("en", "X")));

            // Made by hand for rn1, without an old value, and rebased by hand onto rn2.
            var handMade   = new RoamingNetworkChangeSet(ChangeSet_Id.NewRandom(), rn1.Id, Timestamp.Now, [ rename ], rn1.CalcETags(), rn1.CalcETags()).WithContentId();
            var metadata   = handMade.Metadata;

            metadata[RoamingNetworkChangeSet.PropertyKeys.RebasedFrom] = new JArray(new JObject(
                                                                             new JProperty(RoamingNetworkChangeSet.PropertyKeys.Id,           handMade.Id.ToString()),
                                                                             new JProperty(RoamingNetworkChangeSet.PropertyKeys.BeforeETags,  new JArray(handMade.BeforeETags.Select(eTag => eTag.ToJSON()))),
                                                                             new JProperty(RoamingNetworkChangeSet.PropertyKeys.AfterETags,   new JArray(handMade.AfterETags. Select(eTag => eTag.ToJSON())))));

            var rebased    = new RoamingNetworkChangeSet(handMade.Id, rn2.Id, handMade.CreatedAt, handMade.Changes, rn2.CalcETags(), rn2.CalcETags(), handMade.Description, metadata).WithContentId();

            // Prepared by CreateChangeSet, it has its old value, and TryRebase keeps it.
            Assert.That(rn2.TryRebase(rn1.CreateChangeSet([ rename ], CreatedAt: Timestamp.Now), out var byTryRebase, out var rebaseError),  Is.True, rebaseError);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rebased.IdentificationError(),       Does.Contain($"Change 0 of the change set '{rebased.Id}', which was rebased, has no old value"));
                Assert.That(byTryRebase!.IdentificationError(),  Is.Null);
                Assert.That(byTryRebase.Changes[0].OldValue,     Is.Not.Null);
            }

        }

        #endregion

        #region A_signed_change_set_is_verified_and_rebased_unsigned()

        [Test]
        public void A_signed_change_set_is_verified_and_rebased_unsigned()
        {

            var rn1        = NewNetwork();
            var csA        = rn1.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataSource", null, "A") ]);
            var csB        = rn1.CreateSignedChangeSet([ RoamingNetworkChange.RemoveProperty(rn1, "description") ]).
                                 Sign(Alice, "alice", COSEAlgorithm.Ed25519);

            var rn2        = rn1.ApplyChangeSet(csA);
            var publicKey  = Alice.GeneratePublicKey();

            using (Assert.EnterMultipleScope())
            {

                // An optional verifier checks the signatures in addition.
                Assert.That(rn2.TryRebase(csB, out _, out var error1, (changeSet, signature) => false),  Is.False);
                Assert.That(error1,                                                                     Does.Contain("is invalid"));

                Assert.That(rn2.TryRebase(csB,
                                          out var rebased,
                                          out var error2,
                                          (changeSet, signature) => signature.KeyId != "alice" || changeSet.VerifySignature(signature, publicKey, "alice", out _)),
                            Is.True,
                            error2);

                // Rebased, the signatures no longer cover it: its owners sign it again.
                Assert.That(rebased!.Signatures,                        Is.Empty);
                Assert.That(rn2.TryApplyChangeSet(rebased, out _, out var error3),  Is.False);
                Assert.That(error3,                                                 Does.Contain("is unsigned"));
                Assert.That(rn2.ApplyChangeSet(rebased.SignedByAll(rn2)).Description.Count,  Is.Zero);

            }

        }

        #endregion

        #region Empty_values_survive_the_JSON_text()

        /// <summary>
        /// JSONObject.Create leaves out empty values; changes and change sets must not. An empty array the content holds as
        /// none, as the data licenses, is refused, as is preparing it: it is a RemoveProperty (SPECIFICATION.md, section 4).
        /// </summary>
        [Test]
        public void Empty_values_survive_the_JSON_text()
        {

            var rn1    = NewNetwork();
            var empty  = rn1.CreateSignedChangeSet([]);
            var none   = new RoamingNetworkChangeSet(ChangeSet_Id.NewRandom(),
                                                     rn1.Id,
                                                     Timestamp.Now,
                                                     [ RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, new JArray()) ],
                                                     rn1.CalcChangeSetETags(),
                                                     rn1.CalcChangeSetETags()).WithContentId();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(RoamingNetworkChangeSet.Parse(empty.ToJSON().ToString()).Changes,                  Is.Empty);
                Assert.That(RoamingNetworkChangeSet.Parse(none. ToJSON().ToString()).Changes[0].NewValue,      Is.EqualTo(new JArray()).Using<JToken>(JToken.DeepEquals));

                Assert.That(rn1.TryApplyPendingChangeSet(RoamingNetworkChangeSet.Parse(none.ToJSON().ToString()), out _, out var error),  Is.False);
                Assert.That(error,  Does.Contain("the property 'dataLicenses' of the roaming network 'rn1' is given as '[]', which the content does not hold at all"));

                Assert.That(() => rn1.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(rn1, "dataLicenses", null, new JArray()) ]),
                            Throws.ArgumentException.With.Message.Contains("which the content does not hold at all"));

            }

        }

        #endregion

    }

}
