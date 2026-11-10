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
    /// Charging at the operator's charging station management system, as the backend of EV Taler sessions needs it
    /// (concepts/EVTaler.md, section 5.6): started and limited to the credit paid, the limit raised with every step paid,
    /// stopped; with OCPP 2.1 RequestStartTransaction with a transactionLimit, the limit raised in the answer to the next
    /// TransactionEvent (after a TriggerMessage), RequestStopTransaction. The system tells the backend what happens through
    /// IEVTalerChargingEvents: the start with its signed meter value (Transaction.Begin), what was delivered, and the end
    /// with its signed meter value (Transaction.End).
    /// </summary>
    public interface IEVTalerChargingControl
    {

        /// <summary>
        /// Why the given EVSE takes no session now, e.g. no vehicle plugged in or one charging already; null when it does.
        /// </summary>
        Task<String?> WhyNotStart(EVSE_Id            EVSEId,
                                  CancellationToken  CancellationToken);

        /// <summary>
        /// Start charging at the given EVSE for the given session, limited to the given credit; what happens is told to
        /// the given events. Null when it started, else why not.
        /// </summary>
        Task<String?> Start(EVSE_Id                  EVSEId,
                            String                   SessionId,
                            EVTalerChargingLimit     Limit,
                            IEVTalerChargingEvents   Events,
                            CancellationToken        CancellationToken);

        /// <summary>
        /// Raise the limit of the given session to the given credit.
        /// </summary>
        Task RaiseLimit(String                SessionId,
                        EVTalerChargingLimit  Limit,
                        CancellationToken     CancellationToken);

        /// <summary>
        /// Stop charging of the given session; its end is told as every other end.
        /// </summary>
        Task Stop(String             SessionId,
                  CancellationToken  CancellationToken);

    }


    /// <summary>
    /// What the charging station management system tells the backend of an EV Taler session.
    /// </summary>
    public interface IEVTalerChargingEvents
    {

        /// <summary>
        /// Charging started, with the signed meter value at the start (OCPP 2.1 Transaction.Begin), if the station gave one.
        /// </summary>
        Task Started(String                    SessionId,
                     EVTalerSignedMeterValue?  StartValue);

        /// <summary>
        /// What was delivered since the start, in the unit of the steps: kWh, or minutes.
        /// </summary>
        Task Delivered(String   SessionId,
                       Decimal  Quantity);

        /// <summary>
        /// Charging ended (EVTalerStopReasons), with the signed meter value at the end (OCPP 2.1 Transaction.End), if the
        /// station gave one.
        /// </summary>
        Task Ended(String                    SessionId,
                   EVTalerSignedMeterValue?  EndValue,
                   String                    Reason);

    }


    /// <summary>
    /// The limit of charging: the credit paid, counted from the start of the session, in the unit of its steps; OCPP 2.1's
    /// transactionLimit maxEnergy (in Wh) for kWh, maxTime (in seconds) for minutes.
    /// </summary>
    /// <param name="Quantity">The credit.</param>
    /// <param name="Unit">EVTalerCovers.KWh or EVTalerCovers.Minutes.</param>
    public sealed record EVTalerChargingLimit(Decimal  Quantity,
                                              String   Unit)
    {

        /// <summary>
        /// OCPP 2.1's maxEnergy, in Wh, for a limit in kWh.
        /// </summary>
        public Decimal? MaxEnergyWh
            => Unit == EVTalerCovers.KWh ? Quantity * 1000m : null;

        /// <summary>
        /// OCPP 2.1's maxTime, in seconds, for a limit in minutes.
        /// </summary>
        public Decimal? MaxTimeSeconds
            => Unit == EVTalerCovers.Minutes ? Quantity * 60m : null;

        public override String ToString()
            => $"{EVTalerAmounts.WriteDecimal(Quantity)} {Unit}";

    }

}
