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

            if (dataSnapshot is not null)
                return dataSnapshot.ToJSON();

            var json = ToJSON(
                           ExpandChargingStationOperatorIds:       InfoStatus.Expanded,
                           ExpandChargingPoolIds:                  InfoStatus.Expanded,
                           ExpandChargingStationIds:               InfoStatus.Expanded,
                           ExpandEVSEIds:                          InfoStatus.Expanded,
                           ExpandBrandIds:                         InfoStatus.Expanded,
                           ExpandDataLicenses:                     InfoStatus.Expanded,
                           ExpandEMobilityProviderId:              InfoStatus.Expanded,
                           CustomChargingStationOperatorSerializer: (entity, document) => InfrastructureJson.SnapshotMetadata(document, entity),
                           CustomChargingPoolSerializer:            (entity, document) => InfrastructureJson.SnapshotMetadata(document, entity),
                           CustomChargingStationSerializer:         (entity, document) => InfrastructureJson.SnapshotMetadata(document, entity),
                           CustomEVSESerializer:                    (entity, document) => InfrastructureJson.SnapshotMetadata(document, entity));

            InfrastructureJson.SnapshotMetadata(json, this);
            AddProviderSnapshotMetadata(json);

            return json;

        }

        private void AddProviderSnapshotMetadata(JObject json)
        {

            if (json["eMobilityProviders"] is not JArray providers)
                return;

            var byId = EMobilityProviders.ToDictionary(provider => provider.Id.ToString());

            foreach (var document in providers.OfType<JObject>())
            {
                var id = document["@id"]!.Value<String>()!;

                InfrastructureJson.SnapshotMetadata(document, byId[id]);
            }

        }

        #endregion

    }

}
