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
    /// For which charging sessions a charging tariff is, as in OCPI: an EVSE may name several tariffs,
    /// one per type. A tariff without a type is for every charging session.
    /// </summary>
    public enum ChargingTariffType
    {

        /// <summary>
        /// For charging sessions paid ad hoc, e.g. by a payment terminal or a website, without a contract.
        /// </summary>
        AD_HOC_PAYMENT,

        /// <summary>
        /// For charging sessions whose driver prefers to charge cheaply.
        /// </summary>
        PROFILE_CHEAP,

        /// <summary>
        /// For charging sessions whose driver prefers to charge fast.
        /// </summary>
        PROFILE_FAST,

        /// <summary>
        /// For charging sessions whose driver prefers green energy.
        /// </summary>
        PROFILE_GREEN,

        /// <summary>
        /// For charging sessions with a contract, without a preference.
        /// </summary>
        REGULAR

    }

}
