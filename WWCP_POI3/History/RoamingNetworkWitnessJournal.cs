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
using System.Globalization;
using System.Text;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A checkpoint a witness signed: of which roaming network, epoch and sequence, whether it is the final one of its
    /// epoch, and when it was signed.
    /// </summary>
    /// <param name="RoamingNetworkId">The roaming network of the checkpoint.</param>
    /// <param name="Epoch">Its epoch.</param>
    /// <param name="Sequence">Its sequence.</param>
    /// <param name="CheckpointId">Its identification.</param>
    /// <param name="IsFinal">Whether it is the final checkpoint of its epoch.</param>
    /// <param name="SignedAt">When the witness signed it.</param>
    public sealed record RoamingNetworkWitnessJournalEntry(RoamingNetwork_Id            RoamingNetworkId,
                                                           UInt32                       Epoch,
                                                           UInt64                       Sequence,
                                                           RoamingNetworkCheckpoint_Id  CheckpointId,
                                                           Boolean                      IsFinal,
                                                           DateTimeOffset               SignedAt)
    {

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {
            public const String  RoamingNetworkId  = "roamingNetworkId";
            public const String  Epoch             = "epoch";
            public const String  Sequence          = "sequence";
            public const String  CheckpointId      = "checkpointId";
            public const String  Final             = "final";
            public const String  SignedAt          = "signedAt";
        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this entry, a line of the journal file.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString()),
                           new JProperty(PropertyKeys.Epoch,             Epoch),
                           new JProperty(PropertyKeys.Sequence,          Sequence),
                           new JProperty(PropertyKeys.CheckpointId,      CheckpointId.ToString())
                       );

            if (IsFinal)
                json.Add(PropertyKeys.Final, true);

            json.Add(PropertyKeys.SignedAt, SignedAt.ToISO8601());

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out Entry, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an entry.
        /// </summary>
        public static Boolean TryParse(JObject                                                      JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkWitnessJournalEntry?  Entry,
                                       [NotNullWhen(false)] out String?                             ErrorResponse)
        {

            Entry = null;

            if (JSON[PropertyKeys.RoamingNetworkId]?.Type != JTokenType.String ||
                !RoamingNetwork_Id.TryParse(JSON[PropertyKeys.RoamingNetworkId]!.Value<String>()!, out var roamingNetworkId))
            {
                ErrorResponse = "A journal entry of a witness needs its roaming network!";
                return false;
            }

            if (JSON[PropertyKeys.Epoch]?.Type != JTokenType.Integer || JSON[PropertyKeys.Epoch]!.Value<Int64>() is < 1 or > UInt32.MaxValue)
            {
                ErrorResponse = "A journal entry of a witness needs its epoch!";
                return false;
            }

            if (JSON[PropertyKeys.Sequence]?.Type != JTokenType.Integer || JSON[PropertyKeys.Sequence]!.Value<Decimal>() < 0)
            {
                ErrorResponse = "A journal entry of a witness needs its sequence!";
                return false;
            }

            if (JSON[PropertyKeys.CheckpointId]?.Type != JTokenType.String ||
                !RoamingNetworkCheckpoint_Id.TryParse(JSON[PropertyKeys.CheckpointId]!.Value<String>(), out var checkpointId))
            {
                ErrorResponse = "A journal entry of a witness needs its checkpoint!";
                return false;
            }

            if (JSON[PropertyKeys.Final] is JToken final && final.Type != JTokenType.Boolean)
            {
                ErrorResponse = "The final flag of a journal entry of a witness must be a boolean!";
                return false;
            }

            if (JSON[PropertyKeys.SignedAt]?.Type != JTokenType.String ||
                !DateTimeOffset.TryParse(JSON[PropertyKeys.SignedAt]!.Value<String>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var signedAt))
            {
                ErrorResponse = "A journal entry of a witness needs when it was signed!";
                return false;
            }

            Entry          = new (roamingNetworkId,
                                  JSON[PropertyKeys.Epoch]!.Value<UInt32>(),
                                  JSON[PropertyKeys.Sequence]!.Value<UInt64>(),
                                  checkpointId,
                                  JSON[PropertyKeys.Final]?.Value<Boolean>() ?? false,
                                  signedAt);
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }


    /// <summary>
    /// What a witness signed (SPECIFICATION.md, section 10.2), kept beyond the history it signed it from: across restarts,
    /// histories started anew from a snapshot of another server, and epochs. One per witness, shared by every history it
    /// witnesses with. In memory, e.g. for tests, or in a file of one JSON line per checkpoint, written through before the
    /// signature is given; a last line written only partly, e.g. by a crash, is dropped when loading, as no signature
    /// followed it, an invalid line before it is an error.
    /// </summary>
    public sealed class RoamingNetworkWitnessJournal : IDisposable
    {

        #region Data

        private static readonly UTF8Encoding utf8 = new (encoderShouldEmitUTF8Identifier: false);

        private readonly Dictionary<(RoamingNetwork_Id, UInt32, UInt64), RoamingNetworkWitnessJournalEntry>  entries      = [];
        private readonly FileStream?                                                                          file;
        private readonly Lock                                                                                 journalLock  = new();

        #endregion

        #region Properties

        /// <summary>
        /// The file of the journal, none when it is kept in memory only.
        /// </summary>
        public String?  Path    { get; }

        /// <summary>
        /// The checkpoints signed, in the order of their roaming networks, epochs and sequences.
        /// </summary>
        public IReadOnlyList<RoamingNetworkWitnessJournalEntry> Entries
        {
            get
            {
                lock (journalLock)
                {
                    return [ .. entries.Values.OrderBy(entry => entry.RoamingNetworkId.ToString(), StringComparer.Ordinal).
                                               ThenBy (entry => entry.Epoch).
                                               ThenBy (entry => entry.Sequence) ];
                }
            }
        }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a journal kept in memory only: it forgets what was signed when the process ends.
        /// </summary>
        public RoamingNetworkWitnessJournal()
        { }

        /// <summary>
        /// Open the journal in the given file, created when it does not exist yet, and read what was signed.
        /// </summary>
        /// <param name="Path">The file of the journal.</param>
        /// <exception cref="InvalidDataException">A line before the last one is no entry, or two name another checkpoint of one sequence.</exception>
        public RoamingNetworkWitnessJournal(String Path)
        {

            this.Path = Path ?? throw new ArgumentNullException(nameof(Path));

            if (System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(Path)) is String directory)
                Directory.CreateDirectory(directory);

            file = new FileStream(Path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

            try
            {

                var text   = new StreamReader(file, utf8, detectEncodingFromByteOrderMarks: false, leaveOpen: true).ReadToEnd();
                var lines  = text.Split('\n');
                var kept   = 0L;

                for (var index = 0; index < lines.Length; index++)
                {

                    var line    = lines[index];
                    var isLast  = index == lines.Length - 1;

                    // The text after the last line break: empty, or a line written only partly.
                    if (isLast)
                        break;

                    if (!TryParseLine(line, out var entry, out var error))
                        throw new InvalidDataException($"Line {index + 1} of the journal of the witness '{Path}' is invalid: {error}");

                    var key = (entry.RoamingNetworkId, entry.Epoch, entry.Sequence);

                    if (entries.TryGetValue(key, out var signed) && signed.CheckpointId != entry.CheckpointId)
                        throw new InvalidDataException($"The journal of the witness '{Path}' names two checkpoints of the sequence {entry.Sequence} of the epoch {entry.Epoch}: '{signed.CheckpointId}' and '{entry.CheckpointId}'!");

                    entries[key]  = entry;
                    kept         += utf8.GetByteCount(line) + 1;

                }

                // A line written only partly is dropped, so that the next one starts on a line of its own.
                file.SetLength(kept);
                file.Seek(kept, SeekOrigin.Begin);

            }
            catch
            {
                file.Dispose();
                throw;
            }

        }

        #endregion


        #region TryGetSigned(RoamingNetworkId, Epoch, Sequence, out Entry)

        /// <summary>
        /// The checkpoint of the given roaming network, epoch and sequence the witness signed, if any.
        /// </summary>
        public Boolean TryGetSigned(RoamingNetwork_Id                                            RoamingNetworkId,
                                    UInt32                                                       Epoch,
                                    UInt64                                                       Sequence,
                                    [NotNullWhen(true)] out RoamingNetworkWitnessJournalEntry?  Entry)
        {
            lock (journalLock)
            {
                return entries.TryGetValue((RoamingNetworkId, Epoch, Sequence), out Entry);
            }
        }

        #endregion

        #region (internal) TryKeep(Checkpoint, Holds, StartedFrom, SignedAt, out ErrorResponse)

        /// <summary>
        /// Keep that the witness signs the given checkpoint, unless it may not (SPECIFICATION.md, section 10.2): it signed
        /// another one of the same roaming network, epoch and sequence; the history it signs from does not hold the one it
        /// signed last of the epoch, so that it can not tell that this one is of the same line; or it signed the final
        /// checkpoint of the epoch before, and the epoch of this one does not start from it. Kept before the signature is
        /// given; one signed already is kept already, and signed again.
        /// </summary>
        /// <param name="Checkpoint">The checkpoint.</param>
        /// <param name="Holds">Whether the history it signs from holds the given checkpoint.</param>
        /// <param name="StartedFrom">The final checkpoint of the epoch before the epoch of the checkpoint starts from, if known.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="ErrorResponse">Why it may not be signed.</param>
        internal Boolean TryKeep(RoamingNetworkCheckpoint                    Checkpoint,
                                 Func<RoamingNetworkCheckpoint_Id, Boolean>  Holds,
                                 RoamingNetworkEpochLink?                    StartedFrom,
                                 DateTimeOffset                              SignedAt,
                                 [NotNullWhen(false)] out String?            ErrorResponse)
        {

            lock (journalLock)
            {

                var key = (Checkpoint.RoamingNetworkId, Checkpoint.Epoch, Checkpoint.Sequence);

                // Never two checkpoints of one sequence: that is what makes a split view visible.
                if (entries.TryGetValue(key, out var signed))
                {

                    if (signed.CheckpointId == Checkpoint.Id)
                    {
                        ErrorResponse = null;
                        return true;
                    }

                    ErrorResponse = $"This witness signed the checkpoint '{signed.CheckpointId}' of the sequence {Checkpoint.Sequence} of the epoch {Checkpoint.Epoch} already: two checkpoints of one sequence are a split view!";
                    return false;

                }

                // Of the line it signed: the history holds the checkpoint it signed last of the epoch, one per sequence.
                var latest = entries.Values.Where  (entry => entry.RoamingNetworkId == Checkpoint.RoamingNetworkId && entry.Epoch == Checkpoint.Epoch).
                                            MaxBy  (entry => entry.Sequence);

                if (latest is not null && !Holds(latest.CheckpointId))
                {
                    ErrorResponse = $"This history does not hold the checkpoint '{latest.CheckpointId}' of the sequence {latest.Sequence} this witness signed last in the epoch {Checkpoint.Epoch}: it can not tell that '{Checkpoint.Id}' is of the same line!";
                    return false;
                }

                // An epoch starting from the final checkpoint it signed of the epoch before.
                if (Checkpoint.Epoch > 1 &&
                    entries.Values.FirstOrDefault(entry => entry.RoamingNetworkId == Checkpoint.RoamingNetworkId && entry.Epoch == Checkpoint.Epoch - 1 && entry.IsFinal) is RoamingNetworkWitnessJournalEntry final &&
                    (StartedFrom is null || StartedFrom.Epoch != final.Epoch || StartedFrom.CheckpointId != final.CheckpointId))
                {
                    ErrorResponse = StartedFrom is null
                                        ? $"This history does not tell from which final checkpoint the epoch {Checkpoint.Epoch} started; this witness signed '{final.CheckpointId}' as the final one of the epoch {final.Epoch}!"
                                        : $"The epoch {Checkpoint.Epoch} starts from the final checkpoint '{StartedFrom.CheckpointId}', but this witness signed '{final.CheckpointId}' as the final one of the epoch {final.Epoch}!";
                    return false;
                }

                var entry = new RoamingNetworkWitnessJournalEntry(Checkpoint.RoamingNetworkId,
                                                                  Checkpoint.Epoch,
                                                                  Checkpoint.Sequence,
                                                                  Checkpoint.Id,
                                                                  Checkpoint.IsFinal,
                                                                  DateTimeOffset.FromUnixTimeMilliseconds(SignedAt.ToUnixTimeMilliseconds()));

                try
                {

                    if (file is not null)
                    {
                        file.Write(utf8.GetBytes(POIJSON.ToText(entry.ToJSON()) + "\n"));
                        file.Flush(true);
                    }

                }
                catch (Exception e)
                {
                    ErrorResponse = $"The journal of the witness can not keep the checkpoint '{Checkpoint.Id}': {e.Message}";
                    return false;
                }

                entries[key]   = entry;
                ErrorResponse  = null;
                return true;

            }

        }

        #endregion

        #region (private static) TryParseLine(Line, out Entry, out ErrorResponse)

        private static Boolean TryParseLine(String                                                       Line,
                                            [NotNullWhen(true)]  out RoamingNetworkWitnessJournalEntry?  Entry,
                                            [NotNullWhen(false)] out String?                             ErrorResponse)
        {

            Entry = null;

            try
            {

                if (POIJSON.Read(Line.TrimEnd('\r')) is not JObject json)
                {
                    ErrorResponse = "It is no JSON object!";
                    return false;
                }

                return RoamingNetworkWitnessJournalEntry.TryParse(json, out Entry, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region Dispose()

        /// <summary>
        /// Close the file of the journal.
        /// </summary>
        public void Dispose()
        {
            lock (journalLock)
            {
                file?.Dispose();
            }
        }

        #endregion

    }

}
