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

using System.Collections.Immutable;
using System.Text.Json;
using System.Globalization;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Immutable opening hours. Mutable Illias values are converted at the API boundary.
/// </summary>
public sealed partial class ImmutableOpeningTimes
{
    private readonly JsonElement document;
    public ImmutableOpeningTimes(OpeningTimes value)
    {
        ArgumentNullException.ThrowIfNull(value);
        document = JsonSerializer.Deserialize<JsonElement>(value.ToJSON().ToString(Newtonsoft.Json.Formatting.None));
        IsOpen24Hours = value.IsOpen24Hours;
        FreeText = value.FreeText;
        RegularOpenings = value.RegularOpenings.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray());
        ExceptionalOpenings = ReadPeriods("exceptionalOpenings");
        ExceptionalClosings = ReadPeriods("exceptionalClosings");
    }
    public Boolean IsOpen24Hours { get; }
    public String? FreeText { get; }
    public ImmutableDictionary<DayOfWeek, ImmutableArray<RegularHours>> RegularOpenings { get; }
    public ImmutableArray<ExceptionalPeriod> ExceptionalOpenings { get; }
    public ImmutableArray<ExceptionalPeriod> ExceptionalClosings { get; }

    private ImmutableArray<ExceptionalPeriod> ReadPeriods(String field)
        => document.TryGetProperty(field, out var periods)
               ? periods.EnumerateArray().Select(period =>
                 {
                     var interval = period.GetString()!.Split(" -> ");
                     return new ExceptionalPeriod(
                         DateTime.Parse(interval[0], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                         DateTime.Parse(interval[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
                 }).ToImmutableArray()
               : [];
    public JObject ToJSON() => JObject.Parse(document.GetRawText());
    public OpeningTimes ToMutable() => InfrastructureJson.Openings(new JObject(new JProperty("openingTimes", ToJSON())))!;
    public String AsFreeText() => ToMutable().AsFreeText();
    public static implicit operator ImmutableOpeningTimes(OpeningTimes value) => new(value);
    public static implicit operator OpeningTimes(ImmutableOpeningTimes value) => value.ToMutable();
    public override String ToString() => AsFreeText();
}
