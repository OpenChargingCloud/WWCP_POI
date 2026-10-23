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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.loadtests
{

    /// <summary>
    /// Where a load test keeps the history of its roaming network: in memory only, or in a file store, as the hub does.
    /// </summary>
    public enum LoadTestStore
    {
        Memory,
        File
    }

    /// <summary>
    /// The kinds of changes the writers of a load test make.
    /// </summary>
    public enum LoadTestChange
    {

        /// <summary>
        /// A property of an EVSE.
        /// </summary>
        UpdateEVSE,

        /// <summary>
        /// A new charging station with its EVSEs in a charging pool there is.
        /// </summary>
        AddStation,

        /// <summary>
        /// A charging station removed with its EVSEs.
        /// </summary>
        RemoveStation,

        /// <summary>
        /// A new charging pool with its charging stations and EVSEs.
        /// </summary>
        AddPool

    }

    /// <summary>
    /// The kinds of reads the readers of a load test make.
    /// </summary>
    public enum LoadTestRead
    {

        /// <summary>
        /// An EVSE of the head, as JSON.
        /// </summary>
        EVSE,

        /// <summary>
        /// The proof of a charging station of the head, as JSON: it takes the attestations of the head under the lock of
        /// the history, which every commit holds as long as it takes.
        /// </summary>
        Proof,

        /// <summary>
        /// The commit of the head by its identification, under the lock of the history: how long a reader waits behind
        /// a commit, and nothing else.
        /// </summary>
        Commit

    }


    /// <summary>
    /// What a load test does, read from its command line. An option given twice counts as given last.
    /// </summary>
    public sealed class LoadTestOptions
    {

        #region Usage

        public const String Usage = """
            WWCP_POI3_LoadTests: load tests of WWCP POI 3 histories, one process for each size and store.

            Sizes
              --suite quick|standard|full    10k | 10k,50k,100k | 10k,50k,100k,250k,1M charging stations (standard)
              --stations LIST                charging stations, e.g. 10k,50k,1M (overrides --suite)
              --operators N                  charging station operators (200)
              --zipf S                       their sizes: the i-th has a share of 1/i^S, 0 = all alike (1.0)
              --stations-per-pool N          charging stations of a charging pool (2)
              --evses-per-station N          EVSEs of a charging station (2)
              --templates                    EVSEs, stations and pools take properties from templates of their operator

            Stores
              --stores LIST                  memory,file (both); the file store as the hub keeps it, with its archive
              --directory DIR                where the file stores go (the temp directory), each removed afterwards
              --keep                         keep the file stores
              --recent-versions N            versions of the latest commits kept (100, as the hub)
              --version-interval N           the version of every N-th revision kept (100, as the hub)
              --no-attest                    do not attest every head, which the hub does

            Commits one after another
              --warmup N                     commits not measured first (5)
              --commits N                    commits measured (50)
              --changes-per-commit N         changes of one change set, all of one operator (1)
              --mix LIST                     update=85,addStation=7,removeStation=4,addPool=4
              --merges N                     commits made for the version before the head, so each is merged (3)

            Concurrency
              --writers LIST                 writers at the same time, one level after another (0,1,2,4,8,16);
                                             every writer changes the charging stations of its own operators,
                                             0 measures the readers and status updates alone
              --readers LIST                 reader threads of each kind at the same time as the writers
                                             (evse=3,proof=1,commit=1): an EVSE of the head as JSON, the proof of a
                                             charging station, or the commit of the head, both under the lock of the
                                             history, which a commit holds as long as it takes
              --status-rate N                EVSE status updates per second at the same time (1000), 0 = none
              --duration T                   how long each level runs, e.g. 20s or 2m (20s)
              --max-merge T                  levels of several writers are skipped where a merge took longer (60s):
                                             their commits would be merged one after another

            Output
              --output DIR                   where the reports go (results)
              --label TEXT                   in the name of the reports, e.g. the machine
              --seed N                       of the random choices (1)
              -h, --help
            """;

        #endregion

        #region Properties

        public IReadOnlyList<Int32>                              Stations            { get; private set; } = [ 10_000, 50_000, 100_000 ];
        public Int32                                             Operators           { get; private set; } = 200;
        public Double                                            Zipf                { get; private set; } = 1.0;
        public Int32                                             StationsPerPool     { get; private set; } = 2;
        public Int32                                             EVSEsPerStation     { get; private set; } = 2;
        public Boolean                                           Templates           { get; private set; }

        public IReadOnlyList<LoadTestStore>                      Stores              { get; private set; } = [ LoadTestStore.Memory, LoadTestStore.File ];
        public String                                            Directory           { get; private set; } = Path.Combine(Path.GetTempPath(), "wwcp-poi3-loadtests");
        public Boolean                                           Keep                { get; private set; }
        public Int32                                             RecentVersions      { get; private set; } = new RoamingNetworkHistoryRetention().RecentVersions;
        public UInt64                                            VersionInterval     { get; private set; } = new RoamingNetworkHistoryRetention().VersionCheckpointInterval;
        public Boolean                                           Attest              { get; private set; } = true;

        public Int32                                             Warmup              { get; private set; } = 5;
        public Int32                                             Commits             { get; private set; } = 50;
        public Int32                                             ChangesPerCommit    { get; private set; } = 1;
        public IReadOnlyDictionary<LoadTestChange, Int32>        Mix                 { get; private set; } = DefaultMix;
        public Int32                                             Merges              { get; private set; } = 3;

        public IReadOnlyList<Int32>                              Writers             { get; private set; } = [ 0, 1, 2, 4, 8, 16 ];
        public IReadOnlyDictionary<LoadTestRead, Int32>          Readers             { get; private set; } = DefaultReaders;
        public Int32                                             StatusRate          { get; private set; } = 1000;
        public TimeSpan                                          Duration            { get; private set; } = TimeSpan.FromSeconds(20);
        public TimeSpan                                          MaxMerge            { get; private set; } = TimeSpan.FromSeconds(60);

        public String                                            Output              { get; private set; } = "results";
        public String?                                           Label               { get; private set; }
        public Int32                                             Seed                { get; private set; } = 1;
        public Boolean                                           Help                { get; private set; }

        /// <summary>
        /// Run as the child of a load test: one size, one store, the result into ResultFile.
        /// </summary>
        public Boolean                                           Child               { get; private set; }

        /// <summary>
        /// Where a child writes its result.
        /// </summary>
        public String?                                           ResultFile          { get; private set; }

        /// <summary>
        /// The directory of the file store of a child, which must not be there yet.
        /// </summary>
        public String?                                           StoreDirectory      { get; private set; }

        /// <summary>
        /// Run as the child reading the file store in this directory again, as the hub does when it starts.
        /// </summary>
        public String?                                           Reload              { get; private set; }

        #endregion

        #region Defaults

        public static IReadOnlyDictionary<LoadTestChange, Int32> DefaultMix { get; }

            = new Dictionary<LoadTestChange, Int32> {
                  { LoadTestChange.UpdateEVSE,    85 },
                  { LoadTestChange.AddStation,     7 },
                  { LoadTestChange.RemoveStation,  4 },
                  { LoadTestChange.AddPool,        4 }
              };

        public static IReadOnlyDictionary<LoadTestRead, Int32> DefaultReaders { get; }

            = new Dictionary<LoadTestRead, Int32> {
                  { LoadTestRead.EVSE,    3 },
                  { LoadTestRead.Proof,   1 },
                  { LoadTestRead.Commit,  1 }
              };

        #endregion


        #region TryParse(Arguments, out Options, out Error)

        /// <summary>
        /// The options of the given command line, or what is wrong with it.
        /// </summary>
        public static Boolean TryParse(IReadOnlyList<String>  Arguments,
                                       out LoadTestOptions    Options,
                                       out String?            Error)
        {

            Options  = new LoadTestOptions();
            Error    = null;

            try
            {

                for (var i = 0; i < Arguments.Count; i++)
                {

                    var name = Arguments[i];

                    String Value()
                        => i + 1 < Arguments.Count
                               ? Arguments[++i]
                               : throw new FormatException($"The option '{name}' needs a value!");

                    switch (name)
                    {

                        case "--suite":
                            Options.Stations = Value() switch {
                                                   "quick"     => [ 10_000 ],
                                                   "standard"  => [ 10_000, 50_000, 100_000 ],
                                                   "full"      => [ 10_000, 50_000, 100_000, 250_000, 1_000_000 ],
                                                   var other   => throw new FormatException($"There is no suite '{other}'!")
                                               };
                            break;

                        case "--stations":            Options.Stations          = [ .. List(Value(), Count) ];                          break;
                        case "--operators":           Options.Operators         = Count(Value());                                       break;
                        case "--zipf":                Options.Zipf              = Number(Value());                                      break;
                        case "--stations-per-pool":   Options.StationsPerPool   = Count(Value());                                       break;
                        case "--evses-per-station":   Options.EVSEsPerStation   = Count(Value());                                       break;
                        case "--templates":           Options.Templates         = true;                                                 break;

                        case "--stores":              Options.Stores            = [ .. List(Value(), Enum<LoadTestStore>).Distinct() ]; break;
                        case "--directory":           Options.Directory         = Path.GetFullPath(Value());                            break;
                        case "--keep":                Options.Keep              = true;                                                 break;
                        case "--recent-versions":     Options.RecentVersions    = Count(Value());                                       break;
                        case "--version-interval":    Options.VersionInterval   = (UInt64) Count(Value());                              break;
                        case "--no-attest":           Options.Attest            = false;                                                break;

                        case "--warmup":              Options.Warmup            = Count(Value(), Zero: true);                           break;
                        case "--commits":             Options.Commits           = Count(Value());                                       break;
                        case "--changes-per-commit":  Options.ChangesPerCommit  = Count(Value());                                       break;
                        case "--mix":                 Options.Mix               = Shares<LoadTestChange>(Value(), MixNames);            break;
                        case "--merges":              Options.Merges            = Count(Value(), Zero: true);                           break;

                        case "--writers":             Options.Writers           = [ .. List(Value(), value => Count(value, Zero: true)) ]; break;
                        case "--readers":             Options.Readers           = Shares<LoadTestRead>(Value(), ReaderNames, Zero: true); break;
                        case "--status-rate":         Options.StatusRate        = Count(Value(), Zero: true);                           break;
                        case "--duration":            Options.Duration          = TimeSpanOf(Value());                                  break;
                        case "--max-merge":           Options.MaxMerge          = TimeSpanOf(Value());                                  break;

                        case "--output":              Options.Output            = Value();                                              break;
                        case "--label":               Options.Label             = Value();                                              break;
                        case "--seed":                Options.Seed              = Count(Value(), Zero: true);                           break;
                        case "-h" or "--help":        Options.Help              = true;                                                 break;

                        case "--child":               Options.Child             = true;                                                 break;
                        case "--result":              Options.ResultFile        = Value();                                              break;
                        case "--store-directory":     Options.StoreDirectory    = Value();                                              break;
                        case "--reload":              Options.Reload            = Value();                                              break;

                        default:
                            throw new FormatException($"There is no option '{name}'!");

                    }

                }

                if (Options.Stations.Count == 0)
                    throw new FormatException("There must be at least one size!");

                if (Options.Stores.Count == 0)
                    throw new FormatException("There must be at least one store!");

                if (Options.Writers.Count == 0)
                    throw new FormatException("There must be at least one level of writers, 0 for readers only!");

                if (Options.Label is not null && Options.Label.Any(character => !Char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_' and not '.'))
                    throw new FormatException("A label may only have letters, digits, '-', '_' and '.', as it goes into the names of files!");

                return true;

            }
            catch (FormatException e)
            {
                Error = e.Message;
                return false;
            }

        }

        #endregion


        #region (private) Parsers

        private static readonly IReadOnlyDictionary<String, LoadTestChange> MixNames = new Dictionary<String, LoadTestChange>(StringComparer.OrdinalIgnoreCase) {
                                                                                           { "update",         LoadTestChange.UpdateEVSE    },
                                                                                           { "addStation",     LoadTestChange.AddStation    },
                                                                                           { "removeStation",  LoadTestChange.RemoveStation },
                                                                                           { "addPool",        LoadTestChange.AddPool       }
                                                                                       };

        private static readonly IReadOnlyDictionary<String, LoadTestRead>   ReaderNames  = new Dictionary<String, LoadTestRead>(StringComparer.OrdinalIgnoreCase) {
                                                                                           { "evse",   LoadTestRead.EVSE  },
                                                                                           { "proof",  LoadTestRead.Proof },
                                                                                           { "commit", LoadTestRead.Commit }
                                                                                       };

        private static IEnumerable<T> List<T>(String Text, Func<String, T> Parse)

            => Text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Select(Parse);

        /// <summary>
        /// A count, e.g. 250000, 250_000, 250k or 1M.
        /// </summary>
        private static Int32 Count(String Text)
            => Count(Text, false);

        private static Int32 Count(String Text, Boolean Zero)
        {

            var text        = Text.Replace("_", "");
            var multiplier  = 1L;

            if (text.EndsWith('k') || text.EndsWith('K')) { multiplier = 1_000;     text = text[..^1]; }
            if (text.EndsWith('m') || text.EndsWith('M')) { multiplier = 1_000_000; text = text[..^1]; }

            if (!Int64.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ||
                value * multiplier > Int32.MaxValue ||
                (!Zero && value == 0))
            {
                throw new FormatException($"'{Text}' is no count{(Zero ? "" : " above 0")}!");
            }

            return (Int32) (value * multiplier);

        }

        private static Double Number(String Text)

            => Double.TryParse(Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0 && Double.IsFinite(value)
                   ? value
                   : throw new FormatException($"'{Text}' is no number of 0 or more!");

        private static TimeSpan TimeSpanOf(String Text)
        {

            var unit = Text.TrimStart("0123456789.".ToCharArray());

            if (!Double.TryParse(Text[..^unit.Length], NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value <= 0)
                throw new FormatException($"'{Text}' is no duration, e.g. 20s or 2m!");

            return unit switch {
                       "" or "s"  => TimeSpan.FromSeconds(value),
                       "m"        => TimeSpan.FromMinutes(value),
                       "ms"       => TimeSpan.FromMilliseconds(value),
                       _          => throw new FormatException($"'{Text}' is no duration, e.g. 20s or 2m!")
                   };

        }

        private static T Enum<T>(String Text) where T : struct, System.Enum

            => System.Enum.TryParse<T>(Text, true, out var value) && System.Enum.IsDefined(value)
                   ? value
                   : throw new FormatException($"'{Text}' is none of {String.Join(", ", System.Enum.GetNames<T>().Select(name => name.ToLowerInvariant()))}!");

        /// <summary>
        /// Shares like update=85,addStation=7: what is not named has none. All of them none only where Zero allows it.
        /// </summary>
        private static IReadOnlyDictionary<T, Int32> Shares<T>(String                         Text,
                                                               IReadOnlyDictionary<String, T>  Names,
                                                               Boolean                         Zero   = false) where T : notnull
        {

            var shares = new Dictionary<T, Int32>();

            foreach (var share in List(Text, share => share.Split('=', 2, StringSplitOptions.TrimEntries)))
            {

                if (share.Length != 2 || !Names.TryGetValue(share[0], out var kind))
                    throw new FormatException($"'{String.Join("=", share)}' is none of {String.Join(", ", Names.Keys.Select(key => key + "=N"))}!");

                shares[kind] = Count(share[1], Zero: true);

            }

            if (!Zero && shares.Values.Sum() == 0)
                throw new FormatException($"'{Text}' gives nothing a share!");

            return shares;

        }

        #endregion

    }

}
