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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Where the EVSEs of a charging station operator can be paid for with EV Taler, by which tariff and with which
    /// exchanges (concepts/EVTaler.md, sections 4.1 to 4.3): what a driver's app or a server shows as
    /// "EV Taler (provider)". Nothing here takes or makes a payment.
    /// </summary>
    public sealed partial class ChargingStationOperator
    {

        #region TryGetEVTalerTariff(EVSEId, out ChargingTariff)

        /// <summary>
        /// Try to return the EV Taler tariff of the given EVSE of this operator (concepts/EVTaler.md, section 4.3), as it is
        /// at runtime: of the charging tariffs that apply to it, the one of the type AD_HOC_PAYMENT, or else the one without
        /// a type; that one, when it names no payment means or EV_TALER among them. There is none where this operator has no
        /// EV Taler merchant, or the charging pool of the EVSE, as it is at runtime, does not name the payment option EVTaler.
        /// Its prices may be stated anew at runtime (SPECIFICATION.md, section 12); its currency is the one of the steps.
        /// </summary>
        /// <param name="EVSEId">An EVSE of this operator.</param>
        /// <param name="ChargingTariff">Its EV Taler tariff, as it is at runtime.</param>
        public Boolean TryGetEVTalerTariff(EVSE_Id                                  EVSEId,
                                           [NotNullWhen(true)] out ChargingTariff?  ChargingTariff)
        {

            ChargingTariff = null;

            if (EVTaler is null ||
                !TryGetEffectiveEVSE(EVSEId, out var evse) ||
                ChargingPools.FirstOrDefault(chargingPool => chargingPool.EVSEs.Any(poolEVSE => poolEVSE.Id == EVSEId)) is not ChargingPool chargingPool ||
                !TryGetEffectiveChargingPool(chargingPool.Id, out var effectiveChargingPool) ||
                !effectiveChargingPool.PaymentOptions.Contains(PaymentOptions.EVTaler))
            {
                return false;
            }

            ChargingTariff? adHocTariff    = null;
            ChargingTariff? untypedTariff  = null;

            foreach (var chargingTariffId in evse.ChargingTariffIds)
                if (TryGetEffectiveChargingTariff(chargingTariffId, out var chargingTariff))
                {

                    if (chargingTariff.TariffType == ChargingTariffType.AD_HOC_PAYMENT)
                        adHocTariff   ??= chargingTariff;

                    else if (chargingTariff.TariffType is null)
                        untypedTariff ??= chargingTariff;

                }

            var evTalerTariff = adHocTariff ?? untypedTariff;

            if (evTalerTariff is null ||
                evTalerTariff.Currency is null ||
                (evTalerTariff.PaymentMeans.Length > 0 && !evTalerTariff.PaymentMeans.Contains(PaymentMeans.EV_TALER)))
            {
                return false;
            }

            ChargingTariff = evTalerTariff;
            return true;

        }

        #endregion

        #region EVTalerExchangesAt(EVSEId)

        /// <summary>
        /// The exchanges whose coins pay for charging at the given EVSE of this operator: those of its EV Taler merchant in
        /// the currency of the EV Taler tariff of the EVSE (TryGetEVTalerTariff), in the ordinal order of their master
        /// public keys; none where the EVSE has no EV Taler tariff. Their PaymentOptionName is what drivers see, e.g.
        /// "EV Taler (Example Exchange, EUR)".
        /// </summary>
        /// <param name="EVSEId">An EVSE of this operator.</param>
        public IEnumerable<EVTalerExchange> EVTalerExchangesAt(EVSE_Id EVSEId)

            => TryGetEVTalerTariff(EVSEId, out var chargingTariff)
                   ? EVTaler!.ExchangesFor(chargingTariff.Currency!)
                   : [];

        #endregion

    }

}
