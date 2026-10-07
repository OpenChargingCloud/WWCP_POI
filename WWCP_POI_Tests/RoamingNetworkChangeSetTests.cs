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

using System.Collections.Immutable;
using System.Text.Json;


using cloud.charging.open.protocols.WWCP.POI;


using NUnit.Framework;

#endregion

namespace WWCP_POI_Tests
{
    [TestFixture]
    public sealed class RoamingNetworkChangeSetTests
    {
        private static JsonElement Json(string value)
        {

            using var document = JsonDocument.Parse(value);

            return document.RootElement.Clone();

        }

        [Test]
        public void RoamingNetworks_json_serialization_enumerates_the_source_once()
        {

            var network = new RoamingNetwork(RoamingNetwork_Id.Parse("test-network"));
            var enumerationCount = 0;

            IEnumerable<RoamingNetwork> SingleUseSource()
            {

                if (++enumerationCount > 1)
                {
                    throw new InvalidOperationException("The source can only be enumerated once.");
                }

                yield return network;

            }

            var json = RoamingNetworkExtensions2.ToJSON(SingleUseSource());

            Assert.That(json, Has.Count.EqualTo(1));
            Assert.That(enumerationCount, Is.EqualTo(1));

        }

        [Test]
        public void ChangeSet_round_trips_operations_payloads_and_signature()
        {

            var set = new RoamingNetworkChangeSet(
                                                "change-42", "DE*ABC", 7, DateTimeOffset.Parse("2026-10-06T12:30:00Z"),
                                                [
                                                    RoamingNetworkChange.Add("ChargingStation", "DE*ABC*S1", Json("{\"name\":\"North\"}")),
                RoamingNetworkChange.Remove("ChargingStation", "DE*ABC*S2"),
                RoamingNetworkChange.UpdateProperty("ChargingStation", "DE*ABC*S3", "status", Json("\"Available\""), Json("\"Unavailable\""))
                                                ],
                                                [ETag.Parse("json:sha256:hex:" + new String('0', 64)), ETag.Parse("cbor:sha256:hex:" + new String('0', 64))],
                                                [ETag.Parse("json:sha256:hex:" + new String('1', 64)), ETag.Parse("cbor:sha256:hex:" + new String('1', 64))],
                                                [new RoamingNetworkChangeSetSignature("Ed25519", "key-1", "signature-bytes")]);
            var json = JsonSerializer.Serialize(set);
            var restored = JsonSerializer.Deserialize<RoamingNetworkChangeSet>(json);

            Assert.That(restored, Is.Not.Null);
            Assert.That(restored!.Id, Is.EqualTo(set.Id));
            Assert.That(restored.RoamingNetworkId, Is.EqualTo("DE*ABC"));
            Assert.That(restored.BaseRevision, Is.EqualTo(7));
            Assert.That(restored.Changes, Has.Length.EqualTo(3));
            Assert.That(restored.Changes[0].Kind, Is.EqualTo(RoamingNetworkChangeKind.Add));
            Assert.That(restored.Changes[0].NewValue!.Value.GetProperty("name").GetString(), Is.EqualTo("North"));
            Assert.That(restored.Changes[1].Kind, Is.EqualTo(RoamingNetworkChangeKind.Remove));
            Assert.That(restored.Changes[2].PropertyName, Is.EqualTo("status"));
            Assert.That(restored.Changes[2].OldValue!.Value.GetString(), Is.EqualTo("Available"));
            Assert.That(restored.Changes[2].NewValue!.Value.GetString(), Is.EqualTo("Unavailable"));
            Assert.That(restored.Signatures, Is.EqualTo(set.Signatures));

        }

        [Test]
        public void ChangeSet_rejects_invalid_revision_and_default_operations()
        {

            Assert.That(() => new RoamingNetworkChangeSet("c", "rn", -1, DateTimeOffset.UtcNow,
                                                                                          ImmutableArray<RoamingNetworkChange>.Empty, default, default),
                                                        Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => new RoamingNetworkChangeSet("c", "rn", 0, DateTimeOffset.UtcNow,
                                                                                          default, default, default),
                                                        Throws.ArgumentException);

        }
    }
}
