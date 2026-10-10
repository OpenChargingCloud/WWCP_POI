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

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;
using org.GraphDefined.Vanaheimr.Aegir;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A parking space.
    /// </summary>
    public sealed partial class ParkingSpace : AImmutableEMobilityEntity<ParkingSpace_Id,
                                                 ParkingSpaceAdminStatusTypes,
                                                 ParkingSpaceStatusTypes>,
                                IEquatable<ParkingSpace>, IComparable<ParkingSpace>, IComparable
    {


        #region Properties

        /// <summary>
        /// Available products owned by the same parking operator; assignments do not imply price combination.
        /// </summary>
        public System.Collections.Immutable.ImmutableArray<ParkingProduct_Id> ParkingProductIds { get; }

        /// <summary>
        /// Optional garage assignment; the parking operator remains the owner of this space.
        /// </summary>
        public ParkingGarage_Id? ParkingGarageId { get; }



        #region OSM_WayId

        private String _OSM_WayId;

        /// <summary>
        /// OSM Node Id.
        /// </summary>
        [Optional]
        public String OSM_WayId
        {

            get
            {
                return _OSM_WayId;
            }

            private set
            {
                SetProperty<String>(ref _OSM_WayId, value);
            }

        }

        #endregion

        #region Geometry

        private readonly System.Collections.Immutable.ImmutableArray<GeoCoordinate> _Geometry;

        /// <summary>
        /// An optional polygon geometry of the parking space.
        /// </summary>
        [Optional]
        public System.Collections.Immutable.ImmutableArray<GeoCoordinate> Geometry
        {
            get
            {
                return _Geometry;
            }
        }

        #endregion

        // status := free, ocupied, reserved, not accessible

        // fee := double

        // fee unit := "€/h"

        // Opening hours

        // restrictions := EV only, must be plugged in, disabled persons only

        #region Sensors

        private readonly System.Collections.Immutable.ImmutableArray<String> _Sensors;

        /// <summary>
        /// Parking sensors at the parking space.
        /// </summary>
        [Optional]
        public System.Collections.Immutable.ImmutableArray<String> Sensors
        {
            get
            {
                return _Sensors;
            }
        }

        #endregion

        #endregion

        #region Links

        #region ChargingStations

        private readonly System.Collections.Immutable.ImmutableArray<ChargingStation> _ChargingStations;

        /// <summary>
        /// Charging stations reachable from this parking space.
        /// </summary>
        [Optional]
        public System.Collections.Immutable.ImmutableArray<ChargingStation> ChargingStations
        {
            get
            {
                return _ChargingStations;
            }
        }

        #endregion

        #endregion


        #region Constructor(s)

        #region (internal) ParkingSpace()

        /// <summary>
        /// Create a new parking space having a random identification.
        /// </summary>
        internal ParkingSpace()
            : this(ParkingSpace_Id.New)
        { }

        #endregion

        #region (internal) ParkingSpace(Id)

        /// <summary>
        /// Create a new parking space having the given identification.
        /// </summary>
        /// <param name="Id">The unique identification of the parking space.</param>
        public ParkingSpace(ParkingSpace_Id Id, I18NString? Name = null, I18NString? Description = null,
                              String? OSM_WayId = null, IEnumerable<GeoCoordinate>? Geometry = null,
                              IEnumerable<ChargingStation>? ChargingStations = null, IEnumerable<String>? Sensors = null,
                              IEnumerable<ParkingProduct_Id>? ParkingProductIds = null, ParkingGarage_Id? ParkingGarageId = null)
            : base(Id, Name ?? I18NString.Create(Id.ToString()), Description)
        {
            this.ParkingProductIds = POIGraphJSON.ReferenceIds(ParkingProductIds ?? [], InfrastructureEntityType.ParkingProduct);
            if (ParkingGarageId is { } garage && garage.IsNullOrEmpty) throw new ArgumentException("Invalid garage identifier.");
            this.ParkingGarageId = ParkingGarageId;
            this._OSM_WayId = OSM_WayId;
            this._Geometry = ImmutablePOIValues.CopyItems(Geometry);
            this._ChargingStations = ImmutablePOIValues.CopyItems(ChargingStations);
            this._Sensors = ImmutablePOIValues.CopyItems(Sensors);
        }

        #endregion

        #endregion


        #region IComparable<ParkingSpace> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public override Int32 CompareTo(Object Object)
        {

            if (Object is null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is a service plan.
            var ServicePlan = Object as ParkingSpace;
            if ((Object) ServicePlan is null)
                throw new ArgumentException("The given object is not a service plan!");

            return CompareTo(ServicePlan);

        }

        #endregion

        #region CompareTo(ParkingSpace)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="ParkingSpace">A service plan object to compare with.</param>
        public Int32 CompareTo(ParkingSpace ParkingSpace)
        {

            if ((Object) ParkingSpace is null)
                throw new ArgumentNullException("The given service plan must not be null!");

            return Id.CompareTo(ParkingSpace.Id);

        }

        #endregion

        #endregion

        #region IEquatable<ParkingSpace> Members

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

            // Check if the given object is a service plan.
            var ParkingSpace = Object as ParkingSpace;
            if ((Object) ParkingSpace is null)
                return false;

            return this.Equals(ParkingSpace);

        }

        #endregion

        #region Equals(ParkingSpace)

        /// <summary>
        /// Compares two service plans for equality.
        /// </summary>
        /// <param name="ParkingSpace">A service plan to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(ParkingSpace ParkingSpace)
        {

            if ((Object) ParkingSpace is null)
                return false;

            return Id.Equals(ParkingSpace.Id);

        }

        #endregion

        #endregion

        #region (override) GetHashCode()

        /// <summary>
        /// Get the hash code of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()
        {
            return "eMI3 charging service plan: " + Id.ToString();
        }

        #endregion

    }

}
