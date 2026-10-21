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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An archive in a directory: segment-NNNNNN.base.json and segment-NNNNNN.commits.jsonl per segment,
    /// one commit per line, and segments.jsonl naming every segment, one line each, in their order.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>A segment is written completely and synced, its files first, then its line in segments.jsonl:
    ///         a segment there is complete, files without a line, e.g. after a crash, are written again.</item>
    ///   <item>A segment of a number written before replaces it: the later line in segments.jsonl wins.</item>
    ///   <item>A last line of segments.jsonl written only partly is dropped when opening.</item>
    ///   <item>The file LOCK, held open exclusively, keeps a second archive from the directory.</item>
    /// </list>
    /// </remarks>
    public sealed class RoamingNetworkHistoryFileArchive : IRoamingNetworkHistoryArchive
    {

        #region Data

        /// <summary>
        /// The name of the file naming every segment.
        /// </summary>
        public const String  SegmentsFileName  = "segments.jsonl";

        private static readonly UTF8Encoding  utf8  = new (encoderShouldEmitUTF8Identifier: false);

        private readonly Lock                                                     archiveLock  = new ();
        private readonly FileStream                                               lockFile;
        private readonly SortedDictionary<UInt32, RoamingNetworkArchiveSegmentInfo>  segments  = [];

        #endregion

        #region Properties

        /// <summary>
        /// The directory of this archive.
        /// </summary>
        public String Directory { get; }

        /// <inheritdoc/>
        public IReadOnlyList<RoamingNetworkArchiveSegmentInfo> Segments
        {
            get
            {
                lock (archiveLock)
                {
                    return [ .. segments.Values ];
                }
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Open the archive in the given directory, creating it if needed, e.g. "data/rn1/archive".
        /// </summary>
        /// <param name="Directory">The directory, one per roaming network.</param>
        public RoamingNetworkHistoryFileArchive(String Directory)
        {

            this.Directory = Path.GetFullPath(Directory);

            System.IO.Directory.CreateDirectory(this.Directory);

            try
            {
                lockFile = new FileStream(Path.Combine(this.Directory, "LOCK"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException e)
            {
                throw new InvalidOperationException($"The history archive '{this.Directory}' is used by another archive!", e);
            }

            ReadSegments();

        }

        #endregion


        #region Append(Segment)

        /// <inheritdoc/>
        public void Append(RoamingNetworkArchiveSegment Segment)
        {

            if (!Segment.Check(out var errorResponse))
                throw new ArgumentException(errorResponse, nameof(Segment));

            lock (archiveLock)
            {

                var number = Segment.Info.Number;

                if (!segments.ContainsKey(number) && number != segments.Count + 1)
                    throw new ArgumentException($"The next segment of the archive is {segments.Count + 1}, not {number}!", nameof(Segment));

                WriteSynced(BaseFile(number),     utf8.GetBytes(POIJSON.ToText(Segment.Base)));
                WriteSynced(CommitsFile(number),  utf8.GetBytes(String.Concat(Segment.Commits.Select(commit => POIJSON.ToText(commit.ToJSON(Embedded: true)) + "\n"))));

                using (var segmentsFile = new FileStream(Path.Combine(Directory, SegmentsFileName), FileMode.Append, FileAccess.Write, FileShare.Read))
                {
                    segmentsFile.Write(utf8.GetBytes(POIJSON.ToText(Segment.Info.ToJSON(Embedded: true)) + "\n"));
                    segmentsFile.Flush(true);
                }

                segments[number] = Segment.Info;

            }

        }

        #endregion

        #region Load(Number)

        /// <inheritdoc/>
        public RoamingNetworkArchiveSegment? Load(UInt32 Number)
        {

            lock (archiveLock)
            {

                if (!segments.TryGetValue(Number, out var info))
                    return null;

                var baseJSON  = RoamingNetworkHistoryFileStore.ParseLine(File.ReadAllText(BaseFile(Number), utf8));
                var commits   = new List<RoamingNetworkCommit>();

                foreach (var line in File.ReadAllLines(CommitsFile(Number), utf8))
                {

                    if (line.Trim().Length == 0)
                        continue;

                    if (!RoamingNetworkCommit.TryParse(RoamingNetworkHistoryFileStore.ParseLine(line), out var commit, out var errorResponse))
                        throw new InvalidOperationException($"The archive segment {Number} of '{Directory}' holds an invalid commit: {errorResponse}");

                    commits.Add(commit);

                }

                var segment = new RoamingNetworkArchiveSegment(info, baseJSON, commits);

                if (!segment.Check(out var checkError))
                    throw new InvalidOperationException(checkError);

                return segment;

            }

        }

        #endregion

        #region Dispose()

        /// <inheritdoc/>
        public void Dispose()
        {
            lockFile.Dispose();
        }

        #endregion


        #region (private) ReadSegments()

        private void ReadSegments()
        {

            var file = Path.Combine(Directory, SegmentsFileName);

            if (!File.Exists(file))
                return;

            var bytes   = File.ReadAllBytes(file);
            var start   = 0;
            var lineNo  = 0;

            while (start < bytes.Length)
            {

                var end       = Array.IndexOf(bytes, (Byte) '\n', start);
                var complete  = end >= 0;
                var line      = utf8.GetString(bytes, start, (complete ? end : bytes.Length) - start);

                lineNo++;

                if (line.Trim().Length > 0)
                {

                    RoamingNetworkArchiveSegmentInfo? info  = null;
                    String?                           error = null;

                    try
                    {
                        RoamingNetworkArchiveSegmentInfo.TryParse(RoamingNetworkHistoryFileStore.ParseLine(line), out info, out error);
                    }
                    catch (Exception e)
                    {
                        error = e.Message;
                    }

                    if (info is null)
                    {

                        // Written only partly, e.g. by a crash: dropped, and the file cut there.
                        if (!complete)
                        {
                            using var cut = new FileStream(file, FileMode.Open, FileAccess.Write, FileShare.None);
                            cut.SetLength(start);
                            cut.Flush(true);
                            break;
                        }

                        throw new InvalidOperationException($"The segments of the history archive '{Directory}' are invalid in line {lineNo}: {error}");

                    }

                    segments[info.Number] = info;

                }

                if (!complete)
                    break;

                start = end + 1;

            }

        }

        #endregion

        #region (private) BaseFile(Number), CommitsFile(Number)

        private String BaseFile   (UInt32 Number)
            => Path.Combine(Directory, $"segment-{Number:D6}.base.json");

        private String CommitsFile(UInt32 Number)
            => Path.Combine(Directory, $"segment-{Number:D6}.commits.jsonl");

        #endregion

        #region (private static) WriteSynced(Path, Bytes)

        /// <summary>
        /// Write a file completely and synced: to a temporary file first, then moved into place.
        /// </summary>
        private static void WriteSynced(String  FilePath,
                                        Byte[]  Bytes)
        {

            var temporary = FilePath + ".tmp";

            using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(Bytes);
                stream.Flush(true);
            }

            File.Move(temporary, FilePath, overwrite: true);

        }

        #endregion

    }

}
