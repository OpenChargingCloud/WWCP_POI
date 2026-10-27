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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The QUERY interface of the roaming networks: lists searched and paged, e.g. the charging pools
    /// of all charging station operators of a roaming network.
    /// </summary>
    /// <remarks>
    /// GET ~/RNs/{RN_Id}/CPs?match=…&amp;operatorId=…&amp;offset=…&amp;limit=…    the charging pools of all its operators,
    ///                                                                         the effective ones
    ///
    /// Every list of the roaming networks, ~/RNs and ~/RNs/{RN_Id}/{CSOs|GOs|EMPs|CSMs|POs} as well, takes
    /// - match:   words separated by spaces or commas, each contained in a text of an entry, case-insensitively;
    ///            a word starting with "!" must not be contained; at most MaxMatchLength characters and
    ///            MaxMatchWords words, all of them together,
    /// - offset:  the number of matching entries to skip,
    /// - limit:   the maximum number of entries to return, 1 to MaxListLimit; MaxListLimit without it.
    /// Entries are in the natural order of their identifications, a run of digits by its number: P1, P2, P10.
    /// X-Total-Count is the number of all entries,
    /// X-Filtered-Count the number of matching ones, and Link names the next and the previous page.
    /// The ETag is the one of the returned content. A list without any of these is kept with its version, and is
    /// returned whole.
    /// </remarks>
    public partial class POI_API
    {

        #region Data

        /// <summary>
        /// The default maximum number of entries of a list returned at once, when searched or paged.
        /// </summary>
        public const Int32 DefaultMaxListLimit     = 1000;

        /// <summary>
        /// The default maximum length of the words a list is searched for, all of them together.
        /// </summary>
        public const Int32 DefaultMaxMatchLength   = 256;

        /// <summary>
        /// The default maximum number of words a list is searched for.
        /// </summary>
        public const Int32 DefaultMaxMatchWords    = 16;

        #endregion

        #region Properties

        /// <summary>
        /// The maximum number of entries of a list returned at once, when searched or paged: a larger limit is refused,
        /// and without one, at most this many are returned, with a link to the next page. The whole list, neither
        /// searched nor paged, is kept with its version, and returned whole.
        /// </summary>
        public Int32 MaxListLimit      { get; set; } = DefaultMaxListLimit;

        /// <summary>
        /// The maximum length of the words a list is searched for, all match parameters together; longer is refused.
        /// </summary>
        public Int32 MaxMatchLength    { get; set; } = DefaultMaxMatchLength;

        /// <summary>
        /// The maximum number of words a list is searched for, all match parameters together; more are refused.
        /// </summary>
        public Int32 MaxMatchWords     { get; set; } = DefaultMaxMatchWords;

        #endregion

        #region (private) ListQuery

        /// <summary>
        /// A query of a list: its search words, offset and limit.
        /// </summary>
        /// <param name="Match">The search words, as given.</param>
        /// <param name="Offset">The number of matching entries to skip.</param>
        /// <param name="Limit">The maximum number of entries to return, as given.</param>
        /// <param name="Further">Further parameters of the list, e.g. operatorId, to keep in its links.</param>
        /// <param name="MaxLimit">The maximum number of entries to return without a limit.</param>
        private sealed record ListQuery(String?                                 Match,
                                        UInt64                                  Offset,
                                        UInt64?                                 Limit,
                                        IReadOnlyList<(String Name, String Value)>  Further,
                                        UInt64                                  MaxLimit)
        {

            private readonly String[] terms = (Match ?? "").Split([ ' ', ',' ], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            /// <summary>
            /// The number of its search words.
            /// </summary>
            public Int32 Words
                => terms.Length;

            /// <summary>
            /// The maximum number of entries of a page: the limit, or the maximum one without it.
            /// </summary>
            public UInt64 PageSize
                => Limit ?? MaxLimit;

            /// <summary>
            /// Whether it neither searches nor pages: the whole list.
            /// </summary>
            public Boolean IsWholeList
                => terms.Length == 0 && Offset == 0 && !Limit.HasValue && Further.Count == 0;

            /// <summary>
            /// Whether the given search text of an entry contains every word, and none of the ones starting with "!".
            /// </summary>
            public Boolean Matches(String SearchText)
            {

                foreach (var term in terms)
                {

                    var negated  = term.StartsWith('!');
                    var word     = negated ? term[1..] : term;

                    if (word.Length == 0)
                        continue;

                    if (SearchText.Contains(word, StringComparison.OrdinalIgnoreCase) == negated)
                        return false;

                }

                return true;

            }

            /// <summary>
            /// The query string of this query with the given offset.
            /// </summary>
            public String QueryString(UInt64 Offset)
            {

                var parameters = new List<(String, String)>();

                if (!String.IsNullOrWhiteSpace(Match))
                    parameters.Add(("match", Match));

                parameters.AddRange(Further);

                if (Offset > 0)
                    parameters.Add(("offset", Offset.ToString()));

                if (Limit.HasValue)
                    parameters.Add(("limit", Limit.Value.ToString()));

                return parameters.Count == 0
                           ? ""
                           : "?" + String.Join("&", parameters.Select(parameter => HttpUtility.UrlEncode(parameter.Item1) + "=" + HttpUtility.UrlEncode(parameter.Item2)));

            }

        }

        #endregion

        #region (private) PoolEntry, PoolIndex

        /// <summary>
        /// A charging pool of a roaming network, with its charging station operator.
        /// </summary>
        private sealed record PoolEntry(ChargingStationOperator  Operator,
                                        ChargingPool             Pool);

        /// <summary>
        /// The charging pools of all charging station operators of one version of a roaming network, in the natural
        /// order of their identifications, with the representations of the whole list.
        /// </summary>
        private sealed class PoolIndex(IReadOnlyList<PoolEntry> Entries)
        {

            public IReadOnlyList<PoolEntry>  Entries          { get; } = Entries;
            public Representations           Representations  { get; } = new ();

        }

        /// <summary>
        /// The charging pools of the versions of roaming networks, kept as long as the versions are.
        /// </summary>
        private readonly ConditionalWeakTable<RoamingNetwork, PoolIndex>  poolIndexes  = new ();

        /// <summary>
        /// The search texts of versions of entities, kept as long as the versions are.
        /// </summary>
        private readonly ConditionalWeakTable<Object, String>             searchTexts  = new ();

        #endregion


        #region (private) RegisterQueryURLTemplates()

        private void RegisterQueryURLTemplates()
        {

            #region ~/RNs/{RN_Id}/CPs

            AddEntityHandlers(HTTPPath.Root + "RNs/{roamingNetworkId}/CPs",
                              request => {

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  // As they are at runtime, found by what their groups give them: as they are given, their
                                  // operators embed them.
                                  if (request.QueryString.GetStrings("view").Any())
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"The charging pools of a roaming network are as they are at runtime! As they are given, their charging station operators embed them, ~/RNs/{roamingNetwork.Id}/CSOs/{{CSO_Id}}, and each has the view '{StoredView}'.");

                                  ChargingStationOperator_Id? operatorId = null;

                                  if (request.QueryString.GetString("operatorId") is String operatorIdText)
                                  {

                                      if (!ChargingStationOperator_Id.TryParse(operatorIdText, out var parsedOperatorId))
                                          return EntityErrorResponse(request, HTTPStatusCode.BadRequest, $"Invalid charging station operator identification '{operatorIdText}'!");

                                      operatorId = parsedOperatorId;

                                  }

                                  if (!TryParseListQuery(request,
                                                         operatorId.HasValue ? [ ("operatorId", operatorId.Value.ToString()) ] : [],
                                                         out var query,
                                                         out errorResponse))
                                  {
                                      return errorResponse;
                                  }

                                  var index = poolIndexes.GetValue(roamingNetwork, PoolsOf);

                                  return QueryResponse(request,
                                                       query,
                                                       () => operatorId.HasValue
                                                                 ? [.. index.Entries.Where(entry => entry.Operator.Id == operatorId.Value)]
                                                                 : index.Entries,
                                                       entry => SearchTextOf(entry.Pool, () => entry.Pool.ToJSON(Embedded: true)) + "\n" + SearchTextOf(entry.Operator, () => OperatorSummary(entry.Operator)),
                                                       entries => new JArray(entries.Select(PoolQueryJSON)),
                                                       format => index.Representations.GetOrAdd("pools", format, () => POIRepresentation.Of(new JArray(index.Entries.Select(PoolQueryJSON)), format)),
                                                       index.Entries.Count);

                              });

            #endregion

        }

        #endregion


        #region (private static) PoolsOf(RoamingNetwork)

        /// <summary>
        /// The charging pools of all charging station operators of the given roaming network, the effective ones, with the
        /// properties their groups give them, in the natural order of their identifications.
        /// </summary>
        private static PoolIndex PoolsOf(RoamingNetwork RoamingNetwork)

            => new ([.. RoamingNetwork.ChargingStationOperators.
                            SelectMany(chargingStationOperator => chargingStationOperator.EffectiveChargingPools.Select(chargingPool => new PoolEntry(chargingStationOperator, chargingPool))).
                            OrderBy   (entry => entry.Pool.Id.ToString(), POINaturalOrder.Instance)]);

        #endregion

        #region (private static) PoolQueryJSON(Entry)

        /// <summary>
        /// An effective charging pool, as its operator would embed it, with { "@id", "name" } of its operator.
        /// </summary>
        private static JObject PoolQueryJSON(PoolEntry Entry)
        {

            var json = Entry.Pool.ToJSON(Embedded: true);

            json.Add(PropertyKeys.ChargingStationOperator, OperatorSummary(Entry.Operator));

            return json;

        }

        #endregion

        #region (private static) OperatorSummary(Operator)

        private static JObject OperatorSummary(ChargingStationOperator Operator)
        {

            var json = new JObject(
                           new JProperty(POI.RoamingNetwork.BasePropertyKeys.Id, Operator.Id.ToString())
                       );

            if (Operator.Name.IsNotNullOrEmpty())
                json.Add(POI.RoamingNetwork.BasePropertyKeys.Name, Operator.Name.ToJSON());

            return json;

        }

        #endregion


        #region (private) SearchTextOf(Version, JSON)

        /// <summary>
        /// The texts of the given version of an entity a search looks at: every string of its JSON,
        /// one per line, calculated once per version.
        /// </summary>
        private String SearchTextOf(Object        Version,
                                    Func<JToken>  JSON)

            => searchTexts.GetValue(Version, _ => SearchText(JSON()));

        #endregion

        #region (private static) SearchText(JSON)

        /// <summary>
        /// Every string value of the given JSON, one per line; not the property names.
        /// </summary>
        private static String SearchText(JToken JSON)
        {

            var text = new StringBuilder();

            foreach (var value in ((JSON as JContainer)?.DescendantsAndSelf() ?? [ JSON ]).OfType<JValue>())
                if (value.Type == JTokenType.String)
                    text.Append(value.Value<String>()).Append('\n');

            return text.ToString();

        }

        #endregion

        #region (private)        TryParseListQuery(Request, Further, out Query, out ErrorResponse)

        /// <summary>
        /// The match, offset and limit of the request, or an error response: a limit of 1 to MaxListLimit, and a match
        /// of at most MaxMatchLength characters and MaxMatchWords words.
        /// </summary>
        private Boolean TryParseListQuery(HTTPRequest                                       Request,
                                          IReadOnlyList<(String Name, String Value)>        Further,
                                          [NotNullWhen(true)]  out ListQuery?               Query,
                                          [NotNullWhen(false)] out HTTPResponse?            ErrorResponse)
        {

            Query          = null;
            ErrorResponse  = null;

            var     offsetText  = Request.QueryString.GetString("offset");
            var     limitText   = Request.QueryString.GetString("limit");
            var     maxLimit    = (UInt64) Math.Max(1, MaxListLimit);
            UInt64  offset      = 0;
            UInt64? limit       = null;

            if (offsetText is not null && !UInt64.TryParse(offsetText, out offset))
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid offset '{offsetText}'!");
                return false;
            }

            if (limitText is not null)
            {

                // At least one entry: a page of none would name itself as the next one.
                if (!UInt64.TryParse(limitText, out var parsedLimit) || parsedLimit == 0 || parsedLimit > maxLimit)
                {
                    ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid limit '{limitText}': it must be at least 1 and at most {maxLimit}!");
                    return false;
                }

                limit = parsedLimit;

            }

            // Every part of every match: a comma separates words as a space does, and a word given in a match of
            // its own is one more word, e.g. ?match=jena,nord&amp;match=!süd.
            var match = String.Join(" ", Request.QueryString.GetStrings("match"));

            // Every word is looked for in the texts of every entry: their number and length bound what a search costs.
            if (match.Length > MaxMatchLength)
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"The match is {match.Length} characters long, longer than {MaxMatchLength}!");
                return false;
            }

            Query = new ListQuery(match.Length > 0 ? match : null,
                                  offset,
                                  limit,
                                  Further,
                                  maxLimit);

            if (Query.Words > MaxMatchWords)
            {
                ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"The match has {Query.Words} words, more than {MaxMatchWords}!");
                Query         = null;
                return false;
            }

            return true;

        }

        #endregion

        #region (private)        QueryResponse<T>(Request, Query, Entries, SearchText, ToJSON, WholeList, TotalCount)

        /// <summary>
        /// The entries of the given list the query matches, paged, in JSON or in CBOR as the request prefers,
        /// with X-Total-Count, X-Filtered-Count and Link. The whole list comes from where it is kept.
        /// </summary>
        /// <param name="Request">The HTTP request.</param>
        /// <param name="Query">The query.</param>
        /// <param name="Entries">The entries to search, in their order; asked for only when the query is not the whole list.</param>
        /// <param name="SearchText">The texts of an entry a search looks at.</param>
        /// <param name="ToJSON">The JSON of the given entries.</param>
        /// <param name="WholeList">The representation of the whole list in the given format, kept with its version.</param>
        /// <param name="TotalCount">The number of all entries of the list, before any parameter.</param>
        private static HTTPResponse QueryResponse<T>(HTTPRequest                               Request,
                                                     ListQuery                                 Query,
                                                     Func<IReadOnlyList<T>>                    Entries,
                                                     Func<T, String>                           SearchText,
                                                     Func<IEnumerable<T>, JArray>              ToJSON,
                                                     Func<ETagFormat, POIRepresentation>       WholeList,
                                                     Int32                                     TotalCount)
        {

            var format = FormatOf(Request);

            if (Query.IsWholeList)
                return EntityResponse(Request,
                                      format,
                                      WholeList(format),
                                      response => {
                                          response.Set("X-Total-Count",     TotalCount.ToString());
                                          response.Set("X-Filtered-Count",  TotalCount.ToString());
                                      });

            var entries   = Entries();
            var filtered  = Query.Match is null
                                ? entries
                                : [.. entries.Where(entry => Query.Matches(SearchText(entry)))];

            // At most a page: without a limit, as many as one may ask for.
            var offset    = (Int32) Math.Min(Query.Offset, (UInt64) filtered.Count);
            var limit     = (Int32) Math.Min(Query.PageSize, (UInt64) (filtered.Count - offset));
            var page      = filtered.Skip(offset).Take(limit);

            var representation = POIRepresentation.Of(ToJSON(page), format);

            var links = new List<String>();
            var path  = Request.Path.ToString();

            if (offset + limit < filtered.Count)
                links.Add($"<{path}{Query.QueryString((UInt64) (offset + limit))}>; rel=\"next\"");

            if (offset > 0)
                links.Add($"<{path}{Query.QueryString((UInt64) Math.Max(0L, (Int64) offset - (Int64) Math.Min(Query.PageSize, (UInt64) Int32.MaxValue)))}>; rel=\"prev\"");

            return EntityResponse(Request,
                                  format,
                                  representation,
                                  response => {

                                      response.Set("X-Total-Count",     TotalCount.     ToString());
                                      response.Set("X-Filtered-Count",  filtered.Count. ToString());

                                      if (links.Count > 0)
                                          response.Set("Link", String.Join(", ", links));

                                  });

        }

        #endregion

    }

}
