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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public partial class RoamingNetwork
    {

        #region Snapshot state

        private readonly Object snapshotLock = new();

        private volatile RoamingNetworkDataSnapshot? dataSnapshot;

        private Boolean snapshotProjectionMaterialized = true;
        private Boolean snapshotProjectionMaterializing;

        /// <summary>
        /// Capture the current POI data once as authoritative immutable versioned data.
        /// Subsequent changes must use ApplyChangeSet. Legacy entities are mutable, isolated
        /// projections; editing them does not edit this captured snapshot.
        /// </summary>
        public RoamingNetworkDataSnapshot DataSnapshot
        {
            get
            {
                lock (snapshotLock)
                    return dataSnapshot ??= RoamingNetworkDataSnapshot.Capture(ToJSONSnapshot(), 0);
            }
        }

        /// <summary>
        /// The revision of the authoritative snapshot.
        /// </summary>
        public Int64 Revision
            => DataSnapshot.Revision;

        /// <summary>
        /// The change set that produced this snapshot, if one has been applied.
        /// </summary>
        public String? AppliedChangeSetId
            => DataSnapshot.AppliedChangeSetId;

        #endregion

        #region ApplyChangeSet/TryApplyChangeSet

        /// <summary>
        /// Apply all operations atomically to a new immutable version, leaving this version unchanged.
        /// Legacy hierarchy objects in the returned network are materialized only when requested.
        /// Signed batches require a caller-provided verifier; signing/canonicalization is not supplied here.
        /// </summary>
        public RoamingNetwork ApplyChangeSet(RoamingNetworkChangeSet                  changeSet,
                                             Func<RoamingNetworkChangeSet, Boolean>?  VerifySignature = null)
        {

            var snapshot = DataSnapshot.ApplyChangeSet(changeSet, VerifySignature);
            var result   = Parse(RoamingNetworkDataSnapshot.OwnJSON(snapshot.Entities[snapshot.Root]));

            result.dataSnapshot                   = snapshot;
            result.snapshotProjectionMaterialized = false;

            return result;

        }

        /// <summary>
        /// Try to apply an entire change set without returning a partially modified network.
        /// </summary>
        public Boolean TryApplyChangeSet(RoamingNetworkChangeSet                  changeSet,
                                         [NotNullWhen(true)] out RoamingNetwork?  network,
                                         [NotNullWhen(false)] out String?         error,
                                         Func<RoamingNetworkChangeSet, Boolean>?  VerifySignature = null)
        {

            network = null;
            error   = null;

            try
            {
                network = ApplyChangeSet(changeSet, VerifySignature);
                return true;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

        }

        #endregion

        #region Materialize the legacy hierarchy

        private void EnsureSnapshotProjection()
        {

            lock (snapshotLock)
            {
                if (snapshotProjectionMaterialized || snapshotProjectionMaterializing || dataSnapshot is null)
                    return;

                snapshotProjectionMaterializing = true;

                try
                {
                    MaterializeSnapshotProjection(dataSnapshot);
                    snapshotProjectionMaterialized = true;
                }
                finally
                {
                    snapshotProjectionMaterializing = false;
                }
            }

        }

        private void MaterializeSnapshotProjection(RoamingNetworkDataSnapshot snapshot)
        {

            // Parse into this network so all upward links refer to the returned version.
            var operators = new List<ChargingStationOperator>();
            var providers = new List<EMobilityProvider>();

            foreach (var key in snapshot.Entities[snapshot.Root].Children)
            {
                var document = snapshot.GetEntityJSON(key.Type, key.Id);

                if (key.Type == InfrastructureEntityType.ChargingStationOperator)
                    operators.Add(ChargingStationOperator.Parse(document, this));

                else if (key.Type == InfrastructureEntityType.EMobilityProvider)
                    providers.Add(EMobilityProvider.Parse(document, this));
            }

            foreach (var chargingStationOperator in operators)
                projectedChargingStationOperators.TryAdd(chargingStationOperator.Id, chargingStationOperator);

            foreach (var provider in providers)
                projectedEMobilityProviders.TryAdd(provider.Id, provider);

        }

        #endregion

        #region Restore revision metadata

        internal void RestoreVersionedSnapshot(JObject json)
        {

            if (json["revision"] is not { } revision)
                return;

            if (revision.Type != JTokenType.Integer || !Int64.TryParse(revision.ToString(), out var number) || number < 0)
                throw new ArgumentException("revision: expected a nonnegative Int64.");

            var changeSetId = InfrastructureJson.Text(json, "appliedChangeSetId");

            dataSnapshot = RoamingNetworkDataSnapshot.Capture(json, number, changeSetId);

        }

        #endregion

    }

}
