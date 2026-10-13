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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A store in memory, e.g. for tests: it keeps the JSON a file store writes, so that loading reads it again.
    /// </summary>
    public sealed class RoamingNetworkHistoryMemoryStore : IRoamingNetworkHistoryStore
    {

        #region Data

        private readonly Lock          storeLock  = new ();
        private          String?       baseText;
        private readonly List<String>  lines      = [];

        #endregion

        #region Properties

        /// <summary>
        /// The number of log entries.
        /// </summary>
        public Int32                Count
        {
            get
            {
                lock (storeLock)
                {
                    return lines.Count;
                }
            }
        }

        /// <summary>
        /// Called before every write; throw from here to let a write fail.
        /// </summary>
        public Action<String>?      BeforeWrite  { get; set; }

        #endregion


        /// <inheritdoc/>
        public RoamingNetworkHistoryStoreContent? Load()
        {

            lock (storeLock)
            {

                if (baseText is null)
                    return null;

                var entries = new List<RoamingNetworkHistoryLogEntry>();

                foreach (var line in lines)
                {

                    if (!RoamingNetworkHistoryLogEntry.TryParse(RoamingNetworkHistoryFileStore.ParseLine(line), out var entry, out var error))
                        throw new InvalidOperationException("Invalid log entry: " + error);

                    entries.Add(entry);

                }

                return new (RoamingNetworkHistoryFileStore.ParseLine(baseText), entries);

            }

        }

        /// <inheritdoc/>
        public void WriteBase(JObject Snapshot)
        {

            lock (storeLock)
            {

                BeforeWrite?.Invoke("base");

                if (baseText is not null)
                    throw new InvalidOperationException("The store is not empty!");

                baseText = Snapshot.ToString(Newtonsoft.Json.Formatting.None);

            }

        }

        /// <inheritdoc/>
        public void Append(RoamingNetworkHistoryLogEntry Entry)
        {

            lock (storeLock)
            {
                BeforeWrite?.Invoke(Entry.Type.ToString());
                lines.Add(Entry.ToJSON().ToString(Newtonsoft.Json.Formatting.None));
            }

        }

        /// <inheritdoc/>
        public void Compact(JObject                                     Snapshot,
                            IEnumerable<RoamingNetworkHistoryLogEntry>  Entries)
        {

            var newLines = Entries.Select(entry => entry.ToJSON().ToString(Newtonsoft.Json.Formatting.None)).ToList();

            lock (storeLock)
            {

                BeforeWrite?.Invoke("compact");

                baseText = Snapshot.ToString(Newtonsoft.Json.Formatting.None);
                lines.Clear();
                lines.AddRange(newLines);

            }

        }

        /// <inheritdoc/>
        public void Dispose()
        { }

    }

}
