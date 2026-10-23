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

using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.loadtests
{

    /// <summary>
    /// The machine a load test runs on, and its reports: everything as JSON, the main numbers as Markdown tables and as
    /// CSV, one row for every size and store, and one for every level of writers.
    /// </summary>
    internal static class LoadTestReport
    {

        #region Environment(Options)

        /// <summary>
        /// The machine, the runtime, its garbage collector, the builds measured, and the options.
        /// </summary>
        public static JObject Environment(LoadTestOptions Options)
        {

            var poi       = typeof(RoamingNetwork).Assembly;
            var self      = typeof(Program).       Assembly;
            var styx      = typeof(COSEKeyPair).   Assembly;
            var warnings  = new JArray();

            foreach (var assembly in new[] { poi, self, styx })
                if (IsDebug(assembly))
                    warnings.Add($"{assembly.GetName().Name} is a Debug build: measure a Release build (dotnet build -c Release).");

            if (Debugger.IsAttached)
                warnings.Add("A debugger is attached.");

            return new JObject(new JProperty("cpu",                   CPUName()),
                               new JProperty("logicalProcessors",     System.Environment.ProcessorCount),
                               new JProperty("memoryBytes",           GC.GetGCMemoryInfo().TotalAvailableMemoryBytes),
                               new JProperty("os",                    RuntimeInformation.OSDescription),
                               new JProperty("architecture",          RuntimeInformation.ProcessArchitecture.ToString()),
                               new JProperty("runtime",               RuntimeInformation.FrameworkDescription),
                               new JProperty("serverGC",              GCSettings.IsServerGC),
                               new JProperty("gcLatencyMode",         GCSettings.LatencyMode.ToString()),
                               new JProperty("gcConfiguration",       new JObject(GC.GetConfigurationVariables().Select(variable => new JProperty(variable.Key, JToken.FromObject(variable.Value))))),
                               new JProperty("environmentVariables",  new JObject(System.Environment.GetEnvironmentVariables().Keys.Cast<String>().
                                                                                      Where  (name => name.StartsWith("DOTNET_", StringComparison.OrdinalIgnoreCase)).
                                                                                      Order  (StringComparer.Ordinal).
                                                                                      Select (name => new JProperty(name, System.Environment.GetEnvironmentVariable(name))))),
                               new JProperty("builds",                new JArray(new[] { poi, styx, self }.Select(Build))),
                               new JProperty("options",               JObject.FromObject(Options, JsonSerializer.Create(new JsonSerializerSettings { Converters = [ new Newtonsoft.Json.Converters.StringEnumConverter() ] }))),
                               new JProperty("warnings",              warnings));

        }

        private static Boolean IsDebug(Assembly Assembly)
            => Assembly.GetCustomAttribute<DebuggableAttribute>()?.IsJITOptimizerDisabled == true;

        private static JObject Build(Assembly Assembly)

            => new (new JProperty("assembly",       Assembly.GetName().Name),
                    new JProperty("configuration",  IsDebug(Assembly) ? "Debug" : "Release"),
                    new JProperty("gitRepository",  Metadata(Assembly, "GitRepository")),
                    new JProperty("gitCommit",      Metadata(Assembly, "GitCommit")));

        private static String Metadata(Assembly  Assembly,
                                       String    Key)

            => Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(attribute => attribute.Key == Key)?.Value ?? "unknown";

        /// <summary>
        /// The name of the processor, as the operating system tells it.
        /// </summary>
        private static String CPUName()
        {

            try
            {

                if (OperatingSystem.IsWindows())
                    return (Microsoft.Win32.Registry.GetValue(@"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0", "ProcessorNameString", null) as String)?.Trim() ?? "unknown";

                if (OperatingSystem.IsLinux())
                    foreach (var line in File.ReadLines("/proc/cpuinfo"))
                        if (line.StartsWith("model name", StringComparison.Ordinal) || line.StartsWith("Model", StringComparison.Ordinal))
                            return line[(line.IndexOf(':') + 1)..].Trim();

                if (OperatingSystem.IsMacOS())
                {
                    using var sysctl = Process.Start(new ProcessStartInfo("sysctl", "-n machdep.cpu.brand_string") { RedirectStandardOutput = true });
                    if (sysctl is not null)
                    {
                        var name = sysctl.StandardOutput.ReadToEnd().Trim();
                        sysctl.WaitForExit();
                        return name.Length > 0 ? name : "unknown";
                    }
                }

            }
            catch
            { }

            return "unknown";

        }

        #endregion

        #region Header(Environment)

        public static String Header(JObject Environment)
        {

            var options  = (JObject) Environment["options"]!;
            var builds   = String.Join(", ", ((JArray) Environment["builds"]!).Select(build => $"{build["assembly"]} {build["configuration"]} {build["gitRepository"]}@{Short(build["gitCommit"]?.Value<String>())}"));

            return $"""
                    WWCP POI 3 load test
                      machine   {Environment["cpu"]}, {Environment["logicalProcessors"]} logical processors, {LoadRun.Bytes(Environment["memoryBytes"]!.Value<Int64>()).Trim()}, {Environment["os"]}, {Environment["architecture"]}
                      runtime   {Environment["runtime"]}, {(Environment["serverGC"]!.Value<Boolean>() ? "server" : "workstation")} GC, {Environment["gcLatencyMode"]}
                      builds    {builds}
                      network   {String.Join(", ", options["Stations"]!.Select(stations => $"{stations.Value<Int32>():N0}"))} charging stations of {options["Operators"]} operators (zipf {options["Zipf"]}), {options["StationsPerPool"]} per pool, {options["EVSEsPerStation"]} EVSEs each{(options["Templates"]!.Value<Boolean>() ? ", with templates" : "")}
                      commits   {options["Warmup"]} + {options["Commits"]} one after another and {options["Merges"]} merged, {options["ChangesPerCommit"]} change(s) each; writers {String.Join(",", options["Writers"]!)} for {options["Duration"]}, readers {String.Join(", ", ((JObject) options["Readers"]!).Properties().Select(reader => $"{reader.Name} {reader.Value}"))}, {options["StatusRate"]} status updates/s
                    """;

        }

        private static String Short(String? Commit)
            => Commit is null ? "unknown" : Commit.Length > 12 && !Commit.StartsWith("unknown") ? Commit[..10] + (Commit.EndsWith("-dirty") ? "-dirty" : "") : Commit;

        #endregion

        #region Write(Output, Name, Environment, Started, Runs)

        /// <summary>
        /// The reports of the given runs into the given directory, replacing those of the same name.
        /// </summary>
        public static (String JSON, String Markdown, String SeriesCSV, String ConcurrencyCSV) Write(String          Output,
                                                                                                   String          Name,
                                                                                                   JObject         Environment,
                                                                                                   DateTimeOffset  Started,
                                                                                                   JArray          Runs)
        {

            var json            = Path.Combine(Output, Name + ".json");
            var markdown        = Path.Combine(Output, Name + ".md");
            var seriesCSV       = Path.Combine(Output, Name + "-series.csv");
            var concurrencyCSV  = Path.Combine(Output, Name + "-concurrency.csv");

            File.WriteAllText(json, new JObject(new JProperty("name",         Name),
                                                new JProperty("started",      Started.ToString("o")),
                                                new JProperty("written",      DateTimeOffset.UtcNow.ToString("o")),
                                                new JProperty("environment",  Environment),
                                                new JProperty("runs",         Runs)).ToString(Formatting.Indented));

            var series       = Runs.OfType<JObject>().Select(SeriesRow).ToArray();
            var concurrency  = Runs.OfType<JObject>().SelectMany(ConcurrencyRows).ToArray();

            File.WriteAllText(seriesCSV,       CSV(SeriesColumns,      series));
            File.WriteAllText(concurrencyCSV,  CSV(ConcurrencyColumns, concurrency));

            var text = new StringBuilder();

            text.AppendLine($"# {Name}");
            text.AppendLine();
            text.AppendLine("```");
            text.AppendLine(Header(Environment));
            text.AppendLine("```");
            text.AppendLine();
            text.AppendLine("## Sizes");
            text.AppendLine();
            text.Append    (Table(SeriesColumns, series));
            text.AppendLine();
            text.AppendLine("## Concurrency");
            text.AppendLine();
            text.Append    (Table(ConcurrencyColumns, concurrency));

            var errors = Runs.OfType<JObject>().Where(run => run["error"] is not null || run["reload"]?["error"] is not null).ToArray();

            if (errors.Length > 0)
            {
                text.AppendLine();
                text.AppendLine("## Errors");
                text.AppendLine();
                foreach (var run in errors)
                    text.AppendLine($"- {run["stations"]} {run["store"]}: {run["error"] ?? run["reload"]?["error"]}");
            }

            File.WriteAllText(markdown, text.ToString());

            return (json, markdown, seriesCSV, concurrencyCSV);

        }

        #endregion


        #region (private) Columns and rows

        private static readonly String[] SeriesColumns = [
            "stations", "evses", "operators", "largest operator", "store",
            "entities s", "network s", "start s", "start CPU s", "managed after start MiB", "managed bytes/station", "peak working set MiB",
            "first proof s",
            "commit p50 ms", "commit p90 ms", "commit p99 ms", "commit max ms", "commit CPU p50 ms", "allocated/commit MiB", "gen2/commit", "commits/s",
            "merge commit p50 ms", "allocated/merge MiB",
            "managed after commits MiB",
            "base.json MiB", "compact s", "reload s", "reload managed MiB", "reload peak working set MiB", "reload first proof s"
        ];

        private static readonly String[] ConcurrencyColumns = [
            "stations", "store", "writers", "readers evse+proof+commit", "status/s target",
            "commits/s", "committed", "merges", "refused", "commit p50 ms", "commit p99 ms", "prepare p50 ms",
            "evse reads/s", "evse p99 ms", "proofs/s", "proof p50 ms", "proof p99 ms", "commit reads/s", "commit read max ms", "commit reads >10 ms", "commit read wait s",
            "status/s", "status p99 ms",
            "CPU %", "allocated MiB/s", "gen2", "gc pause ms", "peak working set MiB"
        ];

        private static String?[] SeriesRow(JObject Run)
        {

            var phases      = Run["phases"];
            var sequential  = phases?["sequential"];
            var stations    = Number(Run, "stations") ?? 0;

            return [
                Format(Run["stations"]),
                Format(Run["network"]?["evses"]),
                Format(Run["network"]?["operators"]),
                Format(Run["network"]?["largestOperator"]),
                Run["store"]?.Value<String>(),
                Seconds(phases?["entities"]?["wallMs"]),
                Seconds(phases?["network"]?["wallMs"]),
                Seconds(phases?["start"]?["wallMs"]),
                Seconds(phases?["start"]?["cpuMs"]),
                MiB(phases?["start"]?["memoryAfter"]?["managedBytes"]),
                stations > 0 && Number(phases?["start"]?["memoryAfter"]?["managedBytes"]) is Double managed ? Format(managed / stations, "N0") : null,
                MiB(Run["peakWorkingSetBytes"]),
                Seconds(phases?["firstProof"]?["coldMs"], "F2"),
                Format(sequential?["commit"]?["p50Ms"], "F1"),
                Format(sequential?["commit"]?["p90Ms"], "F1"),
                Format(sequential?["commit"]?["p99Ms"], "F1"),
                Format(sequential?["commit"]?["maxMs"], "F1"),
                Format(sequential?["cpuPerCommit"]?["p50Ms"], "F1"),
                MiB(sequential?["allocatedBytesPerCommit"], "F1"),
                Number(sequential?["gen2"]) is Double gen2 && Number(sequential?["commits"]) is Double commits && commits > 0 ? Format(gen2 / commits, "F2") : null,
                Format(sequential?["commitsPerSecond"], "F2"),
                Format(phases?["merge"]?["commit"]?["p50Ms"], "F1"),
                MiB(phases?["merge"]?["allocatedBytesPerCommit"], "F1"),
                MiB(sequential?["memoryAfter"]?["managedBytes"]),
                MiB(phases?["start"]?["disk"]?["baseBytes"]),
                Seconds(phases?["compact"]?["wallMs"]),
                Seconds(Run["reload"]?["load"]?["wallMs"]),
                MiB(Run["reload"]?["load"]?["memoryAfter"]?["managedBytes"]),
                MiB(Run["reload"]?["peakWorkingSetBytes"]),
                Seconds(Run["reload"]?["firstProof"]?["coldMs"], "F2")
            ];

        }

        private static IEnumerable<String?[]> ConcurrencyRows(JObject Run)
        {

            foreach (var level in (Run["phases"]?["concurrent"] as JArray ?? []).OfType<JObject>())
            {

                var seconds = (Number(level["resources"]?["wallMs"]) ?? 0) / 1000;

                yield return [
                    Format(Run["stations"]),
                    Run["store"]?.Value<String>(),
                    Format(level["writers"]),
                    level["readers"] is JObject readers ? String.Join("+", readers.Properties().Select(reader => reader.Value)) : null,
                    Format(level["statusRate"]),
                    level["skipped"] is not null ? "skipped" : Format(level["commits"]?["perSecond"], "F2"),
                    Format(level["commits"]?["outcomes"]?["Committed"] ?? 0),
                    Format(level["commits"]?["merges"]),
                    Format(level["commits"]?["outcomes"]?["Refused"] ?? 0),
                    Format(level["commit"]?["p50Ms"], "F1"),
                    Format(level["commit"]?["p99Ms"], "F1"),
                    Format(level["prepare"]?["p50Ms"], "F1"),
                    Format(level["reads"]?["evse"]?["perSecond"], "N0"),
                    Format(level["reads"]?["evse"]?["p99Ms"], "F2"),
                    Format(level["reads"]?["proof"]?["perSecond"], "N0"),
                    Format(level["reads"]?["proof"]?["p50Ms"], "F2"),
                    Format(level["reads"]?["proof"]?["p99Ms"], "F1"),
                    Format(level["reads"]?["commit"]?["perSecond"], "N0"),
                    Format(level["reads"]?["commit"]?["maxMs"], "F1"),
                    Format(level["reads"]?["commit"]?["over10msCount"]),
                    Seconds(level["reads"]?["commit"]?["over10msSumMs"], "F1"),
                    Format(level["status"]?["perSecond"], "N0"),
                    Format(level["status"]?["p99Ms"], "F2"),
                    Number(level["resources"]?["cpuUtilization"]) is Double cpu ? Format(cpu * 100, "F1") : null,
                    Number(level["resources"]?["allocatedBytes"]) is Double allocated && seconds > 0 ? Format(allocated / seconds / (1 << 20), "N0") : null,
                    Format(level["resources"]?["gen2"]),
                    Format(level["resources"]?["gcPauseMs"], "N0"),
                    MiB(level["peakWorkingSetBytes"])
                ];

            }

        }

        #endregion

        #region (private) Formats

        private static Double? Number(JToken? Token)
            => Token is JValue { Type: JTokenType.Integer or JTokenType.Float } value ? Convert.ToDouble(value.Value, CultureInfo.InvariantCulture) : null;

        private static Double? Number(JToken? Token, String Property)
            => Number(Token?[Property]);

        private static String? Format(JToken? Token, String Format = "N0")
            => Number(Token) is Double value ? value.ToString(Format, CultureInfo.InvariantCulture) : null;

        private static String Format(Double Value, String Format)
            => Value.ToString(Format, CultureInfo.InvariantCulture);

        private static String? Seconds(JToken? Milliseconds, String Format = "F1")
            => Number(Milliseconds) is Double value ? (value / 1000).ToString(Format, CultureInfo.InvariantCulture) : null;

        private static String? MiB(JToken? Bytes, String Format = "N0")
            => Number(Bytes) is Double value ? (value / (1 << 20)).ToString(Format, CultureInfo.InvariantCulture) : null;

        private static String Table(String[] Columns, IEnumerable<String?[]> Rows)
        {

            var text = new StringBuilder();

            text.AppendLine("| " + String.Join(" | ", Columns) + " |");
            text.AppendLine("|" + String.Concat(Columns.Select((_, index) => Columns[index] is "store" ? " --- |" : " ---: |")));

            foreach (var row in Rows)
                text.AppendLine("| " + String.Join(" | ", row.Select(cell => cell ?? "")) + " |");

            return text.ToString();

        }

        private static String CSV(String[] Columns, IEnumerable<String?[]> Rows)
        {

            static String Cell(String? Text)
                => Text is null ? "" : Text.Contains(',') || Text.Contains('"') ? "\"" + Text.Replace("\"", "\"\"") + "\"" : Text;

            var text = new StringBuilder();

            text.AppendLine(String.Join(",", Columns.Select(Cell)));

            // Without thousands separators, for spreadsheets.
            foreach (var row in Rows)
                text.AppendLine(String.Join(",", row.Select(cell => Cell(cell?.Replace(",", "")))));

            return text.ToString();

        }

        #endregion

    }

}
