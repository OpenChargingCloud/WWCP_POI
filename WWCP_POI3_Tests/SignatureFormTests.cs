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
        /// A commit and a checkpoint of a new history, and a signature of each, written as they are embedded.
        /// </summary>
        private static (RoamingNetworkCommit Commit, JObject CommitSignature, RoamingNetworkCheckpoint Checkpoint, JObject CheckpointSignature) Signed()
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
                    CheckpointTests.RNOSigner.Sign(checkpoint!,    T0.AddMinutes(3)).ToJSON(Embedded: true));

        }

        #endregion


        #region A_signature_is_read_in_its_one_form()

        [Test]
        public void A_signature_is_read_in_its_one_form()
        {

            var (commit, commitSignature, checkpoint, checkpointSignature) = Signed();

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
                Assert.That(CommitError    (signature => signature["signer"]   = 42),               Does.Contain("The signer of a commit signature must be a text!"));
                Assert.That(CheckpointError(signature => signature["signer"]   = 42),               Does.Contain("The signer of a commit signature must be a text!"));
                Assert.That(CommitError    (signature => signature["signer"]   = JValue.CreateNull()),  Does.Contain("must be a text"));
                Assert.That(CommitError    (signature => signature.Remove("signer")),               Is.Null, "a signature without a signer");

                // Embedded without its JSON-LD context; on its own, it may name it.
                Assert.That(CommitError    (signature => signature["@context"] = context),          Does.Contain("must not name its JSON-LD context"));
                Assert.That(CheckpointError(signature => signature["@context"] = context),          Does.Contain("must not name its JSON-LD context"));
                Assert.That(AloneError     (signature => signature["@context"] = context),          Is.Null);

                // Nothing else: whatever else it names is signed by nobody.
                Assert.That(CommitError    (signature => signature["comment"]  = "unsigned"),       Does.Contain("must not name the property 'comment'"));
                Assert.That(CheckpointError(signature => signature["comment"]  = "unsigned"),       Does.Contain("must not name the property 'comment'"));
                Assert.That(AloneError     (signature => signature["comment"]  = "unsigned"),       Does.Contain("must not name the property 'comment'"));

            }

        }

        #endregion

    }

}
