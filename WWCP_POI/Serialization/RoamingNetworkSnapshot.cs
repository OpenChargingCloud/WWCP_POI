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

using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public partial class RoamingNetwork
    {

        #region ToJSONSnapshot()

        /// <summary>
        /// Serialize a complete nested infrastructure snapshot with current timestamped statuses,
        /// entity timestamps and custom data. Status history and internal runtime data are excluded.
        /// This representation is not yet canonical JSON for signing.
        /// </summary>
        public JObject ToJSONSnapshot()
        {

            var persisted = DataSnapshot.ToJSON();
            WriteCurrentRuntimeStatuses(persisted);
            return persisted;

        }

        internal JObject? GetCapturedSnapshotDocument()
            => dataSnapshot?.GetDocument();

        #endregion

    }

}
