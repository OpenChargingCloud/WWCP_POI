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
using System.Text.Json.Serialization;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An immutable, serializable batch of changes to one roaming network. The base revision
    /// allows the copy-on-write applier to reject changes built against stale data.
    /// </summary>
    public sealed record RoamingNetworkChangeSet
    {
        /// <summary>
        /// Creates a change set.
        /// </summary>
        [JsonConstructor]
        public RoamingNetworkChangeSet(String                                id,
                                       String                                roamingNetworkId,
                                       Int64                                 baseRevision,
                                       DateTimeOffset                        createdAt,
                                       ImmutableArray<RoamingNetworkChange>  changes,
                                       RoamingNetworkChangeSetSignature?     signature        = null)
        {

            Id = Required(id, nameof(id));
            RoamingNetworkId = Required(roamingNetworkId, nameof(roamingNetworkId));

            if (baseRevision < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseRevision));
            }

            if (changes.IsDefault || changes.Any(change => change is null))
            {
                throw new ArgumentException("Changes must be an initialized collection without null entries.", nameof(changes));
            }

            BaseRevision = baseRevision;
            CreatedAt = createdAt;
            Changes = changes;
            Signature = signature;

        }

        /// <summary>
        /// The unique change set identifier.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String Id { get; private init; }

        /// <summary>
        /// The roaming network targeted by this batch.
        /// </summary>
        [JsonInclude, JsonRequired]
        public String RoamingNetworkId { get; private init; }

        /// <summary>
        /// The revision against which these changes were prepared.
        /// </summary>
        [JsonInclude, JsonRequired]
        public Int64 BaseRevision { get; private init; }

        /// <summary>
        /// The commit timestamp applied to modified entities and their ancestors.
        /// </summary>
        [JsonInclude, JsonRequired]
        public DateTimeOffset CreatedAt { get; private init; }

        /// <summary>
        /// The immutable operations applied in their declared order.
        /// </summary>
        [JsonInclude, JsonRequired]
        public ImmutableArray<RoamingNetworkChange> Changes { get; private init; }

        /// <summary>
        /// An optional signature envelope for caller-provided verification.
        /// </summary>
        public RoamingNetworkChangeSetSignature? Signature { get; }

        private static String Required(String  value,
                                       String  parameterName)
            => !String.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);
    }
}
