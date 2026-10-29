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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// How charging at an EVSE or a charging pool can be paid with EV Taler (concepts/EVTaler.md, section 4): the EV Taler
    /// tariff of an EVSE, the merchant of its operator and the exchanges in the currency of the tariff, as the operator
    /// tells them (TryGetEVTalerTariff, EVTalerExchangesAt), so that an app or a web page shows "EV Taler (provider)"
    /// without the rules of section 4.3. Nothing here makes an offer or takes a payment.
    /// </summary>
    /// <remarks>
    /// GET  ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/evTaler                                  its EVSEs with an EV Taler tariff, the merchant, and the
    ///                                                                                    exchanges of all of them
    /// GET  ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/evTaler        its EV Taler tariff, the merchant and the exchanges in its currency
    /// GET  ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/evTaler                                             the same, found by the identification alone
    ///
    /// 404 Not Found where the pool or the EVSE offers no EV Taler.
    /// </remarks>
    public partial class POI_API
    {

        #region (private) RegisterEVTalerURLTemplates()

        private void RegisterEVTalerURLTemplates()
        {

            var roamingNetworkPath   = HTTPPath.Root + "RNs/{roamingNetworkId}";
            var chargingPoolPath     = roamingNetworkPath + "/CSOs/{chargingStationOperatorId}/CPs/{chargingPoolId}";
            var chargingStationPath  = chargingPoolPath + "/CSs/{chargingStationId}";

            #region GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/evTaler

            AddEntityHandlers(chargingPoolPath + "/evTaler",
                              request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse) &&
                                         TryGetRequestedChargingPool           (request, out var chargingPool,            out errorResponse)
                                             ? ChargingPoolEVTalerResponse(request, chargingStationOperator, chargingPool)
                                             : errorResponse);

            #endregion

            #region GET ~/RNs/{RN_Id}/CSOs/{CSO_Id}/CPs/{CP_Id}/CSs/{CS_Id}/EVSEs/{EVSE_Id}/evTaler

            AddEntityHandlers(chargingStationPath + "/EVSEs/{evseId}/evTaler",
                              request => TryGetRequestedChargingStationOperator(request, out var chargingStationOperator, out var errorResponse) &&
                                         TryGetRequestedEVSE                   (request, out var evse,                    out errorResponse)
                                             ? EVSEEVTalerResponse(request, chargingStationOperator, evse.Id)
                                             : errorResponse);

            #endregion

            #region GET ~/RNs/{RN_Id}/EVSEs/{EVSE_Id}/evTaler

            AddEntityHandlers(roamingNetworkPath + "/EVSEs/{id}/evTaler",
                              request => {

                                  if (!EVSE_Id.TryParse(request.ParsedURLParameters.ElementAtOrDefault(1) ?? "", out var evseId))
                                      return EntityErrorResponse(request, HTTPStatusCode.BadRequest, "Invalid EVSE identification!");

                                  if (!TryGetRequestedRoamingNetwork(request, out var roamingNetwork, out var errorResponse))
                                      return errorResponse;

                                  return roamingNetwork.TryGetChargingStationOperatorById(evseId.OperatorId, out var chargingStationOperator)
                                             ? EVSEEVTalerResponse(request, chargingStationOperator, evseId)
                                             : EntityErrorResponse(request, HTTPStatusCode.NotFound, $"The EVSE '{evseId}' is unknown!");

                              });

            #endregion

        }

        #endregion


        #region (private) EVSEEVTalerResponse        (Request, ChargingStationOperator, EVSEId)

        /// <summary>
        /// The EV Taler tariff of the given EVSE of the given operator, its merchant and the exchanges in the currency of the
        /// tariff: { EVSEId, chargingTariffId, currency, merchantBaseURL, merchantPublicKey, exchanges }; 404 where the EVSE
        /// offers no EV Taler.
        /// </summary>
        private static HTTPResponse EVSEEVTalerResponse(HTTPRequest              Request,
                                                        ChargingStationOperator  ChargingStationOperator,
                                                        EVSE_Id                  EVSEId)

            => ChargingStationOperator.TryGetEVTalerTariff(EVSEId, out var chargingTariff)

                   ? EntityResponse(Request,
                                    new JObject(
                                        new JProperty("EVSEId",              EVSEId.ToString()),
                                        new JProperty("chargingTariffId",    chargingTariff.Id.ToString()),
                                        new JProperty("currency",            chargingTariff.Currency!.ISOCode),
                                        new JProperty(EVTalerMerchant.PropertyKeys.MerchantBaseURL,    ChargingStationOperator.EVTaler!.MerchantBaseURL.ToString()),
                                        new JProperty(EVTalerMerchant.PropertyKeys.MerchantPublicKey,  ChargingStationOperator.EVTaler.MerchantPublicKey),
                                        new JProperty(EVTalerMerchant.PropertyKeys.Exchanges,          ExchangesJSON(ChargingStationOperator.EVTalerExchangesAt(EVSEId)))
                                    ))

                   : EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The EVSE '{EVSEId}' offers no EV Taler!");

        #endregion

        #region (private) ChargingPoolEVTalerResponse(Request, ChargingStationOperator, ChargingPool)

        /// <summary>
        /// The EVSEs of the given charging pool with an EV Taler tariff, each with its tariff and currency, the merchant of
        /// the operator and the exchanges of all of them: { chargingPoolId, merchantBaseURL, merchantPublicKey, EVSEs,
        /// exchanges }; 404 where none of its EVSEs offers EV Taler.
        /// </summary>
        private static HTTPResponse ChargingPoolEVTalerResponse(HTTPRequest              Request,
                                                                ChargingStationOperator  ChargingStationOperator,
                                                                ChargingPool             ChargingPool)
        {

            var evses      = new JArray();
            var exchanges  = new SortedDictionary<String, EVTalerExchange>(StringComparer.Ordinal);

            foreach (var evseId in ChargingPool.EVSEs.Select(evse => evse.Id).Order())
                if (ChargingStationOperator.TryGetEVTalerTariff(evseId, out var chargingTariff))
                {

                    evses.Add(new JObject(
                                  new JProperty("EVSEId",            evseId.ToString()),
                                  new JProperty("chargingTariffId",  chargingTariff.Id.ToString()),
                                  new JProperty("currency",          chargingTariff.Currency!.ISOCode)
                              ));

                    foreach (var exchange in ChargingStationOperator.EVTalerExchangesAt(evseId))
                        exchanges.TryAdd(exchange.MasterPublicKey, exchange);

                }

            if (evses.Count == 0)
                return EntityErrorResponse(Request, HTTPStatusCode.NotFound, $"The charging pool '{ChargingPool.Id}' offers no EV Taler!");

            return EntityResponse(Request,
                                  new JObject(
                                      new JProperty("chargingPoolId",    ChargingPool.Id.ToString()),
                                      new JProperty(EVTalerMerchant.PropertyKeys.MerchantBaseURL,    ChargingStationOperator.EVTaler!.MerchantBaseURL.ToString()),
                                      new JProperty(EVTalerMerchant.PropertyKeys.MerchantPublicKey,  ChargingStationOperator.EVTaler.MerchantPublicKey),
                                      new JProperty("EVSEs",             evses),
                                      new JProperty(EVTalerMerchant.PropertyKeys.Exchanges,          ExchangesJSON(exchanges.Values))
                                  ));

        }

        #endregion

        #region (private static) ExchangesJSON(Exchanges)

        /// <summary>
        /// The given exchanges as their operator writes them, each with the payment option as drivers see it,
        /// e.g. "EV Taler (Example Exchange, EUR)".
        /// </summary>
        private static JArray ExchangesJSON(IEnumerable<EVTalerExchange> Exchanges)

            => new (Exchanges.Select(exchange => {
                        var json = exchange.ToJSON(Embedded: true);
                        json.Add("paymentOptionName", exchange.PaymentOptionName.ToJSON());
                        return json;
                    }));

        #endregion

    }

}
