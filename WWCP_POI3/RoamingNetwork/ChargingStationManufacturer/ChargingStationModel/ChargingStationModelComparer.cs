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

using System.Globalization;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a charging station must not contradict of the model it names (SPECIFICATION.md, section 4.1, "What a model
    /// rates"). Compared: its maxPower, and the one of each of its EVSEs, not above the one of its model, where both name
    /// one. The other data of a model describe it, and a station may differ from them, e.g. its connector types, current
    /// types and number of EVSEs, as an operator may give a station fewer, or count its EVSEs otherwise. A rule more is a
    /// method more here.
    /// </summary>
    public static class ChargingStationModelComparer
    {

        #region Contradiction(ChargingStation, Model, ManufacturerId)

        /// <summary>
        /// The first contradiction of the given charging station, as it is at runtime with what its templates give it, to
        /// the given model it names, in words; null when there is none.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="Model">The model it names.</param>
        /// <param name="ManufacturerId">The manufacturer of the model.</param>
        public static String? Contradiction(ChargingStation                 ChargingStation,
                                            ChargingStationModel            Model,
                                            ChargingStationManufacturer_Id  ManufacturerId)

            => MaxPowerAbove(ChargingStation, Model, ManufacturerId);

        #endregion


        #region (private) MaxPowerAbove(ChargingStation, Model, ManufacturerId)

        /// <summary>
        /// The station delivers no more than its model, nor does any of its EVSEs.
        /// </summary>
        private static String? MaxPowerAbove(ChargingStation                 ChargingStation,
                                             ChargingStationModel            Model,
                                             ChargingStationManufacturer_Id  ManufacturerId)
        {

            if (Model.MaxPower is not Watt modelMaxPower)
                return null;

            if (ChargingStation.MaxPower is Watt maxPower && maxPower > modelMaxPower)
                return $"The maximum power of the charging station '{ChargingStation.Id}', {KW(maxPower)}, is above the one of its model '{Model.Id}' of '{ManufacturerId}', {KW(modelMaxPower)}!";

            foreach (var evse in ChargingStation.EVSEs)
                if (evse.MaxPower is Watt evseMaxPower && evseMaxPower > modelMaxPower)
                    return $"The maximum power of the EVSE '{evse.Id}', {KW(evseMaxPower)}, is above the one of the model '{Model.Id}' of '{ManufacturerId}' of its charging station '{ChargingStation.Id}', {KW(modelMaxPower)}!";

            return null;

        }

        private static String KW(Watt Power)
            => Power.ToString("kW", CultureInfo.InvariantCulture);

        #endregion

    }

}
