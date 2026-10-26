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

using System.Collections.Immutable;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The hardware revisions or firmware versions of a model of charging stations or energy meters, or those a
    /// certificate covers.
    /// </summary>
    public static class ModelVersions
    {

        #region Of<T>(Versions, Kind, Model)

        /// <summary>
        /// The given versions, each once and none empty, in their given order: a station, a meter or a certificate names
        /// them exactly.
        /// </summary>
        /// <param name="Versions">The hardware revisions or firmware versions.</param>
        /// <param name="Kind">What they are, in words, e.g. "hardware revision".</param>
        /// <param name="Model">Whose they are, in words, e.g. "the charging station model 'GD-DC300'".</param>
        public static ImmutableArray<T> Of<T>(IEnumerable<T>?  Versions,
                                              String           Kind,
                                              String           Model)

            where T : struct, IId<T>

        {

            var versions = (Versions ?? []).ToArray();

            if (versions.Any(version => version.IsNullOrEmpty))
                throw new ArgumentException($"A {Kind} of {Model} must not be empty!", nameof(Versions));

            if (versions.Distinct().Count() != versions.Length)
                throw new ArgumentException($"A {Kind} of {Model} must not be given twice!", nameof(Versions));

            return [ .. versions ];

        }

        #endregion

    }

}
