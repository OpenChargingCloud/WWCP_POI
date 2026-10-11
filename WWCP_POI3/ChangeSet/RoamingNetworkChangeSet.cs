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
using System.Text.Json.Serialization;

using org.GraphDefined.Vanaheimr.Illias;
using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An immutable, serializable batch of changes to one roaming network. The base revision
    /// and before/after content identifiers bind the batch to its source and resulting POI data.
    /// </summary>
    public partial record RoamingNetworkChangeSet
    {

        /// <summary>
        /// The unique change set identifier.
        /// </summary>
        public ChangeSet_Id Id { get; private init; }

        /// <summary>
        /// The roaming network targeted by this batch.
        /// </summary>
        public RoamingNetwork_Id RoamingNetworkId { get; private init; }

        /// <summary>
        /// The revision against which these changes were prepared.
        /// </summary>
        public Int64 BaseRevision { get; private init; }

        /// <summary>
        /// The commit timestamp applied to modified entities and their ancestors.
        /// </summary>
        public DateTimeOffset CreatedAt { get; private init; }

        /// <summary>
        /// The immutable operations applied in their declared order.
        /// </summary>
        public ImmutableArray<RoamingNetworkChange> Changes { get; private init; }

        /// <summary>
        /// The canonical JSON and CBOR identifiers of the expected source POI content.
        /// </summary>
        public ImmutableArray<ETag> BeforeETags { get; private init; }

        /// <summary>
        /// The canonical JSON and CBOR identifiers required after all operations and timestamp updates.
        /// </summary>
        public ImmutableArray<ETag> AfterETags { get; private init; }

        /// <summary>
        /// Immutable commit descriptions by language label, included in the signature.
        /// </summary>
        public ImmutableI18NString Description { get; private init; }

        /// <summary>
        /// Immutable application metadata with detached JSON values, included in the signature.
        /// </summary>
        public ImmutableDictionary<String, JsonElement> Metadata { get; private init; }

        /// <summary>
        /// Equal peer signatures over the complete batch; an empty array means unsigned.
        /// </summary>
        public ImmutableArray<RoamingNetworkChangeSetSignature> Signatures { get; private init; }



        /// <summary>
        /// Creates a change set.
        /// </summary>
        public RoamingNetworkChangeSet(ChangeSet_Id                                      id,
                                       RoamingNetwork_Id                                 roamingNetworkId,
                                       Int64                                             baseRevision,
                                       DateTimeOffset                                    createdAt,
                                       ImmutableArray<RoamingNetworkChange>              changes,
                                       ImmutableArray<ETag>                              beforeETags,
                                       ImmutableArray<ETag>                              afterETags,
                                       ImmutableI18NString?                              description   = null,
                                       ImmutableDictionary<String, JsonElement>?         metadata      = null,
                                       ImmutableArray<RoamingNetworkChangeSetSignature>  signatures    = default)
        {

            Id                  = id;
            RoamingNetworkId    = roamingNetworkId;

            if (baseRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(baseRevision));

            if (changes.IsDefault || changes.Any(change => change is null))
                throw new ArgumentException("Changes must be an initialized collection without null entries.", nameof(changes));

            BaseRevision        = baseRevision;
            CreatedAt           = createdAt;
            Changes             = changes;
            BeforeETags         = beforeETags;
            AfterETags          = afterETags;
            Signatures          = signatures.IsDefault ? ImmutableArray<RoamingNetworkChangeSetSignature>.Empty : signatures;
            if (Signatures.Any(signature => signature is null))
                throw new ArgumentException("Signatures must not contain null entries.", nameof(signatures));
            Description         = description ?? ImmutableI18NString.Empty;
            Metadata            = metadata    ?? ImmutableDictionary<String, JsonElement>.Empty;

        }



        /// <summary>
        /// Append an equal peer signature envelope without changing the batch's signed content.
        /// </summary>
        public RoamingNetworkChangeSet WithSignature(RoamingNetworkChangeSetSignature signature)
        {
            ArgumentNullException.ThrowIfNull(signature);
            return WithSignatures(Signatures.Add(signature));
        }

        /// <summary>
        /// Return an immutable copy with the supplied peer signature array.
        /// </summary>
        public RoamingNetworkChangeSet WithSignatures(ImmutableArray<RoamingNetworkChangeSetSignature> signatures)
            => new (Id, RoamingNetworkId, BaseRevision, CreatedAt, Changes, BeforeETags, AfterETags,
                    Description, Metadata, signatures);

    }

}
