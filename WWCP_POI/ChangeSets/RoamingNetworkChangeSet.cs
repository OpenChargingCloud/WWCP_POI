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
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    public sealed partial record RoamingNetworkChangeSet
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
                                       ImmutableArray<ETag>                  beforeETags,
                                       ImmutableArray<ETag>                  afterETags,
                                       ImmutableArray<RoamingNetworkChangeSetSignature> signatures = default,
                                       ImmutableDictionary<String, String>? description      = null,
                                       ImmutableDictionary<String, JsonElement>? metadata    = null)
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
            CreatedAt = createdAt.ToUniversalTime();
            Changes = changes;
            BeforeETags = ETag.ValidatePair(beforeETags, nameof(beforeETags));
            AfterETags = ETag.ValidatePair(afterETags, nameof(afterETags));
            Signatures = signatures.IsDefault ? ImmutableArray<RoamingNetworkChangeSetSignature>.Empty : signatures;
            if (Signatures.Any(signature => signature is null))
                throw new ArgumentException("Signatures must not contain null entries.", nameof(signatures));
            Description = CopyDescription(description);
            Metadata = CopyMetadata(metadata);

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
        /// The canonical JSON and CBOR identifiers of the expected source POI content.
        /// </summary>
        [JsonInclude, JsonRequired]
        public ImmutableArray<ETag> BeforeETags { get; private init; }

        /// <summary>
        /// The canonical JSON and CBOR identifiers required after all operations and timestamp updates.
        /// </summary>
        [JsonInclude, JsonRequired]
        public ImmutableArray<ETag> AfterETags { get; private init; }

        /// <summary>
        /// Equal peer signatures over the complete batch; an empty array means unsigned.
        /// </summary>
        [JsonInclude, JsonRequired]
        public ImmutableArray<RoamingNetworkChangeSetSignature> Signatures { get; private init; }

        /// <summary>
        /// Immutable commit descriptions by language label, included in the signature.
        /// </summary>
        [JsonInclude, JsonRequired]
        public ImmutableDictionary<String, String> Description { get; private init; }

        /// <summary>
        /// Immutable application metadata with detached JSON values, included in the signature.
        /// </summary>
        [JsonInclude, JsonRequired]
        public ImmutableDictionary<String, JsonElement> Metadata { get; private init; }

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
            => new(Id, RoamingNetworkId, BaseRevision, CreatedAt, Changes, BeforeETags, AfterETags,
                   signatures, Description, Metadata);

        /// <summary>
        /// Return an unsigned copy, preserving the complete batch and its commit metadata.
        /// </summary>
        public RoamingNetworkChangeSet WithoutSignatures()
            => WithSignatures([]);

        /// <summary>
        /// Replace all commit descriptions, returning an unsigned batch with unchanged operations/ETags.
        /// </summary>
        public RoamingNetworkChangeSet WithDescription(ImmutableDictionary<String, String> description)
        {
            ArgumentNullException.ThrowIfNull(description);
            return new(Id, RoamingNetworkId, BaseRevision, CreatedAt, Changes, BeforeETags, AfterETags,
                       description: description, metadata: Metadata);
        }

        /// <summary>
        /// Copy multilingual Illias text into immutable descriptions and clear the old signature.
        /// </summary>
        public RoamingNetworkChangeSet WithDescription(I18NString description)
        {
            ArgumentNullException.ThrowIfNull(description);
            return WithDescription(description.ToJSON().Properties().ToImmutableDictionary(
                property => property.Name, property => property.Value.Value<String>()!, StringComparer.Ordinal));
        }

        /// <summary>
        /// Add or replace one language's description, returning an unsigned batch.
        /// </summary>
        public RoamingNetworkChangeSet WithDescription(String language, String text)
            => WithDescription(Description.SetItem(Required(language, nameof(language)),
                                                   text ?? throw new ArgumentNullException(nameof(text))));

        /// <summary>
        /// Replace all application metadata, copying its values and clearing the old signature.
        /// </summary>
        public RoamingNetworkChangeSet WithMetadata(ImmutableDictionary<String, JsonElement> metadata)
        {
            ArgumentNullException.ThrowIfNull(metadata);
            return new(Id, RoamingNetworkId, BaseRevision, CreatedAt, Changes, BeforeETags, AfterETags,
                       description: Description, metadata: metadata);
        }

        /// <summary>
        /// Add or replace one metadata value, returning an unsigned batch.
        /// </summary>
        public RoamingNetworkChangeSet WithMetadata(String key, JsonElement value)
            => WithMetadata(Metadata.SetItem(Required(key, nameof(key)), value));

        /// <summary>
        /// Serialize an application value as JSON metadata and clear the old signature.
        /// </summary>
        public RoamingNetworkChangeSet WithMetadata<T>(String key, T value)
            => WithMetadata(key, JsonSerializer.SerializeToElement(value));

        private static ImmutableDictionary<String, String> CopyDescription(ImmutableDictionary<String, String>? description)
        {
            var result = ImmutableDictionary.CreateBuilder<String, String>(StringComparer.Ordinal);
            if (description is not null)
                foreach (var entry in description)
                    result.Add(Required(entry.Key, nameof(description)),
                               entry.Value ?? throw new ArgumentException("Description text must not be null.", nameof(description)));
            return result.ToImmutable();
        }

        private static ImmutableDictionary<String, JsonElement> CopyMetadata(ImmutableDictionary<String, JsonElement>? metadata)
        {
            var result = ImmutableDictionary.CreateBuilder<String, JsonElement>(StringComparer.Ordinal);
            if (metadata is not null)
                foreach (var entry in metadata)
                {
                    var key = Required(entry.Key, nameof(metadata));
                    ValidateSigningJSON(entry.Value);
                    result.Add(key, entry.Value.Clone());
                }
            return result.ToImmutable();
        }

        private static String Required(String  value,
                                       String  parameterName)
            => !String.IsNullOrWhiteSpace(value) ? value : throw new ArgumentException("Value must not be empty.", parameterName);
    }
}
