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
    /// What an energy meter must not contradict of the model it names (SPECIFICATION.md, section 4.1, "What a model
    /// rates"). Compared: its signatureMethods, each one its model names, where its model names any: a transparency
    /// software verifies a measurement by the method of the model it was certified for. The other data of a model describe
    /// it, e.g. its current types, maximum current, accuracy class and measurands. A rule more is a method more here.
    /// </summary>
    public static class EnergyMeterModelComparer
    {

        #region Contradiction(EnergyMeter, Model, ManufacturerId)

        /// <summary>
        /// The first contradiction of the given energy meter, as it is at runtime with what its templates give it, to the
        /// given model it names, in words; null when there is none.
        /// </summary>
        /// <param name="EnergyMeter">An energy meter.</param>
        /// <param name="Model">The model it names.</param>
        /// <param name="ManufacturerId">The manufacturer of the model.</param>
        public static String? Contradiction(EnergyMeter                 EnergyMeter,
                                            EnergyMeterModel            Model,
                                            EnergyMeterManufacturer_Id  ManufacturerId)

            => SignatureMethodNotOfModel(EnergyMeter, Model, ManufacturerId);

        #endregion


        #region (private) SignatureMethodNotOfModel(EnergyMeter, Model, ManufacturerId)

        /// <summary>
        /// The meter signs with the methods of its model only.
        /// </summary>
        private static String? SignatureMethodNotOfModel(EnergyMeter                 EnergyMeter,
                                                         EnergyMeterModel            Model,
                                                         EnergyMeterManufacturer_Id  ManufacturerId)
        {

            if (Model.SignatureMethods.IsDefaultOrEmpty)
                return null;

            foreach (var signatureMethod in EnergyMeter.SignatureMethods)
                if (!Model.SignatureMethods.Contains(signatureMethod))
                    return $"The energy meter '{EnergyMeter.Id}' signs with '{signatureMethod}', no signature method of its model '{Model.Id}' of '{ManufacturerId}'!";

            return null;

        }

        #endregion

    }

}
