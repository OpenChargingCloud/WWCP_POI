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
using System.Text.RegularExpressions;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The time zone of a charging pool or a charging pool template (SPECIFICATION.md, section 9.4): a time zone of the
    /// IANA time zone database of one of its regions, as the database writes it, e.g. Europe/Berlin, which the time zone
    /// database of this platform knows. No Windows name, none without a region (CET, UTC, Etc/GMT-1, US/Eastern), none in
    /// another case: a reader refuses them, as it refuses a time zone its database does not know, in place of taking it as
    /// UTC - two readers would else find different offers overlapping, or price one session differently.
    /// </summary>
    public static partial class POITimeZones
    {

        /// <summary>
        /// A name of one of the regions of the IANA time zone database, of a place and maybe a part of the region.
        /// </summary>
        [GeneratedRegex(@"^(?:Africa|America|Antarctica|Arctic|Asia|Atlantic|Australia|Europe|Indian|Pacific)(?:/[A-Za-z0-9_+\-]+){1,2}\z")]
        private static partial Regex NameOfARegion();


        #region TryFind(TimeZone, out TimeZoneInfo)

        /// <summary>
        /// The time zone of the given name, if it is one a charging pool may have and the time zone database of this
        /// platform knows it: .NET finds an IANA name in its case on Windows too, but only with ICU, not under invariant
        /// globalization; and only one the ICU of Windows maps, e.g. not America/Ciudad_Juarez (2022g).
        /// </summary>
        /// <param name="TimeZone">The time zone of a charging pool or a charging pool template.</param>
        /// <param name="TimeZoneInfo">The time zone found.</param>
        public static Boolean TryFind(Time_Zone                                TimeZone,
                                      [NotNullWhen(true)] out TimeZoneInfo?    TimeZoneInfo)
        {

            var name = TimeZone.ToString();

            if (NameOfARegion().IsMatch(name)                                       &&
                System.TimeZoneInfo.TryFindSystemTimeZoneById(name, out var found)  &&
                found.HasIanaId                                                     &&
                found.Id == name)
            {
                TimeZoneInfo = found;
                return true;
            }

            TimeZoneInfo = null;
            return false;

        }

        #endregion

        #region (internal) Known(TimeZone, Owner)

        /// <summary>
        /// The given time zone of a charging pool or a charging pool template, if it is one it may have and this platform
        /// knows it.
        /// </summary>
        /// <param name="TimeZone">The time zone, if any.</param>
        /// <param name="Owner">The charging pool or charging pool template, for the error.</param>
        /// <exception cref="ArgumentException">The time zone is none a charging pool may have, or this platform does not know it.</exception>
        internal static Time_Zone? Known(Time_Zone?  TimeZone,
                                         String      Owner)

            => TimeZone is Time_Zone timeZone && !TryFind(timeZone, out _)
                   ? throw new ArgumentException($"The time zone '{timeZone}' of {Owner} is no time zone of a region of the IANA time zone database this reader knows!", nameof(TimeZone))
                   : TimeZone;

        #endregion

    }

}
