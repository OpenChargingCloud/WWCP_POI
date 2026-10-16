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
    /// Where the charging station is, seen from a vehicle parked on a parking space, as in OCPI.
    /// </summary>
    public enum EVSEPosition
    {

        /// <summary>
        /// On the left of the parking space.
        /// </summary>
        LEFT,

        /// <summary>
        /// On the right of the parking space.
        /// </summary>
        RIGHT,

        /// <summary>
        /// In front of or behind the middle of the parking space.
        /// </summary>
        CENTER

    }


    /// <summary>
    /// How a parking space lies to the road or aisle it is entered from, as in OCPI.
    /// </summary>
    public enum ParkingDirection
    {

        /// <summary>
        /// Along it.
        /// </summary>
        PARALLEL,

        /// <summary>
        /// At a right angle to it.
        /// </summary>
        PERPENDICULAR,

        /// <summary>
        /// At an angle to it, between parallel and perpendicular.
        /// </summary>
        ANGLE

    }


    /// <summary>
    /// The surface of a parking space, as in OCPI.
    /// </summary>
    public enum ParkingSurface
    {

        /// <summary>
        /// Asphalt.
        /// </summary>
        ASPHALT,

        /// <summary>
        /// Concrete.
        /// </summary>
        CONCRETE,

        /// <summary>
        /// Tiles or paving stones.
        /// </summary>
        TILES,

        /// <summary>
        /// Gravel.
        /// </summary>
        GRAVEL,

        /// <summary>
        /// Grass, e.g. on lawn grids.
        /// </summary>
        GRASS,

        /// <summary>
        /// Bare soil.
        /// </summary>
        SOIL

    }


    /// <summary>
    /// The slope of a parking space, as in OCPI.
    /// </summary>
    public enum ParkingSlope
    {

        /// <summary>
        /// Flat.
        /// </summary>
        FLAT,

        /// <summary>
        /// Sloped.
        /// </summary>
        SLOPED,

        /// <summary>
        /// Steeply sloped.
        /// </summary>
        STEEPLY_SLOPED

    }

}
