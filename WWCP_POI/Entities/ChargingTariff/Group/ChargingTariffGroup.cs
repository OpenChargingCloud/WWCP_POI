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

using System.Collections.Immutable;
#region Usings

using System;
using System.Collections.Generic;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A charging tariff group.
    /// </summary>
    public sealed partial class ChargingTariffGroup : AImmutableEMobilityEntity<ChargingTariffGroup_Id,
                                                        ChargingTariffGroupAdminStatusTypes,
                                                        ChargingTariffGroupStatusTypes>,
                                       IEquatable<ChargingTariffGroup>, IComparable<ChargingTariffGroup>, IComparable,
                                       IEnumerable<ChargingTariff>
    {

        #region Data

        private readonly ImmutableDictionary<ChargingTariff_Id, ChargingTariff> _ChargingTariffs;

        #endregion

        #region Properties

        /// <summary>
        /// An optional (multi-language) description of this group.
        /// </summary>


        /// <summary>
        /// Return all charging stations registered within this charging station group.
        /// </summary>
        public IEnumerable<ChargingTariff> ChargingTariffs
            => _ChargingTariffs.Values;


        /// <summary>
        /// Return all charging station identifications registered within this charging station group.
        /// </summary>
        public IEnumerable<ChargingTariff_Id> ChargingTariffIds
            => ChargingTariffs.SafeSelect(station => station.Id);

        #endregion

        #region Links

        /// <summary>
        /// The Charging Station Operator of this charging pool.
        /// </summary>
        [Mandatory]
        public ChargingStationOperator Operator { get; }

        /// <summary>
        /// The roaming network of this charging station.
        /// </summary>
        [InternalUseOnly]
        public RoamingNetwork RoamingNetwork
            => Operator.RoamingNetwork;

        #endregion

        #region Events

        #region ChargingTariffAddition

        internal readonly IVotingNotificator<DateTimeOffset, ChargingTariffGroup, ChargingTariff, Boolean> ChargingTariffAddition;

        /// <summary>
        /// Called whenever a charging station will be or was added.
        /// </summary>
        public IVotingSender<DateTimeOffset, ChargingTariffGroup, ChargingTariff, Boolean> OnChargingTariffAddition
        {
            get
            {
                return ChargingTariffAddition;
            }
        }

        #endregion

        #region ChargingTariffRemoval

        internal readonly IVotingNotificator<DateTimeOffset, ChargingTariffGroup, ChargingTariff, Boolean> ChargingTariffRemoval;

        /// <summary>
        /// Called whenever a charging station will be or was removed.
        /// </summary>
        public IVotingSender<DateTimeOffset, ChargingTariffGroup, ChargingTariff, Boolean> OnChargingTariffRemoval
        {
            get
            {
                return ChargingTariffRemoval;
            }
        }

        #endregion

        #endregion


        public ChargingTariffGroup WithMembers(IEnumerable<ChargingTariff> members)
        {
            var copy = new ChargingTariffGroup(Id, Operator, Description, members);
            copy.SetAdminStatus(AdminStatusSchedule());
            copy.SetStatus(StatusSchedule());
            copy.RestoreSnapshotTimestamps(Created, Timestamp.Now);
            return copy;
        }
        public ChargingTariffGroup WithMember(ChargingTariff tariff)
            => WithMembers(ChargingTariffs.Where(value => value.Id != tariff.Id).Append(tariff));
        public ChargingTariffGroup WithoutMember(ChargingTariff_Id id)
            => WithMembers(ChargingTariffs.Where(value => value.Id != id));

        public Newtonsoft.Json.Linq.JObject ToJSON(Boolean Embedded = false)
        {
            var json = InfrastructureJson.SnapshotMetadata(new Newtonsoft.Json.Linq.JObject(
                   new Newtonsoft.Json.Linq.JProperty("@id", Id.ToString()),
                   new Newtonsoft.Json.Linq.JProperty("description", Description.ToJSON()),
                   new Newtonsoft.Json.Linq.JProperty("chargingStationOperatorId", Operator.Id.ToString()),
                   new Newtonsoft.Json.Linq.JProperty("chargingTariffIds", new Newtonsoft.Json.Linq.JArray(
                       ChargingTariffIds.OrderBy(id => id).Select(id => id.ToString())))), this);
            if (!Embedded)
                json["@context"] = "https://open.charging.cloud/contexts/wwcp+json/ChargingTariffGroup";
            return POIRepresentation.AddETags(this, json);
        }

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station group.
        /// </summary>
        /// <param name="Id">The unique identification of the charging station group.</param>
        /// <param name="Operator">The charging station operator of this charging station group.</param>
        /// <param name="Description">An optional (multi-language) description of this charging station group.</param>
        public ChargingTariffGroup(ChargingTariffGroup_Id   Id,
                                     ChargingStationOperator  Operator,
                                     I18NString? Description = null, IEnumerable<ChargingTariff>? Members = null)

            : base(Id, Description: Description)

        {

            this.Operator                = Operator ?? throw new ArgumentNullException(nameof(Operator), "The charging station operator must not be null!");


            if (Id.OperatorId != Operator.Id)
                throw new ArgumentException("Group identifier belongs to a different operator.", nameof(Id));
            this._ChargingTariffs = (Members ?? []).ToImmutableDictionary(tariff => tariff.Id);
            if (_ChargingTariffs.Values.Any(tariff => tariff.Operator.Id != Operator.Id))
                throw new ArgumentException("A tariff belongs to a different operator.", nameof(Members));


            this.ChargingTariffAddition  = new VotingNotificator<DateTimeOffset, ChargingTariffGroup, ChargingTariff, Boolean>(() => new VetoVote(), true);
            this.ChargingTariffRemoval   = new VotingNotificator<DateTimeOffset, ChargingTariffGroup, ChargingTariff, Boolean>(() => new VetoVote(), true);

        }

        #endregion


        #region ContainsId(ChargingTariffId)

        /// <summary>
        /// Check if the given charging tariff identification is member of this charging tariff group.
        /// </summary>
        /// <param name="ChargingTariffId">The unique identification of the charging tariff.</param>
        public Boolean ContainsId(ChargingTariff_Id ChargingTariffId)
            => _ChargingTariffs.ContainsKey(ChargingTariffId);

        #endregion


        #region IEnumerable<ChargingTariff> Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ChargingTariffs.GetEnumerator();
        }

        public IEnumerator<ChargingTariff> GetEnumerator()
        {
            return ChargingTariffs.GetEnumerator();
        }

        #endregion


        #region Operator overloading

        #region Operator == (ChargingTariffGroup1, ChargingTariffGroup2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroup1">A charging station group.</param>
        /// <param name="ChargingTariffGroup2">Another charging station group.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator == (ChargingTariffGroup ChargingTariffGroup1, ChargingTariffGroup ChargingTariffGroup2)
        {

            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(ChargingTariffGroup1, ChargingTariffGroup2))
                return true;

            // If one is null, but not both, return false.
            if (((Object) ChargingTariffGroup1 is null) || ((Object) ChargingTariffGroup2 is null))
                return false;

            return ChargingTariffGroup1.Equals(ChargingTariffGroup2);

        }

        #endregion

        #region Operator != (ChargingTariffGroup1, ChargingTariffGroup2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroup1">A charging station group.</param>
        /// <param name="ChargingTariffGroup2">Another charging station group.</param>
        /// <returns>False if both match; True otherwise.</returns>
        public static Boolean operator != (ChargingTariffGroup ChargingTariffGroup1, ChargingTariffGroup ChargingTariffGroup2)
            => !(ChargingTariffGroup1 == ChargingTariffGroup2);

        #endregion

        #region Operator <  (ChargingTariffGroup1, ChargingTariffGroup2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroup1">A charging station group.</param>
        /// <param name="ChargingTariffGroup2">Another charging station group.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator < (ChargingTariffGroup ChargingTariffGroup1, ChargingTariffGroup ChargingTariffGroup2)
        {

            if ((Object) ChargingTariffGroup1 is null)
                throw new ArgumentNullException(nameof(ChargingTariffGroup1), "The given ChargingTariffGroup1 must not be null!");

            return ChargingTariffGroup1.CompareTo(ChargingTariffGroup2) < 0;

        }

        #endregion

        #region Operator <= (ChargingTariffGroup1, ChargingTariffGroup2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroup1">A charging station group.</param>
        /// <param name="ChargingTariffGroup2">Another charging station group.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator <= (ChargingTariffGroup ChargingTariffGroup1, ChargingTariffGroup ChargingTariffGroup2)
            => !(ChargingTariffGroup1 > ChargingTariffGroup2);

        #endregion

        #region Operator >  (ChargingTariffGroup1, ChargingTariffGroup2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroup1">A charging station group.</param>
        /// <param name="ChargingTariffGroup2">Another charging station group.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator > (ChargingTariffGroup ChargingTariffGroup1, ChargingTariffGroup ChargingTariffGroup2)
        {

            if ((Object) ChargingTariffGroup1 is null)
                throw new ArgumentNullException(nameof(ChargingTariffGroup1), "The given ChargingTariffGroup1 must not be null!");

            return ChargingTariffGroup1.CompareTo(ChargingTariffGroup2) > 0;

        }

        #endregion

        #region Operator >= (ChargingTariffGroup1, ChargingTariffGroup2)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroup1">A charging station group.</param>
        /// <param name="ChargingTariffGroup2">Another charging station group.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public static Boolean operator >= (ChargingTariffGroup ChargingTariffGroup1, ChargingTariffGroup ChargingTariffGroup2)
            => !(ChargingTariffGroup1 < ChargingTariffGroup2);

        #endregion

        #endregion

        #region IComparable<ChargingTariffGroup> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object Object)
        {

            if (Object is null)
                throw new ArgumentNullException(nameof(Object), "The given object must not be null!");

            var ChargingTariffGroup = Object as ChargingTariffGroup;
            if ((Object) ChargingTariffGroup is null)
                throw new ArgumentException("The given object is not a charging pool!", nameof(Object));

            return CompareTo(ChargingTariffGroup);

        }

        #endregion

        #region CompareTo(ChargingTariffGroup)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ChargingTariffGroup">A charging station group object to compare with.</param>
        public Int32 CompareTo(ChargingTariffGroup ChargingTariffGroup)
        {

            if ((Object) ChargingTariffGroup is null)
                throw new ArgumentNullException(nameof(ChargingTariffGroup), "The given charging station group must not be null!");

            return Id.CompareTo(ChargingTariffGroup.Id);

        }

        #endregion

        #endregion

        #region IEquatable<ChargingTariffGroup> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public override Boolean Equals(Object Object)
        {

            if (Object is null)
                return false;

            var ChargingTariffGroup = Object as ChargingTariffGroup;
            if ((Object) ChargingTariffGroup is null)
                return false;

            return Equals(ChargingTariffGroup);

        }

        #endregion

        #region Equals(ChargingTariffGroup)

        /// <summary>
        /// Compares two charging pools for equality.
        /// </summary>
        /// <param name="ChargingTariffGroup">A charging station group to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ChargingTariffGroup ChargingTariffGroup)
        {

            if ((Object) ChargingTariffGroup is null)
                return false;

            return Id.Equals(ChargingTariffGroup.Id);

        }

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Get the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
            => Id.GetHashCode();

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
            => String.Concat(Id, ", ", Description.FirstText());

        #endregion

    }

}
