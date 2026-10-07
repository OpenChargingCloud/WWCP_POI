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

using System.Globalization;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Shared validation for the infrastructure hierarchy. Errors retain their JSON path.
    /// </summary>
    internal static class InfrastructureJson
    {

        #region Read and validate documents

        /// <summary>
        /// Read JSON without inferring dates or converting decimal numbers to doubles.
        /// </summary>
        internal static JObject ReadObject(String json)

            => (JObject) ReadToken(json);

        internal static JToken ReadToken(String json)
        {

            using var text   = new StringReader(json);
            using var reader = new JsonTextReader(text)
            {
                DateParseHandling  = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal
            };

            var token = JToken.ReadFrom(reader, new JsonLoadSettings {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            if (reader.Read())
                throw new ArgumentException("Unexpected content after the JSON value.");
            return token;

        }

        internal static void ValidateFields(JObject json, params String[] fields)
        {

            ArgumentNullException.ThrowIfNull(json);
            if (json[POIContentProfile.PropertyName] is { } profile)
                POIContentProfile.Require(profile.Type == JTokenType.String ? profile.Value<String>() : null);

            foreach (var property in json.Properties())
            {
                if (property.Name != "ETags" && property.Name != POIContentProfile.PropertyName && !fields.Contains(property.Name))
                    throw new ArgumentException($"Unknown property '{property.Name}'.");
            }

        }

        internal static void Validate(JObject json, String context)
        {

            ArgumentNullException.ThrowIfNull(json);
            if (json[POIContentProfile.PropertyName] is { } profile)
                POIContentProfile.Require(profile.Type == JTokenType.String ? profile.Value<String>() : null);

            if (json["exception"] is not null)
                throw new ArgumentException("Cannot parse a serializer error document.");

            if (json["@context"] is { } token &&
                (token.Type != JTokenType.String || token.Value<String>() != context))
            {
                throw new ArgumentException("Invalid '@context'.");
            }

        }

        #endregion

        #region Read scalar values

        internal static String? Text(JObject json, String field)
        {

            if (json[field] is not { } token || token.Type == JTokenType.Null)
                return null;

            if (token.Type != JTokenType.String)
                throw new ArgumentException($"{field}: expected a string.");

            return token.Value<String>();

        }

        internal static Boolean? Boolean(JObject json, String field)
        {

            if (json[field] is not { } token || token.Type == JTokenType.Null)
                return null;

            if (token.Type != JTokenType.Boolean)
                throw new ArgumentException($"{field}: expected a boolean.");

            return token.Value<Boolean>();

        }

        internal static JObject? Object(JObject json, String field)
        {

            if (json[field] is not { } token || token.Type == JTokenType.Null)
                return null;

            return token as JObject ??
                   throw new ArgumentException($"{field}: expected an object.");

        }

        internal static I18NString? Name(JObject json, String field)
        {

            var document = Object(json, field);

            if (document is null)
                return null;

            if (!document.HasValues)
                return I18NString.Empty;

            if (!I18NString.TryParse(document, out I18NString? result, out var error))
                throw new ArgumentException($"{field}: {error}");

            return result;

        }

        internal static T? Scalar<T>(JObject json, String field, JsonValueParsing.ScalarParser<T> parser)
            where T : struct
        {

            if (json[field] is { } token && token.Type is not (JTokenType.String or JTokenType.Null))
                throw new ArgumentException($"{field}: expected a string.");

            if (!JsonValueParsing.TryReadOptional(json, field, parser, out var value, out var error))
                throw new ArgumentException(error);

            return value;

        }

        internal static DateTimeOffset? Date(JObject json, String field)
        {

            if (json[field] is not { } token)
                return null;

            if (token.Type == JTokenType.Date)
            {
                return ((JValue) token).Value switch
                {
                    DateTimeOffset offset => offset,
                    DateTime dateTime when dateTime.Kind != DateTimeKind.Unspecified => new DateTimeOffset(dateTime),
                    _                     => throw new ArgumentException($"{field}: invalid timestamp.")
                };
            }

            if (token.Type != JTokenType.String ||
                !System.Text.RegularExpressions.Regex.IsMatch(token.Value<String>()!, @"(?:Z|[+-][0-9]{2}:[0-9]{2})$") ||
                !DateTimeOffset.TryParse(token.Value<String>(),
                                         CultureInfo.InvariantCulture,
                                         DateTimeStyles.RoundtripKind,
                                         out var date))
            {
                throw new ArgumentException($"{field}: invalid timestamp.");
            }

            return date;

        }

        /// <summary>
        /// Read a single named flag from a flat array.
        /// </summary>
        internal static T Flags<T>(JToken token)
            where T : struct, Enum
        {

            if (token.Type != JTokenType.String ||
                !Enum.TryParse<T>(token.Value<String>(), out var flag) ||
                !Enum.IsDefined(flag))
            {
                throw new ArgumentException($"Invalid {typeof(T).Name} value.");
            }

            return flag;

        }

        #endregion

        #region Read location, custom data and address

        internal static JObject LocationJSON(GeoCoordinate coordinate, Boolean embedded = false)
        {
            var json = coordinate.ToJSON(Embedded: embedded);
            if (coordinate.Altitude is { } altitude) json["alt"] = MetrologyJson.AltitudeText(altitude.Value);
            return json;
        }

        internal static GeoCoordinate? Location(JObject json)
        {

            if (Object(json, "geoLocation") is not { } document)
                return null;

            return At("geoLocation", () =>
            {
                var copy = new JObject(
                               new JProperty("latitude",  document["lat"]?.DeepClone()),
                               new JProperty("longitude", document["lng"]?.DeepClone()));

                if (document["alt"] is { } altitude)
                    copy["altitude"] = altitude.DeepClone();

                var parsed     = AdditionalGeoLocation.Parse(copy).GeoLocation;
                var projection = Text(document, "projection");

                if (projection is null)
                    return parsed;

                if (!Enum.TryParse<GravitationalModel>(projection, out var model) || !Enum.IsDefined(model))
                    throw new ArgumentException("Invalid projection.");

                return new GeoCoordinate(parsed.Latitude, parsed.Longitude, parsed.Altitude, model);
            });

        }

        internal static CustomDataNew? CustomData(JObject json)

            => Object(json, "customData") is { } document
                   ? CustomDataNew.ParseJSON(document.ToString(Formatting.None))
                   : null;

        internal static Address? Address(JObject json)

            => Object(json, "address") is { } document
                   ? At("address", () => org.GraphDefined.Vanaheimr.Illias.Address.Parse(document))
                   : null;

        #endregion

        #region Read opening times

        internal static OpeningTimes? Openings(JObject json)
        {

            if (Object(json, "openingTimes") is not { } document)
                return null;

            return At("openingTimes", () =>
            {
                var result = new OpeningTimes(Text(document, "freeText") ?? "");
                var allDay = Boolean(document, "24/7") ??
                             throw new ArgumentException("Missing '24/7'.");

                ReadRegularOpenings(document, result);
                ReadExceptionalOpenings(document, result);

                if (result.IsOpen24Hours != allDay)
                    throw new ArgumentException("'24/7' contradicts the regular opening hours.");

                return result;
            });

        }

        private static void ReadRegularOpenings(JObject json, OpeningTimes result)
        {

            var days = new HashSet<DayOfWeek>();

            foreach (var dayJSON in Array(json, "regularOpenings", Entry))
            {
                if (dayJSON.Count != 1)
                    throw new ArgumentException("Each regular opening must describe one weekday.");

                var day = dayJSON.Properties().Single();

                if (!Enum.TryParse<DayOfWeek>(day.Name, true, out var weekday) ||
                    !Enum.IsDefined(weekday) ||
                    !days.Add(weekday))
                {
                    throw new ArgumentException("Invalid or duplicate weekday.");
                }

                if (day.Value is not JArray hours || hours.Count == 0)
                    throw new ArgumentException("Expected nonempty opening hours.");

                foreach (var entry in hours)
                {
                    var hour = Entry(entry);

                    if (!HourMin.TryParse(Text(hour, "begin") ?? "", out var begin) ||
                        !HourMin.TryParse(Text(hour, "end")   ?? "", out var end) ||
                        begin == end)
                    {
                        throw new ArgumentException("Invalid opening interval.");
                    }

                    result.AddRegularOpening(weekday, begin, end);
                }
            }

        }

        private static void ReadExceptionalOpenings(JObject json, OpeningTimes result)
        {

            foreach (var field in new[] { "exceptionalOpenings", "exceptionalClosings" })
            {
                foreach (var token in Array(json, field, token => token))
                {
                    var parts = token.Type == JTokenType.String
                                    ? token.Value<String>()!.Split(" -> ")
                                    : [];

                    if (parts.Length != 2 ||
                        !DateTime.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var begin) ||
                        !DateTime.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var end) ||
                        begin >= end)
                    {
                        throw new ArgumentException($"Invalid {field} interval.");
                    }

                    if (field == "exceptionalOpenings")
                        result.AddExceptionalOpening(begin, end);
                    else
                        result.AddExceptionalClosing(begin, end);
                }
            }

        }

        #endregion

        #region Read collections with JSON paths

        internal static T At<T>(String path, Func<T> parse)
        {

            try
            {
                return parse();
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"{path}: {exception.Message}", exception);
            }

        }

        internal static List<T> Array<T>(JObject json, String field, Func<JToken, T> parse)
        {

            var result = new List<T>();

            if (json[field] is not { } token)
                return result;

            if (token is not JArray array)
                throw new ArgumentException($"{field}: expected an array.");

            for (var index = 0; index < array.Count; index++)
                result.Add(At($"{field}[{index}]", () => parse(array[index])));

            return result;

        }

        internal static JObject Entry(JToken token)

            => token as JObject ??
               throw new ArgumentException("Expected an object.");

        #endregion

        #region Read licenses, brands and entities

        internal static List<DataLicense> Licenses(JObject json)
        {

            var licenses = Array(json, "dataLicenseIds", token =>
            {
                if (token.Type != JTokenType.String || !DataLicense_Id.TryParse(token.Value<String>()!, out var id))
                    throw new ArgumentException("Invalid license identifier.");

                return new DataLicense(id);
            });

            licenses.AddRange(Array(json, "dataLicenses", token => ParseDataLicense(Entry(token))));

            Unique(licenses, license => license.Id, "dataLicenses");

            return licenses;

        }

        internal static DataLicense ParseDataLicense(JObject json)
        {
            Validate(json, DataLicense.JSONLDContext);
            ValidateFields(json, "@id", "@context", "description", "URLs");
            var text = Text(json, "@id");
            if (text is null || !DataLicense_Id.TryParse(text, out var id))
                throw new ArgumentException("@id: invalid or missing data license identifier.");
            var links = Array(json, "URLs", token =>
            {
                if (token.Type != JTokenType.String || !Uri.TryCreate(token.Value<String>(), UriKind.Absolute, out _) ||
                    !URL.TryParse(token.Value<String>()!, out var url))
                    throw new ArgumentException("URLs: expected an absolute URL string.");
                return url;
            });
            return new DataLicense(id, Name(json, "description") ?? I18NString.Empty, links.ToArray());
        }

        internal static List<Brand> Brands(JObject                            json,
                                           String                             ids     = "brandIds",
                                           String                             objects = "brands",
                                           InfrastructureJsonParsingContext?  context = null)
        {

            var brands = Entities(json,
                                  ids,
                                  objects,
                                  Brand_Id.TryParse,
                                  context?.ResolveBrand,
                                  document => Brand.Parse(document),
                                  "id");

            Unique(brands, brand => brand.Id, objects);

            return brands;

        }

        internal static List<T> Entities<TId, T>(JObject                                json,
                                                String                                 ids,
                                                String                                 objects,
                                                JsonValueParsing.ScalarParser<TId>     parseId,
                                                Func<TId, JObject?>?                   resolve,
                                                Func<JObject, T>                       parse,
                                                String                                 idProperty = "@id")
            where TId : struct
        {

            var result = Array(json, objects, token => parse(Entry(token)));

            result.AddRange(Array(json, ids, token =>
            {
                if (token.Type != JTokenType.String || !parseId(token.Value<String>()!, out var id))
                    throw new ArgumentException("Invalid reference identifier.");

                var document = resolve?.Invoke(id) ??
                               throw new ArgumentException($"Unresolved reference '{id}'.");

                var resolvedId = Text(document, idProperty);

                if (resolvedId is null || !parseId(resolvedId, out var actualId) || !id.Equals(actualId))
                    throw new ArgumentException($"Resolved document does not match identifier '{id}'.");

                // A fresh parse creates children bound to the new parent. Never alter resolver-owned JSON.
                return parse((JObject) document.DeepClone());
            }));

            return result;

        }

        #endregion

        #region Validate hierarchy references

        internal static void RejectReferences(JObject json, String field)
        {

            var references = Array(json, field, token => token);

            if (references.Count > 0)
                throw new ArgumentException($"{field}: unresolved references; supply expanded objects.");

        }

        internal static void Parent(JObject json, String field, String? expected)
        {

            var id = Text(json, field + "Id");

            if (id is not null && (expected is null || id != expected))
                throw new ArgumentException($"{field}Id: does not match the supplied parent.");

            if (json[field] is not null)
                throw new ArgumentException($"{field}: expanded ancestor is not supported; supply the parent explicitly.");

        }

        internal static void Unique<T, TKey>(IEnumerable<T> items, Func<T, TKey> key, String field)
            where TKey : notnull
        {

            var ids = new HashSet<TKey>();

            foreach (var item in items)
            {
                if (!ids.Add(key(item)))
                    throw new ArgumentException($"{field}: duplicate identifier '{key(item)}'.");
            }

        }

        internal static void References<TId>(JObject                               json,
                                            String                                field,
                                            JsonValueParsing.ScalarParser<TId>    parse,
                                            IEnumerable<TId>                      existing)
            where TId : struct
        {

            var ids  = existing.ToHashSet();
            var seen = new HashSet<TId>();

            foreach (var token in Array(json, field, token => token))
            {
                if (token.Type != JTokenType.String ||
                    !parse(token.Value<String>()!, out var id) ||
                    !ids.Contains(id))
                {
                    throw new ArgumentException($"{field}: reference is not present in the parsed hierarchy.");
                }

                if (!seen.Add(id))
                    throw new ArgumentException($"{field}: duplicate reference '{id}'.");
            }

        }

        #endregion

        #region Restore and serialize snapshot metadata



        internal static void RestoreMetadata<TId, TAdmin, TStatus>(
            JObject                                    json,
            AImmutableEMobilityEntity<TId, TAdmin, TStatus>       entity,
            JsonValueParsing.ScalarParser<TAdmin>      adminParser,
            JsonValueParsing.ScalarParser<TStatus>     statusParser)

            where TId     : IId
            where TAdmin  : struct, IComparable
            where TStatus : struct, IComparable
        {

            if (ReadStatus(json, "adminStatus", adminParser) is { } admin)
                entity.SetAdminStatus([admin]);

            if (ReadStatus(json, "status", statusParser) is { } status)
                entity.SetStatus([status]);

            entity.RestoreSnapshotTimestamps(Date(json, "created"), Date(json, "lastChange"));

        }

        private static Timestamped<T>? ReadStatus<T>(JObject json, String field, JsonValueParsing.ScalarParser<T> parser)
            where T : struct
        {

            if (Object(json, field) is not { } state)
                return null;

            return At(field, () =>
            {
                var value = Text(state, "value");

                if (value is null || !parser(value, out var parsed))
                    throw new ArgumentException("value: invalid status.");

                var timestamp = Date(state, "timestamp") ??
                                throw new ArgumentException("timestamp: missing timestamp.");

                return new Timestamped<T>(timestamp, parsed);
            });

        }



        internal static JObject SnapshotMetadata<TId, TAdmin, TStatus>(
            JObject                                    json,
            AImmutableEMobilityEntity<TId, TAdmin, TStatus>       entity)

            where TId     : IId
            where TAdmin  : IComparable
            where TStatus : IComparable
        {

            json["created"]     = entity.Created.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            json["lastChange"]  = entity.LastChangeDate.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            json["adminStatus"] = new JObject(
                                      new JProperty("value",     entity.AdminStatus.Value.ToString()),
                                      new JProperty("timestamp", entity.AdminStatus.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
            json["status"]      = new JObject(
                                      new JProperty("value",     entity.Status.Value.ToString()),
                                      new JProperty("timestamp", entity.Status.Timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));

            if (entity.CustomData.HasValues)
                json["customData"] = entity.CustomData.ToJObject();

            if (entity.DataSource is not null)
                json["dataSource"] = entity.DataSource;

            if (entity is ChargingStationOperator chargingStationOperator && chargingStationOperator.ChargingTariffs.Any())
            {
                json["chargingTariffs"] = new JArray(
                                             chargingStationOperator.ChargingTariffs.
                                                 OrderBy(tariff => tariff.Id.ToString(), StringComparer.Ordinal).
                                                 Select(tariff => SnapshotMetadata(
                                                                      tariff.ToJSON(Embedded:           true,
                                                                                    ExpandBrandIds:     InfoStatus.Expanded,
                                                                                    ExpandDataLicenses: InfoStatus.Expanded),
                                                                      tariff)));
            }

            POIAdditionalProperties.Write(json, entity);

            // Snapshot fields describe the node's own values, including overrides equal to its parent.
            if (entity is EVSE meterOwner && meterOwner.EnergyMeter is { } meter)
                json["energyMeter"] = SnapshotMetadata(meter.ToJSON(Embedded: true), meter);

            if (entity is EVSE evse && evse.GeoLocation is { } evseLocation)
                json["geoLocation"] = InfrastructureJson.LocationJSON(evseLocation, true);

            if (entity is ChargingStation station)
                SnapshotStationProperties(json, station);

            return json;

        }

        private static void SnapshotStationProperties(JObject json, ChargingStation station)
        {

            if (station.GeoLocation is { } location)
                json["geoLocation"] = InfrastructureJson.LocationJSON(location, true);

            if (station.Address is { } address)
                json["address"] = address.ToJSON(Embedded: true);

            if (station.OpeningTimes is { } openings)
                json["openingTimes"] = openings.ToJSON();

            if (station.HotlinePhoneNumber is { } hotline)
                json["hotlinePhoneNumber"] = hotline.ToString();

            if (station.AuthenticationModes.Any())
                json["authenticationModes"] = station.AuthenticationModes.ToJSON();

        }

        #endregion

    }

}
