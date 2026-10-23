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

        /// <summary>
        /// The size of the buffer the log is read with: a line may be longer.
        /// </summary>
        private const Int32  ReadBufferSize   = 64 * 1024;

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

                // Read as a stream: the base of a large roaming network is longer than a string may be.
                JObject baseJSON;

                using (var baseStream = new FileStream(Path.Combine(Directory, generation, BaseFileName), FileMode.Open, FileAccess.Read, FileShare.Read, ReadBufferSize))
                {
                    baseJSON = POIJSON.Read(baseStream) as JObject
                                   ?? throw new InvalidOperationException($"The base of the history store '{Directory}' is no JSON object!");
                }

                var logFile   = Path.Combine(Directory, generation, LogFileName);
                var entries   = new List<RoamingNetworkHistoryLogEntry>();

                Int64? cutAt  = null;

                // Read line by line: the log grows with every entry, also beyond the 2 GiB File.ReadAllBytes reads.
                if (File.Exists(logFile))
                {

                    using var stream  = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.Read, ReadBufferSize);
                    using var lines   = ReadLines(stream).GetEnumerator();
                    var lineNo        = 0;

                    while (lines.MoveNext())
                    {

                        var (start, bytes, complete) = lines.Current;
                        var line = utf8.GetString(bytes);

                        lineNo++;

                        if (line.Trim().Length == 0)
                            continue;

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
                            if (!complete || OnlyBlankLinesFollow(lines))
                            {
                                cutAt = start;
                                break;
                            }

                            throw new InvalidOperationException($"The log of the history store '{Directory}' is invalid in line {lineNo}: {error}");

                        }

                        entries.Add(entry);

                    }

                }

                if (cutAt.HasValue)
                {
                    using var cut = new FileStream(logFile, FileMode.Open, FileAccess.Write, FileShare.None);
                    cut.SetLength(cutAt.Value);
                    cut.Flush(true);
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

            var bytes = utf8.GetBytes(POIJSON.ToText(Entry.ToJSON(Embedded: true)) + "\n");

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

        #region (private static) ReadLines(Stream)

        /// <summary>
        /// The lines of the given stream, without their '\n', each with the position it starts at, and whether it ends
        /// with a '\n': only the last one may not, e.g. when it was written only partly.
        /// </summary>
        private static IEnumerable<(Int64 Start, Byte[] Line, Boolean Complete)> ReadLines(Stream Stream)
        {

            var buffer    = new Byte[ReadBufferSize];
            var line      = new MemoryStream();
            var start     = 0L;
            var position  = 0L;
            Int32 read;

            while ((read = Stream.Read(buffer, 0, buffer.Length)) > 0)
            {

                var offset = 0;

                while (offset < read)
                {

                    var end = Array.IndexOf(buffer, (Byte) '\n', offset, read - offset);

                    if (end < 0)
                    {
                        line.Write(buffer, offset, read - offset);
                        position += read - offset;
                        break;
                    }

                    line.Write(buffer, offset, end - offset);
                    position += end - offset + 1;

                    yield return (start, line.ToArray(), true);

                    line.SetLength(0);
                    start   = position;
                    offset  = end + 1;

                }

            }

            if (line.Length > 0)
                yield return (start, line.ToArray(), false);

        }

        #endregion

        #region (private static) OnlyBlankLinesFollow(Lines)

        /// <summary>
        /// Whether the lines still to come hold nothing but spaces and carriage returns.
        /// </summary>
        private static Boolean OnlyBlankLinesFollow(IEnumerator<(Int64 Start, Byte[] Line, Boolean Complete)> Lines)
        {

            while (Lines.MoveNext())
                if (Lines.Current.Line.Any(b => b is not ((Byte) '\r' or (Byte) ' ')))
                    return false;

            return true;

        }

        #endregion

        #region (internal static) ParseLine(Text)

        /// <summary>
        /// Read JSON text as written: dates stay text, numbers with a fraction decimals, so that what is signed stays as it was.
        /// </summary>
        internal static JObject ParseLine(String Text)
        {

            return POIJSON.Read(Text) as JObject
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

            // Written as a stream: the base of a large roaming network is longer than a string may be.
            using (var baseStream = new FileStream(Path.Combine(directory, BaseFileName), FileMode.Create, FileAccess.Write, FileShare.None, 1 << 20))
            {
                POIJSON.Write(baseStream, Snapshot);
                baseStream.Flush(true);
            }

            using (var logStream = new FileStream(Path.Combine(directory, LogFileName), FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {

                foreach (var entry in Entries)
                    logStream.Write(utf8.GetBytes(POIJSON.ToText(entry.ToJSON(Embedded: true)) + "\n"));

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
