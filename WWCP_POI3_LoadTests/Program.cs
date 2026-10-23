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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.loadtests
{

    /// <summary>
    /// Load tests of WWCP POI 3 histories: every size with every store in a process of its own, started by this one, so
    /// that one does not inherit the memory of another; a file store is read again by one more, as a hub starting.
    /// The reports go to the output directory as JSON, Markdown and CSV.
    /// </summary>
    public static class Program
    {

        public static Int32 Main(String[] Arguments)
        {

            if (!LoadTestOptions.TryParse(Arguments, out var options, out var error))
            {
                Console.Error.WriteLine(error);
                Console.Error.WriteLine();
                Console.Error.WriteLine(LoadTestOptions.Usage);
                return 2;
            }

            if (options.Help)
            {
                Console.WriteLine(LoadTestOptions.Usage);
                return 0;
            }

            if (options.Reload is not null)
                return Child(options, checkpoint => LoadRun.Reload(options));

            if (options.Child)
                return Child(options, checkpoint => {
                           using var run = new LoadRun(options);
                           return run.Run(checkpoint);
                       });

            return Parent(options, Arguments);

        }


        #region (private) Child(Options, Run)

        /// <summary>
        /// One size with one store, or a file store read again: its result into the result file of the options, also
        /// after each phase, so that the phases measured stay, should the process end.
        /// </summary>
        private static Int32 Child(LoadTestOptions                 Options,
                                   Func<Action<JObject>, JObject>  Run)
        {

            JObject? written = null;

            void Checkpoint(JObject Result)
            {
                written = Result;
                if (Options.ResultFile is not null)
                    File.WriteAllText(Options.ResultFile, Result.ToString(Formatting.Indented));
            }

            try
            {
                Checkpoint(Run(Checkpoint));
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                var result      = written ?? new JObject();
                result["error"] = $"{e.GetType().Name}: {e.Message}";
                Checkpoint(result);
                return 1;
            }

        }

        #endregion

        #region (private) Parent(Options, Arguments)

        private static Int32 Parent(LoadTestOptions  Options,
                                    String[]         Arguments)
        {

            var environment  = LoadTestReport.Environment(Options);
            var started      = DateTimeOffset.UtcNow;
            var name         = $"loadtest{(Options.Label is not null ? "-" + Options.Label : "")}-{started:yyyyMMdd-HHmmss}";
            var runs         = new JArray();
            var scratch      = Path.Combine(Options.Directory, name);

            Directory.CreateDirectory(Options.Output);
            Directory.CreateDirectory(scratch);

            Console.WriteLine(LoadTestReport.Header(environment));

            if (environment["warnings"] is JArray { Count: > 0 } warnings)
                foreach (var warning in warnings)
                    Console.WriteLine($"warning: {warning}");

            foreach (var stations in Options.Stations)
            {
                foreach (var store in Options.Stores)
                {

                    var storeDirectory  = Path.Combine(scratch, $"{stations}-{LoadRun.Name(store)}");
                    var resultFile      = Path.Combine(scratch, $"{stations}-{LoadRun.Name(store)}.json");

                    var run             = RunChild(Arguments, [ "--child",
                                                                "--stations",         stations.ToString(),
                                                                "--stores",           LoadRun.Name(store),
                                                                "--store-directory",  storeDirectory,
                                                                "--result",           resultFile ],
                                                   resultFile);

                    run["stations"]     = stations;
                    run["store"]        = LoadRun.Name(store);

                    if (store == LoadTestStore.File && Directory.Exists(storeDirectory))
                    {

                        if (run["error"] is null)
                        {

                            var reloadFile  = Path.Combine(scratch, $"{stations}-reload.json");
                            var reload      = RunChild(Arguments, [ "--reload", storeDirectory, "--result", reloadFile ], reloadFile);

                            reload["sameHead"] = JToken.DeepEquals(reload["head"], run["head"]);
                            run["reload"]      = reload;

                        }

                        if (!Options.Keep)
                            Directory.Delete(storeDirectory, true);

                    }

                    runs.Add(run);

                    // Written after every run: what was measured stays, should a larger size fail.
                    LoadTestReport.Write(Options.Output, name, environment, started, runs);

                }
            }

            if (!Options.Keep)
                Directory.Delete(scratch, true);

            var files = LoadTestReport.Write(Options.Output, name, environment, started, runs);

            Console.WriteLine();
            Console.WriteLine(File.ReadAllText(files.Markdown));
            Console.WriteLine($"Reports: {files.JSON}, {files.Markdown}, {files.SeriesCSV}, {files.ConcurrencyCSV}");

            return runs.Any(run => run["error"] is not null) ? 1 : 0;

        }

        #endregion

        #region (private) RunChild(Arguments, More, ResultFile)

        /// <summary>
        /// This program once more, with the arguments it was given and the given ones, which count as given last.
        /// </summary>
        private static JObject RunChild(String[]               Arguments,
                                        IEnumerable<String>    More,
                                        String                 ResultFile)
        {

            var processPath  = Environment.ProcessPath ?? throw new InvalidOperationException("The path of this program is unknown!");
            var startInfo    = new ProcessStartInfo(processPath) { UseShellExecute = false };

            // Started as "dotnet WWCP_POI3_LoadTests.dll": the dll is the first argument.
            if (Path.GetFileNameWithoutExtension(processPath).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                startInfo.ArgumentList.Add(typeof(Program).Assembly.Location);

            foreach (var argument in Arguments.Concat(More))
                startInfo.ArgumentList.Add(argument);

            var stopwatch = Stopwatch.StartNew();

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("The child process did not start!");

            process.WaitForExit();

            var result = File.Exists(ResultFile)
                             ? JObject.Parse(File.ReadAllText(ResultFile))
                             : new JObject();

            // Ended without saying why, e.g. out of memory: the phases written before stay.
            if (process.ExitCode != 0 && result["error"] is null)
                result["error"] = $"The child process ended with exit code {process.ExitCode}, after the phases it wrote.";

            result["exitCode"]       = process.ExitCode;
            result["processWallMs"]  = Math.Round(stopwatch.Elapsed.TotalMilliseconds, 3);

            return result;

        }

        #endregion

    }

}
