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

using System.Globalization;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Complex
{

    [TestFixture]
    public class CommitTests
    {

        #region RoamingNetwork_JSON_ApplyChangeSetTest()

        [Test]
        public void RoamingNetwork_JSON_ApplyChangeSetTest()
        {

            var rn1 = new RoamingNetwork(
                          Id:           RoamingNetwork_Id.Parse("rn1"),
                          Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                          Description:  I18NString.Create(Languages.en, "My first roaming network!").
                                                      Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                          DataSource:   "test"
                      ).WithKeys();

            var e1  = rn1.CalcChangeSetETags();

            System.Collections.Immutable.ImmutableArray<RoamingNetworkChange> c1 = [
                RoamingNetworkChange.UpdateProperty(
                    RoamingNetwork:  rn1,
                    PropertyName:    "dataSource",
                    OldValue:        "test",
                    NewValue:        "test2"
                )
            ];

            // The change set the network makes of the changes knows the ETags after them.
            var cs1 = rn1.CreateSignedChangeSet(
                          Changes:      c1,
                          Description:  new ImmutableI18NString(I18NString.Create("Hello world!")),
                          Metadata:     new JObject(new JProperty("hello", "world!")),
                          CreatedAt:    DateTimeOffset.Parse("2026-10-12T12:00:00Z")
                      );

            var rn2 = rn1.ApplyChangeSet(cs1);
            var e2  = rn2.CalcChangeSetETags();

            // Written by hand, it has to know them too: with the ETags before as the ETags after, it is refused.
            var wrongAfterETags = new RoamingNetworkChangeSet(
                                      Id:                 ChangeSet_Id.NewRandom(),
                                      RoamingNetworkId:   rn1.Id,
                                      CreatedAt:          DateTimeOffset.Parse("2026-10-12T12:00:00Z"),
                                      Changes:            c1,
                                      BeforeETags:        e1,
                                      AfterETags:         e1,
                                      Description:        I18NString.Create("Hello world!"),
                                      Signatures:         null
                                  );

            var refused = rn1.TryApplyChangeSet(wrongAfterETags, out _, out var refusedError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(rn2.Id,                          Is.EqualTo(rn1.Id));
                Assert.That(rn2.Name,                        Is.EqualTo(rn1.Name));
                Assert.That(rn2.Description,                 Is.EqualTo(rn1.Description));
                Assert.That(rn2.DataSource,                  Is.EqualTo("test2"));
                Assert.That(rn2.Revision,                    Is.EqualTo(rn1.Revision + 1));

                // The given version stays as it is.
                Assert.That(rn1.DataSource,                  Is.EqualTo("test"));
                Assert.That(rn1.CalcChangeSetETags(),                 Is.EqualTo(e1));

                Assert.That(cs1.BeforeETags,                 Is.EqualTo(e1));
                Assert.That(cs1.AfterETags,                  Is.EqualTo(e2));
                Assert.That(e2,                              Is.Not.EqualTo(e1));
                Assert.That(cs1.Metadata["hello"]!.ToString(), Is.EqualTo("world!"));

                Assert.That(refused,                         Is.False);
                Assert.That(refusedError,                    Is.Not.Null);

            }

        }

        #endregion

        #region A_snapshot_leaves_the_given_version_unchanged()

        [Test]
        public void A_snapshot_leaves_the_given_version_unchanged()
        {

            var leader  = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                                         DataSource:  "test",
                                                                         Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")).WithKeys());

            leader.Commit(leader.Head.RoamingNetwork.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, "test2") ],
                                                                      CreatedAt: DateTimeOffset.Parse("2026-10-12T12:00:00Z")));

            // The same content, but read anew: revision 0.
            var given   = RoamingNetwork.Parse(leader.Head.RoamingNetwork.ToJSON());

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(given, leader.Head.Commit, out var follower, out var error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(given.Revision,                                    Is.Zero);
                Assert.That(follower!.Head.RoamingNetwork.Revision,            Is.EqualTo(1));
                Assert.That(follower.Head.RoamingNetwork.DataSource,           Is.EqualTo("test2"));
                Assert.That(follower.Head.RoamingNetwork.MatchesETags(leader.Head.Commit.ETags),  Is.True);
            }

        }

        #endregion

        #region RoamingNetwork_CBOR_RoundtripTest()

        [Test]
        public void RoamingNetwork_CBOR_RoundtripTest()
        {

            var rn1 = new RoamingNetwork(
                          Id:           RoamingNetwork_Id.Parse("rn1"),
                          Name:         I18NString.Create(Languages.en, "Roaming Network 1"),
                          Description:  I18NString.Create(Languages.en, "My first roaming network!").
                                                      Set(Languages.de, "Mein erstes Roaming-Netzwerk!"),
                          DataSource:   "test"
                      ).WithKeys();

            var c1  = rn1.ToCBOR();
            var e1  = rn1.CalcETags(CBOR: c1);
            var b1  = c1.ToByteArray(CBORWriterOptions.Canonical);

            var rn2 = RoamingNetwork.Parse(CBORValue.Parse(b1));
            var c2  = rn2.ToCBOR();
            var e2  = rn2.CalcETags(CBOR: c2);
            var b2  = c2.ToByteArray(CBORWriterOptions.Canonical);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(rn2.Id,              Is.EqualTo(rn1.Id));
                Assert.That(rn2.Name,            Is.EqualTo(rn1.Name));
                Assert.That(rn2.Description,     Is.EqualTo(rn1.Description));
                Assert.That(rn2.DataSource,      Is.EqualTo(rn1.DataSource));
                Assert.That(e1.First().Digest,   Is.EqualTo(e2.First().Digest));
                Assert.That(b2,                  Is.EqualTo(b1));
            }

        }

        #endregion

    }

}
