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
    ///            a word starting with "!" must not be contained,
    /// - offset:  the number of matching entries to skip,
    /// - limit:   the maximum number of entries to return, at least 1.
    /// Entries are in the ordinal order of their identifications. X-Total-Count is the number of all entries,
    /// X-Filtered-Count the number of matching ones, and Link names the next and the previous page.
    /// The ETag is the one of the returned content. A list without any of these is kept with its version.
    /// </remarks>
    public partial class POI_API
    {

        #region (private) ListQuery

        /// <summary>
        /// A query of a list: its search words, offset and limit.
        /// </summary>
        /// <param name="Match">The search words, as given.</param>
        /// <param name="Offset">The number of matching entries to skip.</param>
        /// <param name="Limit">The maximum number of entries to return, or all.</param>
        /// <param name="Further">Further parameters of the list, e.g. operatorId, to keep in its links.</param>
        private sealed record ListQuery(String?                                 Match,
                                        UInt64                                  Offset,
                                        UInt64?                                 Limit,
                                        IReadOnlyList<(String Name, String Value)>  Further)
        {

            private readonly String[] terms = (Match ?? "").Split([ ' ', ',' ], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

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
        /// The charging pools of all charging station operators of one version of a roaming network, in the ordinal
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
                                                       entry => SearchTextOf(entry.Pool, entry.Pool.ToJSON) + "\n" + SearchTextOf(entry.Operator, () => OperatorSummary(entry.Operator)),
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
        /// properties their groups give them, in the ordinal order of their identifications.
        /// </summary>
        private static PoolIndex PoolsOf(RoamingNetwork RoamingNetwork)

            => new ([.. RoamingNetwork.ChargingStationOperators.
                            SelectMany(chargingStationOperator => chargingStationOperator.EffectiveChargingPools.Select(chargingPool => new PoolEntry(chargingStationOperator, chargingPool))).
                            OrderBy   (entry => entry.Pool.Id.ToString(), StringComparer.Ordinal)]);

        #endregion

        #region (private static) PoolQueryJSON(Entry)

        /// <summary>
        /// An effective charging pool, as its operator would embed it, with { "@id", "name" } of its operator.
        /// </summary>
        private static JObject PoolQueryJSON(PoolEntry Entry)
        {

            var json = Entry.Pool.ToJSON();

            json.Add("chargingStationOperator", OperatorSummary(Entry.Operator));

            return json;

        }

        #endregion

        #region (private static) OperatorSummary(Operator)

        private static JObject OperatorSummary(ChargingStationOperator Operator)
        {

            var json = new JObject(
                           new JProperty("@id", Operator.Id.ToString())
                       );

            if (Operator.Name.IsNotNullOrEmpty())
                json.Add("name", Operator.Name.ToJSON());

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
        /// The match, offset and limit of the request, or an error response.
        /// </summary>
        private static Boolean TryParseListQuery(HTTPRequest                                       Request,
                                                 IReadOnlyList<(String Name, String Value)>        Further,
                                                 [NotNullWhen(true)]  out ListQuery?               Query,
                                                 [NotNullWhen(false)] out HTTPResponse?            ErrorResponse)
        {

            Query          = null;
            ErrorResponse  = null;

            var     offsetText  = Request.QueryString.GetString("offset");
            var     limitText   = Request.QueryString.GetString("limit");
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
                if (!UInt64.TryParse(limitText, out var parsedLimit) || parsedLimit == 0)
                {
                    ErrorResponse = EntityErrorResponse(Request, HTTPStatusCode.BadRequest, $"Invalid limit '{limitText}': it must be at least 1!");
                    return false;
                }

                limit = parsedLimit;

            }

            // Every part: Hermod splits a value at its commas, and GetString(…) would be the last part only,
            // e.g. "nord" of "jena, nord". A comma separates words as a space does.
            var match = String.Join(" ", Request.QueryString.GetStrings("match"));

            Query = new ListQuery(match.Length > 0 ? match : null,
                                  offset,
                                  limit,
                                  Further);

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

            var offset    = (Int32) Math.Min(Query.Offset, (UInt64) filtered.Count);
            var limit     = (Int32) Math.Min(Query.Limit ?? (UInt64) filtered.Count, (UInt64) (filtered.Count - offset));
            var page      = filtered.Skip(offset).Take(limit);

            var representation = POIRepresentation.Of(ToJSON(page), format);

            var links = new List<String>();
            var path  = Request.Path.ToString();

            if (offset + limit < filtered.Count)
                links.Add($"<{path}{Query.QueryString((UInt64) (offset + limit))}>; rel=\"next\"");

            if (offset > 0 && Query.Limit.HasValue)
                links.Add($"<{path}{Query.QueryString((UInt64) Math.Max(0L, (Int64) offset - (Int64) Math.Min(Query.Limit.Value, (UInt64) Int32.MaxValue)))}>; rel=\"prev\"");

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
