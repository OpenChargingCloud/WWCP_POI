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
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Why two versions of a roaming network can not be merged.
    /// </summary>
    public enum RoamingNetworkMergeConflictKind
    {

        /// <summary>
        /// Both changed a property differently.
        /// </summary>
        DifferentValues,

        /// <summary>
        /// Both added an entity with different properties.
        /// </summary>
        AddAdd,

        /// <summary>
        /// One removed an entity the other changed, or added something to.
        /// </summary>
        DeleteModify,

        /// <summary>
        /// One moved an entity to another parent, the other changed it or moved it elsewhere.
        /// </summary>
        Moved,

        /// <summary>
        /// The merge is invalid as a whole, e.g. it names an entity one of them removed.
        /// </summary>
        InvalidResult,

        /// <summary>
        /// A change set of one names an entity as it must still be, by a precondition, and the other changed or removed it.
        /// </summary>
        Precondition

    }

}
