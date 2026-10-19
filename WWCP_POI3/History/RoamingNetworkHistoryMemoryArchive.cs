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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// An archive in memory, e.g. for tests: the segments as they were appended.
    /// </summary>
    public sealed class RoamingNetworkHistoryMemoryArchive : IRoamingNetworkHistoryArchive
    {

        #region Data

        private readonly Lock                                archiveLock  = new ();
        private readonly List<RoamingNetworkArchiveSegment>  segments     = [];

        #endregion

        #region Properties

        /// <inheritdoc/>
        public IReadOnlyList<RoamingNetworkArchiveSegmentInfo> Segments
        {
            get
            {
                lock (archiveLock)
                {
                    return [ .. segments.Select(segment => segment.Info) ];
                }
            }
        }

        /// <summary>
        /// Fail every Append from now on, e.g. to test a failing archive.
        /// </summary>
        public Boolean FailAppends { get; set; }

        #endregion


        #region Append(Segment)

        /// <inheritdoc/>
        public void Append(RoamingNetworkArchiveSegment Segment)
        {

            if (FailAppends)
                throw new IOException("The archive fails, as told.");

            if (!Segment.Check(out var errorResponse))
                throw new ArgumentException(errorResponse, nameof(Segment));

            lock (archiveLock)
            {

                var index = segments.FindIndex(segment => segment.Info.Number == Segment.Info.Number);

                if (index >= 0)
                    segments[index] = Segment;

                else if (Segment.Info.Number == segments.Count + 1)
                    segments.Add(Segment);

                else
                    throw new ArgumentException($"The next segment of the archive is {segments.Count + 1}, not {Segment.Info.Number}!", nameof(Segment));

            }

        }

        #endregion

        #region Load(Number)

        /// <inheritdoc/>
        public RoamingNetworkArchiveSegment? Load(UInt32 Number)
        {
            lock (archiveLock)
            {
                return segments.FirstOrDefault(segment => segment.Info.Number == Number);
            }
        }

        #endregion

        #region Dispose()

        /// <inheritdoc/>
        public void Dispose()
        { }

        #endregion

    }

}
