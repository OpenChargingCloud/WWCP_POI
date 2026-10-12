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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// A follower catching up with a leader over HTTP.
    /// </summary>
    [TestFixture]
    public class HistoryAPITests
    {

        #region Data

        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private RoamingNetworkHistory?  leader;
        private HttpClient?             httpClient;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            httpServer  = new HTTPServer(
                              IPAddress:  IPv4Address.Localhost,
                              TCPPort:    IPPort.Zero,
                              AutoStart:  true
                          );

            poiAPI      = new POI_API(httpServer);

            leader      = RoamingNetworkHistory.Start(
                              new RoamingNetwork(
                                  Id:          RoamingNetwork_Id.Parse("rn1"),
                                  Name:        I18NString.Create(Languages.en, "Roaming Network 1"),
                                  DataSource:  "v0",
                                  Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")
                              )
                          );

            poiAPI.AddRoamingNetworkHistory(leader);

            httpClient  = new HttpClient {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/roamingNetworks/rn1/history/")
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


        #region Publish(Value, Minute)

        private RoamingNetworkPublishResult Publish(String Value, Int32 Minute)

            => leader!.Publish(
                   leader.Head.RoamingNetwork.CreateChangeSet(
                       [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, Value) ],
                       Description:  I18NString.Create(Languages.en, "Data source " + Value),
                       CreatedAt:    DateTimeOffset.Parse("2026-10-11T12:00:00Z").AddMinutes(Minute)
                   )
               );

        #endregion

        #region Get(Path)

        private async Task<(HttpStatusCode StatusCode, String Text)> Get(String Path)
        {

            using var response = await httpClient!.GetAsync(Path);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());

        }

        #endregion


        #region A_follower_catches_up_over_HTTP()

        [Test]
        public async Task A_follower_catches_up_over_HTTP()
        {

            Publish("v1", 1);

            // 1. Start from a snapshot of the leader's head.
            var snapshot = await Get("snapshot");
            Assert.That(snapshot.StatusCode,  Is.EqualTo(HttpStatusCode.OK), snapshot.Text);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot.Text, out var follower, out var error1),  Is.True, error1);

            Publish("v2", 2);
            Publish("v3", 3);

            // 2. Look at what is new: the metadata only.
            var head   = await Get("head");
            var infos  = await Get($"commitInfos?since={follower!.Head.Commit.Id}");

            // 3. Fetch the commits completely, one page of one commit, and one commit by its identification.
            var page   = await Get($"commits?since={follower.Head.Commit.Id}&max=1");
            var single = await Get($"commits/{leader!.Head.Commit.Id}");

            var infosJSON   = JArray.Parse(infos.Text);
            var pageJSON    = JArray.Parse(page.Text);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(head.StatusCode,                                       Is.EqualTo(HttpStatusCode.OK));
                Assert.That(JObject.Parse(head.Text)["revision"]!.Value<Int32>(),  Is.EqualTo(3));

                Assert.That(infosJSON.Count,                                       Is.EqualTo(2));
                Assert.That(infosJSON[0]["description"]!["en"]!.Value<String>(),   Is.EqualTo("Data source v2"));
                Assert.That(infosJSON[0]["changeSet"],                             Is.Null);

                Assert.That(pageJSON.Count,                                        Is.EqualTo(1));
                Assert.That(single.StatusCode,                                     Is.EqualTo(HttpStatusCode.OK));

            }

            var commits = new[] {
                              RoamingNetworkCommit.Parse(pageJSON[0].ToString()),
                              RoamingNetworkCommit.Parse(single.Text)
                          };

            Assert.That(follower.TryAppend(commits, out var appended, out var error2),  Is.True, error2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(appended,                                  Is.EqualTo(2));
                Assert.That(follower.Head.Commit.Id,                   Is.EqualTo(leader.Head.Commit.Id));
                Assert.That(follower.Head.RoamingNetwork.DataSource,   Is.EqualTo("v3"));
            }

        }

        #endregion

        #region Commit(ChangeSet, Query = "")

        private static readonly HttpMethod COMMIT = new ("COMMIT");

        private async Task<(HttpStatusCode StatusCode, JObject JSON)> Commit(RoamingNetworkChangeSet  ChangeSet,
                                                                           String                   Query   = "")
        {

            using var content   = new StringContent(ChangeSet.ToJSON().ToString(), System.Text.Encoding.UTF8, "application/json");
            using var request   = new HttpRequestMessage(COMMIT, "changeSets" + Query) { Content = content };
            using var response  = await httpClient!.SendAsync(request);

            return (response.StatusCode, JObject.Parse(await response.Content.ReadAsStringAsync()));

        }

        #endregion

        #region ChangeSet(History, Property, Value)

        private static RoamingNetworkChangeSet ChangeSet(RoamingNetworkHistory  History,
                                                         String                 Property,
                                                         JToken                 Value)

            => History.Head.RoamingNetwork.CreateChangeSet(
                   [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork, Property, null, Value) ],
                   CreatedAt: DateTimeOffset.Parse("2026-10-11T13:00:00Z")
               );

        #endregion


        #region Signatures_are_added_to_commits_over_HTTP()

        [Test]
        public async Task Signatures_are_added_to_commits_over_HTTP()
        {

            var commit     = Publish("v1", 1).Commit!;
            var signature  = commit.CreateSignature(CommitSignatureTests.RNOKey,
                                                    "rno",
                                                    COSEAlgorithm.Ed25519,
                                                    RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                    "DE*RNO");

            var forged     = commit.CreateSignature(CommitSignatureTests.LeaderKey,
                                                    "rno",
                                                    COSEAlgorithm.Ed25519,
                                                    RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator);

            async Task<(HttpStatusCode StatusCode, JObject JSON)> Post(String CommitId, String Body)
            {
                using var content   = new StringContent(Body, System.Text.Encoding.UTF8, "application/json");
                using var response  = await httpClient!.PostAsync($"commits/{CommitId}/signatures", content);
                return (response.StatusCode, JObject.Parse(await response.Content.ReadAsStringAsync()));
            }

            String Body(params RoamingNetworkCommitSignature[] Signatures)
                => new JObject(new JProperty("signatures", new JArray(Signatures.Select(signature => signature.ToJSON())))).ToString();

            var notEnabled  = await Post(commit.Id.ToString(), Body(signature));

            poiAPI!.AuthorizeCommitSignature  = (request, commit, signature) => true;
            poiAPI.VerifyCommitSignature      = CommitSignatureTests.VerifyCommit;

            var garbage     = await Post(commit.Id.ToString(), "{ \"signatures\": [] }");
            var unknown     = await Post(new String('0', 64),  Body(signature));
            var invalid     = await Post(commit.Id.ToString(), Body(forged));
            var added       = await Post(commit.Id.ToString(), Body(signature));
            var known       = await Post(commit.Id.ToString(), Body(signature));

            var (_, text)   = await Get($"commits/{commit.Id}");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(notEnabled.StatusCode,                        Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(garbage.StatusCode,                           Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(unknown.StatusCode,                           Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(invalid.StatusCode,                           Is.EqualTo((HttpStatusCode) 422));
                Assert.That(added.StatusCode,                             Is.EqualTo(HttpStatusCode.Created));
                Assert.That(added.JSON["added"]!.Value<Int32>(),          Is.EqualTo(1));
                Assert.That(known.StatusCode,                             Is.EqualTo(HttpStatusCode.OK));
                Assert.That(known.JSON["added"]!.Value<Int32>(),          Is.Zero);

                Assert.That(RoamingNetworkCommit.TryParse(text, out var fetched, out var error),  Is.True, error);
                Assert.That(fetched!.Signatures,                          Is.EqualTo(new[] { signature }));

            }

        }

        #endregion

        #region Change_sets_are_committed_not_posted()

        [Test]
        public async Task Change_sets_are_committed_not_posted()
        {

            poiAPI!.AuthorizeChangeSetSubmission = (request, changeSet) => true;

            using var content  = new StringContent(ChangeSet(leader!, "dataSource", "v1").ToJSON().ToString(), System.Text.Encoding.UTF8, "application/json");
            using var response = await httpClient!.PostAsync("changeSets", content);

            Assert.That(response.IsSuccessStatusCode,      Is.False);
            Assert.That(leader!.Head.Commit.Revision,      Is.Zero);

        }

        #endregion

        #region Submitting_is_refused_unless_enabled()

        [Test]
        public async Task Submitting_is_refused_unless_enabled()
        {

            var (statusCode, json) = await Commit(ChangeSet(leader!, "dataSource", "v1"));

            Assert.That(statusCode,                               Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(json["description"]!.Value<String>(),     Does.Contain("not enabled"));
            Assert.That(leader!.Head.Commit.Revision,             Is.Zero);

        }

        #endregion

        #region A_follower_submits_a_change_set_and_catches_up()

        [Test]
        public async Task A_follower_submits_a_change_set_and_catches_up()
        {

            poiAPI!.AuthorizeChangeSetSubmission = (request, changeSet) => true;

            RoamingNetworkHistory.TryFromSnapshot((await Get("snapshot")).Text, out var follower, out _);

            // The follower prepares a change set against its head and submits it to the leader...
            var changeSet  = ChangeSet(follower!, "dataSource", "from the follower");
            var published  = await Commit(changeSet, $"?expectedHead={follower!.Head.Commit.Id}");
            var again      = await Commit(changeSet);

            // ...and gets it back as a commit, as every other follower does.
            leader!.TryGetCommitsSince(follower.Head.Commit.Id, out var commits, out _);
            follower.TryAppend(commits!, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(published.StatusCode,                                  Is.EqualTo(HttpStatusCode.Created));
                Assert.That(published.JSON["status"]!.Value<String>(),             Is.EqualTo("Published"));
                Assert.That(published.JSON["rebased"]!.Value<Boolean>(),           Is.False);
                Assert.That(published.JSON["@id"]!.Value<String>(),                Is.EqualTo(leader.Head.Commit.Id.ToString()));

                Assert.That(again.StatusCode,                                      Is.EqualTo(HttpStatusCode.OK));
                Assert.That(again.JSON["status"]!.Value<String>(),                 Is.EqualTo("AlreadyPublished"));

                Assert.That(follower.Head.Commit.Id,                               Is.EqualTo(leader.Head.Commit.Id));
                Assert.That(follower.Head.RoamingNetwork.DataSource,               Is.EqualTo("from the follower"));

            }

        }

        #endregion

        #region Concurrent_submissions()

        [Test]
        public async Task Concurrent_submissions()
        {

            poiAPI!.AuthorizeChangeSetSubmission = (request, changeSet) => true;

            var head0      = leader!.Head.Commit.Id;

            // Three change sets against the same head.
            var a          = ChangeSet(leader, "dataSource",  "A");
            var b          = ChangeSet(leader, "name",        new JObject(new JProperty("en", "B")));
            var c          = ChangeSet(leader, "dataSource",  "C");

            var postA      = await Commit(a);
            var staleB     = await Commit(b, $"?expectedHead={head0}");
            var notFitB    = await Commit(b);
            var rebasedB   = await Commit(b, "?rebase=true");
            var conflictC  = await Commit(c, "?rebase=true");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(postA.StatusCode,                                  Is.EqualTo(HttpStatusCode.Created));

                Assert.That(staleB.StatusCode,                                 Is.EqualTo(HttpStatusCode.Conflict));
                Assert.That(staleB.JSON["head"]!["revision"]!.Value<Int32>(),  Is.EqualTo(1));
                Assert.That(notFitB.StatusCode,                                Is.EqualTo(HttpStatusCode.Conflict));

                Assert.That(rebasedB.StatusCode,                               Is.EqualTo(HttpStatusCode.Created));
                Assert.That(rebasedB.JSON["rebased"]!.Value<Boolean>(),        Is.True);

                Assert.That(conflictC.StatusCode,                              Is.EqualTo((HttpStatusCode) 422));
                Assert.That(conflictC.JSON["description"]!.Value<String>(),    Does.Contain("no longer the expected old value"));

                Assert.That(leader.Head.Commit.Revision,                       Is.EqualTo(2));

            }

        }

        #endregion

        #region Invalid_submissions_are_refused()

        [Test]
        public async Task Invalid_submissions_are_refused()
        {

            var allowed = true;

            poiAPI!.AuthorizeChangeSetSubmission = (request, changeSet) => allowed;

            var otherNetwork  = new RoamingNetwork(Id: RoamingNetwork_Id.Parse("rn2"), Created: DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
            var otherHistory  = RoamingNetworkHistory.Start(otherNetwork);

            using var garbage = new StringContent("{ \"no\": \"change set\" }", System.Text.Encoding.UTF8, "application/json");
            using var invalidRequest = new HttpRequestMessage(COMMIT, "changeSets") { Content = garbage };
            using var invalid        = await httpClient!.SendAsync(invalidRequest);

            var wrongNetwork  = await Commit(ChangeSet(otherHistory, "dataSource", "x"));
            var invalidRebase = await Commit(ChangeSet(leader!, "dataSource", "x"), "?rebase=yes");

            var signed        = await Commit(ChangeSet(leader!, "dataSource", "x").Sign(new Org.BouncyCastle.Crypto.Parameters.Ed25519PrivateKeyParameters(new Byte[32], 0),
                                                                                    "alice",
                                                                                    COSEAlgorithm.Ed25519));

            allowed = false;
            var forbidden     = await Commit(ChangeSet(leader!, "dataSource", "x"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(invalid.StatusCode,        Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(wrongNetwork.StatusCode,   Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(invalidRebase.StatusCode,  Is.EqualTo(HttpStatusCode.BadRequest));

                // Signed, but the leader has no verifier for signatures.
                Assert.That(signed.StatusCode,                                Is.EqualTo((HttpStatusCode) 422));
                Assert.That(signed.JSON["description"]!.Value<String>(),      Does.Contain("requires a signature verifier"));

                Assert.That(forbidden.StatusCode,                             Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(leader!.Head.Commit.Revision,                     Is.Zero);

            }

        }

        #endregion

        #region GetCBOR(Path)

        private async Task<(HttpStatusCode StatusCode, String? ContentType, CBORValue CBOR)> GetCBOR(String Path)
        {

            using var request  = new HttpRequestMessage(HttpMethod.Get, Path);
            request.Headers.Accept.ParseAdd("application/cbor");

            using var response = await httpClient!.SendAsync(request);

            return (response.StatusCode,
                    response.Content.Headers.ContentType?.MediaType,
                    CBORValue.Parse(await response.Content.ReadAsByteArrayAsync()));

        }

        #endregion

        #region A_follower_syncs_in_CBOR()

        [Test]
        public async Task A_follower_syncs_in_CBOR()
        {

            poiAPI!.AuthorizeChangeSetSubmission = (request, changeSet) => true;

            Publish("v1", 1);

            // The snapshot in CBOR.
            var snapshot = await GetCBOR("snapshot");

            Assert.That(snapshot.ContentType,  Is.EqualTo("application/cbor"));
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot.CBOR, out var follower, out var error1),  Is.True, error1);

            // A change set submitted in CBOR, answered in CBOR.
            using var post = new HttpRequestMessage(COMMIT, "changeSets") {
                                 Content = new ByteArrayContent(follower!.Head.RoamingNetwork.CreateChangeSet(
                                                                    [ RoamingNetworkChange.UpdateProperty(follower.Head.RoamingNetwork, "dataSource", null, "from CBOR") ],
                                                                    CreatedAt: DateTimeOffset.Parse("2026-10-11T13:00:00Z")
                                                                ).ToCBOR().ToByteArray(CBORWriterOptions.Canonical))
                             };

            post.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/cbor");
            post.Headers.Accept.ParseAdd("application/cbor");

            using var posted       = await httpClient!.SendAsync(post);
            var postedCBOR         = CBORValue.Parse(await posted.Content.ReadAsByteArrayAsync());

            // The commits in CBOR.
            var commits = await GetCBOR($"commits?since={follower.Head.Commit.Id}");

            var received = commits.CBOR.AsArray().Select(commit => RoamingNetworkCommit.TryParse(commit, out var parsed, out _) ? parsed : null).ToArray();

            Assert.That(follower.TryAppend(received!, out _, out var error2),  Is.True, error2);

            var notFound = await GetCBOR($"commits/{new String('0', 64)}");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(posted.StatusCode,                                    Is.EqualTo(HttpStatusCode.Created));
                Assert.That(posted.Content.Headers.ContentType?.MediaType,        Is.EqualTo("application/cbor"));
                Assert.That(postedCBOR.TryGetValue(CBORValue.FromText("status"), out var status) && status.AsText() == "Published",  Is.True);

                Assert.That(follower.Head.Commit.Id,                              Is.EqualTo(leader!.Head.Commit.Id));
                Assert.That(follower.Head.RoamingNetwork.DataSource,              Is.EqualTo("from CBOR"));

                // Errors in CBOR as well.
                Assert.That(notFound.StatusCode,                                  Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(notFound.ContentType,                                 Is.EqualTo("application/cbor"));
                Assert.That(notFound.CBOR.TryGetValue(CBORValue.FromText("description"), out _),  Is.True);

                // Without an Accept header field: JSON.
                Assert.That((await Get("head")).Text,                             Does.StartWith("{"));

            }

        }

        #endregion

        #region The_snapshot_brings_the_runtime_status()

        [Test]
        public async Task The_snapshot_brings_the_runtime_status()
        {

            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, nameof(RoamingNetwork), "rn1", "offline", DateTimeOffset.Parse("2026-10-11T12:00:00Z"), out _, out _);

            var withStatus     = await Get("snapshot");
            var withoutStatus  = await Get("snapshot?runtimeStatus=false");
            var invalid        = await Get("snapshot?runtimeStatus=maybe");
            var inCBOR         = await GetCBOR("snapshot");

            Assert.That(RoamingNetworkHistory.TryFromSnapshot(withStatus.Text, out var follower, out var error),  Is.True, error);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(inCBOR.CBOR,     out var followerCBOR, out var errorCBOR),  Is.True, errorCBOR);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(JObject.Parse(withStatus.Text)["runtimeStatus"]!.Count(),  Is.EqualTo(2));
                Assert.That(JObject.Parse(withoutStatus.Text)["runtimeStatus"],        Is.Null);
                Assert.That(invalid.StatusCode,                                        Is.EqualTo(HttpStatusCode.BadRequest));

                Assert.That(follower!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(RoamingNetwork), "rn1", out var status),  Is.True);
                Assert.That(status!.Value,                                             Is.EqualTo("offline"));
                Assert.That(followerCBOR!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, nameof(RoamingNetwork), "rn1", out var statusCBOR),  Is.True);
                Assert.That(statusCBOR!.Value,                                         Is.EqualTo("offline"));

            }

        }

        #endregion

        #region Errors_are_answered_in_JSON()

        [Test]
        public async Task Errors_are_answered_in_JSON()
        {

            var unknownCommit   = await Get($"commits?since={new String('0', 64)}");
            var invalidCommit   = await Get("commits?since=nope");
            var invalidMax      = await Get("commits?max=0");
            var missingCommit   = await Get($"commits/{new String('0', 64)}");

            using var unknownNetworkResponse = await httpClient!.GetAsync("/roamingNetworks/rn2/history/head");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(unknownCommit.StatusCode,                   Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(JObject.Parse(unknownCommit.Text)["description"]!.Value<String>(),  Does.Contain("start from a new snapshot"));

                Assert.That(invalidCommit.StatusCode,                   Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(invalidMax.StatusCode,                      Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(missingCommit.StatusCode,                   Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(unknownNetworkResponse.StatusCode,          Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

    }

}
