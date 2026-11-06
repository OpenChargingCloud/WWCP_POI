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

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// A signature of a commit or a checkpoint is read in one form only, as WWCP_POI.TS reads it (SPECIFICATION.md,
    /// section 5.4): its own properties and nothing else, whatever else being signed by nobody, its signer a text, and
    /// embedded in a commit or a checkpoint without its JSON-LD context.
    /// </summary>
    [TestFixture]
    public class SignatureFormTests
    {

        private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-11-06T12:00:00Z");

        #region (private static) Signed()

        /// <summary>
        /// A commit and a checkpoint of a new history, and a signature of each, written as they are embedded, and the history.
        /// </summary>
        private static (RoamingNetworkCommit Commit, JObject CommitSignature, RoamingNetworkCheckpoint Checkpoint, JObject CheckpointSignature, RoamingNetworkHistory History) Signed()
        {

            var history = RoamingNetworkHistory.Start(new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                                                         DataSource:  "v0",
                                                                         Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")).WithKeys());

            var head    = history.Head.RoamingNetwork;
            var result  = history.Commit(head.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(head, "dataSource", null, "v1") ], CreatedAt: T0));

            Assert.That(result.Status,                                                                                     Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);
            Assert.That(history.TryMakeCheckpoint(out var checkpoint, out var checkpointError, CreatedAt: T0.AddMinutes(1)),  Is.True, checkpointError);

            return (result.Commit!,
                    CheckpointTests.RNOSigner.Sign(result.Commit!, T0.AddMinutes(2)).ToJSON(Embedded: true),
                    checkpoint!,
                    CheckpointTests.RNOSigner.Sign(checkpoint!,    T0.AddMinutes(3)).ToJSON(Embedded: true),
                    history);

        }

        #endregion


        #region A_signature_is_read_in_its_one_form()

        [Test]
        public void A_signature_is_read_in_its_one_form()
        {

            var (commit, commitSignature, checkpoint, checkpointSignature, _) = Signed();

            // Why the commit, or the checkpoint, with its signature changed so is refused; null when it is read.
            String? CommitError(Action<JObject> Change)
            {
                var signature = (JObject) commitSignature.DeepClone();
                Change(signature);
                var json = commit.ToJSON();
                json["signatures"] = new JArray(signature);
                return RoamingNetworkCommit.TryParse(json, out _, out var error) ? null : error;
            }

            String? CheckpointError(Action<JObject> Change)
            {
                var signature = (JObject) checkpointSignature.DeepClone();
                Change(signature);
                var json = checkpoint.ToJSON();
                json["signatures"] = new JArray(signature);
                return RoamingNetworkCheckpoint.TryParse(json, out _, out var error) ? null : error;
            }

            String? AloneError(Action<JObject> Change)
            {
                var signature = (JObject) commitSignature.DeepClone();
                Change(signature);
                return RoamingNetworkCommitSignature.TryParse(signature, out _, out var error) ? null : error;
            }

            var context = RoamingNetworkCommitSignature.JSONLDContext;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(CommitError    (signature => { }),                                      Is.Null);
                Assert.That(CheckpointError(signature => { }),                                      Is.Null);

                // The signer a text: no number, no null.
                Assert.That(CommitError    (signature => signature["signer"]   = 42),               Is.EqualTo("The signer of a signature of a commit must be a text!"));
                Assert.That(CheckpointError(signature => signature["signer"]   = 42),               Is.EqualTo("The signer of a signature of a checkpoint must be a text!"));
                Assert.That(CommitError    (signature => signature["signer"]   = JValue.CreateNull()),  Does.Contain("must be a text"));
                Assert.That(CommitError    (signature => signature.Remove("signer")),               Is.Null, "a signature without a signer");

                // Embedded without its JSON-LD context; on its own, it may name it.
                Assert.That(CommitError    (signature => signature["@context"] = context),          Is.EqualTo("A signature of a commit must not name the property '@context'!"));
                Assert.That(CheckpointError(signature => signature["@context"] = context),          Is.EqualTo("A signature of a checkpoint must not name the property '@context'!"));
                Assert.That(AloneError     (signature => signature["@context"] = context),          Is.Null);

                // Nothing else: whatever else it names is signed by nobody.
                Assert.That(CommitError    (signature => signature["comment"]  = "unsigned"),       Is.EqualTo("A signature of a commit must not name the property 'comment'!"));
                Assert.That(CheckpointError(signature => signature["comment"]  = "unsigned"),       Is.EqualTo("A signature of a checkpoint must not name the property 'comment'!"));
                Assert.That(AloneError     (signature => signature["comment"]  = "unsigned"),       Is.EqualTo("A commit signature must not name the property 'comment'!"));

            }

        }

        #endregion


        #region A_signature_is_refused_in_the_words_of_WWCP_POI_TS()

        /// <summary>
        /// A signature of a commit or a checkpoint is refused in the words of WWCP_POI.TS (parseRoleSignature), in its order:
        /// its properties, then each of them a text, its signedAt a point in time, then none of them empty; a snapshot names
        /// why its commit is refused.
        /// </summary>
        [Test]
        public void A_signature_is_refused_in_the_words_of_WWCP_POI_TS()
        {

            var (commit, commitSignature, checkpoint, checkpointSignature, history) = Signed();

            // Why the commit, the checkpoint or the snapshot, with its signatures changed so, is refused; null when it is read.
            JArray Signatures(JObject Signature, Action<JObject> Change)
            {
                var signature = (JObject) Signature.DeepClone();
                Change(signature);
                return new JArray(signature);
            }

            String? CommitError(Action<JObject> Change, JToken? Given = null)
            {
                var json = commit.ToJSON();
                json["signatures"] = Given ?? Signatures(commitSignature, Change);
                return RoamingNetworkCommit.TryParse(json, out _, out var error) ? null : error;
            }

            String? CheckpointError(Action<JObject> Change, JToken? Given = null)
            {
                var json = checkpoint.ToJSON();
                json["signatures"] = Given ?? Signatures(checkpointSignature, Change);
                return RoamingNetworkCheckpoint.TryParse(json, out _, out var error) ? null : error;
            }

            String? AloneError(Action<JObject> Change)
            {
                var signature = (JObject) commitSignature.DeepClone();
                Change(signature);
                return RoamingNetworkCommitSignature.TryParse(signature, out _, out var error) ? null : error;
            }

            String? SnapshotError(Action<JObject> Change)
            {
                var snapshot = history.ToSnapshotJSON(IncludeRuntimeStatus: false);
                Change(snapshot);
                return RoamingNetworkHistory.TryFromSnapshot(snapshot, out _, out var error) ? null : error;
            }

            String? CBORSnapshotError(Action<JObject> Change)
            {
                var snapshot = history.ToSnapshotJSON(IncludeRuntimeStatus: false);
                Change(snapshot);
                return RoamingNetworkHistory.TryFromSnapshot(CBORJSON.ToCBOR(snapshot), out _, out var error) ? null : error;
            }

            using (Assert.EnterMultipleScope())
            {

                // Each of them a text, the signer if it is given.
                Assert.That(CommitError    (signature => signature.Remove("role")),                       Is.EqualTo("The role of a signature of a commit must be a text!"));
                Assert.That(CommitError    (signature => signature["role"]      = 7),                     Is.EqualTo("The role of a signature of a commit must be a text!"));
                Assert.That(CommitError    (signature => signature.Remove("signedAt")),                   Is.EqualTo("The signedAt of a signature of a commit must be a text!"));
                Assert.That(CommitError    (signature => signature.Remove("algorithm")),                  Is.EqualTo("The algorithm of a signature of a commit must be a text!"));
                Assert.That(CheckpointError(signature => signature["keyId"]     = true),                  Is.EqualTo("The keyId of a signature of a checkpoint must be a text!"));
                Assert.That(CommitError    (signature => signature["value"]     = 42),                    Is.EqualTo("The value of a signature of a commit must be a text!"));
                Assert.That(CommitError    (signature => signature["signer"]    = JValue.CreateNull()),   Is.EqualTo("The signer of a signature of a commit must be a text!"));

                // Its signedAt written as POI writes it, and a point in time.
                Assert.That(CommitError    (signature => signature["signedAt"]  = "2026-11-06T12:02:00Z"),      Is.EqualTo("The signedAt of a signature of a commit must be written as yyyy-MM-ddTHH:mm:ss.fffZ, not '2026-11-06T12:02:00Z'!"));
                Assert.That(CommitError    (signature => signature["signedAt"]  = "2026-02-30T12:00:00.000Z"),  Is.EqualTo("The signedAt of a signature of a commit '2026-02-30T12:00:00.000Z' is no point in time!"));

                // None of them empty, nor whitespace alone; the signer neither, when it is given.
                Assert.That(CommitError    (signature => signature["role"]      = "  "),                  Is.EqualTo("The role of a signature of a commit must not be empty!"));
                Assert.That(CommitError    (signature => signature["signer"]    = " "),                   Is.EqualTo("The signer of a signature of a commit must not be empty!"));
                Assert.That(CheckpointError(signature => signature["keyId"]     = ""),                    Is.EqualTo("The keyId of a signature of a checkpoint must not be empty!"));
                Assert.That(CommitError    (signature => signature["value"]     = "\t"),                 Is.EqualTo("The value of a signature of a commit must not be empty!"));

                // Each a text first, then none empty.
                Assert.That(CommitError    (signature => { signature["role"] = " "; signature["keyId"] = 42; }),  Is.EqualTo("The keyId of a signature of a commit must be a text!"));

                // Its properties first.
                Assert.That(CommitError    (signature => { signature["role"] = 7;   signature["extra"] = 1;  }),  Is.EqualTo("A signature of a commit must not name the property 'extra'!"));

                // The signatures a JSON array of JSON objects.
                Assert.That(CommitError    (signature => { }, new JValue("none")),                        Is.EqualTo("The signatures of a commit must be a JSON array!"));
                Assert.That(CheckpointError(signature => { }, new JArray(42)),                            Is.EqualTo("A signature of a checkpoint must be a JSON object!"));

                // On its own.
                Assert.That(AloneError     (signature => signature.Remove("role")),                       Is.EqualTo("The role of a commit signature must be a text!"));

                // A snapshot names why its commit is refused.
                Assert.That(SnapshotError    (snapshot => snapshot["commit"]!["signatures"] = Signatures(commitSignature, signature => signature["keyId"] = "")),
                                                                                                          Is.EqualTo("Invalid commit of the snapshot: The keyId of a signature of a commit must not be empty!"));
                Assert.That(CBORSnapshotError(snapshot => snapshot["commit"]!["signatures"] = Signatures(commitSignature, signature => signature["keyId"] = "")),
                                                                                                          Is.EqualTo("Invalid commit of the snapshot: The keyId of a signature of a commit must not be empty!"));
                Assert.That(SnapshotError    (snapshot => snapshot.Remove("commit")),                     Is.EqualTo("Invalid commit of the snapshot: A commit must be a JSON object!"));

            }

        }

        #endregion

    }

}
