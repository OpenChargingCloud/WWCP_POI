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

using System.Text;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A store in a directory: the base and the log of one generation, base.json and log.jsonl
    /// with one JSON log entry per line, in a directory gen-NNNNNN named by the file CURRENT.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Commits and signatures are synced to the disk before the history shows them;
    ///         status updates are synced in batches, every StatusFlushInterval, and with the next commit.</item>
    ///   <item>A last line written only partly, e.g. by a crash, is dropped when loading; an invalid line before it is an error.</item>
    ///   <item>Compacting writes a new generation completely, then replaces CURRENT, then deletes the old one:
    ///         after a crash, CURRENT names a complete generation.</item>
    ///   <item>The file LOCK, held open exclusively, keeps a second store, e.g. of another process, from the directory.</item>
    /// </list>
    /// </remarks>
    public sealed class RoamingNetworkHistoryFileStore : IRoamingNetworkHistoryStore
    {

        #region Data

        /// <summary>
        /// The name of the file naming the current generation.
        /// </summary>
        public const String  CurrentFileName  = "CURRENT";

        /// <summary>
        /// The name of the file of the base of a generation.
        /// </summary>
        public const String  BaseFileName     = "base.json";

        /// <summary>
        /// The name of the file of the log of a generation.
        /// </summary>
        public const String  LogFileName      = "log.jsonl";

        private static readonly UTF8Encoding  utf8  = new (encoderShouldEmitUTF8Identifier: false);

        private readonly Lock         storeLock  = new ();
        private readonly FileStream   lockFile;
        private readonly Timer        flushTimer;
        private          String?      generation;
        private          FileStream?  log;
        private          Boolean      unsynced;
        private          Boolean      disposed;

        #endregion

        #region Properties

        /// <summary>
        /// The directory of this store.
        /// </summary>
        public String    Directory              { get; }

        /// <summary>
        /// How long status updates may wait before they are synced to the disk.
        /// </summary>
        public TimeSpan  StatusFlushInterval    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Open the store in the given directory, creating it if needed.
        /// </summary>
        /// <param name="Directory">The directory, one per roaming network.</param>
        /// <param name="StatusFlushInterval">How long status updates may wait before they are synced to the disk; 1 second by default.</param>
        public RoamingNetworkHistoryFileStore(String     Directory,
                                              TimeSpan?  StatusFlushInterval   = null)
        {

            this.Directory            = Path.GetFullPath(Directory);
            this.StatusFlushInterval  = StatusFlushInterval ?? TimeSpan.FromSeconds(1);

            System.IO.Directory.CreateDirectory(this.Directory);

            try
            {
                lockFile = new FileStream(Path.Combine(this.Directory, "LOCK"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException e)
            {
                throw new InvalidOperationException($"The history store '{this.Directory}' is used by another store!", e);
            }

            flushTimer = new Timer(_ => SyncStatus(), null, this.StatusFlushInterval, this.StatusFlushInterval);

        }

        #endregion


        #region Load()

        /// <inheritdoc/>
        public RoamingNetworkHistoryStoreContent? Load()
        {

            lock (storeLock)
            {

                var currentFile = Path.Combine(Directory, CurrentFileName);

                if (!File.Exists(currentFile))
                    return null;

                // Read with the log closed; it is opened again for appending afterwards.
                log?.Flush(true);
                log?.Dispose();
                log      = null;
                unsynced = false;

                generation = File.ReadAllText(currentFile, utf8).Trim();

                var baseJSON  = ParseLine(File.ReadAllText(Path.Combine(Directory, generation, BaseFileName), utf8));
                var logFile   = Path.Combine(Directory, generation, LogFileName);
                var entries   = new List<RoamingNetworkHistoryLogEntry>();

                var bytes     = File.Exists(logFile) ? File.ReadAllBytes(logFile) : [];
                var start     = 0;
                var lineNo    = 0;

                while (start < bytes.Length)
                {

                    var end       = Array.IndexOf(bytes, (Byte) '\n', start);
                    var complete  = end >= 0;
                    var line      = utf8.GetString(bytes, start, (complete ? end : bytes.Length) - start);

                    lineNo++;

                    if (line.Trim().Length > 0)
                    {

                        RoamingNetworkHistoryLogEntry? entry = null;
                        String?                        error = null;

                        try
                        {
                            RoamingNetworkHistoryLogEntry.TryParse(ParseLine(line), out entry, out error);
                        }
                        catch (Exception e)
                        {
                            error = e.Message;
                        }

                        if (entry is null)
                        {

                            // Written only partly, e.g. by a crash: dropped, and the log cut there.
                            if (!complete || bytes.Skip(end + 1).All(b => b is (Byte) '\n' or (Byte) '\r' or (Byte) ' '))
                            {
                                using var cut = new FileStream(logFile, FileMode.Open, FileAccess.Write, FileShare.None);
                                cut.SetLength(start);
                                cut.Flush(true);
                                break;
                            }

                            throw new InvalidOperationException($"The log of the history store '{Directory}' is invalid in line {lineNo}: {error}");

                        }

                        entries.Add(entry);

                    }

                    if (!complete)
                        break;

                    start = end + 1;

                }

                OpenLog();

                return new (baseJSON, entries);

            }

        }

        #endregion

        #region WriteBase(Snapshot)

        /// <inheritdoc/>
        public void WriteBase(JObject Snapshot)
        {

            lock (storeLock)
            {

                if (File.Exists(Path.Combine(Directory, CurrentFileName)))
                    throw new InvalidOperationException($"The history store '{Directory}' is not empty!");

                WriteGeneration("gen-000001", Snapshot, []);
                OpenLog();

            }

        }

        #endregion

        #region Append(Entry)

        /// <inheritdoc/>
        public void Append(RoamingNetworkHistoryLogEntry Entry)
        {

            var bytes = utf8.GetBytes(Entry.ToJSON().ToString(Formatting.None) + "\n");

            lock (storeLock)
            {

                if (log is null)
                    throw new InvalidOperationException($"The history store '{Directory}' has no base yet!");

                log.Write(bytes);

                if (Entry.Type == RoamingNetworkHistoryLogEntryType.Status)
                {
                    log.Flush(false);
                    unsynced = true;
                }
                else
                {
                    log.Flush(true);
                    unsynced = false;
                }

            }

        }

        #endregion

        #region Compact(Snapshot, Entries)

        /// <inheritdoc/>
        public void Compact(JObject                                     Snapshot,
                            IEnumerable<RoamingNetworkHistoryLogEntry>  Entries)
        {

            lock (storeLock)
            {

                var oldGeneration  = generation;
                var number         = oldGeneration is not null && oldGeneration.StartsWith("gen-") && Int32.TryParse(oldGeneration[4..], out var n) ? n + 1 : 1;
                var newGeneration  = $"gen-{number:D6}";

                log?.Flush(true);
                log?.Dispose();
                log = null;

                try
                {
                    WriteGeneration(newGeneration, Snapshot, Entries);
                }
                finally
                {
                    OpenLog();
                }

                if (oldGeneration is not null && oldGeneration != newGeneration)
                {
                    try
                    {
                        System.IO.Directory.Delete(Path.Combine(Directory, oldGeneration), recursive: true);
                    }
                    catch
                    { }
                }

            }

        }

        #endregion

        #region (internal static) ParseLine(Text)

        /// <summary>
        /// Read JSON text as written: dates stay text, numbers with a fraction decimals, so that what is signed stays as it was.
        /// </summary>
        internal static JObject ParseLine(String Text)
        {

            using var reader = new JsonTextReader(new StringReader(Text)) {
                                   DateParseHandling   = DateParseHandling.None,
                                   FloatParseHandling  = FloatParseHandling.Decimal
                               };

            return JToken.ReadFrom(reader) as JObject
                       ?? throw new InvalidOperationException("A JSON object was expected!");

        }

        #endregion


        #region (private) WriteGeneration(Generation, Snapshot, Entries)

        /// <summary>
        /// Write the given generation completely and synced, then name it in CURRENT.
        /// </summary>
        private void WriteGeneration(String                                      Generation,
                                     JObject                                     Snapshot,
                                     IEnumerable<RoamingNetworkHistoryLogEntry>  Entries)
        {

            var directory = Path.Combine(Directory, Generation);

            if (System.IO.Directory.Exists(directory))
                System.IO.Directory.Delete(directory, recursive: true);

            System.IO.Directory.CreateDirectory(directory);

            WriteSynced(Path.Combine(directory, BaseFileName), utf8.GetBytes(Snapshot.ToString(Formatting.None)));

            using (var logStream = new FileStream(Path.Combine(directory, LogFileName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {

                foreach (var entry in Entries)
                    logStream.Write(utf8.GetBytes(entry.ToJSON().ToString(Formatting.None) + "\n"));

                logStream.Flush(true);

            }

            var currentTemp = Path.Combine(Directory, CurrentFileName + ".tmp");
            WriteSynced(currentTemp, utf8.GetBytes(Generation));
            File.Move(currentTemp, Path.Combine(Directory, CurrentFileName), overwrite: true);

            generation = Generation;

        }

        #endregion

        #region (private static) WriteSynced(File, Bytes)

        private static void WriteSynced(String  FileName,
                                        Byte[]  Bytes)
        {
            using var stream = new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.None);
            stream.Write(Bytes);
            stream.Flush(true);
        }

        #endregion

        #region (private) OpenLog()

        private void OpenLog()
        {

            if (log is not null || generation is null)
                return;

            log = new FileStream(Path.Combine(Directory, generation, LogFileName), FileMode.Append, FileAccess.Write, FileShare.Read);

        }

        #endregion

        #region (private) SyncStatus()

        private void SyncStatus()
        {

            lock (storeLock)
            {

                if (!unsynced || log is null || disposed)
                    return;

                try
                {
                    log.Flush(true);
                    unsynced = false;
                }
                catch
                { }

            }

        }

        #endregion


        #region Dispose()

        /// <inheritdoc/>
        public void Dispose()
        {

            lock (storeLock)
            {

                if (disposed)
                    return;

                disposed = true;

                flushTimer.Dispose();

                try
                {
                    log?.Flush(true);
                }
                catch
                { }

                log?.Dispose();
                log = null;

                lockFile.Dispose();

            }

        }

        #endregion

    }

}
