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

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// One representation of a version: its canonical content and its ETag.
    /// </summary>
    /// <param name="Content">Canonical JSON (UTF-8) or canonical CBOR.</param>
    /// <param name="ETag">The SHA-256 ETag of the content.</param>
    public sealed record POIRepresentation(Byte[]  Content,
                                           ETag    ETag)
    {

        /// <summary>
        /// The given JSON as canonical JSON, or as canonical CBOR with its keys and values, with the ETag of the content.
        /// </summary>
        /// <param name="JSON">The JSON.</param>
        /// <param name="Format">The format of the representation.</param>
        public static POIRepresentation Of(JToken      JSON,
                                           ETagFormat  Format)
        {

            var content = Format switch {
                              ETagFormat.JSON  => CanonicalJSON.ToUTF8Bytes(JSON),
                              ETagFormat.CBOR  => CBORJSON.ToCBOR(JSON).ToByteArray(CBORWriterOptions.Canonical),
                              _                => throw new ArgumentOutOfRangeException(nameof(Format), "Unsupported format!")
                          };

            return new (content, ETag.Compute(Format, content));

        }

    }


    /// <summary>
    /// An immutable collection of children with unique identifications, in the ordinal order of the texts of
    /// their identifications, the order of their JSON and CBOR representations; looked up by identification.
    /// Identifications are equal as their type compares them, e.g. without case.
    /// Its representations, e.g. the list of its children in short, are calculated once per view and format.
    /// </summary>
    /// <typeparam name="TId">The type of the identifications.</typeparam>
    /// <typeparam name="TEntity">The type of the children.</typeparam>
    public sealed class POICollection<TId, TEntity> : IReadOnlyList<TEntity>

        where TId     : notnull
        where TEntity : class

    {

        #region Data

        private readonly ImmutableArray<TEntity>                                                       items;
        private readonly ImmutableArray<TId>                                                           ids;
        private readonly ImmutableDictionary<TId, TEntity>                                             index;
        private readonly ConcurrentDictionary<(String View, ETagFormat Format), Lazy<POIRepresentation>>  representations = new ();

        #endregion

        #region Properties

        /// <summary>
        /// An empty collection.
        /// </summary>
        public static POICollection<TId, TEntity>  Empty    { get; } = new ([], [], ImmutableDictionary<TId, TEntity>.Empty);

        /// <summary>
        /// The children, in the ordinal order of the texts of their identifications.
        /// </summary>
        public ImmutableArray<TEntity>             Items
            => items;

        /// <summary>
        /// The identifications of the children, in their order.
        /// </summary>
        public ImmutableArray<TId>                 Ids
            => ids;

        /// <summary>
        /// The children by their identifications.
        /// </summary>
        public IReadOnlyDictionary<TId, TEntity>   Index
            => index;

        /// <summary>
        /// The number of children.
        /// </summary>
        public Int32                               Count
            => items.Length;

        /// <summary>
        /// Whether it has no children.
        /// </summary>
        public Boolean                             IsEmpty
            => items.IsEmpty;

        /// <summary>
        /// The child at the given position, in the order of the identifications.
        /// </summary>
        public TEntity this[Int32 Position]
            => items[Position];

        #endregion

        #region Constructor(s)

        private POICollection(ImmutableArray<TEntity>            Items,
                              ImmutableArray<TId>                Ids,
                              ImmutableDictionary<TId, TEntity>  Index)
        {
            this.items  = Items;
            this.ids    = Ids;
            this.index  = Index;
        }

        /// <summary>
        /// Create a new collection of the given children.
        /// </summary>
        /// <param name="Children">The children, in any order.</param>
        /// <param name="GetId">The identification of a child.</param>
        /// <param name="Description">The children in words, e.g. "charging pool".</param>
        /// <param name="Owner">Optionally, whose children they are in words, e.g. "the charging station operator 'DE*GEF'".</param>
        /// <exception cref="ArgumentException">A child is null, or two children have equal identifications.</exception>
        public POICollection(IEnumerable<TEntity>?  Children,
                             Func<TEntity, TId>     GetId,
                             String                 Description,
                             String?                Owner   = null)
        {

            var children  = new List<(TId Id, TEntity Child)>();
            var builder   = ImmutableDictionary.CreateBuilder<TId, TEntity>();

            foreach (var child in Children ?? [])
            {

                if (child is null)
                    throw new ArgumentException($"The {Description}s{(Owner is not null ? $" of {Owner}" : "")} must not contain null entries!", nameof(Children));

                var id = GetId(child);

                if (builder.ContainsKey(id))
                    throw new ArgumentException($"The {Description} '{id}'{(Owner is not null ? $" of {Owner}" : "")} must not be given twice!", nameof(Children));

                builder.Add(id, child);
                children.Add((id, child));

            }

            var ordered = children.OrderBy(child => child.Id.ToString(), StringComparer.Ordinal).ToArray();

            this.items  = [.. ordered.Select(child => child.Child)];
            this.ids    = [.. ordered.Select(child => child.Id)];
            this.index  = builder.ToImmutable();

        }

        #endregion


        #region Contains(Id)

        /// <summary>
        /// Whether it has a child with the given identification.
        /// </summary>
        public Boolean Contains(TId Id)

            => index.ContainsKey(Id);

        #endregion

        #region TryGet  (Id, out Child)

        /// <summary>
        /// Try to return the child with the given identification.
        /// </summary>
        public Boolean TryGet(TId                                Id,
                              [NotNullWhen(true)] out TEntity?   Child)

            => index.TryGetValue(Id, out Child);

        #endregion

        #region Get     (Id)

        /// <summary>
        /// The child with the given identification, or null.
        /// </summary>
        public TEntity? Get(TId Id)

            => index.TryGetValue(Id, out var child)
                   ? child
                   : null;

        #endregion

        #region Representation(View, Format, JSON)

        /// <summary>
        /// The representation of the given view of this collection, e.g. its children in short, in the given format:
        /// calculated from the JSON of the given delegate the first time only, then kept with the collection.
        /// A view must always be calculated alike, as the collection is immutable.
        /// </summary>
        /// <param name="View">The name of the view, e.g. "summaries".</param>
        /// <param name="Format">The format of the representation.</param>
        /// <param name="JSON">The JSON of the view.</param>
        public POIRepresentation Representation(String        View,
                                                ETagFormat    Format,
                                                Func<JToken>  JSON)

            => representations.GetOrAdd((View, Format),
                                        _ => new Lazy<POIRepresentation>(() => POIRepresentation.Of(JSON(), Format))).Value;

        #endregion


        #region GetEnumerator()

        /// <summary>
        /// The children, in the order of their identifications.
        /// </summary>
        public ImmutableArray<TEntity>.Enumerator GetEnumerator()
            => items.GetEnumerator();

        IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator()
            => ((IEnumerable<TEntity>) items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => ((IEnumerable) items).GetEnumerator();

        #endregion

    }

}
