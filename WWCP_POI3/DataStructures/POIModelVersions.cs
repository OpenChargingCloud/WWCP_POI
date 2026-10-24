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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The versions of a model of charging stations or energy meters.
    /// </summary>
    internal static class ModelVersions
    {

        #region Of(Versions, Model)

        /// <summary>
        /// The given versions, each once and none empty, in their given order: a certificate names one of them exactly.
        /// </summary>
        /// <param name="Versions">The versions of a model.</param>
        /// <param name="Model">The model in words, e.g. "the charging station model 'GD-DC300'".</param>
        public static ImmutableArray<String> Of(IEnumerable<String>?  Versions,
                                                String                Model)
        {

            var versions = (Versions ?? []).ToArray();

            if (versions.Any(String.IsNullOrWhiteSpace))
                throw new ArgumentException($"A version of {Model} must not be empty!", nameof(Versions));

            if (versions.Any(version => version != version.Trim()))
                throw new ArgumentException($"A version of {Model} must not begin or end with whitespace!", nameof(Versions));

            if (versions.Distinct(StringComparer.Ordinal).Count() != versions.Length)
                throw new ArgumentException($"A version of {Model} must not be given twice!", nameof(Versions));

            return [ .. versions ];

        }

        #endregion

    }

}
